using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Data
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
        public DbSet<Language> Languages { get; set; }
        public DbSet<Ethnicity> Ethnicities { get; set; }
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

        // Task Management System
        public DbSet<TaskTemplate> TaskTemplates { get; set; }
        public DbSet<TaskType> TaskTypes { get; set; }
        public DbSet<CaseTask> CaseTasks { get; set; }
        public DbSet<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; }
        public DbSet<TaskCallAttempt> TaskCallAttempts { get; set; }

        // Survey System
        public DbSet<SurveyTemplate> SurveyTemplates { get; set; }
        public DbSet<SurveyTemplateDisease> SurveyTemplateDiseases { get; set; }

        // Outbreak Management System
        public DbSet<Outbreak> Outbreaks { get; set; }
        public DbSet<OutbreakTeamMember> OutbreakTeamMembers { get; set; }
        public DbSet<OutbreakCaseDefinition> OutbreakCaseDefinitions { get; set; }
        public DbSet<OutbreakCase> OutbreakCases { get; set; }
        public DbSet<OutbreakTimeline> OutbreakTimelines { get; set; }
        public DbSet<OutbreakSearchQuery> OutbreakSearchQueries { get; set; }
        public DbSet<OutbreakLineListConfiguration> OutbreakLineListConfigurations { get; set; }

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
                .HasOne(ee => ee.Case)
                .WithMany(c => c.ExposureEvents)
                .HasForeignKey(ee => ee.CaseId)
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
                .HasOne(ee => ee.RelatedCase)
                .WithMany()
                .HasForeignKey(ee => ee.RelatedCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.CaseId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.EventId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.LocationId);

            builder.Entity<ExposureEvent>()
                .HasIndex(ee => ee.RelatedCaseId);

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

            // Global Query Filters for Soft Delete
            builder.Entity<Patient>().HasQueryFilter(p => !p.IsDeleted);
            builder.Entity<Case>().HasQueryFilter(c => !c.IsDeleted);
            builder.Entity<LabResult>().HasQueryFilter(lr => !lr.IsDeleted);
            builder.Entity<Note>().HasQueryFilter(n => !n.IsDeleted);
            builder.Entity<Symptom>().HasQueryFilter(s => !s.IsDeleted);
            builder.Entity<CaseSymptom>().HasQueryFilter(cs => !cs.IsDeleted);
            builder.Entity<DiseaseSymptom>().HasQueryFilter(ds => !ds.IsDeleted);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await GeneratePatientFriendlyIds();
            await GenerateLabResultFriendlyIds();
            await GenerateOrganizationFriendlyIds();
            await UpdateDiseasePaths();
            UpdateAuditableEntities();
            var auditEntries = OnBeforeSaveChanges();
            var result = await base.SaveChangesAsync(cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        public override int SaveChanges()
        {
            GeneratePatientFriendlyIdsSync();
            GenerateLabResultFriendlyIdsSync();
            GenerateOrganizationFriendlyIdsSync();
            UpdateDiseasePathsSync();
            UpdateAuditableEntities();
            var auditEntries = OnBeforeSaveChanges();
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

            foreach (var patient in newPatients)
            {
                if (patient.Id == Guid.Empty)
                {
                    patient.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(patient.FriendlyId))
                {
                    patient.FriendlyId = await GenerateNextFriendlyId();
                }
            }
        }

        private void GeneratePatientFriendlyIdsSync()
        {
            var newPatients = ChangeTracker.Entries<Patient>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var patient in newPatients)
            {
                if (patient.Id == Guid.Empty)
                {
                    patient.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(patient.FriendlyId))
                {
                    patient.FriendlyId = GenerateNextFriendlyIdSync();
                }
            }
        }

        private async Task<string> GenerateNextFriendlyId()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"P-{year}-";
            
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

            return $"{prefix}{nextSequence:D5}";
        }

        private string GenerateNextFriendlyIdSync()
        {
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

            return $"{prefix}{nextSequence:D5}";
        }

        private async Task GenerateLabResultFriendlyIds()
        {
            var newLabResults = ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var labResult in newLabResults)
            {
                if (labResult.Id == Guid.Empty)
                {
                    labResult.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(labResult.FriendlyId))
                {
                    labResult.FriendlyId = await GenerateNextLabResultFriendlyId();
                }
            }
        }

        private void GenerateLabResultFriendlyIdsSync()
        {
            var newLabResults = ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            foreach (var labResult in newLabResults)
            {
                if (labResult.Id == Guid.Empty)
                {
                    labResult.Id = Guid.NewGuid();
                }

                if (string.IsNullOrEmpty(labResult.FriendlyId))
                {
                    labResult.FriendlyId = GenerateNextLabResultFriendlyIdSync();
                }
            }
        }

        private async Task<string> GenerateNextLabResultFriendlyId()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"L-{year}-";
            
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

            return $"{prefix}{nextSequence:D4}";
        }

        private string GenerateNextLabResultFriendlyIdSync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"L-{year}-";
            
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

            return $"{prefix}{nextSequence:D4}";
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
