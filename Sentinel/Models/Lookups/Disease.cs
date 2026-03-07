using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public enum DiseaseAccessLevel
    {
        [Display(Name = "Public")]
        Public = 0,      // Everyone can access (default for most diseases)
        
        [Display(Name = "Restricted")]
        Restricted = 1   // Requires explicit permission grant
    }

    public class Disease : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ExportCode { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        public Guid? DiseaseCategoryId { get; set; }
        public DiseaseCategory? DiseaseCategory { get; set; }

        public Guid? ParentDiseaseId { get; set; }
        public Disease? ParentDisease { get; set; }
        public ICollection<Disease> SubDiseases { get; set; } = new List<Disease>();

        [StringLength(4000)]
        public string PathIds { get; set; } = string.Empty;

        public int Level { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        [Display(Name = "Access Level")]
        public DiseaseAccessLevel AccessLevel { get; set; } = DiseaseAccessLevel.Public;

        [Display(Name = "Exposure Tracking Mode")]
        public ExposureTrackingMode ExposureTrackingMode { get; set; } = ExposureTrackingMode.Optional;

        [Display(Name = "Default to Residential Address")]
        public bool DefaultToResidentialAddress { get; set; } = false;

        [Display(Name = "Always Prompt for Location")]
        public bool AlwaysPromptForLocation { get; set; } = false;

        [Display(Name = "Sync with Patient Address Updates")]
        public bool SyncWithPatientAddressUpdates { get; set; } = false;

        [Display(Name = "Exposure Guidance Text")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string? ExposureGuidanceText { get; set; }

        [Display(Name = "Require Geographic Coordinates")]
        public bool RequireGeographicCoordinates { get; set; } = false;

        [Display(Name = "Allow Domestic Acquisition")]
        public bool AllowDomesticAcquisition { get; set; } = true;

        [Display(Name = "Exposure Data Grace Period (Days)")]
        public int? ExposureDataGracePeriodDays { get; set; }

        [Display(Name = "Required Location Type IDs")]
        [StringLength(500)]
        public string? RequiredLocationTypeIds { get; set; }

        // Data Review Configuration
        [Display(Name = "Review Grouping Window (Hours)")]
        public int ReviewGroupingWindowHours { get; set; } = 6;

        [Display(Name = "Auto-review Lab Results")]
        public bool ReviewAutoQueueLabResults { get; set; } = true;

        [Display(Name = "Auto-review Exposures")]
        public bool ReviewAutoQueueExposures { get; set; } = false;

        [Display(Name = "Auto-review Contacts")]
        public bool ReviewAutoQueueContacts { get; set; } = false;

        [Display(Name = "Auto-review Confirmation Status Changes")]
        public bool ReviewAutoQueueConfirmationChanges { get; set; } = true;

        [Display(Name = "Auto-review Disease Changes")]
        public bool ReviewAutoQueueDiseaseChanges { get; set; } = true;


        [Display(Name = "Auto-review Clinical Notifications")]
        public bool ReviewAutoQueueClinicalNotifications { get; set; } = false;

        [Display(Name = "Auto-review New Cases")]
        public bool ReviewAutoQueueNewCases { get; set; } = false;

        [Display(Name = "Review Default Priority")]
        public int ReviewDefaultPriority { get; set; } = 1;

        public ICollection<Case> Cases { get; set; } = new List<Case>();
        
        public ICollection<DiseaseCustomField> DiseaseCustomFields { get; set; } = new List<DiseaseCustomField>();
        
        public ICollection<DiseaseSymptom> DiseaseSymptoms { get; set; } = new List<DiseaseSymptom>();
        
        public ICollection<RoleDiseaseAccess> RoleDiseaseAccess { get; set; } = new List<RoleDiseaseAccess>();
        
        public ICollection<UserDiseaseAccess> UserDiseaseAccess { get; set; } = new List<UserDiseaseAccess>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
