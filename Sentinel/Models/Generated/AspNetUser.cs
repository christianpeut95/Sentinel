using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class AspNetUser
{
    public string Id { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PrimaryLanguage { get; set; }

    public string? LanguagesSpokenJson { get; set; }

    public bool IsInterviewWorker { get; set; }

    public bool AvailableForAutoAssignment { get; set; }

    public int CurrentTaskCapacity { get; set; }

    public string? UserName { get; set; }

    public string? NormalizedUserName { get; set; }

    public string? Email { get; set; }

    public string? NormalizedEmail { get; set; }

    public bool EmailConfirmed { get; set; }

    public string? PasswordHash { get; set; }

    public string? SecurityStamp { get; set; }

    public string? ConcurrencyStamp { get; set; }

    public string? PhoneNumber { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool LockoutEnabled { get; set; }

    public int AccessFailedCount { get; set; }

    public string? DashboardConfigJson { get; set; }

    public virtual ICollection<AspNetUserClaim> AspNetUserClaims { get; set; } = new List<AspNetUserClaim>();

    public virtual ICollection<AspNetUserLogin> AspNetUserLogins { get; set; } = new List<AspNetUserLogin>();

    public virtual ICollection<AspNetUserToken> AspNetUserTokens { get; set; } = new List<AspNetUserToken>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<CaseTask> CaseTaskAssignedToUsers { get; set; } = new List<CaseTask>();

    public virtual ICollection<CaseTask> CaseTaskCompletedByUsers { get; set; } = new List<CaseTask>();

    public virtual ICollection<Hl7message> Hl7messageManualReviewByUsers { get; set; } = new List<Hl7message>();

    public virtual ICollection<Hl7message> Hl7messageProcessedByUsers { get; set; } = new List<Hl7message>();

    public virtual ICollection<Hl7parsingIssue> Hl7parsingIssues { get; set; } = new List<Hl7parsingIssue>();

    public virtual ICollection<Hl7testMessageHistory> Hl7testMessageHistories { get; set; } = new List<Hl7testMessageHistory>();

    public virtual ICollection<Hl7testMessageTemplate> Hl7testMessageTemplateCreatedByUsers { get; set; } = new List<Hl7testMessageTemplate>();

    public virtual ICollection<Hl7testMessageTemplate> Hl7testMessageTemplateUpdatedByUsers { get; set; } = new List<Hl7testMessageTemplate>();

    public virtual ICollection<LabResultMarkerHistory> LabResultMarkerHistories { get; set; } = new List<LabResultMarkerHistory>();

    public virtual ICollection<OutbreakLineListConfiguration> OutbreakLineListConfigurationCreatedByUsers { get; set; } = new List<OutbreakLineListConfiguration>();

    public virtual ICollection<OutbreakLineListConfiguration> OutbreakLineListConfigurationUsers { get; set; } = new List<OutbreakLineListConfiguration>();

    public virtual ICollection<OutbreakTeamMember> OutbreakTeamMembers { get; set; } = new List<OutbreakTeamMember>();

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual ICollection<ReportFolderShare> ReportFolderShares { get; set; } = new List<ReportFolderShare>();

    public virtual ICollection<ReviewQueue> ReviewQueueCreatedByUsers { get; set; } = new List<ReviewQueue>();

    public virtual ICollection<ReviewQueue> ReviewQueueReviewedByUsers { get; set; } = new List<ReviewQueue>();

    public virtual ICollection<RoleDiseaseAccess> RoleDiseaseAccesses { get; set; } = new List<RoleDiseaseAccess>();

    public virtual ICollection<SurveyFieldMapping> SurveyFieldMappingCreatedByUsers { get; set; } = new List<SurveyFieldMapping>();

    public virtual ICollection<SurveyFieldMapping> SurveyFieldMappingLastModifiedByUsers { get; set; } = new List<SurveyFieldMapping>();

    public virtual ICollection<TaskCallAttempt> TaskCallAttempts { get; set; } = new List<TaskCallAttempt>();

    public virtual ICollection<UserDiseaseAccess> UserDiseaseAccessGrantedByUsers { get; set; } = new List<UserDiseaseAccess>();

    public virtual ICollection<UserDiseaseAccess> UserDiseaseAccessUsers { get; set; } = new List<UserDiseaseAccess>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<AspNetRole> Roles { get; set; } = new List<AspNetRole>();
}
