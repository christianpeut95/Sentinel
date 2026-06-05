using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class Add_HL7Message_Unique_MessageControlId_Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create unique index on MessageControlId and SendingFacility
            // This prevents duplicate HL7 messages from the same facility
            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages",
                columns: new[] { "MessageControlId", "SendingFacility" },
                unique: true,
                filter: "[MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HL7Messages_MessageControlId_SendingFacility",
                table: "HL7Messages");
        }
    }
}
