using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class DropTestTypesAndResultsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestResult_TestResultId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResult_TestType_TestTypeId",
                table: "TestResult");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_LabResults_TestResultId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_TestTypeId",
                table: "LabResults");

            // Drop columns from LabResults
            migrationBuilder.DropColumn(
                name: "TestResultId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "TestTypeId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "QuantitativeResult",
                table: "LabResults");

            // Drop the legacy tables
            migrationBuilder.DropTable(
                name: "TestResult");

            migrationBuilder.DropTable(
                name: "TestType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate TestType table
            migrationBuilder.CreateTable(
                name: "TestType",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestType", x => x.Id);
                });

            // Recreate TestResult table
            migrationBuilder.CreateTable(
                name: "TestResult",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 100, nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true),
                    TestTypeId = table.Column<int>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResult_TestType_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestType",
                        principalColumn: "Id");
                });

            // Add columns back to LabResults
            migrationBuilder.AddColumn<int>(
                name: "TestTypeId",
                table: "LabResults",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestResultId",
                table: "LabResults",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantitativeResult",
                table: "LabResults",
                type: "decimal(18,2)",
                nullable: true);

            // Recreate indexes
            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestTypeId",
                table: "LabResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestResultId",
                table: "LabResults",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResult_TestTypeId",
                table: "TestResult",
                column: "TestTypeId");

            // Recreate foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestResult_TestResultId",
                table: "LabResults",
                column: "TestResultId",
                principalTable: "TestResult",
                principalColumn: "Id");
        }
    }
}
