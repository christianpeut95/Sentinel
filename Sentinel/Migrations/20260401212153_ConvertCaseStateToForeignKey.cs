using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCaseStateToForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new CaseStateId column
            migrationBuilder.AddColumn<int>(
                name: "CaseStateId",
                table: "Cases",
                type: "INTEGER",
                nullable: true);

            // Migrate existing data: map state strings to State IDs
            migrationBuilder.Sql(@"
                UPDATE Cases 
                SET CaseStateId = 
                    CASE 
                        WHEN UPPER(TRIM(CaseState)) IN ('NSW', 'NEW SOUTH WALES') THEN 1
                        WHEN UPPER(TRIM(CaseState)) IN ('VIC', 'VICTORIA') THEN 2
                        WHEN UPPER(TRIM(CaseState)) IN ('QLD', 'QUEENSLAND') THEN 3
                        WHEN UPPER(TRIM(CaseState)) IN ('SA', 'SOUTH AUSTRALIA') THEN 4
                        WHEN UPPER(TRIM(CaseState)) IN ('WA', 'WESTERN AUSTRALIA') THEN 5
                        WHEN UPPER(TRIM(CaseState)) IN ('TAS', 'TASMANIA') THEN 6
                        WHEN UPPER(TRIM(CaseState)) IN ('NT', 'NORTHERN TERRITORY') THEN 7
                        WHEN UPPER(TRIM(CaseState)) IN ('ACT', 'AUSTRALIAN CAPITAL TERRITORY') THEN 8
                        ELSE NULL
                    END
                WHERE CaseState IS NOT NULL AND CaseState <> '';
            ");

            // Create foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseStateId",
                table: "Cases",
                column: "CaseStateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_States_CaseStateId",
                table: "Cases",
                column: "CaseStateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Drop old CaseState string column
            migrationBuilder.DropColumn(
                name: "CaseState",
                table: "Cases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add CaseState string column back
            migrationBuilder.AddColumn<string>(
                name: "CaseState",
                table: "Cases",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            // Migrate CaseStateId back to CaseState strings (using Code)
            migrationBuilder.Sql(@"
                UPDATE Cases 
                SET CaseState = (SELECT Code FROM States WHERE States.Id = Cases.CaseStateId)
                WHERE CaseStateId IS NOT NULL;
            ");

            // Drop foreign key and CaseStateId column
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_States_CaseStateId",
                table: "Cases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_CaseStateId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "CaseStateId",
                table: "Cases");
        }
    }
}
