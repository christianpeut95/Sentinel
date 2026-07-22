using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Remove_HL7Message_Unique_Index_To_Allow_Duplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the unique index on MessageControlId + SendingFacility
            // This allows us to create duplicate HL7Message records for auditing purposes
            migrationBuilder.DropIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages");

            // Recreate the index as non-unique for query performance
            // This supports duplicate detection queries while allowing duplicate records
            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages",
                columns: new[] { "MessageControlId", "SendingFacility" },
                unique: false,
                filter: "[MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the non-unique index
            migrationBuilder.DropIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages");

            // Restore the unique index (this will fail if duplicates exist)
            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages",
                columns: new[] { "MessageControlId", "SendingFacility" },
                unique: true,
                filter: "[MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL");
        }
    }
}
