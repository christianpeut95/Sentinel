using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sentinel.Data;

#nullable disable

namespace Sentinel.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260729000000_AddModifiedByToReportDefinition")]
    public partial class AddModifiedByToReportDefinition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedByUserId",
                table: "ReportDefinitions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "ReportDefinitions");
        }
    }
}
