using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddOutbreakTeamClassificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClassificationDate",
                table: "OutbreakCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassificationNotes",
                table: "OutbreakCases",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassifiedBy",
                table: "OutbreakCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefinitionName",
                table: "OutbreakCaseDefinitions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefinitionText",
                table: "OutbreakCaseDefinitions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassificationDate",
                table: "OutbreakCases");

            migrationBuilder.DropColumn(
                name: "ClassificationNotes",
                table: "OutbreakCases");

            migrationBuilder.DropColumn(
                name: "ClassifiedBy",
                table: "OutbreakCases");

            migrationBuilder.DropColumn(
                name: "DefinitionName",
                table: "OutbreakCaseDefinitions");

            migrationBuilder.DropColumn(
                name: "DefinitionText",
                table: "OutbreakCaseDefinitions");
        }
    }
}
