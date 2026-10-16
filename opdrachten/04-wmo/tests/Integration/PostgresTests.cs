using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wmo.Application;
using Wmo.Domain;
using Wmo.Infrastructure;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Wmo.Integration;

public sealed class PostgresTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-16T10:00:00Z");
    private static readonly DateOnly Today = BusinessClock.LocalDate(Now);
    private readonly string connection = new NpgsqlConnectionStringBuilder
    {
        Host = "postgres",
        Database = "wmo_test",
        Username = "wmo_owner",
        Password = File.ReadAllText("/secrets/wmo-owner-password").Trim(),
        SslMode = SslMode.VerifyFull,
        RootCertificate = "/certs/ca.crt"
    }.ConnectionString;
    private WmoDbContext Open() => new(new DbContextOptionsBuilder<WmoDbContext>().UseNpgsql(connection).Options);
    private static BusinessClock Clock() => new(new FixedClock(Now));
    private static BookingService Booking(WmoDbContext db)
    {
        var store = new PostgresWmoStore(db);
        return new(store, store, store, Clock());
    }
    private static RenewalService Renewals(WmoDbContext db)
    {
        var store = new PostgresWmoStore(db);
        return new(store, store, store, Clock());
    }
    private static Actor Actor => new("test:callcenter", Guid.NewGuid().ToString("N"));
    private static BookingRequest Request(decimal distance) => new(Now.AddHours(1), "Fictief bestemmingsplein 1", distance);

    public async Task InitializeAsync()
    {
        await using var db = Open();
        // Alleen de expliciete testdatabase; nooit de demodatabase wissen.
        Assert.Equal("wmo_test", db.Database.GetDbConnection().Database);
        await db.Database.ExecuteSqlRawAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db, Today);
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Booking_selects_home_carrier_and_persists_with_budget_and_audit()
    {
        await using (var db = Open())
        {
            var trip = await Booking(db).BookAsync(SeedData.ResidentId(2), Request(12.5m), Actor, default);
            Assert.Equal(SeedData.CompanyB, trip.TaxiCompanyId);
        }
        await using var reopened = Open();
        Assert.Equal(87.5m, (await reopened.Permits.SingleAsync(p => p.ResidentId == SeedData.ResidentId(2))).RemainingKilometers);
        Assert.Single(await reopened.Trips.ToListAsync());
        Assert.Single(await reopened.AuditEvents.Where(a => a.Action == "trip.booked").ToListAsync());
    }

    [Fact]
    public async Task Concurrent_bookings_cannot_overspend()
    {
        async Task<bool> Attempt()
        {
            await using var db = Open();
            try { await Booking(db).BookAsync(SeedData.ResidentId(1), Request(60), Actor, default); return true; }
            catch (DomainException e) when (e.Code == DomainError.InsufficientBudget) { return false; }
        }
        var results = await Task.WhenAll(Attempt(), Attempt());
        Assert.Single(results, success => success);
        await using var check = Open();
        Assert.Equal(40m, (await check.Permits.SingleAsync(p => p.ResidentId == SeedData.ResidentId(1))).RemainingKilometers);
        Assert.Single(await check.Trips.ToListAsync());
    }

    [Fact]
    public async Task Concurrent_renewals_and_booking_do_not_refill_spent_budget()
    {
        async Task Renew() { await using var db = Open(); await Renewals(db).RenewAsync(Actor, default); }
        async Task Book() { await using var db = Open(); await Booking(db).BookAsync(SeedData.ResidentId(5), Request(10), Actor, default); }
        await Task.WhenAll(Renew(), Renew(), Book());
        await Renew();
        await using var check = Open();
        var permit = await check.Permits.SingleAsync(p => p.ResidentId == SeedData.ResidentId(5));
        Assert.Equal(90m, permit.RemainingKilometers);
        Assert.True(permit.PeriodStart <= Today && permit.PeriodEnd > Today);
        Assert.Single(await check.AuditEvents.Where(a => a.Action == "permit.renewed").ToListAsync());
    }

    [Fact]
    public async Task Failed_booking_rolls_back_renewal_and_audit()
    {
        await using (var db = Open())
            await Assert.ThrowsAsync<DomainException>(() => Booking(db).BookAsync(SeedData.ResidentId(5), Request(101), Actor, default));
        await using var check = Open();
        Assert.True((await check.Permits.SingleAsync(p => p.ResidentId == SeedData.ResidentId(5))).PeriodEnd <= Today);
        Assert.Empty(await check.AuditEvents.ToListAsync());
    }

    [Fact]
    public async Task Queries_exclude_other_municipalities_and_other_carriers()
    {
        await using var db = Open();
        await Booking(db).BookAsync(SeedData.ResidentId(2), Request(1), Actor, default);
        var queries = new PostgresWmoStore(db);
        var first = await queries.ResidentsAsync(1, 2, default);
        Assert.Equal(6, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Empty((await queries.TripsAsync(SeedData.CompanyA, 1, 50, default)).Items);
        Assert.Single((await queries.TripsAsync(SeedData.CompanyB, 1, 50, default)).Items);
    }

    [Fact]
    public async Task Runtime_account_cannot_modify_audit_or_schema()
    {
        var builder = new NpgsqlConnectionStringBuilder(connection) { Username = "wmo", Password = File.ReadAllText("/secrets/wmo-password").Trim() };
        await using var db = new WmoDbContext(new DbContextOptionsBuilder<WmoDbContext>().UseNpgsql(builder.ConnectionString).Options);
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"AuditEvents\""));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("SELECT * FROM \"__EFMigrationsHistory\""));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("CREATE TABLE forbidden (id int)"));
    }

    [Fact]
    public async Task Stopped_permit_cannot_be_booked_or_renewed()
    {
        await using var db = Open();
        await Assert.ThrowsAsync<DomainException>(() => Booking(db).BookAsync(SeedData.ResidentId(4), Request(1), Actor, default));
        await Renewals(db).RenewAsync(Actor, default);
        Assert.Equal(Today, (await db.Permits.AsNoTracking().SingleAsync(p => p.ResidentId == SeedData.ResidentId(4))).StoppedOn);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
