using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ContactsListSimple
{
    public Guid ContactId { get; set; }

    public string ContactNumber { get; set; } = null!;

    public DateTime DateIdentified { get; set; }

    public DateTime? ContactDateOfOnset { get; set; }

    public string? PatientId { get; set; }

    public string ContactName { get; set; } = null!;

    public string ContactFirstName { get; set; } = null!;

    public string ContactLastName { get; set; } = null!;

    public DateTime? ContactDob { get; set; }

    public string? ContactMobile { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactSuburb { get; set; }

    public string? ContactState { get; set; }

    public string? ContactDisease { get; set; }

    public string? ContactStatus { get; set; }

    public string? ExposedByCase { get; set; }

    public string? ExposedByName { get; set; }

    public string? ExposedByDisease { get; set; }

    public int? ExposureTypeEnum { get; set; }

    public string? ExposureType { get; set; }

    public DateTime? ExposureDate { get; set; }

    public DateTime? ExposureEndDate { get; set; }

    public string? ExposureSetting { get; set; }

    public string? EventName { get; set; }

    public string? EventType { get; set; }

    public string? LocationName { get; set; }

    public string? LocationType { get; set; }

    public string? ContactClassification { get; set; }

    public string? Jurisdiction1 { get; set; }

    public int TotalTasks { get; set; }

    public int CompletedTasks { get; set; }

    public int InterviewTasks { get; set; }

    public DateTime? NextTaskDueDate { get; set; }

    public string FollowUpStatus { get; set; } = null!;
}
