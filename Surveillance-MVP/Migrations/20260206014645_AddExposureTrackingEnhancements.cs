using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddExposureTrackingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterstateOriginState",
                table: "ExposureEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefaultedFromResidentialAddress",
                table: "ExposureEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterstateTravel",
                table: "ExposureEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowDomesticAcquisition",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AlwaysPromptForLocation",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultToResidentialAddress",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ExposureDataGracePeriodDays",
                table: "Diseases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExposureGuidanceText",
                table: "Diseases",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExposureTrackingMode",
                table: "Diseases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequireGeographicCoordinates",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredLocationTypeIds",
                table: "Diseases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SyncWithPatientAddressUpdates",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterstateOriginState",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "IsDefaultedFromResidentialAddress",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "IsInterstateTravel",
                table: "ExposureEvents");

            migrationBuilder.DropColumn(
                name: "AllowDomesticAcquisition",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "AlwaysPromptForLocation",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "DefaultToResidentialAddress",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "ExposureDataGracePeriodDays",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "ExposureGuidanceText",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "ExposureTrackingMode",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "RequireGeographicCoordinates",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "RequiredLocationTypeIds",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "SyncWithPatientAddressUpdates",
                table: "Diseases");
        }
    }
}
