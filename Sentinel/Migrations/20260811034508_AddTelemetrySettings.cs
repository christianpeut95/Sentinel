using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetrySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeSystemInformation",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeUserInformation",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LocalLoggingEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LogRetentionDays",
                table: "SystemSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MinimumLogLevel",
                table: "SystemSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TelemetryEnabled",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeSystemInformation",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "IncludeUserInformation",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LocalLoggingEnabled",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "LogRetentionDays",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "MinimumLogLevel",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "TelemetryEnabled",
                table: "SystemSettings");
        }
    }
}
