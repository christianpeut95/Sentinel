using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models;

/// <summary>
/// Audit trail for changes to lab result markers
/// Tracks all updates to test results from preliminary to final to corrected
/// </summary>
public class LabResultMarkerHistory
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Lab Result Marker")]
    public Guid LabResultMarkerId { get; set; }
    public LabResultMarker? LabResultMarker { get; set; }

    [Required]
    [Display(Name = "HL7 Message")]
    public Guid HL7MessageId { get; set; }
    public HL7Message? HL7Message { get; set; }

    [Required]
    [Display(Name = "Changed At")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Display(Name = "Change Type")]
    public MarkerChangeType ChangeType { get; set; }

    // Previous values (before change)
    [Display(Name = "Previous Qualitative Value")]
    [StringLength(1000)]
    public string? PreviousQualitativeValue { get; set; }

    [Display(Name = "Previous Quantitative Value")]
    public decimal? PreviousQuantitativeValue { get; set; }

    [Display(Name = "Previous Result Status")]
    [StringLength(10)]
    public string? PreviousResultStatus { get; set; }

    [Display(Name = "Previous Abnormal Flag")]
    [StringLength(10)]
    public string? PreviousAbnormalFlag { get; set; }

    // New values (after change)
    [Display(Name = "New Qualitative Value")]
    [StringLength(1000)]
    public string? NewQualitativeValue { get; set; }

    [Display(Name = "New Quantitative Value")]
    public decimal? NewQuantitativeValue { get; set; }

    [Display(Name = "New Result Status")]
    [StringLength(10)]
    public string? NewResultStatus { get; set; }

    [Display(Name = "New Abnormal Flag")]
    [StringLength(10)]
    public string? NewAbnormalFlag { get; set; }

    [Display(Name = "Change Reason")]
    [StringLength(500)]
    public string? ChangeReason { get; set; }

    [Display(Name = "Changed By System")]
    public bool ChangedBySystem { get; set; } = true;

    [Display(Name = "Changed By User")]
    [StringLength(450)]
    public string? ChangedByUserId { get; set; }
    public ApplicationUser? ChangedByUser { get; set; }
}

/// <summary>
/// Types of changes that can occur to lab result markers
/// </summary>
public enum MarkerChangeType
{
    [Display(Name = "Created")]
    Created = 1,

    [Display(Name = "Updated")]
    Updated = 2,

    [Display(Name = "Corrected")]
    Corrected = 3,

    [Display(Name = "Amended")]
    Amended = 4,

    [Display(Name = "Cancelled")]
    Cancelled = 5,

    [Display(Name = "Finalized")]
    Finalized = 6
}
