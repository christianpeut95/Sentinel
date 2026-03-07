using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Node data for contact tracing network visualization
/// Use with vw_ContactTracingMindMapEdges for graph rendering
/// </summary>
[Keyless]
public class ContactTracingMindMapNode
{
    public Guid NodeId { get; set; }
    
    [Display(Name = "Label")]
    public string NodeLabel { get; set; } = string.Empty;
    
    [Display(Name = "Name")]
    public string NodeName { get; set; } = string.Empty;
    
    [Display(Name = "Type")]
    public string NodeType { get; set; } = string.Empty;
    
    public Guid? DiseaseId { get; set; }
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Disease Code")]
    public string? DiseaseCode { get; set; }
    
    [Display(Name = "Date of Onset")]
    public DateTime? DateOfOnset { get; set; }
    
    [Display(Name = "Date of Notification")]
    public DateTime? DateOfNotification { get; set; }
    
    [Display(Name = "Date Identified")]
    public DateTime DateIdentified { get; set; }
    
    [Display(Name = "Status")]
    public string? CaseStatus { get; set; }
    
    [Display(Name = "Outgoing Transmissions")]
    public int OutgoingTransmissions { get; set; }
    
    [Display(Name = "Incoming Exposures")]
    public int IncomingExposures { get; set; }
    
    [Display(Name = "Total Tasks")]
    public int TotalTasks { get; set; }
    
    [Display(Name = "Completed Tasks")]
    public int CompletedTasks { get; set; }
    
    [Display(Name = "Follow-up Status")]
    public string FollowUpStatus { get; set; } = string.Empty;
    
    [Display(Name = "Suburb")]
    public string? Suburb { get; set; }
    
    [Display(Name = "State")]
    public string? State { get; set; }
    
    [Display(Name = "Jurisdiction")]
    public string? Jurisdiction1 { get; set; }
}
