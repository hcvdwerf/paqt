namespace Wmo.Domain;

public enum DomainError
{
    DateOutOfRange,
    InvalidDistance,
    InactivePermit,
    OutsideBudgetPeriod,
    InsufficientBudget,
    NoPermit,
    ParcelUnavailable
}

public sealed class DomainException(DomainError code, string message) : Exception(message)
{
    public DomainError Code { get; } = code;
}
