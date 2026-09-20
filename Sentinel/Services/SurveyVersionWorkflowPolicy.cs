using Sentinel.Models;

namespace Sentinel.Services;

/// <summary>
/// Defines the survey-version publication state machine. Published and
/// archived versions are historical records: a change to either must be made
/// by creating a new draft version, rather than reusing a previous record.
/// </summary>
public static class SurveyVersionWorkflowPolicy
{
    public static bool CanPublish(SurveyVersionStatus status) =>
        status == SurveyVersionStatus.Draft;

    public static bool CanArchive(SurveyVersionStatus status) =>
        status == SurveyVersionStatus.Draft;
}
