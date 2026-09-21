using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddWebDataRocksLicenseAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableWebDataRocks",
                table: "SystemSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "WebDataRocksLicenseAcceptedAt",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebDataRocksLicenseAcceptedByUserId",
                table: "SystemSettings",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebDataRocksLicenseVersion",
                table: "SystemSettings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WebDataRocksLicenseAcceptedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebDataRocksLicenseVersion",
                table: "AspNetUsers",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableWebDataRocks",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WebDataRocksLicenseAcceptedAt",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WebDataRocksLicenseAcceptedByUserId",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WebDataRocksLicenseVersion",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "WebDataRocksLicenseAcceptedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WebDataRocksLicenseVersion",
                table: "AspNetUsers");
        }
    }
}
