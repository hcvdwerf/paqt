using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wmo.Infrastructure;

// Alleen voor migratiegeneratie. Er wordt geen verbinding geopend bij het genereren.
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<WmoDbContext>
{
    public WmoDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<WmoDbContext>()
        .UseNpgsql("Host=localhost;Database=wmo;Username=migrations").Options);
}
