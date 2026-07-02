using System.ComponentModel.DataAnnotations;
using Sentinel.HL7Generator.Models;

namespace Sentinel.Models;

/// <summary>
/// Saved template for generating test HL7 messages
/// </summary>
public class HL7TestMessageTemplate : IAuditable
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Template Name")]
    [StringLength(200)]
    public string TemplateName { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Lab Template Type")]
    public LabTemplateType LabTemplateType { get; set; }

    [Display(Name = "Configuration")]
    public string ConfigurationJson { get; set; } = "{}"; // JSON serialized HL7MessageRequest

    [Display(Name = "Test Comment")]
    [StringLength(2000)]
    public string? TestComment { get; set; }

    [Display(Name = "Is Favorite")]
    public bool IsFavorite { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(450)]
    public string? CreatedBy { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(450)]
    public string? UpdatedBy { get; set; }
    public ApplicationUser? UpdatedByUser { get; set; }
}
