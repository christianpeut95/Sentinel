using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models;

public class OutbreakSearchQuery
{
    public int Id { get; set; }
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    [Required]
    [StringLength(200)]
    public string QueryName { get; set; } = string.Empty;
    
    [Required]
    public string QueryJson { get; set; } = string.Empty;
    
    public bool IsAutoLink { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastRunDate { get; set; }
    public int? LastRunMatchCount { get; set; }
    
    public bool IsActive { get; set; } = true;
}
