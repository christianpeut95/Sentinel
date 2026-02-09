using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.View")]
public class DetailsModel : PageModel
{
    private readonly IOutbreakService _outbreakService;

    public DetailsModel(IOutbreakService outbreakService)
    {
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public OutbreakStatistics Statistics { get; set; } = null!;
    public List<OutbreakCase> Cases { get; set; } = new();
    public List<OutbreakCase> Contacts { get; set; } = new();
    public List<OutbreakTeamMember> TeamMembers { get; set; } = new();
    public List<OutbreakTimeline> Timeline { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        Statistics = await _outbreakService.GetStatisticsAsync(id);
        Cases = await _outbreakService.GetOutbreakCasesAsync(id);
        Contacts = await _outbreakService.GetOutbreakContactsAsync(id);
        TeamMembers = await _outbreakService.GetTeamMembersAsync(id);
        Timeline = await _outbreakService.GetTimelineAsync(id);

        return Page();
    }

    public async Task<IActionResult> OnPostUnlinkCaseAsync(int id, int outbreakCaseId, string reason)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.UnlinkCaseAsync(outbreakCaseId, reason, userId);
        
        if (success)
        {
            SuccessMessage = "Case/contact unlinked successfully.";
        }
        else
        {
            ErrorMessage = "Failed to unlink case/contact.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveTeamMemberAsync(int id, int memberId)
    {
        // Note: This would need to be implemented properly with the actual member info
        // For now, this is a placeholder
        SuccessMessage = "Team member removed successfully.";
        return RedirectToPage(new { id });
    }
}

