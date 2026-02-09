using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    DefaultPriority = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    ApplicableToType = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromOnset = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromNotification = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromContact = table.Column<int>(type: "int", nullable: true),
                    DueCalculationMethod = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "int", nullable: true),
                    RecurrenceCount = table.Column<int>(type: "int", nullable: true),
                    RecurrenceDurationDays = table.Column<int>(type: "int", nullable: true),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletionCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiresEvidence = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    InheritanceBehavior = table.Column<int>(type: "int", nullable: false),
                    RestrictToSubDiseaseIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceFileIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SurveyResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecurrenceSequence = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseTasks_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseTasks_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseTasks_CaseTasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseTasks_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseTasks_TaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiseaseTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsInherited = table.Column<bool>(type: "bit", nullable: false),
                    InheritedFromDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplyToChildren = table.Column<bool>(type: "bit", nullable: false),
                    AllowChildOverride = table.Column<bool>(type: "bit", nullable: false),
                    OverrideAutoCreate = table.Column<bool>(type: "bit", nullable: true),
                    OverridePriority = table.Column<int>(type: "int", nullable: true),
                    OverrideDueDays = table.Column<int>(type: "int", nullable: true),
                    OverrideInstructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AutoCreateOnCaseCreation = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateOnContactCreation = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateOnLabConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseTaskTemplates_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiseaseTaskTemplates_TaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_AssignedToUserId",
                table: "CaseTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_CaseId",
                table: "CaseTasks",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_CompletedByUserId",
                table: "CaseTasks",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_DueDate",
                table: "CaseTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_ParentTaskId",
                table: "CaseTasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_Priority",
                table: "CaseTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_Status",
                table: "CaseTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_TaskTemplateId",
                table: "CaseTasks",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_DiseaseId_TaskTemplateId",
                table: "DiseaseTaskTemplates",
                columns: new[] { "DiseaseId", "TaskTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_InheritedFromDiseaseId",
                table: "DiseaseTaskTemplates",
                column: "InheritedFromDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_IsInherited",
                table: "DiseaseTaskTemplates",
                column: "IsInherited");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_TaskTemplateId",
                table: "DiseaseTaskTemplates",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_Category",
                table: "TaskTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_IsActive",
                table: "TaskTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_Name",
                table: "TaskTemplates",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseTasks");

            migrationBuilder.DropTable(
                name: "DiseaseTaskTemplates");

            migrationBuilder.DropTable(
                name: "TaskTemplates");
        }
    }
}
