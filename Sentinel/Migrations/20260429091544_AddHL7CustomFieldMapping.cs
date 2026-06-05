using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddHL7CustomFieldMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HL7CustomFieldMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HL7TestCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TestCodeDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    ExtractQualitativeResult = table.Column<bool>(type: "bit", nullable: false),
                    ExtractQuantitativeResult = table.Column<bool>(type: "bit", nullable: false),
                    ValueTransformation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7CustomFieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7CustomFieldMappings_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HL7CustomFieldMappings_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HL7CustomFieldMappings_CustomFieldDefinitionId",
                table: "HL7CustomFieldMappings",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7CustomFieldMappings_DiseaseId",
                table: "HL7CustomFieldMappings",
                column: "DiseaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HL7CustomFieldMappings");
        }
    }
}
