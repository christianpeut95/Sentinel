using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyReinfectionRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlwaysCreateNewCase",
                table: "DiseaseReinfectionRules");

            migrationBuilder.DropColumn(
                name: "CaseMatchingStrategy",
                table: "DiseaseReinfectionRules");

            migrationBuilder.DropColumn(
                name: "IsChronic",
                table: "DiseaseReinfectionRules");

            migrationBuilder.DropColumn(
                name: "MatchOnResultType",
                table: "DiseaseReinfectionRules");

            migrationBuilder.DropColumn(
                name: "MatchOnTestType",
                table: "DiseaseReinfectionRules");

            migrationBuilder.AddColumn<string>(
                name: "PartialMatchDetailsJson",
                table: "HL7Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMissingPathogen",
                table: "DiseaseHL7MatchingConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMissingResult",
                table: "DiseaseHL7MatchingConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMissingSpecimenType",
                table: "DiseaseHL7MatchingConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMissingTestMethod",
                table: "DiseaseHL7MatchingConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxMissingFieldsAllowed",
                table: "DiseaseHL7MatchingConfigs",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs",
                type: "int",
                nullable: true,
                defaultValue: null);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseHL7MatchingConfigs_PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs",
                column: "PartialMatchConfirmationStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_DiseaseHL7MatchingConfigs_CaseStatuses_PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs",
                column: "PartialMatchConfirmationStatusId",
                principalTable: "CaseStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiseaseHL7MatchingConfigs_CaseStatuses_PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropIndex(
                name: "IX_DiseaseHL7MatchingConfigs_PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "PartialMatchDetailsJson",
                table: "HL7Messages");

            migrationBuilder.DropColumn(
                name: "AllowMissingPathogen",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "AllowMissingResult",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "AllowMissingSpecimenType",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "AllowMissingTestMethod",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "MaxMissingFieldsAllowed",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.DropColumn(
                name: "PartialMatchConfirmationStatusId",
                table: "DiseaseHL7MatchingConfigs");

            migrationBuilder.AddColumn<bool>(
                name: "AlwaysCreateNewCase",
                table: "DiseaseReinfectionRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CaseMatchingStrategy",
                table: "DiseaseReinfectionRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsChronic",
                table: "DiseaseReinfectionRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MatchOnResultType",
                table: "DiseaseReinfectionRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MatchOnTestType",
                table: "DiseaseReinfectionRules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
