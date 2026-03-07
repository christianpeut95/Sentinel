using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Edge data for contact tracing network visualization
/// Represents exposures/transmissions between cases
/// Use with vw_ContactTracingMindMapNodes for graph rendering
/// </summary>
[Keyless]
public class ContactTracingMindMapEdge
{
    public Guid EdgeId { get; set; }
    
    [Display(Name = "Source Node")]
    public Guid SourceNodeId { get; set; }
    
    [Display(Name = "Target Node")]
    public Guid TargetNodeId { get; set; }
    
    [Display(Name = "Source Label")]
    public string SourceLabel { get; set; } = string.Empty;
    
    [Display(Name = "Target Label")]
    public string TargetLabel { get; set; } = string.Empty;
    
    public int ExposureTypeEnum { get; set; }
    
    [Display(Name = "Exposure Type")]
    public string ExposureType { get; set; } = string.Empty;
    
    public int ExposureStatusEnum { get; set; }
    
    [Display(Name = "Exposure Status")]
    public string ExposureStatus { get; set; } = string.Empty;
    
    [Display(Name = "Label")]
    public string? EdgeLabel { get; set; }
    
    [Display(Name = "Event Name")]
    public string? EventName { get; set; }
    
    [Display(Name = "Event Type")]
    public string? EventType { get; set; }
    
    [Display(Name = "Location Name")]
    public string? LocationName { get; set; }
    
    [Display(Name = "Location Type")]
    public string? LocationType { get; set; }
    
    [Display(Name = "Location Address")]
    public string? LocationAddress { get; set; }
    
    [Display(Name = "Contact Classification")]
    public string? ContactClassification { get; set; }
    
    [Display(Name = "Exposure Start")]
    public DateTime? ExposureStartDate { get; set; }
    
    [Display(Name = "Exposure End")]
    public DateTime? ExposureEndDate { get; set; }
    
    [Display(Name = "Edge Style")]
    public string EdgeStyle { get; set; } = string.Empty;
    
    [Display(Name = "Edge Color")]
    public string? EdgeColor { get; set; }
    
    [Display(Name = "Edge Weight")]
    public int? EdgeWeight { get; set; }
}
