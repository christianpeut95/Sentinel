using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddDiseaseHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiseaseId",
                table: "Cases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Diseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PathIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsNotifiable = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diseases_Diseases_ParentDiseaseId",
                        column: x => x.ParentDiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_DiseaseId",
                table: "Cases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_Code",
                table: "Diseases",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_ExportCode",
                table: "Diseases",
                column: "ExportCode");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_Level_DisplayOrder",
                table: "Diseases",
                columns: new[] { "Level", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_ParentDiseaseId",
                table: "Diseases",
                column: "ParentDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_PathIds",
                table: "Diseases",
                column: "PathIds");

            migrationBuilder.AddForeignKey(
                name: "FK_Cases_Diseases_DiseaseId",
                table: "Cases",
                column: "DiseaseId",
                principalTable: "Diseases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cases_Diseases_DiseaseId",
                table: "Cases");

            migrationBuilder.DropTable(
                name: "Diseases");

            migrationBuilder.DropIndex(
                name: "IX_Cases_DiseaseId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "DiseaseId",
                table: "Cases");
        }
    }
}
