using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyTestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestResults_TestResultId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestTypes_TestTypeId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_TestTypes_TestTypeId",
                table: "TestResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestTypes",
                table: "TestTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults");

            migrationBuilder.RenameTable(
                name: "TestTypes",
                newName: "TestType");

            migrationBuilder.RenameTable(
                name: "TestResults",
                newName: "TestResult");

            migrationBuilder.RenameIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResult",
                newName: "IX_TestResult_TestTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestType",
                table: "TestType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestResult",
                table: "TestResult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestResult_TestResultId",
                table: "LabResults",
                column: "TestResultId",
                principalTable: "TestResult",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResult_TestType_TestTypeId",
                table: "TestResult",
                column: "TestTypeId",
                principalTable: "TestType",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestResult_TestResultId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_TestType_TestTypeId",
                table: "LabResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TestResult_TestType_TestTypeId",
                table: "TestResult");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestType",
                table: "TestType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestResult",
                table: "TestResult");

            migrationBuilder.RenameTable(
                name: "TestType",
                newName: "TestTypes");

            migrationBuilder.RenameTable(
                name: "TestResult",
                newName: "TestResults");

            migrationBuilder.RenameIndex(
                name: "IX_TestResult_TestTypeId",
                table: "TestResults",
                newName: "IX_TestResults_TestTypeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestTypes",
                table: "TestTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestResults",
                table: "TestResults",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestResults_TestResultId",
                table: "LabResults",
                column: "TestResultId",
                principalTable: "TestResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_TestTypes_TestTypeId",
                table: "LabResults",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_TestTypes_TestTypeId",
                table: "TestResults",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id");
        }
    }
}
