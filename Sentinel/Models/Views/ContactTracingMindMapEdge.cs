using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Edge data for contact tracing network visualization
/// Matches vw_ContactTracingMindMapEdges SQL view
/// </summary>
[Keyless]
public class ContactTracingMindMapEdge
{
    public Guid EdgeId { get; set; }
    
    [Display(Name = "Source Node")]
    public Guid SourceNodeId { get; set; }
    
    [Display(Name = "Target Node")]
    public Guid TargetNodeId { get; set; }
    
    [Display(Name = "Edge Type")]
    public string? EdgeType { get; set; }
    
    [Display(Name = "Exposure Date")]
    public DateTime? ExposureDate { get; set; }
    
    [Display(Name = "Confidence Level")]
    public string? ConfidenceLevel { get; set; }
}
