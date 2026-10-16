using Microsoft.EntityFrameworkCore;
using Wmo.Application;
using Wmo.Domain;

namespace Wmo.Infrastructure;

public sealed class PostgresWmoStore(WmoDbContext db) : IUnitOfWork, IBookingData, IRenewalData, IAuditWriter, IWmoQueries
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        finally
        {
            // De achtergrondtaak verwerkt meerdere transacties in dezelfde scope.
            db.ChangeTracker.Clear();
        }
    }

    public Task<Resident?> FindResidentAsync(Guid id, CancellationToken ct) => db.Residents.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
    public Task<Parcel?> FindParcelAsync(Guid id, CancellationToken ct) => db.Parcels.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);
    public Task<Permit?> LockPermitForResidentAsync(Guid id, CancellationToken ct) => db.Permits
        .FromSqlInterpolated($"SELECT * FROM \"Permits\" WHERE \"ResidentId\" = {id} FOR UPDATE").SingleOrDefaultAsync(ct);
    public Task<Permit> LockPermitAsync(Guid id, CancellationToken ct) => db.Permits
        .FromSqlInterpolated($"SELECT * FROM \"Permits\" WHERE \"Id\" = {id} FOR UPDATE").SingleAsync(ct);
    public async Task<IReadOnlyList<Guid>> DuePermitIdsAsync(DateOnly today, CancellationToken ct) => await db.Permits.AsNoTracking()
        .Where(p => p.StartDate <= today && p.PeriodEnd <= today && (p.StoppedOn == null || p.StoppedOn > today))
        .OrderBy(p => p.Id).Select(p => p.Id).ToListAsync(ct);
    public void AddTrip(Trip trip) => db.Trips.Add(trip);
    public void Append(AuditEvent auditEvent) => db.AuditEvents.Add(auditEvent);

    public async Task<Page<ResidentView>> ResidentsAsync(int page, int size, CancellationToken ct)
    {
        var query = db.Residents.AsNoTracking().Where(r => r.Municipality == "Utrecht");
        var total = await query.CountAsync(ct);
        var residents = await query.OrderBy(r => r.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var ids = residents.Select(r => r.Id).ToArray();
        var permits = await db.Permits.AsNoTracking().Where(p => ids.Contains(p.ResidentId)).ToDictionaryAsync(p => p.ResidentId, ct);
        var result = residents.Select(r => new ResidentView(r.Id, r.Name, r.HomeAddress,
            permits.TryGetValue(r.Id, out var p) ? new BudgetView(p.PeriodStart, p.PeriodEnd, p.RemainingKilometers, p.StoppedOn) : null)).ToArray();
        return new(result, page, size, total);
    }

    public async Task<Page<TripView>> TripsAsync(Guid companyId, int page, int size, CancellationToken ct)
    {
        var query = db.Trips.AsNoTracking().Where(t => t.TaxiCompanyId == companyId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.DepartureAt).ThenBy(t => t.Id).Skip((page - 1) * size).Take(size)
            .Select(t => new TripView(t.Id, t.ResidentId, t.TaxiCompanyId, t.PickupAddress, t.DestinationAddress, t.DepartureAt, t.DistanceKilometers)).ToArrayAsync(ct);
        return new(items, page, size, total);
    }
}
