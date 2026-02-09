using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddCasesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendlyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfNotification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmationStatusId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_CaseStatuses_ConfirmationStatusId",
                        column: x => x.ConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cases_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ConfirmationStatusId",
                table: "Cases",
                column: "ConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_PatientId",
                table: "Cases",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cases");
        }
    }
}
