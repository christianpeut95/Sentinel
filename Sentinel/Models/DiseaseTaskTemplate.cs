using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models
{
    public class DiseaseTaskTemplate : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Required]
        [Display(Name = "Task Template")]
        public Guid TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        [Display(Name = "Applicable To")]
        public CaseType? ApplicableTo { get; set; }

        // Hierarchy Support
        [Display(Name = "Inherited from Parent")]
        public bool IsInherited { get; set; }

        [Display(Name = "Inherited From Disease")]
        public Guid? InheritedFromDiseaseId { get; set; }

        [Display(Name = "Apply to Child Diseases")]
        public bool ApplyToChildren { get; set; } = true;

        [Display(Name = "Allow Child Override")]
        public bool AllowChildOverride { get; set; } = true;

        // Child-specific Overrides (when IsInherited = true)
        [Display(Name = "Override Auto-Create")]
        public bool? OverrideAutoCreate { get; set; }

        [Display(Name = "Override Priority")]
        public TaskPriority? OverridePriority { get; set; }

        [Display(Name = "Override Due Days")]
        public int? OverrideDueDays { get; set; }

        [StringLength(4000)]
        [Display(Name = "Override Instructions")]
        [DataType(DataType.MultilineText)]
        public string? OverrideInstructions { get; set; }

        // Original Settings (when IsInherited = false)
        [Display(Name = "Auto-Create on Case Creation")]
        public bool AutoCreateOnCaseCreation { get; set; }

        [Display(Name = "Auto-Create on Contact Creation")]
        public bool AutoCreateOnContactCreation { get; set; }

        [Display(Name = "Auto-Create on Lab Confirmation")]
        public bool AutoCreateOnLabConfirmation { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // Survey Field Mapping (JSON)
        [Display(Name = "Input Mapping (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? InputMappingJson { get; set; }

        [Display(Name = "Output Mapping (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? OutputMappingJson { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
    }
}
