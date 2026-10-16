using Microsoft.EntityFrameworkCore;
using Wmo.Domain;

namespace Wmo.Infrastructure;

public sealed class WmoDbContext(DbContextOptions<WmoDbContext> options) : DbContext(options)
{
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<Permit> Permits => Set<Permit>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<TaxiCompany> TaxiCompanies => Set<TaxiCompany>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Resident>().HasOne<Parcel>().WithMany().HasForeignKey(r => r.ParcelId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Resident>().Property(r => r.Name).HasMaxLength(150);
        model.Entity<Resident>().Property(r => r.HomeAddress).HasMaxLength(300);
        model.Entity<Resident>().Property(r => r.Municipality).HasMaxLength(100);
        model.Entity<Parcel>().HasOne<TaxiCompany>().WithMany().HasForeignKey(p => p.TaxiCompanyId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Permit>().HasIndex(p => p.ResidentId).IsUnique();
        model.Entity<Permit>().HasOne<Resident>().WithMany().HasForeignKey(p => p.ResidentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Permit>().Property(p => p.AnnualKilometers).HasPrecision(12, 3);
        model.Entity<Permit>().Property(p => p.RemainingKilometers).HasPrecision(12, 3);
        model.Entity<Permit>().ToTable(t => t.HasCheckConstraint("CK_Permit_Budget", "\"RemainingKilometers\" >= 0 AND \"RemainingKilometers\" <= \"AnnualKilometers\" AND \"AnnualKilometers\" > 0"));
        model.Entity<Trip>().Property(t => t.DistanceKilometers).HasPrecision(12, 3);
        model.Entity<Trip>().Property(t => t.PickupAddress).HasMaxLength(300);
        model.Entity<Trip>().Property(t => t.DestinationAddress).HasMaxLength(300);
        model.Entity<Trip>().HasOne<Resident>().WithMany().HasForeignKey(t => t.ResidentId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Trip>().HasOne<Permit>().WithMany().HasForeignKey(t => t.PermitId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Trip>().HasOne<TaxiCompany>().WithMany().HasForeignKey(t => t.TaxiCompanyId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Trip>().HasIndex(t => new { t.TaxiCompanyId, t.DepartureAt, t.Id });
        model.Entity<Trip>().ToTable(t => t.HasCheckConstraint("CK_Trip_Distance", "\"DistanceKilometers\" > 0"));
        model.Entity<AuditEvent>().Property(a => a.ActorId).HasMaxLength(200);
        model.Entity<AuditEvent>().Property(a => a.Action).HasMaxLength(100);
        model.Entity<AuditEvent>().Property(a => a.CorrelationId).HasMaxLength(64);
    }
}
