using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Merge")]
    public class MergeModel : PageModel
    {
        private readonly IPatientMergeService _mergeService;

        public MergeModel(IPatientMergeService mergeService)
        {
            _mergeService = mergeService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid SourceId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid TargetId { get; set; }

        public PatientMergeComparison? Comparison { get; set; }

        [BindProperty]
        public Dictionary<string, string> SelectedValues { get; set; } = new();

        [BindProperty]
        public Dictionary<int, string> SelectedCustomFieldValues { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Comparison = await _mergeService.GetMergeComparisonAsync(SourceId, TargetId);
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var selection = new PatientMergeSelection();

                // Convert selected standard fields
                foreach (var kvp in SelectedValues)
                {
                    var parts = kvp.Value.Split(':');
                    if (parts.Length == 2)
                    {
                        var source = parts[0]; // "source" or "target"
                        var fieldName = kvp.Key;

                        // Reload comparison to get actual values
                        var comparison = await _mergeService.GetMergeComparisonAsync(SourceId, TargetId);
                        var patient = source == "source" ? comparison.SourcePatient : comparison.TargetPatient;

                        var property = typeof(Models.Patient).GetProperty(fieldName);
                        if (property != null)
                        {
                            var value = property.GetValue(patient);
                            selection.SelectedValues[fieldName] = value;
                        }
                    }
                }

                // Convert selected custom fields
                foreach (var kvp in SelectedCustomFieldValues)
                {
                    var fieldDefId = kvp.Key;
                    var parts = kvp.Value.Split(':');
                    if (parts.Length == 2)
                    {
                        var source = parts[0];
                        var comparison = await _mergeService.GetMergeComparisonAsync(SourceId, TargetId);
                        var customFields = source == "source" ? comparison.SourceCustomFields : comparison.TargetCustomFields;

                        if (customFields.TryGetValue(fieldDefId.ToString(), out var fieldValue))
                        {
                            selection.SelectedCustomFieldValues[fieldDefId] = fieldValue.RawValue;
                        }
                    }
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _mergeService.MergePatientsAsync(SourceId, TargetId, selection, userId, ipAddress);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Successfully merged patient {SourceId} into patient {TargetId}.";
                    return RedirectToPage("./Details", new { id = result.MergedPatientId });
                }
                else
                {
                    ErrorMessage = result.ErrorMessage;
                    Comparison = await _mergeService.GetMergeComparisonAsync(SourceId, TargetId);
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Comparison = await _mergeService.GetMergeComparisonAsync(SourceId, TargetId);
                return Page();
            }
        }
    }
}
