using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sentinel.Migrations
{
    /// <inheritdoc />
    public partial class AddHL7IntakeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiseaseReinfectionRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiseaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    ReinfectionWindowDays = table.Column<int>(type: "int", nullable: true),
                    IsChronic = table.Column<bool>(type: "bit", nullable: false),
                    AlwaysCreateNewCase = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CaseMatchingStrategy = table.Column<int>(type: "int", nullable: false),
                    MatchOnTestType = table.Column<bool>(type: "bit", nullable: false),
                    MatchOnResultType = table.Column<bool>(type: "bit", nullable: false),
                    RequireConfirmationForNewCase = table.Column<bool>(type: "bit", nullable: false),
                    NotificationMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiseaseReinfectionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseReinfectionRules_Diseases_DiseaseId",
                        column: x => x.DiseaseId,
                        principalTable: "Diseases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HL7Configurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SendingFacility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SendingApplication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileDropPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FilePattern = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CharacterEncoding = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultLaboratoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AutoCreateOrganizations = table.Column<bool>(type: "bit", nullable: false),
                    PatientMatchingStrategy = table.Column<int>(type: "int", nullable: false),
                    AutoCreatePatients = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateCases = table.Column<bool>(type: "bit", nullable: false),
                    DuplicateDetectionWindowHours = table.Column<int>(type: "int", nullable: false),
                    DuplicateDetectionStrategy = table.Column<int>(type: "int", nullable: false),
                    FieldMappingConfig = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessOnReceipt = table.Column<bool>(type: "bit", nullable: false),
                    ArchiveProcessedFiles = table.Column<bool>(type: "bit", nullable: false),
                    ArchivePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DeleteAfterArchive = table.Column<bool>(type: "bit", nullable: false),
                    SendNotificationsOnError = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEmailAddresses = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultDateFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimezoneOffset = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequirePatientIdentifier = table.Column<bool>(type: "bit", nullable: false),
                    RequireSpecimenCollectionDate = table.Column<bool>(type: "bit", nullable: false),
                    RequireResultDate = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7Configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7Configurations_Organizations_DefaultLaboratoryId",
                        column: x => x.DefaultLaboratoryId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "HL7Messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageControlId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SendingFacility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SendingApplication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivingFacility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivingApplication = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HL7Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RawMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LabResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LaboratoryOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderingProviderOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    DuplicateOfMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DuplicateDetectionMethod = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequiresManualReview = table.Column<bool>(type: "bit", nullable: false),
                    ManualReviewCompleted = table.Column<bool>(type: "bit", nullable: false),
                    ManualReviewByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ManualReviewDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManualReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7Messages_AspNetUsers_ManualReviewByUserId",
                        column: x => x.ManualReviewByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_HL7Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "HL7Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HL7Messages_HL7Messages_DuplicateOfMessageId",
                        column: x => x.DuplicateOfMessageId,
                        principalTable: "HL7Messages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_LabResults_LabResultId",
                        column: x => x.LabResultId,
                        principalTable: "LabResults",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_Organizations_LaboratoryOrganizationId",
                        column: x => x.LaboratoryOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_Organizations_OrderingProviderOrganizationId",
                        column: x => x.OrderingProviderOrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7Messages_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HL7MessageSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HL7MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: true),
                    RawSegment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsParsed = table.Column<bool>(type: "bit", nullable: false),
                    ParsedData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldCount = table.Column<int>(type: "int", nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HasIssues = table.Column<bool>(type: "bit", nullable: false),
                    ParsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7MessageSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7MessageSegments_HL7Messages_HL7MessageId",
                        column: x => x.HL7MessageId,
                        principalTable: "HL7Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HL7FieldMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SegmentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TargetEntity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetProperty = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MappingType = table.Column<int>(type: "int", nullable: false),
                    TransformationRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LookupTable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CodeMappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ValidationRegex = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExampleHL7Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExampleMappedValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    TimesFailed = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedFromIssueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7FieldMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7FieldMappings_HL7Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "HL7Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HL7ParsingIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HL7MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageSegmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SegmentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RawValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExpectedFormat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SuggestedMapping = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FieldMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IgnoreFutureOccurrences = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HL7ParsingIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HL7ParsingIssues_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7ParsingIssues_HL7FieldMappings_FieldMappingId",
                        column: x => x.FieldMappingId,
                        principalTable: "HL7FieldMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HL7ParsingIssues_HL7MessageSegments_MessageSegmentId",
                        column: x => x.MessageSegmentId,
                        principalTable: "HL7MessageSegments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HL7ParsingIssues_HL7Messages_HL7MessageId",
                        column: x => x.HL7MessageId,
                        principalTable: "HL7Messages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseReinfectionRules_DiseaseId",
                table: "DiseaseReinfectionRules",
                column: "DiseaseId");

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseReinfectionRules_IsActive",
                table: "DiseaseReinfectionRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Configurations_DefaultLaboratoryId",
                table: "HL7Configurations",
                column: "DefaultLaboratoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Configurations_IsActive",
                table: "HL7Configurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Configurations_SendingFacility",
                table: "HL7Configurations",
                column: "SendingFacility");

            migrationBuilder.CreateIndex(
                name: "IX_HL7FieldMappings_ConfigurationId_SegmentType_FieldPath",
                table: "HL7FieldMappings",
                columns: new[] { "ConfigurationId", "SegmentType", "FieldPath" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7FieldMappings_CreatedFromIssueId",
                table: "HL7FieldMappings",
                column: "CreatedFromIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7FieldMappings_IsActive_Priority",
                table: "HL7FieldMappings",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_CaseId",
                table: "HL7Messages",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_ConfigurationId",
                table: "HL7Messages",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_DuplicateOfMessageId",
                table: "HL7Messages",
                column: "DuplicateOfMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_IsDuplicate",
                table: "HL7Messages",
                column: "IsDuplicate");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_LaboratoryOrganizationId",
                table: "HL7Messages",
                column: "LaboratoryOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_LabResultId",
                table: "HL7Messages",
                column: "LabResultId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_ManualReviewByUserId",
                table: "HL7Messages",
                column: "ManualReviewByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_MessageControlId",
                table: "HL7Messages",
                column: "MessageControlId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_OrderingProviderOrganizationId",
                table: "HL7Messages",
                column: "OrderingProviderOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_PatientId",
                table: "HL7Messages",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_ProcessedByUserId",
                table: "HL7Messages",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_ReceivedAt",
                table: "HL7Messages",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_RequiresManualReview",
                table: "HL7Messages",
                column: "RequiresManualReview");

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_SendingFacility_MessageControlId",
                table: "HL7Messages",
                columns: new[] { "SendingFacility", "MessageControlId" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7Messages_Status",
                table: "HL7Messages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HL7MessageSegments_HL7MessageId_SegmentType",
                table: "HL7MessageSegments",
                columns: new[] { "HL7MessageId", "SegmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7MessageSegments_HL7MessageId_SequenceNumber",
                table: "HL7MessageSegments",
                columns: new[] { "HL7MessageId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_CreatedAt",
                table: "HL7ParsingIssues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_FieldMappingId",
                table: "HL7ParsingIssues",
                column: "FieldMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_HL7MessageId",
                table: "HL7ParsingIssues",
                column: "HL7MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_IsResolved_IssueType",
                table: "HL7ParsingIssues",
                columns: new[] { "IsResolved", "IssueType" });

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_MessageSegmentId",
                table: "HL7ParsingIssues",
                column: "MessageSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HL7ParsingIssues_ResolvedByUserId",
                table: "HL7ParsingIssues",
                column: "ResolvedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_HL7FieldMappings_HL7ParsingIssues_CreatedFromIssueId",
                table: "HL7FieldMappings",
                column: "CreatedFromIssueId",
                principalTable: "HL7ParsingIssues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HL7FieldMappings_HL7Configurations_ConfigurationId",
                table: "HL7FieldMappings");

            migrationBuilder.DropForeignKey(
                name: "FK_HL7Messages_HL7Configurations_ConfigurationId",
                table: "HL7Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_HL7FieldMappings_HL7ParsingIssues_CreatedFromIssueId",
                table: "HL7FieldMappings");

            migrationBuilder.DropTable(
                name: "DiseaseReinfectionRules");

            migrationBuilder.DropTable(
                name: "HL7Configurations");

            migrationBuilder.DropTable(
                name: "HL7ParsingIssues");

            migrationBuilder.DropTable(
                name: "HL7FieldMappings");

            migrationBuilder.DropTable(
                name: "HL7MessageSegments");

            migrationBuilder.DropTable(
                name: "HL7Messages");
        }
    }
}
