using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Timeline view showing all events for a case in chronological order
/// Includes: Case creation, tasks, lab results, exposures
/// </summary>
[Keyless]
public class CaseTimelineEvent
{
    public Guid CaseId { get; set; }
    
    [Display(Name = "Case Number")]
    public string CaseNumber { get; set; } = string.Empty;
    
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; } = string.Empty;
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Event Type")]
    public string EventType { get; set; } = string.Empty;
    
    [Display(Name = "Event Date")]
    public DateTime EventDate { get; set; }
    
    [Display(Name = "User")]
    public string? EventUser { get; set; }
    
    [Display(Name = "Description")]
    public string EventDescription { get; set; } = string.Empty;
    
    public int EventSequence { get; set; }
}
