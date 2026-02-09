using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddExposureTrackingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop IsNotifiable column only if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT * 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[Diseases]') 
                    AND name = 'IsNotifiable'
                )
                BEGIN
                    ALTER TABLE [Diseases] DROP COLUMN [IsNotifiable];
                END
            ");

            // Add ApplyToChildren to UserDiseaseAccess only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[UserDiseaseAccess]') 
                    AND name = 'ApplyToChildren'
                )
                BEGIN
                    ALTER TABLE [UserDiseaseAccess] ADD [ApplyToChildren] bit NOT NULL DEFAULT 0;
                END
            ");

            // Add InheritedFromDiseaseId to UserDiseaseAccess only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[UserDiseaseAccess]') 
                    AND name = 'InheritedFromDiseaseId'
                )
                BEGIN
                    ALTER TABLE [UserDiseaseAccess] ADD [InheritedFromDiseaseId] uniqueidentifier NULL;
                END
            ");

            // Add ApplyToChildren to RoleDiseaseAccess only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[RoleDiseaseAccess]') 
                    AND name = 'ApplyToChildren'
                )
                BEGIN
                    ALTER TABLE [RoleDiseaseAccess] ADD [ApplyToChildren] bit NOT NULL DEFAULT 0;
                END
            ");

            // Add InheritedFromDiseaseId to RoleDiseaseAccess only if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT * 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[RoleDiseaseAccess]') 
                    AND name = 'InheritedFromDiseaseId'
                )
                BEGIN
                    ALTER TABLE [RoleDiseaseAccess] ADD [InheritedFromDiseaseId] uniqueidentifier NULL;
                END
            ");

            // NOTE: Tables and indexes created via manual SQL script (AddExposureTrackingSystem.sql)
            // This migration has been simplified to avoid conflicts with manually created objects.
            // The SQL script checks for object existence before creating them.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseSymptoms");

            migrationBuilder.DropTable(
                name: "DiseaseSymptoms");

            migrationBuilder.DropTable(
                name: "ExposureEvents");

            migrationBuilder.DropTable(
                name: "Symptoms");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropColumn(
                name: "ApplyToChildren",
                table: "UserDiseaseAccess");

            migrationBuilder.DropColumn(
                name: "InheritedFromDiseaseId",
                table: "UserDiseaseAccess");

            migrationBuilder.DropColumn(
                name: "ApplyToChildren",
                table: "RoleDiseaseAccess");

            migrationBuilder.DropColumn(
                name: "InheritedFromDiseaseId",
                table: "RoleDiseaseAccess");

            migrationBuilder.AddColumn<bool>(
                name: "IsNotifiable",
                table: "Diseases",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
