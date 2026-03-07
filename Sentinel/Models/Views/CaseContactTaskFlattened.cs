using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Flattened view showing Case ? Exposure ? Contact ? Tasks (one row per task)
/// Includes recursive transmission chains (max 10 generations)
/// Supports Event, Location, and Contact exposures
/// </summary>
[Keyless]
public class CaseContactTaskFlattened
{
    // Transmission chain
    public Guid CaseGuid { get; set; }
    
    [Display(Name = "Case Number")]
    public string CaseNumber { get; set; } = string.Empty;
    
    [Display(Name = "Generation")]
    public int GenerationNumber { get; set; }
    
    [Display(Name = "Transmission Path")]
    public string TransmissionChainPath { get; set; } = string.Empty;
    
    [Display(Name = "Transmitted By")]
    public string? TransmittedByCase { get; set; }
    
    // Case details
    public int CaseTypeEnum { get; set; }
    
    [Display(Name = "Case Type")]
    public string CaseType { get; set; } = string.Empty;
    
    [Display(Name = "Date of Onset")]
    public DateTime? DateOfOnset { get; set; }
    
    [Display(Name = "Date of Notification")]
    public DateTime? DateOfNotification { get; set; }
    
    [Display(Name = "Case Status")]
    public string? CaseStatus { get; set; }
    
    // Patient
    [Display(Name = "Patient ID")]
    public string? PatientId { get; set; }
    
    [Display(Name = "Patient Name")]
    public string PatientName { get; set; } = string.Empty;
    
    [Display(Name = "First Name")]
    public string PatientFirstName { get; set; } = string.Empty;
    
    [Display(Name = "Last Name")]
    public string PatientLastName { get; set; } = string.Empty;
    
    [Display(Name = "DOB")]
    public DateTime? PatientDOB { get; set; }
    
    [Display(Name = "Age at Onset")]
    public int? AgeAtOnset { get; set; }
    
    [Display(Name = "Suburb")]
    public string? PatientSuburb { get; set; }
    
    [Display(Name = "State")]
    public string? PatientState { get; set; }
    
    [Display(Name = "Mobile")]
    public string? PatientMobile { get; set; }
    
    [Display(Name = "Email")]
    public string? PatientEmail { get; set; }
    
    // Disease
    [Display(Name = "Disease")]
    public string? DiseaseName { get; set; }
    
    [Display(Name = "Disease Code")]
    public string? DiseaseCode { get; set; }
    
    // Jurisdiction
    [Display(Name = "Jurisdiction 1")]
    public string? Jurisdiction1 { get; set; }
    
    [Display(Name = "Jurisdiction 2")]
    public string? Jurisdiction2 { get; set; }
    
    [Display(Name = "Jurisdiction 3")]
    public string? Jurisdiction3 { get; set; }
    
    // Exposure details
    public Guid? ExposureEventId { get; set; }
    
    [Display(Name = "Exposure Type")]
    public string? ExposureType { get; set; }
    
    [Display(Name = "Exposure Status")]
    public string? ExposureStatusDisplay { get; set; }
    
    [Display(Name = "Exposure Start")]
    public DateTime? ExposureStartDate { get; set; }
    
    [Display(Name = "Exposure End")]
    public DateTime? ExposureEndDate { get; set; }
    
    [Display(Name = "Exposure Details")]
    public string? ExposureDescription { get; set; }
    
    [Display(Name = "Confidence")]
    public string? ConfidenceLevel { get; set; }
    
    [Display(Name = "Contact Classification")]
    public string? ContactClassification { get; set; }
    
    // Event (if Event exposure)
    public Guid? EventId { get; set; }
    
    [Display(Name = "Event Name")]
    public string? EventName { get; set; }
    
    [Display(Name = "Event Type")]
    public string? EventType { get; set; }
    
    [Display(Name = "Event Start")]
    public DateTime? EventStartDate { get; set; }
    
    [Display(Name = "Event End")]
    public DateTime? EventEndDate { get; set; }
    
    [Display(Name = "Attendees")]
    public int? EstimatedAttendees { get; set; }
    
    [Display(Name = "Setting")]
    public string? EventSetting { get; set; }
    
    [Display(Name = "Organizer")]
    public string? EventOrganizer { get; set; }
    
    // Location (if Location exposure or Event location)
    public Guid? LocationId { get; set; }
    
    [Display(Name = "Location Name")]
    public string? LocationName { get; set; }
    
    [Display(Name = "Location Type")]
    public string? LocationType { get; set; }
    
    [Display(Name = "Location Address")]
    public string? LocationAddress { get; set; }
    
    [Display(Name = "High Risk")]
    public string? LocationIsHighRisk { get; set; }
    
    [Display(Name = "Location Org")]
    public string? LocationOrganization { get; set; }
    
    // Task details (ONE ROW PER TASK)
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
    
    [Display(Name = "Created")]
    public DateTime? TaskCreatedAt { get; set; }
    
    [Display(Name = "Completed")]
    public DateTime? TaskCompletedAt { get; set; }
    
    [Display(Name = "Cancelled")]
    public DateTime? TaskCancelledAt { get; set; }
    
    [Display(Name = "Is Interview")]
    public bool? IsInterviewTask { get; set; }
    
    [Display(Name = "Task Type")]
    public string? TaskType { get; set; }
    
    [Display(Name = "Assignment Type")]
    public string? AssignmentType { get; set; }
    
    [Display(Name = "Assigned To")]
    public string? AssignedToEmail { get; set; }
    
    [Display(Name = "Assigned To Name")]
    public string? AssignedToName { get; set; }
    
    [Display(Name = "Survey Status")]
    public string? SurveyStatus { get; set; }
    
    // Calculated
    [Display(Name = "Incubation Days")]
    public int? IncubationPeriodDays { get; set; }
    
    [Display(Name = "Days Until Due")]
    public int? DaysUntilTaskDue { get; set; }
    
    [Display(Name = "Task Age (Days)")]
    public int? TaskAgeDays { get; set; }
    
    [Display(Name = "Due Status")]
    public string? TaskDueStatus { get; set; }
}
