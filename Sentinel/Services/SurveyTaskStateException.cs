using Sentinel.Models;

namespace Sentinel.Services;

/// <summary>
/// Raised when a survey operation targets a task that is in a terminal state.
/// Keeping this rule in the service prevents a future UI or API entry point
/// from reopening or completing an already completed/cancelled workflow.
/// </summary>
public sealed class SurveyTaskStateException : InvalidOperationException
{
    public SurveyTaskStateException(CaseTaskStatus status)
        : base(status == CaseTaskStatus.Completed
            ? "This task is already completed."
            : "This task has been cancelled.")
    {
        Status = status;
    }

    public CaseTaskStatus Status { get; }
}
