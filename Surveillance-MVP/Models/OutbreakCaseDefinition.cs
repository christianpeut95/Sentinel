using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models;

public class OutbreakCaseDefinition
{
    public int Id { get; set; }
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    [Required]
    [StringLength(200)]
    public string DefinitionName { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? DefinitionText { get; set; }
    
    [Required]
    public CaseClassification Classification { get; set; }
    
    [Required]
    public string CriteriaJson { get; set; } = string.Empty;
    
    public int Version { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

