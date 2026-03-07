using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Simple, fast contact list for Contacts Index page
/// Ordered by CreatedAt DESC (most recent first)
/// Includes exposure source and task metrics
/// </summary>
[Keyless]
public class ContactListSimple
{
    public Guid ContactId { get; set; }
    
    [Display(Name = "Contact Number")]
    public string ContactNumber { get; set; } = string.Empty;
    
    [Display(Name = "Date Identified")]
    public DateTime DateIdentified { get; set; }
    
    [Display(Name = "Date of Onset")]
    public DateTime? ContactDateOfOnset { get; set; }
    
    // Patient
    [Display(Name = "Patient ID")]
    public string? PatientId { get; set; }
    
    [Display(Name = "Name")]
    public string ContactName { get; set; } = string.Empty;
    
    [Display(Name = "First Name")]
    public string ContactFirstName { get; set; } = string.Empty;
    
    [Display(Name = "Last Name")]
    public string ContactLastName { get; set; } = string.Empty;
    
    [Display(Name = "DOB")]
    public DateTime? ContactDOB { get; set; }
    
    [Display(Name = "Mobile")]
    public string? ContactMobile { get; set; }
    
    [Display(Name = "Email")]
    public string? ContactEmail { get; set; }
    
    [Display(Name = "Suburb")]
    public string? ContactSuburb { get; set; }
    
    [Display(Name = "State")]
    public string? ContactState { get; set; }
    
    [Display(Name = "Disease")]
    public string? ContactDisease { get; set; }
    
    [Display(Name = "Status")]
    public string? ContactStatus { get; set; }
    
    // Source case
    [Display(Name = "Exposed By Case")]
    public string? ExposedByCase { get; set; }
    
    [Display(Name = "Exposed By Name")]
    public string? ExposedByName { get; set; }
    
    [Display(Name = "Exposed By Disease")]
    public string? ExposedByDisease { get; set; }
    
    // Exposure
    public int? ExposureTypeEnum { get; set; }
    
    [Display(Name = "Exposure Type")]
    public string? ExposureType { get; set; }
    
    [Display(Name = "Exposure Date")]
    public DateTime? ExposureDate { get; set; }
    
    [Display(Name = "Exposure End")]
    public DateTime? ExposureEndDate { get; set; }
    
    [Display(Name = "Exposure Setting")]
    public string? ExposureSetting { get; set; }
    
    [Display(Name = "Event Name")]
    public string? EventName { get; set; }
    
    [Display(Name = "Event Type")]
    public string? EventType { get; set; }
    
    [Display(Name = "Location Name")]
    public string? LocationName { get; set; }
    
    [Display(Name = "Location Type")]
    public string? LocationType { get; set; }
    
    [Display(Name = "Contact Classification")]
    public string? ContactClassification { get; set; }
    
    [Display(Name = "Jurisdiction")]
    public string? Jurisdiction1 { get; set; }
    
    // Task metrics
    [Display(Name = "Total Tasks")]
    public int TotalTasks { get; set; }
    
    [Display(Name = "Completed Tasks")]
    public int CompletedTasks { get; set; }
    
    [Display(Name = "Interview Tasks")]
    public int InterviewTasks { get; set; }
    
    [Display(Name = "Next Task Due")]
    public DateTime? NextTaskDueDate { get; set; }
    
    [Display(Name = "Follow-up Status")]
    public string FollowUpStatus { get; set; } = string.Empty;
}
