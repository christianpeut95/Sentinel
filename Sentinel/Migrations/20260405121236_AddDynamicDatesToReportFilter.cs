using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicDatesToReportFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DynamicDateOffset",
                table: "ReportFilters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DynamicDateOffsetUnit",
                table: "ReportFilters",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DynamicDateType",
                table: "ReportFilters",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDynamicDate",
                table: "ReportFilters",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DynamicDateOffset",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "DynamicDateOffsetUnit",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "DynamicDateType",
                table: "ReportFilters");

            migrationBuilder.DropColumn(
                name: "IsDynamicDate",
                table: "ReportFilters");
        }
    }
}
