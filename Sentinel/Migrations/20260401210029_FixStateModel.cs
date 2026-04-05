using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class FixStateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CaseState",
                table: "Cases");

            migrationBuilder.AddColumn<int>(
                name: "StateId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaseStateId",
                table: "Cases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "States",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_States", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_StateId",
                table: "Patients",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseStateId",
                table: "Cases",
                column: "CaseStateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_States_CaseStateId",
                table: "Cases",
                column: "CaseStateId",
                principalTable: "States",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_States_StateId",
                table: "Patients",
                column: "StateId",
                principalTable: "States",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_States_CaseStateId",
                table: "Cases");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_States_StateId",
                table: "Patients");

            migrationBuilder.DropTable(
                name: "States");

            migrationBuilder.DropIndex(
                name: "IX_Patients_StateId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_Cases_CaseStateId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "StateId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CaseStateId",
                table: "Cases");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaseState",
                table: "Cases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
