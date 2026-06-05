using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class MergeCaseDefinitionLabCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseDefinitionLabCriteria");

            migrationBuilder.AlterColumn<string>(
                name: "ValueJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "Operator",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FieldPath",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayText",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<string>(
                name: "AcceptablePathogensJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptableResultsJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptableSpecimenTypesJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcceptableTestMethodsJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BiomarkerStoragePreference",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalPathogenId",
                table: "CaseDefinitionCriteria",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanonicalTestMethodId",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CanonicalTestResultId",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CaseDefinitionCriteria",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "CaseDefinitionCriteria",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequireAllElementsMatch",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultStoragePreference",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecimenStoragePreference",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TestMethodStoragePreference",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalPathogenId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalPathogenId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalSpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalTestMethodId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalTestMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalTestResultId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalTestResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseDefinitionCriteria_Pathogens_CanonicalPathogenId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalPathogenId",
                principalTable: "Pathogens",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseDefinitionCriteria_SpecimenTypes_CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalSpecimenTypeId",
                principalTable: "SpecimenTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseDefinitionCriteria_TestMethods_CanonicalTestMethodId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalTestMethodId",
                principalTable: "TestMethods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseDefinitionCriteria_TestResults_CanonicalTestResultId",
                table: "CaseDefinitionCriteria",
                column: "CanonicalTestResultId",
                principalTable: "TestResults",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseDefinitionCriteria_Pathogens_CanonicalPathogenId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseDefinitionCriteria_SpecimenTypes_CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseDefinitionCriteria_TestMethods_CanonicalTestMethodId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseDefinitionCriteria_TestResults_CanonicalTestResultId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalPathogenId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalTestMethodId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropIndex(
                name: "IX_CaseDefinitionCriteria_CanonicalTestResultId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "AcceptablePathogensJson",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "AcceptableResultsJson",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "AcceptableSpecimenTypesJson",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "AcceptableTestMethodsJson",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "BiomarkerStoragePreference",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "CanonicalPathogenId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "CanonicalSpecimenTypeId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "CanonicalTestMethodId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "CanonicalTestResultId",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "RequireAllElementsMatch",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "ResultStoragePreference",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "SpecimenStoragePreference",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "TestMethodStoragePreference",
                table: "CaseDefinitionCriteria");

            migrationBuilder.AlterColumn<string>(
                name: "ValueJson",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Operator",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FieldPath",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayText",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CaseDefinitionLabCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalPathogenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CanonicalSpecimenTypeId = table.Column<int>(type: "int", nullable: true),
                    CanonicalTestMethodId = table.Column<int>(type: "int", nullable: true),
                    CanonicalTestResultId = table.Column<int>(type: "int", nullable: true),
                    CaseDefinitionId = table.Column<int>(type: "int", nullable: false),
                    AcceptablePathogensJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptableResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptableSpecimenTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptableTestMethodsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BiomarkerStoragePreference = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    GroupNumber = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    LogicalOperator = table.Column<int>(type: "int", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequireAllElementsMatch = table.Column<bool>(type: "bit", nullable: false),
                    ResultStoragePreference = table.Column<int>(type: "int", nullable: false),
                    SpecimenStoragePreference = table.Column<int>(type: "int", nullable: false),
                    TestMethodStoragePreference = table.Column<int>(type: "int", nullable: false)
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
    }
}
