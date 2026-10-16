using Microsoft.EntityFrameworkCore;
using Wmo.Application;
using Wmo.Domain;

namespace Wmo.Infrastructure;

public static class SeedData
{
    public static readonly Guid CompanyA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid CompanyB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static Guid ResidentId(int number) => Guid.Parse($"30000000-0000-0000-0000-{number:D12}");

    public static async Task SeedAsync(WmoDbContext db, DateOnly today, CancellationToken ct = default)
    {
        if (await db.Residents.AnyAsync(ct)) return;
        var parcelA = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var parcelB = Guid.Parse("20000000-0000-0000-0000-000000000002");
        db.TaxiCompanies.AddRange(new TaxiCompany(CompanyA), new TaxiCompany(CompanyB));
        db.Parcels.AddRange(new Parcel(parcelA, CompanyA), new Parcel(parcelB, CompanyB));
        for (var i = 1; i <= 7; i++)
        {
            db.Residents.Add(new(ResidentId(i), $"Fictieve bewoner {i}", i == 7 ? "Zeist" : "Utrecht", $"Voorbeeldstraat {i}, Utrecht (fictief)", i == 2 ? parcelB : parcelA));
            if (i == 6 || i == 7) continue;
            var start = today.AddDays(-1).AddYears(i == 5 ? -2 : 0);
            db.Permits.Add(new(Guid.NewGuid(), ResidentId(i), start, i == 3 ? 5m : 100m, i == 4 ? today : null));
        }
        await db.SaveChangesAsync(ct);
    }
}
