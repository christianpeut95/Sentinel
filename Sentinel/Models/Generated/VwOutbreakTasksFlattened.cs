using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class VwOutbreakTasksFlattened
{
    public int OutbreakId { get; set; }

    public string OutbreakName { get; set; } = null!;

    public string? OutbreakReferenceNumber { get; set; }

    public string DiseaseName { get; set; } = null!;

    public Guid? CaseGuid { get; set; }

    public string? CaseNumber { get; set; }

    public string PatientName { get; set; } = null!;

    public Guid? TaskId { get; set; }

    public string? TaskTitle { get; set; }

    public string? TaskType { get; set; }

    public string TaskStatus { get; set; } = null!;

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string AssignedToName { get; set; } = null!;

    public string? AssignedToEmail { get; set; }

    public DateTime OutbreakCreatedAt { get; set; }
}
