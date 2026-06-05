using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// Associates an HL7 configuration with one or more diseases
    /// Allows field mappings to be disease-specific
    /// </summary>
    public class HL7ConfigurationDisease : IAuditable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Configuration")]
        public Guid ConfigurationId { get; set; }
        public HL7Configuration? Configuration { get; set; }

        [Required]
        [Display(Name = "Disease")]
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }

        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; } = false; // Only one default per configuration

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 100; // When multiple diseases match, higher priority wins

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }
}
