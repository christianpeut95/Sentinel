using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class SurveyFamilyReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing FK and index on old column name
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyFamilyRootId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_SurveyFamilyRootId",
                table: "TaskTemplates");

            // Rename column
            migrationBuilder.RenameColumn(
                name: "SurveyFamilyRootId",
                table: "TaskTemplates",
                newName: "SurveyTemplateId");

            // Add missing columns
            migrationBuilder.AddColumn<string>(
                name: "SurveyDefinitionJson",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultInputMappingJson",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOutputMappingJson",
                table: "TaskTemplates",
                type: "nvarchar(max)",
                nullable: true);

            // Recreate index and FK with correct names
            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_SurveyTemplateId",
                table: "TaskTemplates",
                column: "SurveyTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyTemplateId",
                table: "TaskTemplates",
                column: "SurveyTemplateId",
                principalTable: "SurveyTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyTemplateId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_SurveyTemplateId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "SurveyDefinitionJson",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultInputMappingJson",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultOutputMappingJson",
                table: "TaskTemplates");

            migrationBuilder.RenameColumn(
                name: "SurveyTemplateId",
                table: "TaskTemplates",
                newName: "SurveyFamilyRootId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_SurveyFamilyRootId",
                table: "TaskTemplates",
                column: "SurveyFamilyRootId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_SurveyTemplates_SurveyFamilyRootId",
                table: "TaskTemplates",
                column: "SurveyFamilyRootId",
                principalTable: "SurveyTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
