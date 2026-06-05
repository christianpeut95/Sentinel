using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Represents an incoming HL7 message for laboratory results processing
    /// </summary>
    public class HL7Message : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Display(Name = "Message Control ID")]
        [StringLength(100)]
        public string MessageControlId { get; set; } = string.Empty; // MSH-10

        [Display(Name = "Message Type")]
        [StringLength(50)]
        public string MessageType { get; set; } = string.Empty; // e.g., ORU^R01

        [Display(Name = "Message Date/Time")]
        public DateTime MessageDateTime { get; set; } // MSH-7

        [Display(Name = "Sending Facility")]
        [StringLength(200)]
        public string? SendingFacility { get; set; } // MSH-4

        [Display(Name = "Sending Application")]
        [StringLength(200)]
        public string? SendingApplication { get; set; } // MSH-3

        [Display(Name = "Receiving Facility")]
        [StringLength(200)]
        public string? ReceivingFacility { get; set; } // MSH-6

        [Display(Name = "Receiving Application")]
        [StringLength(200)]
        public string? ReceivingApplication { get; set; } // MSH-5

        [Display(Name = "HL7 Version")]
        [StringLength(20)]
        public string? HL7Version { get; set; } // MSH-12 (e.g., 2.5, 2.3.1)

        [Required]
        [Display(Name = "Raw Message")]
        public string RawMessage { get; set; } = string.Empty; // Full HL7 text

        [Display(Name = "File Path")]
        [StringLength(1000)]
        public string? FilePath { get; set; } // Original file path

        [Display(Name = "File Name")]
        [StringLength(500)]
        public string? FileName { get; set; }

        [Display(Name = "File Size (bytes)")]
        public long? FileSizeBytes { get; set; }

        [Required]
        [Display(Name = "Processing Status")]
        public HL7ProcessingStatus Status { get; set; } = HL7ProcessingStatus.Received;

        [Display(Name = "Error Message")]
        [StringLength(4000)]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Processing Notes")]
        [DataType(DataType.MultilineText)]
        public string? ProcessingNotes { get; set; }

        [Required]
        [Display(Name = "Received At")]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Parsed At")]
        public DateTime? ParsedAt { get; set; }

        [Display(Name = "Processed At")]
        public DateTime? ProcessedAt { get; set; }

        [Display(Name = "Processed By")]
        [StringLength(450)]
        public string? ProcessedByUserId { get; set; }
        public ApplicationUser? ProcessedByUser { get; set; }

        // Links to created entities
        [Display(Name = "Patient")]
        public Guid? PatientId { get; set; }
        public Patient? Patient { get; set; }

        [Display(Name = "Case")]
        public Guid? CaseId { get; set; }
        public Case? Case { get; set; }

        [Display(Name = "Lab Result")]
        public Guid? LabResultId { get; set; }
        public LabResult? LabResult { get; set; }

        [Display(Name = "Laboratory Organization")]
        public Guid? LaboratoryOrganizationId { get; set; }
        public Organization? LaboratoryOrganization { get; set; }

        [Display(Name = "Ordering Provider Organization")]
        public Guid? OrderingProviderOrganizationId { get; set; }
        public Organization? OrderingProviderOrganization { get; set; }

        // Configuration used for processing
        [Display(Name = "Configuration")]
        public Guid? ConfigurationId { get; set; }
        public HL7Configuration? Configuration { get; set; }

        // Duplicate Detection
        [Display(Name = "Is Duplicate")]
        public bool IsDuplicate { get; set; } = false;

        [Display(Name = "Duplicate of Message")]
        public Guid? DuplicateOfMessageId { get; set; }
        public HL7Message? DuplicateOfMessage { get; set; }

        [Display(Name = "Duplicate Detection Method")]
        [StringLength(200)]
        public string? DuplicateDetectionMethod { get; set; }

        // Manual Review Tracking
        [Display(Name = "Requires Manual Review")]
        public bool RequiresManualReview { get; set; } = false;

        [Display(Name = "Manual Review Completed")]
        public bool ManualReviewCompleted { get; set; } = false;

        [Display(Name = "Manual Review By")]
        [StringLength(450)]
        public string? ManualReviewByUserId { get; set; }
        public ApplicationUser? ManualReviewByUser { get; set; }

        [Display(Name = "Manual Review Date")]
        public DateTime? ManualReviewDate { get; set; }

        [Display(Name = "Manual Review Notes")]
        [DataType(DataType.MultilineText)]
        public string? ManualReviewNotes { get; set; }

        // Parsing Details
        public ICollection<HL7MessageSegment> Segments { get; set; } = new List<HL7MessageSegment>();
        public ICollection<HL7ParsingIssue> ParsingIssues { get; set; } = new List<HL7ParsingIssue>();

        // Audit fields (from IAuditable)
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }

        // Soft Delete Properties (from ISoftDeletable)
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }

    /// <summary>
    /// Processing status for HL7 messages
    /// </summary>
    public enum HL7ProcessingStatus
    {
        [Display(Name = "Received")]
        Received = 0,

        [Display(Name = "Queued for Parsing")]
        QueuedForParsing = 1,

        [Display(Name = "Parsing")]
        Parsing = 2,

        [Display(Name = "Parsed Successfully")]
        ParsedSuccessfully = 3,

        [Display(Name = "Parsing Failed")]
        ParsingFailed = 4,

        [Display(Name = "Queued for Processing")]
        QueuedForProcessing = 5,

        [Display(Name = "Processing")]
        Processing = 6,

        [Display(Name = "Processed Successfully")]
        ProcessedSuccessfully = 7,

        [Display(Name = "Processed with Warnings")]
        ProcessedWithWarnings = 8,

        [Display(Name = "Processing Failed")]
        ProcessingFailed = 9,

        [Display(Name = "Awaiting Manual Review")]
        AwaitingManualReview = 10,

        [Display(Name = "Manual Review Complete")]
        ManualReviewComplete = 11,

        [Display(Name = "Duplicate Detected")]
        DuplicateDetected = 12,

        [Display(Name = "Archived")]
        Archived = 13
    }
}
