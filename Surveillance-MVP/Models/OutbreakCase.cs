using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models;

public class OutbreakCase
{
    public int Id { get; set; }
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    public Guid CaseId { get; set; }
    public Case Case { get; set; } = null!;
    
    public CaseClassification? Classification { get; set; }
    public DateTime? ClassificationDate { get; set; }
    public string? ClassifiedBy { get; set; }
    
    [StringLength(1000)]
    public string? ClassificationNotes { get; set; }
    
    public LinkMethod LinkMethod { get; set; }
    
    public int? SearchQueryId { get; set; }
    public OutbreakSearchQuery? SearchQuery { get; set; }
    
    public DateTime LinkedDate { get; set; }
    public string? LinkedBy { get; set; }
    
    public DateTime? UnlinkedDate { get; set; }
    public string? UnlinkedBy { get; set; }
    
    [StringLength(500)]
    public string? UnlinkReason { get; set; }
    
    public bool IsActive { get; set; } = true;
}

