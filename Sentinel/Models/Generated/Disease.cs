using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Disease
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Code { get; set; } = null!;

    public string ExportCode { get; set; } = null!;

    public string? Description { get; set; }

    public Guid? DiseaseCategoryId { get; set; }

    public Guid? ParentDiseaseId { get; set; }

    public string PathIds { get; set; } = null!;

    public int Level { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public int AccessLevel { get; set; }

    public int ExposureTrackingMode { get; set; }

    public bool DefaultToResidentialAddress { get; set; }

    public bool AlwaysPromptForLocation { get; set; }

    public bool SyncWithPatientAddressUpdates { get; set; }

    public string? ExposureGuidanceText { get; set; }

    public bool RequireGeographicCoordinates { get; set; }

    public bool AllowDomesticAcquisition { get; set; }

    public int? ExposureDataGracePeriodDays { get; set; }

    public string? RequiredLocationTypeIds { get; set; }

    public int ReviewGroupingWindowHours { get; set; }

    public bool ReviewAutoQueueLabResults { get; set; }

    public bool ReviewAutoQueueExposures { get; set; }

    public bool ReviewAutoQueueContacts { get; set; }

    public bool ReviewAutoQueueConfirmationChanges { get; set; }

    public bool ReviewAutoQueueDiseaseChanges { get; set; }

    public bool ReviewAutoQueueClinicalNotifications { get; set; }

    public bool ReviewAutoQueueNewCases { get; set; }

    public int ReviewDefaultPriority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public int? AddressReviewWindowAfterDays { get; set; }

    public int? AddressReviewWindowBeforeDays { get; set; }

    public bool CheckJurisdictionCrossing { get; set; }

    public string? JurisdictionFieldsToCheck { get; set; }

    public bool InheritAddressSettingsFromParent { get; set; }

    public virtual ICollection<CaseDefinition> CaseDefinitions { get; set; } = new List<CaseDefinition>();

    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    public virtual DiseaseCategory? DiseaseCategory { get; set; }

    public virtual ICollection<DiseaseCustomField> DiseaseCustomFields { get; set; } = new List<DiseaseCustomField>();

    public virtual DiseaseHl7matchingConfig? DiseaseHl7matchingConfig { get; set; }

    public virtual ICollection<DiseaseReinfectionRule> DiseaseReinfectionRules { get; set; } = new List<DiseaseReinfectionRule>();

    public virtual ICollection<DiseaseSymptom> DiseaseSymptoms { get; set; } = new List<DiseaseSymptom>();

    public virtual ICollection<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; } = new List<DiseaseTaskTemplate>();

    public virtual ICollection<Hl7configurationDisease> Hl7configurationDiseases { get; set; } = new List<Hl7configurationDisease>();

    public virtual ICollection<Hl7customFieldMapping> Hl7customFieldMappings { get; set; } = new List<Hl7customFieldMapping>();

    public virtual ICollection<Hl7fieldMapping> Hl7fieldMappings { get; set; } = new List<Hl7fieldMapping>();

    public virtual ICollection<Disease> InverseParentDisease { get; set; } = new List<Disease>();

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual ICollection<Outbreak> Outbreaks { get; set; } = new List<Outbreak>();

    public virtual Disease? ParentDisease { get; set; }

    public virtual ICollection<Pathogen> Pathogens { get; set; } = new List<Pathogen>();

    public virtual ICollection<ReviewQueue> ReviewQueues { get; set; } = new List<ReviewQueue>();

    public virtual ICollection<RoleDiseaseAccess> RoleDiseaseAccesses { get; set; } = new List<RoleDiseaseAccess>();

    public virtual ICollection<SurveyTemplateDisease> SurveyTemplateDiseases { get; set; } = new List<SurveyTemplateDisease>();

    public virtual ICollection<UserDiseaseAccess> UserDiseaseAccesses { get; set; } = new List<UserDiseaseAccess>();
}
