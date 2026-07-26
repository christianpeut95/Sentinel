IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Ancestries] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_Ancestries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NULL,
        [LastName] nvarchar(100) NULL,
        [PrimaryLanguage] nvarchar(50) NULL,
        [LanguagesSpokenJson] nvarchar(max) NULL,
        [IsInterviewWorker] bit NOT NULL,
        [AvailableForAutoAssignment] bit NOT NULL,
        [CurrentTaskCapacity] int NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AtsiStatuses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_AtsiStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [BackupHistory] (
        [Id] int NOT NULL IDENTITY,
        [BackupType] nvarchar(50) NOT NULL,
        [BackupFileName] nvarchar(500) NOT NULL,
        [BackupFilePath] nvarchar(1000) NOT NULL,
        [SizeInBytes] bigint NOT NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Success] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [CreatedBy] nvarchar(256) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BackupHistory] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseContactTasksFlattened] (
        [CaseGuid] uniqueidentifier NOT NULL,
        [CaseNumber] nvarchar(max) NOT NULL,
        [GenerationNumber] int NOT NULL,
        [TransmissionChainPath] nvarchar(max) NOT NULL,
        [TransmittedByCase] nvarchar(max) NULL,
        [CaseTypeEnum] int NOT NULL,
        [CaseType] nvarchar(max) NOT NULL,
        [DateOfOnset] datetime2 NULL,
        [DateOfNotification] datetime2 NULL,
        [CaseStatus] nvarchar(max) NULL,
        [PatientId] nvarchar(max) NULL,
        [PatientName] nvarchar(max) NOT NULL,
        [PatientFirstName] nvarchar(max) NOT NULL,
        [PatientLastName] nvarchar(max) NOT NULL,
        [PatientDOB] datetime2 NULL,
        [AgeAtOnset] int NULL,
        [PatientSuburb] nvarchar(max) NULL,
        [PatientState] nvarchar(max) NULL,
        [PatientMobile] nvarchar(max) NULL,
        [PatientEmail] nvarchar(max) NULL,
        [DiseaseName] nvarchar(max) NULL,
        [DiseaseCode] nvarchar(max) NULL,
        [Jurisdiction1] nvarchar(max) NULL,
        [Jurisdiction2] nvarchar(max) NULL,
        [Jurisdiction3] nvarchar(max) NULL,
        [ExposureEventId] uniqueidentifier NULL,
        [ExposureType] nvarchar(max) NULL,
        [ExposureStatusDisplay] nvarchar(max) NULL,
        [ExposureStartDate] datetime2 NULL,
        [ExposureEndDate] datetime2 NULL,
        [ExposureDescription] nvarchar(max) NULL,
        [ConfidenceLevel] nvarchar(max) NULL,
        [ContactClassification] nvarchar(max) NULL,
        [EventId] uniqueidentifier NULL,
        [EventName] nvarchar(max) NULL,
        [EventType] nvarchar(max) NULL,
        [EventStartDate] datetime2 NULL,
        [EventEndDate] datetime2 NULL,
        [EstimatedAttendees] int NULL,
        [EventSetting] nvarchar(max) NULL,
        [EventOrganizer] nvarchar(max) NULL,
        [LocationId] uniqueidentifier NULL,
        [LocationName] nvarchar(max) NULL,
        [LocationType] nvarchar(max) NULL,
        [LocationAddress] nvarchar(max) NULL,
        [LocationIsHighRisk] nvarchar(max) NULL,
        [LocationOrganization] nvarchar(max) NULL,
        [TaskId] uniqueidentifier NULL,
        [TaskNumber] nvarchar(max) NULL,
        [TaskTitle] nvarchar(max) NULL,
        [TaskDescription] nvarchar(max) NULL,
        [TaskStatus] nvarchar(max) NULL,
        [TaskPriority] nvarchar(max) NULL,
        [TaskDueDate] datetime2 NULL,
        [TaskCreatedAt] datetime2 NULL,
        [TaskCompletedAt] datetime2 NULL,
        [TaskCancelledAt] datetime2 NULL,
        [IsInterviewTask] bit NULL,
        [TaskType] nvarchar(max) NULL,
        [AssignmentType] nvarchar(max) NULL,
        [AssignedToEmail] nvarchar(max) NULL,
        [AssignedToName] nvarchar(max) NULL,
        [SurveyStatus] nvarchar(max) NULL,
        [IncubationPeriodDays] int NULL,
        [DaysUntilTaskDue] int NULL,
        [TaskAgeDays] int NULL,
        [TaskDueStatus] nvarchar(max) NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseStatuses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        [ApplicableTo] int NOT NULL,
        CONSTRAINT [PK_CaseStatuses] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseTimelineAll] (
        [CaseId] uniqueidentifier NOT NULL,
        [CaseNumber] nvarchar(max) NOT NULL,
        [PatientName] nvarchar(max) NOT NULL,
        [DiseaseName] nvarchar(max) NULL,
        [EventType] nvarchar(max) NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [EventUser] nvarchar(max) NULL,
        [EventDescription] nvarchar(max) NOT NULL,
        [EventSequence] int NOT NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ContactClassifications] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ContactClassifications] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ContactsListSimple] (
        [ContactId] uniqueidentifier NOT NULL,
        [ContactNumber] nvarchar(max) NOT NULL,
        [DateIdentified] datetime2 NOT NULL,
        [ContactDateOfOnset] datetime2 NULL,
        [PatientId] nvarchar(max) NULL,
        [ContactName] nvarchar(max) NOT NULL,
        [ContactFirstName] nvarchar(max) NOT NULL,
        [ContactLastName] nvarchar(max) NOT NULL,
        [ContactDOB] datetime2 NULL,
        [ContactMobile] nvarchar(max) NULL,
        [ContactEmail] nvarchar(max) NULL,
        [ContactSuburb] nvarchar(max) NULL,
        [ContactState] nvarchar(max) NULL,
        [ContactDisease] nvarchar(max) NULL,
        [ContactStatus] nvarchar(max) NULL,
        [ExposedByCase] nvarchar(max) NULL,
        [ExposedByName] nvarchar(max) NULL,
        [ExposedByDisease] nvarchar(max) NULL,
        [ExposureTypeEnum] int NULL,
        [ExposureType] nvarchar(max) NULL,
        [ExposureDate] datetime2 NULL,
        [ExposureEndDate] datetime2 NULL,
        [ExposureSetting] nvarchar(max) NULL,
        [EventName] nvarchar(max) NULL,
        [EventType] nvarchar(max) NULL,
        [LocationName] nvarchar(max) NULL,
        [LocationType] nvarchar(max) NULL,
        [ContactClassification] nvarchar(max) NULL,
        [Jurisdiction1] nvarchar(max) NULL,
        [TotalTasks] int NOT NULL,
        [CompletedTasks] int NOT NULL,
        [InterviewTasks] int NOT NULL,
        [NextTaskDueDate] datetime2 NULL,
        [FollowUpStatus] nvarchar(max) NOT NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ContactTracingMindMapEdges] (
        [EdgeId] uniqueidentifier NOT NULL,
        [SourceNodeId] uniqueidentifier NOT NULL,
        [TargetNodeId] uniqueidentifier NOT NULL,
        [SourceLabel] nvarchar(max) NOT NULL,
        [TargetLabel] nvarchar(max) NOT NULL,
        [ExposureTypeEnum] int NOT NULL,
        [ExposureType] nvarchar(max) NOT NULL,
        [ExposureStatusEnum] int NOT NULL,
        [ExposureStatus] nvarchar(max) NOT NULL,
        [EdgeLabel] nvarchar(max) NULL,
        [EventName] nvarchar(max) NULL,
        [EventType] nvarchar(max) NULL,
        [LocationName] nvarchar(max) NULL,
        [LocationType] nvarchar(max) NULL,
        [LocationAddress] nvarchar(max) NULL,
        [ContactClassification] nvarchar(max) NULL,
        [ExposureStartDate] datetime2 NULL,
        [ExposureEndDate] datetime2 NULL,
        [EdgeStyle] nvarchar(max) NOT NULL,
        [EdgeColor] nvarchar(max) NULL,
        [EdgeWeight] int NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ContactTracingMindMapNodes] (
        [NodeId] uniqueidentifier NOT NULL,
        [NodeLabel] nvarchar(max) NOT NULL,
        [NodeName] nvarchar(max) NOT NULL,
        [NodeType] nvarchar(max) NOT NULL,
        [DiseaseId] uniqueidentifier NULL,
        [DiseaseName] nvarchar(max) NULL,
        [DiseaseCode] nvarchar(max) NULL,
        [DateOfOnset] datetime2 NULL,
        [DateOfNotification] datetime2 NULL,
        [DateIdentified] datetime2 NOT NULL,
        [CaseStatus] nvarchar(max) NULL,
        [OutgoingTransmissions] int NOT NULL,
        [IncomingExposures] int NOT NULL,
        [TotalTasks] int NOT NULL,
        [CompletedTasks] int NOT NULL,
        [FollowUpStatus] nvarchar(max) NOT NULL,
        [Suburb] nvarchar(max) NULL,
        [State] nvarchar(max) NULL,
        [Jurisdiction1] nvarchar(max) NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Countries] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Countries] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [DiseaseCategories] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ReportingId] nvarchar(50) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_DiseaseCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [EventTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_EventTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Genders] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Genders] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Groups] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Groups] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [JurisdictionTypes] (
        [Id] int NOT NULL IDENTITY,
        [FieldNumber] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(20) NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_JurisdictionTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Languages] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Languages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [LocationTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsHighRisk] bit NOT NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_LocationTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [LookupTables] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LookupTables] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Occupations] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(6) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [MajorGroupCode] nvarchar(1) NULL,
        [MajorGroupName] nvarchar(max) NULL,
        [SubMajorGroupCode] nvarchar(2) NULL,
        [SubMajorGroupName] nvarchar(max) NULL,
        [MinorGroupCode] nvarchar(3) NULL,
        [MinorGroupName] nvarchar(max) NULL,
        [UnitGroupCode] nvarchar(4) NULL,
        [UnitGroupName] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Occupations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OrganizationTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OrganizationTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakTasksFlattened] (
        [OutbreakNumber] nvarchar(max) NOT NULL,
        [OutbreakName] nvarchar(max) NOT NULL,
        [OutbreakLevel] int NOT NULL,
        [HierarchyPath] nvarchar(max) NOT NULL,
        [OutbreakTypeEnum] int NOT NULL,
        [OutbreakType] nvarchar(max) NOT NULL,
        [OutbreakStatusEnum] int NOT NULL,
        [OutbreakStatus] nvarchar(max) NOT NULL,
        [OutbreakStartDate] datetime2 NOT NULL,
        [OutbreakEndDate] datetime2 NULL,
        [OutbreakConfirmationStatus] nvarchar(max) NULL,
        [PrimaryDisease] nvarchar(max) NULL,
        [PrimaryLocation] nvarchar(max) NULL,
        [PrimaryEvent] nvarchar(max) NULL,
        [LeadInvestigator] nvarchar(max) NULL,
        [LeadInvestigatorEmail] nvarchar(max) NULL,
        [CaseId] uniqueidentifier NOT NULL,
        [CaseNumber] nvarchar(max) NOT NULL,
        [CaseType] nvarchar(max) NOT NULL,
        [DateOfOnset] datetime2 NULL,
        [DateOfNotification] datetime2 NULL,
        [PatientName] nvarchar(max) NOT NULL,
        [PatientSuburb] nvarchar(max) NULL,
        [PatientState] nvarchar(max) NULL,
        [DiseaseName] nvarchar(max) NULL,
        [CaseStatus] nvarchar(max) NULL,
        [Jurisdiction1] nvarchar(max) NULL,
        [TaskId] uniqueidentifier NULL,
        [TaskNumber] nvarchar(max) NULL,
        [TaskTitle] nvarchar(max) NULL,
        [TaskDescription] nvarchar(max) NULL,
        [TaskStatus] nvarchar(max) NULL,
        [TaskPriority] nvarchar(max) NULL,
        [TaskDueDate] datetime2 NULL,
        [TaskCompletedAt] datetime2 NULL,
        [IsInterviewTask] bit NULL,
        [TaskType] nvarchar(max) NULL,
        [AssignedToEmail] nvarchar(max) NULL,
        [AssignedToName] nvarchar(max) NULL,
        [DaysIntoOutbreak] int NULL,
        [DaysUntilTaskDue] int NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Permissions] (
        [Id] int NOT NULL IDENTITY,
        [Module] int NOT NULL,
        [Action] int NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReportFolders] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [ParentFolderId] int NULL,
        [CreatedByUserId] nvarchar(450) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [AccessType] int NOT NULL,
        [Color] nvarchar(max) NULL,
        [Icon] nvarchar(max) NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_ReportFolders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportFolders_ReportFolders_ParentFolderId] FOREIGN KEY ([ParentFolderId]) REFERENCES [ReportFolders] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ResultUnits] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(50) NOT NULL,
        [Abbreviation] nvarchar(20) NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_ResultUnits] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [SexAtBirths] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_SexAtBirths] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [SpecimenTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExportCode] nvarchar(50) NULL,
        [IsInvasive] bit NOT NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_SpecimenTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [SurveyTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [Category] nvarchar(100) NULL,
        [SurveyDefinitionJson] nvarchar(max) NOT NULL,
        [DefaultInputMappingJson] nvarchar(max) NULL,
        [DefaultOutputMappingJson] nvarchar(max) NULL,
        [Version] int NOT NULL,
        [ParentSurveyTemplateId] uniqueidentifier NULL,
        [VersionNumber] nvarchar(20) NOT NULL,
        [VersionStatus] int NOT NULL,
        [VersionNotes] nvarchar(2000) NULL,
        [PublishedAt] datetime2 NULL,
        [PublishedBy] nvarchar(256) NULL,
        [Tags] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsSystemTemplate] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [UsageCount] int NOT NULL,
        [LastUsedAt] datetime2 NULL,
        CONSTRAINT [PK_SurveyTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SurveyTemplates_SurveyTemplates_ParentSurveyTemplateId] FOREIGN KEY ([ParentSurveyTemplateId]) REFERENCES [SurveyTemplates] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Symptoms] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Code] nvarchar(50) NULL,
        [ExportCode] nvarchar(50) NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Symptoms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [TaskTypes] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Code] nvarchar(20) NULL,
        [IconClass] nvarchar(50) NULL,
        [ColorClass] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [IsInterviewTask] bit NOT NULL,
        CONSTRAINT [PK_TaskTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [TestTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExportCode] nvarchar(50) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TestTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(50) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [FieldName] nvarchar(100) NOT NULL,
        [OldValue] nvarchar(max) NULL,
        [NewValue] nvarchar(max) NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangedByUserId] nvarchar(450) NULL,
        [IpAddress] nvarchar(45) NULL,
        [UserAgent] nvarchar(500) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_AspNetUsers_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Diseases] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [ExportCode] nvarchar(50) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DiseaseCategoryId] uniqueidentifier NULL,
        [ParentDiseaseId] uniqueidentifier NULL,
        [PathIds] nvarchar(4000) NOT NULL,
        [Level] int NOT NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [AccessLevel] int NOT NULL,
        [ExposureTrackingMode] int NOT NULL,
        [DefaultToResidentialAddress] bit NOT NULL,
        [AlwaysPromptForLocation] bit NOT NULL,
        [SyncWithPatientAddressUpdates] bit NOT NULL,
        [ExposureGuidanceText] nvarchar(1000) NULL,
        [RequireGeographicCoordinates] bit NOT NULL,
        [AllowDomesticAcquisition] bit NOT NULL,
        [ExposureDataGracePeriodDays] int NULL,
        [RequiredLocationTypeIds] nvarchar(500) NULL,
        [ReviewGroupingWindowHours] int NOT NULL,
        [ReviewAutoQueueLabResults] bit NOT NULL,
        [ReviewAutoQueueExposures] bit NOT NULL,
        [ReviewAutoQueueContacts] bit NOT NULL,
        [ReviewAutoQueueConfirmationChanges] bit NOT NULL,
        [ReviewAutoQueueDiseaseChanges] bit NOT NULL,
        [ReviewAutoQueueClinicalNotifications] bit NOT NULL,
        [ReviewAutoQueueNewCases] bit NOT NULL,
        [ReviewDefaultPriority] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Diseases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Diseases_DiseaseCategories_DiseaseCategoryId] FOREIGN KEY ([DiseaseCategoryId]) REFERENCES [DiseaseCategories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Diseases_Diseases_ParentDiseaseId] FOREIGN KEY ([ParentDiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [UserGroups] (
        [UserId] nvarchar(450) NOT NULL,
        [GroupId] int NOT NULL,
        CONSTRAINT [PK_UserGroups] PRIMARY KEY ([UserId], [GroupId]),
        CONSTRAINT [FK_UserGroups_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserGroups_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Jurisdictions] (
        [Id] int NOT NULL IDENTITY,
        [JurisdictionTypeId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NULL,
        [Description] nvarchar(1000) NULL,
        [ParentJurisdictionId] int NULL,
        [BoundaryData] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [Population] bigint NULL,
        [PopulationYear] int NULL,
        [PopulationSource] nvarchar(200) NULL,
        CONSTRAINT [PK_Jurisdictions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Jurisdictions_JurisdictionTypes_JurisdictionTypeId] FOREIGN KEY ([JurisdictionTypeId]) REFERENCES [JurisdictionTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Jurisdictions_Jurisdictions_ParentJurisdictionId] FOREIGN KEY ([ParentJurisdictionId]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CustomFieldDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(450) NOT NULL,
        [Label] nvarchar(max) NOT NULL,
        [Category] nvarchar(450) NOT NULL,
        [FieldType] int NOT NULL,
        [IsRequired] bit NOT NULL,
        [IsSearchable] bit NOT NULL,
        [ShowOnList] bit NOT NULL,
        [ShowOnCreateEdit] bit NOT NULL,
        [ShowOnDetails] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ValidationRules] nvarchar(max) NULL,
        [LookupTableId] int NULL,
        [ShowOnPatientForm] bit NOT NULL,
        [ShowOnCaseForm] bit NOT NULL,
        CONSTRAINT [PK_CustomFieldDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CustomFieldDefinitions_LookupTables_LookupTableId] FOREIGN KEY ([LookupTableId]) REFERENCES [LookupTables] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [LookupValues] (
        [Id] int NOT NULL IDENTITY,
        [LookupTableId] int NOT NULL,
        [Value] nvarchar(max) NOT NULL,
        [DisplayText] nvarchar(max) NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LookupValues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LookupValues_LookupTables_LookupTableId] FOREIGN KEY ([LookupTableId]) REFERENCES [LookupTables] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Organizations] (
        [Id] uniqueidentifier NOT NULL,
        [FriendlyId] nvarchar(20) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [OrganizationTypeId] int NULL,
        [Address] nvarchar(500) NULL,
        [Phone] nvarchar(50) NULL,
        [Email] nvarchar(200) NULL,
        [ContactPerson] nvarchar(200) NULL,
        [ExportCode] nvarchar(50) NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Organizations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Organizations_OrganizationTypes_OrganizationTypeId] FOREIGN KEY ([OrganizationTypeId]) REFERENCES [OrganizationTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [RolePermissions] (
        [RoleId] nvarchar(450) NOT NULL,
        [PermissionId] int NOT NULL,
        [IsGranted] bit NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RoleId], [PermissionId]),
        CONSTRAINT [FK_RolePermissions_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [UserPermissions] (
        [UserId] nvarchar(450) NOT NULL,
        [PermissionId] int NOT NULL,
        [IsGranted] bit NOT NULL,
        CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([UserId], [PermissionId]),
        CONSTRAINT [FK_UserPermissions_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReportDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [EntityType] nvarchar(50) NOT NULL,
        [Category] nvarchar(100) NULL,
        [PivotConfiguration] nvarchar(max) NULL,
        [CollectionQueriesJson] nvarchar(max) NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [IsPublic] bit NOT NULL,
        [IsTemplate] bit NOT NULL,
        [LastRunDate] datetime2 NULL,
        [RunCount] int NOT NULL,
        [FolderId] int NULL,
        CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportDefinitions_ReportFolders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [ReportFolders] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReportFolderShares] (
        [Id] int NOT NULL IDENTITY,
        [ReportFolderId] int NOT NULL,
        [TargetType] int NOT NULL,
        [UserId] nvarchar(450) NULL,
        [GroupId] int NULL,
        [PermissionLevel] int NOT NULL,
        [SharedByUserId] nvarchar(450) NOT NULL,
        [SharedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ReportFolderShares] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportFolderShares_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ReportFolderShares_Groups_GroupId] FOREIGN KEY ([GroupId]) REFERENCES [Groups] ([Id]),
        CONSTRAINT [FK_ReportFolderShares_ReportFolders_ReportFolderId] FOREIGN KEY ([ReportFolderId]) REFERENCES [ReportFolders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [SurveyFieldMappings] (
        [Id] uniqueidentifier NOT NULL,
        [ConfigurationType] int NOT NULL,
        [ConfigurationId] uniqueidentifier NOT NULL,
        [Priority] int NOT NULL,
        [SurveyQuestionName] nvarchar(500) NOT NULL,
        [TargetFieldPath] nvarchar(500) NOT NULL,
        [TargetFieldType] int NOT NULL,
        [FieldCategory] int NOT NULL,
        [MappingAction] int NOT NULL,
        [BusinessRule] int NOT NULL,
        [TriggerReviewQueue] bit NOT NULL,
        [ReviewPriority] int NOT NULL,
        [GroupingWindowHours] int NOT NULL,
        [ValidationRules] nvarchar(2000) NULL,
        [TransformationScript] nvarchar(2000) NULL,
        [DisplayName] nvarchar(500) NULL,
        [Description] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [TargetSymptomId] int NULL,
        [Complexity] int NOT NULL,
        [CollectionConfigJson] nvarchar(max) NULL,
        [MatchingRulesJson] nvarchar(max) NULL,
        [OnDuplicateFound] int NULL,
        [ExecutionOrder] int NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        CONSTRAINT [PK_SurveyFieldMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SurveyFieldMappings_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_SurveyFieldMappings_AspNetUsers_LastModifiedByUserId] FOREIGN KEY ([LastModifiedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_SurveyFieldMappings_Symptoms_TargetSymptomId] FOREIGN KEY ([TargetSymptomId]) REFERENCES [Symptoms] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [TaskTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [TaskTypeId] uniqueidentifier NOT NULL,
        [DefaultPriority] int NOT NULL,
        [TriggerType] int NOT NULL,
        [ApplicableToType] int NULL,
        [DueDaysFromOnset] int NULL,
        [DueDaysFromNotification] int NULL,
        [DueDaysFromContact] int NULL,
        [DueCalculationMethod] int NOT NULL,
        [IsRecurring] bit NOT NULL,
        [RecurrencePattern] int NULL,
        [RecurrenceCount] int NULL,
        [RecurrenceDurationDays] int NULL,
        [SurveyTemplateId] uniqueidentifier NULL,
        [SurveyDefinitionJson] nvarchar(max) NULL,
        [DefaultInputMappingJson] nvarchar(max) NULL,
        [DefaultOutputMappingJson] nvarchar(max) NULL,
        [Instructions] nvarchar(4000) NULL,
        [CompletionCriteria] nvarchar(1000) NULL,
        [RequiresEvidence] bit NOT NULL,
        [AssignmentType] int NOT NULL,
        [InheritanceBehavior] int NOT NULL,
        [RestrictToSubDiseaseIds] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [IsInterviewTask] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_TaskTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskTemplates_SurveyTemplates_SurveyTemplateId] FOREIGN KEY ([SurveyTemplateId]) REFERENCES [SurveyTemplates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_TaskTemplates_TaskTypes_TaskTypeId] FOREIGN KEY ([TaskTypeId]) REFERENCES [TaskTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [TestResults] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExportCode] nvarchar(50) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        [TestTypeId] int NULL,
        CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestResults_TestTypes_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestTypes] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [DiseaseSymptoms] (
        [Id] int NOT NULL IDENTITY,
        [DiseaseId] uniqueidentifier NOT NULL,
        [SymptomId] int NOT NULL,
        [IsCommon] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_DiseaseSymptoms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiseaseSymptoms_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiseaseSymptoms_Symptoms_SymptomId] FOREIGN KEY ([SymptomId]) REFERENCES [Symptoms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [RoleDiseaseAccess] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [IsAllowed] bit NOT NULL,
        [ApplyToChildren] bit NOT NULL,
        [InheritedFromDiseaseId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_RoleDiseaseAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleDiseaseAccess_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RoleDiseaseAccess_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_RoleDiseaseAccess_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [SurveyTemplateDiseases] (
        [Id] uniqueidentifier NOT NULL,
        [SurveyTemplateId] uniqueidentifier NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_SurveyTemplateDiseases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SurveyTemplateDiseases_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SurveyTemplateDiseases_SurveyTemplates_SurveyTemplateId] FOREIGN KEY ([SurveyTemplateId]) REFERENCES [SurveyTemplates] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [UserDiseaseAccess] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [IsAllowed] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NULL,
        [GrantedByUserId] nvarchar(450) NULL,
        [Reason] nvarchar(500) NULL,
        [ApplyToChildren] bit NOT NULL,
        [InheritedFromDiseaseId] uniqueidentifier NULL,
        CONSTRAINT [PK_UserDiseaseAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserDiseaseAccess_AspNetUsers_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_UserDiseaseAccess_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDiseaseAccess_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Patients] (
        [Id] uniqueidentifier NOT NULL,
        [FriendlyId] nvarchar(20) NOT NULL,
        [GivenName] nvarchar(max) NOT NULL,
        [FamilyName] nvarchar(max) NOT NULL,
        [DateOfBirth] datetime2 NULL,
        [SexAtBirthId] int NULL,
        [GenderId] int NULL,
        [HomePhone] nvarchar(max) NULL,
        [MobilePhone] nvarchar(max) NULL,
        [EmailAddress] nvarchar(max) NULL,
        [AddressLine] nvarchar(max) NULL,
        [City] nvarchar(max) NULL,
        [State] nvarchar(max) NULL,
        [PostalCode] nvarchar(max) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [CountryOfBirthId] int NULL,
        [LanguageSpokenAtHomeId] int NULL,
        [AncestryId] int NULL,
        [AtsiStatusId] int NULL,
        [OccupationId] int NULL,
        [IsDeceased] bit NOT NULL,
        [DateOfDeath] datetime2 NULL,
        [Jurisdiction1Id] int NULL,
        [Jurisdiction2Id] int NULL,
        [Jurisdiction3Id] int NULL,
        [Jurisdiction4Id] int NULL,
        [Jurisdiction5Id] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_Ancestries_AncestryId] FOREIGN KEY ([AncestryId]) REFERENCES [Ancestries] ([Id]),
        CONSTRAINT [FK_Patients_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Patients_AtsiStatuses_AtsiStatusId] FOREIGN KEY ([AtsiStatusId]) REFERENCES [AtsiStatuses] ([Id]),
        CONSTRAINT [FK_Patients_Countries_CountryOfBirthId] FOREIGN KEY ([CountryOfBirthId]) REFERENCES [Countries] ([Id]),
        CONSTRAINT [FK_Patients_Genders_GenderId] FOREIGN KEY ([GenderId]) REFERENCES [Genders] ([Id]),
        CONSTRAINT [FK_Patients_Jurisdictions_Jurisdiction1Id] FOREIGN KEY ([Jurisdiction1Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Patients_Jurisdictions_Jurisdiction2Id] FOREIGN KEY ([Jurisdiction2Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Patients_Jurisdictions_Jurisdiction3Id] FOREIGN KEY ([Jurisdiction3Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Patients_Jurisdictions_Jurisdiction4Id] FOREIGN KEY ([Jurisdiction4Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Patients_Jurisdictions_Jurisdiction5Id] FOREIGN KEY ([Jurisdiction5Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Patients_Languages_LanguageSpokenAtHomeId] FOREIGN KEY ([LanguageSpokenAtHomeId]) REFERENCES [Languages] ([Id]),
        CONSTRAINT [FK_Patients_Occupations_OccupationId] FOREIGN KEY ([OccupationId]) REFERENCES [Occupations] ([Id]),
        CONSTRAINT [FK_Patients_SexAtBirths_SexAtBirthId] FOREIGN KEY ([SexAtBirthId]) REFERENCES [SexAtBirths] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [DiseaseCustomFields] (
        [Id] int NOT NULL IDENTITY,
        [DiseaseId] uniqueidentifier NOT NULL,
        [CustomFieldDefinitionId] int NOT NULL,
        [InheritToChildDiseases] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_DiseaseCustomFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiseaseCustomFields_CustomFieldDefinitions_CustomFieldDefinitionId] FOREIGN KEY ([CustomFieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiseaseCustomFields_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Locations] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [LocationTypeId] int NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [GeocodingStatus] nvarchar(50) NULL,
        [LastGeocoded] datetime2 NULL,
        [OrganizationId] uniqueidentifier NULL,
        [IsHighRisk] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId] FOREIGN KEY ([LocationTypeId]) REFERENCES [LocationTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Locations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CalculatedFields] (
        [Id] int NOT NULL IDENTITY,
        [ReportDefinitionId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Expression] nvarchar(max) NOT NULL,
        [DataType] nvarchar(50) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_CalculatedFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CalculatedFields_ReportDefinitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [ReportDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReportFields] (
        [Id] int NOT NULL IDENTITY,
        [ReportDefinitionId] int NOT NULL,
        [FieldPath] nvarchar(500) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [DataType] nvarchar(50) NOT NULL,
        [PivotArea] nvarchar(20) NULL,
        [AggregationType] nvarchar(50) NULL,
        [DisplayOrder] int NOT NULL,
        [IsCustomField] bit NOT NULL,
        [CustomFieldDefinitionId] int NULL,
        CONSTRAINT [PK_ReportFields] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportFields_ReportDefinitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [ReportDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReportFilters] (
        [Id] int NOT NULL IDENTITY,
        [ReportDefinitionId] int NOT NULL,
        [FieldPath] nvarchar(500) NOT NULL,
        [Operator] nvarchar(50) NOT NULL,
        [Value] nvarchar(max) NULL,
        [DisplayOrder] int NOT NULL,
        [IsCustomField] bit NOT NULL,
        [CustomFieldDefinitionId] int NULL,
        [DataType] nvarchar(50) NOT NULL,
        [LogicOperator] nvarchar(10) NOT NULL,
        [GroupId] int NULL,
        [GroupLogicOperator] nvarchar(10) NOT NULL,
        [IsCollectionQuery] bit NOT NULL,
        [CollectionSubFilters] nvarchar(max) NULL,
        [CollectionOperator] nvarchar(20) NULL,
        CONSTRAINT [PK_ReportFilters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReportFilters_ReportDefinitions_ReportDefinitionId] FOREIGN KEY ([ReportDefinitionId]) REFERENCES [ReportDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [DiseaseTaskTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [TaskTemplateId] uniqueidentifier NOT NULL,
        [ApplicableTo] int NULL,
        [IsInherited] bit NOT NULL,
        [InheritedFromDiseaseId] uniqueidentifier NULL,
        [ApplyToChildren] bit NOT NULL,
        [AllowChildOverride] bit NOT NULL,
        [OverrideAutoCreate] bit NULL,
        [OverridePriority] int NULL,
        [OverrideDueDays] int NULL,
        [OverrideInstructions] nvarchar(4000) NULL,
        [AutoCreateOnCaseCreation] bit NOT NULL,
        [AutoCreateOnContactCreation] bit NOT NULL,
        [AutoCreateOnLabConfirmation] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [InputMappingJson] nvarchar(max) NULL,
        [OutputMappingJson] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_DiseaseTaskTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiseaseTaskTemplates_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DiseaseTaskTemplates_TaskTemplates_TaskTemplateId] FOREIGN KEY ([TaskTemplateId]) REFERENCES [TaskTemplates] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Cases] (
        [Id] uniqueidentifier NOT NULL,
        [FriendlyId] nvarchar(20) NOT NULL,
        [PatientId] uniqueidentifier NOT NULL,
        [DateOfOnset] datetime2 NULL,
        [DateOfNotification] datetime2 NULL,
        [ClinicalNotificationDate] datetime2 NULL,
        [ClinicalNotifierOrganisation] nvarchar(200) NULL,
        [ClinicalNotificationNotes] nvarchar(1000) NULL,
        [ConfirmationStatusId] int NULL,
        [DiseaseId] uniqueidentifier NULL,
        [Hospitalised] int NULL,
        [HospitalId] uniqueidentifier NULL,
        [DateOfAdmission] datetime2 NULL,
        [DateOfDischarge] datetime2 NULL,
        [DiedDueToDisease] int NULL,
        [Jurisdiction1Id] int NULL,
        [Jurisdiction2Id] int NULL,
        [Jurisdiction3Id] int NULL,
        [Jurisdiction4Id] int NULL,
        [Jurisdiction5Id] int NULL,
        [Type] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Cases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Cases_CaseStatuses_ConfirmationStatusId] FOREIGN KEY ([ConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]),
        CONSTRAINT [FK_Cases_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Jurisdictions_Jurisdiction1Id] FOREIGN KEY ([Jurisdiction1Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Jurisdictions_Jurisdiction2Id] FOREIGN KEY ([Jurisdiction2Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Jurisdictions_Jurisdiction3Id] FOREIGN KEY ([Jurisdiction3Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Jurisdictions_Jurisdiction4Id] FOREIGN KEY ([Jurisdiction4Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Jurisdictions_Jurisdiction5Id] FOREIGN KEY ([Jurisdiction5Id]) REFERENCES [Jurisdictions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Cases_Organizations_HospitalId] FOREIGN KEY ([HospitalId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_Cases_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [PatientCustomFieldBooleans] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] bit NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatientCustomFieldBooleans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientCustomFieldBooleans_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PatientCustomFieldBooleans_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [PatientCustomFieldDates] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatientCustomFieldDates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientCustomFieldDates_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PatientCustomFieldDates_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [PatientCustomFieldLookups] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [LookupValueId] int NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatientCustomFieldLookups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientCustomFieldLookups_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PatientCustomFieldLookups_LookupValues_LookupValueId] FOREIGN KEY ([LookupValueId]) REFERENCES [LookupValues] ([Id]),
        CONSTRAINT [FK_PatientCustomFieldLookups_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [PatientCustomFieldNumbers] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] decimal(18,2) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatientCustomFieldNumbers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientCustomFieldNumbers_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PatientCustomFieldNumbers_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [PatientCustomFieldStrings] (
        [Id] int NOT NULL IDENTITY,
        [PatientId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PatientCustomFieldStrings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PatientCustomFieldStrings_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PatientCustomFieldStrings_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Events] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [EventTypeId] int NULL,
        [LocationId] uniqueidentifier NOT NULL,
        [StartDateTime] datetime2 NOT NULL,
        [EndDateTime] datetime2 NULL,
        [EstimatedAttendees] int NULL,
        [IsIndoor] bit NULL,
        [OrganizerOrganizationId] uniqueidentifier NULL,
        [Description] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Events_EventTypes_EventTypeId] FOREIGN KEY ([EventTypeId]) REFERENCES [EventTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Events_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Events_Organizations_OrganizerOrganizationId] FOREIGN KEY ([OrganizerOrganizationId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseCustomFieldBooleans] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] bit NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CaseCustomFieldBooleans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseCustomFieldBooleans_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldBooleans_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseCustomFieldDates] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] datetime2 NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CaseCustomFieldDates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseCustomFieldDates_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldDates_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseCustomFieldLookups] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [LookupValueId] int NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CaseCustomFieldLookups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseCustomFieldLookups_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldLookups_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldLookups_LookupValues_LookupValueId] FOREIGN KEY ([LookupValueId]) REFERENCES [LookupValues] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseCustomFieldNumbers] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] decimal(18,2) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CaseCustomFieldNumbers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseCustomFieldNumbers_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldNumbers_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseCustomFieldStrings] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FieldDefinitionId] int NOT NULL,
        [Value] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CaseCustomFieldStrings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseCustomFieldStrings_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseCustomFieldStrings_CustomFieldDefinitions_FieldDefinitionId] FOREIGN KEY ([FieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseSymptoms] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [SymptomId] int NOT NULL,
        [OnsetDate] datetime2 NULL,
        [Severity] nvarchar(20) NULL,
        [Notes] nvarchar(1000) NULL,
        [OtherSymptomText] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_CaseSymptoms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseSymptoms_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseSymptoms_Symptoms_SymptomId] FOREIGN KEY ([SymptomId]) REFERENCES [Symptoms] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [CaseTasks] (
        [Id] uniqueidentifier NOT NULL,
        [CaseId] uniqueidentifier NOT NULL,
        [TaskTemplateId] uniqueidentifier NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [TaskTypeId] uniqueidentifier NOT NULL,
        [Priority] int NOT NULL,
        [AssignedToUserId] nvarchar(450) NULL,
        [AssignmentType] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DueDate] datetime2 NULL,
        [CompletedAt] datetime2 NULL,
        [CancelledAt] datetime2 NULL,
        [Status] int NOT NULL,
        [CompletionNotes] nvarchar(2000) NULL,
        [CompletedByUserId] nvarchar(450) NULL,
        [CancellationReason] nvarchar(1000) NULL,
        [EvidenceFileIds] nvarchar(2000) NULL,
        [SurveyResponseJson] nvarchar(max) NULL,
        [ParentTaskId] uniqueidentifier NULL,
        [RecurrenceSequence] int NULL,
        [IsInterviewTask] bit NOT NULL,
        [AssignmentMethod] int NOT NULL,
        [LanguageRequired] nvarchar(50) NULL,
        [MaxCallAttempts] int NOT NULL,
        [CurrentAttemptCount] int NOT NULL,
        [EscalationLevel] int NOT NULL,
        [LastCallAttempt] datetime2 NULL,
        [AutoAssignedAt] datetime2 NULL,
        [ModifiedAt] datetime2 NULL,
        [CaseId1] uniqueidentifier NULL,
        CONSTRAINT [PK_CaseTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseTasks_AspNetUsers_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_CaseTasks_AspNetUsers_CompletedByUserId] FOREIGN KEY ([CompletedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_CaseTasks_CaseTasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [CaseTasks] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseTasks_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseTasks_Cases_CaseId1] FOREIGN KEY ([CaseId1]) REFERENCES [Cases] ([Id]),
        CONSTRAINT [FK_CaseTasks_TaskTemplates_TaskTemplateId] FOREIGN KEY ([TaskTemplateId]) REFERENCES [TaskTemplates] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_CaseTasks_TaskTypes_TaskTypeId] FOREIGN KEY ([TaskTypeId]) REFERENCES [TaskTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [LabResults] (
        [Id] uniqueidentifier NOT NULL,
        [FriendlyId] nvarchar(20) NOT NULL,
        [CaseId] uniqueidentifier NOT NULL,
        [LaboratoryId] uniqueidentifier NULL,
        [AccessionNumber] nvarchar(100) NULL,
        [SpecimenCollectionDate] datetime2 NULL,
        [SpecimenTypeId] int NULL,
        [TestTypeId] int NULL,
        [TestedDiseaseId] uniqueidentifier NULL,
        [OrderingProviderId] uniqueidentifier NULL,
        [TestResultId] int NULL,
        [ResultDate] datetime2 NULL,
        [QuantitativeResult] decimal(18,2) NULL,
        [ResultUnitsId] int NULL,
        [IsAmended] bit NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [LabInterpretation] nvarchar(2000) NULL,
        [AttachmentPath] nvarchar(500) NULL,
        [AttachmentFileName] nvarchar(200) NULL,
        [AttachmentSize] bigint NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_LabResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabResults_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_Diseases_TestedDiseaseId] FOREIGN KEY ([TestedDiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_Organizations_LaboratoryId] FOREIGN KEY ([LaboratoryId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_Organizations_OrderingProviderId] FOREIGN KEY ([OrderingProviderId]) REFERENCES [Organizations] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_ResultUnits_ResultUnitsId] FOREIGN KEY ([ResultUnitsId]) REFERENCES [ResultUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_SpecimenTypes_SpecimenTypeId] FOREIGN KEY ([SpecimenTypeId]) REFERENCES [SpecimenTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_TestResults_TestResultId] FOREIGN KEY ([TestResultId]) REFERENCES [TestResults] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResults_TestTypes_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestTypes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ExposureEvents] (
        [Id] uniqueidentifier NOT NULL,
        [ExposedCaseId] uniqueidentifier NOT NULL,
        [ExposureType] int NOT NULL,
        [ExposureStartDate] datetime2 NOT NULL,
        [ExposureEndDate] datetime2 NULL,
        [EventId] uniqueidentifier NULL,
        [LocationId] uniqueidentifier NULL,
        [SourceCaseId] uniqueidentifier NULL,
        [ContactClassificationId] int NULL,
        [CountryCode] nvarchar(3) NULL,
        [FreeTextLocation] nvarchar(500) NULL,
        [Description] nvarchar(2000) NULL,
        [ExposureStatus] int NOT NULL,
        [ConfidenceLevel] nvarchar(50) NULL,
        [IsDefaultedFromResidentialAddress] bit NOT NULL,
        [IsReportingExposure] bit NOT NULL,
        [IsInterstateTravel] bit NOT NULL,
        [InterstateOriginState] nvarchar(100) NULL,
        [AddressLine] nvarchar(200) NULL,
        [City] nvarchar(100) NULL,
        [State] nvarchar(100) NULL,
        [PostalCode] nvarchar(20) NULL,
        [Country] nvarchar(100) NULL,
        [Latitude] decimal(18,6) NULL,
        [Longitude] decimal(18,6) NULL,
        [GeocodingAccuracy] nvarchar(50) NULL,
        [GeocodedDate] datetime2 NULL,
        [InvestigationNotes] nvarchar(2000) NULL,
        [StatusChangedDate] datetime2 NULL,
        [StatusChangedByUserId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_ExposureEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExposureEvents_Cases_ExposedCaseId] FOREIGN KEY ([ExposedCaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExposureEvents_Cases_SourceCaseId] FOREIGN KEY ([SourceCaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExposureEvents_ContactClassifications_ContactClassificationId] FOREIGN KEY ([ContactClassificationId]) REFERENCES [ContactClassifications] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExposureEvents_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ExposureEvents_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Outbreaks] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Type] int NOT NULL,
        [Status] int NOT NULL,
        [ConfirmationStatusId] int NULL,
        [ParentOutbreakId] int NULL,
        [IndexCaseId] uniqueidentifier NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [PrimaryDiseaseId] uniqueidentifier NULL,
        [PrimaryLocationId] uniqueidentifier NULL,
        [PrimaryEventId] uniqueidentifier NULL,
        [LeadInvestigatorId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [ModifiedDate] datetime2 NULL,
        [ModifiedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Outbreaks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Outbreaks_AspNetUsers_LeadInvestigatorId] FOREIGN KEY ([LeadInvestigatorId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_Outbreaks_CaseStatuses_ConfirmationStatusId] FOREIGN KEY ([ConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]),
        CONSTRAINT [FK_Outbreaks_Cases_IndexCaseId] FOREIGN KEY ([IndexCaseId]) REFERENCES [Cases] ([Id]),
        CONSTRAINT [FK_Outbreaks_Diseases_PrimaryDiseaseId] FOREIGN KEY ([PrimaryDiseaseId]) REFERENCES [Diseases] ([Id]),
        CONSTRAINT [FK_Outbreaks_Events_PrimaryEventId] FOREIGN KEY ([PrimaryEventId]) REFERENCES [Events] ([Id]),
        CONSTRAINT [FK_Outbreaks_Locations_PrimaryLocationId] FOREIGN KEY ([PrimaryLocationId]) REFERENCES [Locations] ([Id]),
        CONSTRAINT [FK_Outbreaks_Outbreaks_ParentOutbreakId] FOREIGN KEY ([ParentOutbreakId]) REFERENCES [Outbreaks] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [ReviewQueue] (
        [Id] int NOT NULL IDENTITY,
        [EntityType] nvarchar(50) NOT NULL,
        [EntityId] int NOT NULL,
        [CaseId] uniqueidentifier NULL,
        [PatientId] uniqueidentifier NULL,
        [DiseaseId] uniqueidentifier NULL,
        [ChangeType] nvarchar(50) NOT NULL,
        [TriggerField] nvarchar(100) NULL,
        [ChangeSnapshot] nvarchar(max) NULL,
        [Priority] int NOT NULL,
        [ReviewStatus] nvarchar(50) NOT NULL,
        [ReviewAction] nvarchar(50) NULL,
        [GroupKey] nvarchar(255) NULL,
        [GroupCount] int NOT NULL,
        [PotentialMatchesJson] nvarchar(max) NULL,
        [ProposedEntityDataJson] nvarchar(max) NULL,
        [CollectionSourceDataJson] nvarchar(max) NULL,
        [SelectedExistingEntityId] uniqueidentifier NULL,
        [ReviewedByUserId] nvarchar(450) NULL,
        [ReviewedDate] datetime2 NULL,
        [ReviewNotes] nvarchar(max) NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [TaskId] uniqueidentifier NULL,
        CONSTRAINT [PK_ReviewQueue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReviewQueue_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ReviewQueue_AspNetUsers_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_ReviewQueue_CaseTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [CaseTasks] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_ReviewQueue_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReviewQueue_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReviewQueue_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [TaskCallAttempts] (
        [Id] uniqueidentifier NOT NULL,
        [TaskId] uniqueidentifier NOT NULL,
        [AttemptedByUserId] nvarchar(450) NOT NULL,
        [AttemptedAt] datetime2 NOT NULL,
        [Outcome] int NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [DurationSeconds] int NULL,
        [NextCallbackScheduled] datetime2 NULL,
        [PhoneNumberCalled] nvarchar(50) NULL,
        CONSTRAINT [PK_TaskCallAttempts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskCallAttempts_AspNetUsers_AttemptedByUserId] FOREIGN KEY ([AttemptedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskCallAttempts_CaseTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [CaseTasks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [Notes] (
        [Id] uniqueidentifier NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [Subject] nvarchar(100) NULL,
        [Type] nvarchar(50) NOT NULL,
        [Recipient] nvarchar(200) NULL,
        [PatientId] uniqueidentifier NULL,
        [CaseId] uniqueidentifier NULL,
        [OutbreakId] int NULL,
        [AttachmentPath] nvarchar(500) NULL,
        [AttachmentFileName] nvarchar(200) NULL,
        [AttachmentSize] bigint NULL,
        [CreatedBy] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_Notes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notes_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Notes_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]),
        CONSTRAINT [FK_Notes_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakCaseDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [DefinitionName] nvarchar(200) NOT NULL,
        [DefinitionText] nvarchar(2000) NULL,
        [Classification] int NOT NULL,
        [CriteriaJson] nvarchar(max) NOT NULL,
        [Version] int NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OutbreakCaseDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakCaseDefinitions_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakLineListConfigurations] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [SelectedFields] nvarchar(max) NOT NULL,
        [SortConfiguration] nvarchar(max) NOT NULL,
        [FilterConfiguration] nvarchar(max) NULL,
        [UserId] nvarchar(450) NULL,
        [IsShared] bit NOT NULL,
        [IsDefault] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_OutbreakLineListConfigurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakLineListConfigurations_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_OutbreakLineListConfigurations_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_OutbreakLineListConfigurations_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakSearchQueries] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [QueryName] nvarchar(200) NOT NULL,
        [QueryJson] nvarchar(max) NOT NULL,
        [IsAutoLink] bit NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        [LastRunDate] datetime2 NULL,
        [LastRunMatchCount] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OutbreakSearchQueries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakSearchQueries_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakTeamMembers] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Role] int NOT NULL,
        [AssignedDate] datetime2 NOT NULL,
        [AssignedBy] nvarchar(max) NULL,
        [RemovedDate] datetime2 NULL,
        [RemovedBy] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OutbreakTeamMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakTeamMembers_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OutbreakTeamMembers_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakTimelines] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [EventDate] datetime2 NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(2000) NULL,
        [EventType] int NOT NULL,
        [RelatedCaseId] uniqueidentifier NULL,
        [RelatedNoteId] int NULL,
        [CreatedDate] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_OutbreakTimelines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakTimelines_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE TABLE [OutbreakCases] (
        [Id] int NOT NULL IDENTITY,
        [OutbreakId] int NOT NULL,
        [CaseId] uniqueidentifier NOT NULL,
        [IsIndexCase] bit NOT NULL,
        [Classification] int NULL,
        [ClassificationDate] datetime2 NULL,
        [ClassifiedBy] nvarchar(max) NULL,
        [ClassificationNotes] nvarchar(1000) NULL,
        [LinkMethod] int NOT NULL,
        [SearchQueryId] int NULL,
        [LinkedDate] datetime2 NOT NULL,
        [LinkedBy] nvarchar(max) NULL,
        [UnlinkedDate] datetime2 NULL,
        [UnlinkedBy] nvarchar(max) NULL,
        [UnlinkReason] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_OutbreakCases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OutbreakCases_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OutbreakCases_OutbreakSearchQueries_SearchQueryId] FOREIGN KEY ([SearchQueryId]) REFERENCES [OutbreakSearchQueries] ([Id]),
        CONSTRAINT [FK_OutbreakCases_Outbreaks_OutbreakId] FOREIGN KEY ([OutbreakId]) REFERENCES [Outbreaks] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ChangedAt] ON [AuditLogs] ([ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ChangedByUserId] ON [AuditLogs] ([ChangedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [AuditLogs] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CalculatedFields_ReportDefinitionId] ON [CalculatedFields] ([ReportDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseCustomFieldBooleans_CaseId_FieldDefinitionId] ON [CaseCustomFieldBooleans] ([CaseId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldBooleans_FieldDefinitionId] ON [CaseCustomFieldBooleans] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseCustomFieldDates_CaseId_FieldDefinitionId] ON [CaseCustomFieldDates] ([CaseId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldDates_FieldDefinitionId] ON [CaseCustomFieldDates] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldDates_Value] ON [CaseCustomFieldDates] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseCustomFieldLookups_CaseId_FieldDefinitionId] ON [CaseCustomFieldLookups] ([CaseId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldLookups_FieldDefinitionId] ON [CaseCustomFieldLookups] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldLookups_LookupValueId] ON [CaseCustomFieldLookups] ([LookupValueId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseCustomFieldNumbers_CaseId_FieldDefinitionId] ON [CaseCustomFieldNumbers] ([CaseId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldNumbers_FieldDefinitionId] ON [CaseCustomFieldNumbers] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldNumbers_Value] ON [CaseCustomFieldNumbers] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CaseCustomFieldStrings_CaseId_FieldDefinitionId] ON [CaseCustomFieldStrings] ([CaseId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldStrings_FieldDefinitionId] ON [CaseCustomFieldStrings] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseCustomFieldStrings_Value] ON [CaseCustomFieldStrings] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_ConfirmationStatusId] ON [Cases] ([ConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_DiseaseId] ON [Cases] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_HospitalId] ON [Cases] ([HospitalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_Jurisdiction1Id] ON [Cases] ([Jurisdiction1Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_Jurisdiction2Id] ON [Cases] ([Jurisdiction2Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_Jurisdiction3Id] ON [Cases] ([Jurisdiction3Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_Jurisdiction4Id] ON [Cases] ([Jurisdiction4Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_Jurisdiction5Id] ON [Cases] ([Jurisdiction5Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Cases_PatientId] ON [Cases] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CaseSymptoms_CaseId_SymptomId] ON [CaseSymptoms] ([CaseId], [SymptomId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_CaseSymptoms_OnsetDate] ON [CaseSymptoms] ([OnsetDate]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseSymptoms_SymptomId] ON [CaseSymptoms] ([SymptomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_AssignedToUserId] ON [CaseTasks] ([AssignedToUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_CaseId] ON [CaseTasks] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_CaseId1] ON [CaseTasks] ([CaseId1]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_CompletedByUserId] ON [CaseTasks] ([CompletedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_DueDate] ON [CaseTasks] ([DueDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_ParentTaskId] ON [CaseTasks] ([ParentTaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_Priority] ON [CaseTasks] ([Priority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_Status] ON [CaseTasks] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_TaskTemplateId] ON [CaseTasks] ([TaskTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CaseTasks_TaskTypeId] ON [CaseTasks] ([TaskTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CustomFieldDefinitions_Category_DisplayOrder] ON [CustomFieldDefinitions] ([Category], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_CustomFieldDefinitions_LookupTableId] ON [CustomFieldDefinitions] ([LookupTableId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CustomFieldDefinitions_Name] ON [CustomFieldDefinitions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseCategories_DisplayOrder] ON [DiseaseCategories] ([DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiseaseCategories_Name] ON [DiseaseCategories] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiseaseCategories_ReportingId] ON [DiseaseCategories] ([ReportingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseCustomFields_CustomFieldDefinitionId] ON [DiseaseCustomFields] ([CustomFieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiseaseCustomFields_DiseaseId_CustomFieldDefinitionId] ON [DiseaseCustomFields] ([DiseaseId], [CustomFieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Diseases_Code] ON [Diseases] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Diseases_DiseaseCategoryId] ON [Diseases] ([DiseaseCategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Diseases_ExportCode] ON [Diseases] ([ExportCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Diseases_Level_DisplayOrder] ON [Diseases] ([Level], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Diseases_ParentDiseaseId] ON [Diseases] ([ParentDiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Diseases_PathIds] ON [Diseases] ([PathIds]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_DiseaseSymptoms_DiseaseId_IsCommon_SortOrder] ON [DiseaseSymptoms] ([DiseaseId], [IsCommon], [SortOrder]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DiseaseSymptoms_DiseaseId_SymptomId] ON [DiseaseSymptoms] ([DiseaseId], [SymptomId]) WHERE [IsDeleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseSymptoms_SymptomId] ON [DiseaseSymptoms] ([SymptomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DiseaseTaskTemplates_DiseaseId_TaskTemplateId] ON [DiseaseTaskTemplates] ([DiseaseId], [TaskTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseTaskTemplates_InheritedFromDiseaseId] ON [DiseaseTaskTemplates] ([InheritedFromDiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseTaskTemplates_IsInherited] ON [DiseaseTaskTemplates] ([IsInherited]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_DiseaseTaskTemplates_TaskTemplateId] ON [DiseaseTaskTemplates] ([TaskTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Events_EventTypeId] ON [Events] ([EventTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Events_LocationId] ON [Events] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Events_Name] ON [Events] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Events_OrganizerOrganizationId] ON [Events] ([OrganizerOrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Events_StartDateTime_EndDateTime] ON [Events] ([StartDateTime], [EndDateTime]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_City] ON [ExposureEvents] ([City]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_ContactClassificationId] ON [ExposureEvents] ([ContactClassificationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_EventId] ON [ExposureEvents] ([EventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_ExposedCaseId] ON [ExposureEvents] ([ExposedCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_ExposureStartDate_ExposureEndDate] ON [ExposureEvents] ([ExposureStartDate], [ExposureEndDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_ExposureStatus] ON [ExposureEvents] ([ExposureStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_ExposureType] ON [ExposureEvents] ([ExposureType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_IsReportingExposure] ON [ExposureEvents] ([IsReportingExposure]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_Latitude_Longitude] ON [ExposureEvents] ([Latitude], [Longitude]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_LocationId] ON [ExposureEvents] ([LocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_PostalCode] ON [ExposureEvents] ([PostalCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_SourceCaseId] ON [ExposureEvents] ([SourceCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ExposureEvents_State] ON [ExposureEvents] ([State]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Jurisdictions_JurisdictionTypeId] ON [Jurisdictions] ([JurisdictionTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Jurisdictions_Name] ON [Jurisdictions] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Jurisdictions_ParentJurisdictionId] ON [Jurisdictions] ([ParentJurisdictionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JurisdictionTypes_FieldNumber] ON [JurisdictionTypes] ([FieldNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_JurisdictionTypes_Name] ON [JurisdictionTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_AccessionNumber] ON [LabResults] ([AccessionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_CaseId] ON [LabResults] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LabResults_FriendlyId] ON [LabResults] ([FriendlyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_LaboratoryId] ON [LabResults] ([LaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_OrderingProviderId] ON [LabResults] ([OrderingProviderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_ResultDate] ON [LabResults] ([ResultDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_ResultUnitsId] ON [LabResults] ([ResultUnitsId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_SpecimenCollectionDate] ON [LabResults] ([SpecimenCollectionDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_SpecimenTypeId] ON [LabResults] ([SpecimenTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestedDiseaseId] ON [LabResults] ([TestedDiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestResultId] ON [LabResults] ([TestResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestTypeId] ON [LabResults] ([TestTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Locations_GeocodingStatus] ON [Locations] ([GeocodingStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Locations_Latitude_Longitude] ON [Locations] ([Latitude], [Longitude]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Locations_LocationTypeId] ON [Locations] ([LocationTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Locations_Name] ON [Locations] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Locations_OrganizationId] ON [Locations] ([OrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LookupTables_Name] ON [LookupTables] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_LookupValues_LookupTableId_DisplayOrder] ON [LookupValues] ([LookupTableId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Notes_CaseId] ON [Notes] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Notes_CreatedAt] ON [Notes] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Notes_CreatedBy] ON [Notes] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Notes_OutbreakId] ON [Notes] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Notes_PatientId] ON [Notes] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Organizations_ExportCode] ON [Organizations] ([ExportCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Organizations_FriendlyId] ON [Organizations] ([FriendlyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Organizations_Name] ON [Organizations] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Organizations_OrganizationTypeId] ON [Organizations] ([OrganizationTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakCaseDefinitions_OutbreakId] ON [OutbreakCaseDefinitions] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakCases_CaseId] ON [OutbreakCases] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakCases_OutbreakId] ON [OutbreakCases] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakCases_SearchQueryId] ON [OutbreakCases] ([SearchQueryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakLineListConfigurations_CreatedByUserId] ON [OutbreakLineListConfigurations] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakLineListConfigurations_OutbreakId] ON [OutbreakLineListConfigurations] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakLineListConfigurations_UserId] ON [OutbreakLineListConfigurations] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_ConfirmationStatusId] ON [Outbreaks] ([ConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_IndexCaseId] ON [Outbreaks] ([IndexCaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_LeadInvestigatorId] ON [Outbreaks] ([LeadInvestigatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_ParentOutbreakId] ON [Outbreaks] ([ParentOutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_PrimaryDiseaseId] ON [Outbreaks] ([PrimaryDiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_PrimaryEventId] ON [Outbreaks] ([PrimaryEventId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Outbreaks_PrimaryLocationId] ON [Outbreaks] ([PrimaryLocationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakSearchQueries_OutbreakId] ON [OutbreakSearchQueries] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakTeamMembers_OutbreakId] ON [OutbreakTeamMembers] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakTeamMembers_UserId] ON [OutbreakTeamMembers] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_OutbreakTimelines_OutbreakId] ON [OutbreakTimelines] ([OutbreakId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldBooleans_FieldDefinitionId] ON [PatientCustomFieldBooleans] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatientCustomFieldBooleans_PatientId_FieldDefinitionId] ON [PatientCustomFieldBooleans] ([PatientId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldDates_FieldDefinitionId] ON [PatientCustomFieldDates] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatientCustomFieldDates_PatientId_FieldDefinitionId] ON [PatientCustomFieldDates] ([PatientId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldDates_Value] ON [PatientCustomFieldDates] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldLookups_FieldDefinitionId] ON [PatientCustomFieldLookups] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldLookups_LookupValueId] ON [PatientCustomFieldLookups] ([LookupValueId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatientCustomFieldLookups_PatientId_FieldDefinitionId] ON [PatientCustomFieldLookups] ([PatientId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldNumbers_FieldDefinitionId] ON [PatientCustomFieldNumbers] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatientCustomFieldNumbers_PatientId_FieldDefinitionId] ON [PatientCustomFieldNumbers] ([PatientId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldNumbers_Value] ON [PatientCustomFieldNumbers] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldStrings_FieldDefinitionId] ON [PatientCustomFieldStrings] ([FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PatientCustomFieldStrings_PatientId_FieldDefinitionId] ON [PatientCustomFieldStrings] ([PatientId], [FieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_PatientCustomFieldStrings_Value] ON [PatientCustomFieldStrings] ([Value]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_AncestryId] ON [Patients] ([AncestryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_AtsiStatusId] ON [Patients] ([AtsiStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_CountryOfBirthId] ON [Patients] ([CountryOfBirthId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_CreatedByUserId] ON [Patients] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_FriendlyId] ON [Patients] ([FriendlyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_GenderId] ON [Patients] ([GenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_Jurisdiction1Id] ON [Patients] ([Jurisdiction1Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_Jurisdiction2Id] ON [Patients] ([Jurisdiction2Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_Jurisdiction3Id] ON [Patients] ([Jurisdiction3Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_Jurisdiction4Id] ON [Patients] ([Jurisdiction4Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_Jurisdiction5Id] ON [Patients] ([Jurisdiction5Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_LanguageSpokenAtHomeId] ON [Patients] ([LanguageSpokenAtHomeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_OccupationId] ON [Patients] ([OccupationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Patients_SexAtBirthId] ON [Patients] ([SexAtBirthId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Permissions_Module_Action] ON [Permissions] ([Module], [Action]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_Category] ON [ReportDefinitions] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_CreatedByUserId] ON [ReportDefinitions] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_EntityType] ON [ReportDefinitions] ([EntityType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportDefinitions_FolderId] ON [ReportDefinitions] ([FolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFields_FieldPath] ON [ReportFields] ([FieldPath]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFields_ReportDefinitionId] ON [ReportFields] ([ReportDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFilters_ReportDefinitionId] ON [ReportFilters] ([ReportDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFolders_ParentFolderId] ON [ReportFolders] ([ParentFolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFolderShares_GroupId] ON [ReportFolderShares] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFolderShares_ReportFolderId] ON [ReportFolderShares] ([ReportFolderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReportFolderShares_UserId] ON [ReportFolderShares] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_CaseId] ON [ReviewQueue] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_CreatedByUserId] ON [ReviewQueue] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_DiseaseId] ON [ReviewQueue] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_GroupKey_Created] ON [ReviewQueue] ([GroupKey], [CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_PatientId] ON [ReviewQueue] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_ReviewedByUserId] ON [ReviewQueue] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_Status_EntityType_Disease_Created] ON [ReviewQueue] ([ReviewStatus], [EntityType], [DiseaseId], [CreatedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_ReviewQueue_TaskId] ON [ReviewQueue] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_RoleDiseaseAccess_CreatedByUserId] ON [RoleDiseaseAccess] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_RoleDiseaseAccess_DiseaseId] ON [RoleDiseaseAccess] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoleDiseaseAccess_RoleId_DiseaseId] ON [RoleDiseaseAccess] ([RoleId], [DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMapping_Config_Order] ON [SurveyFieldMappings] ([ConfigurationType], [ConfigurationId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMapping_Config_Question] ON [SurveyFieldMappings] ([ConfigurationType], [ConfigurationId], [SurveyQuestionName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMapping_IsActive] ON [SurveyFieldMappings] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMapping_TargetField] ON [SurveyFieldMappings] ([TargetFieldPath]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMappings_CreatedByUserId] ON [SurveyFieldMappings] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMappings_LastModifiedByUserId] ON [SurveyFieldMappings] ([LastModifiedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyFieldMappings_TargetSymptomId] ON [SurveyFieldMappings] ([TargetSymptomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyTemplateDiseases_DiseaseId] ON [SurveyTemplateDiseases] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SurveyTemplateDiseases_SurveyTemplateId_DiseaseId] ON [SurveyTemplateDiseases] ([SurveyTemplateId], [DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyTemplates_Category] ON [SurveyTemplates] ([Category]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyTemplates_IsActive] ON [SurveyTemplates] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyTemplates_Name] ON [SurveyTemplates] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_SurveyTemplates_ParentSurveyTemplateId] ON [SurveyTemplates] ([ParentSurveyTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Symptoms_Code] ON [Symptoms] ([Code]) WHERE [Code] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_Symptoms_IsDeleted_IsActive_SortOrder] ON [Symptoms] ([IsDeleted], [IsActive], [SortOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskCallAttempts_AttemptedByUserId] ON [TaskCallAttempts] ([AttemptedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskCallAttempts_TaskId] ON [TaskCallAttempts] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTemplates_IsActive] ON [TaskTemplates] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTemplates_Name] ON [TaskTemplates] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTemplates_SurveyTemplateId] ON [TaskTemplates] ([SurveyTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTemplates_TaskTypeId] ON [TaskTemplates] ([TaskTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTypes_IsActive] ON [TaskTypes] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TaskTypes_Name] ON [TaskTypes] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_TestResults_TestTypeId] ON [TestResults] ([TestTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_UserDiseaseAccess_DiseaseId] ON [UserDiseaseAccess] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_UserDiseaseAccess_ExpiresAt] ON [UserDiseaseAccess] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_UserDiseaseAccess_GrantedByUserId] ON [UserDiseaseAccess] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserDiseaseAccess_UserId_DiseaseId] ON [UserDiseaseAccess] ([UserId], [DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_UserGroups_GroupId] ON [UserGroups] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    CREATE INDEX [IX_UserPermissions_PermissionId] ON [UserPermissions] ([PermissionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304034303_InitialCreate_Clean'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304034303_InitialCreate_Clean', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_ContactsListSimple', 'V') IS NOT NULL DROP VIEW vw_ContactsListSimple;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_CaseContactTasksFlattened', 'V') IS NOT NULL DROP VIEW vw_CaseContactTasksFlattened;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_OutbreakTasksFlattened', 'V') IS NOT NULL DROP VIEW vw_OutbreakTasksFlattened;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_CaseTimelineAll', 'V') IS NOT NULL DROP VIEW vw_CaseTimelineAll;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_ContactTracingMindMapNodes', 'V') IS NOT NULL DROP VIEW vw_ContactTracingMindMapNodes;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    IF OBJECT_ID('vw_ContactTracingMindMapEdges', 'V') IS NOT NULL DROP VIEW vw_ContactTracingMindMapEdges;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_ContactsListSimple AS
    SELECT 
        c.Id AS ContactId,
        c.FriendlyId AS ContactNumber,
        c.DateOfOnset AS DateIdentified,
        c.DateOfOnset AS ContactDateOfOnset,
        p.Id AS PatientId,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS ContactName,
        p.GivenName AS ContactFirstName,
        p.FamilyName AS ContactLastName,
        p.DateOfBirth AS ContactDOB,
        p.MobilePhone AS ContactMobile,
        p.EmailAddress AS ContactEmail,
        p.City AS ContactSuburb,
        p.State AS ContactState,
        d.Name AS DiseaseName,
        'Contact' AS ExposureType,
        '' AS ExposureSourceName,
        0 AS TotalTasks,
        0 AS CompletedTasks,
        GETDATE() AS CreatedAt,
        GETDATE() AS UpdatedAt
    FROM Cases c
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    WHERE c.Type = 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_CaseContactTasksFlattened AS
    SELECT 
        c.Id AS CaseGuid,
        c.FriendlyId AS CaseNumber,
        c.Type AS CaseTypeEnum,
        CASE c.Type WHEN 0 THEN 'Case' WHEN 1 THEN 'Contact' ELSE 'Unknown' END AS CaseType,
        0 AS GenerationNumber,
        c.FriendlyId AS TransmissionChainPath,
        '' AS TransmittedByCase,
        c.DateOfOnset,
        c.DateOfNotification,
        cs.Name AS CaseStatus,
        p.Id AS PatientId,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS PatientName,
        p.GivenName AS PatientFirstName,
        p.FamilyName AS PatientLastName,
        p.DateOfBirth AS PatientDOB,
        DATEDIFF(YEAR, p.DateOfBirth, COALESCE(c.DateOfOnset, GETDATE())) AS AgeAtOnset,
        p.City AS PatientSuburb,
        p.State AS PatientState,
        p.MobilePhone AS PatientMobile,
        p.EmailAddress AS PatientEmail,
        d.Name AS DiseaseName,
        d.Code AS DiseaseCode,
        j1.Name AS Jurisdiction1,
        j2.Name AS Jurisdiction2,
        j3.Name AS Jurisdiction3,
        CAST(NULL AS UNIQUEIDENTIFIER) AS ExposureEventId,
        'Unknown' AS ExposureType,
        '' AS ExposureStatusDisplay,
        CAST(NULL AS DATETIME2) AS ExposureDate,
        '' AS ExposureLocation,
        '' AS ContactClassification,
        '' AS ConfidenceLevel,
        CAST(NULL AS UNIQUEIDENTIFIER) AS TaskId,
        '' AS TaskTitle,
        '' AS TaskType,
        'NotStarted' AS TaskStatus,
        CAST(NULL AS DATETIME2) AS TaskDueDate,
        CAST(NULL AS DATETIME2) AS TaskCompletedDate,
        GETDATE() AS TaskCreatedAt,
        '' AS AssignedToName,
        '' AS AssignedToEmail,
        'User' AS AssignmentType,
        GETDATE() AS CaseCreatedAt,
        GETDATE() AS CaseUpdatedAt
    FROM Cases c
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
    LEFT JOIN Jurisdictions j1 ON c.Jurisdiction1Id = j1.Id
    LEFT JOIN Jurisdictions j2 ON c.Jurisdiction2Id = j2.Id
    LEFT JOIN Jurisdictions j3 ON c.Jurisdiction3Id = j3.Id;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_OutbreakTasksFlattened AS
    SELECT 
        o.Id AS OutbreakId,
        o.Name AS OutbreakName,
        '' AS OutbreakReferenceNumber,
        '' AS DiseaseName,
        CAST(NULL AS UNIQUEIDENTIFIER) AS CaseGuid,
        '' AS CaseNumber,
        '' AS PatientName,
        CAST(NULL AS UNIQUEIDENTIFIER) AS TaskId,
        '' AS TaskTitle,
        '' AS TaskType,
        'NotStarted' AS TaskStatus,
        CAST(NULL AS DATETIME2) AS DueDate,
        CAST(NULL AS DATETIME2) AS CompletedDate,
        '' AS AssignedToName,
        '' AS AssignedToEmail,
        o.StartDate AS OutbreakCreatedAt
    FROM Outbreaks o;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_CaseTimelineAll AS
    SELECT 
        c.Id AS CaseId,
        'CaseCreated' AS EventType,
        GETDATE() AS EventDate,
        CONCAT('Case: ', c.FriendlyId) AS EventDescription,
        '' AS ActorName,
        GETDATE() AS SortDate
    FROM Cases c;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_ContactTracingMindMapNodes AS
    SELECT 
        c.Id AS NodeId,
        c.FriendlyId AS NodeLabel,
        c.Type AS NodeType,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS PersonName,
        d.Name AS DiseaseName,
        cs.Name AS CaseStatus,
        c.DateOfOnset,
        CAST(0 AS BIT) AS IsDeleted
    FROM Cases c
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN

    CREATE VIEW vw_ContactTracingMindMapEdges AS
    SELECT 
        NEWID() AS EdgeId,
        c1.Id AS SourceNodeId,
        c2.Id AS TargetNodeId,
        'Contact' AS EdgeType,
        c2.DateOfOnset AS ExposureDate,
        'Medium' AS ConfidenceLevel
    FROM Cases c1
    CROSS JOIN Cases c2
    WHERE c1.Id <> c2.Id AND c2.Type = 1;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304060957_AddReportingViews'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304060957_AddReportingViews', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_OutbreakTasksFlattened;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_CaseTimelineAll;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_ContactTracingMindMapNodes;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_ContactTracingMindMapEdges;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    DROP VIEW IF EXISTS vw_ContactsListSimple;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_CaseContactTasksFlattened AS

    WITH TransmissionChain AS (
        -- Anchor: Root cases (no source case)
        SELECT 
            c.Id AS CaseId,
            c.FriendlyId AS CaseNumber,
            CAST(NULL AS UNIQUEIDENTIFIER) AS SourceCaseId,
            CAST(NULL AS NVARCHAR(MAX)) AS SourceCaseNumber,
            0 AS TransmissionDepth,
            CAST(c.FriendlyId AS NVARCHAR(MAX)) AS TransmissionChainPath
        FROM Cases c
        WHERE c.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 
              FROM ExposureEvents ee 
              WHERE ee.ExposedCaseId = c.Id 
                AND ee.IsDeleted = 0
                AND ee.ExposureType = 3
          )
        
        UNION ALL
        
        -- Recursive: Cases exposed by other cases
        SELECT
            exposedCase.Id,
            exposedCase.FriendlyId,
            tc.CaseId AS SourceCaseId,
            CAST(tc.CaseNumber AS NVARCHAR(MAX)) AS SourceCaseNumber,
            tc.TransmissionDepth + 1,
            CAST(tc.TransmissionChainPath + ' ? ' + exposedCase.FriendlyId AS NVARCHAR(MAX))
        FROM TransmissionChain tc
        INNER JOIN ExposureEvents ee ON ee.SourceCaseId = tc.CaseId
        INNER JOIN Cases exposedCase ON ee.ExposedCaseId = exposedCase.Id
        WHERE tc.TransmissionDepth < 10
          AND ee.IsDeleted = 0
          AND exposedCase.IsDeleted = 0
          AND ee.ExposureType = 3
    )

    SELECT 
        tc.CaseId AS CaseGuid,
        tc.CaseNumber,
        tc.TransmissionDepth AS GenerationNumber,
        tc.TransmissionChainPath,
        tc.SourceCaseNumber AS TransmittedByCase,
        
        c.Type AS CaseTypeEnum,
        CASE c.Type 
            WHEN 0 THEN 'Case' 
            WHEN 1 THEN 'Contact' 
            ELSE 'Unknown'
        END AS CaseType,
        c.DateOfOnset,
        c.DateOfNotification,
        
        cs.Name AS CaseStatus,
        
        p.FriendlyId AS PatientId,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS PatientName,
        p.GivenName AS PatientFirstName,
        p.FamilyName AS PatientLastName,
        p.DateOfBirth AS PatientDOB,
        DATEDIFF(YEAR, p.DateOfBirth, COALESCE(c.DateOfOnset, GETDATE())) AS AgeAtOnset,
        p.City AS PatientSuburb,
        p.State AS PatientState,
        p.MobilePhone AS PatientMobile,
        p.EmailAddress AS PatientEmail,
        
        d.Name AS DiseaseName,
        d.Code AS DiseaseCode,
        
        j1.Name AS Jurisdiction1,
        j2.Name AS Jurisdiction2,
        j3.Name AS Jurisdiction3,
        
        ee.Id AS ExposureEventId,
        
        CASE ee.ExposureType
            WHEN 0 THEN 'Unknown'
            WHEN 1 THEN 'Event'
            WHEN 2 THEN 'Location'
            WHEN 3 THEN 'Contact'
            WHEN 4 THEN 'Travel'
            WHEN 5 THEN 'Locally Acquired'
            ELSE 'Unknown'
        END AS ExposureType,
        
        CASE ee.ExposureStatus
            WHEN 0 THEN 'Unknown'
            WHEN 1 THEN 'Potential Exposure'
            WHEN 2 THEN 'Under Investigation'
            WHEN 3 THEN 'Confirmed Exposure'
            WHEN 4 THEN 'Ruled Out'
            ELSE 'Unknown'
        END AS ExposureStatusDisplay,
        
        ee.ExposureStartDate,
        ee.ExposureEndDate,
        ee.Description AS ExposureDescription,
        CASE ee.ConfidenceLevel
            WHEN 0 THEN 'Low'
            WHEN 1 THEN 'Medium'
            WHEN 2 THEN 'High'
            ELSE NULL
        END AS ConfidenceLevel,
        
        cc.Name AS ContactClassification,
        
        evt.Id AS EventId,
        evt.Name AS EventName,
        evtType.Name AS EventType,
        evt.StartDateTime AS EventStartDate,
        evt.EndDateTime AS EventEndDate,
        evt.EstimatedAttendees,
        CASE WHEN evt.IsIndoor = 1 THEN 'Indoor' WHEN evt.IsIndoor = 0 THEN 'Outdoor' ELSE 'Unknown' END AS EventSetting,
        evtOrg.Name AS EventOrganizer,
        
        loc.Id AS LocationId,
        loc.Name AS LocationName,
        locType.Name AS LocationType,
        loc.Address AS LocationAddress,
        CASE WHEN loc.IsHighRisk = 1 THEN 'Yes' ELSE 'No' END AS LocationIsHighRisk,
        locOrg.Name AS LocationOrganization,
        
        t.Id AS TaskId,
        CAST(t.Id AS NVARCHAR(50)) AS TaskNumber,
        t.Title AS TaskTitle,
        t.Description AS TaskDescription,
        
        CASE t.Status
            WHEN 0 THEN 'NotStarted'
            WHEN 1 THEN 'InProgress'
            WHEN 2 THEN 'OnHold'
            WHEN 3 THEN 'Completed'
            WHEN 4 THEN 'Cancelled'
            ELSE 'Unknown'
        END AS TaskStatus,
        
        CASE t.Priority
            WHEN 0 THEN 'Low'
            WHEN 1 THEN 'Medium'
            WHEN 2 THEN 'High'
            WHEN 3 THEN 'Urgent'
            ELSE 'Normal'
        END AS TaskPriority,
        
        t.DueDate AS TaskDueDate,
        t.CreatedAt AS TaskCreatedAt,
        t.CompletedAt AS TaskCompletedAt,
        t.CancelledAt AS TaskCancelledAt,
        t.IsInterviewTask,
        
        tt.Name AS TaskType,
        
        CASE t.AssignmentType
            WHEN 0 THEN 'User'
            WHEN 1 THEN 'Group'
            WHEN 2 THEN 'Role'
            ELSE 'User'
        END AS AssignmentType,
        
        u.Email AS AssignedToEmail,
        CONCAT(u.FirstName, ' ', u.LastName) AS AssignedToName,
        
        CASE 
            WHEN t.IsInterviewTask = 1 AND t.SurveyResponseJson IS NOT NULL THEN 'Completed'
            WHEN t.IsInterviewTask = 1 THEN 'Pending'
            ELSE 'Not Interview Task'
        END AS SurveyStatus,
        
        DATEDIFF(DAY, ee.ExposureEndDate, c.DateOfOnset) AS IncubationPeriodDays,
        DATEDIFF(DAY, GETDATE(), t.DueDate) AS DaysUntilTaskDue,
        DATEDIFF(DAY, t.CreatedAt, COALESCE(t.CompletedAt, GETDATE())) AS TaskAgeDays,
        
        CASE 
            WHEN t.Status = 3 THEN 'Complete'
            WHEN t.Status = 4 THEN 'Cancelled'
            WHEN t.DueDate < GETDATE() AND t.Status NOT IN (3, 4) THEN 'Overdue'
            WHEN t.DueDate < DATEADD(DAY, 3, GETDATE()) THEN 'Due Soon'
            WHEN t.Status = 1 THEN 'In Progress'
            ELSE 'On Track'
        END AS TaskDueStatus,
        
        GETDATE() AS CaseCreatedAt,
        GETDATE() AS CaseUpdatedAt

    FROM TransmissionChain tc
    INNER JOIN Cases c ON tc.CaseId = c.Id
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
    LEFT JOIN Jurisdictions j1 ON c.Jurisdiction1Id = j1.Id
    LEFT JOIN Jurisdictions j2 ON c.Jurisdiction2Id = j2.Id
    LEFT JOIN Jurisdictions j3 ON c.Jurisdiction3Id = j3.Id
    LEFT JOIN ExposureEvents ee ON ee.ExposedCaseId = c.Id AND ee.IsDeleted = 0
    LEFT JOIN ContactClassifications cc ON ee.ContactClassificationId = cc.Id
    LEFT JOIN Events evt ON ee.EventId = evt.Id
    LEFT JOIN EventTypes evtType ON evt.EventTypeId = evtType.Id
    LEFT JOIN Organizations evtOrg ON evt.OrganizerOrganizationId = evtOrg.Id
    LEFT JOIN Locations loc ON ee.LocationId = loc.Id OR evt.LocationId = loc.Id
    LEFT JOIN LocationTypes locType ON loc.LocationTypeId = locType.Id
    LEFT JOIN Organizations locOrg ON loc.OrganizationId = locOrg.Id
    LEFT JOIN CaseTasks t ON t.CaseId = c.Id
    LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
    LEFT JOIN AspNetUsers u ON t.AssignedToUserId = u.Id

    WHERE c.IsDeleted = 0 AND p.IsDeleted = 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_OutbreakTasksFlattened AS
    SELECT 
        o.Id AS OutbreakId,
        o.Name AS OutbreakName,
        CAST(o.Id AS NVARCHAR(50)) AS OutbreakReferenceNumber,
        '' AS DiseaseName,
        c.Id AS CaseGuid,
        c.FriendlyId AS CaseNumber,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS PatientName,
        t.Id AS TaskId,
        t.Title AS TaskTitle,
        tt.Name AS TaskType,
        CASE t.Status
            WHEN 0 THEN 'NotStarted'
            WHEN 1 THEN 'InProgress'
            WHEN 2 THEN 'OnHold'
            WHEN 3 THEN 'Completed'
            WHEN 4 THEN 'Cancelled'
            ELSE 'Unknown'
        END AS TaskStatus,
        t.DueDate,
        t.CompletedAt AS CompletedDate,
        CONCAT(u.FirstName, ' ', u.LastName) AS AssignedToName,
        u.Email AS AssignedToEmail,
        o.StartDate AS OutbreakCreatedAt
    FROM Outbreaks o
    LEFT JOIN OutbreakCases oc ON o.Id = oc.OutbreakId
    LEFT JOIN Cases c ON oc.CaseId = c.Id AND c.IsDeleted = 0
    LEFT JOIN Patients p ON c.PatientId = p.Id AND p.IsDeleted = 0
    LEFT JOIN CaseTasks t ON c.Id = t.CaseId
    LEFT JOIN TaskTypes tt ON t.TaskTypeId = tt.Id
    LEFT JOIN AspNetUsers u ON t.AssignedToUserId = u.Id;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_CaseTimelineAll AS
    SELECT 
        c.Id AS CaseId,
        'CaseNotification' AS EventType,
        c.DateOfNotification AS EventDate,
        CONCAT('Case notified: ', c.FriendlyId) AS EventDescription,
        '' AS ActorName,
        COALESCE(c.DateOfNotification, GETDATE()) AS SortDate
    FROM Cases c
    WHERE c.IsDeleted = 0 AND c.DateOfNotification IS NOT NULL

    UNION ALL

    SELECT 
        c.Id AS CaseId,
        'LabResult' AS EventType,
        lr.ResultDate AS EventDate,
        CONCAT('Lab: ', tt.Name, ' - ', tr.Name) AS EventDescription,
        '' AS ActorName,
        lr.ResultDate AS SortDate
    FROM Cases c
    INNER JOIN LabResults lr ON c.Id = lr.CaseId AND lr.IsDeleted = 0
    LEFT JOIN TestTypes tt ON lr.TestTypeId = tt.Id
    LEFT JOIN TestResults tr ON lr.TestResultId = tr.Id
    WHERE c.IsDeleted = 0

    UNION ALL

    SELECT 
        c.Id AS CaseId,
        'TaskCompleted' AS EventType,
        t.CompletedAt AS EventDate,
        CONCAT('Task: ', t.Title) AS EventDescription,
        CONCAT(u.FirstName, ' ', u.LastName) AS ActorName,
        t.CompletedAt AS SortDate
    FROM Cases c
    INNER JOIN CaseTasks t ON c.Id = t.CaseId AND t.Status = 3
    LEFT JOIN AspNetUsers u ON t.CompletedByUserId = u.Id
    WHERE c.IsDeleted = 0 AND t.CompletedAt IS NOT NULL

    UNION ALL

    SELECT 
        c.Id AS CaseId,
        'Note' AS EventType,
        n.CreatedAt AS EventDate,
        CONCAT('Note: ', LEFT(n.Content, 50), CASE WHEN LEN(n.Content) > 50 THEN '...' ELSE '' END) AS EventDescription,
        n.CreatedBy AS ActorName,
        n.CreatedAt AS SortDate
    FROM Cases c
    INNER JOIN Notes n ON c.Id = n.CaseId AND n.IsDeleted = 0
    WHERE c.IsDeleted = 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_ContactTracingMindMapNodes AS
    SELECT 
        c.Id AS NodeId,
        c.FriendlyId AS NodeLabel,
        c.Type AS NodeType,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS PersonName,
        d.Name AS DiseaseName,
        cs.Name AS CaseStatus,
        c.DateOfOnset,
        c.IsDeleted
    FROM Cases c
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    LEFT JOIN CaseStatuses cs ON c.ConfirmationStatusId = cs.Id
    WHERE c.IsDeleted = 0 AND p.IsDeleted = 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_ContactTracingMindMapEdges AS
    SELECT 
        ee.Id AS EdgeId,
        ee.SourceCaseId AS SourceNodeId,
        c.Id AS TargetNodeId,
        CASE ee.ExposureType
            WHEN 0 THEN 'Unknown'
            WHEN 1 THEN 'Event'
            WHEN 2 THEN 'Location'
            WHEN 3 THEN 'Direct Contact'
            WHEN 4 THEN 'Travel'
            WHEN 5 THEN 'Locally Acquired'
            ELSE 'Unknown'
        END AS EdgeType,
        ee.ExposureStartDate AS ExposureDate,
        CASE ee.ConfidenceLevel
            WHEN 0 THEN 'Low'
            WHEN 1 THEN 'Medium'
            WHEN 2 THEN 'High'
            ELSE 'Medium'
        END AS ConfidenceLevel
    FROM ExposureEvents ee
    INNER JOIN Cases c ON ee.ExposedCaseId = c.Id
    WHERE ee.IsDeleted = 0 
      AND c.IsDeleted = 0
      AND ee.SourceCaseId IS NOT NULL;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN

    CREATE VIEW vw_ContactsListSimple AS
    SELECT 
        c.Id AS ContactId,
        c.FriendlyId AS ContactNumber,
        c.DateOfOnset AS DateIdentified,
        c.DateOfOnset AS ContactDateOfOnset,
        
        p.Id AS PatientId,
        CONCAT(p.GivenName, ' ', p.FamilyName) AS ContactName,
        p.GivenName AS ContactFirstName,
        p.FamilyName AS ContactLastName,
        p.DateOfBirth AS ContactDOB,
        p.MobilePhone AS ContactMobile,
        p.EmailAddress AS ContactEmail,
        p.City AS ContactSuburb,
        p.State AS ContactState,
        
        d.Name AS DiseaseName,
        
        CASE 
            WHEN ee.ExposureType = 0 THEN 'Unknown'
            WHEN ee.ExposureType = 1 THEN 'Event'
            WHEN ee.ExposureType = 2 THEN 'Location'
            WHEN ee.ExposureType = 3 THEN 'Contact'
            WHEN ee.ExposureType = 4 THEN 'Travel'
            WHEN ee.ExposureType = 5 THEN 'Locally Acquired'
            ELSE 'Unknown'
        END AS ExposureType,
        
        CASE 
            WHEN ee.ExposureType = 1 THEN evt.Name
            WHEN ee.ExposureType = 2 THEN loc.Name
            WHEN ee.ExposureType = 3 THEN CONCAT(psrc.GivenName, ' ', psrc.FamilyName)
            ELSE NULL
        END AS ExposureSourceName,
        
        (SELECT COUNT(*) FROM CaseTasks ct WHERE ct.CaseId = c.Id) AS TotalTasks,
        (SELECT COUNT(*) FROM CaseTasks ct WHERE ct.CaseId = c.Id AND ct.Status = 3) AS CompletedTasks,
        
        GETDATE() AS CreatedAt,
        GETDATE() AS UpdatedAt
        
    FROM Cases c
    INNER JOIN Patients p ON c.PatientId = p.Id
    LEFT JOIN Diseases d ON c.DiseaseId = d.Id
    LEFT JOIN ExposureEvents ee ON c.Id = ee.ExposedCaseId AND ee.IsDeleted = 0
    LEFT JOIN Events evt ON ee.EventId = evt.Id
    LEFT JOIN Locations loc ON ee.LocationId = loc.Id
    LEFT JOIN Cases csrc ON ee.SourceCaseId = csrc.Id
    LEFT JOIN Patients psrc ON csrc.PatientId = psrc.Id AND psrc.IsDeleted = 0
    WHERE c.Type = 1 AND c.IsDeleted = 0 AND p.IsDeleted = 0;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260304072216_EnhanceViewsWithRealData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260304072216_EnhanceViewsWithRealData', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035807_SurveyFamilyReferences'
)
BEGIN

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
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035807_SurveyFamilyReferences'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260318035807_SurveyFamilyReferences', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329070047_AddSurveySubmissionLog'
)
BEGIN
    CREATE TABLE [SurveySubmissionLogs] (
        [Id] int NOT NULL IDENTITY,
        [TaskId] uniqueidentifier NULL,
        [CaseId] uniqueidentifier NULL,
        [PatientName] nvarchar(200) NULL,
        [CaseReference] nvarchar(50) NULL,
        [DiseaseName] nvarchar(200) NULL,
        [SurveyName] nvarchar(200) NULL,
        [TaskName] nvarchar(200) NULL,
        [SubmittedAt] datetime2 NOT NULL,
        [SubmittedByUserId] nvarchar(450) NULL,
        [SubmittedByName] nvarchar(200) NULL,
        [Outcome] int NOT NULL,
        [FieldsSavedAutomatically] int NOT NULL,
        [FieldsSentForReview] int NOT NULL,
        [FieldsRequiringApproval] int NOT NULL,
        [FieldsSkipped] int NOT NULL,
        [FieldsWithErrors] int NOT NULL,
        [TotalMappingsConfigured] int NOT NULL,
        [IssuesSummary] nvarchar(2000) NULL,
        [MappingDetailJson] nvarchar(max) NULL,
        CONSTRAINT [PK_SurveySubmissionLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SurveySubmissionLogs_CaseTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [CaseTasks] ([Id]),
        CONSTRAINT [FK_SurveySubmissionLogs_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329070047_AddSurveySubmissionLog'
)
BEGIN
    CREATE INDEX [IX_SurveySubmissionLogs_CaseId] ON [SurveySubmissionLogs] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329070047_AddSurveySubmissionLog'
)
BEGIN
    CREATE INDEX [IX_SurveySubmissionLogs_TaskId] ON [SurveySubmissionLogs] ([TaskId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260329070047_AddSurveySubmissionLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260329070047_AddSurveySubmissionLog', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331092405_AddReviewQueueLinkToSubmissionLog'
)
BEGIN
    ALTER TABLE [SurveySubmissionLogs] ADD [ReviewQueueItemId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331092405_AddReviewQueueLinkToSubmissionLog'
)
BEGIN
    CREATE INDEX [IX_SurveySubmissionLogs_ReviewQueueItemId] ON [SurveySubmissionLogs] ([ReviewQueueItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331092405_AddReviewQueueLinkToSubmissionLog'
)
BEGIN
    ALTER TABLE [SurveySubmissionLogs] ADD CONSTRAINT [FK_SurveySubmissionLogs_ReviewQueue_ReviewQueueItemId] FOREIGN KEY ([ReviewQueueItemId]) REFERENCES [ReviewQueue] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331092405_AddReviewQueueLinkToSubmissionLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331092405_AddReviewQueueLinkToSubmissionLog', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Diseases] ADD [AddressReviewWindowAfterDays] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Diseases] ADD [AddressReviewWindowBeforeDays] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Diseases] ADD [CheckJurisdictionCrossing] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Diseases] ADD [JurisdictionFieldsToCheck] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseAddressCapturedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseAddressLine] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseAddressManualOverride] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseCity] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseLatitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseLongitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CasePostalCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseState] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331215823_AddInheritAddressSettingsFromParent'
)
BEGIN
    ALTER TABLE [Diseases] ADD [InheritAddressSettingsFromParent] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260331215823_AddInheritAddressSettingsFromParent'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331215823_AddInheritAddressSettingsFromParent', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Patients]') AND [c].[name] = N'State');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Patients] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Patients] DROP COLUMN [State];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Cases]') AND [c].[name] = N'CaseState');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Cases] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Cases] DROP COLUMN [CaseState];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    ALTER TABLE [Patients] ADD [StateId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    ALTER TABLE [Cases] ADD [CaseStateId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    CREATE TABLE [States] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_States] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    CREATE INDEX [IX_Patients_StateId] ON [Patients] ([StateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    CREATE INDEX [IX_Cases_CaseStateId] ON [Cases] ([CaseStateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    ALTER TABLE [Cases] ADD CONSTRAINT [FK_Cases_States_CaseStateId] FOREIGN KEY ([CaseStateId]) REFERENCES [States] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    ALTER TABLE [Patients] ADD CONSTRAINT [FK_Patients_States_StateId] FOREIGN KEY ([StateId]) REFERENCES [States] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260401210029_FixStateModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260401210029_FixStateModel', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405121236_AddDynamicDatesToReportFilter'
)
BEGIN
    ALTER TABLE [ReportFilters] ADD [DynamicDateOffset] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405121236_AddDynamicDatesToReportFilter'
)
BEGIN
    ALTER TABLE [ReportFilters] ADD [DynamicDateOffsetUnit] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405121236_AddDynamicDatesToReportFilter'
)
BEGIN
    ALTER TABLE [ReportFilters] ADD [DynamicDateType] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405121236_AddDynamicDatesToReportFilter'
)
BEGIN
    ALTER TABLE [ReportFilters] ADD [IsDynamicDate] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260405121236_AddDynamicDatesToReportFilter'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260405121236_AddDynamicDatesToReportFilter', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [IsSterileSite] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE TABLE [Pathogens] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ShortName] nvarchar(50) NULL,
        [LOINCCode] nvarchar(20) NULL,
        [LOINCDisplayName] nvarchar(500) NULL,
        [Description] nvarchar(1000) NULL,
        [DiseaseId] uniqueidentifier NULL,
        [Category] int NOT NULL,
        [ResultType] int NOT NULL,
        [DefaultUnit] nvarchar(50) NULL,
        [DefaultReferenceRangeLow] decimal(18,2) NULL,
        [DefaultReferenceRangeHigh] decimal(18,2) NULL,
        [DisplayOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Pathogens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Pathogens_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE TABLE [TestMethods] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExportCode] nvarchar(50) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TestMethods] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE TABLE [LabResultMarkers] (
        [Id] uniqueidentifier NOT NULL,
        [LabResultId] uniqueidentifier NOT NULL,
        [PathogenId] uniqueidentifier NOT NULL,
        [TestMethodId] int NULL,
        [QualitativeResult] nvarchar(50) NULL,
        [QuantitativeValue] decimal(18,2) NULL,
        [QuantitativeUnit] nvarchar(50) NULL,
        [ReferenceRangeLow] decimal(18,2) NULL,
        [ReferenceRangeHigh] decimal(18,2) NULL,
        [InterpretationFlag] nvarchar(20) NULL,
        [LOINCCode] nvarchar(20) NULL,
        [Notes] nvarchar(1000) NULL,
        [DisplayOrder] int NOT NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_LabResultMarkers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabResultMarkers_LabResults_LabResultId] FOREIGN KEY ([LabResultId]) REFERENCES [LabResults] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LabResultMarkers_Pathogens_PathogenId] FOREIGN KEY ([PathogenId]) REFERENCES [Pathogens] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LabResultMarkers_TestMethods_TestMethodId] FOREIGN KEY ([TestMethodId]) REFERENCES [TestMethods] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkers_LabResultId] ON [LabResultMarkers] ([LabResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkers_LOINCCode] ON [LabResultMarkers] ([LOINCCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkers_PathogenId] ON [LabResultMarkers] ([PathogenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkers_TestMethodId] ON [LabResultMarkers] ([TestMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_Pathogens_DiseaseId_DisplayOrder] ON [Pathogens] ([DiseaseId], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_Pathogens_IsActive] ON [Pathogens] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Pathogens_LOINCCode] ON [Pathogens] ([LOINCCode]) WHERE [LOINCCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_TestMethods_IsActive_DisplayOrder] ON [TestMethods] ([IsActive], [DisplayOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    CREATE INDEX [IX_TestMethods_Name] ON [TestMethods] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427063953_AddPathogensAndLabResultMarkers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427063953_AddPathogensAndLabResultMarkers', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [LabResults] DROP CONSTRAINT [FK_LabResults_TestResults_TestResultId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [LabResults] DROP CONSTRAINT [FK_LabResults_TestTypes_TestTypeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestResults] DROP CONSTRAINT [FK_TestResults_TestTypes_TestTypeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestTypes] DROP CONSTRAINT [PK_TestTypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestResults] DROP CONSTRAINT [PK_TestResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    EXEC sp_rename N'[TestTypes]', N'TestType', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    EXEC sp_rename N'[TestResults]', N'TestResult', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    EXEC sp_rename N'[TestResult].[IX_TestResults_TestTypeId]', N'IX_TestResult_TestTypeId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestType] ADD CONSTRAINT [PK_TestType] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestResult] ADD CONSTRAINT [PK_TestResult] PRIMARY KEY ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_TestResult_TestResultId] FOREIGN KEY ([TestResultId]) REFERENCES [TestResult] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_TestType_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestType] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    ALTER TABLE [TestResult] ADD CONSTRAINT [FK_TestResult_TestType_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestType] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095052_RemoveLegacyTestSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427095052_RemoveLegacyTestSystem', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    ALTER TABLE [LabResults] DROP CONSTRAINT [FK_LabResults_TestResult_TestResultId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    ALTER TABLE [LabResults] DROP CONSTRAINT [FK_LabResults_TestType_TestTypeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    ALTER TABLE [TestResult] DROP CONSTRAINT [FK_TestResult_TestType_TestTypeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DROP INDEX [IX_LabResults_TestResultId] ON [LabResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DROP INDEX [IX_LabResults_TestTypeId] ON [LabResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LabResults]') AND [c].[name] = N'TestResultId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [LabResults] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [LabResults] DROP COLUMN [TestResultId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LabResults]') AND [c].[name] = N'TestTypeId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [LabResults] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [LabResults] DROP COLUMN [TestTypeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LabResults]') AND [c].[name] = N'QuantitativeResult');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [LabResults] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [LabResults] DROP COLUMN [QuantitativeResult];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DROP TABLE [TestResult];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    DROP TABLE [TestType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427095151_DropTestTypesAndResultsTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427095151_DropTestTypesAndResultsTables', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427113424_SyncModelAfterObsoleteRemoval'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427113424_SyncModelAfterObsoleteRemoval', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [BodySite] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [CollectionMethod] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [Hl7Code] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [LoincSystemCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [SnomedCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    ALTER TABLE [SpecimenTypes] ADD [SnomedDisplay] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260427215200_AddSnomedToSpecimenType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260427215200_AddSnomedToSpecimenType', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    ALTER TABLE [Cases] ADD [ConfirmationStatusClassifiedBy] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    ALTER TABLE [Cases] ADD [ConfirmationStatusClassifiedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    ALTER TABLE [Cases] ADD [IsAutoClassified] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    ALTER TABLE [Cases] ADD [LastEvaluatedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    ALTER TABLE [Cases] ADD [LastEvaluatedDefinitionIds] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE TABLE [CaseDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [ApplyToChildDiseases] bit NOT NULL,
        [ConfirmationStatusId] int NOT NULL,
        [Status] int NOT NULL,
        [DateActiveFrom] datetime2 NOT NULL,
        [DateActiveTo] datetime2 NULL,
        [AllowAutoClassification] bit NOT NULL,
        [CreateReviewQueueOnChange] bit NOT NULL,
        [CreateReviewQueueOnSuggestion] bit NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedBy] nvarchar(450) NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_CaseDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseDefinitions_CaseStatuses_ConfirmationStatusId] FOREIGN KEY ([ConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseDefinitions_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE TABLE [CaseClassificationHistory] (
        [Id] int NOT NULL IDENTITY,
        [CaseId] uniqueidentifier NOT NULL,
        [FromConfirmationStatusId] int NULL,
        [ToConfirmationStatusId] int NOT NULL,
        [AppliedDefinitionId] int NULL,
        [ClassifiedDate] datetime2 NOT NULL,
        [ClassifiedByUserId] nvarchar(450) NULL,
        [IsAutoClassified] bit NOT NULL,
        [Rationale] nvarchar(max) NULL,
        [MetCriteriaJson] nvarchar(max) NULL,
        [IsCurrent] bit NOT NULL,
        CONSTRAINT [PK_CaseClassificationHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseClassificationHistory_CaseDefinitions_AppliedDefinitionId] FOREIGN KEY ([AppliedDefinitionId]) REFERENCES [CaseDefinitions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseClassificationHistory_CaseStatuses_FromConfirmationStatusId] FOREIGN KEY ([FromConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseClassificationHistory_CaseStatuses_ToConfirmationStatusId] FOREIGN KEY ([ToConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseClassificationHistory_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE TABLE [CaseDefinitionCriteria] (
        [Id] int NOT NULL IDENTITY,
        [CaseDefinitionId] int NOT NULL,
        [ParentCriteriaId] int NULL,
        [CriterionType] int NOT NULL,
        [LogicalOperator] int NOT NULL,
        [GroupNumber] int NOT NULL,
        [FieldPath] nvarchar(200) NOT NULL,
        [Operator] int NOT NULL,
        [ValueJson] nvarchar(max) NOT NULL,
        [DisplayText] nvarchar(500) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK_CaseDefinitionCriteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseDefinitionCriteria_CaseDefinitionCriteria_ParentCriteriaId] FOREIGN KEY ([ParentCriteriaId]) REFERENCES [CaseDefinitionCriteria] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CaseDefinitionCriteria_CaseDefinitions_CaseDefinitionId] FOREIGN KEY ([CaseDefinitionId]) REFERENCES [CaseDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_AppliedDefinitionId] ON [CaseClassificationHistory] ([AppliedDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_CaseId] ON [CaseClassificationHistory] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_CaseId_IsCurrent] ON [CaseClassificationHistory] ([CaseId], [IsCurrent]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_ClassifiedDate] ON [CaseClassificationHistory] ([ClassifiedDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_FromConfirmationStatusId] ON [CaseClassificationHistory] ([FromConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_IsAutoClassified] ON [CaseClassificationHistory] ([IsAutoClassified]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseClassificationHistory_ToConfirmationStatusId] ON [CaseClassificationHistory] ([ToConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CaseDefinitionId] ON [CaseDefinitionCriteria] ([CaseDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CaseDefinitionId_GroupNumber] ON [CaseDefinitionCriteria] ([CaseDefinitionId], [GroupNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_ParentCriteriaId] ON [CaseDefinitionCriteria] ([ParentCriteriaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitions_ConfirmationStatusId] ON [CaseDefinitions] ([ConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitions_DateActiveFrom] ON [CaseDefinitions] ([DateActiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitions_DateActiveTo] ON [CaseDefinitions] ([DateActiveTo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitions_DiseaseId_ConfirmationStatusId] ON [CaseDefinitions] ([DiseaseId], [ConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitions_Status] ON [CaseDefinitions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428000829_AddCaseDefinitionSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428000829_AddCaseDefinitionSystem', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] DROP CONSTRAINT [FK_CaseClassificationHistory_CaseDefinitions_AppliedDefinitionId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    EXEC sp_rename N'[CaseClassificationHistory].[MetCriteriaJson]', N'CriteriaResultJson', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    EXEC sp_rename N'[CaseClassificationHistory].[AppliedDefinitionId]', N'CaseDefinitionId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    EXEC sp_rename N'[CaseClassificationHistory].[IX_CaseClassificationHistory_AppliedDefinitionId]', N'IX_CaseClassificationHistory_CaseDefinitionId', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseClassificationHistory]') AND [c].[name] = N'ClassifiedDate');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [CaseClassificationHistory] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [CaseClassificationHistory] ALTER COLUMN [ClassifiedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] ADD [EvaluationDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] ADD [IsMatch] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] ADD [RecommendedAction] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] ADD [WasApplied] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    ALTER TABLE [CaseClassificationHistory] ADD CONSTRAINT [FK_CaseClassificationHistory_CaseDefinitions_CaseDefinitionId] FOREIGN KEY ([CaseDefinitionId]) REFERENCES [CaseDefinitions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260428130413_AddCaseDefinitionEvaluationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260428130413_AddCaseDefinitionEvaluationFields', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429000755_AddAutoEvaluationAndManualOverride'
)
BEGIN
    ALTER TABLE [Cases] ADD [ConfirmationStatusManualOverride] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429000755_AddAutoEvaluationAndManualOverride'
)
BEGIN
    ALTER TABLE [Cases] ADD [ConfirmationStatusManualOverrideByUserId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429000755_AddAutoEvaluationAndManualOverride'
)
BEGIN
    ALTER TABLE [Cases] ADD [ConfirmationStatusManualOverrideDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429000755_AddAutoEvaluationAndManualOverride'
)
BEGIN
    ALTER TABLE [CaseDefinitions] ADD [EnableAutoEvaluation] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429000755_AddAutoEvaluationAndManualOverride'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429000755_AddAutoEvaluationAndManualOverride', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [DiseaseReinfectionRules] (
        [Id] uniqueidentifier NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [RuleType] int NOT NULL,
        [ReinfectionWindowDays] int NULL,
        [IsChronic] bit NOT NULL,
        [AlwaysCreateNewCase] bit NOT NULL,
        [Description] nvarchar(2000) NULL,
        [CaseMatchingStrategy] int NOT NULL,
        [MatchOnTestType] bit NOT NULL,
        [MatchOnResultType] bit NOT NULL,
        [RequireConfirmationForNewCase] bit NOT NULL,
        [NotificationMessage] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_DiseaseReinfectionRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DiseaseReinfectionRules_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [HL7Configurations] (
        [Id] uniqueidentifier NOT NULL,
        [ConfigurationName] nvarchar(200) NOT NULL,
        [SendingFacility] nvarchar(200) NULL,
        [SendingApplication] nvarchar(200) NULL,
        [FileDropPath] nvarchar(1000) NULL,
        [FilePattern] nvarchar(100) NOT NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [CharacterEncoding] nvarchar(50) NOT NULL,
        [DefaultLaboratoryId] uniqueidentifier NULL,
        [AutoCreateOrganizations] bit NOT NULL,
        [PatientMatchingStrategy] int NOT NULL,
        [AutoCreatePatients] bit NOT NULL,
        [AutoCreateCases] bit NOT NULL,
        [DuplicateDetectionWindowHours] int NOT NULL,
        [DuplicateDetectionStrategy] int NOT NULL,
        [FieldMappingConfig] nvarchar(max) NULL,
        [ProcessOnReceipt] bit NOT NULL,
        [ArchiveProcessedFiles] bit NOT NULL,
        [ArchivePath] nvarchar(1000) NULL,
        [DeleteAfterArchive] bit NOT NULL,
        [SendNotificationsOnError] bit NOT NULL,
        [NotificationEmailAddresses] nvarchar(1000) NULL,
        [DefaultDateFormat] nvarchar(50) NULL,
        [TimezoneOffset] nvarchar(20) NULL,
        [RequirePatientIdentifier] bit NOT NULL,
        [RequireSpecimenCollectionDate] bit NOT NULL,
        [RequireResultDate] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_HL7Configurations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7Configurations_Organizations_DefaultLaboratoryId] FOREIGN KEY ([DefaultLaboratoryId]) REFERENCES [Organizations] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [HL7Messages] (
        [Id] uniqueidentifier NOT NULL,
        [MessageControlId] nvarchar(100) NOT NULL,
        [MessageType] nvarchar(50) NOT NULL,
        [MessageDateTime] datetime2 NOT NULL,
        [SendingFacility] nvarchar(200) NULL,
        [SendingApplication] nvarchar(200) NULL,
        [ReceivingFacility] nvarchar(200) NULL,
        [ReceivingApplication] nvarchar(200) NULL,
        [HL7Version] nvarchar(20) NULL,
        [RawMessage] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(1000) NULL,
        [FileName] nvarchar(500) NULL,
        [FileSizeBytes] bigint NULL,
        [Status] int NOT NULL,
        [ErrorMessage] nvarchar(4000) NULL,
        [ProcessingNotes] nvarchar(max) NULL,
        [ReceivedAt] datetime2 NOT NULL,
        [ParsedAt] datetime2 NULL,
        [ProcessedAt] datetime2 NULL,
        [ProcessedByUserId] nvarchar(450) NULL,
        [PatientId] uniqueidentifier NULL,
        [CaseId] uniqueidentifier NULL,
        [LabResultId] uniqueidentifier NULL,
        [LaboratoryOrganizationId] uniqueidentifier NULL,
        [OrderingProviderOrganizationId] uniqueidentifier NULL,
        [ConfigurationId] uniqueidentifier NULL,
        [IsDuplicate] bit NOT NULL,
        [DuplicateOfMessageId] uniqueidentifier NULL,
        [DuplicateDetectionMethod] nvarchar(200) NULL,
        [RequiresManualReview] bit NOT NULL,
        [ManualReviewCompleted] bit NOT NULL,
        [ManualReviewByUserId] nvarchar(450) NULL,
        [ManualReviewDate] datetime2 NULL,
        [ManualReviewNotes] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL,
        [DeletedByUserId] nvarchar(max) NULL,
        CONSTRAINT [PK_HL7Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7Messages_AspNetUsers_ManualReviewByUserId] FOREIGN KEY ([ManualReviewByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_HL7Messages_AspNetUsers_ProcessedByUserId] FOREIGN KEY ([ProcessedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_HL7Messages_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases] ([Id]),
        CONSTRAINT [FK_HL7Messages_HL7Configurations_ConfigurationId] FOREIGN KEY ([ConfigurationId]) REFERENCES [HL7Configurations] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_HL7Messages_HL7Messages_DuplicateOfMessageId] FOREIGN KEY ([DuplicateOfMessageId]) REFERENCES [HL7Messages] ([Id]),
        CONSTRAINT [FK_HL7Messages_LabResults_LabResultId] FOREIGN KEY ([LabResultId]) REFERENCES [LabResults] ([Id]),
        CONSTRAINT [FK_HL7Messages_Organizations_LaboratoryOrganizationId] FOREIGN KEY ([LaboratoryOrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_HL7Messages_Organizations_OrderingProviderOrganizationId] FOREIGN KEY ([OrderingProviderOrganizationId]) REFERENCES [Organizations] ([Id]),
        CONSTRAINT [FK_HL7Messages_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [HL7MessageSegments] (
        [Id] uniqueidentifier NOT NULL,
        [HL7MessageId] uniqueidentifier NOT NULL,
        [SegmentType] nvarchar(10) NOT NULL,
        [SequenceNumber] int NOT NULL,
        [SetId] int NULL,
        [RawSegment] nvarchar(4000) NOT NULL,
        [IsParsed] bit NOT NULL,
        [ParsedData] nvarchar(max) NULL,
        [FieldCount] int NULL,
        [ErrorDetails] nvarchar(2000) NULL,
        [HasIssues] bit NOT NULL,
        [ParsedAt] datetime2 NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_HL7MessageSegments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7MessageSegments_HL7Messages_HL7MessageId] FOREIGN KEY ([HL7MessageId]) REFERENCES [HL7Messages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [HL7FieldMappings] (
        [Id] uniqueidentifier NOT NULL,
        [ConfigurationId] uniqueidentifier NOT NULL,
        [SegmentType] nvarchar(10) NOT NULL,
        [FieldPath] nvarchar(100) NOT NULL,
        [FieldName] nvarchar(200) NULL,
        [TargetEntity] nvarchar(100) NOT NULL,
        [TargetProperty] nvarchar(100) NOT NULL,
        [MappingType] int NOT NULL,
        [TransformationRule] nvarchar(500) NULL,
        [LookupTable] nvarchar(200) NULL,
        [CodeMappingJson] nvarchar(max) NULL,
        [DefaultValue] nvarchar(500) NULL,
        [IsRequired] bit NOT NULL,
        [ValidationRegex] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [ExampleHL7Value] nvarchar(500) NULL,
        [ExampleMappedValue] nvarchar(500) NULL,
        [TimesUsed] int NOT NULL,
        [TimesFailed] int NOT NULL,
        [LastUsedAt] datetime2 NULL,
        [CreatedFromIssueId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_HL7FieldMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7FieldMappings_HL7Configurations_ConfigurationId] FOREIGN KEY ([ConfigurationId]) REFERENCES [HL7Configurations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE TABLE [HL7ParsingIssues] (
        [Id] uniqueidentifier NOT NULL,
        [HL7MessageId] uniqueidentifier NOT NULL,
        [MessageSegmentId] uniqueidentifier NULL,
        [SegmentType] nvarchar(10) NOT NULL,
        [FieldPath] nvarchar(100) NULL,
        [FieldName] nvarchar(200) NULL,
        [IssueType] int NOT NULL,
        [Severity] int NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [RawValue] nvarchar(1000) NULL,
        [ExpectedFormat] nvarchar(500) NULL,
        [SuggestedMapping] nvarchar(500) NULL,
        [IsResolved] bit NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [ResolvedByUserId] nvarchar(450) NULL,
        [ResolutionNotes] nvarchar(2000) NULL,
        [FieldMappingId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IgnoreFutureOccurrences] bit NOT NULL,
        CONSTRAINT [PK_HL7ParsingIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7ParsingIssues_AspNetUsers_ResolvedByUserId] FOREIGN KEY ([ResolvedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_HL7ParsingIssues_HL7FieldMappings_FieldMappingId] FOREIGN KEY ([FieldMappingId]) REFERENCES [HL7FieldMappings] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_HL7ParsingIssues_HL7MessageSegments_MessageSegmentId] FOREIGN KEY ([MessageSegmentId]) REFERENCES [HL7MessageSegments] ([Id]),
        CONSTRAINT [FK_HL7ParsingIssues_HL7Messages_HL7MessageId] FOREIGN KEY ([HL7MessageId]) REFERENCES [HL7Messages] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_DiseaseReinfectionRules_DiseaseId] ON [DiseaseReinfectionRules] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_DiseaseReinfectionRules_IsActive] ON [DiseaseReinfectionRules] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Configurations_DefaultLaboratoryId] ON [HL7Configurations] ([DefaultLaboratoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Configurations_IsActive] ON [HL7Configurations] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Configurations_SendingFacility] ON [HL7Configurations] ([SendingFacility]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7FieldMappings_ConfigurationId_SegmentType_FieldPath] ON [HL7FieldMappings] ([ConfigurationId], [SegmentType], [FieldPath]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7FieldMappings_CreatedFromIssueId] ON [HL7FieldMappings] ([CreatedFromIssueId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7FieldMappings_IsActive_Priority] ON [HL7FieldMappings] ([IsActive], [Priority]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_CaseId] ON [HL7Messages] ([CaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_ConfigurationId] ON [HL7Messages] ([ConfigurationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_DuplicateOfMessageId] ON [HL7Messages] ([DuplicateOfMessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_IsDuplicate] ON [HL7Messages] ([IsDuplicate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_LaboratoryOrganizationId] ON [HL7Messages] ([LaboratoryOrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_LabResultId] ON [HL7Messages] ([LabResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_ManualReviewByUserId] ON [HL7Messages] ([ManualReviewByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_MessageControlId] ON [HL7Messages] ([MessageControlId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_OrderingProviderOrganizationId] ON [HL7Messages] ([OrderingProviderOrganizationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_PatientId] ON [HL7Messages] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_ProcessedByUserId] ON [HL7Messages] ([ProcessedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_ReceivedAt] ON [HL7Messages] ([ReceivedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_RequiresManualReview] ON [HL7Messages] ([RequiresManualReview]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_SendingFacility_MessageControlId] ON [HL7Messages] ([SendingFacility], [MessageControlId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7Messages_Status] ON [HL7Messages] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7MessageSegments_HL7MessageId_SegmentType] ON [HL7MessageSegments] ([HL7MessageId], [SegmentType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7MessageSegments_HL7MessageId_SequenceNumber] ON [HL7MessageSegments] ([HL7MessageId], [SequenceNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_CreatedAt] ON [HL7ParsingIssues] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_FieldMappingId] ON [HL7ParsingIssues] ([FieldMappingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_HL7MessageId] ON [HL7ParsingIssues] ([HL7MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_IsResolved_IssueType] ON [HL7ParsingIssues] ([IsResolved], [IssueType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_MessageSegmentId] ON [HL7ParsingIssues] ([MessageSegmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    CREATE INDEX [IX_HL7ParsingIssues_ResolvedByUserId] ON [HL7ParsingIssues] ([ResolvedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    ALTER TABLE [HL7FieldMappings] ADD CONSTRAINT [FK_HL7FieldMappings_HL7ParsingIssues_CreatedFromIssueId] FOREIGN KEY ([CreatedFromIssueId]) REFERENCES [HL7ParsingIssues] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429022714_AddHL7IntakeSystem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429022714_AddHL7IntakeSystem', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LabResults]') AND [c].[name] = N'CaseId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [LabResults] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [LabResults] ALTER COLUMN [CaseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    ALTER TABLE [LabResults] ADD [PatientId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    ALTER TABLE [LabResultMarkers] ADD [ResultFinalizedDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    ALTER TABLE [LabResultMarkers] ADD [ResultStatus] nvarchar(10) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    ALTER TABLE [LabResultMarkers] ADD [TestCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE TABLE [LabResultMarkerHistories] (
        [Id] uniqueidentifier NOT NULL,
        [LabResultMarkerId] uniqueidentifier NOT NULL,
        [HL7MessageId] uniqueidentifier NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangeType] int NOT NULL,
        [PreviousQualitativeValue] nvarchar(1000) NULL,
        [PreviousQuantitativeValue] decimal(18,2) NULL,
        [PreviousResultStatus] nvarchar(10) NULL,
        [PreviousAbnormalFlag] nvarchar(10) NULL,
        [NewQualitativeValue] nvarchar(1000) NULL,
        [NewQuantitativeValue] decimal(18,2) NULL,
        [NewResultStatus] nvarchar(10) NULL,
        [NewAbnormalFlag] nvarchar(10) NULL,
        [ChangeReason] nvarchar(500) NULL,
        [ChangedBySystem] bit NOT NULL,
        [ChangedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_LabResultMarkerHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabResultMarkerHistories_AspNetUsers_ChangedByUserId] FOREIGN KEY ([ChangedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_LabResultMarkerHistories_HL7Messages_HL7MessageId] FOREIGN KEY ([HL7MessageId]) REFERENCES [HL7Messages] ([Id]),
        CONSTRAINT [FK_LabResultMarkerHistories_LabResultMarkers_LabResultMarkerId] FOREIGN KEY ([LabResultMarkerId]) REFERENCES [LabResultMarkers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResults_PatientId] ON [LabResults] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkerHistories_ChangedAt] ON [LabResultMarkerHistories] ([ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkerHistories_ChangedByUserId] ON [LabResultMarkerHistories] ([ChangedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkerHistories_HL7MessageId] ON [LabResultMarkerHistories] ([HL7MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkerHistories_LabResultMarkerId] ON [LabResultMarkerHistories] ([LabResultMarkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkerHistories_LabResultMarkerId_ChangedAt] ON [LabResultMarkerHistories] ([LabResultMarkerId], [ChangedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429055211_AddLabResultMarkerHistory_And_UpdateLabResult', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429091544_AddHL7CustomFieldMapping'
)
BEGIN
    CREATE TABLE [HL7CustomFieldMappings] (
        [Id] int NOT NULL IDENTITY,
        [DiseaseId] uniqueidentifier NOT NULL,
        [HL7TestCode] nvarchar(100) NOT NULL,
        [TestCodeDescription] nvarchar(200) NULL,
        [CustomFieldDefinitionId] int NOT NULL,
        [ExtractQualitativeResult] bit NOT NULL,
        [ExtractQuantitativeResult] bit NOT NULL,
        [ValueTransformation] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [Priority] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_HL7CustomFieldMappings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7CustomFieldMappings_CustomFieldDefinitions_CustomFieldDefinitionId] FOREIGN KEY ([CustomFieldDefinitionId]) REFERENCES [CustomFieldDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HL7CustomFieldMappings_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429091544_AddHL7CustomFieldMapping'
)
BEGIN
    CREATE INDEX [IX_HL7CustomFieldMappings_CustomFieldDefinitionId] ON [HL7CustomFieldMappings] ([CustomFieldDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429091544_AddHL7CustomFieldMapping'
)
BEGIN
    CREATE INDEX [IX_HL7CustomFieldMappings_DiseaseId] ON [HL7CustomFieldMappings] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429091544_AddHL7CustomFieldMapping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429091544_AddHL7CustomFieldMapping', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429121339_Fix_PatientFriendlyId_UniqueIndex_ExcludeSoftDeleted'
)
BEGIN
    DROP INDEX [IX_Patients_FriendlyId] ON [Patients];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429121339_Fix_PatientFriendlyId_UniqueIndex_ExcludeSoftDeleted'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Patients_FriendlyId] ON [Patients] ([FriendlyId]) WHERE [IsDeleted] = 0 AND [FriendlyId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429121339_Fix_PatientFriendlyId_UniqueIndex_ExcludeSoftDeleted'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429121339_Fix_PatientFriendlyId_UniqueIndex_ExcludeSoftDeleted', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260430001501_MakeLabResultMarkerPathogenIdNullable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260430001501_MakeLabResultMarkerPathogenIdNullable', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502125920_AddStandardCodeAndCodingSystemToTestMethod'
)
BEGIN
    ALTER TABLE [TestMethods] ADD [CodingSystem] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502125920_AddStandardCodeAndCodingSystemToTestMethod'
)
BEGIN
    ALTER TABLE [TestMethods] ADD [StandardCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260502125920_AddStandardCodeAndCodingSystemToTestMethod'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260502125920_AddStandardCodeAndCodingSystemToTestMethod', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CaseInsensitive] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [IgnorePunctuation] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [MatchingStrategy] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [NormalizeWhitespace] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [NormalizedValue] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [ResultNormalizationMode] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503034854_AddCodeFieldsToLookupTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503034854_AddCodeFieldsToLookupTables', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TestMethods]') AND [c].[name] = N'CodingSystem');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [TestMethods] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [TestMethods] DROP COLUMN [CodingSystem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TestMethods]') AND [c].[name] = N'StandardCode');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [TestMethods] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [TestMethods] DROP COLUMN [StandardCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'CaseInsensitive');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [CaseInsensitive];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'IgnorePunctuation');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [IgnorePunctuation];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'MatchingStrategy');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [MatchingStrategy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'NormalizeWhitespace');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [NormalizeWhitespace];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'NormalizedValue');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [NormalizedValue];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'ResultNormalizationMode');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [CaseDefinitionCriteria] DROP COLUMN [ResultNormalizationMode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    EXEC sp_rename N'[LabResultMarkers].[QualitativeResult]', N'QualitativeResultText', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [TestMethods] ADD [LoincMethodCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [TestMethods] ADD [SnomedCode] nvarchar(20) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [TestMethods] ADD [SnomedDisplay] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResults] ADD [TestResultId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResults] ADD [TestTypeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResultMarkers] ADD [TestResultId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE TABLE [TestType] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [ExportCode] nvarchar(50) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_TestType] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE TABLE [TestResults] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [SnomedCode] nvarchar(20) NULL,
        [SnomedDisplay] nvarchar(200) NULL,
        [Hl7Code] nvarchar(20) NULL,
        [ExportCode] nvarchar(50) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL,
        [TestTypeId] int NULL,
        CONSTRAINT [PK_TestResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TestResults_TestType_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestType] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestResultId] ON [LabResults] ([TestResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestTypeId] ON [LabResults] ([TestTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE INDEX [IX_LabResultMarkers_TestResultId] ON [LabResultMarkers] ([TestResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    CREATE INDEX [IX_TestResults_TestTypeId] ON [TestResults] ([TestTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResultMarkers] ADD CONSTRAINT [FK_LabResultMarkers_TestResults_TestResultId] FOREIGN KEY ([TestResultId]) REFERENCES [TestResults] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_TestResults_TestResultId] FOREIGN KEY ([TestResultId]) REFERENCES [TestResults] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_TestType_TestTypeId] FOREIGN KEY ([TestTypeId]) REFERENCES [TestType] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503093737_Add_SNOMED_To_TestResults_And_TestMethods', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503101043_Add_DiseaseHL7MatchingConfig'
)
BEGIN
    CREATE TABLE [DiseaseHL7MatchingConfigs] (
        [DiseaseId] uniqueidentifier NOT NULL,
        [OverrideParentRules] bit NOT NULL,
        [TestMethod_UseTextFallback] bit NOT NULL,
        [TestMethod_NormalizeWhitespace] bit NOT NULL,
        [TestMethod_IgnorePunctuation] bit NOT NULL,
        [TestMethod_CaseInsensitive] bit NOT NULL,
        [SpecimenType_UseTextFallback] bit NOT NULL,
        [SpecimenType_NormalizeWhitespace] bit NOT NULL,
        [SpecimenType_IgnorePunctuation] bit NOT NULL,
        [SpecimenType_CaseInsensitive] bit NOT NULL,
        [Pathogen_UseTextFallback] bit NOT NULL,
        [Pathogen_NormalizeWhitespace] bit NOT NULL,
        [Pathogen_IgnorePunctuation] bit NOT NULL,
        [Pathogen_CaseInsensitive] bit NOT NULL,
        [TestResult_UseTextFallback] bit NOT NULL,
        [TestResult_NormalizeWhitespace] bit NOT NULL,
        [TestResult_IgnorePunctuation] bit NOT NULL,
        [TestResult_CaseInsensitive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(max) NULL,
        [UpdatedBy] nvarchar(max) NULL,
        CONSTRAINT [PK_DiseaseHL7MatchingConfigs] PRIMARY KEY ([DiseaseId]),
        CONSTRAINT [FK_DiseaseHL7MatchingConfigs_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260503101043_Add_DiseaseHL7MatchingConfig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260503101043_Add_DiseaseHL7MatchingConfig', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE TABLE [CaseDefinitionLabCriteria] (
        [Id] int NOT NULL IDENTITY,
        [CaseDefinitionId] int NOT NULL,
        [AcceptableSpecimenTypesJson] nvarchar(max) NOT NULL,
        [SpecimenStoragePreference] int NOT NULL,
        [CanonicalSpecimenTypeId] int NULL,
        [AcceptablePathogensJson] nvarchar(max) NOT NULL,
        [BiomarkerStoragePreference] int NOT NULL,
        [CanonicalPathogenId] uniqueidentifier NULL,
        [AcceptableTestMethodsJson] nvarchar(max) NOT NULL,
        [TestMethodStoragePreference] int NOT NULL,
        [CanonicalTestMethodId] int NULL,
        [AcceptableResultsJson] nvarchar(max) NOT NULL,
        [ResultStoragePreference] int NOT NULL,
        [CanonicalTestResultId] int NULL,
        [GroupNumber] int NOT NULL,
        [LogicalOperator] int NOT NULL,
        [IsRequired] bit NOT NULL,
        [RequireAllElementsMatch] bit NOT NULL,
        [DisplayOrder] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_CaseDefinitionLabCriteria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CaseDefinitionLabCriteria_CaseDefinitions_CaseDefinitionId] FOREIGN KEY ([CaseDefinitionId]) REFERENCES [CaseDefinitions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CaseDefinitionLabCriteria_Pathogens_CanonicalPathogenId] FOREIGN KEY ([CanonicalPathogenId]) REFERENCES [Pathogens] ([Id]),
        CONSTRAINT [FK_CaseDefinitionLabCriteria_SpecimenTypes_CanonicalSpecimenTypeId] FOREIGN KEY ([CanonicalSpecimenTypeId]) REFERENCES [SpecimenTypes] ([Id]),
        CONSTRAINT [FK_CaseDefinitionLabCriteria_TestMethods_CanonicalTestMethodId] FOREIGN KEY ([CanonicalTestMethodId]) REFERENCES [TestMethods] ([Id]),
        CONSTRAINT [FK_CaseDefinitionLabCriteria_TestResults_CanonicalTestResultId] FOREIGN KEY ([CanonicalTestResultId]) REFERENCES [TestResults] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionLabCriteria_CanonicalPathogenId] ON [CaseDefinitionLabCriteria] ([CanonicalPathogenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionLabCriteria_CanonicalSpecimenTypeId] ON [CaseDefinitionLabCriteria] ([CanonicalSpecimenTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionLabCriteria_CanonicalTestMethodId] ON [CaseDefinitionLabCriteria] ([CanonicalTestMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionLabCriteria_CanonicalTestResultId] ON [CaseDefinitionLabCriteria] ([CanonicalTestResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionLabCriteria_CaseDefinitionId] ON [CaseDefinitionLabCriteria] ([CaseDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505053947_AddCaseDefinitionLabCriteria'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505053947_AddCaseDefinitionLabCriteria', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    DROP TABLE [CaseDefinitionLabCriteria];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'ValueJson');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [CaseDefinitionCriteria] ALTER COLUMN [ValueJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'Operator');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [CaseDefinitionCriteria] ALTER COLUMN [Operator] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'FieldPath');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [CaseDefinitionCriteria] ALTER COLUMN [FieldPath] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[CaseDefinitionCriteria]') AND [c].[name] = N'DisplayText');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [CaseDefinitionCriteria] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [CaseDefinitionCriteria] ALTER COLUMN [DisplayText] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [AcceptablePathogensJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [AcceptableResultsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [AcceptableSpecimenTypesJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [AcceptableTestMethodsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [BiomarkerStoragePreference] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CanonicalPathogenId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CanonicalSpecimenTypeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CanonicalTestMethodId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CanonicalTestResultId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [Description] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [IsRequired] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [ModifiedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [RequireAllElementsMatch] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [ResultStoragePreference] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [SpecimenStoragePreference] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [TestMethodStoragePreference] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CanonicalPathogenId] ON [CaseDefinitionCriteria] ([CanonicalPathogenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CanonicalSpecimenTypeId] ON [CaseDefinitionCriteria] ([CanonicalSpecimenTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CanonicalTestMethodId] ON [CaseDefinitionCriteria] ([CanonicalTestMethodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    CREATE INDEX [IX_CaseDefinitionCriteria_CanonicalTestResultId] ON [CaseDefinitionCriteria] ([CanonicalTestResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD CONSTRAINT [FK_CaseDefinitionCriteria_Pathogens_CanonicalPathogenId] FOREIGN KEY ([CanonicalPathogenId]) REFERENCES [Pathogens] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD CONSTRAINT [FK_CaseDefinitionCriteria_SpecimenTypes_CanonicalSpecimenTypeId] FOREIGN KEY ([CanonicalSpecimenTypeId]) REFERENCES [SpecimenTypes] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD CONSTRAINT [FK_CaseDefinitionCriteria_TestMethods_CanonicalTestMethodId] FOREIGN KEY ([CanonicalTestMethodId]) REFERENCES [TestMethods] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD CONSTRAINT [FK_CaseDefinitionCriteria_TestResults_CanonicalTestResultId] FOREIGN KEY ([CanonicalTestResultId]) REFERENCES [TestResults] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505082928_MergeCaseDefinitionLabCriteria'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505082928_MergeCaseDefinitionLabCriteria', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505115951_IncreaseQualitativeResultTextLength'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LabResultMarkers]') AND [c].[name] = N'QualitativeResultText');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [LabResultMarkers] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [LabResultMarkers] ALTER COLUMN [QualitativeResultText] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260505115951_IncreaseQualitativeResultTextLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260505115951_IncreaseQualitativeResultTextLength', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506094628_Add_HL7Message_Unique_MessageControlId_Index'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_HL7Messages_MessageControlId_SendingFacility] ON [HL7Messages] ([MessageControlId], [SendingFacility]) WHERE [MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260506094628_Add_HL7Message_Unique_MessageControlId_Index'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260506094628_Add_HL7Message_Unique_MessageControlId_Index', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    ALTER TABLE [HL7FieldMappings] ADD [DiseaseId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    ALTER TABLE [HL7Configurations] ADD [IsTestMode] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    ALTER TABLE [HL7Configurations] ADD [TestModeDescription] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    CREATE TABLE [HL7ConfigurationDiseases] (
        [Id] uniqueidentifier NOT NULL,
        [ConfigurationId] uniqueidentifier NOT NULL,
        [DiseaseId] uniqueidentifier NOT NULL,
        [IsDefault] bit NOT NULL,
        [Priority] int NOT NULL,
        [Notes] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_HL7ConfigurationDiseases] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7ConfigurationDiseases_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_HL7ConfigurationDiseases_HL7Configurations_ConfigurationId] FOREIGN KEY ([ConfigurationId]) REFERENCES [HL7Configurations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    CREATE INDEX [IX_HL7FieldMappings_DiseaseId] ON [HL7FieldMappings] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    CREATE UNIQUE INDEX [IX_HL7ConfigurationDiseases_ConfigurationId_DiseaseId] ON [HL7ConfigurationDiseases] ([ConfigurationId], [DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    CREATE INDEX [IX_HL7ConfigurationDiseases_DiseaseId] ON [HL7ConfigurationDiseases] ([DiseaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    CREATE INDEX [IX_HL7ConfigurationDiseases_IsDefault] ON [HL7ConfigurationDiseases] ([IsDefault]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    ALTER TABLE [HL7FieldMappings] ADD CONSTRAINT [FK_HL7FieldMappings_Diseases_DiseaseId] FOREIGN KEY ([DiseaseId]) REFERENCES [Diseases] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507122737_Add_HL7_Configuration_MVP_Fields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507122737_Add_HL7_Configuration_MVP_Fields', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507130251_Add_SampleMessage_To_HL7FieldMapping'
)
BEGIN
    ALTER TABLE [HL7FieldMappings] ADD [SampleMessage] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260507130251_Add_SampleMessage_To_HL7FieldMapping'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260507130251_Add_SampleMessage_To_HL7FieldMapping', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE TABLE [HL7TestMessageTemplates] (
        [Id] uniqueidentifier NOT NULL,
        [TemplateName] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [LabTemplateType] int NOT NULL,
        [ConfigurationJson] nvarchar(max) NOT NULL,
        [TestComment] nvarchar(2000) NULL,
        [IsFavorite] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(450) NULL,
        [CreatedByUserId] nvarchar(450) NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(450) NULL,
        [UpdatedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_HL7TestMessageTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7TestMessageTemplates_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_HL7TestMessageTemplates_AspNetUsers_UpdatedByUserId] FOREIGN KEY ([UpdatedByUserId]) REFERENCES [AspNetUsers] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE TABLE [HL7TestMessageHistory] (
        [Id] uniqueidentifier NOT NULL,
        [TemplateId] uniqueidentifier NULL,
        [RawHL7Message] nvarchar(max) NOT NULL,
        [FilePath] nvarchar(1000) NULL,
        [TestComment] nvarchar(2000) NULL,
        [AccessionNumber] nvarchar(100) NULL,
        [PatientMRN] nvarchar(100) NULL,
        [ConfigurationSnapshot] nvarchar(max) NULL,
        [HL7MessageId] uniqueidentifier NULL,
        [ProcessingResultJson] nvarchar(max) NULL,
        [ProcessingStatus] int NULL,
        [GeneratedAt] datetime2 NOT NULL,
        [GeneratedBy] nvarchar(450) NULL,
        [GeneratedByUserId] nvarchar(450) NULL,
        [WasAutoProcessed] bit NOT NULL,
        CONSTRAINT [PK_HL7TestMessageHistory] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HL7TestMessageHistory_AspNetUsers_GeneratedByUserId] FOREIGN KEY ([GeneratedByUserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_HL7TestMessageHistory_HL7Messages_HL7MessageId] FOREIGN KEY ([HL7MessageId]) REFERENCES [HL7Messages] ([Id]),
        CONSTRAINT [FK_HL7TestMessageHistory_HL7TestMessageTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [HL7TestMessageTemplates] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE INDEX [IX_HL7TestMessageHistory_GeneratedByUserId] ON [HL7TestMessageHistory] ([GeneratedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE INDEX [IX_HL7TestMessageHistory_HL7MessageId] ON [HL7TestMessageHistory] ([HL7MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE INDEX [IX_HL7TestMessageHistory_TemplateId] ON [HL7TestMessageHistory] ([TemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE INDEX [IX_HL7TestMessageTemplates_CreatedByUserId] ON [HL7TestMessageTemplates] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    CREATE INDEX [IX_HL7TestMessageTemplates_UpdatedByUserId] ON [HL7TestMessageTemplates] ([UpdatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260624125019_AddHL7TestGeneratorTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260624125019_AddHL7TestGeneratorTables', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625100551_AddHL7Permissions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625100551_AddHL7Permissions', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701091429_AddGeocodingQueueTable'
)
BEGIN
    CREATE TABLE [GeocodingQueueItems] (
        [Id] uniqueidentifier NOT NULL,
        [PatientId] uniqueidentifier NOT NULL,
        [FullAddress] nvarchar(max) NOT NULL,
        [QueuedAt] datetime2 NOT NULL,
        [ProcessedAt] datetime2 NULL,
        [AttemptCount] int NOT NULL,
        [NextAttemptAt] datetime2 NULL,
        [IsCompleted] bit NOT NULL,
        [Failed] bit NOT NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [ResultLatitude] float NULL,
        [ResultLongitude] float NULL,
        CONSTRAINT [PK_GeocodingQueueItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GeocodingQueueItems_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701091429_AddGeocodingQueueTable'
)
BEGIN
    CREATE INDEX [IX_GeocodingQueueItems_PatientId] ON [GeocodingQueueItems] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701091429_AddGeocodingQueueTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701091429_AddGeocodingQueueTable', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701114611_AddDashboardConfigToUser'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [DashboardConfigJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701114611_AddDashboardConfigToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701114611_AddDashboardConfigToUser', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseReinfectionRules]') AND [c].[name] = N'AlwaysCreateNewCase');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseReinfectionRules] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [DiseaseReinfectionRules] DROP COLUMN [AlwaysCreateNewCase];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseReinfectionRules]') AND [c].[name] = N'CaseMatchingStrategy');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseReinfectionRules] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [DiseaseReinfectionRules] DROP COLUMN [CaseMatchingStrategy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseReinfectionRules]') AND [c].[name] = N'IsChronic');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseReinfectionRules] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [DiseaseReinfectionRules] DROP COLUMN [IsChronic];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseReinfectionRules]') AND [c].[name] = N'MatchOnResultType');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseReinfectionRules] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [DiseaseReinfectionRules] DROP COLUMN [MatchOnResultType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseReinfectionRules]') AND [c].[name] = N'MatchOnTestType');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseReinfectionRules] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [DiseaseReinfectionRules] DROP COLUMN [MatchOnTestType];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [HL7Messages] ADD [PartialMatchDetailsJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [AllowMissingPathogen] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [AllowMissingResult] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [AllowMissingSpecimenType] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [AllowMissingTestMethod] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [MaxMissingFieldsAllowed] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD [PartialMatchConfirmationStatusId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    CREATE INDEX [IX_DiseaseHL7MatchingConfigs_PartialMatchConfirmationStatusId] ON [DiseaseHL7MatchingConfigs] ([PartialMatchConfirmationStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    ALTER TABLE [DiseaseHL7MatchingConfigs] ADD CONSTRAINT [FK_DiseaseHL7MatchingConfigs_CaseStatuses_PartialMatchConfirmationStatusId] FOREIGN KEY ([PartialMatchConfirmationStatusId]) REFERENCES [CaseStatuses] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710105354_SimplifyReinfectionRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710105354_SimplifyReinfectionRules', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710114507_RemoveRequiredFromPartialMatchStatus'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DiseaseHL7MatchingConfigs]') AND [c].[name] = N'PartialMatchConfirmationStatusId');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [DiseaseHL7MatchingConfigs] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [DiseaseHL7MatchingConfigs] ALTER COLUMN [PartialMatchConfirmationStatusId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260710114507_RemoveRequiredFromPartialMatchStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260710114507_RemoveRequiredFromPartialMatchStatus', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714234516_Remove_HL7Message_Unique_Index_To_Allow_Duplicates'
)
BEGIN
    DROP INDEX [IX_HL7Messages_MessageControlId_SendingFacility] ON [HL7Messages];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714234516_Remove_HL7Message_Unique_Index_To_Allow_Duplicates'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_HL7Messages_MessageControlId_SendingFacility] ON [HL7Messages] ([MessageControlId], [SendingFacility]) WHERE [MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714234516_Remove_HL7Message_Unique_Index_To_Allow_Duplicates'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260714234516_Remove_HL7Message_Unique_Index_To_Allow_Duplicates', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722002830_AddGroupExitOperatorToCriteria'
)
BEGIN
    ALTER TABLE [CaseDefinitionCriteria] ADD [GroupExitOperator] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260722002830_AddGroupExitOperatorToCriteria'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722002830_AddGroupExitOperatorToCriteria', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082854_AddMultiplexLabResultSupport'
)
BEGIN
    ALTER TABLE [LabResults] ADD [IsMultiplexClone] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082854_AddMultiplexLabResultSupport'
)
BEGIN
    ALTER TABLE [LabResults] ADD [ParentLabResultId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082854_AddMultiplexLabResultSupport'
)
BEGIN
    CREATE INDEX [IX_LabResults_ParentLabResultId] ON [LabResults] ([ParentLabResultId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082854_AddMultiplexLabResultSupport'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_LabResults_ParentLabResultId] FOREIGN KEY ([ParentLabResultId]) REFERENCES [LabResults] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724082854_AddMultiplexLabResultSupport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724082854_AddMultiplexLabResultSupport', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093035_SeedHealthcareProviderOrganizationType'
)
BEGIN

                    IF NOT EXISTS (SELECT 1 FROM OrganizationTypes WHERE Name = 'Healthcare Provider')
                    BEGIN
                        INSERT INTO OrganizationTypes (Name, IsActive)
                        VALUES ('Healthcare Provider', 1);
                    END
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724093035_SeedHealthcareProviderOrganizationType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724093035_SeedHealthcareProviderOrganizationType', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725000750_UpdateHL7ReviewOutcomeModel'
)
BEGIN
    ALTER TABLE [HL7Messages] ADD [ReviewOutcome] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725000750_UpdateHL7ReviewOutcomeModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725000750_UpdateHL7ReviewOutcomeModel', N'10.0.2');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725000941_AddHL7MessageReviewOutcome'
)
BEGIN
    ALTER TABLE [HL7Messages] ADD [ReviewOutcome] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725000941_AddHL7MessageReviewOutcome'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725000941_AddHL7MessageReviewOutcome', N'10.0.2');
END;

COMMIT;
GO

