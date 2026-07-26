using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sentinel.Models.Generated;

namespace Sentinel.Data;

public partial class AspnetSentinelA57ac8baCcc64d7aB380485d37f1148dContext : DbContext
{
    public AspnetSentinelA57ac8baCcc64d7aB380485d37f1148dContext(DbContextOptions<AspnetSentinelA57ac8baCcc64d7aB380485d37f1148dContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ancestry> Ancestries { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<AtsiStatus> AtsiStatuses { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BackupHistory> BackupHistories { get; set; }

    public virtual DbSet<CalculatedField> CalculatedFields { get; set; }

    public virtual DbSet<Case> Cases { get; set; }

    public virtual DbSet<CaseClassificationHistory> CaseClassificationHistories { get; set; }

    public virtual DbSet<CaseContactTasksFlattened> CaseContactTasksFlatteneds { get; set; }

    public virtual DbSet<CaseCustomFieldBoolean> CaseCustomFieldBooleans { get; set; }

    public virtual DbSet<CaseCustomFieldDate> CaseCustomFieldDates { get; set; }

    public virtual DbSet<CaseCustomFieldLookup> CaseCustomFieldLookups { get; set; }

    public virtual DbSet<CaseCustomFieldNumber> CaseCustomFieldNumbers { get; set; }

    public virtual DbSet<CaseCustomFieldString> CaseCustomFieldStrings { get; set; }

    public virtual DbSet<CaseDefinition> CaseDefinitions { get; set; }

    public virtual DbSet<CaseDefinitionCriterion> CaseDefinitionCriteria { get; set; }

    public virtual DbSet<CaseStatus> CaseStatuses { get; set; }

    public virtual DbSet<CaseSymptom> CaseSymptoms { get; set; }

    public virtual DbSet<CaseTask> CaseTasks { get; set; }

    public virtual DbSet<CaseTimelineAll> CaseTimelineAlls { get; set; }

    public virtual DbSet<ContactClassification> ContactClassifications { get; set; }

    public virtual DbSet<ContactTracingMindMapEdge> ContactTracingMindMapEdges { get; set; }

    public virtual DbSet<ContactTracingMindMapNode> ContactTracingMindMapNodes { get; set; }

    public virtual DbSet<ContactsListSimple> ContactsListSimples { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; set; }

    public virtual DbSet<Disease> Diseases { get; set; }

    public virtual DbSet<DiseaseCategory> DiseaseCategories { get; set; }

    public virtual DbSet<DiseaseCustomField> DiseaseCustomFields { get; set; }

    public virtual DbSet<DiseaseHl7matchingConfig> DiseaseHl7matchingConfigs { get; set; }

    public virtual DbSet<DiseaseReinfectionRule> DiseaseReinfectionRules { get; set; }

    public virtual DbSet<DiseaseSymptom> DiseaseSymptoms { get; set; }

    public virtual DbSet<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<EventType> EventTypes { get; set; }

    public virtual DbSet<ExposureEvent> ExposureEvents { get; set; }

    public virtual DbSet<Gender> Genders { get; set; }

    public virtual DbSet<GeocodingQueueItem> GeocodingQueueItems { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Hl7configuration> Hl7configurations { get; set; }

    public virtual DbSet<Hl7configurationDisease> Hl7configurationDiseases { get; set; }

    public virtual DbSet<Hl7customFieldMapping> Hl7customFieldMappings { get; set; }

    public virtual DbSet<Hl7fieldMapping> Hl7fieldMappings { get; set; }

    public virtual DbSet<Hl7message> Hl7messages { get; set; }

    public virtual DbSet<Hl7messageSegment> Hl7messageSegments { get; set; }

    public virtual DbSet<Hl7parsingIssue> Hl7parsingIssues { get; set; }

    public virtual DbSet<Hl7testMessageHistory> Hl7testMessageHistories { get; set; }

    public virtual DbSet<Hl7testMessageTemplate> Hl7testMessageTemplates { get; set; }

    public virtual DbSet<Jurisdiction> Jurisdictions { get; set; }

    public virtual DbSet<JurisdictionType> JurisdictionTypes { get; set; }

    public virtual DbSet<LabResult> LabResults { get; set; }

    public virtual DbSet<LabResultMarker> LabResultMarkers { get; set; }

    public virtual DbSet<LabResultMarkerHistory> LabResultMarkerHistories { get; set; }

    public virtual DbSet<Language> Languages { get; set; }

    public virtual DbSet<Location> Locations { get; set; }

    public virtual DbSet<LocationType> LocationTypes { get; set; }

    public virtual DbSet<LookupTable> LookupTables { get; set; }

    public virtual DbSet<LookupValue> LookupValues { get; set; }

    public virtual DbSet<Note> Notes { get; set; }

    public virtual DbSet<Occupation> Occupations { get; set; }

    public virtual DbSet<Organization> Organizations { get; set; }

    public virtual DbSet<OrganizationType> OrganizationTypes { get; set; }

    public virtual DbSet<Outbreak> Outbreaks { get; set; }

    public virtual DbSet<OutbreakCase> OutbreakCases { get; set; }

    public virtual DbSet<OutbreakCaseDefinition> OutbreakCaseDefinitions { get; set; }

    public virtual DbSet<OutbreakLineListConfiguration> OutbreakLineListConfigurations { get; set; }

    public virtual DbSet<OutbreakSearchQuery> OutbreakSearchQueries { get; set; }

    public virtual DbSet<OutbreakTasksFlattened> OutbreakTasksFlatteneds { get; set; }

    public virtual DbSet<OutbreakTeamMember> OutbreakTeamMembers { get; set; }

    public virtual DbSet<OutbreakTimeline> OutbreakTimelines { get; set; }

    public virtual DbSet<Pathogen> Pathogens { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientCustomFieldBoolean> PatientCustomFieldBooleans { get; set; }

    public virtual DbSet<PatientCustomFieldDate> PatientCustomFieldDates { get; set; }

    public virtual DbSet<PatientCustomFieldLookup> PatientCustomFieldLookups { get; set; }

    public virtual DbSet<PatientCustomFieldNumber> PatientCustomFieldNumbers { get; set; }

    public virtual DbSet<PatientCustomFieldString> PatientCustomFieldStrings { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<ReportDefinition> ReportDefinitions { get; set; }

    public virtual DbSet<ReportField> ReportFields { get; set; }

    public virtual DbSet<ReportFilter> ReportFilters { get; set; }

    public virtual DbSet<ReportFolder> ReportFolders { get; set; }

    public virtual DbSet<ReportFolderShare> ReportFolderShares { get; set; }

    public virtual DbSet<ResultUnit> ResultUnits { get; set; }

    public virtual DbSet<ReviewQueue> ReviewQueues { get; set; }

    public virtual DbSet<RoleDiseaseAccess> RoleDiseaseAccesses { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<SexAtBirth> SexAtBirths { get; set; }

    public virtual DbSet<SpecimenType> SpecimenTypes { get; set; }

    public virtual DbSet<State> States { get; set; }

    public virtual DbSet<SurveyFieldMapping> SurveyFieldMappings { get; set; }

    public virtual DbSet<SurveySubmissionLog> SurveySubmissionLogs { get; set; }

    public virtual DbSet<SurveyTemplate> SurveyTemplates { get; set; }

    public virtual DbSet<SurveyTemplateDisease> SurveyTemplateDiseases { get; set; }

    public virtual DbSet<Symptom> Symptoms { get; set; }

    public virtual DbSet<TaskCallAttempt> TaskCallAttempts { get; set; }

    public virtual DbSet<TaskTemplate> TaskTemplates { get; set; }

    public virtual DbSet<TaskType> TaskTypes { get; set; }

    public virtual DbSet<TestMethod> TestMethods { get; set; }

    public virtual DbSet<TestResult> TestResults { get; set; }

    public virtual DbSet<TestType> TestTypes { get; set; }

    public virtual DbSet<UserDiseaseAccess> UserDiseaseAccesses { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<VwCaseContactTasksFlattened> VwCaseContactTasksFlatteneds { get; set; }

    public virtual DbSet<VwCaseTimelineAll> VwCaseTimelineAlls { get; set; }

    public virtual DbSet<VwContactTracingMindMapEdge> VwContactTracingMindMapEdges { get; set; }

    public virtual DbSet<VwContactTracingMindMapNode> VwContactTracingMindMapNodes { get; set; }

    public virtual DbSet<VwContactsListSimple> VwContactsListSimples { get; set; }

    public virtual DbSet<VwOutbreakTasksFlattened> VwOutbreakTasksFlatteneds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedName] IS NOT NULL)");

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex")
                .IsUnique()
                .HasFilter("([NormalizedUserName] IS NOT NULL)");

            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PrimaryLanguage).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Groups).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserGroup",
                    r => r.HasOne<Group>().WithMany().HasForeignKey("GroupId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "GroupId");
                        j.ToTable("UserGroups");
                        j.HasIndex(new[] { "GroupId" }, "IX_UserGroups_GroupId");
                    });

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.ProviderKey).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.Property(e => e.LoginProvider).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.ChangedAt, "IX_AuditLogs_ChangedAt");

            entity.HasIndex(e => e.ChangedByUserId, "IX_AuditLogs_ChangedByUserId");

            entity.HasIndex(e => new { e.EntityType, e.EntityId }, "IX_AuditLogs_EntityType_EntityId");

            entity.Property(e => e.Action).HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.FieldName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BackupHistory>(entity =>
        {
            entity.ToTable("BackupHistory");

            entity.Property(e => e.BackupFileName).HasMaxLength(500);
            entity.Property(e => e.BackupFilePath).HasMaxLength(1000);
            entity.Property(e => e.BackupType).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<CalculatedField>(entity =>
        {
            entity.HasIndex(e => e.ReportDefinitionId, "IX_CalculatedFields_ReportDefinitionId");

            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.ReportDefinition).WithMany(p => p.CalculatedFields).HasForeignKey(d => d.ReportDefinitionId);
        });

        modelBuilder.Entity<Case>(entity =>
        {
            entity.HasIndex(e => e.CaseStateId, "IX_Cases_CaseStateId");

            entity.HasIndex(e => e.ConfirmationStatusId, "IX_Cases_ConfirmationStatusId");

            entity.HasIndex(e => e.DiseaseId, "IX_Cases_DiseaseId");

            entity.HasIndex(e => e.HospitalId, "IX_Cases_HospitalId");

            entity.HasIndex(e => e.Jurisdiction1Id, "IX_Cases_Jurisdiction1Id");

            entity.HasIndex(e => e.Jurisdiction2Id, "IX_Cases_Jurisdiction2Id");

            entity.HasIndex(e => e.Jurisdiction3Id, "IX_Cases_Jurisdiction3Id");

            entity.HasIndex(e => e.Jurisdiction4Id, "IX_Cases_Jurisdiction4Id");

            entity.HasIndex(e => e.Jurisdiction5Id, "IX_Cases_Jurisdiction5Id");

            entity.HasIndex(e => e.PatientId, "IX_Cases_PatientId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CaseAddressLine).HasMaxLength(500);
            entity.Property(e => e.CaseCity).HasMaxLength(200);
            entity.Property(e => e.CasePostalCode).HasMaxLength(20);
            entity.Property(e => e.ClinicalNotificationNotes).HasMaxLength(1000);
            entity.Property(e => e.ClinicalNotifierOrganisation).HasMaxLength(200);
            entity.Property(e => e.ConfirmationStatusClassifiedBy).HasMaxLength(450);
            entity.Property(e => e.ConfirmationStatusManualOverrideByUserId).HasMaxLength(450);
            entity.Property(e => e.FriendlyId).HasMaxLength(20);

            entity.HasOne(d => d.CaseState).WithMany(p => p.Cases).HasForeignKey(d => d.CaseStateId);

            entity.HasOne(d => d.ConfirmationStatus).WithMany(p => p.Cases).HasForeignKey(d => d.ConfirmationStatusId);

            entity.HasOne(d => d.Disease).WithMany(p => p.Cases).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.Hospital).WithMany(p => p.Cases).HasForeignKey(d => d.HospitalId);

            entity.HasOne(d => d.Jurisdiction1).WithMany(p => p.CaseJurisdiction1s).HasForeignKey(d => d.Jurisdiction1Id);

            entity.HasOne(d => d.Jurisdiction2).WithMany(p => p.CaseJurisdiction2s).HasForeignKey(d => d.Jurisdiction2Id);

            entity.HasOne(d => d.Jurisdiction3).WithMany(p => p.CaseJurisdiction3s).HasForeignKey(d => d.Jurisdiction3Id);

            entity.HasOne(d => d.Jurisdiction4).WithMany(p => p.CaseJurisdiction4s).HasForeignKey(d => d.Jurisdiction4Id);

            entity.HasOne(d => d.Jurisdiction5).WithMany(p => p.CaseJurisdiction5s).HasForeignKey(d => d.Jurisdiction5Id);

            entity.HasOne(d => d.Patient).WithMany(p => p.Cases).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<CaseClassificationHistory>(entity =>
        {
            entity.ToTable("CaseClassificationHistory");

            entity.HasIndex(e => e.CaseDefinitionId, "IX_CaseClassificationHistory_CaseDefinitionId");

            entity.HasIndex(e => e.CaseId, "IX_CaseClassificationHistory_CaseId");

            entity.HasIndex(e => new { e.CaseId, e.IsCurrent }, "IX_CaseClassificationHistory_CaseId_IsCurrent");

            entity.HasIndex(e => e.ClassifiedDate, "IX_CaseClassificationHistory_ClassifiedDate");

            entity.HasIndex(e => e.FromConfirmationStatusId, "IX_CaseClassificationHistory_FromConfirmationStatusId");

            entity.HasIndex(e => e.IsAutoClassified, "IX_CaseClassificationHistory_IsAutoClassified");

            entity.HasIndex(e => e.ToConfirmationStatusId, "IX_CaseClassificationHistory_ToConfirmationStatusId");

            entity.Property(e => e.ClassifiedByUserId).HasMaxLength(450);

            entity.HasOne(d => d.CaseDefinition).WithMany(p => p.CaseClassificationHistories).HasForeignKey(d => d.CaseDefinitionId);

            entity.HasOne(d => d.Case).WithMany(p => p.CaseClassificationHistories).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FromConfirmationStatus).WithMany(p => p.CaseClassificationHistoryFromConfirmationStatuses).HasForeignKey(d => d.FromConfirmationStatusId);

            entity.HasOne(d => d.ToConfirmationStatus).WithMany(p => p.CaseClassificationHistoryToConfirmationStatuses)
                .HasForeignKey(d => d.ToConfirmationStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CaseContactTasksFlattened>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CaseContactTasksFlattened");

            entity.Property(e => e.PatientDob).HasColumnName("PatientDOB");
        });

        modelBuilder.Entity<CaseCustomFieldBoolean>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.FieldDefinitionId }, "IX_CaseCustomFieldBooleans_CaseId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.FieldDefinitionId, "IX_CaseCustomFieldBooleans_FieldDefinitionId");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseCustomFieldBooleans).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.CaseCustomFieldBooleans).HasForeignKey(d => d.FieldDefinitionId);
        });

        modelBuilder.Entity<CaseCustomFieldDate>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.FieldDefinitionId }, "IX_CaseCustomFieldDates_CaseId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.FieldDefinitionId, "IX_CaseCustomFieldDates_FieldDefinitionId");

            entity.HasIndex(e => e.Value, "IX_CaseCustomFieldDates_Value");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseCustomFieldDates).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.CaseCustomFieldDates).HasForeignKey(d => d.FieldDefinitionId);
        });

        modelBuilder.Entity<CaseCustomFieldLookup>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.FieldDefinitionId }, "IX_CaseCustomFieldLookups_CaseId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.FieldDefinitionId, "IX_CaseCustomFieldLookups_FieldDefinitionId");

            entity.HasIndex(e => e.LookupValueId, "IX_CaseCustomFieldLookups_LookupValueId");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseCustomFieldLookups).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.CaseCustomFieldLookups).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.LookupValue).WithMany(p => p.CaseCustomFieldLookups).HasForeignKey(d => d.LookupValueId);
        });

        modelBuilder.Entity<CaseCustomFieldNumber>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.FieldDefinitionId }, "IX_CaseCustomFieldNumbers_CaseId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.FieldDefinitionId, "IX_CaseCustomFieldNumbers_FieldDefinitionId");

            entity.HasIndex(e => e.Value, "IX_CaseCustomFieldNumbers_Value");

            entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseCustomFieldNumbers).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.CaseCustomFieldNumbers).HasForeignKey(d => d.FieldDefinitionId);
        });

        modelBuilder.Entity<CaseCustomFieldString>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.FieldDefinitionId }, "IX_CaseCustomFieldStrings_CaseId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.FieldDefinitionId, "IX_CaseCustomFieldStrings_FieldDefinitionId");

            entity.HasIndex(e => e.Value, "IX_CaseCustomFieldStrings_Value");

            entity.HasOne(d => d.Case).WithMany(p => p.CaseCustomFieldStrings).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.CaseCustomFieldStrings).HasForeignKey(d => d.FieldDefinitionId);
        });

        modelBuilder.Entity<CaseDefinition>(entity =>
        {
            entity.HasIndex(e => e.ConfirmationStatusId, "IX_CaseDefinitions_ConfirmationStatusId");

            entity.HasIndex(e => e.DateActiveFrom, "IX_CaseDefinitions_DateActiveFrom");

            entity.HasIndex(e => e.DateActiveTo, "IX_CaseDefinitions_DateActiveTo");

            entity.HasIndex(e => new { e.DiseaseId, e.ConfirmationStatusId }, "IX_CaseDefinitions_DiseaseId_ConfirmationStatusId");

            entity.HasIndex(e => e.Status, "IX_CaseDefinitions_Status");

            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            entity.Property(e => e.ModifiedBy).HasMaxLength(450);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.ConfirmationStatus).WithMany(p => p.CaseDefinitions)
                .HasForeignKey(d => d.ConfirmationStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Disease).WithMany(p => p.CaseDefinitions)
                .HasForeignKey(d => d.DiseaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CaseDefinitionCriterion>(entity =>
        {
            entity.HasIndex(e => e.CanonicalPathogenId, "IX_CaseDefinitionCriteria_CanonicalPathogenId");

            entity.HasIndex(e => e.CanonicalSpecimenTypeId, "IX_CaseDefinitionCriteria_CanonicalSpecimenTypeId");

            entity.HasIndex(e => e.CanonicalTestMethodId, "IX_CaseDefinitionCriteria_CanonicalTestMethodId");

            entity.HasIndex(e => e.CanonicalTestResultId, "IX_CaseDefinitionCriteria_CanonicalTestResultId");

            entity.HasIndex(e => e.CaseDefinitionId, "IX_CaseDefinitionCriteria_CaseDefinitionId");

            entity.HasIndex(e => new { e.CaseDefinitionId, e.GroupNumber }, "IX_CaseDefinitionCriteria_CaseDefinitionId_GroupNumber");

            entity.HasIndex(e => e.ParentCriteriaId, "IX_CaseDefinitionCriteria_ParentCriteriaId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DisplayText).HasMaxLength(500);
            entity.Property(e => e.FieldPath).HasMaxLength(200);

            entity.HasOne(d => d.CanonicalPathogen).WithMany(p => p.CaseDefinitionCriteria).HasForeignKey(d => d.CanonicalPathogenId);

            entity.HasOne(d => d.CanonicalSpecimenType).WithMany(p => p.CaseDefinitionCriteria).HasForeignKey(d => d.CanonicalSpecimenTypeId);

            entity.HasOne(d => d.CanonicalTestMethod).WithMany(p => p.CaseDefinitionCriteria).HasForeignKey(d => d.CanonicalTestMethodId);

            entity.HasOne(d => d.CanonicalTestResult).WithMany(p => p.CaseDefinitionCriteria).HasForeignKey(d => d.CanonicalTestResultId);

            entity.HasOne(d => d.CaseDefinition).WithMany(p => p.CaseDefinitionCriteria).HasForeignKey(d => d.CaseDefinitionId);

            entity.HasOne(d => d.ParentCriteria).WithMany(p => p.InverseParentCriteria).HasForeignKey(d => d.ParentCriteriaId);
        });

        modelBuilder.Entity<CaseSymptom>(entity =>
        {
            entity.HasIndex(e => new { e.CaseId, e.SymptomId }, "IX_CaseSymptoms_CaseId_SymptomId").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.OnsetDate, "IX_CaseSymptoms_OnsetDate").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.SymptomId, "IX_CaseSymptoms_SymptomId");

            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OtherSymptomText).HasMaxLength(200);
            entity.Property(e => e.Severity).HasMaxLength(20);

            entity.HasOne(d => d.Case).WithMany(p => p.CaseSymptoms).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.Symptom).WithMany(p => p.CaseSymptoms)
                .HasForeignKey(d => d.SymptomId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CaseTask>(entity =>
        {
            entity.HasIndex(e => e.AssignedToUserId, "IX_CaseTasks_AssignedToUserId");

            entity.HasIndex(e => e.CaseId, "IX_CaseTasks_CaseId");

            entity.HasIndex(e => e.CaseId1, "IX_CaseTasks_CaseId1");

            entity.HasIndex(e => e.CompletedByUserId, "IX_CaseTasks_CompletedByUserId");

            entity.HasIndex(e => e.DueDate, "IX_CaseTasks_DueDate");

            entity.HasIndex(e => e.ParentTaskId, "IX_CaseTasks_ParentTaskId");

            entity.HasIndex(e => e.Priority, "IX_CaseTasks_Priority");

            entity.HasIndex(e => e.Status, "IX_CaseTasks_Status");

            entity.HasIndex(e => e.TaskTemplateId, "IX_CaseTasks_TaskTemplateId");

            entity.HasIndex(e => e.TaskTypeId, "IX_CaseTasks_TaskTypeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CancellationReason).HasMaxLength(1000);
            entity.Property(e => e.CompletionNotes).HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.EvidenceFileIds).HasMaxLength(2000);
            entity.Property(e => e.LanguageRequired).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.AssignedToUser).WithMany(p => p.CaseTaskAssignedToUsers).HasForeignKey(d => d.AssignedToUserId);

            entity.HasOne(d => d.Case).WithMany(p => p.CaseTaskCases)
                .HasForeignKey(d => d.CaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.CaseId1Navigation).WithMany(p => p.CaseTaskCaseId1Navigations).HasForeignKey(d => d.CaseId1);

            entity.HasOne(d => d.CompletedByUser).WithMany(p => p.CaseTaskCompletedByUsers).HasForeignKey(d => d.CompletedByUserId);

            entity.HasOne(d => d.ParentTask).WithMany(p => p.InverseParentTask).HasForeignKey(d => d.ParentTaskId);

            entity.HasOne(d => d.TaskTemplate).WithMany(p => p.CaseTasks)
                .HasForeignKey(d => d.TaskTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.TaskType).WithMany(p => p.CaseTasks)
                .HasForeignKey(d => d.TaskTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CaseTimelineAll>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CaseTimelineAll");
        });

        modelBuilder.Entity<ContactClassification>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ContactTracingMindMapEdge>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<ContactTracingMindMapNode>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<ContactsListSimple>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("ContactsListSimple");

            entity.Property(e => e.ContactDob).HasColumnName("ContactDOB");
        });

        modelBuilder.Entity<CustomFieldDefinition>(entity =>
        {
            entity.HasIndex(e => new { e.Category, e.DisplayOrder }, "IX_CustomFieldDefinitions_Category_DisplayOrder");

            entity.HasIndex(e => e.LookupTableId, "IX_CustomFieldDefinitions_LookupTableId");

            entity.HasIndex(e => e.Name, "IX_CustomFieldDefinitions_Name").IsUnique();

            entity.HasOne(d => d.LookupTable).WithMany(p => p.CustomFieldDefinitions).HasForeignKey(d => d.LookupTableId);
        });

        modelBuilder.Entity<Disease>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_Diseases_Code").IsUnique();

            entity.HasIndex(e => e.DiseaseCategoryId, "IX_Diseases_DiseaseCategoryId");

            entity.HasIndex(e => e.ExportCode, "IX_Diseases_ExportCode");

            entity.HasIndex(e => new { e.Level, e.DisplayOrder }, "IX_Diseases_Level_DisplayOrder");

            entity.HasIndex(e => e.ParentDiseaseId, "IX_Diseases_ParentDiseaseId");

            entity.HasIndex(e => e.PathIds, "IX_Diseases_PathIds");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.ExposureGuidanceText).HasMaxLength(1000);
            entity.Property(e => e.InheritAddressSettingsFromParent).HasDefaultValue(true);
            entity.Property(e => e.JurisdictionFieldsToCheck).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PathIds).HasMaxLength(4000);
            entity.Property(e => e.RequiredLocationTypeIds).HasMaxLength(500);

            entity.HasOne(d => d.DiseaseCategory).WithMany(p => p.Diseases).HasForeignKey(d => d.DiseaseCategoryId);

            entity.HasOne(d => d.ParentDisease).WithMany(p => p.InverseParentDisease).HasForeignKey(d => d.ParentDiseaseId);
        });

        modelBuilder.Entity<DiseaseCategory>(entity =>
        {
            entity.HasIndex(e => e.DisplayOrder, "IX_DiseaseCategories_DisplayOrder");

            entity.HasIndex(e => e.Name, "IX_DiseaseCategories_Name").IsUnique();

            entity.HasIndex(e => e.ReportingId, "IX_DiseaseCategories_ReportingId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ReportingId).HasMaxLength(50);
        });

        modelBuilder.Entity<DiseaseCustomField>(entity =>
        {
            entity.HasIndex(e => e.CustomFieldDefinitionId, "IX_DiseaseCustomFields_CustomFieldDefinitionId");

            entity.HasIndex(e => new { e.DiseaseId, e.CustomFieldDefinitionId }, "IX_DiseaseCustomFields_DiseaseId_CustomFieldDefinitionId").IsUnique();

            entity.HasOne(d => d.CustomFieldDefinition).WithMany(p => p.DiseaseCustomFields).HasForeignKey(d => d.CustomFieldDefinitionId);

            entity.HasOne(d => d.Disease).WithMany(p => p.DiseaseCustomFields).HasForeignKey(d => d.DiseaseId);
        });

        modelBuilder.Entity<DiseaseHl7matchingConfig>(entity =>
        {
            entity.HasKey(e => e.DiseaseId);

            entity.ToTable("DiseaseHL7MatchingConfigs");

            entity.HasIndex(e => e.PartialMatchConfirmationStatusId, "IX_DiseaseHL7MatchingConfigs_PartialMatchConfirmationStatusId");

            entity.Property(e => e.DiseaseId).ValueGeneratedNever();
            entity.Property(e => e.MaxMissingFieldsAllowed).HasDefaultValue(1);
            entity.Property(e => e.PathogenCaseInsensitive).HasColumnName("Pathogen_CaseInsensitive");
            entity.Property(e => e.PathogenIgnorePunctuation).HasColumnName("Pathogen_IgnorePunctuation");
            entity.Property(e => e.PathogenNormalizeWhitespace).HasColumnName("Pathogen_NormalizeWhitespace");
            entity.Property(e => e.PathogenUseTextFallback).HasColumnName("Pathogen_UseTextFallback");
            entity.Property(e => e.SpecimenTypeCaseInsensitive).HasColumnName("SpecimenType_CaseInsensitive");
            entity.Property(e => e.SpecimenTypeIgnorePunctuation).HasColumnName("SpecimenType_IgnorePunctuation");
            entity.Property(e => e.SpecimenTypeNormalizeWhitespace).HasColumnName("SpecimenType_NormalizeWhitespace");
            entity.Property(e => e.SpecimenTypeUseTextFallback).HasColumnName("SpecimenType_UseTextFallback");
            entity.Property(e => e.TestMethodCaseInsensitive).HasColumnName("TestMethod_CaseInsensitive");
            entity.Property(e => e.TestMethodIgnorePunctuation).HasColumnName("TestMethod_IgnorePunctuation");
            entity.Property(e => e.TestMethodNormalizeWhitespace).HasColumnName("TestMethod_NormalizeWhitespace");
            entity.Property(e => e.TestMethodUseTextFallback).HasColumnName("TestMethod_UseTextFallback");
            entity.Property(e => e.TestResultCaseInsensitive).HasColumnName("TestResult_CaseInsensitive");
            entity.Property(e => e.TestResultIgnorePunctuation).HasColumnName("TestResult_IgnorePunctuation");
            entity.Property(e => e.TestResultNormalizeWhitespace).HasColumnName("TestResult_NormalizeWhitespace");
            entity.Property(e => e.TestResultUseTextFallback).HasColumnName("TestResult_UseTextFallback");

            entity.HasOne(d => d.Disease).WithOne(p => p.DiseaseHl7matchingConfig).HasForeignKey<DiseaseHl7matchingConfig>(d => d.DiseaseId);

            entity.HasOne(d => d.PartialMatchConfirmationStatus).WithMany(p => p.DiseaseHl7matchingConfigs).HasForeignKey(d => d.PartialMatchConfirmationStatusId);
        });

        modelBuilder.Entity<DiseaseReinfectionRule>(entity =>
        {
            entity.HasIndex(e => e.DiseaseId, "IX_DiseaseReinfectionRules_DiseaseId");

            entity.HasIndex(e => e.IsActive, "IX_DiseaseReinfectionRules_IsActive");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.NotificationMessage).HasMaxLength(1000);

            entity.HasOne(d => d.Disease).WithMany(p => p.DiseaseReinfectionRules)
                .HasForeignKey(d => d.DiseaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DiseaseSymptom>(entity =>
        {
            entity.HasIndex(e => new { e.DiseaseId, e.IsCommon, e.SortOrder }, "IX_DiseaseSymptoms_DiseaseId_IsCommon_SortOrder").HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => new { e.DiseaseId, e.SymptomId }, "IX_DiseaseSymptoms_DiseaseId_SymptomId")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0))");

            entity.HasIndex(e => e.SymptomId, "IX_DiseaseSymptoms_SymptomId");

            entity.HasOne(d => d.Disease).WithMany(p => p.DiseaseSymptoms).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.Symptom).WithMany(p => p.DiseaseSymptoms)
                .HasForeignKey(d => d.SymptomId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DiseaseTaskTemplate>(entity =>
        {
            entity.HasIndex(e => new { e.DiseaseId, e.TaskTemplateId }, "IX_DiseaseTaskTemplates_DiseaseId_TaskTemplateId").IsUnique();

            entity.HasIndex(e => e.InheritedFromDiseaseId, "IX_DiseaseTaskTemplates_InheritedFromDiseaseId");

            entity.HasIndex(e => e.IsInherited, "IX_DiseaseTaskTemplates_IsInherited");

            entity.HasIndex(e => e.TaskTemplateId, "IX_DiseaseTaskTemplates_TaskTemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.OverrideInstructions).HasMaxLength(4000);

            entity.HasOne(d => d.Disease).WithMany(p => p.DiseaseTaskTemplates).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.TaskTemplate).WithMany(p => p.DiseaseTaskTemplates).HasForeignKey(d => d.TaskTemplateId);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.EventTypeId, "IX_Events_EventTypeId");

            entity.HasIndex(e => e.LocationId, "IX_Events_LocationId");

            entity.HasIndex(e => e.Name, "IX_Events_Name");

            entity.HasIndex(e => e.OrganizerOrganizationId, "IX_Events_OrganizerOrganizationId");

            entity.HasIndex(e => new { e.StartDateTime, e.EndDateTime }, "IX_Events_StartDateTime_EndDateTime");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.LastModifiedByUserId).HasMaxLength(450);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.EventType).WithMany(p => p.Events).HasForeignKey(d => d.EventTypeId);

            entity.HasOne(d => d.Location).WithMany(p => p.Events)
                .HasForeignKey(d => d.LocationId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.OrganizerOrganization).WithMany(p => p.Events).HasForeignKey(d => d.OrganizerOrganizationId);
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<ExposureEvent>(entity =>
        {
            entity.HasIndex(e => e.City, "IX_ExposureEvents_City");

            entity.HasIndex(e => e.ContactClassificationId, "IX_ExposureEvents_ContactClassificationId");

            entity.HasIndex(e => e.EventId, "IX_ExposureEvents_EventId");

            entity.HasIndex(e => e.ExposedCaseId, "IX_ExposureEvents_ExposedCaseId");

            entity.HasIndex(e => new { e.ExposureStartDate, e.ExposureEndDate }, "IX_ExposureEvents_ExposureStartDate_ExposureEndDate");

            entity.HasIndex(e => e.ExposureStatus, "IX_ExposureEvents_ExposureStatus");

            entity.HasIndex(e => e.ExposureType, "IX_ExposureEvents_ExposureType");

            entity.HasIndex(e => e.IsReportingExposure, "IX_ExposureEvents_IsReportingExposure");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_ExposureEvents_Latitude_Longitude");

            entity.HasIndex(e => e.LocationId, "IX_ExposureEvents_LocationId");

            entity.HasIndex(e => e.PostalCode, "IX_ExposureEvents_PostalCode");

            entity.HasIndex(e => e.SourceCaseId, "IX_ExposureEvents_SourceCaseId");

            entity.HasIndex(e => e.State, "IX_ExposureEvents_State");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AddressLine).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.ConfidenceLevel).HasMaxLength(50);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CountryCode).HasMaxLength(3);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.DeletedByUserId).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.FreeTextLocation).HasMaxLength(500);
            entity.Property(e => e.GeocodingAccuracy).HasMaxLength(50);
            entity.Property(e => e.InterstateOriginState).HasMaxLength(100);
            entity.Property(e => e.InvestigationNotes).HasMaxLength(2000);
            entity.Property(e => e.LastModifiedByUserId).HasMaxLength(450);
            entity.Property(e => e.Latitude).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.StatusChangedByUserId).HasMaxLength(450);

            entity.HasOne(d => d.ContactClassification).WithMany(p => p.ExposureEvents).HasForeignKey(d => d.ContactClassificationId);

            entity.HasOne(d => d.Event).WithMany(p => p.ExposureEvents).HasForeignKey(d => d.EventId);

            entity.HasOne(d => d.ExposedCase).WithMany(p => p.ExposureEventExposedCases)
                .HasForeignKey(d => d.ExposedCaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Location).WithMany(p => p.ExposureEvents).HasForeignKey(d => d.LocationId);

            entity.HasOne(d => d.SourceCase).WithMany(p => p.ExposureEventSourceCases).HasForeignKey(d => d.SourceCaseId);
        });

        modelBuilder.Entity<GeocodingQueueItem>(entity =>
        {
            entity.HasIndex(e => e.PatientId, "IX_GeocodingQueueItems_PatientId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Patient).WithMany(p => p.GeocodingQueueItems).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<Hl7configuration>(entity =>
        {
            entity.ToTable("HL7Configurations");

            entity.HasIndex(e => e.DefaultLaboratoryId, "IX_HL7Configurations_DefaultLaboratoryId");

            entity.HasIndex(e => e.IsActive, "IX_HL7Configurations_IsActive");

            entity.HasIndex(e => e.SendingFacility, "IX_HL7Configurations_SendingFacility");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ArchivePath).HasMaxLength(1000);
            entity.Property(e => e.CharacterEncoding).HasMaxLength(50);
            entity.Property(e => e.ConfigurationName).HasMaxLength(200);
            entity.Property(e => e.DefaultDateFormat).HasMaxLength(50);
            entity.Property(e => e.FileDropPath).HasMaxLength(1000);
            entity.Property(e => e.FilePattern).HasMaxLength(100);
            entity.Property(e => e.NotificationEmailAddresses).HasMaxLength(1000);
            entity.Property(e => e.SendingApplication).HasMaxLength(200);
            entity.Property(e => e.SendingFacility).HasMaxLength(200);
            entity.Property(e => e.TestModeDescription).HasMaxLength(1000);
            entity.Property(e => e.TimezoneOffset).HasMaxLength(20);

            entity.HasOne(d => d.DefaultLaboratory).WithMany(p => p.Hl7configurations)
                .HasForeignKey(d => d.DefaultLaboratoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Hl7configurationDisease>(entity =>
        {
            entity.ToTable("HL7ConfigurationDiseases");

            entity.HasIndex(e => new { e.ConfigurationId, e.DiseaseId }, "IX_HL7ConfigurationDiseases_ConfigurationId_DiseaseId").IsUnique();

            entity.HasIndex(e => e.DiseaseId, "IX_HL7ConfigurationDiseases_DiseaseId");

            entity.HasIndex(e => e.IsDefault, "IX_HL7ConfigurationDiseases_IsDefault");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(d => d.Configuration).WithMany(p => p.Hl7configurationDiseases).HasForeignKey(d => d.ConfigurationId);

            entity.HasOne(d => d.Disease).WithMany(p => p.Hl7configurationDiseases).HasForeignKey(d => d.DiseaseId);
        });

        modelBuilder.Entity<Hl7customFieldMapping>(entity =>
        {
            entity.ToTable("HL7CustomFieldMappings");

            entity.HasIndex(e => e.CustomFieldDefinitionId, "IX_HL7CustomFieldMappings_CustomFieldDefinitionId");

            entity.HasIndex(e => e.DiseaseId, "IX_HL7CustomFieldMappings_DiseaseId");

            entity.Property(e => e.Hl7testCode)
                .HasMaxLength(100)
                .HasColumnName("HL7TestCode");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.TestCodeDescription).HasMaxLength(200);
            entity.Property(e => e.ValueTransformation).HasMaxLength(500);

            entity.HasOne(d => d.CustomFieldDefinition).WithMany(p => p.Hl7customFieldMappings).HasForeignKey(d => d.CustomFieldDefinitionId);

            entity.HasOne(d => d.Disease).WithMany(p => p.Hl7customFieldMappings).HasForeignKey(d => d.DiseaseId);
        });

        modelBuilder.Entity<Hl7fieldMapping>(entity =>
        {
            entity.ToTable("HL7FieldMappings");

            entity.HasIndex(e => new { e.ConfigurationId, e.SegmentType, e.FieldPath }, "IX_HL7FieldMappings_ConfigurationId_SegmentType_FieldPath");

            entity.HasIndex(e => e.CreatedFromIssueId, "IX_HL7FieldMappings_CreatedFromIssueId");

            entity.HasIndex(e => e.DiseaseId, "IX_HL7FieldMappings_DiseaseId");

            entity.HasIndex(e => new { e.IsActive, e.Priority }, "IX_HL7FieldMappings_IsActive_Priority");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DefaultValue).HasMaxLength(500);
            entity.Property(e => e.ExampleHl7value)
                .HasMaxLength(500)
                .HasColumnName("ExampleHL7Value");
            entity.Property(e => e.ExampleMappedValue).HasMaxLength(500);
            entity.Property(e => e.FieldName).HasMaxLength(200);
            entity.Property(e => e.FieldPath).HasMaxLength(100);
            entity.Property(e => e.LookupTable).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.SegmentType).HasMaxLength(10);
            entity.Property(e => e.TargetEntity).HasMaxLength(100);
            entity.Property(e => e.TargetProperty).HasMaxLength(100);
            entity.Property(e => e.TransformationRule).HasMaxLength(500);
            entity.Property(e => e.ValidationRegex).HasMaxLength(500);

            entity.HasOne(d => d.Configuration).WithMany(p => p.Hl7fieldMappings).HasForeignKey(d => d.ConfigurationId);

            entity.HasOne(d => d.CreatedFromIssue).WithMany(p => p.Hl7fieldMappings)
                .HasForeignKey(d => d.CreatedFromIssueId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Disease).WithMany(p => p.Hl7fieldMappings).HasForeignKey(d => d.DiseaseId);
        });

        modelBuilder.Entity<Hl7message>(entity =>
        {
            entity.ToTable("HL7Messages");

            entity.HasIndex(e => e.CaseId, "IX_HL7Messages_CaseId");

            entity.HasIndex(e => e.ConfigurationId, "IX_HL7Messages_ConfigurationId");

            entity.HasIndex(e => e.DuplicateOfMessageId, "IX_HL7Messages_DuplicateOfMessageId");

            entity.HasIndex(e => e.IsDuplicate, "IX_HL7Messages_IsDuplicate");

            entity.HasIndex(e => e.LabResultId, "IX_HL7Messages_LabResultId");

            entity.HasIndex(e => e.LaboratoryOrganizationId, "IX_HL7Messages_LaboratoryOrganizationId");

            entity.HasIndex(e => e.ManualReviewByUserId, "IX_HL7Messages_ManualReviewByUserId");

            entity.HasIndex(e => e.MessageControlId, "IX_HL7Messages_MessageControlId");

            entity.HasIndex(e => new { e.MessageControlId, e.SendingFacility }, "IX_HL7Messages_MessageControlId_SendingFacility").HasFilter("([MessageControlId] IS NOT NULL AND [SendingFacility] IS NOT NULL)");

            entity.HasIndex(e => e.OrderingProviderOrganizationId, "IX_HL7Messages_OrderingProviderOrganizationId");

            entity.HasIndex(e => e.PatientId, "IX_HL7Messages_PatientId");

            entity.HasIndex(e => e.ProcessedByUserId, "IX_HL7Messages_ProcessedByUserId");

            entity.HasIndex(e => e.ReceivedAt, "IX_HL7Messages_ReceivedAt");

            entity.HasIndex(e => e.RequiresManualReview, "IX_HL7Messages_RequiresManualReview");

            entity.HasIndex(e => new { e.SendingFacility, e.MessageControlId }, "IX_HL7Messages_SendingFacility_MessageControlId");

            entity.HasIndex(e => e.Status, "IX_HL7Messages_Status");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DuplicateDetectionMethod).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.FileName).HasMaxLength(500);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.Hl7version)
                .HasMaxLength(20)
                .HasColumnName("HL7Version");
            entity.Property(e => e.MessageControlId).HasMaxLength(100);
            entity.Property(e => e.MessageType).HasMaxLength(50);
            entity.Property(e => e.ReceivingApplication).HasMaxLength(200);
            entity.Property(e => e.ReceivingFacility).HasMaxLength(200);
            entity.Property(e => e.SendingApplication).HasMaxLength(200);
            entity.Property(e => e.SendingFacility).HasMaxLength(200);

            entity.HasOne(d => d.Case).WithMany(p => p.Hl7messages).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.Configuration).WithMany(p => p.Hl7messages)
                .HasForeignKey(d => d.ConfigurationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.DuplicateOfMessage).WithMany(p => p.InverseDuplicateOfMessage).HasForeignKey(d => d.DuplicateOfMessageId);

            entity.HasOne(d => d.LabResult).WithMany(p => p.Hl7messages).HasForeignKey(d => d.LabResultId);

            entity.HasOne(d => d.LaboratoryOrganization).WithMany(p => p.Hl7messageLaboratoryOrganizations).HasForeignKey(d => d.LaboratoryOrganizationId);

            entity.HasOne(d => d.ManualReviewByUser).WithMany(p => p.Hl7messageManualReviewByUsers).HasForeignKey(d => d.ManualReviewByUserId);

            entity.HasOne(d => d.OrderingProviderOrganization).WithMany(p => p.Hl7messageOrderingProviderOrganizations).HasForeignKey(d => d.OrderingProviderOrganizationId);

            entity.HasOne(d => d.Patient).WithMany(p => p.Hl7messages).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.ProcessedByUser).WithMany(p => p.Hl7messageProcessedByUsers).HasForeignKey(d => d.ProcessedByUserId);
        });

        modelBuilder.Entity<Hl7messageSegment>(entity =>
        {
            entity.ToTable("HL7MessageSegments");

            entity.HasIndex(e => new { e.Hl7messageId, e.SegmentType }, "IX_HL7MessageSegments_HL7MessageId_SegmentType");

            entity.HasIndex(e => new { e.Hl7messageId, e.SequenceNumber }, "IX_HL7MessageSegments_HL7MessageId_SequenceNumber");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ErrorDetails).HasMaxLength(2000);
            entity.Property(e => e.Hl7messageId).HasColumnName("HL7MessageId");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.RawSegment).HasMaxLength(4000);
            entity.Property(e => e.SegmentType).HasMaxLength(10);

            entity.HasOne(d => d.Hl7message).WithMany(p => p.Hl7messageSegments).HasForeignKey(d => d.Hl7messageId);
        });

        modelBuilder.Entity<Hl7parsingIssue>(entity =>
        {
            entity.ToTable("HL7ParsingIssues");

            entity.HasIndex(e => e.CreatedAt, "IX_HL7ParsingIssues_CreatedAt");

            entity.HasIndex(e => e.FieldMappingId, "IX_HL7ParsingIssues_FieldMappingId");

            entity.HasIndex(e => e.Hl7messageId, "IX_HL7ParsingIssues_HL7MessageId");

            entity.HasIndex(e => new { e.IsResolved, e.IssueType }, "IX_HL7ParsingIssues_IsResolved_IssueType");

            entity.HasIndex(e => e.MessageSegmentId, "IX_HL7ParsingIssues_MessageSegmentId");

            entity.HasIndex(e => e.ResolvedByUserId, "IX_HL7ParsingIssues_ResolvedByUserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ExpectedFormat).HasMaxLength(500);
            entity.Property(e => e.FieldName).HasMaxLength(200);
            entity.Property(e => e.FieldPath).HasMaxLength(100);
            entity.Property(e => e.Hl7messageId).HasColumnName("HL7MessageId");
            entity.Property(e => e.RawValue).HasMaxLength(1000);
            entity.Property(e => e.ResolutionNotes).HasMaxLength(2000);
            entity.Property(e => e.SegmentType).HasMaxLength(10);
            entity.Property(e => e.SuggestedMapping).HasMaxLength(500);

            entity.HasOne(d => d.FieldMapping).WithMany(p => p.Hl7parsingIssues)
                .HasForeignKey(d => d.FieldMappingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Hl7message).WithMany(p => p.Hl7parsingIssues)
                .HasForeignKey(d => d.Hl7messageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.MessageSegment).WithMany(p => p.Hl7parsingIssues).HasForeignKey(d => d.MessageSegmentId);

            entity.HasOne(d => d.ResolvedByUser).WithMany(p => p.Hl7parsingIssues).HasForeignKey(d => d.ResolvedByUserId);
        });

        modelBuilder.Entity<Hl7testMessageHistory>(entity =>
        {
            entity.ToTable("HL7TestMessageHistory");

            entity.HasIndex(e => e.GeneratedByUserId, "IX_HL7TestMessageHistory_GeneratedByUserId");

            entity.HasIndex(e => e.Hl7messageId, "IX_HL7TestMessageHistory_HL7MessageId");

            entity.HasIndex(e => e.TemplateId, "IX_HL7TestMessageHistory_TemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AccessionNumber).HasMaxLength(100);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.GeneratedBy).HasMaxLength(450);
            entity.Property(e => e.Hl7messageId).HasColumnName("HL7MessageId");
            entity.Property(e => e.PatientMrn)
                .HasMaxLength(100)
                .HasColumnName("PatientMRN");
            entity.Property(e => e.RawHl7message).HasColumnName("RawHL7Message");
            entity.Property(e => e.TestComment).HasMaxLength(2000);

            entity.HasOne(d => d.GeneratedByUser).WithMany(p => p.Hl7testMessageHistories).HasForeignKey(d => d.GeneratedByUserId);

            entity.HasOne(d => d.Hl7message).WithMany(p => p.Hl7testMessageHistories).HasForeignKey(d => d.Hl7messageId);

            entity.HasOne(d => d.Template).WithMany(p => p.Hl7testMessageHistories).HasForeignKey(d => d.TemplateId);
        });

        modelBuilder.Entity<Hl7testMessageTemplate>(entity =>
        {
            entity.ToTable("HL7TestMessageTemplates");

            entity.HasIndex(e => e.CreatedByUserId, "IX_HL7TestMessageTemplates_CreatedByUserId");

            entity.HasIndex(e => e.UpdatedByUserId, "IX_HL7TestMessageTemplates_UpdatedByUserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.TemplateName).HasMaxLength(200);
            entity.Property(e => e.TestComment).HasMaxLength(2000);
            entity.Property(e => e.UpdatedBy).HasMaxLength(450);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Hl7testMessageTemplateCreatedByUsers).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.UpdatedByUser).WithMany(p => p.Hl7testMessageTemplateUpdatedByUsers).HasForeignKey(d => d.UpdatedByUserId);
        });

        modelBuilder.Entity<Jurisdiction>(entity =>
        {
            entity.HasIndex(e => e.JurisdictionTypeId, "IX_Jurisdictions_JurisdictionTypeId");

            entity.HasIndex(e => e.Name, "IX_Jurisdictions_Name");

            entity.HasIndex(e => e.ParentJurisdictionId, "IX_Jurisdictions_ParentJurisdictionId");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PopulationSource).HasMaxLength(200);

            entity.HasOne(d => d.JurisdictionType).WithMany(p => p.Jurisdictions)
                .HasForeignKey(d => d.JurisdictionTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ParentJurisdiction).WithMany(p => p.InverseParentJurisdiction).HasForeignKey(d => d.ParentJurisdictionId);
        });

        modelBuilder.Entity<JurisdictionType>(entity =>
        {
            entity.HasIndex(e => e.FieldNumber, "IX_JurisdictionTypes_FieldNumber").IsUnique();

            entity.HasIndex(e => e.Name, "IX_JurisdictionTypes_Name");

            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.HasIndex(e => e.AccessionNumber, "IX_LabResults_AccessionNumber");

            entity.HasIndex(e => e.CaseId, "IX_LabResults_CaseId");

            entity.HasIndex(e => e.FriendlyId, "IX_LabResults_FriendlyId").IsUnique();

            entity.HasIndex(e => e.LaboratoryId, "IX_LabResults_LaboratoryId");

            entity.HasIndex(e => e.OrderingProviderId, "IX_LabResults_OrderingProviderId");

            entity.HasIndex(e => e.ParentLabResultId, "IX_LabResults_ParentLabResultId");

            entity.HasIndex(e => e.PatientId, "IX_LabResults_PatientId");

            entity.HasIndex(e => e.ResultDate, "IX_LabResults_ResultDate");

            entity.HasIndex(e => e.ResultUnitsId, "IX_LabResults_ResultUnitsId");

            entity.HasIndex(e => e.SpecimenCollectionDate, "IX_LabResults_SpecimenCollectionDate");

            entity.HasIndex(e => e.SpecimenTypeId, "IX_LabResults_SpecimenTypeId");

            entity.HasIndex(e => e.TestResultId, "IX_LabResults_TestResultId");

            entity.HasIndex(e => e.TestTypeId, "IX_LabResults_TestTypeId");

            entity.HasIndex(e => e.TestedDiseaseId, "IX_LabResults_TestedDiseaseId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AccessionNumber).HasMaxLength(100);
            entity.Property(e => e.AttachmentFileName).HasMaxLength(200);
            entity.Property(e => e.AttachmentPath).HasMaxLength(500);
            entity.Property(e => e.FriendlyId).HasMaxLength(20);
            entity.Property(e => e.LabInterpretation).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasOne(d => d.Case).WithMany(p => p.LabResults).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.Laboratory).WithMany(p => p.LabResultLaboratories).HasForeignKey(d => d.LaboratoryId);

            entity.HasOne(d => d.OrderingProvider).WithMany(p => p.LabResultOrderingProviders).HasForeignKey(d => d.OrderingProviderId);

            entity.HasOne(d => d.ParentLabResult).WithMany(p => p.InverseParentLabResult).HasForeignKey(d => d.ParentLabResultId);

            entity.HasOne(d => d.Patient).WithMany(p => p.LabResults).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.ResultUnits).WithMany(p => p.LabResults).HasForeignKey(d => d.ResultUnitsId);

            entity.HasOne(d => d.SpecimenType).WithMany(p => p.LabResults).HasForeignKey(d => d.SpecimenTypeId);

            entity.HasOne(d => d.TestResult).WithMany(p => p.LabResults).HasForeignKey(d => d.TestResultId);

            entity.HasOne(d => d.TestType).WithMany(p => p.LabResults).HasForeignKey(d => d.TestTypeId);

            entity.HasOne(d => d.TestedDisease).WithMany(p => p.LabResults).HasForeignKey(d => d.TestedDiseaseId);
        });

        modelBuilder.Entity<LabResultMarker>(entity =>
        {
            entity.HasIndex(e => e.Loinccode, "IX_LabResultMarkers_LOINCCode");

            entity.HasIndex(e => e.LabResultId, "IX_LabResultMarkers_LabResultId");

            entity.HasIndex(e => e.PathogenId, "IX_LabResultMarkers_PathogenId");

            entity.HasIndex(e => e.TestMethodId, "IX_LabResultMarkers_TestMethodId");

            entity.HasIndex(e => e.TestResultId, "IX_LabResultMarkers_TestResultId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.InterpretationFlag).HasMaxLength(20);
            entity.Property(e => e.Loinccode)
                .HasMaxLength(20)
                .HasColumnName("LOINCCode");
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.QualitativeResultText).HasMaxLength(500);
            entity.Property(e => e.QuantitativeUnit).HasMaxLength(50);
            entity.Property(e => e.QuantitativeValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenceRangeHigh).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ReferenceRangeLow).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ResultStatus).HasMaxLength(10);
            entity.Property(e => e.TestCode).HasMaxLength(50);

            entity.HasOne(d => d.LabResult).WithMany(p => p.LabResultMarkers).HasForeignKey(d => d.LabResultId);

            entity.HasOne(d => d.Pathogen).WithMany(p => p.LabResultMarkers)
                .HasForeignKey(d => d.PathogenId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TestMethod).WithMany(p => p.LabResultMarkers).HasForeignKey(d => d.TestMethodId);

            entity.HasOne(d => d.TestResult).WithMany(p => p.LabResultMarkers).HasForeignKey(d => d.TestResultId);
        });

        modelBuilder.Entity<LabResultMarkerHistory>(entity =>
        {
            entity.HasIndex(e => e.ChangedAt, "IX_LabResultMarkerHistories_ChangedAt");

            entity.HasIndex(e => e.ChangedByUserId, "IX_LabResultMarkerHistories_ChangedByUserId");

            entity.HasIndex(e => e.Hl7messageId, "IX_LabResultMarkerHistories_HL7MessageId");

            entity.HasIndex(e => e.LabResultMarkerId, "IX_LabResultMarkerHistories_LabResultMarkerId");

            entity.HasIndex(e => new { e.LabResultMarkerId, e.ChangedAt }, "IX_LabResultMarkerHistories_LabResultMarkerId_ChangedAt");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ChangeReason).HasMaxLength(500);
            entity.Property(e => e.Hl7messageId).HasColumnName("HL7MessageId");
            entity.Property(e => e.NewAbnormalFlag).HasMaxLength(10);
            entity.Property(e => e.NewQualitativeValue).HasMaxLength(1000);
            entity.Property(e => e.NewQuantitativeValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NewResultStatus).HasMaxLength(10);
            entity.Property(e => e.PreviousAbnormalFlag).HasMaxLength(10);
            entity.Property(e => e.PreviousQualitativeValue).HasMaxLength(1000);
            entity.Property(e => e.PreviousQuantitativeValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PreviousResultStatus).HasMaxLength(10);

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.LabResultMarkerHistories).HasForeignKey(d => d.ChangedByUserId);

            entity.HasOne(d => d.Hl7message).WithMany(p => p.LabResultMarkerHistories)
                .HasForeignKey(d => d.Hl7messageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.LabResultMarker).WithMany(p => p.LabResultMarkerHistories).HasForeignKey(d => d.LabResultMarkerId);
        });

        modelBuilder.Entity<Language>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => e.GeocodingStatus, "IX_Locations_GeocodingStatus");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "IX_Locations_Latitude_Longitude");

            entity.HasIndex(e => e.LocationTypeId, "IX_Locations_LocationTypeId");

            entity.HasIndex(e => e.Name, "IX_Locations_Name");

            entity.HasIndex(e => e.OrganizationId, "IX_Locations_OrganizationId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.GeocodingStatus).HasMaxLength(50);
            entity.Property(e => e.LastModifiedByUserId).HasMaxLength(450);
            entity.Property(e => e.Latitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(10, 7)");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            entity.HasOne(d => d.LocationType).WithMany(p => p.Locations).HasForeignKey(d => d.LocationTypeId);

            entity.HasOne(d => d.Organization).WithMany(p => p.Locations).HasForeignKey(d => d.OrganizationId);
        });

        modelBuilder.Entity<LocationType>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<LookupTable>(entity =>
        {
            entity.HasIndex(e => e.Name, "IX_LookupTables_Name").IsUnique();
        });

        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasIndex(e => new { e.LookupTableId, e.DisplayOrder }, "IX_LookupValues_LookupTableId_DisplayOrder");

            entity.HasOne(d => d.LookupTable).WithMany(p => p.LookupValues).HasForeignKey(d => d.LookupTableId);
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasIndex(e => e.CaseId, "IX_Notes_CaseId");

            entity.HasIndex(e => e.CreatedAt, "IX_Notes_CreatedAt");

            entity.HasIndex(e => e.CreatedBy, "IX_Notes_CreatedBy");

            entity.HasIndex(e => e.OutbreakId, "IX_Notes_OutbreakId");

            entity.HasIndex(e => e.PatientId, "IX_Notes_PatientId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AttachmentFileName).HasMaxLength(200);
            entity.Property(e => e.AttachmentPath).HasMaxLength(500);
            entity.Property(e => e.Recipient).HasMaxLength(200);
            entity.Property(e => e.Subject).HasMaxLength(100);
            entity.Property(e => e.Type).HasMaxLength(50);

            entity.HasOne(d => d.Case).WithMany(p => p.Notes).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.Notes).HasForeignKey(d => d.OutbreakId);

            entity.HasOne(d => d.Patient).WithMany(p => p.Notes).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<Occupation>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(6);
            entity.Property(e => e.MajorGroupCode).HasMaxLength(1);
            entity.Property(e => e.MinorGroupCode).HasMaxLength(3);
            entity.Property(e => e.SubMajorGroupCode).HasMaxLength(2);
            entity.Property(e => e.UnitGroupCode).HasMaxLength(4);
        });

        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasIndex(e => e.ExportCode, "IX_Organizations_ExportCode");

            entity.HasIndex(e => e.FriendlyId, "IX_Organizations_FriendlyId").IsUnique();

            entity.HasIndex(e => e.Name, "IX_Organizations_Name");

            entity.HasIndex(e => e.OrganizationTypeId, "IX_Organizations_OrganizationTypeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactPerson).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.FriendlyId).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Phone).HasMaxLength(50);

            entity.HasOne(d => d.OrganizationType).WithMany(p => p.Organizations).HasForeignKey(d => d.OrganizationTypeId);
        });

        modelBuilder.Entity<OrganizationType>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Outbreak>(entity =>
        {
            entity.HasIndex(e => e.ConfirmationStatusId, "IX_Outbreaks_ConfirmationStatusId");

            entity.HasIndex(e => e.IndexCaseId, "IX_Outbreaks_IndexCaseId");

            entity.HasIndex(e => e.LeadInvestigatorId, "IX_Outbreaks_LeadInvestigatorId");

            entity.HasIndex(e => e.ParentOutbreakId, "IX_Outbreaks_ParentOutbreakId");

            entity.HasIndex(e => e.PrimaryDiseaseId, "IX_Outbreaks_PrimaryDiseaseId");

            entity.HasIndex(e => e.PrimaryEventId, "IX_Outbreaks_PrimaryEventId");

            entity.HasIndex(e => e.PrimaryLocationId, "IX_Outbreaks_PrimaryLocationId");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.ConfirmationStatus).WithMany(p => p.Outbreaks).HasForeignKey(d => d.ConfirmationStatusId);

            entity.HasOne(d => d.IndexCase).WithMany(p => p.Outbreaks).HasForeignKey(d => d.IndexCaseId);

            entity.HasOne(d => d.LeadInvestigator).WithMany(p => p.Outbreaks).HasForeignKey(d => d.LeadInvestigatorId);

            entity.HasOne(d => d.ParentOutbreak).WithMany(p => p.InverseParentOutbreak).HasForeignKey(d => d.ParentOutbreakId);

            entity.HasOne(d => d.PrimaryDisease).WithMany(p => p.Outbreaks).HasForeignKey(d => d.PrimaryDiseaseId);

            entity.HasOne(d => d.PrimaryEvent).WithMany(p => p.Outbreaks).HasForeignKey(d => d.PrimaryEventId);

            entity.HasOne(d => d.PrimaryLocation).WithMany(p => p.Outbreaks).HasForeignKey(d => d.PrimaryLocationId);
        });

        modelBuilder.Entity<OutbreakCase>(entity =>
        {
            entity.HasIndex(e => e.CaseId, "IX_OutbreakCases_CaseId");

            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakCases_OutbreakId");

            entity.HasIndex(e => e.SearchQueryId, "IX_OutbreakCases_SearchQueryId");

            entity.Property(e => e.ClassificationNotes).HasMaxLength(1000);
            entity.Property(e => e.UnlinkReason).HasMaxLength(500);

            entity.HasOne(d => d.Case).WithMany(p => p.OutbreakCases).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakCases).HasForeignKey(d => d.OutbreakId);

            entity.HasOne(d => d.SearchQuery).WithMany(p => p.OutbreakCases).HasForeignKey(d => d.SearchQueryId);
        });

        modelBuilder.Entity<OutbreakCaseDefinition>(entity =>
        {
            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakCaseDefinitions_OutbreakId");

            entity.Property(e => e.DefinitionName).HasMaxLength(200);
            entity.Property(e => e.DefinitionText).HasMaxLength(2000);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakCaseDefinitions).HasForeignKey(d => d.OutbreakId);
        });

        modelBuilder.Entity<OutbreakLineListConfiguration>(entity =>
        {
            entity.HasIndex(e => e.CreatedByUserId, "IX_OutbreakLineListConfigurations_CreatedByUserId");

            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakLineListConfigurations_OutbreakId");

            entity.HasIndex(e => e.UserId, "IX_OutbreakLineListConfigurations_UserId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.OutbreakLineListConfigurationCreatedByUsers).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakLineListConfigurations).HasForeignKey(d => d.OutbreakId);

            entity.HasOne(d => d.User).WithMany(p => p.OutbreakLineListConfigurationUsers).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<OutbreakSearchQuery>(entity =>
        {
            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakSearchQueries_OutbreakId");

            entity.Property(e => e.QueryName).HasMaxLength(200);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakSearchQueries).HasForeignKey(d => d.OutbreakId);
        });

        modelBuilder.Entity<OutbreakTasksFlattened>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("OutbreakTasksFlattened");
        });

        modelBuilder.Entity<OutbreakTeamMember>(entity =>
        {
            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakTeamMembers_OutbreakId");

            entity.HasIndex(e => e.UserId, "IX_OutbreakTeamMembers_UserId");

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakTeamMembers).HasForeignKey(d => d.OutbreakId);

            entity.HasOne(d => d.User).WithMany(p => p.OutbreakTeamMembers).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<OutbreakTimeline>(entity =>
        {
            entity.HasIndex(e => e.OutbreakId, "IX_OutbreakTimelines_OutbreakId");

            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Outbreak).WithMany(p => p.OutbreakTimelines).HasForeignKey(d => d.OutbreakId);
        });

        modelBuilder.Entity<Pathogen>(entity =>
        {
            entity.HasIndex(e => new { e.DiseaseId, e.DisplayOrder }, "IX_Pathogens_DiseaseId_DisplayOrder");

            entity.HasIndex(e => e.IsActive, "IX_Pathogens_IsActive");

            entity.HasIndex(e => e.Loinccode, "IX_Pathogens_LOINCCode")
                .IsUnique()
                .HasFilter("([LOINCCode] IS NOT NULL)");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.DefaultReferenceRangeHigh).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DefaultReferenceRangeLow).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DefaultUnit).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Loinccode)
                .HasMaxLength(20)
                .HasColumnName("LOINCCode");
            entity.Property(e => e.LoincdisplayName)
                .HasMaxLength(500)
                .HasColumnName("LOINCDisplayName");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ShortName).HasMaxLength(50);

            entity.HasOne(d => d.Disease).WithMany(p => p.Pathogens).HasForeignKey(d => d.DiseaseId);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.AncestryId, "IX_Patients_AncestryId");

            entity.HasIndex(e => e.AtsiStatusId, "IX_Patients_AtsiStatusId");

            entity.HasIndex(e => e.CountryOfBirthId, "IX_Patients_CountryOfBirthId");

            entity.HasIndex(e => e.CreatedByUserId, "IX_Patients_CreatedByUserId");

            entity.HasIndex(e => e.FriendlyId, "IX_Patients_FriendlyId")
                .IsUnique()
                .HasFilter("([IsDeleted]=(0) AND [FriendlyId] IS NOT NULL)");

            entity.HasIndex(e => e.GenderId, "IX_Patients_GenderId");

            entity.HasIndex(e => e.Jurisdiction1Id, "IX_Patients_Jurisdiction1Id");

            entity.HasIndex(e => e.Jurisdiction2Id, "IX_Patients_Jurisdiction2Id");

            entity.HasIndex(e => e.Jurisdiction3Id, "IX_Patients_Jurisdiction3Id");

            entity.HasIndex(e => e.Jurisdiction4Id, "IX_Patients_Jurisdiction4Id");

            entity.HasIndex(e => e.Jurisdiction5Id, "IX_Patients_Jurisdiction5Id");

            entity.HasIndex(e => e.LanguageSpokenAtHomeId, "IX_Patients_LanguageSpokenAtHomeId");

            entity.HasIndex(e => e.OccupationId, "IX_Patients_OccupationId");

            entity.HasIndex(e => e.SexAtBirthId, "IX_Patients_SexAtBirthId");

            entity.HasIndex(e => e.StateId, "IX_Patients_StateId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FriendlyId).HasMaxLength(20);

            entity.HasOne(d => d.Ancestry).WithMany(p => p.Patients).HasForeignKey(d => d.AncestryId);

            entity.HasOne(d => d.AtsiStatus).WithMany(p => p.Patients).HasForeignKey(d => d.AtsiStatusId);

            entity.HasOne(d => d.CountryOfBirth).WithMany(p => p.Patients).HasForeignKey(d => d.CountryOfBirthId);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Patients).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.Gender).WithMany(p => p.Patients).HasForeignKey(d => d.GenderId);

            entity.HasOne(d => d.Jurisdiction1).WithMany(p => p.PatientJurisdiction1s).HasForeignKey(d => d.Jurisdiction1Id);

            entity.HasOne(d => d.Jurisdiction2).WithMany(p => p.PatientJurisdiction2s).HasForeignKey(d => d.Jurisdiction2Id);

            entity.HasOne(d => d.Jurisdiction3).WithMany(p => p.PatientJurisdiction3s).HasForeignKey(d => d.Jurisdiction3Id);

            entity.HasOne(d => d.Jurisdiction4).WithMany(p => p.PatientJurisdiction4s).HasForeignKey(d => d.Jurisdiction4Id);

            entity.HasOne(d => d.Jurisdiction5).WithMany(p => p.PatientJurisdiction5s).HasForeignKey(d => d.Jurisdiction5Id);

            entity.HasOne(d => d.LanguageSpokenAtHome).WithMany(p => p.Patients).HasForeignKey(d => d.LanguageSpokenAtHomeId);

            entity.HasOne(d => d.Occupation).WithMany(p => p.Patients).HasForeignKey(d => d.OccupationId);

            entity.HasOne(d => d.SexAtBirth).WithMany(p => p.Patients).HasForeignKey(d => d.SexAtBirthId);

            entity.HasOne(d => d.State).WithMany(p => p.Patients).HasForeignKey(d => d.StateId);
        });

        modelBuilder.Entity<PatientCustomFieldBoolean>(entity =>
        {
            entity.HasIndex(e => e.FieldDefinitionId, "IX_PatientCustomFieldBooleans_FieldDefinitionId");

            entity.HasIndex(e => new { e.PatientId, e.FieldDefinitionId }, "IX_PatientCustomFieldBooleans_PatientId_FieldDefinitionId").IsUnique();

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.PatientCustomFieldBooleans).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientCustomFieldBooleans).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<PatientCustomFieldDate>(entity =>
        {
            entity.HasIndex(e => e.FieldDefinitionId, "IX_PatientCustomFieldDates_FieldDefinitionId");

            entity.HasIndex(e => new { e.PatientId, e.FieldDefinitionId }, "IX_PatientCustomFieldDates_PatientId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.Value, "IX_PatientCustomFieldDates_Value");

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.PatientCustomFieldDates).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientCustomFieldDates).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<PatientCustomFieldLookup>(entity =>
        {
            entity.HasIndex(e => e.FieldDefinitionId, "IX_PatientCustomFieldLookups_FieldDefinitionId");

            entity.HasIndex(e => e.LookupValueId, "IX_PatientCustomFieldLookups_LookupValueId");

            entity.HasIndex(e => new { e.PatientId, e.FieldDefinitionId }, "IX_PatientCustomFieldLookups_PatientId_FieldDefinitionId").IsUnique();

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.PatientCustomFieldLookups).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.LookupValue).WithMany(p => p.PatientCustomFieldLookups).HasForeignKey(d => d.LookupValueId);

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientCustomFieldLookups).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<PatientCustomFieldNumber>(entity =>
        {
            entity.HasIndex(e => e.FieldDefinitionId, "IX_PatientCustomFieldNumbers_FieldDefinitionId");

            entity.HasIndex(e => new { e.PatientId, e.FieldDefinitionId }, "IX_PatientCustomFieldNumbers_PatientId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.Value, "IX_PatientCustomFieldNumbers_Value");

            entity.Property(e => e.Value).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.PatientCustomFieldNumbers).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientCustomFieldNumbers).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<PatientCustomFieldString>(entity =>
        {
            entity.HasIndex(e => e.FieldDefinitionId, "IX_PatientCustomFieldStrings_FieldDefinitionId");

            entity.HasIndex(e => new { e.PatientId, e.FieldDefinitionId }, "IX_PatientCustomFieldStrings_PatientId_FieldDefinitionId").IsUnique();

            entity.HasIndex(e => e.Value, "IX_PatientCustomFieldStrings_Value");

            entity.HasOne(d => d.FieldDefinition).WithMany(p => p.PatientCustomFieldStrings).HasForeignKey(d => d.FieldDefinitionId);

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientCustomFieldStrings).HasForeignKey(d => d.PatientId);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => new { e.Module, e.Action }, "IX_Permissions_Module_Action").IsUnique();
        });

        modelBuilder.Entity<ReportDefinition>(entity =>
        {
            entity.HasIndex(e => e.Category, "IX_ReportDefinitions_Category");

            entity.HasIndex(e => e.CreatedByUserId, "IX_ReportDefinitions_CreatedByUserId");

            entity.HasIndex(e => e.EntityType, "IX_ReportDefinitions_EntityType");

            entity.HasIndex(e => e.FolderId, "IX_ReportDefinitions_FolderId");

            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Folder).WithMany(p => p.ReportDefinitions).HasForeignKey(d => d.FolderId);
        });

        modelBuilder.Entity<ReportField>(entity =>
        {
            entity.HasIndex(e => e.FieldPath, "IX_ReportFields_FieldPath");

            entity.HasIndex(e => e.ReportDefinitionId, "IX_ReportFields_ReportDefinitionId");

            entity.Property(e => e.AggregationType).HasMaxLength(50);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.FieldPath).HasMaxLength(500);
            entity.Property(e => e.PivotArea).HasMaxLength(20);

            entity.HasOne(d => d.ReportDefinition).WithMany(p => p.ReportFields).HasForeignKey(d => d.ReportDefinitionId);
        });

        modelBuilder.Entity<ReportFilter>(entity =>
        {
            entity.HasIndex(e => e.ReportDefinitionId, "IX_ReportFilters_ReportDefinitionId");

            entity.Property(e => e.CollectionOperator).HasMaxLength(20);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.DynamicDateOffsetUnit).HasMaxLength(20);
            entity.Property(e => e.DynamicDateType).HasMaxLength(50);
            entity.Property(e => e.FieldPath).HasMaxLength(500);
            entity.Property(e => e.GroupLogicOperator).HasMaxLength(10);
            entity.Property(e => e.LogicOperator).HasMaxLength(10);
            entity.Property(e => e.Operator).HasMaxLength(50);

            entity.HasOne(d => d.ReportDefinition).WithMany(p => p.ReportFilters).HasForeignKey(d => d.ReportDefinitionId);
        });

        modelBuilder.Entity<ReportFolder>(entity =>
        {
            entity.HasIndex(e => e.ParentFolderId, "IX_ReportFolders_ParentFolderId");

            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.ParentFolder).WithMany(p => p.InverseParentFolder).HasForeignKey(d => d.ParentFolderId);
        });

        modelBuilder.Entity<ReportFolderShare>(entity =>
        {
            entity.HasIndex(e => e.GroupId, "IX_ReportFolderShares_GroupId");

            entity.HasIndex(e => e.ReportFolderId, "IX_ReportFolderShares_ReportFolderId");

            entity.HasIndex(e => e.UserId, "IX_ReportFolderShares_UserId");

            entity.Property(e => e.SharedByUserId).HasMaxLength(450);

            entity.HasOne(d => d.Group).WithMany(p => p.ReportFolderShares).HasForeignKey(d => d.GroupId);

            entity.HasOne(d => d.ReportFolder).WithMany(p => p.ReportFolderShares).HasForeignKey(d => d.ReportFolderId);

            entity.HasOne(d => d.User).WithMany(p => p.ReportFolderShares).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ResultUnit>(entity =>
        {
            entity.Property(e => e.Abbreviation).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ReviewQueue>(entity =>
        {
            entity.ToTable("ReviewQueue");

            entity.HasIndex(e => e.CaseId, "IX_ReviewQueue_CaseId");

            entity.HasIndex(e => e.CreatedByUserId, "IX_ReviewQueue_CreatedByUserId");

            entity.HasIndex(e => e.DiseaseId, "IX_ReviewQueue_DiseaseId");

            entity.HasIndex(e => new { e.GroupKey, e.CreatedDate }, "IX_ReviewQueue_GroupKey_Created");

            entity.HasIndex(e => e.PatientId, "IX_ReviewQueue_PatientId");

            entity.HasIndex(e => e.ReviewedByUserId, "IX_ReviewQueue_ReviewedByUserId");

            entity.HasIndex(e => new { e.ReviewStatus, e.EntityType, e.DiseaseId, e.CreatedDate }, "IX_ReviewQueue_Status_EntityType_Disease_Created");

            entity.HasIndex(e => e.TaskId, "IX_ReviewQueue_TaskId");

            entity.Property(e => e.ChangeType).HasMaxLength(50);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.GroupKey).HasMaxLength(255);
            entity.Property(e => e.ReviewAction).HasMaxLength(50);
            entity.Property(e => e.ReviewStatus).HasMaxLength(50);
            entity.Property(e => e.TriggerField).HasMaxLength(100);

            entity.HasOne(d => d.Case).WithMany(p => p.ReviewQueues).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.ReviewQueueCreatedByUsers).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.Disease).WithMany(p => p.ReviewQueues).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.Patient).WithMany(p => p.ReviewQueues).HasForeignKey(d => d.PatientId);

            entity.HasOne(d => d.ReviewedByUser).WithMany(p => p.ReviewQueueReviewedByUsers).HasForeignKey(d => d.ReviewedByUserId);

            entity.HasOne(d => d.Task).WithMany(p => p.ReviewQueues)
                .HasForeignKey(d => d.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RoleDiseaseAccess>(entity =>
        {
            entity.ToTable("RoleDiseaseAccess");

            entity.HasIndex(e => e.CreatedByUserId, "IX_RoleDiseaseAccess_CreatedByUserId");

            entity.HasIndex(e => e.DiseaseId, "IX_RoleDiseaseAccess_DiseaseId");

            entity.HasIndex(e => new { e.RoleId, e.DiseaseId }, "IX_RoleDiseaseAccess_RoleId_DiseaseId").IsUnique();

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.RoleDiseaseAccesses)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Disease).WithMany(p => p.RoleDiseaseAccesses).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.Role).WithMany(p => p.RoleDiseaseAccesses).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasIndex(e => e.PermissionId, "IX_RolePermissions_PermissionId");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions).HasForeignKey(d => d.PermissionId);

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<SpecimenType>(entity =>
        {
            entity.Property(e => e.BodySite).HasMaxLength(100);
            entity.Property(e => e.CollectionMethod).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.Hl7Code).HasMaxLength(20);
            entity.Property(e => e.LoincSystemCode).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SnomedCode).HasMaxLength(20);
            entity.Property(e => e.SnomedDisplay).HasMaxLength(200);
        });

        modelBuilder.Entity<SurveyFieldMapping>(entity =>
        {
            entity.HasIndex(e => new { e.ConfigurationType, e.ConfigurationId, e.DisplayOrder }, "IX_SurveyFieldMapping_Config_Order");

            entity.HasIndex(e => new { e.ConfigurationType, e.ConfigurationId, e.SurveyQuestionName }, "IX_SurveyFieldMapping_Config_Question");

            entity.HasIndex(e => e.IsActive, "IX_SurveyFieldMapping_IsActive");

            entity.HasIndex(e => e.TargetFieldPath, "IX_SurveyFieldMapping_TargetField");

            entity.HasIndex(e => e.CreatedByUserId, "IX_SurveyFieldMappings_CreatedByUserId");

            entity.HasIndex(e => e.LastModifiedByUserId, "IX_SurveyFieldMappings_LastModifiedByUserId");

            entity.HasIndex(e => e.TargetSymptomId, "IX_SurveyFieldMappings_TargetSymptomId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.DisplayName).HasMaxLength(500);
            entity.Property(e => e.SurveyQuestionName).HasMaxLength(500);
            entity.Property(e => e.TargetFieldPath).HasMaxLength(500);
            entity.Property(e => e.TransformationScript).HasMaxLength(2000);
            entity.Property(e => e.ValidationRules).HasMaxLength(2000);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.SurveyFieldMappingCreatedByUsers).HasForeignKey(d => d.CreatedByUserId);

            entity.HasOne(d => d.LastModifiedByUser).WithMany(p => p.SurveyFieldMappingLastModifiedByUsers).HasForeignKey(d => d.LastModifiedByUserId);

            entity.HasOne(d => d.TargetSymptom).WithMany(p => p.SurveyFieldMappings)
                .HasForeignKey(d => d.TargetSymptomId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SurveySubmissionLog>(entity =>
        {
            entity.HasIndex(e => e.CaseId, "IX_SurveySubmissionLogs_CaseId");

            entity.HasIndex(e => e.ReviewQueueItemId, "IX_SurveySubmissionLogs_ReviewQueueItemId");

            entity.HasIndex(e => e.TaskId, "IX_SurveySubmissionLogs_TaskId");

            entity.Property(e => e.CaseReference).HasMaxLength(50);
            entity.Property(e => e.DiseaseName).HasMaxLength(200);
            entity.Property(e => e.IssuesSummary).HasMaxLength(2000);
            entity.Property(e => e.PatientName).HasMaxLength(200);
            entity.Property(e => e.SubmittedByName).HasMaxLength(200);
            entity.Property(e => e.SubmittedByUserId).HasMaxLength(450);
            entity.Property(e => e.SurveyName).HasMaxLength(200);
            entity.Property(e => e.TaskName).HasMaxLength(200);

            entity.HasOne(d => d.Case).WithMany(p => p.SurveySubmissionLogs).HasForeignKey(d => d.CaseId);

            entity.HasOne(d => d.ReviewQueueItem).WithMany(p => p.SurveySubmissionLogs).HasForeignKey(d => d.ReviewQueueItemId);

            entity.HasOne(d => d.Task).WithMany(p => p.SurveySubmissionLogs).HasForeignKey(d => d.TaskId);
        });

        modelBuilder.Entity<SurveyTemplate>(entity =>
        {
            entity.HasIndex(e => e.Category, "IX_SurveyTemplates_Category");

            entity.HasIndex(e => e.IsActive, "IX_SurveyTemplates_IsActive");

            entity.HasIndex(e => e.Name, "IX_SurveyTemplates_Name");

            entity.HasIndex(e => e.ParentSurveyTemplateId, "IX_SurveyTemplates_ParentSurveyTemplateId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.PublishedBy).HasMaxLength(256);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.VersionNotes).HasMaxLength(2000);
            entity.Property(e => e.VersionNumber).HasMaxLength(20);

            entity.HasOne(d => d.ParentSurveyTemplate).WithMany(p => p.InverseParentSurveyTemplate).HasForeignKey(d => d.ParentSurveyTemplateId);
        });

        modelBuilder.Entity<SurveyTemplateDisease>(entity =>
        {
            entity.HasIndex(e => e.DiseaseId, "IX_SurveyTemplateDiseases_DiseaseId");

            entity.HasIndex(e => new { e.SurveyTemplateId, e.DiseaseId }, "IX_SurveyTemplateDiseases_SurveyTemplateId_DiseaseId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Disease).WithMany(p => p.SurveyTemplateDiseases).HasForeignKey(d => d.DiseaseId);

            entity.HasOne(d => d.SurveyTemplate).WithMany(p => p.SurveyTemplateDiseases).HasForeignKey(d => d.SurveyTemplateId);
        });

        modelBuilder.Entity<Symptom>(entity =>
        {
            entity.HasIndex(e => e.Code, "IX_Symptoms_Code")
                .IsUnique()
                .HasFilter("([Code] IS NOT NULL)");

            entity.HasIndex(e => new { e.IsDeleted, e.IsActive, e.SortOrder }, "IX_Symptoms_IsDeleted_IsActive_SortOrder");

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TaskCallAttempt>(entity =>
        {
            entity.HasIndex(e => e.AttemptedByUserId, "IX_TaskCallAttempts_AttemptedByUserId");

            entity.HasIndex(e => e.TaskId, "IX_TaskCallAttempts_TaskId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.PhoneNumberCalled).HasMaxLength(50);

            entity.HasOne(d => d.AttemptedByUser).WithMany(p => p.TaskCallAttempts).HasForeignKey(d => d.AttemptedByUserId);

            entity.HasOne(d => d.Task).WithMany(p => p.TaskCallAttempts).HasForeignKey(d => d.TaskId);
        });

        modelBuilder.Entity<TaskTemplate>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_TaskTemplates_IsActive");

            entity.HasIndex(e => e.Name, "IX_TaskTemplates_Name");

            entity.HasIndex(e => e.SurveyTemplateId, "IX_TaskTemplates_SurveyTemplateId");

            entity.HasIndex(e => e.TaskTypeId, "IX_TaskTemplates_TaskTypeId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CompletionCriteria).HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Instructions).HasMaxLength(4000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RestrictToSubDiseaseIds).HasMaxLength(500);

            entity.HasOne(d => d.SurveyTemplate).WithMany(p => p.TaskTemplates)
                .HasForeignKey(d => d.SurveyTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.TaskType).WithMany(p => p.TaskTemplates)
                .HasForeignKey(d => d.TaskTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TaskType>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_TaskTypes_IsActive");

            entity.HasIndex(e => e.Name, "IX_TaskTypes_Name");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.ColorClass).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconClass).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<TestMethod>(entity =>
        {
            entity.HasIndex(e => new { e.IsActive, e.DisplayOrder }, "IX_TestMethods_IsActive_DisplayOrder");

            entity.HasIndex(e => e.Name, "IX_TestMethods_Name");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.LoincMethodCode).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SnomedCode).HasMaxLength(20);
            entity.Property(e => e.SnomedDisplay).HasMaxLength(200);
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasIndex(e => e.TestTypeId, "IX_TestResults_TestTypeId");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.Hl7Code).HasMaxLength(20);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.SnomedCode).HasMaxLength(20);
            entity.Property(e => e.SnomedDisplay).HasMaxLength(200);

            entity.HasOne(d => d.TestType).WithMany(p => p.TestResults).HasForeignKey(d => d.TestTypeId);
        });

        modelBuilder.Entity<TestType>(entity =>
        {
            entity.ToTable("TestType");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ExportCode).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<UserDiseaseAccess>(entity =>
        {
            entity.ToTable("UserDiseaseAccess");

            entity.HasIndex(e => e.DiseaseId, "IX_UserDiseaseAccess_DiseaseId");

            entity.HasIndex(e => e.ExpiresAt, "IX_UserDiseaseAccess_ExpiresAt");

            entity.HasIndex(e => e.GrantedByUserId, "IX_UserDiseaseAccess_GrantedByUserId");

            entity.HasIndex(e => new { e.UserId, e.DiseaseId }, "IX_UserDiseaseAccess_UserId_DiseaseId").IsUnique();

            entity.Property(e => e.Reason).HasMaxLength(500);

            entity.HasOne(d => d.Disease).WithMany(p => p.UserDiseaseAccesses)
                .HasForeignKey(d => d.DiseaseId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.GrantedByUser).WithMany(p => p.UserDiseaseAccessGrantedByUsers)
                .HasForeignKey(d => d.GrantedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.User).WithMany(p => p.UserDiseaseAccessUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PermissionId });

            entity.HasIndex(e => e.PermissionId, "IX_UserPermissions_PermissionId");

            entity.HasOne(d => d.Permission).WithMany(p => p.UserPermissions).HasForeignKey(d => d.PermissionId);

            entity.HasOne(d => d.User).WithMany(p => p.UserPermissions).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<VwCaseContactTasksFlattened>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CaseContactTasksFlattened");

            entity.Property(e => e.AssignedToEmail).HasMaxLength(256);
            entity.Property(e => e.AssignedToName).HasMaxLength(201);
            entity.Property(e => e.AssignmentType)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.CaseCreatedAt).HasColumnType("datetime");
            entity.Property(e => e.CaseNumber).HasMaxLength(20);
            entity.Property(e => e.CaseType)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.CaseUpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.ConfidenceLevel)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.ContactClassification).HasMaxLength(100);
            entity.Property(e => e.DiseaseCode).HasMaxLength(50);
            entity.Property(e => e.DiseaseName).HasMaxLength(200);
            entity.Property(e => e.EventName).HasMaxLength(200);
            entity.Property(e => e.EventOrganizer).HasMaxLength(200);
            entity.Property(e => e.EventSetting)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.ExposureDescription).HasMaxLength(2000);
            entity.Property(e => e.ExposureStatusDisplay)
                .HasMaxLength(19)
                .IsUnicode(false);
            entity.Property(e => e.ExposureType)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.Jurisdiction1).HasMaxLength(200);
            entity.Property(e => e.Jurisdiction2).HasMaxLength(200);
            entity.Property(e => e.Jurisdiction3).HasMaxLength(200);
            entity.Property(e => e.LocationAddress).HasMaxLength(500);
            entity.Property(e => e.LocationIsHighRisk)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.LocationOrganization).HasMaxLength(200);
            entity.Property(e => e.LocationType).HasMaxLength(100);
            entity.Property(e => e.PatientDob).HasColumnName("PatientDOB");
            entity.Property(e => e.PatientId).HasMaxLength(20);
            entity.Property(e => e.SurveyStatus)
                .HasMaxLength(18)
                .IsUnicode(false);
            entity.Property(e => e.TaskDescription).HasMaxLength(2000);
            entity.Property(e => e.TaskDueStatus)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TaskNumber).HasMaxLength(50);
            entity.Property(e => e.TaskPriority)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.TaskStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TaskTitle).HasMaxLength(200);
            entity.Property(e => e.TaskType).HasMaxLength(100);
        });

        modelBuilder.Entity<VwCaseTimelineAll>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CaseTimelineAll");

            entity.Property(e => e.ActorName).HasMaxLength(450);
            entity.Property(e => e.EventType)
                .HasMaxLength(16)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwContactTracingMindMapEdge>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ContactTracingMindMapEdges");

            entity.Property(e => e.ConfidenceLevel)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.EdgeType)
                .HasMaxLength(16)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwContactTracingMindMapNode>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ContactTracingMindMapNodes");

            entity.Property(e => e.DiseaseName).HasMaxLength(200);
            entity.Property(e => e.NodeLabel).HasMaxLength(20);
        });

        modelBuilder.Entity<VwContactsListSimple>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ContactsListSimple");

            entity.Property(e => e.ContactDob).HasColumnName("ContactDOB");
            entity.Property(e => e.ContactNumber).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DiseaseName).HasMaxLength(200);
            entity.Property(e => e.ExposureType)
                .HasMaxLength(16)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwOutbreakTasksFlattened>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_OutbreakTasksFlattened");

            entity.Property(e => e.AssignedToEmail).HasMaxLength(256);
            entity.Property(e => e.AssignedToName).HasMaxLength(201);
            entity.Property(e => e.CaseNumber).HasMaxLength(20);
            entity.Property(e => e.DiseaseName)
                .HasMaxLength(1)
                .IsUnicode(false);
            entity.Property(e => e.OutbreakName).HasMaxLength(200);
            entity.Property(e => e.OutbreakReferenceNumber).HasMaxLength(50);
            entity.Property(e => e.TaskStatus)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TaskTitle).HasMaxLength(200);
            entity.Property(e => e.TaskType).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
