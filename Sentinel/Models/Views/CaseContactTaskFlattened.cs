using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Views;

/// <summary>
/// Comprehensive flattened view matching actual database vw_CaseContactTasksFlattened
/// 67 columns total - includes full case, exposure, event, location, and task details
/// </summary>
[Keyless]
public class CaseContactTaskFlattened
{
    // Case identification
    public Guid CaseGuid { get; set; }
    public string? CaseNumber { get; set; }
    public int GenerationNumber { get; set; }
    public string? TransmissionChainPath { get; set; }
    public string? TransmittedByCase { get; set; }
    public int CaseTypeEnum { get; set; }
    public string? CaseType { get; set; }
    
    // Case dates
    public DateTime? DateOfOnset { get; set; }
    public DateTime? DateOfNotification { get; set; }
    public string? CaseStatus { get; set; }
    
    // Patient
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string? PatientFirstName { get; set; }
    public string? PatientLastName { get; set; }
    public DateTime? PatientDOB { get; set; }
    public int? AgeAtOnset { get; set; }
    public string? PatientSuburb { get; set; }
    public string? PatientState { get; set; }
    public string? PatientMobile { get; set; }
    public string? PatientEmail { get; set; }
    
    // Disease
    public string? DiseaseName { get; set; }
    public string? DiseaseCode { get; set; }
    
    // Jurisdiction
    public string? Jurisdiction1 { get; set; }
    public string? Jurisdiction2 { get; set; }
    public string? Jurisdiction3 { get; set; }
    
    // Exposure
    public Guid? ExposureEventId { get; set; }
    public string? ExposureType { get; set; }
    public string? ExposureStatusDisplay { get; set; }
    public DateTime? ExposureStartDate { get; set; }
    public DateTime? ExposureEndDate { get; set; }
    public string? ExposureDescription { get; set; }
    public string? ConfidenceLevel { get; set; }
    public string? ContactClassification { get; set; }
    
    // Event (if Event exposure)
    public Guid? EventId { get; set; }
    public string? EventName { get; set; }
    public string? EventType { get; set; }
    public DateTime? EventStartDate { get; set; }
    public DateTime? EventEndDate { get; set; }
    public int? EstimatedAttendees { get; set; }
    public string? EventSetting { get; set; }
    public string? EventOrganizer { get; set; }
    
    // Location (if Location exposure or Event location)
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? LocationType { get; set; }
    public string? LocationAddress { get; set; }
    public string? LocationIsHighRisk { get; set; }
    public string? LocationOrganization { get; set; }
    
    // Task details
    public Guid? TaskId { get; set; }
    public string? TaskNumber { get; set; }
    public string? TaskTitle { get; set; }
    public string? TaskDescription { get; set; }
    public string? TaskStatus { get; set; }
    public string? TaskPriority { get; set; }
    public DateTime? TaskDueDate { get; set; }
    public DateTime? TaskCreatedAt { get; set; }
    public DateTime? TaskCompletedAt { get; set; }
    public DateTime? TaskCancelledAt { get; set; }
    public bool? IsInterviewTask { get; set; }
    public string? TaskType { get; set; }
    
    // Task assignment
    public string? AssignmentType { get; set; }
    public string? AssignedToEmail { get; set; }
    public string? AssignedToName { get; set; }
    
    // Survey
    public string? SurveyStatus { get; set; }
    
    // Calculated fields
    public int? IncubationPeriodDays { get; set; }
    public int? DaysUntilTaskDue { get; set; }
    public int? TaskAgeDays { get; set; }
    public string? TaskDueStatus { get; set; }
    
    // Case audit
    public DateTime CaseCreatedAt { get; set; }
    public DateTime CaseUpdatedAt { get; set; }
}
