using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseReinfectionRule
{
    public Guid Id { get; set; }

    public Guid DiseaseId { get; set; }

    public int RuleType { get; set; }

    public int? ReinfectionWindowDays { get; set; }

    public string? Description { get; set; }

    public bool RequireConfirmationForNewCase { get; set; }

    public string? NotificationMessage { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Disease Disease { get; set; } = null!;
}
