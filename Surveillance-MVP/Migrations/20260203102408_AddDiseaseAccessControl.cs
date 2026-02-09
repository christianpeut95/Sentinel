using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddDiseaseAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccessLevel",
                table: "Diseases",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RoleDiseaseAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDiseaseAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDiseaseAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDiseaseAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_CreatedByUserId",
                table: "RoleDiseaseAccess",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_DiseaseId",
                table: "RoleDiseaseAccess",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_RoleId_DiseaseId",
                table: "RoleDiseaseAccess",
                columns: new[] { "RoleId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_DiseaseId",
                table: "UserDiseaseAccess",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_ExpiresAt",
                table: "UserDiseaseAccess",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_GrantedByUserId",
                table: "UserDiseaseAccess",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_UserId_DiseaseId",
                table: "UserDiseaseAccess",
                columns: new[] { "UserId", "DiseaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleDiseaseAccess");

            migrationBuilder.DropTable(
                name: "UserDiseaseAccess");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "Diseases");
        }
    }
}
