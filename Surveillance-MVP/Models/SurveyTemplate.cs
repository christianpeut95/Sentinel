using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models
{
    /// <summary>
    /// Reusable survey template that can be shared across multiple task templates
    /// </summary>
    public class SurveyTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Display(Name = "Survey Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000)]
        public string? Description { get; set; }

        [Display(Name = "Category")]
        [StringLength(100)]
        public string? Category { get; set; } // e.g., "Foodborne", "Respiratory", "Contact Investigation"

        [Required]
        [Display(Name = "Survey Definition (SurveyJS JSON)")]
        [DataType(DataType.MultilineText)]
        public string SurveyDefinitionJson { get; set; } = string.Empty;

        [Display(Name = "Default Input Mapping")]
        [DataType(DataType.MultilineText)]
        public string? DefaultInputMappingJson { get; set; }

        [Display(Name = "Default Output Mapping")]
        [DataType(DataType.MultilineText)]
        public string? DefaultOutputMappingJson { get; set; }

        [Display(Name = "Version")]
        public int Version { get; set; } = 1;
        
        // Version Management
        [Display(Name = "Parent Survey Template")]
        public Guid? ParentSurveyTemplateId { get; set; } // Links versions together
        
        [Display(Name = "Version Number")]
        [StringLength(20)]
        public string VersionNumber { get; set; } = "1.0"; // User-friendly version like "1.0", "2.0", "2.1-draft"
        
        [Display(Name = "Version Status")]
        public SurveyVersionStatus VersionStatus { get; set; } = SurveyVersionStatus.Draft;
        
        [Display(Name = "Version Notes")]
        [StringLength(2000)]
        public string? VersionNotes { get; set; } // What changed in this version
        
        [Display(Name = "Published At")]
        public DateTime? PublishedAt { get; set; } // When this version became active
        
        [Display(Name = "Published By")]
        [StringLength(256)]
        public string? PublishedBy { get; set; }

        [Display(Name = "Tags (comma-separated)")]
        [StringLength(500)]
        public string? Tags { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "System Template")]
        public bool IsSystemTemplate { get; set; } = false; // Prevent deletion of critical templates

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Usage tracking
        [Display(Name = "Usage Count")]
        public int UsageCount { get; set; } = 0;
        public DateTime? LastUsedAt { get; set; }

        // Navigation properties
        public virtual ICollection<SurveyTemplateDisease> ApplicableDiseases { get; set; } = new List<SurveyTemplateDisease>();
        public virtual ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
        
        // Version management navigation
        public virtual SurveyTemplate? ParentSurveyTemplate { get; set; }
        public virtual ICollection<SurveyTemplate> ChildVersions { get; set; } = new List<SurveyTemplate>();
    }
    
    /// <summary>
    /// Survey version status lifecycle
    /// </summary>
    public enum SurveyVersionStatus
    {
        Draft = 0,      // Work in progress, not used in tasks
        Active = 1,     // Currently in use (only one active version per survey family)
        Archived = 2    // Previously active, now superseded
    }

    /// <summary>
    /// Junction table for many-to-many relationship between SurveyTemplates and Diseases
    /// </summary>
    public class SurveyTemplateDisease
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid SurveyTemplateId { get; set; }
        public virtual SurveyTemplate SurveyTemplate { get; set; } = null!;

        public Guid DiseaseId { get; set; }
        public virtual Surveillance_MVP.Models.Lookups.Disease Disease { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
