using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSetupCompleted = table.Column<bool>(type: "bit", nullable: false),
                    SetupCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SetupCompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SetupToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SetupTokenGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SetupTokenExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AllowPublicRegistration = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApplicationUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnforceHttps = table.Column<bool>(type: "bit", nullable: false),
                    DomainName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SslCertificatePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SmtpHost = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SmtpPort = table.Column<int>(type: "int", nullable: true),
                    SmtpUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SmtpPasswordEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SmtpFromEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SmtpFromDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SmtpEnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    SmtpConfigured = table.Column<bool>(type: "bit", nullable: false),
                    HL7ProcessingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HL7DefaultDropPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HL7DefaultArchivePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SurveillanceStartupCompleted = table.Column<bool>(type: "bit", nullable: false),
                    SurveillanceStartupChecklistJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurveillanceStartupProgressPercentage = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemSettings_AspNetUsers_SetupCompletedByUserId",
                        column: x => x.SetupCompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_ModifiedByUserId",
                table: "SystemSettings",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_SetupCompletedByUserId",
                table: "SystemSettings",
                column: "SetupCompletedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
