using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings;

[Authorize(Policy = "Permission.Settings.ManagePermissions")]
public class PatientAccessModel : PageModel
{
    private readonly ISystemSettingsService _settingsService;
    private readonly ILogger<PatientAccessModel> _logger;

    public PatientAccessModel(
        ISystemSettingsService settingsService,
        ILogger<PatientAccessModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    [BindProperty]
    public bool CaseScopedPatientAccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings == null)
        {
            return NotFound();
        }

        CaseScopedPatientAccess = settings.CaseScopedPatientAccess;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings == null)
        {
            return NotFound();
        }

        settings.CaseScopedPatientAccess = CaseScopedPatientAccess;
        settings.ModifiedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _settingsService.UpdateSettingsAsync(settings);

        _logger.LogInformation(
            "Patient access scope changed to {CaseScopedPatientAccess} by {UserId}",
            CaseScopedPatientAccess,
            settings.ModifiedByUserId);

        TempData["SuccessMessage"] = "Patient access scope saved.";
        return RedirectToPage();
    }
}
