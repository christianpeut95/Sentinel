using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Flattened view for outbreak hierarchy with cases and tasks
/// Includes recursive sub-outbreaks (max 5 levels)
/// One row per task
/// </summary>
[Keyless]
public class OutbreakTaskFlattened
{
    // Outbreak hierarchy
    [Display(Name = "Outbreak Number")]
    public string OutbreakNumber { get; set; } = string.Empty;
    
    [Display(Name = "Outbreak Name")]
    public string OutbreakName { get; set; } = string.Empty;
    
    [Display(Name = "Level")]
    public int OutbreakLevel { get; set; }
    
    [Display(Name = "Hierarchy Path")]
    public string HierarchyPath { get; set; } = string.Empty;
    
    public int OutbreakTypeEnum { get; set; }
    
    [Display(Name = "Outbreak Type")]
    public string OutbreakType { get; set; } = string.Empty;
    
    public int OutbreakStatusEnum { get; set; }
    
    [Display(Name = "Outbreak Status")]
    public string OutbreakStatus { get; set; } = string.Empty;
    
    [Display(Name = "Start Date")]
    public DateTime OutbreakStartDate { get; set; }
    
    [Display(Name = "End Date")]
    public DateTime? OutbreakEndDate { get; set; }
    
    [Display(Name = "Confirmation Status")]
    public string? OutbreakConfirmationStatus { get; set; }
    
    [Display(Name = "Primary Disease")]
    public string? PrimaryDisease { get; set; }
    
    [Display(Name = "Primary Location")]
    public string? PrimaryLocation { get; set; }
    
    [Display(Name = "Primary Event")]
    public string? PrimaryEvent { get; set; }
    
    [Display(Name = "Lead Investigator")]
    public string? LeadInvestigator { get; set; }
    
    [Display(Name = "Lead Email")]
    public string? LeadInvestigatorEmail { get; set; }
    
    // Case/Contact
    public Guid CaseId { get; set; }
    
    [Display(Name = "Case Number")]
    public string CaseNumber { get; set; } = string.Empty;
    
    [Display(Name = "Case Type")]
    public string CaseType { get; set; } = string.Empty;
    
    [Display(Name = "Date of Onset")]
    public DateTime? DateOfOnset { get; set; }
    
    [Display(Name = "Date of Notification")]
    public DateTime? DateOfNotification { get; set; }
    
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; } = string.Empty;
    
    [Display(Name = "Suburb")]
    public string? PatientSuburb { get; set; }
    
    [Display(Name = "State")]
    public string? PatientState { get; set; }
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Case Status")]
    public string? CaseStatus { get; set; }
    
    [Display(Name = "Jurisdiction")]
    public string? Jurisdiction1 { get; set; }
    
    // Task (ONE ROW PER TASK)
    public Guid? TaskId { get; set; }
    
    [Display(Name = "Task Number")]
    public string? TaskNumber { get; set; }
    
    [Display(Name = "Task Title")]
    public string? TaskTitle { get; set; }
    
    [Display(Name = "Description")]
    public string? TaskDescription { get; set; }
    
    [Display(Name = "Status")]
    public string? TaskStatus { get; set; }
    
    [Display(Name = "Priority")]
    public string? TaskPriority { get; set; }
    
    [Display(Name = "Due Date")]
    public DateTime? TaskDueDate { get; set; }
    
    [Display(Name = "Completed")]
    public DateTime? TaskCompletedAt { get; set; }
    
    [Display(Name = "Is Interview")]
    public bool? IsInterviewTask { get; set; }
    
    [Display(Name = "Task Type")]
    public string? TaskType { get; set; }
    
    [Display(Name = "Assigned To")]
    public string? AssignedToEmail { get; set; }
    
    [Display(Name = "Assigned To Name")]
    public string? AssignedToName { get; set; }
    
    // Calculated
    [Display(Name = "Days Into Outbreak")]
    public int? DaysIntoOutbreak { get; set; }
    
    [Display(Name = "Days Until Due")]
    public int? DaysUntilTaskDue { get; set; }
}
