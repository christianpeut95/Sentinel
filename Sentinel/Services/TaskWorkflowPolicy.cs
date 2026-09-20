using Sentinel.Models;

namespace Sentinel.Services;

/// <summary>
/// Defines the terminal boundary for case-task workflows. A completed or
/// cancelled task must not be reopened or altered through a normal edit/start,
/// complete, or cancel action. Normal status edits must also not *create* a
/// terminal state: completion and cancellation have dedicated handlers which
/// record their actor, timestamp, reason/notes and audit trail. A dedicated
/// audited reopening workflow would be required if that becomes a supported
/// business operation.
/// </summary>
public static class TaskWorkflowPolicy
{
    public static bool IsTerminal(CaseTaskStatus status) =>
        status is CaseTaskStatus.Completed or CaseTaskStatus.Cancelled;

    /// <summary>
    /// Determines whether the ordinary task-edit workflow may set a status.
    /// Terminal transitions are deliberately excluded: callers must use the
    /// dedicated completion or cancellation workflow instead.
    /// </summary>
    public static bool CanChangeStatus(CaseTaskStatus current, CaseTaskStatus requested) =>
        Enum.IsDefined(requested) &&
        !IsTerminal(current) &&
        !IsTerminal(requested);
}
