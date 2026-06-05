using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseDefinitionLabCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CaseDefinitionLabCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseDefinitionId = table.Column<int>(type: "int", nullable: false),
                    AcceptableSpecimenTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SpecimenStoragePreference = table.Column<int>(type: "int", nullable: false),
                    CanonicalSpecimenTypeId = table.Column<int>(type: "int", nullable: true),
                    AcceptablePathogensJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BiomarkerStoragePreference = table.Column<int>(type: "int", nullable: false),
                    CanonicalPathogenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcceptableTestMethodsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestMethodStoragePreference = table.Column<int>(type: "int", nullable: false),
                    CanonicalTestMethodId = table.Column<int>(type: "int", nullable: true),
                    AcceptableResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultStoragePreference = table.Column<int>(type: "int", nullable: false),
                    CanonicalTestResultId = table.Column<int>(type: "int", nullable: true),
                    GroupNumber = table.Column<int>(type: "int", nullable: false),
                    LogicalOperator = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequireAllElementsMatch = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDefinitionLabCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDefinitionLabCriteria_CaseDefinitions_CaseDefinitionId",
                        column: x => x.CaseDefinitionId,
                        principalTable: "CaseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseDefinitionLabCriteria_Pathogens_CanonicalPathogenId",
                        column: x => x.CanonicalPathogenId,
                        principalTable: "Pathogens",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseDefinitionLabCriteria_SpecimenTypes_CanonicalSpecimenTypeId",
                        column: x => x.CanonicalSpecimenTypeId,
                        principalTable: "SpecimenTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseDefinitionLabCriteria_TestMethods_CanonicalTestMethodId",
                        column: x => x.CanonicalTestMethodId,
                        principalTable: "TestMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseDefinitionLabCriteria_TestResults_CanonicalTestResultId",
                        column: x => x.CanonicalTestResultId,
                        principalTable: "TestResults",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionLabCriteria_CanonicalPathogenId",
                table: "CaseDefinitionLabCriteria",
                column: "CanonicalPathogenId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionLabCriteria_CanonicalSpecimenTypeId",
                table: "CaseDefinitionLabCriteria",
                column: "CanonicalSpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionLabCriteria_CanonicalTestMethodId",
                table: "CaseDefinitionLabCriteria",
                column: "CanonicalTestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionLabCriteria_CanonicalTestResultId",
                table: "CaseDefinitionLabCriteria",
                column: "CanonicalTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionLabCriteria_CaseDefinitionId",
                table: "CaseDefinitionLabCriteria",
                column: "CaseDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseDefinitionLabCriteria");
        }
    }
}
