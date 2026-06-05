using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddPathogensAndLabResultMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSterileSite",
                table: "SpecimenTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Pathogens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LOINCCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LOINCDisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ResultType = table.Column<int>(type: "int", nullable: false),
                    DefaultUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultReferenceRangeLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DefaultReferenceRangeHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pathogens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pathogens_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestMethods",
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
                    table.PrimaryKey("PK_TestMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabResultMarkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PathogenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestMethodId = table.Column<int>(type: "int", nullable: true),
                    QualitativeResult = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    QuantitativeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    QuantitativeUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferenceRangeLow = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ReferenceRangeHigh = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    InterpretationFlag = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LOINCCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultMarkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResultMarkers_LabResults_LabResultId",
                        column: x => x.LabResultId,
                        principalTable: "LabResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabResultMarkers_Pathogens_PathogenId",
                        column: x => x.PathogenId,
                        principalTable: "Pathogens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResultMarkers_TestMethods_TestMethodId",
                        column: x => x.TestMethodId,
                        principalTable: "TestMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkers_LabResultId",
                table: "LabResultMarkers",
                column: "LabResultId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkers_LOINCCode",
                table: "LabResultMarkers",
                column: "LOINCCode");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkers_PathogenId",
                table: "LabResultMarkers",
                column: "PathogenId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkers_TestMethodId",
                table: "LabResultMarkers",
                column: "TestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Pathogens_DiseaseId_DisplayOrder",
                table: "Pathogens",
                columns: new[] { "DiseaseId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Pathogens_IsActive",
                table: "Pathogens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Pathogens_LOINCCode",
                table: "Pathogens",
                column: "LOINCCode",
                unique: true,
                filter: "[LOINCCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TestMethods_IsActive_DisplayOrder",
                table: "TestMethods",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TestMethods_Name",
                table: "TestMethods",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabResultMarkers");

            migrationBuilder.DropTable(
                name: "Pathogens");

            migrationBuilder.DropTable(
                name: "TestMethods");

            migrationBuilder.DropColumn(
                name: "IsSterileSite",
                table: "SpecimenTypes");
        }
    }
}
