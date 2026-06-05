using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoEvaluationAndManualOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfirmationStatusManualOverride",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationStatusManualOverrideByUserId",
                table: "Cases",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationStatusManualOverrideDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableAutoEvaluation",
                table: "CaseDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmationStatusManualOverride",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatusManualOverrideByUserId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatusManualOverrideDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EnableAutoEvaluation",
                table: "CaseDefinitions");
        }
    }
}
