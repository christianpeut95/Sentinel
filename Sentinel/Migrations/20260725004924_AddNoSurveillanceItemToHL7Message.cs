using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddNoSurveillanceItemToHL7Message : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoSurveillanceItem",
                table: "HL7Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoSurveillanceItem",
                table: "HL7Messages");
        }
    }
}
