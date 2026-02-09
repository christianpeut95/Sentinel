using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskTypeAndDiseaseToTaskTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_Category",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CaseTasks");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskTypeId",
                table: "TaskTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TaskTypeId",
                table: "CaseTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TaskTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColorClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_TaskTypeId",
                table: "TaskTemplates",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_TaskTypeId",
                table: "CaseTasks",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTypes_IsActive",
                table: "TaskTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTypes_Name",
                table: "TaskTypes",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseTasks_TaskTypes_TaskTypeId",
                table: "CaseTasks",
                column: "TaskTypeId",
                principalTable: "TaskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_TaskTypes_TaskTypeId",
                table: "TaskTemplates",
                column: "TaskTypeId",
                principalTable: "TaskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseTasks_TaskTypes_TaskTypeId",
                table: "CaseTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_TaskTypes_TaskTypeId",
                table: "TaskTemplates");

            migrationBuilder.DropTable(
                name: "TaskTypes");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_TaskTypeId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CaseTasks_TaskTypeId",
                table: "CaseTasks");

            migrationBuilder.DropColumn(
                name: "TaskTypeId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "TaskTypeId",
                table: "CaseTasks");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "TaskTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "CaseTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_Category",
                table: "TaskTemplates",
                column: "Category");
        }
    }
}
