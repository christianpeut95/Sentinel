using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCustomFieldsForPatientAndCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowOnPatientList",
                table: "CustomFieldDefinitions",
                newName: "ShowOnPatientForm");

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnCaseForm",
                table: "CustomFieldDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnList",
                table: "CustomFieldDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldBooleans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldBooleans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldBooleans_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldBooleans_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldDates_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldDates_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    LookupValueId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldLookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_LookupValues_LookupValueId",
                        column: x => x.LookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldNumbers_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldNumbers_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldStrings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldStrings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldStrings_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiseaseCustomFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    InheritToChildDiseases = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseCustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseCustomFields_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiseaseCustomFields_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldBooleans_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldBooleans",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldBooleans_FieldDefinitionId",
                table: "CaseCustomFieldBooleans",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldDates",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_FieldDefinitionId",
                table: "CaseCustomFieldDates",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_Value",
                table: "CaseCustomFieldDates",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldLookups",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_FieldDefinitionId",
                table: "CaseCustomFieldLookups",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_LookupValueId",
                table: "CaseCustomFieldLookups",
                column: "LookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldNumbers",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_FieldDefinitionId",
                table: "CaseCustomFieldNumbers",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_Value",
                table: "CaseCustomFieldNumbers",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldStrings",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_FieldDefinitionId",
                table: "CaseCustomFieldStrings",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_Value",
                table: "CaseCustomFieldStrings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCustomFields_CustomFieldDefinitionId",
                table: "DiseaseCustomFields",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCustomFields_DiseaseId_CustomFieldDefinitionId",
                table: "DiseaseCustomFields",
                columns: new[] { "DiseaseId", "CustomFieldDefinitionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseCustomFieldBooleans");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldDates");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldLookups");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldNumbers");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldStrings");

            migrationBuilder.DropTable(
                name: "DiseaseCustomFields");

            migrationBuilder.DropColumn(
                name: "ShowOnCaseForm",
                table: "CustomFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "ShowOnList",
                table: "CustomFieldDefinitions");

            migrationBuilder.RenameColumn(
                name: "ShowOnPatientForm",
                table: "CustomFieldDefinitions",
                newName: "ShowOnPatientList");
        }
    }
}
