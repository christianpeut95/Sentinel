using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddLabResultMarkerHistory_And_UpdateLabResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CaseId",
                table: "LabResults",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "LabResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResultFinalizedDate",
                table: "LabResultMarkers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultStatus",
                table: "LabResultMarkers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCode",
                table: "LabResultMarkers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabResultMarkerHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabResultMarkerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HL7MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    PreviousQualitativeValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousQuantitativeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PreviousResultStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PreviousAbnormalFlag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NewQualitativeValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NewQuantitativeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewResultStatus = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    NewAbnormalFlag = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBySystem = table.Column<bool>(type: "bit", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResultMarkerHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResultMarkerHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LabResultMarkerHistories_HL7Messages_HL7MessageId",
                        column: x => x.HL7MessageId,
                        principalTable: "HL7Messages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LabResultMarkerHistories_LabResultMarkers_LabResultMarkerId",
                        column: x => x.LabResultMarkerId,
                        principalTable: "LabResultMarkers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkerHistories_ChangedAt",
                table: "LabResultMarkerHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkerHistories_ChangedByUserId",
                table: "LabResultMarkerHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkerHistories_HL7MessageId",
                table: "LabResultMarkerHistories",
                column: "HL7MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkerHistories_LabResultMarkerId",
                table: "LabResultMarkerHistories",
                column: "LabResultMarkerId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResultMarkerHistories_LabResultMarkerId_ChangedAt",
                table: "LabResultMarkerHistories",
                columns: new[] { "LabResultMarkerId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_Patients_PatientId",
                table: "LabResults",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_Patients_PatientId",
                table: "LabResults");

            migrationBuilder.DropTable(
                name: "LabResultMarkerHistories");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "ResultFinalizedDate",
                table: "LabResultMarkers");

            migrationBuilder.DropColumn(
                name: "ResultStatus",
                table: "LabResultMarkers");

            migrationBuilder.DropColumn(
                name: "TestCode",
                table: "LabResultMarkers");

            migrationBuilder.AlterColumn<Guid>(
                name: "CaseId",
                table: "LabResults",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
