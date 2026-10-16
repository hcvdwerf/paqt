using Wmo.Domain;

namespace Wmo.Application;

public sealed class BusinessClock(TimeProvider timeProvider)
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
    public DateOnly Today => LocalDate(UtcNow);
    public static DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
}

public sealed class BookingService(IBookingData data, IUnitOfWork unitOfWork, IAuditWriter audit, BusinessClock clock)
{
    public Task<TripView> BookAsync(Guid residentId, BookingRequest request, Actor actor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationAddress) || request.DestinationAddress.Length > 300)
            throw new InputException("Geef een bestemmingsadres van maximaal 300 tekens op.");
        if (request.DistanceKilometers <= 0 || decimal.Round(request.DistanceKilometers, 3) != request.DistanceKilometers)
            throw new InputException("Afstand moet positief zijn met maximaal drie decimalen.");
        return unitOfWork.ExecuteAsync(async ct =>
        {
            var resident = await data.FindResidentAsync(residentId, ct);
            if (resident is null || resident.Municipality != "Utrecht") throw new NotFoundException();
            var permit = await data.LockPermitForResidentAsync(residentId, ct)
                ?? throw new DomainException(DomainError.NoPermit, "Bewoner heeft geen beschikking.");
            // Lees de klok na het verkrijgen van het slot: ook rond middernacht consistent.
            var now = clock.UtcNow;
            var today = BusinessClock.LocalDate(now);
            if (request.DepartureAt < now) throw new InputException("Het vertrektijdstip moet in de toekomst liggen.");
            if (permit.Renew(today))
                audit.Append(new(actor.Id, "permit.renewed", permit.Id, now, actor.CorrelationId));
            var parcel = await data.FindParcelAsync(resident.ParcelId, ct)
                ?? throw new DomainException(DomainError.ParcelUnavailable, "Geen vervoerder beschikbaar voor het woonadres.");
            permit.Book(today, BusinessClock.LocalDate(request.DepartureAt), request.DistanceKilometers);
            var trip = new Trip(resident.Id, permit.Id, parcel.TaxiCompanyId, resident.HomeAddress,
                request.DestinationAddress.Trim(), request.DepartureAt, request.DistanceKilometers);
            data.AddTrip(trip);
            audit.Append(new(actor.Id, "trip.booked", trip.Id, now, actor.CorrelationId));
            return new TripView(trip.Id, trip.ResidentId, trip.TaxiCompanyId, trip.PickupAddress,
                trip.DestinationAddress, trip.DepartureAt, trip.DistanceKilometers);
        }, cancellationToken);
    }
}

public sealed class RenewalService(IRenewalData data, IUnitOfWork unitOfWork, IAuditWriter audit, BusinessClock clock)
{
    public async Task<int> RenewAsync(Actor actor, CancellationToken cancellationToken)
    {
        var ids = await data.DuePermitIdsAsync(clock.Today, cancellationToken);
        var renewed = 0;
        foreach (var id in ids)
        {
            var changed = await unitOfWork.ExecuteAsync(async ct =>
            {
                var permit = await data.LockPermitAsync(id, ct);
                if (!permit.Renew(clock.Today)) return false;
                audit.Append(new(actor.Id, "permit.renewed", permit.Id, clock.UtcNow, actor.CorrelationId));
                return true;
            }, cancellationToken);
            if (changed) renewed++;
        }
        return renewed;
    }
}
