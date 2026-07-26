using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Case
{
    public Guid Id { get; set; }

    public string FriendlyId { get; set; } = null!;

    public Guid PatientId { get; set; }

    public DateTime? DateOfOnset { get; set; }

    public DateTime? DateOfNotification { get; set; }

    public DateTime? ClinicalNotificationDate { get; set; }

    public string? ClinicalNotifierOrganisation { get; set; }

    public string? ClinicalNotificationNotes { get; set; }

    public int? ConfirmationStatusId { get; set; }

    public Guid? DiseaseId { get; set; }

    public int? Hospitalised { get; set; }

    public Guid? HospitalId { get; set; }

    public DateTime? DateOfAdmission { get; set; }

    public DateTime? DateOfDischarge { get; set; }

    public int? DiedDueToDisease { get; set; }

    public int? Jurisdiction1Id { get; set; }

    public int? Jurisdiction2Id { get; set; }

    public int? Jurisdiction3Id { get; set; }

    public int? Jurisdiction4Id { get; set; }

    public int? Jurisdiction5Id { get; set; }

    public int Type { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public DateTime? CaseAddressCapturedAt { get; set; }

    public string? CaseAddressLine { get; set; }

    public bool CaseAddressManualOverride { get; set; }

    public string? CaseCity { get; set; }

    public double? CaseLatitude { get; set; }

    public double? CaseLongitude { get; set; }

    public string? CasePostalCode { get; set; }

    public int? CaseStateId { get; set; }

    public string? ConfirmationStatusClassifiedBy { get; set; }

    public DateTime? ConfirmationStatusClassifiedDate { get; set; }

    public bool IsAutoClassified { get; set; }

    public DateTime? LastEvaluatedDate { get; set; }

    public string? LastEvaluatedDefinitionIds { get; set; }

    public bool ConfirmationStatusManualOverride { get; set; }

    public string? ConfirmationStatusManualOverrideByUserId { get; set; }

    public DateTime? ConfirmationStatusManualOverrideDate { get; set; }

    public virtual ICollection<CaseClassificationHistory> CaseClassificationHistories { get; set; } = new List<CaseClassificationHistory>();

    public virtual ICollection<CaseCustomFieldBoolean> CaseCustomFieldBooleans { get; set; } = new List<CaseCustomFieldBoolean>();

    public virtual ICollection<CaseCustomFieldDate> CaseCustomFieldDates { get; set; } = new List<CaseCustomFieldDate>();

    public virtual ICollection<CaseCustomFieldLookup> CaseCustomFieldLookups { get; set; } = new List<CaseCustomFieldLookup>();

    public virtual ICollection<CaseCustomFieldNumber> CaseCustomFieldNumbers { get; set; } = new List<CaseCustomFieldNumber>();

    public virtual ICollection<CaseCustomFieldString> CaseCustomFieldStrings { get; set; } = new List<CaseCustomFieldString>();

    public virtual State? CaseState { get; set; }

    public virtual ICollection<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();

    public virtual ICollection<CaseTask> CaseTaskCaseId1Navigations { get; set; } = new List<CaseTask>();

    public virtual ICollection<CaseTask> CaseTaskCases { get; set; } = new List<CaseTask>();

    public virtual CaseStatus? ConfirmationStatus { get; set; }

    public virtual Disease? Disease { get; set; }

    public virtual ICollection<ExposureEvent> ExposureEventExposedCases { get; set; } = new List<ExposureEvent>();

    public virtual ICollection<ExposureEvent> ExposureEventSourceCases { get; set; } = new List<ExposureEvent>();

    public virtual ICollection<Hl7message> Hl7messages { get; set; } = new List<Hl7message>();

    public virtual Organization? Hospital { get; set; }

    public virtual Jurisdiction? Jurisdiction1 { get; set; }

    public virtual Jurisdiction? Jurisdiction2 { get; set; }

    public virtual Jurisdiction? Jurisdiction3 { get; set; }

    public virtual Jurisdiction? Jurisdiction4 { get; set; }

    public virtual Jurisdiction? Jurisdiction5 { get; set; }

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual ICollection<OutbreakCase> OutbreakCases { get; set; } = new List<OutbreakCase>();

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<ReviewQueue> ReviewQueues { get; set; } = new List<ReviewQueue>();

    public virtual ICollection<SurveySubmissionLog> SurveySubmissionLogs { get; set; } = new List<SurveySubmissionLog>();
}
