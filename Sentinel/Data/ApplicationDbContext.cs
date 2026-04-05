using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Ancestry> Ancestries { get; set; }
        public DbSet<AboriginalTorresStraitIslanderStatus> AtsiStatuses { get; set; }
        public DbSet<SexAtBirth> SexAtBirths { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<Occupation> Occupations { get; set; }
        public DbSet<CaseStatus> CaseStatuses { get; set; }
        public DbSet<DiseaseCategory> DiseaseCategories { get; set; }
        public DbSet<Disease> Diseases { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Groups for organizing users
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }

        // Permissions System
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        
        // Disease Access Control
        public DbSet<RoleDiseaseAccess> RoleDiseaseAccess { get; set; }
        public DbSet<UserDiseaseAccess> UserDiseaseAccess { get; set; }

        // Custom Fields System
        public DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; set; }
        public DbSet<LookupTable> LookupTables { get; set; }
        public DbSet<LookupValue> LookupValues { get; set; }
        public DbSet<PatientCustomFieldString> PatientCustomFieldStrings { get; set; }
        public DbSet<PatientCustomFieldNumber> PatientCustomFieldNumbers { get; set; }
        public DbSet<PatientCustomFieldDate> PatientCustomFieldDates { get; set; }
        public DbSet<PatientCustomFieldBoolean> PatientCustomFieldBooleans { get; set; }
        public DbSet<PatientCustomFieldLookup> PatientCustomFieldLookups { get; set; }

        // Disease Custom Fields Junction and Case Custom Fields System
        public DbSet<DiseaseCustomField> DiseaseCustomFields { get; set; }
        public DbSet<CaseCustomFieldString> CaseCustomFieldStrings { get; set; }
        public DbSet<CaseCustomFieldNumber> CaseCustomFieldNumbers { get; set; }
        public DbSet<CaseCustomFieldDate> CaseCustomFieldDates { get; set; }
        public DbSet<CaseCustomFieldBoolean> CaseCustomFieldBooleans { get; set; }
        public DbSet<CaseCustomFieldLookup> CaseCustomFieldLookups { get; set; }

        // Laboratory Results System
        public DbSet<LabResult> LabResults { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationType> OrganizationTypes { get; set; }
        public DbSet<SpecimenType> SpecimenTypes { get; set; }
        public DbSet<TestType> TestTypes { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<ResultUnits> ResultUnits { get; set; }

        // Symptom Tracking System
        public DbSet<Symptom> Symptoms { get; set; }
        public DbSet<CaseSymptom> CaseSymptoms { get; set; }
        public DbSet<DiseaseSymptom> DiseaseSymptoms { get; set; }

        // Exposure Tracking System
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationType> LocationTypes { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<ExposureEvent> ExposureEvents { get; set; }
        public DbSet<ContactClassification> ContactClassifications { get; set; }

        // Task Management System
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TaskType> TaskTypes { get; set; }
        public DbSet<CaseTask> CaseTasks { get; set; }
        public DbSet<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; }
        public DbSet<TaskCallAttempt> TaskCallAttempts { get; set; }

        // Survey System
        public DbSet<SurveyTemplate> SurveyTemplates { get; set; }
        public DbSet<SurveyTemplateDisease> SurveyTemplateDiseases { get; set; }
        public DbSet<SurveyFieldMapping> SurveyFieldMappings { get; set; }
        public DbSet<SurveySubmissionLog> SurveySubmissionLogs { get; set; }

        // Outbreak Management System
        public DbSet<Outbreak> Outbreaks { get; set; }
        public DbSet<OutbreakTeamMember> OutbreakTeamMembers { get; set; }
        public DbSet<OutbreakCaseDefinition> OutbreakCaseDefinitions { get; set; }
        public DbSet<OutbreakCase> OutbreakCases { get; set; }
        public DbSet<OutbreakTimeline> OutbreakTimelines { get; set; }
        public DbSet<OutbreakSearchQuery> OutbreakSearchQueries { get; set; }
        public DbSet<OutbreakLineListConfiguration> OutbreakLineListConfigurations { get; set; }

        // Jurisdiction System
        public DbSet<JurisdictionType> JurisdictionTypes { get; set; }
        public DbSet<Jurisdiction> Jurisdictions { get; set; }

        // Reporting System
        public DbSet<Models.Reporting.ReportDefinition> ReportDefinitions { get; set; }
        public DbSet<Models.Reporting.ReportField> ReportFields { get; set; }
        public DbSet<Models.Reporting.ReportFilter> ReportFilters { get; set; }
        public DbSet<Models.Reporting.CalculatedField> CalculatedFields { get; set; }
        public DbSet<Models.Reporting.ReportFolder> ReportFolders { get; set; }
        public DbSet<Models.Reporting.ReportFolderShare> ReportFolderShares { get; set; }

        // Data Review System
        public DbSet<ReviewQueue> ReviewQueue { get; set; }

        // Backup System
        public DbSet<BackupHistory> BackupHistory { get; set; }

        // Flattened Report Views
        public DbSet<Models.Views.CaseContactTaskFlattened> CaseContactTasksFlattened { get; set; }
        public DbSet<Models.Views.OutbreakTaskFlattened> OutbreakTasksFlattened { get; set; }
        public DbSet<Models.Views.CaseTimelineEvent> CaseTimelineAll { get; set; }
        public DbSet<Models.Views.ContactTracingMindMapNode> ContactTracingMindMapNodes { get; set; }
        public DbSet<Models.Views.ContactTracingMindMapEdge> ContactTracingMindMapEdges { get; set; }
        public DbSet<Models.Views.ContactListSimple> ContactsListSimple { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserGroup>().HasKey(ug => new { ug.UserId, ug.GroupId });

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);

            // Permissions Configuration
            builder.Entity<Permission>()
                .HasIndex(p => new { p.Module, p.Action })
                .IsUnique();

            builder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            builder.Entity<UserPermission>()
                .HasKey(up => new { up.UserId, up.PermissionId });

            builder.Entity<UserPermission>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(up => up.UserId);

            builder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(up => up.PermissionId);

            builder.Entity<Patient>()
                .HasIndex(p => p.FriendlyId)
                .IsUnique();

            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId });

            builder.Entity<AuditLog>()
                .HasIndex(a => a.ChangedAt);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.ChangedByUserId);

            builder.Entity<AuditLog>()
                .HasOne(a => a.ChangedByUser)
                .WithMany()
                .HasForeignKey(a => a.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Custom Fields Configuration
            builder.Entity<CustomFieldDefinition>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<CustomFieldDefinition>()
                .HasIndex(c => new { c.Category, c.DisplayOrder });

            // Lookup Tables
            builder.Entity<LookupTable>()
                .HasIndex(l => l.Name)
                .IsUnique();

            builder.Entity<LookupValue>()
                .HasIndex(l => new { l.LookupTableId, l.DisplayOrder });

            // Patient Custom Fields - Composite keys for uniqueness
            builder.Entity<PatientCustomFieldString>()
                .HasIndex(p => new { p.PatientId, p.FieldDefinitionId })
                .IsUnique();

            builder.Entity<PatientCustomFieldNumber>()
                .HasIndex(p => new { p.PatientId, p.FieldDefinitionId })
                .IsUnique();

            builder.Entity<PatientCustomFieldDate>()
                .HasIndex(p => new { p.PatientId, p.FieldDefinitionId })
                .IsUnique();

            builder.Entity<PatientCustomFieldBoolean>()
                .HasIndex(p => new { p.PatientId, p.FieldDefinitionId })
                .IsUnique();

            builder.Entity<PatientCustomFieldLookup>()
                .HasIndex(p => new { p.PatientId, p.FieldDefinitionId })
                .IsUnique();

            // Searchable value indexes
            builder.Entity<PatientCustomFieldString>()
                .HasIndex(p => p.Value);

            builder.Entity<PatientCustomFieldNumber>()
                .HasIndex(p => p.Value);

            builder.Entity<PatientCustomFieldDate>()
                .HasIndex(p => p.Value);

            // DiseaseCustomField Junction Table Configuration
            builder.Entity<DiseaseCustomField>()
                .HasIndex(dc => new { dc.DiseaseId, dc.CustomFieldDefinitionId })
                .IsUnique();

            builder.Entity<DiseaseCustomField>()
                .HasOne(dc => dc.Disease)
                .WithMany(d => d.DiseaseCustomFields)
                .HasForeignKey(dc => dc.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiseaseCustomField>()
                .HasOne(dc => dc.CustomFieldDefinition)
                .WithMany(cf => cf.DiseaseCustomFields)
                .HasForeignKey(dc => dc.CustomFieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Case Custom Fields - Composite keys for uniqueness
            builder.Entity<CaseCustomFieldString>()
                .HasIndex(c => new { c.CaseId, c.FieldDefinitionId })
                .IsUnique();

            builder.Entity<CaseCustomFieldNumber>()
                .HasIndex(c => new { c.CaseId, c.FieldDefinitionId })
                .IsUnique();

            builder.Entity<CaseCustomFieldDate>()
                .HasIndex(c => new { c.CaseId, c.FieldDefinitionId })
                .IsUnique();

            builder.Entity<CaseCustomFieldBoolean>()
                .HasIndex(c => new { c.CaseId, c.FieldDefinitionId })
                .IsUnique();

            builder.Entity<CaseCustomFieldLookup>()
                .HasIndex(c => new { c.CaseId, c.FieldDefinitionId })
                .IsUnique();

            // Searchable value indexes for Case custom fields
            builder.Entity<CaseCustomFieldString>()
                .HasIndex(c => c.Value);

            builder.Entity<CaseCustomFieldNumber>()
                .HasIndex(c => c.Value);

            builder.Entity<CaseCustomFieldDate>()
                .HasIndex(c => c.Value);

            // Disease Category Configuration
            builder.Entity<DiseaseCategory>()
                .HasIndex(dc => dc.Name)
                .IsUnique();

            builder.Entity<DiseaseCategory>()
                .HasIndex(dc => dc.ReportingId)
                .IsUnique();

            builder.Entity<DiseaseCategory>()
                .HasIndex(dc => dc.DisplayOrder);

            // Disease Configuration
            builder.Entity<Disease>()
                .HasOne(d => d.DiseaseCategory)
                .WithMany(dc => dc.Diseases)
                .HasForeignKey(d => d.DiseaseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Disease>()
                .HasOne(d => d.ParentDisease)
                .WithMany(d => d.SubDiseases)
                .HasForeignKey(d => d.ParentDiseaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Disease>()
                .HasIndex(d => d.Code)
                .IsUnique();

            builder.Entity<Disease>()
                .HasIndex(d => d.ExportCode);

            builder.Entity<Disease>()
                .HasIndex(d => d.PathIds);

            builder.Entity<Disease>()
                .HasIndex(d => new { d.Level, d.DisplayOrder });

            builder.Entity<Case>()
                .HasOne(c => c.Disease)
                .WithMany(d => d.Cases)
                .HasForeignKey(c => c.DiseaseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Note Configuration
            builder.Entity<Note>()
                .HasOne(n => n.Patient)
                .WithMany(p => p.Notes)
                .HasForeignKey(n => n.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Note>()
                .HasOne(n => n.Case)
                .WithMany(c => c.Notes)
                .HasForeignKey(n => n.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Note>()
                .HasIndex(n => n.PatientId);

            builder.Entity<Note>()
                .HasIndex(n => n.CaseId);

            builder.Entity<Note>()
                .HasIndex(n => n.CreatedAt);

            builder.Entity<Note>()
                .HasIndex(n => n.CreatedBy);

            // LabResult Configuration
            builder.Entity<LabResult>()
                .HasIndex(lr => lr.FriendlyId)
                .IsUnique();

            builder.Entity<LabResult>()
                .HasOne(lr => lr.Case)
                .WithMany(c => c.LabResults)
                .HasForeignKey(lr => lr.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.Laboratory)
                .WithMany(o => o.LabResultsAsLaboratory)
                .HasForeignKey(lr => lr.LaboratoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.OrderingProvider)
                .WithMany(o => o.LabResultsAsOrderingProvider)
                .HasForeignKey(lr => lr.OrderingProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.TestedDisease)
                .WithMany()
                .HasForeignKey(lr => lr.TestedDiseaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.SpecimenType)
                .WithMany(st => st.LabResults)
                .HasForeignKey(lr => lr.SpecimenTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.TestType)
                .WithMany(tt => tt.LabResults)
                .HasForeignKey(lr => lr.TestTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.TestResult)
                .WithMany(tr => tr.LabResults)
                .HasForeignKey(lr => lr.TestResultId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasOne(lr => lr.ResultUnits)
                .WithMany(ru => ru.LabResults)
                .HasForeignKey(lr => lr.ResultUnitsId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<LabResult>()
                .HasIndex(lr => lr.CaseId);

            builder.Entity<LabResult>()
                .HasIndex(lr => lr.AccessionNumber);

            builder.Entity<LabResult>()
                .HasIndex(lr => lr.SpecimenCollectionDate);

            builder.Entity<LabResult>()
                .HasIndex(lr => lr.ResultDate);

            // Organization Configuration
            builder.Entity<Organization>()
                .HasIndex(o => o.FriendlyId)
                .IsUnique();

            builder.Entity<Organization>()
                .HasOne(o => o.OrganizationType)
                .WithMany(ot => ot.Organizations)
                .HasForeignKey(o => o.OrganizationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Organization>()
                .HasIndex(o => o.Name);

            builder.Entity<Organization>()
                .HasIndex(o => o.OrganizationTypeId);

            builder.Entity<Organization>()
                .HasIndex(o => o.ExportCode);

            // Disease Access Control Configuration
            builder.Entity<RoleDiseaseAccess>()
                .HasIndex(rda => new { rda.RoleId, rda.DiseaseId })
                .IsUnique();

            builder.Entity<RoleDiseaseAccess>()
                .HasOne(rda => rda.Disease)
                .WithMany(d => d.RoleDiseaseAccess)
                .HasForeignKey(rda => rda.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RoleDiseaseAccess>()
                .HasOne(rda => rda.CreatedByUser)
                .WithMany()
                .HasForeignKey(rda => rda.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserDiseaseAccess>()
                .HasIndex(uda => new { uda.UserId, uda.DiseaseId })
                .IsUnique();

            builder.Entity<UserDiseaseAccess>()
                .HasOne(uda => uda.User)
                .WithMany(u => u.UserDiseaseAccess)
                .HasForeignKey(uda => uda.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserDiseaseAccess>()
                .HasOne(uda => uda.Disease)
                .WithMany(d => d.UserDiseaseAccess)
                .HasForeignKey(uda => uda.DiseaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserDiseaseAccess>()
                .HasOne(uda => uda.GrantedByUser)
                .WithMany()
                .HasForeignKey(uda => uda.GrantedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserDiseaseAccess>()
                .HasIndex(uda => uda.ExpiresAt);

            // Symptom Tracking Configuration
            builder.Entity<Symptom>()
                .HasIndex(s => s.Code)
                .IsUnique()
                .HasFilter("[Code] IS NOT NULL");

            builder.Entity<Symptom>()
                .HasIndex(s => new { s.IsDeleted, s.IsActive, s.SortOrder });

            builder.Entity<CaseSymptom>()
                .HasOne(cs => cs.Case)
                .WithMany(c => c.CaseSymptoms)
                .HasForeignKey(cs => cs.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CaseSymptom>()
                .HasOne(cs => cs.Symptom)
                .WithMany(s => s.CaseSymptoms)
                .HasForeignKey(cs => cs.SymptomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseSymptom>()
                .HasIndex(cs => new { cs.CaseId, cs.SymptomId })
                .HasFilter("[IsDeleted] = 0");

            builder.Entity<CaseSymptom>()
                .HasIndex(cs => cs.OnsetDate)
                .HasFilter("[IsDeleted] = 0");

            builder.Entity<DiseaseSymptom>()
                .HasOne(ds => ds.Disease)
                .WithMany(d => d.DiseaseSymptoms)
                .HasForeignKey(ds => ds.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiseaseSymptom>()
                .HasOne(ds => ds.Symptom)
                .WithMany(s => s.DiseaseSymptoms)
                .HasForeignKey(ds => ds.SymptomId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DiseaseSymptom>()
                .HasIndex(ds => new { ds.DiseaseId, ds.SymptomId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Entity<DiseaseSymptom>()
                .HasIndex(ds => new { ds.DiseaseId, ds.IsCommon, ds.SortOrder })
                .HasFilter("[IsDeleted] = 0");

            // Exposure Tracking Configuration
            
            // Location Configuration
            builder.Entity<Location>()
                .HasOne(l => l.LocationType)
                .WithMany(lt => lt.Locations)
                .HasForeignKey(l => l.LocationTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Location>()
                .HasOne(l => l.Organization)
                .WithMany()
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Location>()
                .HasIndex(l => l.Name);

            builder.Entity<Location>()
                .HasIndex(l => l.LocationTypeId);

            builder.Entity<Location>()
                .HasIndex(l => new { l.Latitude, l.Longitude });

            builder.Entity<Location>()
                .HasIndex(l => l.GeocodingStatus);

            // Event Configuration
            builder.Entity<Event>()
                .HasOne(e => e.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.Location)
                .WithMany(l => l.Events)
                .HasForeignKey(e => e.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasOne(e => e.OrganizerOrganization)
                .WithMany()
                .HasForeignKey(e => e.OrganizerOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Event>()
                .HasIndex(e => e.Name);

            builder.Entity<Event>()
                .HasIndex(e => e.LocationId);

            builder.Entity<Event>()
                .HasIndex(e => new { e.StartDateTime, e.EndDateTime });

            // ExposureEvent Configuration
            builder.Entity<ExposureEvent>()
                .HasOne(ee => ee.ExposedCase)
                .WithMany(c => c.ExposureEvents)
                .HasForeignKey(ee => ee.ExposedCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasOne(ee => ee.Event)
                .WithMany(e => e.ExposureEvents)
                .HasForeignKey(ee => ee.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasOne(ee => ee.Location)
                .WithMany(l => l.ExposureEvents)
                .HasForeignKey(ee => ee.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasOne(ee => ee.SourceCase)
                .WithMany()
                .HasForeignKey(ee => ee.SourceCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasOne(ee => ee.ContactClassification)
                .WithMany(cc => cc.ExposureEvents)
                .HasForeignKey(ee => ee.ContactClassificationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.ExposedCaseId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.EventId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.LocationId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.SourceCaseId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.ExposureType);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.ExposureStatus);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => new { ee.ExposureStartDate, ee.ExposureEndDate });

            // Configure decimal precision for ExposureEvent geocoding
            builder.Entity<ExposureEvent>()
                .Property(ee => ee.Latitude)
                .HasPrecision(18, 6);

            builder.Entity<ExposureEvent>()
                .Property(ee => ee.Longitude)
                .HasPrecision(18, 6);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => new { ee.Latitude, ee.Longitude });

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.City);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.State);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.PostalCode);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.IsReportingExposure);

            // Task Management System Configuration
            builder.Entity<TaskType>()
                .HasIndex(tt => tt.Name);

            builder.Entity<TaskType>()
                .HasIndex(tt => tt.IsActive);

            builder.Entity<TaskTemplate>()
                .HasIndex(tt => tt.Name);

            builder.Entity<TaskTemplate>()
                .HasOne(tt => tt.TaskType)
                .WithMany(t => t.TaskTemplates)
                .HasForeignKey(tt => tt.TaskTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TaskTemplate>()
                .HasIndex(tt => tt.TaskTypeId);

            builder.Entity<TaskTemplate>()
                .HasIndex(tt => tt.IsActive);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.Case)
                .WithMany()
                .HasForeignKey(ct => ct.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.TaskTemplate)
                .WithMany(tt => tt.CaseTasks)
                .HasForeignKey(ct => ct.TaskTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.TaskType)
                .WithMany()
                .HasForeignKey(ct => ct.TaskTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.AssignedToUser)
                .WithMany()
                .HasForeignKey(ct => ct.AssignedToUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.CompletedByUser)
                .WithMany()
                .HasForeignKey(ct => ct.CompletedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<CaseTask>()
                .HasOne(ct => ct.ParentTask)
                .WithMany(pt => pt.RecurrenceInstances)
                .HasForeignKey(ct => ct.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CaseTask>()
                .HasIndex(ct => ct.CaseId);

            builder.Entity<CaseTask>()
                .HasIndex(ct => ct.Status);

            builder.Entity<CaseTask>()
                .HasIndex(ct => ct.DueDate);

            builder.Entity<CaseTask>()
                .HasIndex(ct => ct.AssignedToUserId);

            builder.Entity<CaseTask>()
                .HasIndex(ct => ct.Priority);

            builder.Entity<DiseaseTaskTemplate>()
                .HasOne(dtt => dtt.Disease)
                .WithMany()
                .HasForeignKey(dtt => dtt.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiseaseTaskTemplate>()
                .HasOne(dtt => dtt.TaskTemplate)
                .WithMany(tt => tt.DiseaseTaskTemplates)
                .HasForeignKey(dtt => dtt.TaskTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DiseaseTaskTemplate>()
                .HasIndex(dtt => new { dtt.DiseaseId, dtt.TaskTemplateId })
                .IsUnique();

            builder.Entity<DiseaseTaskTemplate>()
                .HasIndex(dtt => dtt.IsInherited);

            builder.Entity<DiseaseTaskTemplate>()
                .HasIndex(dtt => dtt.InheritedFromDiseaseId);

            // Survey Template Configuration
            builder.Entity<SurveyTemplate>()
                .HasIndex(st => st.Name);

            builder.Entity<SurveyTemplate>()
                .HasIndex(st => st.Category);

            builder.Entity<SurveyTemplate>()
                .HasIndex(st => st.IsActive);

            builder.Entity<SurveyTemplateDisease>()
                .HasOne(std => std.SurveyTemplate)
                .WithMany(st => st.ApplicableDiseases)
                .HasForeignKey(std => std.SurveyTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SurveyTemplateDisease>()
                .HasOne(std => std.Disease)
                .WithMany()
                .HasForeignKey(std => std.DiseaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SurveyTemplateDisease>()
                .HasIndex(std => new { std.SurveyTemplateId, std.DiseaseId })
                .IsUnique();

            builder.Entity<TaskTemplate>()
                .HasOne(tt => tt.SurveyTemplate)
                .WithMany(st => st.TaskTemplates)
                .HasForeignKey(tt => tt.SurveyTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<TaskTemplate>()
                .HasIndex(tt => tt.SurveyTemplateId);

            // Survey Field Mapping Configuration
            builder.Entity<SurveyFieldMapping>()
                .HasIndex(sfm => new { sfm.ConfigurationType, sfm.ConfigurationId, sfm.SurveyQuestionName })
                .HasDatabaseName("IX_SurveyFieldMapping_Config_Question");

            builder.Entity<SurveyFieldMapping>()
                .HasIndex(sfm => new { sfm.ConfigurationType, sfm.ConfigurationId, sfm.DisplayOrder })
                .HasDatabaseName("IX_SurveyFieldMapping_Config_Order");

            builder.Entity<SurveyFieldMapping>()
                .HasIndex(sfm => sfm.TargetFieldPath)
                .HasDatabaseName("IX_SurveyFieldMapping_TargetField");

            builder.Entity<SurveyFieldMapping>()
                .HasIndex(sfm => sfm.IsActive)
                .HasDatabaseName("IX_SurveyFieldMapping_IsActive");

            builder.Entity<SurveyFieldMapping>()
                .HasOne(sfm => sfm.CreatedBy)
                .WithMany()
                .HasForeignKey(sfm => sfm.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SurveyFieldMapping>()
                .HasOne(sfm => sfm.LastModifiedBy)
                .WithMany()
                .HasForeignKey(sfm => sfm.LastModifiedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SurveyFieldMapping>()
                .HasOne(sfm => sfm.TargetSymptom)
                .WithMany()
                .HasForeignKey(sfm => sfm.TargetSymptomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Jurisdiction System Configuration
            builder.Entity<JurisdictionType>()
                .HasIndex(jt => jt.FieldNumber)
                .IsUnique();

            builder.Entity<JurisdictionType>()
                .HasIndex(jt => jt.Name);

            builder.Entity<Jurisdiction>()
                .HasOne(j => j.JurisdictionType)
                .WithMany(jt => jt.Jurisdictions)
                .HasForeignKey(j => j.JurisdictionTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Jurisdiction>()
                .HasOne(j => j.ParentJurisdiction)
                .WithMany(j => j.ChildJurisdictions)
                .HasForeignKey(j => j.ParentJurisdictionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Jurisdiction>()
                .HasIndex(j => j.Name);

            builder.Entity<Jurisdiction>()
                .HasIndex(j => j.JurisdictionTypeId);

            // Patient Jurisdiction Relationships
            builder.Entity<Patient>()
                .HasOne(p => p.Jurisdiction1)
                .WithMany()
                .HasForeignKey(p => p.Jurisdiction1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasOne(p => p.Jurisdiction2)
                .WithMany()
                .HasForeignKey(p => p.Jurisdiction2Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasOne(p => p.Jurisdiction3)
                .WithMany()
                .HasForeignKey(p => p.Jurisdiction3Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasOne(p => p.Jurisdiction4)
                .WithMany()
                .HasForeignKey(p => p.Jurisdiction4Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Patient>()
                .HasOne(p => p.Jurisdiction5)
                .WithMany()
                .HasForeignKey(p => p.Jurisdiction5Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Case Jurisdiction Relationships
            builder.Entity<Case>()
                .HasOne(c => c.Jurisdiction1)
                .WithMany()
                .HasForeignKey(c => c.Jurisdiction1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Jurisdiction2)
                .WithMany()
                .HasForeignKey(c => c.Jurisdiction2Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Jurisdiction3)
                .WithMany()
                .HasForeignKey(c => c.Jurisdiction3Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Jurisdiction4)
                .WithMany()
                .HasForeignKey(c => c.Jurisdiction4Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Case>()
                .HasOne(c => c.Jurisdiction5)
                .WithMany()
                .HasForeignKey(c => c.Jurisdiction5Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Reporting System Configuration
            builder.Entity<Models.Reporting.ReportDefinition>()
                .HasIndex(rd => rd.EntityType);

            builder.Entity<Models.Reporting.ReportDefinition>()
                .HasIndex(rd => rd.Category);

            builder.Entity<Models.Reporting.ReportDefinition>()
                .HasIndex(rd => rd.CreatedByUserId);

            builder.Entity<Models.Reporting.ReportField>()
                .HasOne(rf => rf.ReportDefinition)
                .WithMany(rd => rd.Fields)
                .HasForeignKey(rf => rf.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Models.Reporting.ReportField>()
                .HasIndex(rf => rf.FieldPath);

            builder.Entity<Models.Reporting.ReportFilter>()
                .HasOne(rf => rf.ReportDefinition)
                .WithMany(rd => rd.Filters)
                .HasForeignKey(rf => rf.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Models.Reporting.CalculatedField>()
                .HasOne(cf => cf.ReportDefinition)
                .WithMany(rd => rd.CalculatedFields)
                .HasForeignKey(cf => cf.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review Queue Configuration
            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.Case)
                .WithMany()
                .HasForeignKey(rq => rq.CaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.Patient)
                .WithMany()
                .HasForeignKey(rq => rq.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.Disease)
                .WithMany()
                .HasForeignKey(rq => rq.DiseaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.ReviewedBy)
                .WithMany()
                .HasForeignKey(rq => rq.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.CreatedBy)
                .WithMany()
                .HasForeignKey(rq => rq.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ReviewQueue>()
                .HasOne(rq => rq.Task)
                .WithMany()
                .HasForeignKey(rq => rq.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => new { rq.ReviewStatus, rq.EntityType, rq.DiseaseId, rq.CreatedDate })
                .HasDatabaseName("IX_ReviewQueue_Status_EntityType_Disease_Created");

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => new { rq.GroupKey, rq.CreatedDate })
                .HasDatabaseName("IX_ReviewQueue_GroupKey_Created");

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => rq.CaseId)
                .HasDatabaseName("IX_ReviewQueue_CaseId");

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => rq.PatientId)
                .HasDatabaseName("IX_ReviewQueue_PatientId");

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => rq.ReviewedByUserId)
                .HasDatabaseName("IX_ReviewQueue_ReviewedByUserId");

            builder.Entity<ReviewQueue>()
                .HasIndex(rq => rq.TaskId)
                .HasDatabaseName("IX_ReviewQueue_TaskId");

            // Global Query Filters for Soft Delete
            builder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
            builder.Entity<LabResult>().HasQueryFilter(lr => !lr.IsDeleted);
            builder.Entity<Note>().HasQueryFilter(n => !n.IsDeleted);
            builder.Entity<Symptom>().HasQueryFilter(s => !s.IsDeleted);
            builder.Entity<CaseSymptom>().HasQueryFilter(cs => !cs.IsDeleted);
            builder.Entity<DiseaseSymptom>().HasQueryFilter(ds => !ds.IsDeleted);
            builder.Entity<ExposureEvent>().HasQueryFilter(ee => !ee.IsDeleted);

            // Global Query Filter for Case - combines soft delete AND disease access control
            builder.Entity<Case>().HasQueryFilter(c => 
                !c.IsDeleted && 
                (c.DiseaseId == null || 
                 c.Disease!.AccessLevel == DiseaseAccessLevel.Public ||
                 _httpContextAccessor == null ||
                 _httpContextAccessor.HttpContext == null ||
                 _httpContextAccessor.HttpContext.Items["AccessibleDiseaseIds"] == null ||
                 ((List<Guid>)_httpContextAccessor.HttpContext.Items["AccessibleDiseaseIds"]).Contains(c.DiseaseId.Value)));

            // Global Query Filter for Disease - disease access control for dropdowns/selections
            builder.Entity<Disease>().HasQueryFilter(d => 
                d.AccessLevel == DiseaseAccessLevel.Public ||
                _httpContextAccessor == null ||
                _httpContextAccessor.HttpContext == null ||
                _httpContextAccessor.HttpContext.Items["AccessibleDiseaseIds"] == null ||
                ((List<Guid>)_httpContextAccessor.HttpContext.Items["AccessibleDiseaseIds"]).Contains(d.Id));

            // Flattened Report Views (SQL views managed by migrations)
            builder.Entity<Models.Views.CaseContactTaskFlattened>()
                .HasNoKey()
                .ToView("vw_CaseContactTasksFlattened");

            builder.Entity<Models.Views.OutbreakTaskFlattened>()
                .HasNoKey()
                .ToView("vw_OutbreakTasksFlattened");

            builder.Entity<Models.Views.CaseTimelineEvent>()
                .HasNoKey()
                .ToView("vw_CaseTimelineAll");

            builder.Entity<Models.Views.ContactTracingMindMapNode>()
                .HasNoKey()
                .ToView("vw_ContactTracingMindMapNodes");

            builder.Entity<Models.Views.ContactTracingMindMapEdge>()
                .HasNoKey()
                .ToView("vw_ContactTracingMindMapEdges");

            builder.Entity<Models.Views.ContactListSimple>()
                .HasNoKey()
                .ToView("vw_ContactsListSimple");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var saveId = Guid.NewGuid().ToString().Substring(0, 8); // Unique ID for this save
            var stackTrace = new System.Diagnostics.StackTrace(true);
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
            
            System.Diagnostics.Debug.WriteLine("====================================");
            System.Diagnostics.Debug.WriteLine($"[REVIEW:{saveId}] SaveChangesAsync CALLED from {callingMethod}");
            System.Diagnostics.Debug.WriteLine("====================================");
            
            await GeneratePatientFriendlyIds();
            await GenerateCaseFriendlyIds();
            await GenerateLabResultFriendlyIds();
            await GenerateOrganizationFriendlyIds();
            await UpdateDiseasePaths();
            UpdateAuditableEntities();
            var auditEntries = OnBeforeSaveChanges();
            
            System.Diagnostics.Debug.WriteLine($"[REVIEW:{saveId}] About to detect review queue items...");
            
            // Detect items for review queue BEFORE saving
            var reviewQueueEntries = await DetectReviewQueueItemsAsync();
            
            System.Diagnostics.Debug.WriteLine($"[REVIEW:{saveId}] Detection complete. Found {reviewQueueEntries.Count} candidates");
            
            var result = await base.SaveChangesAsync(cancellationToken);
            
            await OnAfterSaveChanges(auditEntries);
            
            System.Diagnostics.Debug.WriteLine($"[REVIEW:{saveId}] Queueing {reviewQueueEntries.Count} review items...");
            
            // Queue review items AFTER successful save
            await QueueReviewItemsAsync(reviewQueueEntries);
            
            System.Diagnostics.Debug.WriteLine($"[REVIEW:{saveId}] Queueing complete");
            System.Diagnostics.Debug.WriteLine("====================================");
            
            return result;
        }

        public override int SaveChanges()
        {
            GeneratePatientFriendlyIdsSync();
            GenerateCaseFriendlyIdsSync();
            GenerateLabResultFriendlyIdsSync();
            GenerateOrganizationFriendlyIdsSync();
            UpdateDiseasePathsSync();
            UpdateAuditableEntities();
            var auditEntries = OnBeforeSaveChanges();
            
            // Note: Sync version doesn't queue reviews - use async version for review queueing
            var result = base.SaveChanges();
            OnAfterSaveChangesSync(auditEntries);
            return result;
        }

        private async Task GeneratePatientFriendlyIds()
        {
            var newPatients = ChangeTracker.Entries<Patient>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newPatients.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"P-{year}-";
            
            // Get the last assigned ID from database
            var lastPatient = await Patients
                .Where(p => p.FriendlyId.StartsWith(prefix))
                .OrderByDescending(p => p.FriendlyId)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastPatient != null)
            {
                var lastSequencePart = lastPatient.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            // Assign IDs sequentially to all new patients in this batch
            foreach (var patient in newPatients)
            {
                if (patient.Id == Guid.Empty)
                {
                    patient.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(patient.FriendlyId))
                {
                    patient.FriendlyId = $"{prefix}{nextSequence:D4}"; // Changed from D5 to D4 to match PatientIdGeneratorService
                    nextSequence++;
                }
            }
        }

        private void GeneratePatientFriendlyIdsSync()
        {
            var newPatients = ChangeTracker.Entries<Patient>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newPatients.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"P-{year}-";
            
            var lastPatient = Patients
                .Where(p => p.FriendlyId.StartsWith(prefix))
                .OrderByDescending(p => p.FriendlyId)
                .FirstOrDefault();

            int nextSequence = 1;
            if (lastPatient != null)
            {
                var lastSequencePart = lastPatient.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            foreach (var patient in newPatients)
            {
                if (patient.Id == Guid.Empty)
                {
                    patient.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(patient.FriendlyId))
                {
                    patient.FriendlyId = $"{prefix}{nextSequence:D4}"; // Changed from D5 to D4 to match PatientIdGeneratorService
                    nextSequence++;
                }
            }
        }

        private async Task GenerateCaseFriendlyIds()
        {
            var newCases = ChangeTracker.Entries<Case>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newCases.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"C-{year}-";
            
            // Get the last assigned ID from database
            var lastCase = await Cases
                .Where(c => c.FriendlyId.StartsWith(prefix))
                .OrderByDescending(c => c.FriendlyId)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastCase != null)
            {
                var lastSequencePart = lastCase.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            // Assign IDs sequentially to all new cases in this batch
            foreach (var caseEntity in newCases)
            {
                if (caseEntity.Id == Guid.Empty)
                {
                    caseEntity.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(caseEntity.FriendlyId))
                {
                    caseEntity.FriendlyId = $"{prefix}{nextSequence:D4}"; // Changed from D5 to D4 to match CaseIdGeneratorService
                    nextSequence++;
                }
            }
        }

        private void GenerateCaseFriendlyIdsSync()
        {
            var newCases = ChangeTracker.Entries<Case>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newCases.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"C-{year}-";
            
            var lastCase = Cases
                .Where(c => c.FriendlyId.StartsWith(prefix))
                .OrderByDescending(c => c.FriendlyId)
                .FirstOrDefault();

            int nextSequence = 1;
            if (lastCase != null)
            {
                var lastSequencePart = lastCase.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            foreach (var caseEntity in newCases)
            {
                if (caseEntity.Id == Guid.Empty)
                {
                    caseEntity.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(caseEntity.FriendlyId))
                {
                    caseEntity.FriendlyId = $"{prefix}{nextSequence:D4}"; // Changed from D5 to D4 to match CaseIdGeneratorService
                    nextSequence++;
                }
            }
        }

        private async Task GenerateLabResultFriendlyIds()
        {
            var newLabResults = ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newLabResults.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"LAB-{year}-";
            
            // Get the last assigned ID from database
            var lastLabResult = await LabResults
                .Where(lr => lr.FriendlyId.StartsWith(prefix))
                .OrderByDescending(lr => lr.FriendlyId)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastLabResult != null)
            {
                var lastSequencePart = lastLabResult.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            // Assign IDs sequentially to all new lab results in this batch
            foreach (var labResult in newLabResults)
            {
                if (labResult.Id == Guid.Empty)
                {
                    labResult.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(labResult.FriendlyId))
                {
                    labResult.FriendlyId = $"{prefix}{nextSequence:D5}";
                    nextSequence++;
                }
            }
        }

        private void GenerateLabResultFriendlyIdsSync()
        {
            var newLabResults = ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            if (!newLabResults.Any())
                return;

            var year = DateTime.UtcNow.Year;
            var prefix = $"LAB-{year}-";
            
            var lastLabResult = LabResults
                .Where(lr => lr.FriendlyId.StartsWith(prefix))
                .OrderByDescending(lr => lr.FriendlyId)
                .FirstOrDefault();

            int nextSequence = 1;
            if (lastLabResult != null)
            {
                var lastSequencePart = lastLabResult.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            // Assign IDs sequentially to all new lab results in this batch
            foreach (var labResult in newLabResults)
            {
                if (labResult.Id == Guid.Empty)
                {
                    labResult.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(labResult.FriendlyId))
                {
                    labResult.FriendlyId = $"{prefix}{nextSequence:D5}";
                    nextSequence++;
                }
            }
        }




        private async Task GenerateOrganizationFriendlyIds()
        {
            var newOrganizations = ChangeTracker.Entries<Organization>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var organization in newOrganizations)
            {
                if (organization.Id == Guid.Empty)
                {
                    organization.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(organization.FriendlyId))
                {
                    organization.FriendlyId = await GenerateNextOrganizationFriendlyId();
                }
            }
        }

        private void GenerateOrganizationFriendlyIdsSync()
        {
            var newOrganizations = ChangeTracker.Entries<Organization>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var organization in newOrganizations)
            {
                if (organization.Id == Guid.Empty)
                {
                    organization.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(organization.FriendlyId))
                {
                    organization.FriendlyId = GenerateNextOrganizationFriendlyIdSync();
                }
            }
        }

        private async Task<string> GenerateNextOrganizationFriendlyId()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"O-{year}-";
            
            var lastOrganization = await Organizations
                .Where(o => o.FriendlyId.StartsWith(prefix))
                .OrderByDescending(o => o.FriendlyId)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastOrganization != null)
            {
                var lastSequencePart = lastOrganization.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            return $"{prefix}{nextSequence:D4}";
        }

        private string GenerateNextOrganizationFriendlyIdSync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"O-{year}-";
            
            var lastOrganization = Organizations
                .Where(o => o.FriendlyId.StartsWith(prefix))
                .OrderByDescending(o => o.FriendlyId)
                .FirstOrDefault();

            int nextSequence = 1;
            if (lastOrganization != null)
            {
                var lastSequencePart = lastOrganization.FriendlyId.Substring(prefix.Length);
                if (int.TryParse(lastSequencePart, out int lastSequence))
                {
                    nextSequence = lastSequence + 1;
                }
            }

            return $"{prefix}{nextSequence:D4}";
        }

        private async Task UpdateDiseasePaths()
        {
            var changedDiseases = ChangeTracker.Entries<Disease>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var disease in changedDiseases)
            {
                if (disease.ParentDiseaseId.HasValue)
                {
                    var parent = await Diseases
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == disease.ParentDiseaseId.Value);
                    
                    if (parent != null)
                    {
                        disease.PathIds = $"{parent.PathIds}{disease.Id}/";
                        disease.Level = parent.Level + 1;
                    }
                }
                else
                {
                    disease.PathIds = $"/{disease.Id}/";
                    disease.Level = 0;
                }
            }
        }

        private void UpdateDiseasePathsSync()
        {
            var changedDiseases = ChangeTracker.Entries<Disease>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            foreach (var disease in changedDiseases)
            {
                if (disease.ParentDiseaseId.HasValue)
                {
                    var parent = Diseases
                        .AsNoTracking()
                        .FirstOrDefault(d => d.Id == disease.ParentDiseaseId.Value);
                    
                    if (parent != null)
                    {
                        disease.PathIds = $"{parent.PathIds}{disease.Id}/";
                        disease.Level = parent.Level + 1;
                    }
                }
                else
                {
                    disease.PathIds = $"/{disease.Id}/";
                    disease.Level = 0;
                }
            }
        }

        private void UpdateAuditableEntities()
        {
            var now = DateTime.UtcNow;
            var userId = GetCurrentUserId();

            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditable && 
                           (e.State == EntityState.Added || e.State == EntityState.Modified))
                .ToList();

            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                var entityType = entity.GetType();

                if (entry.State == EntityState.Added)
                {
                    // Set CreatedDate and CreatedByUserId
                    var createdDateProp = entityType.GetProperty("CreatedDate");
                    var createdByProp = entityType.GetProperty("CreatedByUserId");

                    if (createdDateProp != null && createdDateProp.CanWrite)
                    {
                        createdDateProp.SetValue(entity, now);
                    }

                    if (createdByProp != null && createdByProp.CanWrite && userId != null)
                    {
                        createdByProp.SetValue(entity, userId);
                    }
                }

                if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
                {
                    // Set/Update LastModified and LastModifiedByUserId
                    var lastModifiedProp = entityType.GetProperty("LastModified");
                    var lastModifiedByProp = entityType.GetProperty("LastModifiedByUserId");

                    if (lastModifiedProp != null && lastModifiedProp.CanWrite)
                    {
                        lastModifiedProp.SetValue(entity, now);
                    }

                    if (lastModifiedByProp != null && lastModifiedByProp.CanWrite && userId != null)
                    {
                        lastModifiedByProp.SetValue(entity, userId);
                    }
                }
            }
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                if (entry.Entity is not IAuditable auditable)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    EntityType = entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    ChangedByUserId = GetCurrentUserId(),
                    IpAddress = GetCurrentIpAddress(),
                    UserAgent = GetCurrentUserAgent()
                };

                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;

                    if (property.Metadata.IsPrimaryKey())
                    {
                        if (entry.State != EntityState.Added)
                        {
                            auditEntry.EntityId = property.CurrentValue?.ToString() ?? string.Empty;
                        }
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                var oldValue = property.OriginalValue;
                                var newValue = property.CurrentValue;
                                
                                // Only log if the values are actually different
                                if (!Equals(oldValue, newValue))
                                {
                                    auditEntry.ChangedColumns.Add(propertyName);
                                    auditEntry.OldValues[propertyName] = oldValue;
                                    auditEntry.NewValues[propertyName] = newValue;
                                }
                            }
                            break;
                    }
                }
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            var auditLogs = new List<AuditLog>();

            foreach (var auditEntry in auditEntries)
            {
                if (auditEntry.Action == "Added" && string.IsNullOrEmpty(auditEntry.EntityId))
                {
                    var idProperty = auditEntry.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    if (idProperty != null)
                    {
                        auditEntry.EntityId = idProperty.CurrentValue?.ToString() ?? string.Empty;
                    }
                }

                if (auditEntry.Action == "Added")
                {
                    foreach (var kvp in auditEntry.NewValues)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            EntityType = auditEntry.EntityType,
                            EntityId = auditEntry.EntityId,
                            Action = auditEntry.Action,
                            FieldName = kvp.Key,
                            OldValue = null,
                            NewValue = kvp.Value?.ToString(),
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = auditEntry.ChangedByUserId,
                            IpAddress = auditEntry.IpAddress,
                            UserAgent = auditEntry.UserAgent
                        });
                    }
                }
                else if (auditEntry.Action == "Deleted")
                {
                    foreach (var kvp in auditEntry.OldValues)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            EntityType = auditEntry.EntityType,
                            EntityId = auditEntry.EntityId,
                            Action = auditEntry.Action,
                            FieldName = kvp.Key,
                            OldValue = kvp.Value?.ToString(),
                            NewValue = null,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = auditEntry.ChangedByUserId,
                            IpAddress = auditEntry.IpAddress,
                            UserAgent = auditEntry.UserAgent
                        });
                    }
                }
                else if (auditEntry.Action == "Modified")
                {
                    // Only create audit logs if there were actual changes
                    if (auditEntry.ChangedColumns.Count > 0)
                    {
                        foreach (var columnName in auditEntry.ChangedColumns)
                        {
                            auditLogs.Add(new AuditLog
                            {
                                EntityType = auditEntry.EntityType,
                                EntityId = auditEntry.EntityId,
                                Action = auditEntry.Action,
                                FieldName = columnName,
                                OldValue = auditEntry.OldValues.ContainsKey(columnName)
                                    ? auditEntry.OldValues[columnName]?.ToString()
                                    : null,
                                NewValue = auditEntry.NewValues.ContainsKey(columnName)
                                    ? auditEntry.NewValues[columnName]?.ToString()
                                    : null,
                                ChangedAt = DateTime.UtcNow,
                                ChangedByUserId = auditEntry.ChangedByUserId,
                                IpAddress = auditEntry.IpAddress,
                                UserAgent = auditEntry.UserAgent
                            });
                        }
                    }
                }
            }

            if (auditLogs.Count > 0)
            {
                await AuditLogs.AddRangeAsync(auditLogs);
                await base.SaveChangesAsync();
            }
        }

        private void OnAfterSaveChangesSync(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            var auditLogs = new List<AuditLog>();

            foreach (var auditEntry in auditEntries)
            {
                if (auditEntry.Action == "Added" && string.IsNullOrEmpty(auditEntry.EntityId))
                {
                    var idProperty = auditEntry.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    if (idProperty != null)
                    {
                        auditEntry.EntityId = idProperty.CurrentValue?.ToString() ?? string.Empty;
                    }
                }

                if (auditEntry.Action == "Added")
                {
                    foreach (var kvp in auditEntry.NewValues)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            EntityType = auditEntry.EntityType,
                            EntityId = auditEntry.EntityId,
                            Action = auditEntry.Action,
                            FieldName = kvp.Key,
                            OldValue = null,
                            NewValue = kvp.Value?.ToString(),
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = auditEntry.ChangedByUserId,
                            IpAddress = auditEntry.IpAddress,
                            UserAgent = auditEntry.UserAgent
                        });
                    }
                }
                else if (auditEntry.Action == "Deleted")
                {
                    foreach (var kvp in auditEntry.OldValues)
                    {
                        auditLogs.Add(new AuditLog
                        {
                            EntityType = auditEntry.EntityType,
                            EntityId = auditEntry.EntityId,
                            Action = auditEntry.Action,
                            FieldName = kvp.Key,
                            OldValue = kvp.Value?.ToString(),
                            NewValue = null,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = auditEntry.ChangedByUserId,
                            IpAddress = auditEntry.IpAddress,
                            UserAgent = auditEntry.UserAgent
                        });
                    }
                }
                else if (auditEntry.Action == "Modified")
                {
                    // Only create audit logs if there were actual changes
                    if (auditEntry.ChangedColumns.Count > 0)
                    {
                        foreach (var columnName in auditEntry.ChangedColumns)
                        {
                            auditLogs.Add(new AuditLog
                            {
                                EntityType = auditEntry.EntityType,
                                EntityId = auditEntry.EntityId,
                                Action = auditEntry.Action,
                                FieldName = columnName,
                                OldValue = auditEntry.OldValues.ContainsKey(columnName)
                                    ? auditEntry.OldValues[columnName]?.ToString()
                                    : null,
                                NewValue = auditEntry.NewValues.ContainsKey(columnName)
                                    ? auditEntry.NewValues[columnName]?.ToString()
                                    : null,
                                ChangedAt = DateTime.UtcNow,
                                ChangedByUserId = auditEntry.ChangedByUserId,
                                IpAddress = auditEntry.IpAddress,
                                UserAgent = auditEntry.UserAgent
                            });
                        }
                    }
                }
            }

            if (auditLogs.Count > 0)
            {
                AuditLogs.AddRange(auditLogs);
                base.SaveChanges();
            }
        }

        private string? GetCurrentUserId()
        {
            return _httpContextAccessor?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        }

        private string? GetCurrentIpAddress()
        {
            return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetCurrentUserAgent()
        {
            return _httpContextAccessor?.HttpContext?.Request?.Headers["User-Agent"].ToString();
        }

        // Soft Delete Methods
        public async Task<bool> SoftDeleteAsync<T>(T entity) where T : class, ISoftDeletable
        {
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedByUserId = GetCurrentUserId();

            await SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync<T>(T entity) where T : class, ISoftDeletable
        {
            if (entity == null) return false;

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            entity.DeletedByUserId = null;

            await SaveChangesAsync();
            return true;
        }

        public IQueryable<T> IncludeDeleted<T>() where T : class, ISoftDeletable
        {
            return Set<T>().IgnoreQueryFilters();
        }

        public IQueryable<T> OnlyDeleted<T>() where T : class, ISoftDeletable
        {
            return Set<T>().IgnoreQueryFilters().Where(e => e.IsDeleted);
        }

        // Review Queue Detection Methods
        private async Task<List<ReviewQueueCandidate>> DetectReviewQueueItemsAsync()
        {
            var candidates = new List<ReviewQueueCandidate>();
            var currentUserId = GetCurrentUserId();

            // Debug: Log ALL tracked entities
            var trackedEntities = ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
                .ToList();
            
            System.Diagnostics.Debug.WriteLine($"[REVIEW] Tracked entities: {trackedEntities.Count}");
            foreach (var e in trackedEntities)
            {
                System.Diagnostics.Debug.WriteLine($"[REVIEW]   - {e.Entity.GetType().Name} ({e.State})");
            }

            foreach (var entry in ChangeTracker.Entries())
            {
                // New Lab Results
                if (entry.State == EntityState.Added && entry.Entity is LabResult labResult)
                {
                    System.Diagnostics.Debug.WriteLine($"[REVIEW] Detected new LabResult for CaseId: {labResult.CaseId}");
                    
                    var caseEntity = await Cases
                        .Where(c => c.Id == labResult.CaseId)
                        .Include(c => c.Disease)
                        .FirstOrDefaultAsync();

                    if (caseEntity?.DiseaseId != null)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(caseEntity.DiseaseId.Value);
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] Disease: {caseEntity.Disease?.Name}, AutoQueueLabResults: {settings.AutoQueueLabResults}");
                        
                        if (settings.AutoQueueLabResults)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] Adding LabResult to review queue");
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "LabResult",
                                EntityId = labResult.Id.GetHashCode(), // Temporary ID
                                EntityGuid = labResult.Id,
                                CaseId = caseEntity.Id,
                                PatientId = caseEntity.PatientId,
                                DiseaseId = caseEntity.DiseaseId,
                                ChangeType = "New",
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] Case not found or has no disease for LabResult");
                    }
                }

                // New Exposures
                if (entry.State == EntityState.Added && entry.Entity is ExposureEvent exposure)
                {
                    var caseEntity = await Cases
                        .Where(c => c.Id == exposure.ExposedCaseId)
                        .Include(c => c.Disease)
                        .FirstOrDefaultAsync();

                    if (caseEntity?.DiseaseId != null)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(caseEntity.DiseaseId.Value);
                        if (settings.AutoQueueExposures)
                        {
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "Exposure",
                                EntityId = exposure.Id.GetHashCode(),
                                EntityGuid = exposure.Id,
                                CaseId = caseEntity.Id,
                                PatientId = caseEntity.PatientId,
                                DiseaseId = caseEntity.DiseaseId,
                                ChangeType = "New",
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                    }
                }

                // New Contacts (Cases with Type = Contact)
                if (entry.State == EntityState.Added && entry.Entity is Case contactCase && contactCase.Type == CaseType.Contact)
                {
                    if (contactCase.DiseaseId != null)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(contactCase.DiseaseId.Value);
                        if (settings.AutoQueueContacts)
                        {
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "Contact",
                                EntityId = contactCase.Id.GetHashCode(),
                                EntityGuid = contactCase.Id,
                                CaseId = contactCase.Id,
                                PatientId = contactCase.PatientId,
                                DiseaseId = contactCase.DiseaseId,
                                ChangeType = "New",
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                    }
                }

                // New Cases (regular cases, not contacts)
                if (entry.State == EntityState.Added && entry.Entity is Case newCase && newCase.Type != CaseType.Contact)
                {
                    if (newCase.DiseaseId != null)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(newCase.DiseaseId.Value);
                        if (settings.AutoQueueNewCases)
                        {
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "NewCase",
                                EntityId = newCase.Id.GetHashCode(),
                                EntityGuid = newCase.Id,
                                CaseId = newCase.Id,
                                PatientId = newCase.PatientId,
                                DiseaseId = newCase.DiseaseId,
                                ChangeType = "New",
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                    }
                }

                // Case changes (Confirmation Status or Disease)
                if (entry.State == EntityState.Modified && entry.Entity is Case modifiedCase)
                {
                    System.Diagnostics.Debug.WriteLine($"[REVIEW] ==== Case Modified - ENTERING DETECTION ====");
                    System.Diagnostics.Debug.WriteLine($"[REVIEW] CaseId: {modifiedCase.FriendlyId}");
                    
                    var confirmationStatusProp = entry.Property(nameof(Case.ConfirmationStatusId));
                    var diseaseProp = entry.Property(nameof(Case.DiseaseId));

                    System.Diagnostics.Debug.WriteLine($"[REVIEW] ConfirmationStatusId.IsModified: {confirmationStatusProp.IsModified}");
                    System.Diagnostics.Debug.WriteLine($"[REVIEW] DiseaseId.IsModified: {diseaseProp.IsModified}");
                    
                    if (confirmationStatusProp.IsModified)
                    {
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] ConfirmationStatusId OLD: {confirmationStatusProp.OriginalValue} ? NEW: {confirmationStatusProp.CurrentValue}");
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] Values are equal: {Equals(confirmationStatusProp.OriginalValue, confirmationStatusProp.CurrentValue)}");
                    }
                    
                    if (diseaseProp.IsModified)
                    {
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] DiseaseId OLD: {diseaseProp.OriginalValue} ? NEW: {diseaseProp.CurrentValue}");
                    }

                    if (modifiedCase.DiseaseId != null)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(modifiedCase.DiseaseId.Value);
                        
                        System.Diagnostics.Debug.WriteLine($"[REVIEW] Disease settings - AutoQueueConfirmationChanges: {settings.AutoQueueConfirmationChanges}");

                        // Confirmation Status Changed - ONLY if value actually changed
                        if (confirmationStatusProp.IsModified && 
                            !Equals(confirmationStatusProp.OriginalValue, confirmationStatusProp.CurrentValue) &&
                            settings.AutoQueueConfirmationChanges)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] ? WILL QUEUE - All conditions met for confirmation status change");
                            
                            // Lookup status names for display
                            string? oldStatusName = null;
                            string? newStatusName = null;
                            
                            if (confirmationStatusProp.OriginalValue != null)
                            {
                                var oldStatus = await CaseStatuses
                                    .FirstOrDefaultAsync(cs => cs.Id == (int)confirmationStatusProp.OriginalValue);
                                oldStatusName = oldStatus?.Name;
                            }
                            
                            if (confirmationStatusProp.CurrentValue != null)
                            {
                                var newStatus = await CaseStatuses
                                    .FirstOrDefaultAsync(cs => cs.Id == (int)confirmationStatusProp.CurrentValue);
                                newStatusName = newStatus?.Name;
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] Status display names - Old: '{oldStatusName}' ? New: '{newStatusName}'");

                            var changeSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                field = "ConfirmationStatusId",
                                oldValue = confirmationStatusProp.OriginalValue,
                                newValue = confirmationStatusProp.CurrentValue,
                                oldValueDisplay = oldStatusName,
                                newValueDisplay = newStatusName,
                                changedAt = DateTime.UtcNow
                            });

                            System.Diagnostics.Debug.WriteLine($"[REVIEW] Adding ConfirmationStatus change to review queue");
                            
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "CaseChange",
                                EntityId = modifiedCase.Id.GetHashCode(),
                                EntityGuid = modifiedCase.Id,
                                CaseId = modifiedCase.Id,
                                PatientId = modifiedCase.PatientId,
                                DiseaseId = modifiedCase.DiseaseId,
                                ChangeType = "FieldChanged",
                                TriggerField = "ConfirmationStatusId",
                                ChangeSnapshot = changeSnapshot,
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                        else if (confirmationStatusProp.IsModified)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] ? NOT QUEUING - Reason:");
                            if (Equals(confirmationStatusProp.OriginalValue, confirmationStatusProp.CurrentValue))
                            {
                                System.Diagnostics.Debug.WriteLine($"[REVIEW]   - Values are identical (no actual change)");
                            }
                            if (!settings.AutoQueueConfirmationChanges)
                            {
                                System.Diagnostics.Debug.WriteLine($"[REVIEW]   - AutoQueueConfirmationChanges is OFF");
                            }
                        }

                        // Disease Changed - ONLY if value actually changed
                        if (diseaseProp.IsModified && 
                            !Equals(diseaseProp.OriginalValue, diseaseProp.CurrentValue) &&
                            settings.AutoQueueDiseaseChanges)
                        {
                            // Lookup disease names for display
                            string? oldDiseaseName = null;
                            string? newDiseaseName = null;
                            
                            if (diseaseProp.OriginalValue != null)
                            {
                                var oldDisease = await Diseases
                                    .FirstOrDefaultAsync(d => d.Id == (Guid)diseaseProp.OriginalValue);
                                oldDiseaseName = oldDisease?.Name;
                            }
                            
                            if (diseaseProp.CurrentValue != null)
                            {
                                var newDisease = await Diseases
                                    .FirstOrDefaultAsync(d => d.Id == (Guid)diseaseProp.CurrentValue);
                                newDiseaseName = newDisease?.Name;
                            }

                            var changeSnapshot = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                field = "DiseaseId",
                                oldValue = diseaseProp.OriginalValue,
                                newValue = diseaseProp.CurrentValue,
                                oldValueDisplay = oldDiseaseName,
                                newValueDisplay = newDiseaseName,
                                changedAt = DateTime.UtcNow
                            });

                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "CaseChange",
                                EntityId = modifiedCase.Id.GetHashCode(),
                                EntityGuid = modifiedCase.Id,
                                CaseId = modifiedCase.Id,
                                PatientId = modifiedCase.PatientId,
                                DiseaseId = modifiedCase.DiseaseId,
                                ChangeType = "FieldChanged",
                                TriggerField = "DiseaseId",
                                ChangeSnapshot = changeSnapshot,
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }

                        // Clinical Notification Date Added/Changed - ONLY when changing FROM null TO a value
                        var clinicalNotificationProp = entry.Property(nameof(Case.ClinicalNotificationDate));
                        if (clinicalNotificationProp.IsModified)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] ClinicalNotificationDate modified. Original: {clinicalNotificationProp.OriginalValue}, Current: {clinicalNotificationProp.CurrentValue}");
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] AutoQueueClinicalNotifications: {settings.AutoQueueClinicalNotifications}");
                        }
                        
                        if (clinicalNotificationProp.IsModified && 
                            clinicalNotificationProp.OriginalValue == null &&  // Was null before
                            clinicalNotificationProp.CurrentValue != null &&   // Now has a value
                            settings.AutoQueueClinicalNotifications)
                        {
                            System.Diagnostics.Debug.WriteLine($"[REVIEW] Adding ClinicalNotification to review queue");
                            candidates.Add(new ReviewQueueCandidate
                            {
                                EntityType = "ClinicalNotification",
                                EntityId = modifiedCase.Id.GetHashCode(),
                                EntityGuid = modifiedCase.Id,
                                CaseId = modifiedCase.Id,
                                PatientId = modifiedCase.PatientId,
                                DiseaseId = modifiedCase.DiseaseId,
                                ChangeType = clinicalNotificationProp.OriginalValue == null ? "New" : "Updated",
                                Priority = settings.DefaultPriority,
                                CreatedByUserId = currentUserId
                            });
                        }
                    }
                }
            }

            return candidates;
        }

        private async Task QueueReviewItemsAsync(List<ReviewQueueCandidate> candidates)
        {
            if (!candidates.Any()) return;

            foreach (var candidate in candidates)
            {
                try
                {
                    // Generate group key - use case-based grouping if we have a CaseId
                    var groupKey = GenerateReviewGroupKey(
                        candidate.EntityType, 
                        candidate.TriggerField, 
                        candidate.ChangeSnapshot,
                        candidate.DiseaseId,
                        candidate.CaseId  // Pass CaseId for case-based grouping
                    );

                    // Check for existing group within time window
                    var groupingHours = 6; // Default
                    if (candidate.DiseaseId.HasValue)
                    {
                        var settings = await GetDiseaseReviewSettingsAsync(candidate.DiseaseId.Value);
                        groupingHours = settings.GroupingWindowHours;
                    }

                    var cutoffTime = DateTime.UtcNow.AddHours(-groupingHours);
                    var existingGroup = await ReviewQueue
                        .Where(rq => rq.GroupKey == groupKey 
                                  && rq.CreatedDate >= cutoffTime
                                  && rq.ReviewStatus == "Pending")
                        .OrderByDescending(rq => rq.CreatedDate)
                        .FirstOrDefaultAsync();

                    if (existingGroup != null)
                    {
                        // Add to existing group
                        existingGroup.GroupCount++;
                        // Don't need to call SaveChanges - already in transaction
                    }
                    else
                    {
                        // Create new review queue entry
                        var reviewEntry = new Models.ReviewQueue
                        {
                            EntityType = candidate.EntityType,
                            EntityId = candidate.EntityId,
                            CaseId = candidate.CaseId,
                            PatientId = candidate.PatientId,
                            DiseaseId = candidate.DiseaseId,
                            ChangeType = candidate.ChangeType,
                            TriggerField = candidate.TriggerField,
                            ChangeSnapshot = candidate.ChangeSnapshot,
                            Priority = candidate.Priority,
                            ReviewStatus = "Pending",
                            GroupKey = groupKey,
                            GroupCount = 1,
                            CreatedByUserId = candidate.CreatedByUserId,
                            CreatedDate = DateTime.UtcNow
                        };

                        ReviewQueue.Add(reviewEntry);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the entire save operation
                    System.Diagnostics.Debug.WriteLine($"Error queueing review item: {ex.Message}");
                }
            }

            // Save all review queue entries
            if (ReviewQueue.Local.Any(e => e.Id == 0))
            {
                await base.SaveChangesAsync();
            }
        }

        private async Task<DiseaseReviewSettings> GetDiseaseReviewSettingsAsync(Guid diseaseId)
        {
            var disease = await Diseases
                .Where(d => d.Id == diseaseId)
                .Select(d => new
                {
                    d.ParentDiseaseId,
                    d.ReviewGroupingWindowHours,
                    d.ReviewAutoQueueLabResults,
                    d.ReviewAutoQueueExposures,
                    d.ReviewAutoQueueContacts,
                    d.ReviewAutoQueueConfirmationChanges,
                    d.ReviewAutoQueueDiseaseChanges,
                    d.ReviewAutoQueueClinicalNotifications,
                    d.ReviewAutoQueueNewCases,
                    d.ReviewDefaultPriority
                })
                .FirstOrDefaultAsync();

            if (disease == null)
            {
                return new DiseaseReviewSettings();
            }

            // Simple settings without hierarchy for now (can enhance later)
            return new DiseaseReviewSettings
            {
                GroupingWindowHours = disease.ReviewGroupingWindowHours,
                AutoQueueLabResults = disease.ReviewAutoQueueLabResults,
                AutoQueueExposures = disease.ReviewAutoQueueExposures,
                AutoQueueContacts = disease.ReviewAutoQueueContacts,
                AutoQueueConfirmationChanges = disease.ReviewAutoQueueConfirmationChanges,
                AutoQueueDiseaseChanges = disease.ReviewAutoQueueDiseaseChanges,
                AutoQueueClinicalNotifications = disease.ReviewAutoQueueClinicalNotifications,
                AutoQueueNewCases = disease.ReviewAutoQueueNewCases,
                DefaultPriority = disease.ReviewDefaultPriority
            };
        }

        private string GenerateReviewGroupKey(string entityType, string? triggerField, string? changeSnapshot, Guid? diseaseId, Guid? caseId = null)
        {
            // CRITICAL FIX: Group by SPECIFIC field changes, not just case-level activity
            // This ensures that changing Disease then ConfirmationStatus creates TWO review items
            // instead of grouping them together
            
            var components = new List<string> { entityType };

            // Always include the specific field that triggered the change
            // This prevents different field changes from being grouped together
            if (!string.IsNullOrEmpty(triggerField))
            {
                components.Add(triggerField);
            }

            // Include case ID to group multiple changes to the SAME field on the SAME case
            // within the time window (e.g., changing status from Probable?Confirmed?Suspect)
            if (caseId.HasValue)
            {
                components.Add(caseId.Value.ToString());
            }

            // Include the new value to group identical changes
            // (e.g., multiple people changing the same case to the same status)
            if (!string.IsNullOrEmpty(changeSnapshot))
            {
                try
                {
                    var obj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(changeSnapshot);
                    if (obj?.ContainsKey("newValue") == true)
                    {
                        components.Add(obj["newValue"]?.ToString() ?? "");
                    }
                }
                catch { }
            }

            if (diseaseId.HasValue)
            {
                components.Add(diseaseId.Value.ToString());
            }

            // Round to nearest hour for time-based grouping
            var hourBucket = DateTime.UtcNow.ToString("yyyyMMddHH");
            components.Add(hourBucket);

            return string.Join("|", components);
        }
    }

    internal class ReviewQueueCandidate
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public Guid EntityGuid { get; set; }
        public Guid? CaseId { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? DiseaseId { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string? TriggerField { get; set; }
        public string? ChangeSnapshot { get; set; }
        public int Priority { get; set; }
        public string? CreatedByUserId { get; set; }
    }

    internal class DiseaseReviewSettings
    {
        public int GroupingWindowHours { get; set; } = 6;
        public bool AutoQueueLabResults { get; set; } = true;
        public bool AutoQueueExposures { get; set; } = false;
        public bool AutoQueueContacts { get; set; } = false;
        public bool AutoQueueConfirmationChanges { get; set; } = true;
        public bool AutoQueueDiseaseChanges { get; set; } = true;
        public bool AutoQueueClinicalNotifications { get; set; } = false;
        public bool AutoQueueNewCases { get; set; } = false;
        public int DefaultPriority { get; set; } = 1;
    }

    internal class AuditEntry
    {
        public AuditEntry(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            Entry = entry;
        }

        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; }
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object?> OldValues { get; } = new Dictionary<string, object?>();
        public Dictionary<string, object?> NewValues { get; } = new Dictionary<string, object?>();
        public List<string> ChangedColumns { get; } = new List<string>();
        public string? ChangedByUserId { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
