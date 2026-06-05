using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Fix_PatientFriendlyId_UniqueIndex_ExcludeSoftDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_FriendlyId",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FriendlyId",
                table: "Patients",
                column: "FriendlyId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [FriendlyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_FriendlyId",
                table: "Patients");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FriendlyId",
                table: "Patients",
                column: "FriendlyId",
                unique: true);
        }
    }
}
