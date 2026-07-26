using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TaskCallAttempt
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public string AttemptedByUserId { get; set; } = null!;

    public DateTime AttemptedAt { get; set; }

    public int Outcome { get; set; }

    public string? Notes { get; set; }

    public int? DurationSeconds { get; set; }

    public DateTime? NextCallbackScheduled { get; set; }

    public string? PhoneNumberCalled { get; set; }

    public virtual AspNetUser AttemptedByUser { get; set; } = null!;

    public virtual CaseTask Task { get; set; } = null!;
}
