using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddHL7TestGeneratorTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HL7TestMessageTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LabTemplateType = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TestComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsFavorite = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7TestMessageTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7TestMessageTemplates_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7TestMessageTemplates_AspNetUsers_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HL7TestMessageHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RawHL7Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TestComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AccessionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PatientMRN = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConfigurationSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HL7MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessingResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GeneratedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    WasAutoProcessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7TestMessageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7TestMessageHistory_AspNetUsers_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7TestMessageHistory_HL7Messages_HL7MessageId",
                        column: x => x.HL7MessageId,
                        principalTable: "HL7Messages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7TestMessageHistory_HL7TestMessageTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "HL7TestMessageTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HL7TestMessageHistory_GeneratedByUserId",
                table: "HL7TestMessageHistory",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7TestMessageHistory_HL7MessageId",
                table: "HL7TestMessageHistory",
                column: "HL7MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7TestMessageHistory_TemplateId",
                table: "HL7TestMessageHistory",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7TestMessageTemplates_CreatedByUserId",
                table: "HL7TestMessageTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7TestMessageTemplates_UpdatedByUserId",
                table: "HL7TestMessageTemplates",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HL7TestMessageHistory");

            migrationBuilder.DropTable(
                name: "HL7TestMessageTemplates");
        }
    }
}
