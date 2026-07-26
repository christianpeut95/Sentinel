using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class SeedHealthcareProviderOrganizationType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add "Healthcare Provider" OrganizationType for ordering providers
            // This allows HL7 messages to automatically create/match ordering provider organizations
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Name = 'Healthcare Provider')
                BEGIN
                    INSERT INTO OrganizationTypes (Name, IsActive)
                    VALUES ('Healthcare Provider', 1);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove "Healthcare Provider" organization type
            // Note: This will fail if there are existing organizations with this type
            migrationBuilder.Sql(@"
                DELETE FROM OrganizationTypes WHERE Name = 'Healthcare Provider';
            ");
        }
    }
}
