using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewWorkerSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignmentMethod",
                table: "CaseTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "AutoAssignedAt",
                table: "CaseTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentAttemptCount",
                table: "CaseTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EscalationLevel",
                table: "CaseTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterviewTask",
                table: "CaseTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanguageRequired",
                table: "CaseTasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCallAttempt",
                table: "CaseTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCallAttempts",
                table: "CaseTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AvailableForAutoAssignment",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CurrentTaskCapacity",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterviewWorker",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LanguagesSpokenJson",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryLanguage",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskCallAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    NextCallbackScheduled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumberCalled = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCallAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCallAttempts_AspNetUsers_AttemptedByUserId",
                        column: x => x.AttemptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCallAttempts_CaseTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCallAttempts_AttemptedByUserId",
                table: "TaskCallAttempts",
                column: "AttemptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCallAttempts_TaskId",
                table: "TaskCallAttempts",
                column: "TaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskCallAttempts");

            migrationBuilder.DropColumn(
                name: "AssignmentMethod",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "AutoAssignedAt",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "CurrentAttemptCount",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "EscalationLevel",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "IsInterviewTask",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "LanguageRequired",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "LastCallAttempt",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "MaxCallAttempts",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "AvailableForAutoAssignment",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentTaskCapacity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsInterviewWorker",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LanguagesSpokenJson",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PrimaryLanguage",
                table: "AspNetUsers");
        }
    }
}
