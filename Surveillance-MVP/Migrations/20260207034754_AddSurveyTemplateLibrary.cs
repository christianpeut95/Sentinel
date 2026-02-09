using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyTemplateLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SurveyTemplateId",
                table: "TaskTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SurveyTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SurveyDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultInputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultOutputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyTemplateDiseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTemplateDiseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyTemplateDiseases_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyTemplateDiseases_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_SurveyTemplateId",
                table: "TaskTemplates",
                column: "SurveyTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplateDiseases_DiseaseId",
                table: "SurveyTemplateDiseases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplateDiseases_SurveyTemplateId_DiseaseId",
                table: "SurveyTemplateDiseases",
                columns: new[] { "SurveyTemplateId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_Category",
                table: "SurveyTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_IsActive",
                table: "SurveyTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_Name",
                table: "SurveyTemplates",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyTemplateId",
                table: "TaskTemplates",
                column: "SurveyTemplateId",
                principalTable: "SurveyTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyTemplateId",
                table: "TaskTemplates");

            migrationBuilder.DropTable(
                name: "SurveyTemplateDiseases");

            migrationBuilder.DropTable(
                name: "SurveyTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_SurveyTemplateId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "SurveyTemplateId",
                table: "TaskTemplates");
        }
    }
}
