using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Defines custom field mappings for HL7 messages from specific facilities
    /// Allows learning and storing facility-specific field interpretations
    /// </summary>
    public class HL7FieldMapping : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Configuration")]
        public Guid ConfigurationId { get; set; }
        public HL7Configuration? Configuration { get; set; }

        [Display(Name = "Disease (Optional)")]
        public Guid? DiseaseId { get; set; } // Null = applies to all diseases
        public Lookups.Disease? Disease { get; set; }

        [Required]
        [Display(Name = "Segment Type")]
        [StringLength(10)]
        public string SegmentType { get; set; } = string.Empty; // e.g., PID, OBR, OBX

        [Required]
        [Display(Name = "Field Path")]
        [StringLength(100)]
        public string FieldPath { get; set; } = string.Empty; // e.g., "OBX-3.1", "PID-5.1"

        [Display(Name = "Field Name")]
        [StringLength(200)]
        public string? FieldName { get; set; } // Human-readable name

        [Required]
        [Display(Name = "Target Entity")]
        [StringLength(100)]
        public string TargetEntity { get; set; } = string.Empty; // e.g., "Patient", "LabResult", "Organization"

        [Required]
        [Display(Name = "Target Property")]
        [StringLength(100)]
        public string TargetProperty { get; set; } = string.Empty; // e.g., "FamilyName", "AccessionNumber"

        [Display(Name = "Mapping Type")]
        public HL7MappingType MappingType { get; set; } = HL7MappingType.DirectCopy;

        [Display(Name = "Transformation Rule")]
        [StringLength(500)]
        public string? TransformationRule { get; set; } // Expression or rule for transformation

        [Display(Name = "Lookup Table")]
        [StringLength(200)]
        public string? LookupTable { get; set; } // Reference to lookup table for code mapping

        [Display(Name = "Code Mapping JSON")]
        public string? CodeMappingJson { get; set; } // JSON for code-to-code mappings

        [Display(Name = "Default Value")]
        [StringLength(500)]
        public string? DefaultValue { get; set; } // Use if HL7 field is empty

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Validation Regex")]
        [StringLength(500)]
        public string? ValidationRegex { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 100; // Higher priority mappings processed first

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Example HL7 Value")]
        [StringLength(500)]
        public string? ExampleHL7Value { get; set; }

        [Display(Name = "Example Mapped Value")]
        [StringLength(500)]
        public string? ExampleMappedValue { get; set; }

        [Display(Name = "Sample HL7 Message")]
        public string? SampleMessage { get; set; } // Store sample message for testing/reference

        // Learning System Fields
        [Display(Name = "Times Used")]
        public int TimesUsed { get; set; } = 0;

        [Display(Name = "Times Failed")]
        public int TimesFailed { get; set; } = 0;

        [Display(Name = "Last Used At")]
        public DateTime? LastUsedAt { get; set; }

        [Display(Name = "Created From Issue")]
        public Guid? CreatedFromIssueId { get; set; }
        public HL7ParsingIssue? CreatedFromIssue { get; set; }

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Types of field mappings for HL7 data transformation
    /// </summary>
    public enum HL7MappingType
    {
        [Display(Name = "Direct Copy")]
        DirectCopy = 1,

        [Display(Name = "Code Lookup")]
        CodeLookup = 2,

        [Display(Name = "Date Format Conversion")]
        DateFormatConversion = 3,

        [Display(Name = "Name Parsing")]
        NameParsing = 4,

        [Display(Name = "Phone Number Formatting")]
        PhoneNumberFormatting = 5,

        [Display(Name = "Address Parsing")]
        AddressParsing = 6,

        [Display(Name = "Custom Expression")]
        CustomExpression = 7,

        [Display(Name = "Concatenation")]
        Concatenation = 8,

        [Display(Name = "Split/Extract")]
        SplitExtract = 9,

        [Display(Name = "Numeric Conversion")]
        NumericConversion = 10,

        [Display(Name = "Boolean Conversion")]
        BooleanConversion = 11
    }
}
