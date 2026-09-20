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

        [BindProperty]
        public Guid Id { get; set; }

        [BindProperty]
        public string? ReturnUrl { get; set; }

        // Destructive actions must never be performed through a GET request.
        // This route is retained for backwards compatibility with the settings
        // workflow. Razor Pages validates antiforgery tokens for POST requests
        // by default; this PageModel deliberately does not opt out.
        public IActionResult OnGet()
        {
            return RedirectToPage("/Settings/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                await _mappingService.DeleteMappingAsync(Id);
                TempData["SuccessMessage"] = "Mapping deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Settings/Index");
        }
    }
}
