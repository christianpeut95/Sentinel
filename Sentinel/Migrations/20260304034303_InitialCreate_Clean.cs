using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_Clean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ancestries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ancestries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrimaryLanguage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LanguagesSpokenJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInterviewWorker = table.Column<bool>(type: "bit", nullable: false),
                    AvailableForAutoAssignment = table.Column<bool>(type: "bit", nullable: false),
                    CurrentTaskCapacity = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AtsiStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AtsiStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackupType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BackupFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BackupFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseContactTasksFlattened",
                columns: table => new
                {
                    CaseGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GenerationNumber = table.Column<int>(type: "int", nullable: false),
                    TransmissionChainPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransmittedByCase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaseTypeEnum = table.Column<int>(type: "int", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfNotification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CaseStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientDOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgeAtOnset = table.Column<int>(type: "int", nullable: true),
                    PatientSuburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientMobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiseaseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiseaseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExposureType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureStatusDisplay = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExposureEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExposureDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedAttendees = table.Column<int>(type: "int", nullable: true),
                    EventSetting = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventOrganizer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationIsHighRisk = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationOrganization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskPriority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskCancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInterviewTask = table.Column<bool>(type: "bit", nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignmentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurveyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IncubationPeriodDays = table.Column<int>(type: "int", nullable: true),
                    DaysUntilTaskDue = table.Column<int>(type: "int", nullable: true),
                    TaskAgeDays = table.Column<int>(type: "int", nullable: true),
                    TaskDueStatus = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "CaseStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableTo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaseTimelineAll",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiseaseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventUser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventSequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ContactClassifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactClassifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactsListSimple",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateIdentified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactDateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactFirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactLastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactDOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactMobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactSuburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactDisease = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposedByCase = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposedByName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposedByDisease = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureTypeEnum = table.Column<int>(type: "int", nullable: true),
                    ExposureType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExposureEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExposureSetting = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalTasks = table.Column<int>(type: "int", nullable: false),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    InterviewTasks = table.Column<int>(type: "int", nullable: false),
                    NextTaskDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FollowUpStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ContactTracingMindMapEdges",
                columns: table => new
                {
                    EdgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExposureTypeEnum = table.Column<int>(type: "int", nullable: false),
                    ExposureType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExposureStatusEnum = table.Column<int>(type: "int", nullable: false),
                    ExposureStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EdgeLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactClassification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExposureStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExposureEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EdgeStyle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EdgeColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EdgeWeight = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ContactTracingMindMapNodes",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiseaseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiseaseCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfNotification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateIdentified = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaseStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutgoingTransmissions = table.Column<int>(type: "int", nullable: false),
                    IncomingExposures = table.Column<int>(type: "int", nullable: false),
                    TotalTasks = table.Column<int>(type: "int", nullable: false),
                    CompletedTasks = table.Column<int>(type: "int", nullable: false),
                    FollowUpStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Suburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction1 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiseaseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportingId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JurisdictionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JurisdictionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsHighRisk = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Occupations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MajorGroupCode = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    MajorGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubMajorGroupCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    SubMajorGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinorGroupCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    MinorGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitGroupCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    UnitGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Occupations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakTasksFlattened",
                columns: table => new
                {
                    OutbreakNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutbreakName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutbreakLevel = table.Column<int>(type: "int", nullable: false),
                    HierarchyPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutbreakTypeEnum = table.Column<int>(type: "int", nullable: false),
                    OutbreakType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutbreakStatusEnum = table.Column<int>(type: "int", nullable: false),
                    OutbreakStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutbreakStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OutbreakEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutbreakConfirmationStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryDisease = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryEvent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeadInvestigator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeadInvestigatorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfNotification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientSuburb = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiseaseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaseStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaskNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskTitle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskPriority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaskDueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaskCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInterviewTask = table.Column<bool>(type: "bit", nullable: true),
                    TaskType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysIntoOutbreak = table.Column<int>(type: "int", nullable: true),
                    DaysUntilTaskDue = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessType = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFolders_ReportFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ReportFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResultUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SexAtBirths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SexAtBirths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpecimenTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsInvasive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpecimenTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SurveyTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SurveyDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultInputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultOutputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ParentSurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VersionStatus = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyTemplates_SurveyTemplates_ParentSurveyTemplateId",
                        column: x => x.ParentSurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Symptoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symptoms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ColorClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsInterviewTask = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Diseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiseaseCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PathIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    AccessLevel = table.Column<int>(type: "int", nullable: false),
                    ExposureTrackingMode = table.Column<int>(type: "int", nullable: false),
                    DefaultToResidentialAddress = table.Column<bool>(type: "bit", nullable: false),
                    AlwaysPromptForLocation = table.Column<bool>(type: "bit", nullable: false),
                    SyncWithPatientAddressUpdates = table.Column<bool>(type: "bit", nullable: false),
                    ExposureGuidanceText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequireGeographicCoordinates = table.Column<bool>(type: "bit", nullable: false),
                    AllowDomesticAcquisition = table.Column<bool>(type: "bit", nullable: false),
                    ExposureDataGracePeriodDays = table.Column<int>(type: "int", nullable: true),
                    RequiredLocationTypeIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewGroupingWindowHours = table.Column<int>(type: "int", nullable: false),
                    ReviewAutoQueueLabResults = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueExposures = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueContacts = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueConfirmationChanges = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueDiseaseChanges = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueClinicalNotifications = table.Column<bool>(type: "bit", nullable: false),
                    ReviewAutoQueueNewCases = table.Column<bool>(type: "bit", nullable: false),
                    ReviewDefaultPriority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diseases_DiseaseCategories_DiseaseCategoryId",
                        column: x => x.DiseaseCategoryId,
                        principalTable: "DiseaseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Diseases_Diseases_ParentDiseaseId",
                        column: x => x.ParentDiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGroups",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGroups", x => new { x.UserId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_UserGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Jurisdictions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JurisdictionTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentJurisdictionId = table.Column<int>(type: "int", nullable: true),
                    BoundaryData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Population = table.Column<long>(type: "bigint", nullable: true),
                    PopulationYear = table.Column<int>(type: "int", nullable: true),
                    PopulationSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jurisdictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jurisdictions_JurisdictionTypes_JurisdictionTypeId",
                        column: x => x.JurisdictionTypeId,
                        principalTable: "JurisdictionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jurisdictions_Jurisdictions_ParentJurisdictionId",
                        column: x => x.ParentJurisdictionId,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FieldType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsSearchable = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnList = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnCreateEdit = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnDetails = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LookupTableId = table.Column<int>(type: "int", nullable: true),
                    ShowOnPatientForm = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnCaseForm = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinitions_LookupTables_LookupTableId",
                        column: x => x.LookupTableId,
                        principalTable: "LookupTables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LookupTableId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupTables_LookupTableId",
                        column: x => x.LookupTableId,
                        principalTable: "LookupTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendlyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrganizationTypeId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_OrganizationTypes_OrganizationTypeId",
                        column: x => x.OrganizationTypeId,
                        principalTable: "OrganizationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_UserPermissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PivotConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectionQueriesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    IsTemplate = table.Column<bool>(type: "bit", nullable: false),
                    LastRunDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RunCount = table.Column<int>(type: "int", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDefinitions_ReportFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ReportFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportFolderShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportFolderId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    PermissionLevel = table.Column<int>(type: "int", nullable: false),
                    SharedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SharedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFolderShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFolderShares_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportFolderShares_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReportFolderShares_ReportFolders_ReportFolderId",
                        column: x => x.ReportFolderId,
                        principalTable: "ReportFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyFieldMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationType = table.Column<int>(type: "int", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    SurveyQuestionName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetFieldPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetFieldType = table.Column<int>(type: "int", nullable: false),
                    FieldCategory = table.Column<int>(type: "int", nullable: false),
                    MappingAction = table.Column<int>(type: "int", nullable: false),
                    BusinessRule = table.Column<int>(type: "int", nullable: false),
                    TriggerReviewQueue = table.Column<bool>(type: "bit", nullable: false),
                    ReviewPriority = table.Column<int>(type: "int", nullable: false),
                    GroupingWindowHours = table.Column<int>(type: "int", nullable: false),
                    ValidationRules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TransformationScript = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    TargetSymptomId = table.Column<int>(type: "int", nullable: true),
                    Complexity = table.Column<int>(type: "int", nullable: false),
                    CollectionConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatchingRulesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnDuplicateFound = table.Column<int>(type: "int", nullable: true),
                    ExecutionOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyFieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyFieldMappings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SurveyFieldMappings_AspNetUsers_LastModifiedByUserId",
                        column: x => x.LastModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SurveyFieldMappings_Symptoms_TargetSymptomId",
                        column: x => x.TargetSymptomId,
                        principalTable: "Symptoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TaskTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultPriority = table.Column<int>(type: "int", nullable: false),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    ApplicableToType = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromOnset = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromNotification = table.Column<int>(type: "int", nullable: true),
                    DueDaysFromContact = table.Column<int>(type: "int", nullable: true),
                    DueCalculationMethod = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "int", nullable: true),
                    RecurrenceCount = table.Column<int>(type: "int", nullable: true),
                    RecurrenceDurationDays = table.Column<int>(type: "int", nullable: true),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SurveyDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultInputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultOutputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletionCriteria = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiresEvidence = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    InheritanceBehavior = table.Column<int>(type: "int", nullable: false),
                    RestrictToSubDiseaseIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsInterviewTask = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskTemplates_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TaskTemplates_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TestTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestResults_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiseaseSymptoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SymptomId = table.Column<int>(type: "int", nullable: false),
                    IsCommon = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseSymptoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseSymptoms_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiseaseSymptoms_Symptoms_SymptomId",
                        column: x => x.SymptomId,
                        principalTable: "Symptoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleDiseaseAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    ApplyToChildren = table.Column<bool>(type: "bit", nullable: false),
                    InheritedFromDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleDiseaseAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RoleDiseaseAccess_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SurveyTemplateDiseases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurveyTemplateDiseases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SurveyTemplateDiseases_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SurveyTemplateDiseases_SurveyTemplates_SurveyTemplateId",
                        column: x => x.SurveyTemplateId,
                        principalTable: "SurveyTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDiseaseAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplyToChildren = table.Column<bool>(type: "bit", nullable: false),
                    InheritedFromDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDiseaseAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDiseaseAccess_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendlyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GivenName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FamilyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SexAtBirthId = table.Column<int>(type: "int", nullable: true),
                    GenderId = table.Column<int>(type: "int", nullable: true),
                    HomePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobilePhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    CountryOfBirthId = table.Column<int>(type: "int", nullable: true),
                    LanguageSpokenAtHomeId = table.Column<int>(type: "int", nullable: true),
                    AncestryId = table.Column<int>(type: "int", nullable: true),
                    AtsiStatusId = table.Column<int>(type: "int", nullable: true),
                    OccupationId = table.Column<int>(type: "int", nullable: true),
                    IsDeceased = table.Column<bool>(type: "bit", nullable: false),
                    DateOfDeath = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Jurisdiction1Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction2Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction3Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction4Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction5Id = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_Ancestries_AncestryId",
                        column: x => x.AncestryId,
                        principalTable: "Ancestries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_AtsiStatuses_AtsiStatusId",
                        column: x => x.AtsiStatusId,
                        principalTable: "AtsiStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_Countries_CountryOfBirthId",
                        column: x => x.CountryOfBirthId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_Genders_GenderId",
                        column: x => x.GenderId,
                        principalTable: "Genders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_Jurisdictions_Jurisdiction1Id",
                        column: x => x.Jurisdiction1Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Jurisdictions_Jurisdiction2Id",
                        column: x => x.Jurisdiction2Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Jurisdictions_Jurisdiction3Id",
                        column: x => x.Jurisdiction3Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Jurisdictions_Jurisdiction4Id",
                        column: x => x.Jurisdiction4Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Jurisdictions_Jurisdiction5Id",
                        column: x => x.Jurisdiction5Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Patients_Languages_LanguageSpokenAtHomeId",
                        column: x => x.LanguageSpokenAtHomeId,
                        principalTable: "Languages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_Occupations_OccupationId",
                        column: x => x.OccupationId,
                        principalTable: "Occupations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Patients_SexAtBirths_SexAtBirthId",
                        column: x => x.SexAtBirthId,
                        principalTable: "SexAtBirths",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DiseaseCustomFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    InheritToChildDiseases = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseCustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseCustomFields_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiseaseCustomFields_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationTypeId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    GeocodingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastGeocoded = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsHighRisk = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_LocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalculatedFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculatedFields_ReportDefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDefinitionId = table.Column<int>(type: "int", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PivotArea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AggregationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsCustomField = table.Column<bool>(type: "bit", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFields_ReportDefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportDefinitionId = table.Column<int>(type: "int", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsCustomField = table.Column<bool>(type: "bit", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogicOperator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    GroupLogicOperator = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsCollectionQuery = table.Column<bool>(type: "bit", nullable: false),
                    CollectionSubFilters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectionOperator = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportFilters_ReportDefinitions_ReportDefinitionId",
                        column: x => x.ReportDefinitionId,
                        principalTable: "ReportDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiseaseTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicableTo = table.Column<int>(type: "int", nullable: true),
                    IsInherited = table.Column<bool>(type: "bit", nullable: false),
                    InheritedFromDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApplyToChildren = table.Column<bool>(type: "bit", nullable: false),
                    AllowChildOverride = table.Column<bool>(type: "bit", nullable: false),
                    OverrideAutoCreate = table.Column<bool>(type: "bit", nullable: true),
                    OverridePriority = table.Column<int>(type: "int", nullable: true),
                    OverrideDueDays = table.Column<int>(type: "int", nullable: true),
                    OverrideInstructions = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AutoCreateOnCaseCreation = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateOnContactCreation = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateOnLabConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    InputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseTaskTemplates_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiseaseTaskTemplates_TaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendlyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOfOnset = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfNotification = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClinicalNotificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClinicalNotifierOrganisation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClinicalNotificationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfirmationStatusId = table.Column<int>(type: "int", nullable: true),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Hospitalised = table.Column<int>(type: "int", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DateOfAdmission = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfDischarge = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiedDueToDisease = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction1Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction2Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction3Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction4Id = table.Column<int>(type: "int", nullable: true),
                    Jurisdiction5Id = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_CaseStatuses_ConfirmationStatusId",
                        column: x => x.ConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cases_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Jurisdictions_Jurisdiction1Id",
                        column: x => x.Jurisdiction1Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Jurisdictions_Jurisdiction2Id",
                        column: x => x.Jurisdiction2Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Jurisdictions_Jurisdiction3Id",
                        column: x => x.Jurisdiction3Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Jurisdictions_Jurisdiction4Id",
                        column: x => x.Jurisdiction4Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Jurisdictions_Jurisdiction5Id",
                        column: x => x.Jurisdiction5Id,
                        principalTable: "Jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cases_Organizations_HospitalId",
                        column: x => x.HospitalId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Cases_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientCustomFieldBooleans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCustomFieldBooleans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldBooleans_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldBooleans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientCustomFieldDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCustomFieldDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldDates_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldDates_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientCustomFieldLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    LookupValueId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCustomFieldLookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldLookups_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldLookups_LookupValues_LookupValueId",
                        column: x => x.LookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldLookups_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientCustomFieldNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCustomFieldNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldNumbers_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldNumbers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientCustomFieldStrings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientCustomFieldStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldStrings_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientCustomFieldStrings_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EventTypeId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedAttendees = table.Column<int>(type: "int", nullable: true),
                    IsIndoor = table.Column<bool>(type: "bit", nullable: true),
                    OrganizerOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_EventTypes_EventTypeId",
                        column: x => x.EventTypeId,
                        principalTable: "EventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Events_Organizations_OrganizerOrganizationId",
                        column: x => x.OrganizerOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldBooleans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldBooleans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldBooleans_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldBooleans_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldDates_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldDates_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    LookupValueId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldLookups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldLookups_LookupValues_LookupValueId",
                        column: x => x.LookupValueId,
                        principalTable: "LookupValues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldNumbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldNumbers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldNumbers_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldNumbers_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseCustomFieldStrings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseCustomFieldStrings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldStrings_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseCustomFieldStrings_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseSymptoms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SymptomId = table.Column<int>(type: "int", nullable: false),
                    OnsetDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OtherSymptomText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseSymptoms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseSymptoms_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseSymptoms_Symptoms_SymptomId",
                        column: x => x.SymptomId,
                        principalTable: "Symptoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CaseTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TaskTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceFileIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SurveyResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecurrenceSequence = table.Column<int>(type: "int", nullable: true),
                    IsInterviewTask = table.Column<bool>(type: "bit", nullable: false),
                    AssignmentMethod = table.Column<int>(type: "int", nullable: false),
                    LanguageRequired = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxCallAttempts = table.Column<int>(type: "int", nullable: false),
                    CurrentAttemptCount = table.Column<int>(type: "int", nullable: false),
                    EscalationLevel = table.Column<int>(type: "int", nullable: false),
                    LastCallAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AutoAssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CaseId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseTasks_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseTasks_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseTasks_CaseTasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseTasks_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CaseTasks_Cases_CaseId1",
                        column: x => x.CaseId1,
                        principalTable: "Cases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseTasks_TaskTemplates_TaskTemplateId",
                        column: x => x.TaskTemplateId,
                        principalTable: "TaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CaseTasks_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FriendlyId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaboratoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccessionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SpecimenCollectionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpecimenTypeId = table.Column<int>(type: "int", nullable: true),
                    TestTypeId = table.Column<int>(type: "int", nullable: true),
                    TestedDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderingProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TestResultId = table.Column<int>(type: "int", nullable: true),
                    ResultDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QuantitativeResult = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ResultUnitsId = table.Column<int>(type: "int", nullable: true),
                    IsAmended = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LabInterpretation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_Diseases_TestedDiseaseId",
                        column: x => x.TestedDiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_Organizations_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_Organizations_OrderingProviderId",
                        column: x => x.OrderingProviderId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_ResultUnits_ResultUnitsId",
                        column: x => x.ResultUnitsId,
                        principalTable: "ResultUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_SpecimenTypes_SpecimenTypeId",
                        column: x => x.SpecimenTypeId,
                        principalTable: "SpecimenTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_TestResults_TestResultId",
                        column: x => x.TestResultId,
                        principalTable: "TestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabResults_TestTypes_TestTypeId",
                        column: x => x.TestTypeId,
                        principalTable: "TestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExposureEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExposedCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExposureType = table.Column<int>(type: "int", nullable: false),
                    ExposureStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExposureEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContactClassificationId = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    FreeTextLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExposureStatus = table.Column<int>(type: "int", nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDefaultedFromResidentialAddress = table.Column<bool>(type: "bit", nullable: false),
                    IsReportingExposure = table.Column<bool>(type: "bit", nullable: false),
                    IsInterstateTravel = table.Column<bool>(type: "bit", nullable: false),
                    InterstateOriginState = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressLine = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    GeocodingAccuracy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GeocodedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvestigationNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StatusChangedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifiedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExposureEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExposureEvents_Cases_ExposedCaseId",
                        column: x => x.ExposedCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExposureEvents_Cases_SourceCaseId",
                        column: x => x.SourceCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExposureEvents_ContactClassifications_ContactClassificationId",
                        column: x => x.ContactClassificationId,
                        principalTable: "ContactClassifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExposureEvents_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExposureEvents_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Outbreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfirmationStatusId = table.Column<int>(type: "int", nullable: true),
                    ParentOutbreakId = table.Column<int>(type: "int", nullable: true),
                    IndexCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrimaryDiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrimaryEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeadInvestigatorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Outbreaks_AspNetUsers_LeadInvestigatorId",
                        column: x => x.LeadInvestigatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_CaseStatuses_ConfirmationStatusId",
                        column: x => x.ConfirmationStatusId,
                        principalTable: "CaseStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Cases_IndexCaseId",
                        column: x => x.IndexCaseId,
                        principalTable: "Cases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Diseases_PrimaryDiseaseId",
                        column: x => x.PrimaryDiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Events_PrimaryEventId",
                        column: x => x.PrimaryEventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Locations_PrimaryLocationId",
                        column: x => x.PrimaryLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Outbreaks_Outbreaks_ParentOutbreakId",
                        column: x => x.ParentOutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewQueue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerField = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChangeSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ReviewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReviewAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GroupKey = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GroupCount = table.Column<int>(type: "int", nullable: false),
                    PotentialMatchesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProposedEntityDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectionSourceDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SelectedExistingEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewQueue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewQueue_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewQueue_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReviewQueue_CaseTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ReviewQueue_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewQueue_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewQueue_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskCallAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    NextCallbackScheduled = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNumberCalled = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCallAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCallAttempts_AspNetUsers_AttemptedByUserId",
                        column: x => x.AttemptedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCallAttempts_CaseTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "CaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OutbreakId = table.Column<int>(type: "int", nullable: true),
                    AttachmentPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notes_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notes_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakCaseDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    DefinitionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefinitionText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Classification = table.Column<int>(type: "int", nullable: false),
                    CriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakCaseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakCaseDefinitions_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakLineListConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SelectedFields = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilterConfiguration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakLineListConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakLineListConfigurations_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutbreakLineListConfigurations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutbreakLineListConfigurations_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakSearchQueries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    QueryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QueryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutoLink = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRunDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunMatchCount = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakSearchQueries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakSearchQueries_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakTeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakTeamMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutbreakTeamMembers_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakTimelines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    RelatedCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelatedNoteId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakTimelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakTimelines_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutbreakCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutbreakId = table.Column<int>(type: "int", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsIndexCase = table.Column<bool>(type: "bit", nullable: false),
                    Classification = table.Column<int>(type: "int", nullable: true),
                    ClassificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClassifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassificationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LinkMethod = table.Column<int>(type: "int", nullable: false),
                    SearchQueryId = table.Column<int>(type: "int", nullable: true),
                    LinkedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LinkedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnlinkedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlinkedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnlinkReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutbreakCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutbreakCases_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutbreakCases_OutbreakSearchQueries_SearchQueryId",
                        column: x => x.SearchQueryId,
                        principalTable: "OutbreakSearchQueries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OutbreakCases_Outbreaks_OutbreakId",
                        column: x => x.OutbreakId,
                        principalTable: "Outbreaks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ChangedAt",
                table: "AuditLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ChangedByUserId",
                table: "AuditLogs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedFields_ReportDefinitionId",
                table: "CalculatedFields",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldBooleans_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldBooleans",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldBooleans_FieldDefinitionId",
                table: "CaseCustomFieldBooleans",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldDates",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_FieldDefinitionId",
                table: "CaseCustomFieldDates",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldDates_Value",
                table: "CaseCustomFieldDates",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldLookups",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_FieldDefinitionId",
                table: "CaseCustomFieldLookups",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldLookups_LookupValueId",
                table: "CaseCustomFieldLookups",
                column: "LookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldNumbers",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_FieldDefinitionId",
                table: "CaseCustomFieldNumbers",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldNumbers_Value",
                table: "CaseCustomFieldNumbers",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_CaseId_FieldDefinitionId",
                table: "CaseCustomFieldStrings",
                columns: new[] { "CaseId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_FieldDefinitionId",
                table: "CaseCustomFieldStrings",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseCustomFieldStrings_Value",
                table: "CaseCustomFieldStrings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ConfirmationStatusId",
                table: "Cases",
                column: "ConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_DiseaseId",
                table: "Cases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_HospitalId",
                table: "Cases",
                column: "HospitalId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Jurisdiction1Id",
                table: "Cases",
                column: "Jurisdiction1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Jurisdiction2Id",
                table: "Cases",
                column: "Jurisdiction2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Jurisdiction3Id",
                table: "Cases",
                column: "Jurisdiction3Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Jurisdiction4Id",
                table: "Cases",
                column: "Jurisdiction4Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Jurisdiction5Id",
                table: "Cases",
                column: "Jurisdiction5Id");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_PatientId",
                table: "Cases",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseSymptoms_CaseId_SymptomId",
                table: "CaseSymptoms",
                columns: new[] { "CaseId", "SymptomId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CaseSymptoms_OnsetDate",
                table: "CaseSymptoms",
                column: "OnsetDate",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CaseSymptoms_SymptomId",
                table: "CaseSymptoms",
                column: "SymptomId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_AssignedToUserId",
                table: "CaseTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_CaseId",
                table: "CaseTasks",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_CaseId1",
                table: "CaseTasks",
                column: "CaseId1");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_CompletedByUserId",
                table: "CaseTasks",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_DueDate",
                table: "CaseTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_ParentTaskId",
                table: "CaseTasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_Priority",
                table: "CaseTasks",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_Status",
                table: "CaseTasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_TaskTemplateId",
                table: "CaseTasks",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTasks_TaskTypeId",
                table: "CaseTasks",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_Category_DisplayOrder",
                table: "CustomFieldDefinitions",
                columns: new[] { "Category", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_LookupTableId",
                table: "CustomFieldDefinitions",
                column: "LookupTableId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_Name",
                table: "CustomFieldDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_DisplayOrder",
                table: "DiseaseCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_Name",
                table: "DiseaseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCategories_ReportingId",
                table: "DiseaseCategories",
                column: "ReportingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCustomFields_CustomFieldDefinitionId",
                table: "DiseaseCustomFields",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseCustomFields_DiseaseId_CustomFieldDefinitionId",
                table: "DiseaseCustomFields",
                columns: new[] { "DiseaseId", "CustomFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_Code",
                table: "Diseases",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_DiseaseCategoryId",
                table: "Diseases",
                column: "DiseaseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_ExportCode",
                table: "Diseases",
                column: "ExportCode");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_Level_DisplayOrder",
                table: "Diseases",
                columns: new[] { "Level", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_ParentDiseaseId",
                table: "Diseases",
                column: "ParentDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Diseases_PathIds",
                table: "Diseases",
                column: "PathIds");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseSymptoms_DiseaseId_IsCommon_SortOrder",
                table: "DiseaseSymptoms",
                columns: new[] { "DiseaseId", "IsCommon", "SortOrder" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseSymptoms_DiseaseId_SymptomId",
                table: "DiseaseSymptoms",
                columns: new[] { "DiseaseId", "SymptomId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseSymptoms_SymptomId",
                table: "DiseaseSymptoms",
                column: "SymptomId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_DiseaseId_TaskTemplateId",
                table: "DiseaseTaskTemplates",
                columns: new[] { "DiseaseId", "TaskTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_InheritedFromDiseaseId",
                table: "DiseaseTaskTemplates",
                column: "InheritedFromDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_IsInherited",
                table: "DiseaseTaskTemplates",
                column: "IsInherited");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseTaskTemplates_TaskTemplateId",
                table: "DiseaseTaskTemplates",
                column: "TaskTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventTypeId",
                table: "Events",
                column: "EventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_LocationId",
                table: "Events",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Name",
                table: "Events",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganizerOrganizationId",
                table: "Events",
                column: "OrganizerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDateTime_EndDateTime",
                table: "Events",
                columns: new[] { "StartDateTime", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_City",
                table: "ExposureEvents",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_ContactClassificationId",
                table: "ExposureEvents",
                column: "ContactClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_EventId",
                table: "ExposureEvents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_ExposedCaseId",
                table: "ExposureEvents",
                column: "ExposedCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_ExposureStartDate_ExposureEndDate",
                table: "ExposureEvents",
                columns: new[] { "ExposureStartDate", "ExposureEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_ExposureStatus",
                table: "ExposureEvents",
                column: "ExposureStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_ExposureType",
                table: "ExposureEvents",
                column: "ExposureType");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_IsReportingExposure",
                table: "ExposureEvents",
                column: "IsReportingExposure");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_Latitude_Longitude",
                table: "ExposureEvents",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_LocationId",
                table: "ExposureEvents",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_PostalCode",
                table: "ExposureEvents",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_SourceCaseId",
                table: "ExposureEvents",
                column: "SourceCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExposureEvents_State",
                table: "ExposureEvents",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_Jurisdictions_JurisdictionTypeId",
                table: "Jurisdictions",
                column: "JurisdictionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Jurisdictions_Name",
                table: "Jurisdictions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Jurisdictions_ParentJurisdictionId",
                table: "Jurisdictions",
                column: "ParentJurisdictionId");

            migrationBuilder.CreateIndex(
                name: "IX_JurisdictionTypes_FieldNumber",
                table: "JurisdictionTypes",
                column: "FieldNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JurisdictionTypes_Name",
                table: "JurisdictionTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_AccessionNumber",
                table: "LabResults",
                column: "AccessionNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_CaseId",
                table: "LabResults",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_FriendlyId",
                table: "LabResults",
                column: "FriendlyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_LaboratoryId",
                table: "LabResults",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_OrderingProviderId",
                table: "LabResults",
                column: "OrderingProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_ResultDate",
                table: "LabResults",
                column: "ResultDate");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_ResultUnitsId",
                table: "LabResults",
                column: "ResultUnitsId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_SpecimenCollectionDate",
                table: "LabResults",
                column: "SpecimenCollectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_SpecimenTypeId",
                table: "LabResults",
                column: "SpecimenTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestedDiseaseId",
                table: "LabResults",
                column: "TestedDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestResultId",
                table: "LabResults",
                column: "TestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_TestTypeId",
                table: "LabResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_GeocodingStatus",
                table: "Locations",
                column: "GeocodingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Latitude_Longitude",
                table: "Locations",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationTypeId",
                table: "Locations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_OrganizationId",
                table: "Locations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupTables_Name",
                table: "LookupTables",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_LookupTableId_DisplayOrder",
                table: "LookupValues",
                columns: new[] { "LookupTableId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CaseId",
                table: "Notes",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedAt",
                table: "Notes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedBy",
                table: "Notes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_OutbreakId",
                table: "Notes",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_PatientId",
                table: "Notes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ExportCode",
                table: "Organizations",
                column: "ExportCode");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_FriendlyId",
                table: "Organizations",
                column: "FriendlyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OrganizationTypeId",
                table: "Organizations",
                column: "OrganizationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCaseDefinitions_OutbreakId",
                table: "OutbreakCaseDefinitions",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_CaseId",
                table: "OutbreakCases",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_OutbreakId",
                table: "OutbreakCases",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakCases_SearchQueryId",
                table: "OutbreakCases",
                column: "SearchQueryId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakLineListConfigurations_CreatedByUserId",
                table: "OutbreakLineListConfigurations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakLineListConfigurations_OutbreakId",
                table: "OutbreakLineListConfigurations",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakLineListConfigurations_UserId",
                table: "OutbreakLineListConfigurations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_ConfirmationStatusId",
                table: "Outbreaks",
                column: "ConfirmationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_IndexCaseId",
                table: "Outbreaks",
                column: "IndexCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_LeadInvestigatorId",
                table: "Outbreaks",
                column: "LeadInvestigatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_ParentOutbreakId",
                table: "Outbreaks",
                column: "ParentOutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryDiseaseId",
                table: "Outbreaks",
                column: "PrimaryDiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryEventId",
                table: "Outbreaks",
                column: "PrimaryEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Outbreaks_PrimaryLocationId",
                table: "Outbreaks",
                column: "PrimaryLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakSearchQueries_OutbreakId",
                table: "OutbreakSearchQueries",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTeamMembers_OutbreakId",
                table: "OutbreakTeamMembers",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTeamMembers_UserId",
                table: "OutbreakTeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutbreakTimelines_OutbreakId",
                table: "OutbreakTimelines",
                column: "OutbreakId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldBooleans_FieldDefinitionId",
                table: "PatientCustomFieldBooleans",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldBooleans_PatientId_FieldDefinitionId",
                table: "PatientCustomFieldBooleans",
                columns: new[] { "PatientId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldDates_FieldDefinitionId",
                table: "PatientCustomFieldDates",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldDates_PatientId_FieldDefinitionId",
                table: "PatientCustomFieldDates",
                columns: new[] { "PatientId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldDates_Value",
                table: "PatientCustomFieldDates",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldLookups_FieldDefinitionId",
                table: "PatientCustomFieldLookups",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldLookups_LookupValueId",
                table: "PatientCustomFieldLookups",
                column: "LookupValueId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldLookups_PatientId_FieldDefinitionId",
                table: "PatientCustomFieldLookups",
                columns: new[] { "PatientId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldNumbers_FieldDefinitionId",
                table: "PatientCustomFieldNumbers",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldNumbers_PatientId_FieldDefinitionId",
                table: "PatientCustomFieldNumbers",
                columns: new[] { "PatientId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldNumbers_Value",
                table: "PatientCustomFieldNumbers",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldStrings_FieldDefinitionId",
                table: "PatientCustomFieldStrings",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldStrings_PatientId_FieldDefinitionId",
                table: "PatientCustomFieldStrings",
                columns: new[] { "PatientId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientCustomFieldStrings_Value",
                table: "PatientCustomFieldStrings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AncestryId",
                table: "Patients",
                column: "AncestryId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_AtsiStatusId",
                table: "Patients",
                column: "AtsiStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CountryOfBirthId",
                table: "Patients",
                column: "CountryOfBirthId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CreatedByUserId",
                table: "Patients",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FriendlyId",
                table: "Patients",
                column: "FriendlyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_GenderId",
                table: "Patients",
                column: "GenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Jurisdiction1Id",
                table: "Patients",
                column: "Jurisdiction1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Jurisdiction2Id",
                table: "Patients",
                column: "Jurisdiction2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Jurisdiction3Id",
                table: "Patients",
                column: "Jurisdiction3Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Jurisdiction4Id",
                table: "Patients",
                column: "Jurisdiction4Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_Jurisdiction5Id",
                table: "Patients",
                column: "Jurisdiction5Id");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_LanguageSpokenAtHomeId",
                table: "Patients",
                column: "LanguageSpokenAtHomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_OccupationId",
                table: "Patients",
                column: "OccupationId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_SexAtBirthId",
                table: "Patients",
                column: "SexAtBirthId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module_Action",
                table: "Permissions",
                columns: new[] { "Module", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_Category",
                table: "ReportDefinitions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_CreatedByUserId",
                table: "ReportDefinitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_EntityType",
                table: "ReportDefinitions",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_FolderId",
                table: "ReportDefinitions",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_FieldPath",
                table: "ReportFields",
                column: "FieldPath");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFields_ReportDefinitionId",
                table: "ReportFields",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFilters_ReportDefinitionId",
                table: "ReportFilters",
                column: "ReportDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFolders_ParentFolderId",
                table: "ReportFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFolderShares_GroupId",
                table: "ReportFolderShares",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFolderShares_ReportFolderId",
                table: "ReportFolderShares",
                column: "ReportFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportFolderShares_UserId",
                table: "ReportFolderShares",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_CaseId",
                table: "ReviewQueue",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_CreatedByUserId",
                table: "ReviewQueue",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_DiseaseId",
                table: "ReviewQueue",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_GroupKey_Created",
                table: "ReviewQueue",
                columns: new[] { "GroupKey", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_PatientId",
                table: "ReviewQueue",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_ReviewedByUserId",
                table: "ReviewQueue",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_Status_EntityType_Disease_Created",
                table: "ReviewQueue",
                columns: new[] { "ReviewStatus", "EntityType", "DiseaseId", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewQueue_TaskId",
                table: "ReviewQueue",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_CreatedByUserId",
                table: "RoleDiseaseAccess",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_DiseaseId",
                table: "RoleDiseaseAccess",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleDiseaseAccess_RoleId_DiseaseId",
                table: "RoleDiseaseAccess",
                columns: new[] { "RoleId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMapping_Config_Order",
                table: "SurveyFieldMappings",
                columns: new[] { "ConfigurationType", "ConfigurationId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMapping_Config_Question",
                table: "SurveyFieldMappings",
                columns: new[] { "ConfigurationType", "ConfigurationId", "SurveyQuestionName" });

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMapping_IsActive",
                table: "SurveyFieldMappings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMapping_TargetField",
                table: "SurveyFieldMappings",
                column: "TargetFieldPath");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMappings_CreatedByUserId",
                table: "SurveyFieldMappings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMappings_LastModifiedByUserId",
                table: "SurveyFieldMappings",
                column: "LastModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyFieldMappings_TargetSymptomId",
                table: "SurveyFieldMappings",
                column: "TargetSymptomId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplateDiseases_DiseaseId",
                table: "SurveyTemplateDiseases",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplateDiseases_SurveyTemplateId_DiseaseId",
                table: "SurveyTemplateDiseases",
                columns: new[] { "SurveyTemplateId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_Category",
                table: "SurveyTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_IsActive",
                table: "SurveyTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_Name",
                table: "SurveyTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SurveyTemplates_ParentSurveyTemplateId",
                table: "SurveyTemplates",
                column: "ParentSurveyTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Symptoms_Code",
                table: "Symptoms",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Symptoms_IsDeleted_IsActive_SortOrder",
                table: "Symptoms",
                columns: new[] { "IsDeleted", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskCallAttempts_AttemptedByUserId",
                table: "TaskCallAttempts",
                column: "AttemptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCallAttempts_TaskId",
                table: "TaskCallAttempts",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_IsActive",
                table: "TaskTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_Name",
                table: "TaskTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_SurveyTemplateId",
                table: "TaskTemplates",
                column: "SurveyTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_TaskTypeId",
                table: "TaskTemplates",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTypes_IsActive",
                table: "TaskTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTypes_Name",
                table: "TaskTypes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestTypeId",
                table: "TestResults",
                column: "TestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_DiseaseId",
                table: "UserDiseaseAccess",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_ExpiresAt",
                table: "UserDiseaseAccess",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_GrantedByUserId",
                table: "UserDiseaseAccess",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDiseaseAccess_UserId_DiseaseId",
                table: "UserDiseaseAccess",
                columns: new[] { "UserId", "DiseaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGroups_GroupId",
                table: "UserGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackupHistory");

            migrationBuilder.DropTable(
                name: "CalculatedFields");

            migrationBuilder.DropTable(
                name: "CaseContactTasksFlattened");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldBooleans");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldDates");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldLookups");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldNumbers");

            migrationBuilder.DropTable(
                name: "CaseCustomFieldStrings");

            migrationBuilder.DropTable(
                name: "CaseSymptoms");

            migrationBuilder.DropTable(
                name: "CaseTimelineAll");

            migrationBuilder.DropTable(
                name: "ContactsListSimple");

            migrationBuilder.DropTable(
                name: "ContactTracingMindMapEdges");

            migrationBuilder.DropTable(
                name: "ContactTracingMindMapNodes");

            migrationBuilder.DropTable(
                name: "DiseaseCustomFields");

            migrationBuilder.DropTable(
                name: "DiseaseSymptoms");

            migrationBuilder.DropTable(
                name: "DiseaseTaskTemplates");

            migrationBuilder.DropTable(
                name: "ExposureEvents");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "OutbreakCaseDefinitions");

            migrationBuilder.DropTable(
                name: "OutbreakCases");

            migrationBuilder.DropTable(
                name: "OutbreakLineListConfigurations");

            migrationBuilder.DropTable(
                name: "OutbreakTasksFlattened");

            migrationBuilder.DropTable(
                name: "OutbreakTeamMembers");

            migrationBuilder.DropTable(
                name: "OutbreakTimelines");

            migrationBuilder.DropTable(
                name: "PatientCustomFieldBooleans");

            migrationBuilder.DropTable(
                name: "PatientCustomFieldDates");

            migrationBuilder.DropTable(
                name: "PatientCustomFieldLookups");

            migrationBuilder.DropTable(
                name: "PatientCustomFieldNumbers");

            migrationBuilder.DropTable(
                name: "PatientCustomFieldStrings");

            migrationBuilder.DropTable(
                name: "ReportFields");

            migrationBuilder.DropTable(
                name: "ReportFilters");

            migrationBuilder.DropTable(
                name: "ReportFolderShares");

            migrationBuilder.DropTable(
                name: "ReviewQueue");

            migrationBuilder.DropTable(
                name: "RoleDiseaseAccess");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SurveyFieldMappings");

            migrationBuilder.DropTable(
                name: "SurveyTemplateDiseases");

            migrationBuilder.DropTable(
                name: "TaskCallAttempts");

            migrationBuilder.DropTable(
                name: "UserDiseaseAccess");

            migrationBuilder.DropTable(
                name: "UserGroups");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "ContactClassifications");

            migrationBuilder.DropTable(
                name: "ResultUnits");

            migrationBuilder.DropTable(
                name: "SpecimenTypes");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "OutbreakSearchQueries");

            migrationBuilder.DropTable(
                name: "LookupValues");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Symptoms");

            migrationBuilder.DropTable(
                name: "CaseTasks");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "TestTypes");

            migrationBuilder.DropTable(
                name: "Outbreaks");

            migrationBuilder.DropTable(
                name: "LookupTables");

            migrationBuilder.DropTable(
                name: "ReportFolders");

            migrationBuilder.DropTable(
                name: "TaskTemplates");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "SurveyTemplates");

            migrationBuilder.DropTable(
                name: "TaskTypes");

            migrationBuilder.DropTable(
                name: "CaseStatuses");

            migrationBuilder.DropTable(
                name: "Diseases");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "EventTypes");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "DiseaseCategories");

            migrationBuilder.DropTable(
                name: "Ancestries");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "AtsiStatuses");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Genders");

            migrationBuilder.DropTable(
                name: "Jurisdictions");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "Occupations");

            migrationBuilder.DropTable(
                name: "SexAtBirths");

            migrationBuilder.DropTable(
                name: "LocationTypes");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "JurisdictionTypes");

            migrationBuilder.DropTable(
                name: "OrganizationTypes");
        }
    }
}
