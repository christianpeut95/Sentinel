using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseDefinitionEvaluationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseClassificationHistory_CaseDefinitions_AppliedDefinitionId",
                table: "CaseClassificationHistory");

            migrationBuilder.RenameColumn(
                name: "MetCriteriaJson",
                table: "CaseClassificationHistory",
                newName: "CriteriaResultJson");

            migrationBuilder.RenameColumn(
                name: "AppliedDefinitionId",
                table: "CaseClassificationHistory",
                newName: "CaseDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseClassificationHistory_AppliedDefinitionId",
                table: "CaseClassificationHistory",
                newName: "IX_CaseClassificationHistory_CaseDefinitionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClassifiedDate",
                table: "CaseClassificationHistory",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "EvaluationDate",
                table: "CaseClassificationHistory",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsMatch",
                table: "CaseClassificationHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecommendedAction",
                table: "CaseClassificationHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "WasApplied",
                table: "CaseClassificationHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseClassificationHistory_CaseDefinitions_CaseDefinitionId",
                table: "CaseClassificationHistory",
                column: "CaseDefinitionId",
                principalTable: "CaseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseClassificationHistory_CaseDefinitions_CaseDefinitionId",
                table: "CaseClassificationHistory");

            migrationBuilder.DropColumn(
                name: "EvaluationDate",
                table: "CaseClassificationHistory");

            migrationBuilder.DropColumn(
                name: "IsMatch",
                table: "CaseClassificationHistory");

            migrationBuilder.DropColumn(
                name: "RecommendedAction",
                table: "CaseClassificationHistory");

            migrationBuilder.DropColumn(
                name: "WasApplied",
                table: "CaseClassificationHistory");

            migrationBuilder.RenameColumn(
                name: "CriteriaResultJson",
                table: "CaseClassificationHistory",
                newName: "MetCriteriaJson");

            migrationBuilder.RenameColumn(
                name: "CaseDefinitionId",
                table: "CaseClassificationHistory",
                newName: "AppliedDefinitionId");

            migrationBuilder.RenameIndex(
                name: "IX_CaseClassificationHistory_CaseDefinitionId",
                table: "CaseClassificationHistory",
                newName: "IX_CaseClassificationHistory_AppliedDefinitionId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ClassifiedDate",
                table: "CaseClassificationHistory",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseClassificationHistory_CaseDefinitions_AppliedDefinitionId",
                table: "CaseClassificationHistory",
                column: "AppliedDefinitionId",
                principalTable: "CaseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
