using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class SurveyFamilyReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent migration: handles databases in any state
            // - Fresh DB from InitialCreate (already has SurveyTemplateId)
            // - Old DB with SurveyFamilyRootId (needs rename)
            // - Partially migrated DB (some columns already added)

            migrationBuilder.Sql(@"
                -- 1. Rename SurveyFamilyRootId -> SurveyTemplateId if old column exists
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'SurveyFamilyRootId')
                BEGIN
                    -- Drop old FK if exists
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTemplates_SurveyTemplates_SurveyFamilyRootId')
                        ALTER TABLE [TaskTemplates] DROP CONSTRAINT [FK_TaskTemplates_SurveyTemplates_SurveyFamilyRootId];

                    -- Drop old index if exists
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTemplates_SurveyFamilyRootId' AND object_id = OBJECT_ID('TaskTemplates'))
                        DROP INDEX [IX_TaskTemplates_SurveyFamilyRootId] ON [TaskTemplates];

                    -- Rename column
                    EXEC sp_rename 'TaskTemplates.SurveyFamilyRootId', 'SurveyTemplateId', 'COLUMN';
                END

                -- 2. Add missing columns to TaskTemplates (if not already present)
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'SurveyDefinitionJson')
                    ALTER TABLE [TaskTemplates] ADD [SurveyDefinitionJson] nvarchar(max) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'DefaultInputMappingJson')
                    ALTER TABLE [TaskTemplates] ADD [DefaultInputMappingJson] nvarchar(max) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'DefaultOutputMappingJson')
                    ALTER TABLE [TaskTemplates] ADD [DefaultOutputMappingJson] nvarchar(max) NULL;

                -- 3. Ensure index exists on SurveyTemplateId
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTemplates_SurveyTemplateId' AND object_id = OBJECT_ID('TaskTemplates'))
                    CREATE INDEX [IX_TaskTemplates_SurveyTemplateId] ON [TaskTemplates] ([SurveyTemplateId]);

                -- 4. Ensure FK exists
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTemplates_SurveyTemplates_SurveyTemplateId')
                    ALTER TABLE [TaskTemplates] ADD CONSTRAINT [FK_TaskTemplates_SurveyTemplates_SurveyTemplateId]
                        FOREIGN KEY ([SurveyTemplateId]) REFERENCES [SurveyTemplates] ([Id]) ON DELETE SET NULL;

                -- 5. Add mapping columns to DiseaseTaskTemplates (if not already present)
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'DiseaseTaskTemplates' AND COLUMN_NAME = 'InputMappingJson')
                    ALTER TABLE [DiseaseTaskTemplates] ADD [InputMappingJson] nvarchar(max) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                               WHERE TABLE_NAME = 'DiseaseTaskTemplates' AND COLUMN_NAME = 'OutputMappingJson')
                    ALTER TABLE [DiseaseTaskTemplates] ADD [OutputMappingJson] nvarchar(max) NULL;

                -- 6. Clean up duplicate migration history entry from demo branch
                IF (SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE MigrationId = '20260315013226_SurveyFamilyReferences') > 0
                    DELETE FROM [__EFMigrationsHistory] WHERE MigrationId = '20260315013226_SurveyFamilyReferences';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'DiseaseTaskTemplates' AND COLUMN_NAME = 'InputMappingJson')
                    ALTER TABLE [DiseaseTaskTemplates] DROP COLUMN [InputMappingJson];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'DiseaseTaskTemplates' AND COLUMN_NAME = 'OutputMappingJson')
                    ALTER TABLE [DiseaseTaskTemplates] DROP COLUMN [OutputMappingJson];

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskTemplates_SurveyTemplates_SurveyTemplateId')
                    ALTER TABLE [TaskTemplates] DROP CONSTRAINT [FK_TaskTemplates_SurveyTemplates_SurveyTemplateId];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskTemplates_SurveyTemplateId' AND object_id = OBJECT_ID('TaskTemplates'))
                    DROP INDEX [IX_TaskTemplates_SurveyTemplateId] ON [TaskTemplates];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'SurveyDefinitionJson')
                    ALTER TABLE [TaskTemplates] DROP COLUMN [SurveyDefinitionJson];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'DefaultInputMappingJson')
                    ALTER TABLE [TaskTemplates] DROP COLUMN [DefaultInputMappingJson];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'DefaultOutputMappingJson')
                    ALTER TABLE [TaskTemplates] DROP COLUMN [DefaultOutputMappingJson];

                IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_NAME = 'TaskTemplates' AND COLUMN_NAME = 'SurveyTemplateId')
                    EXEC sp_rename 'TaskTemplates.SurveyTemplateId', 'SurveyFamilyRootId', 'COLUMN';
            ");
        }
    }
}
