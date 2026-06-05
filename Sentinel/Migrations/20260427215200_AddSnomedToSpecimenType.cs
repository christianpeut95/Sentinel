using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddSnomedToSpecimenType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodySite",
                table: "SpecimenTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionMethod",
                table: "SpecimenTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SpecimenTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Hl7Code",
                table: "SpecimenTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoincSystemCode",
                table: "SpecimenTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "SpecimenTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnomedCode",
                table: "SpecimenTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnomedDisplay",
                table: "SpecimenTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodySite",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "CollectionMethod",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "Hl7Code",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "LoincSystemCode",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "SnomedCode",
                table: "SpecimenTypes");

            migrationBuilder.DropColumn(
                name: "SnomedDisplay",
                table: "SpecimenTypes");
        }
    }
}
