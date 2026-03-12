using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Timeline view showing all events for a case
/// Matches vw_CaseTimelineAll SQL view
/// </summary>
[Keyless]
public class CaseTimelineEvent
{
    public Guid CaseId { get; set; }
    
    [Display(Name = "Event Type")]
    public string? EventType { get; set; }
    
    [Display(Name = "Event Date")]
    public DateTime EventDate { get; set; }
    
    [Display(Name = "Description")]
    public string? EventDescription { get; set; }
    
    [Display(Name = "Actor")]
    public string? ActorName { get; set; }
    
    [Display(Name = "Sort Date")]
    public DateTime SortDate { get; set; }
}
