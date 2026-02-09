-- Migration: Add Exposure Tracking System
-- This migration adds the Location, Event, and ExposureEvent models for exposure tracking

-- Create LocationTypes lookup table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LocationTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [LocationTypes] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsHighRisk] bit NOT NULL DEFAULT 0,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_LocationTypes] PRIMARY KEY ([Id])
    );
END
GO

-- Create EventTypes lookup table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EventTypes]') AND type in (N'U'))
BEGIN
    CREATE TABLE [EventTypes] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DisplayOrder] int NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        CONSTRAINT [PK_EventTypes] PRIMARY KEY ([Id])
    );
END
GO

-- Create Locations table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Locations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Locations] (
        [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
        [Name] nvarchar(200) NOT NULL,
        [LocationTypeId] int NULL,
        [Address] nvarchar(500) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [GeocodingStatus] nvarchar(50) NULL,
        [LastGeocoded] datetime2 NULL,
        [OrganizationId] uniqueidentifier NULL,
        [IsHighRisk] bit NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [Notes] nvarchar(2000) NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId] FOREIGN KEY ([LocationTypeId]) REFERENCES [LocationTypes]([Id]),
        CONSTRAINT [FK_Locations_Organizations_OrganizationId] FOREIGN KEY ([OrganizationId]) REFERENCES [Organizations]([Id])
    );

    CREATE INDEX [IX_Locations_Name] ON [Locations] ([Name]);
    CREATE INDEX [IX_Locations_LocationTypeId] ON [Locations] ([LocationTypeId]);
    CREATE INDEX [IX_Locations_Latitude_Longitude] ON [Locations] ([Latitude], [Longitude]);
    CREATE INDEX [IX_Locations_GeocodingStatus] ON [Locations] ([GeocodingStatus]);
    CREATE INDEX [IX_Locations_OrganizationId] ON [Locations] ([OrganizationId]);
END
GO

-- Create Events table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND type in (N'U'))
BEGIN
    CREATE TABLE [Events] (
        [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
        [Name] nvarchar(200) NOT NULL,
        [EventTypeId] int NULL,
        [LocationId] uniqueidentifier NOT NULL,
        [StartDateTime] datetime2 NOT NULL,
        [EndDateTime] datetime2 NULL,
        [EstimatedAttendees] int NULL,
        [IsIndoor] bit NULL,
        [OrganizerOrganizationId] uniqueidentifier NULL,
        [Description] nvarchar(2000) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Events_EventTypes_EventTypeId] FOREIGN KEY ([EventTypeId]) REFERENCES [EventTypes]([Id]),
        CONSTRAINT [FK_Events_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id]),
        CONSTRAINT [FK_Events_Organizations_OrganizerOrganizationId] FOREIGN KEY ([OrganizerOrganizationId]) REFERENCES [Organizations]([Id])
    );

    CREATE INDEX [IX_Events_Name] ON [Events] ([Name]);
    CREATE INDEX [IX_Events_EventTypeId] ON [Events] ([EventTypeId]);
    CREATE INDEX [IX_Events_LocationId] ON [Events] ([LocationId]);
    CREATE INDEX [IX_Events_OrganizerOrganizationId] ON [Events] ([OrganizerOrganizationId]);
    CREATE INDEX [IX_Events_StartDateTime_EndDateTime] ON [Events] ([StartDateTime], [EndDateTime]);
END
GO

-- Create ExposureEvents table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExposureEvents]') AND type in (N'U'))
BEGIN
    CREATE TABLE [ExposureEvents] (
        [Id] uniqueidentifier NOT NULL DEFAULT NEWID(),
        [CaseId] uniqueidentifier NOT NULL,
        [ExposureType] int NOT NULL,
        [ExposureStartDate] datetime2 NOT NULL,
        [ExposureEndDate] datetime2 NULL,
        [EventId] uniqueidentifier NULL,
        [LocationId] uniqueidentifier NULL,
        [RelatedCaseId] uniqueidentifier NULL,
        [ContactType] int NULL,
        [CountryCode] nvarchar(3) NULL,
        [FreeTextLocation] nvarchar(500) NULL,
        [Description] nvarchar(2000) NULL,
        [ExposureStatus] int NOT NULL DEFAULT 0,
        [ConfidenceLevel] nvarchar(50) NULL,
        [InvestigationNotes] nvarchar(2000) NULL,
        [StatusChangedDate] datetime2 NULL,
        [StatusChangedByUserId] nvarchar(450) NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedByUserId] nvarchar(450) NULL,
        [LastModified] datetime2 NULL,
        [LastModifiedByUserId] nvarchar(450) NULL,
        CONSTRAINT [PK_ExposureEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ExposureEvents_Cases_CaseId] FOREIGN KEY ([CaseId]) REFERENCES [Cases]([Id]),
        CONSTRAINT [FK_ExposureEvents_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events]([Id]),
        CONSTRAINT [FK_ExposureEvents_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id]),
        CONSTRAINT [FK_ExposureEvents_Cases_RelatedCaseId] FOREIGN KEY ([RelatedCaseId]) REFERENCES [Cases]([Id])
    );

    CREATE INDEX [IX_ExposureEvents_CaseId] ON [ExposureEvents] ([CaseId]);
    CREATE INDEX [IX_ExposureEvents_EventId] ON [ExposureEvents] ([EventId]);
    CREATE INDEX [IX_ExposureEvents_LocationId] ON [ExposureEvents] ([LocationId]);
    CREATE INDEX [IX_ExposureEvents_RelatedCaseId] ON [ExposureEvents] ([RelatedCaseId]);
    CREATE INDEX [IX_ExposureEvents_ExposureType] ON [ExposureEvents] ([ExposureType]);
    CREATE INDEX [IX_ExposureEvents_ExposureStatus] ON [ExposureEvents] ([ExposureStatus]);
    CREATE INDEX [IX_ExposureEvents_ExposureStartDate_ExposureEndDate] ON [ExposureEvents] ([ExposureStartDate], [ExposureEndDate]);
END
GO

PRINT 'Exposure Tracking System tables created successfully';
