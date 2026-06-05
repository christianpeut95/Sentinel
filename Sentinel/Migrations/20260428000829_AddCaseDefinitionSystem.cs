using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseDefinitionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConfirmationStatusClassifiedBy",
                table: "Cases",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmationStatusClassifiedDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoClassified",
                table: "Cases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEvaluatedDate",
                table: "Cases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastEvaluatedDefinitionIds",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplyToChildDiseases = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmationStatusId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DateActiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateActiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AllowAutoClassification = table.Column<bool>(type: "bit", nullable: false),
                    CreateReviewQueueOnChange = table.Column<bool>(type: "bit", nullable: false),
                    CreateReviewQueueOnSuggestion = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDefinitions_CaseStatuses_ConfirmationStatusId",
                        column: x => x.ConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseDefinitions_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseClassificationHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromConfirmationStatusId = table.Column<int>(type: "int", nullable: true),
                    ToConfirmationStatusId = table.Column<int>(type: "int", nullable: false),
                    AppliedDefinitionId = table.Column<int>(type: "int", nullable: true),
                    ClassifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsAutoClassified = table.Column<bool>(type: "bit", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseClassificationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseClassificationHistory_CaseDefinitions_AppliedDefinitionId",
                        column: x => x.AppliedDefinitionId,
                        principalTable: "CaseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseClassificationHistory_CaseStatuses_FromConfirmationStatusId",
                        column: x => x.FromConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseClassificationHistory_CaseStatuses_ToConfirmationStatusId",
                        column: x => x.ToConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseClassificationHistory_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseDefinitionCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseDefinitionId = table.Column<int>(type: "int", nullable: false),
                    ParentCriteriaId = table.Column<int>(type: "int", nullable: true),
                    CriterionType = table.Column<int>(type: "int", nullable: false),
                    LogicalOperator = table.Column<int>(type: "int", nullable: false),
                    GroupNumber = table.Column<int>(type: "int", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Operator = table.Column<int>(type: "int", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseDefinitionCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseDefinitionCriteria_CaseDefinitionCriteria_ParentCriteriaId",
                        column: x => x.ParentCriteriaId,
                        principalTable: "CaseDefinitionCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseDefinitionCriteria_CaseDefinitions_CaseDefinitionId",
                        column: x => x.CaseDefinitionId,
                        principalTable: "CaseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_AppliedDefinitionId",
                table: "CaseClassificationHistory",
                column: "AppliedDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_CaseId",
                table: "CaseClassificationHistory",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_CaseId_IsCurrent",
                table: "CaseClassificationHistory",
                columns: new[] { "CaseId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_ClassifiedDate",
                table: "CaseClassificationHistory",
                column: "ClassifiedDate");

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_FromConfirmationStatusId",
                table: "CaseClassificationHistory",
                column: "FromConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_IsAutoClassified",
                table: "CaseClassificationHistory",
                column: "IsAutoClassified");

            migrationBuilder.CreateIndex(
                name: "IX_CaseClassificationHistory_ToConfirmationStatusId",
                table: "CaseClassificationHistory",
                column: "ToConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CaseDefinitionId",
                table: "CaseDefinitionCriteria",
                column: "CaseDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_CaseDefinitionId_GroupNumber",
                table: "CaseDefinitionCriteria",
                columns: new[] { "CaseDefinitionId", "GroupNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitionCriteria_ParentCriteriaId",
                table: "CaseDefinitionCriteria",
                column: "ParentCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_ConfirmationStatusId",
                table: "CaseDefinitions",
                column: "ConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_DateActiveFrom",
                table: "CaseDefinitions",
                column: "DateActiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_DateActiveTo",
                table: "CaseDefinitions",
                column: "DateActiveTo");

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_DiseaseId_ConfirmationStatusId",
                table: "CaseDefinitions",
                columns: new[] { "DiseaseId", "ConfirmationStatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseDefinitions_Status",
                table: "CaseDefinitions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseClassificationHistory");

            migrationBuilder.DropTable(
                name: "CaseDefinitionCriteria");

            migrationBuilder.DropTable(
                name: "CaseDefinitions");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatusClassifiedBy",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ConfirmationStatusClassifiedDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "IsAutoClassified",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedDate",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "LastEvaluatedDefinitionIds",
                table: "Cases");
        }
    }
}
