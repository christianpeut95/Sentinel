using System.ComponentModel.DataAnnotations;

namespace Sentinel.DTOs;

public class BulkContactDto
{
    [Required]
    [Display(Name = "First Name")]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Last Name")]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }
    
    [Display(Name = "Contact Phone")]
    [Phone]
    [StringLength(20)]
    public string? ContactPhone { get; set; }
    
    [Display(Name = "Email")]
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }
    
    [Display(Name = "Parent/Guardian Name")]
    [StringLength(200)]
    public string? ParentGuardianName { get; set; }
    
    [Display(Name = "Parent/Guardian Phone")]
    [Phone]
    [StringLength(20)]
    public string? ParentGuardianPhone { get; set; }
    
    // Exposure Details
    [Display(Name = "Contact Classification")]
    public int? ContactClassificationId { get; set; }
    
    [Required]
    [Display(Name = "Exposure Start Date")]
    public DateTime ExposureStartDate { get; set; }
    
    [Display(Name = "Exposure End Date")]
    public DateTime? ExposureEndDate { get; set; }
    
    [Display(Name = "Exposure Status")]
    [StringLength(50)]
    public string? ExposureStatus { get; set; } // ConfirmedExposure, PotentialExposure, UnderInvestigation
    
    [Display(Name = "Confidence Level")]
    [StringLength(50)]
    public string? ConfidenceLevel { get; set; } // High, Medium, Low
    
    [Display(Name = "Notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    // Import Control
    [Display(Name = "Include in Import")]
    public bool IncludeInImport { get; set; } = true;
    
    // For duplicate detection results
    public bool IsPotentialDuplicate { get; set; }
    public List<Guid> PossibleMatchPatientIds { get; set; } = new();
    public string? MatchReason { get; set; }
    
    // User decision
    public Guid? LinkToExistingPatientId { get; set; }
    public bool CreateAsNew { get; set; } = true;
    
    // For CSV row tracking
    public int RowNumber { get; set; }
}

