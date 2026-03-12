using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Node data for contact tracing network visualization
/// Matches vw_ContactTracingMindMapNodes SQL view
/// </summary>
[Keyless]
public class ContactTracingMindMapNode
{
    public Guid NodeId { get; set; }
    
    [Display(Name = "Label")]
    public string? NodeLabel { get; set; }
    
    [Display(Name = "Type")]
    public int NodeType { get; set; }
    
    [Display(Name = "Person Name")]
    public string? PersonName { get; set; }
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Status")]
    public string? CaseStatus { get; set; }
    
    [Display(Name = "Date of Onset")]
    public DateTime? DateOfOnset { get; set; }
    
    public bool IsDeleted { get; set; }
}
