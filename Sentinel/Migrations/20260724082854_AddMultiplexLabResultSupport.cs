using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiplexLabResultSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMultiplexClone",
                table: "LabResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentLabResultId",
                table: "LabResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_ParentLabResultId",
                table: "LabResults",
                column: "ParentLabResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_LabResults_ParentLabResultId",
                table: "LabResults",
                column: "ParentLabResultId",
                principalTable: "LabResults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_LabResults_ParentLabResultId",
                table: "LabResults");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_ParentLabResultId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "IsMultiplexClone",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "ParentLabResultId",
                table: "LabResults");
        }
    }
}
