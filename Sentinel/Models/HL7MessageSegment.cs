using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Represents a single segment within an HL7 message for detailed tracking and parsing
    /// </summary>
    public class HL7MessageSegment
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "HL7 Message")]
        public Guid HL7MessageId { get; set; }
        public HL7Message? HL7Message { get; set; }

        [Required]
        [Display(Name = "Segment Type")]
        [StringLength(10)]
        public string SegmentType { get; set; } = string.Empty; // e.g., PID, OBR, OBX, MSH, etc.

        [Display(Name = "Sequence Number")]
        public int SequenceNumber { get; set; } // Order in message (1-based)

        [Display(Name = "Set ID")]
        public int? SetId { get; set; } // For repeating segments (e.g., OBX set ID)

        [Required]
        [Display(Name = "Raw Segment")]
        [StringLength(4000)]
        public string RawSegment { get; set; } = string.Empty; // Full segment text

        [Display(Name = "Is Parsed")]
        public bool IsParsed { get; set; } = false;

        [Display(Name = "Parsed Data")]
        public string? ParsedData { get; set; } // JSON representation of parsed fields

        [Display(Name = "Field Count")]
        public int? FieldCount { get; set; } // Number of fields detected

        [Display(Name = "Error Details")]
        [StringLength(2000)]
        public string? ErrorDetails { get; set; }

        [Display(Name = "Has Issues")]
        public bool HasIssues { get; set; } = false;

        [Display(Name = "Parsed At")]
        public DateTime? ParsedAt { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
