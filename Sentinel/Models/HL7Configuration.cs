using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Configuration for HL7 message processing from specific facilities or sources
    /// Allows customized parsing rules per sending facility
    /// </summary>
    public class HL7Configuration : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Configuration Name")]
        [StringLength(200)]
        public string ConfigurationName { get; set; } = string.Empty;

        [Display(Name = "Sending Facility")]
        [StringLength(200)]
        public string? SendingFacility { get; set; } // MSH-4 - to match incoming messages

        [Display(Name = "Sending Application")]
        [StringLength(200)]
        public string? SendingApplication { get; set; } // MSH-3

        [Display(Name = "File Drop Path")]
        [StringLength(1000)]
        public string? FileDropPath { get; set; } // Directory to monitor

        [Display(Name = "File Pattern")]
        [StringLength(100)]
        public string FilePattern { get; set; } = "*.hl7"; // e.g., *.hl7, *.oru, *.txt

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 100; // Higher = processed first

        [Display(Name = "Character Encoding")]
        [StringLength(50)]
        public string CharacterEncoding { get; set; } = "UTF-8"; // e.g., UTF-8, ASCII, ISO-8859-1

        // Default Organization Mappings
        [Display(Name = "Default Laboratory")]
        public Guid? DefaultLaboratoryId { get; set; }
        public Organization? DefaultLaboratory { get; set; }

        [Display(Name = "Auto-create Organizations")]
        public bool AutoCreateOrganizations { get; set; } = false;

        // Patient Matching Configuration
        [Display(Name = "Patient Matching Strategy")]
        public PatientMatchingStrategy PatientMatchingStrategy { get; set; } = PatientMatchingStrategy.StrictMatch;

        [Display(Name = "Auto-create Patients")]
        public bool AutoCreatePatients { get; set; } = true;

        [Display(Name = "Auto-create Cases")]
        public bool AutoCreateCases { get; set; } = false; // Require manual case creation by default

        // Duplicate Detection Settings
        [Display(Name = "Duplicate Detection Window (Hours)")]
        public int DuplicateDetectionWindowHours { get; set; } = 72;

        [Display(Name = "Duplicate Detection Strategy")]
        public DuplicateDetectionStrategy DuplicateDetectionStrategy { get; set; } = DuplicateDetectionStrategy.AccessionAndLab;

        // Processing Configuration
        [Display(Name = "Field Mapping Configuration")]
        public string? FieldMappingConfig { get; set; } // JSON configuration for custom field mappings

        [Display(Name = "Process on Receipt")]
        public bool ProcessOnReceipt { get; set; } = true; // Auto-process or queue for manual

        [Display(Name = "Archive Processed Files")]
        public bool ArchiveProcessedFiles { get; set; } = true;

        [Display(Name = "Archive Path")]
        [StringLength(1000)]
        public string? ArchivePath { get; set; }

        [Display(Name = "Delete After Archive")]
        public bool DeleteAfterArchive { get; set; } = true;

        // Notification Settings
        [Display(Name = "Send Notifications on Error")]
        public bool SendNotificationsOnError { get; set; } = true;

        [Display(Name = "Notification Email Addresses")]
        [StringLength(1000)]
        public string? NotificationEmailAddresses { get; set; } // Comma-separated

        // Disease Association (MVP)
        [Display(Name = "Test Mode")]
        public bool IsTestMode { get; set; } = false; // When true, messages are staged but not committed

        [Display(Name = "Test Mode Description")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string? TestModeDescription { get; set; } // User-friendly explanation of test mode behavior

        // Date/Time Parsing
        [Display(Name = "Default Date Format")]
        [StringLength(50)]
        public string? DefaultDateFormat { get; set; } // e.g., "yyyyMMddHHmmss"

        [Display(Name = "Timezone Offset")]
        [StringLength(20)]
        public string? TimezoneOffset { get; set; } // e.g., "+10:00"

        // Validation Rules
        [Display(Name = "Require Patient Identifier")]
        public bool RequirePatientIdentifier { get; set; } = true;

        [Display(Name = "Require Specimen Collection Date")]
        public bool RequireSpecimenCollectionDate { get; set; } = false;

        [Display(Name = "Require Result Date")]
        public bool RequireResultDate { get; set; } = false;

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        // Navigation
        public ICollection<HL7Message> Messages { get; set; } = new List<HL7Message>();
        public ICollection<HL7FieldMapping> FieldMappings { get; set; } = new List<HL7FieldMapping>();
        public ICollection<HL7ConfigurationDisease> ConfigurationDiseases { get; set; } = new List<HL7ConfigurationDisease>();

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Strategy for matching incoming HL7 patient data to existing patients
    /// </summary>
    public enum PatientMatchingStrategy
    {
        [Display(Name = "Strict Match (Identifier + Name + DOB)")]
        StrictMatch = 1,

        [Display(Name = "Identifier Only")]
        IdentifierOnly = 2,

        [Display(Name = "Fuzzy Match (Name + DOB)")]
        FuzzyMatch = 3,

        [Display(Name = "Manual Review Required")]
        ManualReviewRequired = 4
    }

    /// <summary>
    /// Strategy for detecting duplicate messages
    /// </summary>
    public enum DuplicateDetectionStrategy
    {
        [Display(Name = "Message Control ID")]
        MessageControlId = 1,

        [Display(Name = "Accession Number + Laboratory")]
        AccessionAndLab = 2,

        [Display(Name = "Patient + Specimen Date + Test")]
        PatientSpecimenTest = 3,

        [Display(Name = "All Methods Combined")]
        Combined = 4,

        [Display(Name = "Disabled")]
        Disabled = 0
    }
}
