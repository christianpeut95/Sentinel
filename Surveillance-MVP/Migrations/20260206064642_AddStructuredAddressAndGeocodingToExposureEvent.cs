using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredAddressAndGeocodingToExposureEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "ExposureEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "ExposureEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "ExposureEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeocodedDate",
                table: "ExposureEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeocodingAccuracy",
                table: "ExposureEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReportingExposure",
                table: "ExposureEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "ExposureEvents",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "ExposureEvents",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "ExposureEvents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "ExposureEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_City",
                table: "ExposureEvents",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_IsReportingExposure",
                table: "ExposureEvents",
                column: "IsReportingExposure");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_Latitude_Longitude",
                table: "ExposureEvents",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_PostalCode",
                table: "ExposureEvents",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_State",
                table: "ExposureEvents",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExposureEvents_City",
                table: "ExposureEvents");

            migrationBuilder.DropIndex(
                name: "IX_ExposureEvents_IsReportingExposure",
                table: "ExposureEvents");

            migrationBuilder.DropIndex(
                name: "IX_ExposureEvents_Latitude_Longitude",
                table: "ExposureEvents");

            migrationBuilder.DropIndex(
                name: "IX_ExposureEvents_PostalCode",
                table: "ExposureEvents");

            migrationBuilder.DropIndex(
                name: "IX_ExposureEvents_State",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "City",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "GeocodedDate",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "GeocodingAccuracy",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "IsReportingExposure",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "State",
                table: "ExposureEvents");
        }
    }
}
