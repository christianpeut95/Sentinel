using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveySystemSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SurveyTemplateId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "SurveyResponseId",
                table: "CaseTasks");

            migrationBuilder.AddColumn<string>(
                name: "SurveyDefinitionJson",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InputMappingJson",
                table: "DiseaseTaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputMappingJson",
                table: "DiseaseTaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurveyResponseJson",
                table: "CaseTasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SurveyDefinitionJson",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "InputMappingJson",
                table: "DiseaseTaskTemplates");

            migrationBuilder.DropColumn(
                name: "OutputMappingJson",
                table: "DiseaseTaskTemplates");

            migrationBuilder.DropColumn(
                name: "SurveyResponseJson",
                table: "CaseTasks");

            migrationBuilder.AddColumn<Guid>(
                name: "SurveyTemplateId",
                table: "TaskTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SurveyResponseId",
                table: "CaseTasks",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
