using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Add_SNOMED_To_TestResults_And_TestMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodingSystem",
                table: "TestMethods");

            migrationBuilder.DropColumn(
                name: "StandardCode",
                table: "TestMethods");

            migrationBuilder.DropColumn(
                name: "CaseInsensitive",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "IgnorePunctuation",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "MatchingStrategy",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "NormalizeWhitespace",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "NormalizedValue",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "ResultNormalizationMode",
                table: "CaseDefinitionCriteria");

            migrationBuilder.RenameColumn(
                name: "QualitativeResult",
                table: "LabResultMarkers",
                newName: "QualitativeResultText");

            migrationBuilder.AddColumn<string>(
                name: "LoincMethodCode",
                table: "TestMethods",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnomedCode",
                table: "TestMethods",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnomedDisplay",
                table: "TestMethods",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestResultId",
                table: "LabResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestTypeId",
                table: "LabResults",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestResultId",
                table: "LabResultMarkers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TestType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SnomedCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SnomedDisplay = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Hl7Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_TestType_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestResultId",
                table: "LabResults",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestTypeId",
                table: "LabResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkers_TestResultId",
                table: "LabResultMarkers",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResults",
                column: "TestTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResultMarkers_TestResults_TestResultId",
                table: "LabResultMarkers",
                column: "TestResultId",
                principalTable: "TestResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestResults_TestResultId",
                table: "LabResults",
                column: "TestResultId",
                principalTable: "TestResults",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResultMarkers_TestResults_TestResultId",
                table: "LabResultMarkers");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestResults_TestResultId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "TestType");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_TestResultId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_TestTypeId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_LabResultMarkers_TestResultId",
                table: "LabResultMarkers");

            migrationBuilder.DropColumn(
                name: "LoincMethodCode",
                table: "TestMethods");

            migrationBuilder.DropColumn(
                name: "SnomedCode",
                table: "TestMethods");

            migrationBuilder.DropColumn(
                name: "SnomedDisplay",
                table: "TestMethods");

            migrationBuilder.DropColumn(
                name: "TestResultId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "TestTypeId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "TestResultId",
                table: "LabResultMarkers");

            migrationBuilder.RenameColumn(
                name: "QualitativeResultText",
                table: "LabResultMarkers",
                newName: "QualitativeResult");

            migrationBuilder.AddColumn<string>(
                name: "CodingSystem",
                table: "TestMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StandardCode",
                table: "TestMethods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CaseInsensitive",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnorePunctuation",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchingStrategy",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NormalizeWhitespace",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedValue",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultNormalizationMode",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);
        }
    }
}
