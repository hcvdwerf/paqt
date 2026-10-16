using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Wmo.Infrastructure;

#nullable disable

namespace Wmo.Infrastructure.Migrations;

[DbContext(typeof(WmoDbContext))]
[Migration("20261016115000_RemoveUnusedLocationNames")]
public sealed class RemoveUnusedLocationNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Name",
            table: "Parcels");

        migrationBuilder.DropColumn(
            name: "Name",
            table: "TaxiCompanies");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "Parcels",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "TaxiCompanies",
            type: "character varying(150)",
            maxLength: 150,
            nullable: false,
            defaultValue: "");
    }
}
