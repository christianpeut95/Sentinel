using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    [Authorize]
    public class SelectMergeModel : PageModel
    {
        private readonly IPatientMergeService _mergeService;

        public SelectMergeModel(IPatientMergeService mergeService)
        {
            _mergeService = mergeService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? PatientId { get; set; }

        [BindProperty]
        public Guid? SourcePatientId { get; set; }

        [BindProperty]
        public Guid? TargetPatientId { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            if (PatientId.HasValue)
            {
                SourcePatientId = PatientId.Value;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!SourcePatientId.HasValue || !TargetPatientId.HasValue)
            {
                ErrorMessage = "Please select both source and target patients.";
                return Page();
            }

            if (SourcePatientId.Value == TargetPatientId.Value)
            {
                ErrorMessage = "Cannot merge a patient with itself. Please select different patients.";
                return Page();
            }

            var isValid = await _mergeService.ValidateMergeAsync(SourcePatientId.Value, TargetPatientId.Value);
            if (!isValid)
            {
                ErrorMessage = "One or both selected patients do not exist.";
                return Page();
            }

            return RedirectToPage("./Merge", new { sourceId = SourcePatientId, targetId = TargetPatientId });
        }
    }
}
