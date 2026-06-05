using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Tracks specific parsing issues encountered when processing HL7 messages
    /// Used for learning and improving parsing rules
    /// </summary>
    public class HL7ParsingIssue
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "HL7 Message")]
        public Guid HL7MessageId { get; set; }
        public HL7Message? HL7Message { get; set; }

        [Display(Name = "Message Segment")]
        public Guid? MessageSegmentId { get; set; }
        public HL7MessageSegment? MessageSegment { get; set; }

        [Required]
        [Display(Name = "Segment Type")]
        [StringLength(10)]
        public string SegmentType { get; set; } = string.Empty; // e.g., PID, OBR, OBX

        [Display(Name = "Field Path")]
        [StringLength(100)]
        public string? FieldPath { get; set; } // e.g., "PID-3[2].1" (Repeating Field, Component)

        [Display(Name = "Field Name")]
        [StringLength(200)]
        public string? FieldName { get; set; } // Human-readable field name

        [Required]
        [Display(Name = "Issue Type")]
        public HL7IssueType IssueType { get; set; }

        [Display(Name = "Severity")]
        public HL7IssueSeverity Severity { get; set; } = HL7IssueSeverity.Warning;

        [Required]
        [Display(Name = "Description")]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Raw Value")]
        [StringLength(1000)]
        public string? RawValue { get; set; } // The problematic value from HL7

        [Display(Name = "Expected Format")]
        [StringLength(500)]
        public string? ExpectedFormat { get; set; }

        [Display(Name = "Suggested Mapping")]
        [StringLength(500)]
        public string? SuggestedMapping { get; set; }

        [Display(Name = "Is Resolved")]
        public bool IsResolved { get; set; } = false;

        [Display(Name = "Resolved At")]
        public DateTime? ResolvedAt { get; set; }

        [Display(Name = "Resolved By")]
        [StringLength(450)]
        public string? ResolvedByUserId { get; set; }
        public ApplicationUser? ResolvedByUser { get; set; }

        [Display(Name = "Resolution Notes")]
        [StringLength(2000)]
        public string? ResolutionNotes { get; set; }

        [Display(Name = "Field Mapping")]
        public Guid? FieldMappingId { get; set; }
        public HL7FieldMapping? FieldMapping { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Ignore Future Occurrences")]
        public bool IgnoreFutureOccurrences { get; set; } = false;
    }

    /// <summary>
    /// Types of parsing issues that can occur
    /// </summary>
    public enum HL7IssueType
    {
        [Display(Name = "Unrecognized Code")]
        UnrecognizedCode = 1,

        [Display(Name = "Missing Required Field")]
        MissingRequiredField = 2,

        [Display(Name = "Invalid Format")]
        InvalidFormat = 3,

        [Display(Name = "Invalid Data Type")]
        InvalidDataType = 4,

        [Display(Name = "Missing Mapping")]
        MissingMapping = 5,

        [Display(Name = "Ambiguous Data")]
        AmbiguousData = 6,

        [Display(Name = "Date/Time Parse Error")]
        DateTimeParseError = 7,

        [Display(Name = "Unsupported Segment")]
        UnsupportedSegment = 8,

        [Display(Name = "Unsupported Message Type")]
        UnsupportedMessageType = 9,

        [Display(Name = "Field Too Long")]
        FieldTooLong = 10,

        [Display(Name = "Duplicate Field")]
        DuplicateField = 11,

        [Display(Name = "Validation Failed")]
        ValidationFailed = 12,

        [Display(Name = "Other")]
        Other = 99
    }

    /// <summary>
    /// Severity levels for parsing issues
    /// </summary>
    public enum HL7IssueSeverity
    {
        [Display(Name = "Information")]
        Information = 0,

        [Display(Name = "Warning")]
        Warning = 1,

        [Display(Name = "Error")]
        Error = 2,

        [Display(Name = "Critical")]
        Critical = 3
    }
}
