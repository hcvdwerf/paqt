namespace Wmo.Domain;

public sealed class Permit
{
    private Permit() { }

    public Permit(Guid id, Guid residentId, DateOnly startDate, decimal annualKilometers, DateOnly? stoppedOn = null)
    {
        if (annualKilometers <= 0 || decimal.Round(annualKilometers, 3) != annualKilometers)
            throw new ArgumentOutOfRangeException(nameof(annualKilometers));
        if (startDate.Year == 9999) throw new ArgumentOutOfRangeException(nameof(startDate));
        if (stoppedOn < startDate) throw new ArgumentException("Stopdatum ligt voor de ingangsdatum.");
        Id = id;
        ResidentId = residentId;
        StartDate = PeriodStart = startDate;
        PeriodEnd = startDate.AddYears(1);
        AnnualKilometers = RemainingKilometers = annualKilometers;
        StoppedOn = stoppedOn;
    }

    public Guid Id { get; private set; }
    public Guid ResidentId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public DateOnly? StoppedOn { get; private set; }
    public decimal AnnualKilometers { get; private set; }
    public decimal RemainingKilometers { get; private set; }

    public bool IsActive(DateOnly date) => date >= StartDate && (StoppedOn is null || date < StoppedOn);

    public bool Renew(DateOnly today)
    {
        if (!IsActive(today) || today < PeriodEnd) return false;
        if (today.Year == 9999) throw new DomainException(DomainError.DateOutOfRange, "Geen volgende jaarperiode beschikbaar.");
        // Altijd rekenen vanaf de oorspronkelijke datum: 29 februari keert terug.
        var years = today.Year - StartDate.Year;
        if (StartDate.AddYears(years) > today) years--;
        PeriodStart = StartDate.AddYears(years);
        PeriodEnd = StartDate.AddYears(years + 1);
        RemainingKilometers = AnnualKilometers;
        return true;
    }

    public void Book(DateOnly today, DateOnly rideDate, decimal kilometers)
    {
        if (kilometers <= 0 || decimal.Round(kilometers, 3) != kilometers)
            throw new DomainException(DomainError.InvalidDistance, "Afstand moet positief zijn met maximaal drie decimalen.");
        if (!IsActive(today) || !IsActive(rideDate))
            throw new DomainException(DomainError.InactivePermit, "De beschikking is niet actief op de boekings- en ritdatum.");
        if (rideDate < today || rideDate < PeriodStart || rideDate >= PeriodEnd)
            throw new DomainException(DomainError.OutsideBudgetPeriod, "De rit moet binnen de huidige budgetperiode liggen.");
        if (kilometers > RemainingKilometers)
            throw new DomainException(DomainError.InsufficientBudget, "Het kilometerbudget is onvoldoende.");
        RemainingKilometers -= kilometers;
    }
}
