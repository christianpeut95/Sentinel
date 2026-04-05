using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddressReviewWindowAfterDays",
                table: "Diseases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddressReviewWindowBeforeDays",
                table: "Diseases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckJurisdictionCrossing",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JurisdictionFieldsToCheck",
                table: "Diseases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CaseAddressCapturedAt",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseAddressLine",
                table: "Cases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CaseAddressManualOverride",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CaseCity",
                table: "Cases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CaseLatitude",
                table: "Cases",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CaseLongitude",
                table: "Cases",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CasePostalCode",
                table: "Cases",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseState",
                table: "Cases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressReviewWindowAfterDays",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "AddressReviewWindowBeforeDays",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "CheckJurisdictionCrossing",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "JurisdictionFieldsToCheck",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "CaseAddressCapturedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseAddressLine",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseAddressManualOverride",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseCity",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseLatitude",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseLongitude",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CasePostalCode",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseState",
                table: "Cases");
        }
    }
}
