using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Add_HL7_Configuration_MVP_Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiseaseId",
                table: "HL7FieldMappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTestMode",
                table: "HL7Configurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TestModeDescription",
                table: "HL7Configurations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HL7ConfigurationDiseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7ConfigurationDiseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7ConfigurationDiseases_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HL7ConfigurationDiseases_HL7Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "HL7Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HL7FieldMappings_DiseaseId",
                table: "HL7FieldMappings",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ConfigurationDiseases_ConfigurationId_DiseaseId",
                table: "HL7ConfigurationDiseases",
                columns: new[] { "ConfigurationId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HL7ConfigurationDiseases_DiseaseId",
                table: "HL7ConfigurationDiseases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ConfigurationDiseases_IsDefault",
                table: "HL7ConfigurationDiseases",
                column: "IsDefault");

            migrationBuilder.AddForeignKey(
                name: "FK_HL7FieldMappings_Diseases_DiseaseId",
                table: "HL7FieldMappings",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HL7FieldMappings_Diseases_DiseaseId",
                table: "HL7FieldMappings");

            migrationBuilder.DropTable(
                name: "HL7ConfigurationDiseases");

            migrationBuilder.DropIndex(
                name: "IX_HL7FieldMappings_DiseaseId",
                table: "HL7FieldMappings");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "HL7FieldMappings");

            migrationBuilder.DropColumn(
                name: "IsTestMode",
                table: "HL7Configurations");

            migrationBuilder.DropColumn(
                name: "TestModeDescription",
                table: "HL7Configurations");
        }
    }
}
