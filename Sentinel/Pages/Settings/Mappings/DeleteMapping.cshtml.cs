using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Mappings
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class DeleteMappingModel : PageModel
    {
        private readonly ISurveyMappingService _mappingService;

        public DeleteMappingModel(ISurveyMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await _mappingService.DeleteMappingAsync(Id);
                TempData["SuccessMessage"] = "Mapping deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting mapping: {ex.Message}";
            }

            if (!string.IsNullOrEmpty(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return RedirectToPage("/Settings/Index");
        }
    }
}
