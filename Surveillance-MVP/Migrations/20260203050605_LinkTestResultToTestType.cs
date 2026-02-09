using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class LinkTestResultToTestType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestTypeId",
                table: "TestResults",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResults",
                column: "TestTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestResults_TestTypes_TestTypeId",
                table: "TestResults",
                column: "TestTypeId",
                principalTable: "TestTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestResults_TestTypes_TestTypeId",
                table: "TestResults");

            migrationBuilder.DropIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "TestTypeId",
                table: "TestResults");
        }
    }
}
