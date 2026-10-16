using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wmo.Infrastructure.Migrations;

[DbContext(typeof(WmoDbContext))]
[Migration("20261016114500_RuntimePermissions")]
public sealed class RuntimePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            GRANT USAGE ON SCHEMA public TO wmo;
            GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO wmo;
            REVOKE UPDATE, DELETE, TRUNCATE ON TABLE public."AuditEvents" FROM wmo;
            REVOKE ALL ON TABLE public."__EFMigrationsHistory" FROM wmo;
            ALTER DEFAULT PRIVILEGES FOR ROLE wmo_owner IN SCHEMA public
                GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO wmo;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM wmo;
            ALTER DEFAULT PRIVILEGES FOR ROLE wmo_owner IN SCHEMA public
                REVOKE SELECT, INSERT, UPDATE, DELETE ON TABLES FROM wmo;
            """);
    }
}
