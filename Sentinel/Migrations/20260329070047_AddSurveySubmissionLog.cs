using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveySubmissionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurveySubmissionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CaseReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiseaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SurveyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SubmittedByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    FieldsSavedAutomatically = table.Column<int>(type: "int", nullable: false),
                    FieldsSentForReview = table.Column<int>(type: "int", nullable: false),
                    FieldsRequiringApproval = table.Column<int>(type: "int", nullable: false),
                    FieldsSkipped = table.Column<int>(type: "int", nullable: false),
                    FieldsWithErrors = table.Column<int>(type: "int", nullable: false),
                    TotalMappingsConfigured = table.Column<int>(type: "int", nullable: false),
                    IssuesSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MappingDetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveySubmissionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveySubmissionLogs_CaseTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SurveySubmissionLogs_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissionLogs_CaseId",
                table: "SurveySubmissionLogs",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissionLogs_TaskId",
                table: "SurveySubmissionLogs",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurveySubmissionLogs");
        }
    }
}
