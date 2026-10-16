namespace Wmo.Domain;

public sealed class TaxiCompany
{
    private TaxiCompany() { }
    public TaxiCompany(Guid id) { Id = id; }
    public Guid Id { get; private set; }
}

public sealed class Parcel
{
    private Parcel() { }
    public Parcel(Guid id, Guid taxiCompanyId) { Id = id; TaxiCompanyId = taxiCompanyId; }
    public Guid Id { get; private set; }
    public Guid TaxiCompanyId { get; private set; }
}

public sealed class Resident
{
    private Resident() { }
    public Resident(Guid id, string name, string municipality, string homeAddress, Guid parcelId)
    { Id = id; Name = name; Municipality = municipality; HomeAddress = homeAddress; ParcelId = parcelId; }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Municipality { get; private set; } = "";
    public string HomeAddress { get; private set; } = "";
    public Guid ParcelId { get; private set; }
}

public sealed class Trip
{
    private Trip() { }
    public Trip(Guid residentId, Guid permitId, Guid taxiCompanyId, string pickupAddress, string destinationAddress,
        DateTimeOffset departureAt, decimal distanceKilometers)
    {
        Id = Guid.NewGuid(); ResidentId = residentId; PermitId = permitId; TaxiCompanyId = taxiCompanyId;
        PickupAddress = pickupAddress; DestinationAddress = destinationAddress;
        DepartureAt = departureAt.ToUniversalTime(); DistanceKilometers = distanceKilometers;
    }
    public Guid Id { get; private set; }
    public Guid ResidentId { get; private set; }
    public Guid PermitId { get; private set; }
    public Guid TaxiCompanyId { get; private set; }
    public string PickupAddress { get; private set; } = "";
    public string DestinationAddress { get; private set; } = "";
    public DateTimeOffset DepartureAt { get; private set; }
    public decimal DistanceKilometers { get; private set; }
}

public sealed class AuditEvent
{
    private AuditEvent() { }
    public AuditEvent(string actorId, string action, Guid objectId, DateTimeOffset timestamp, string correlationId)
    { Id = Guid.NewGuid(); ActorId = actorId; Action = action; ObjectId = objectId; Timestamp = timestamp.ToUniversalTime(); CorrelationId = correlationId; }
    public Guid Id { get; private set; }
    public string ActorId { get; private set; } = "";
    public string Action { get; private set; } = "";
    public Guid ObjectId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string CorrelationId { get; private set; } = "";
}
