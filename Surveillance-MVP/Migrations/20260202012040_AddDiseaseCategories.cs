using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddDiseaseCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiseaseCategoryId",
                table: "Diseases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiseaseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_DiseaseCategoryId",
                table: "Diseases",
                column: "DiseaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_DisplayOrder",
                table: "DiseaseCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_Name",
                table: "DiseaseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_ReportingId",
                table: "DiseaseCategories",
                column: "ReportingId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Diseases_DiseaseCategories_DiseaseCategoryId",
                table: "Diseases",
                column: "DiseaseCategoryId",
                principalTable: "DiseaseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Diseases_DiseaseCategories_DiseaseCategoryId",
                table: "Diseases");

            migrationBuilder.DropTable(
                name: "DiseaseCategories");

            migrationBuilder.DropIndex(
                name: "IX_Diseases_DiseaseCategoryId",
                table: "Diseases");

            migrationBuilder.DropColumn(
                name: "DiseaseCategoryId",
                table: "Diseases");
        }
    }
}
