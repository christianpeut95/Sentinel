using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Flattened view for outbreak hierarchy with cases and tasks
/// Matches vw_OutbreakTasksFlattened SQL view
/// </summary>
[Keyless]
public class OutbreakTaskFlattened
{
    [Display(Name = "Outbreak ID")]
    public int OutbreakId { get; set; }
    
    [Display(Name = "Outbreak Name")]
    public string? OutbreakName { get; set; }
    
    [Display(Name = "Outbreak Reference")]
    public string? OutbreakReferenceNumber { get; set; }
    
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Case ID")]
    public Guid? CaseGuid { get; set; }
    
    [Display(Name = "Case Number")]
    public string? CaseNumber { get; set; }
    
    [Display(Name = "Patient Name")]
    public string? PatientName { get; set; }
    
    [Display(Name = "Task ID")]
    public Guid? TaskId { get; set; }
    
    [Display(Name = "Task Title")]
    public string? TaskTitle { get; set; }
    
    [Display(Name = "Task Type")]
    public string? TaskType { get; set; }
    
    [Display(Name = "Task Status")]
    public string? TaskStatus { get; set; }
    
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }
    
    [Display(Name = "Completed Date")]
    public DateTime? CompletedDate { get; set; }
    
    [Display(Name = "Assigned To")]
    public string? AssignedToName { get; set; }
    
    [Display(Name = "Assigned Email")]
    public string? AssignedToEmail { get; set; }
    
    [Display(Name = "Created At")]
    public DateTime OutbreakCreatedAt { get; set; }
}
