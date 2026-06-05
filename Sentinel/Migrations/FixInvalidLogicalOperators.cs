using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <summary>
    /// Fixes invalid LogicalOperator values (0) in CaseDefinitionCriteria
    /// and adds a check constraint to prevent future invalid values
    /// </summary>
    public partial class FixInvalidLogicalOperators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update any criteria with invalid LogicalOperator = 0 to AND (1)
            migrationBuilder.Sql(@"
                UPDATE CaseDefinitionCriteria 
                SET LogicalOperator = 1 
                WHERE LogicalOperator = 0 OR LogicalOperator IS NULL;
            ");

            // Add check constraint to ensure LogicalOperator is always valid (1=AND, 2=OR, 3=NOT)
            migrationBuilder.Sql(@"
                ALTER TABLE CaseDefinitionCriteria 
                ADD CONSTRAINT CK_CaseDefinitionCriteria_LogicalOperator 
                CHECK (LogicalOperator IN (1, 2, 3));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the check constraint
            migrationBuilder.Sql(@"
                ALTER TABLE CaseDefinitionCriteria 
                DROP CONSTRAINT IF EXISTS CK_CaseDefinitionCriteria_LogicalOperator;
            ");

            // Note: We don't revert the data changes as there's no way to know what the original invalid values were
        }
    }
}
