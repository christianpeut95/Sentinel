using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakTasksFlattened
{
    public string OutbreakNumber { get; set; } = null!;

    public string OutbreakName { get; set; } = null!;

    public int OutbreakLevel { get; set; }

    public string HierarchyPath { get; set; } = null!;

    public int OutbreakTypeEnum { get; set; }

    public string OutbreakType { get; set; } = null!;

    public int OutbreakStatusEnum { get; set; }

    public string OutbreakStatus { get; set; } = null!;

    public DateTime OutbreakStartDate { get; set; }

    public DateTime? OutbreakEndDate { get; set; }

    public string? OutbreakConfirmationStatus { get; set; }

    public string? PrimaryDisease { get; set; }

    public string? PrimaryLocation { get; set; }

    public string? PrimaryEvent { get; set; }

    public string? LeadInvestigator { get; set; }

    public string? LeadInvestigatorEmail { get; set; }

    public Guid CaseId { get; set; }

    public string CaseNumber { get; set; } = null!;

    public string CaseType { get; set; } = null!;

    public DateTime? DateOfOnset { get; set; }

    public DateTime? DateOfNotification { get; set; }

    public string PatientName { get; set; } = null!;

    public string? PatientSuburb { get; set; }

    public string? PatientState { get; set; }

    public string? DiseaseName { get; set; }

    public string? CaseStatus { get; set; }

    public string? Jurisdiction1 { get; set; }

    public Guid? TaskId { get; set; }

    public string? TaskNumber { get; set; }

    public string? TaskTitle { get; set; }

    public string? TaskDescription { get; set; }

    public string? TaskStatus { get; set; }

    public string? TaskPriority { get; set; }

    public DateTime? TaskDueDate { get; set; }

    public DateTime? TaskCompletedAt { get; set; }

    public bool? IsInterviewTask { get; set; }

    public string? TaskType { get; set; }

    public string? AssignedToEmail { get; set; }

    public string? AssignedToName { get; set; }

    public int? DaysIntoOutbreak { get; set; }

    public int? DaysUntilTaskDue { get; set; }
}
