using Wmo.Application;
using Wmo.Domain;
using Wmo.Infrastructure;

namespace Wmo.Unit;

public class PermitTests
{
    private static Permit Create() => new(Guid.NewGuid(), Guid.NewGuid(), new(2024, 2, 29), 100m);

    [Fact]
    public void Leap_anniversary_returns_after_non_leap_years()
    {
        var permit = Create();
        Assert.True(permit.Renew(new(2025, 2, 28)));
        Assert.Equal(new DateOnly(2025, 2, 28), permit.PeriodStart);
        Assert.True(permit.Renew(new(2027, 3, 1)));
        Assert.Equal(new DateOnly(2028, 2, 29), permit.PeriodEnd);
        Assert.True(permit.Renew(new(2028, 2, 29)));
        Assert.Equal(new DateOnly(2028, 2, 29), permit.PeriodStart);
    }

    [Fact]
    public void Reset_is_idempotent_after_consumption()
    {
        var p = Create();
        var today = new DateOnly(2026, 3, 1);
        Assert.True(p.Renew(today));
        p.Book(today, today, 12.125m);
        Assert.False(p.Renew(today));
        Assert.Equal(87.875m, p.RemainingKilometers);
    }

    [Fact]
    public void Stopped_permit_is_not_renewed()
    {
        var p = new Permit(Guid.NewGuid(), Guid.NewGuid(), new(2024, 1, 1), 100, new(2025, 1, 1));
        Assert.False(p.Renew(new(2026, 1, 1)));
        Assert.False(p.IsActive(new(2025, 1, 1)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.1234)]
    [InlineData(101)]
    public void Invalid_distance_or_insufficient_budget_does_not_change_balance(decimal distance)
    {
        var p = Create();
        Assert.Throws<DomainException>(() => p.Book(new(2024, 3, 1), new(2024, 3, 1), distance));
        Assert.Equal(100m, p.RemainingKilometers);
    }

    [Fact]
    public void Exact_balance_can_be_spent()
    {
        var p = Create();
        p.Book(new(2024, 3, 1), new(2024, 3, 1), 100m);
        Assert.Equal(0m, p.RemainingKilometers);
    }

    [Fact]
    public void Next_budget_period_cannot_be_booked()
    {
        var p = Create();
        var ex = Assert.Throws<DomainException>(() => p.Book(new(2024, 3, 1), p.PeriodEnd, 1));
        Assert.Equal(DomainError.OutsideBudgetPeriod, ex.Code);
    }

    [Fact]
    public void Future_start_is_inactive()
    {
        var p = new Permit(Guid.NewGuid(), Guid.NewGuid(), new(2027, 1, 1), 100);
        Assert.False(p.Renew(new(2026, 12, 31)));
        Assert.Throws<DomainException>(() => p.Book(new(2026, 12, 31), new(2027, 1, 1), 1));
    }

    [Fact]
    public void Amsterdam_date_is_independent_of_server_timezone() => Assert.Equal(new DateOnly(2026, 1, 1),
        BusinessClock.LocalDate(DateTimeOffset.Parse("2025-12-31T23:30:00Z")));

    [Fact]
    public void Domain_and_application_do_not_reference_infrastructure()
    {
        var domain = typeof(Permit).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        var application = typeof(BookingService).Assembly.GetReferencedAssemblies().Select(a => a.Name!).ToArray();
        Assert.DoesNotContain(domain, n => n.StartsWith("Wmo.") || n.Contains("EntityFramework") || n.Contains("AspNetCore"));
        Assert.DoesNotContain(application, n => n.Contains("Infrastructure") || n.Contains("EntityFramework") || n.Contains("AspNetCore"));
        Assert.Contains("Wmo.Application", typeof(PostgresWmoStore).Assembly.GetReferencedAssemblies().Select(a => a.Name));
    }
}
