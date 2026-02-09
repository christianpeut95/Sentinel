using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentSurveyTemplateId",
                table: "SurveyTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "SurveyTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublishedBy",
                table: "SurveyTemplates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionNotes",
                table: "SurveyTemplates",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VersionNumber",
                table: "SurveyTemplates",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VersionStatus",
                table: "SurveyTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_ParentSurveyTemplateId",
                table: "SurveyTemplates",
                column: "ParentSurveyTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyTemplates_SurveyTemplates_ParentSurveyTemplateId",
                table: "SurveyTemplates",
                column: "ParentSurveyTemplateId",
                principalTable: "SurveyTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyTemplates_SurveyTemplates_ParentSurveyTemplateId",
                table: "SurveyTemplates");

            migrationBuilder.DropIndex(
                name: "IX_SurveyTemplates_ParentSurveyTemplateId",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "ParentSurveyTemplateId",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "VersionNotes",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "SurveyTemplates");

            migrationBuilder.DropColumn(
                name: "VersionStatus",
                table: "SurveyTemplates");
        }
    }
}
