using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderMatchingPolicyAndIdentityUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_ProviderIdentifier",
                table: "Organizations");

            migrationBuilder.AddColumn<int>(
                name: "ProviderMatchingMode",
                table: "HL7Configurations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.Sql(
                """
                UPDATE [Organizations]
                SET [ProviderIdentifierAuthority] = NULLIF(
                    UPPER(LTRIM(RTRIM([ProviderIdentifierAuthority]))),
                    '')
                WHERE [ProviderIdentifierAuthority] IS NOT NULL;
                """);

            // Never silently merge or delete identity records during deployment.
            // An administrator must review any existing collision before the
            // authority-scoped uniqueness guarantee can be installed.
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT [ProviderIdentifier], [ProviderIdentifierAuthority]
                    FROM [Organizations]
                    WHERE [ProviderIdentifier] IS NOT NULL
                    GROUP BY [ProviderIdentifier], [ProviderIdentifierAuthority]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    THROW 51000, 'Duplicate provider identifiers exist for the same authority. Resolve them before applying this migration.', 1;
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_Organizations_ProviderIdentity",
                table: "Organizations",
                columns: new[] { "ProviderIdentifier", "ProviderIdentifierAuthority" },
                unique: true,
                filter: "[ProviderIdentifier] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Organizations_ProviderIdentity",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ProviderMatchingMode",
                table: "HL7Configurations");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ProviderIdentifier",
                table: "Organizations",
                column: "ProviderIdentifier");
        }
    }
}
