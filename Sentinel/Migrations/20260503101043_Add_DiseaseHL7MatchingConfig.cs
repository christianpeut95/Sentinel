using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Add_DiseaseHL7MatchingConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiseaseHL7MatchingConfigs",
                columns: table => new
                {
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverrideParentRules = table.Column<bool>(type: "bit", nullable: false),
                    TestMethod_UseTextFallback = table.Column<bool>(type: "bit", nullable: false),
                    TestMethod_NormalizeWhitespace = table.Column<bool>(type: "bit", nullable: false),
                    TestMethod_IgnorePunctuation = table.Column<bool>(type: "bit", nullable: false),
                    TestMethod_CaseInsensitive = table.Column<bool>(type: "bit", nullable: false),
                    SpecimenType_UseTextFallback = table.Column<bool>(type: "bit", nullable: false),
                    SpecimenType_NormalizeWhitespace = table.Column<bool>(type: "bit", nullable: false),
                    SpecimenType_IgnorePunctuation = table.Column<bool>(type: "bit", nullable: false),
                    SpecimenType_CaseInsensitive = table.Column<bool>(type: "bit", nullable: false),
                    Pathogen_UseTextFallback = table.Column<bool>(type: "bit", nullable: false),
                    Pathogen_NormalizeWhitespace = table.Column<bool>(type: "bit", nullable: false),
                    Pathogen_IgnorePunctuation = table.Column<bool>(type: "bit", nullable: false),
                    Pathogen_CaseInsensitive = table.Column<bool>(type: "bit", nullable: false),
                    TestResult_UseTextFallback = table.Column<bool>(type: "bit", nullable: false),
                    TestResult_NormalizeWhitespace = table.Column<bool>(type: "bit", nullable: false),
                    TestResult_IgnorePunctuation = table.Column<bool>(type: "bit", nullable: false),
                    TestResult_CaseInsensitive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseHL7MatchingConfigs", x => x.DiseaseId);
                    table.ForeignKey(
                        name: "FK_DiseaseHL7MatchingConfigs_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiseaseHL7MatchingConfigs");
        }
    }
}
