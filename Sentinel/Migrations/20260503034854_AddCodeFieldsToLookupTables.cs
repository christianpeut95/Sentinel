using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeFieldsToLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CaseInsensitive",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnorePunctuation",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchingStrategy",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "NormalizeWhitespace",
                table: "CaseDefinitionCriteria",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedValue",
                table: "CaseDefinitionCriteria",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResultNormalizationMode",
                table: "CaseDefinitionCriteria",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseInsensitive",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "IgnorePunctuation",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "MatchingStrategy",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "NormalizeWhitespace",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "NormalizedValue",
                table: "CaseDefinitionCriteria");

            migrationBuilder.DropColumn(
                name: "ResultNormalizationMode",
                table: "CaseDefinitionCriteria");
        }
    }
}
