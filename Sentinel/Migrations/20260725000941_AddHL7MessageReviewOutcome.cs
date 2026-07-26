using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddHL7MessageReviewOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if column exists before adding (idempotent migration)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'HL7Messages' AND COLUMN_NAME = 'ReviewOutcome')
                BEGIN
                    ALTER TABLE [HL7Messages] ADD [ReviewOutcome] int NOT NULL DEFAULT 0
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewOutcome",
                table: "HL7Messages");
        }
    }
}
