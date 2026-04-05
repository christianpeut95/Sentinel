using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewQueueLinkToSubmissionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewQueueItemId",
                table: "SurveySubmissionLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveySubmissionLogs_ReviewQueueItemId",
                table: "SurveySubmissionLogs",
                column: "ReviewQueueItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveySubmissionLogs_ReviewQueue_ReviewQueueItemId",
                table: "SurveySubmissionLogs",
                column: "ReviewQueueItemId",
                principalTable: "ReviewQueue",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveySubmissionLogs_ReviewQueue_ReviewQueueItemId",
                table: "SurveySubmissionLogs");

            migrationBuilder.DropIndex(
                name: "IX_SurveySubmissionLogs_ReviewQueueItemId",
                table: "SurveySubmissionLogs");

            migrationBuilder.DropColumn(
                name: "ReviewQueueItemId",
                table: "SurveySubmissionLogs");
        }
    }
}
