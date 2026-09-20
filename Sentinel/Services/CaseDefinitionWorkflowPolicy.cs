using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services;

/// <summary>
/// Defines the permitted lifecycle transitions for a configured case definition.
/// Published definitions are historical configuration records; users must create a
/// new draft rather than reactivate or edit a retired version through a status action.
/// </summary>
public static class CaseDefinitionWorkflowPolicy
{
    public static bool CanActivate(CaseDefinitionStatus status) =>
        status == CaseDefinitionStatus.Draft;

    public static bool CanSaveDraft(CaseDefinitionStatus status) =>
        status == CaseDefinitionStatus.Draft;

    public static bool CanArchive(CaseDefinitionStatus status) =>
        status is CaseDefinitionStatus.Draft or CaseDefinitionStatus.Current;
}
