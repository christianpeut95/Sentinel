using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackWidgetAndInstallationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableFeedbackWidget",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "InstallationId",
                table: "SystemSettings",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            // Generate InstallationId for existing row if it exists
            migrationBuilder.Sql(@"
                UPDATE SystemSettings 
                SET InstallationId = LOWER(NEWID()) 
                WHERE InstallationId IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableFeedbackWidget",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "SystemSettings");
        }
    }
}
