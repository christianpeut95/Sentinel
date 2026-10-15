using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredProviderIdentityToOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Credentials",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyName",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GivenName",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NamePrefix",
                table: "Organizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameSuffix",
                table: "Organizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderIdentifier",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderIdentifierAuthority",
                table: "Organizations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_FamilyName_GivenName",
                table: "Organizations",
                columns: new[] { "FamilyName", "GivenName" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ProviderIdentifier",
                table: "Organizations",
                column: "ProviderIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_FamilyName_GivenName",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_ProviderIdentifier",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Credentials",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "FamilyName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "GivenName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "MiddleName",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "NamePrefix",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "NameSuffix",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ProviderIdentifier",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ProviderIdentifierAuthority",
                table: "Organizations");
        }
    }
}
