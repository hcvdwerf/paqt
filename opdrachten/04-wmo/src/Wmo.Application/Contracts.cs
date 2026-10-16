using Wmo.Domain;

namespace Wmo.Application;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

public interface IBookingData
{
    Task<Resident?> FindResidentAsync(Guid id, CancellationToken cancellationToken);
    Task<Parcel?> FindParcelAsync(Guid id, CancellationToken cancellationToken);
    Task<Permit?> LockPermitForResidentAsync(Guid residentId, CancellationToken cancellationToken);
    void AddTrip(Trip trip);
}

public interface IRenewalData
{
    Task<IReadOnlyList<Guid>> DuePermitIdsAsync(DateOnly today, CancellationToken cancellationToken);
    Task<Permit> LockPermitAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAuditWriter { void Append(AuditEvent auditEvent); }

public interface IWmoQueries
{
    Task<Page<ResidentView>> ResidentsAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<Page<TripView>> TripsAsync(Guid taxiCompanyId, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount);
public sealed record ResidentView(Guid Id, string Name, string HomeAddress, BudgetView? Budget);
public sealed record BudgetView(DateOnly PeriodStart, DateOnly PeriodEnd, decimal RemainingKilometers, DateOnly? StoppedOn);
public sealed record TripView(Guid Id, Guid ResidentId, Guid TaxiCompanyId, string PickupAddress, string DestinationAddress,
    DateTimeOffset DepartureAt, decimal DistanceKilometers);
public sealed record BookingRequest(DateTimeOffset DepartureAt, string DestinationAddress, decimal DistanceKilometers);
public sealed record RenewalResult(int Renewed);
public sealed record Actor(string Id, string CorrelationId);

public sealed class NotFoundException() : Exception("Bewoner bestaat niet binnen gemeente Utrecht.");
public sealed class InputException(string message) : Exception(message);
