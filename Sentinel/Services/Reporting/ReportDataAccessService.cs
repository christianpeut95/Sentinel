using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Enforces the underlying entity permissions needed to extract report data.
/// This is deliberately separate from Report.View/Edit/Create: those permissions allow a user
/// to use reporting features, not to read every entity that can be reported on.
/// </summary>
public sealed class ReportDataAccessService : IReportDataAccessService
{
    private static readonly IReadOnlyDictionary<string, string[]> EntityPermissionPolicies =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Case"] = ["Permission.Case.View"],
            // Contacts are stored as case records and use the Case permission model.
            ["Contact"] = ["Permission.Case.View"],
            ["Patient"] = ["Permission.Patient.View"],
            ["Outbreak"] = ["Permission.Outbreak.View"],
            ["Task"] = ["Permission.Task.View", "Permission.Case.View"],
            ["Location"] = ["Permission.Location.View"],
            ["Event"] = ["Permission.Event.View"],

            // SQL views include case/patient data but cannot participate in EF global filters.
            ["CaseContactTasksFlattened"] = ["Permission.Case.View", "Permission.Task.View"],
            ["OutbreakTasksFlattened"] = ["Permission.Outbreak.View", "Permission.Case.View", "Permission.Task.View"],
            ["CaseTimelineAll"] = ["Permission.Case.View"],
            ["ContactTracingMindMapNodes"] = ["Permission.Case.View", "Permission.Exposure.View"],
            ["ContactTracingMindMapEdges"] = ["Permission.Case.View", "Permission.Exposure.View"],
            ["ContactsListSimple"] = ["Permission.Case.View"]
        };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    public ReportDataAccessService(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    public async Task<bool> CanReadEntityTypeAsync(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType) ||
            !EntityPermissionPolicies.TryGetValue(entityType, out var policies))
        {
            return false;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true ||
            string.IsNullOrWhiteSpace(principal.FindFirstValue(ClaimTypes.NameIdentifier)))
        {
            return false;
        }

        foreach (var policy in policies)
        {
            if (!(await _authorizationService.AuthorizeAsync(principal, null, policy)).Succeeded)
            {
                return false;
            }
        }

        return true;
    }
}
