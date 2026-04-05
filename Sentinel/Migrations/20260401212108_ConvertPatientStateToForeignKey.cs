using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class ConvertPatientStateToForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new StateId column
            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "Patients",
                type: "INTEGER",
                nullable: true);

            // Migrate existing data: map state strings to State IDs
            // NSW -> 1, VIC -> 2, QLD -> 3, SA -> 4, WA -> 5, TAS -> 6, NT -> 7, ACT -> 8
            migrationBuilder.Sql(@"
                UPDATE Patients 
                SET StateId = 
                    CASE 
                        WHEN UPPER(TRIM(State)) IN ('NSW', 'NEW SOUTH WALES') THEN 1
                        WHEN UPPER(TRIM(State)) IN ('VIC', 'VICTORIA') THEN 2
                        WHEN UPPER(TRIM(State)) IN ('QLD', 'QUEENSLAND') THEN 3
                        WHEN UPPER(TRIM(State)) IN ('SA', 'SOUTH AUSTRALIA') THEN 4
                        WHEN UPPER(TRIM(State)) IN ('WA', 'WESTERN AUSTRALIA') THEN 5
                        WHEN UPPER(TRIM(State)) IN ('TAS', 'TASMANIA') THEN 6
                        WHEN UPPER(TRIM(State)) IN ('NT', 'NORTHERN TERRITORY') THEN 7
                        WHEN UPPER(TRIM(State)) IN ('ACT', 'AUSTRALIAN CAPITAL TERRITORY') THEN 8
                        ELSE NULL
                    END
                WHERE State IS NOT NULL AND State <> '';
            ");

            // Create foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_Patients_StateId",
                table: "Patients",
                column: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_States_StateId",
                table: "Patients",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Drop old State string column
            migrationBuilder.DropColumn(
                name: "State",
                table: "Patients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add State string column back
            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Patients",
                type: "TEXT",
                nullable: true);

            // Migrate StateId back to State strings (using Code for brevity)
            migrationBuilder.Sql(@"
                UPDATE Patients 
                SET State = (SELECT Code FROM States WHERE States.Id = Patients.StateId)
                WHERE StateId IS NOT NULL;
            ");

            // Drop foreign key and StateId column
            migrationBuilder.DropForeignKey(
                name: "FK_Patients_States_StateId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_StateId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Patients");
        }
    }
}
