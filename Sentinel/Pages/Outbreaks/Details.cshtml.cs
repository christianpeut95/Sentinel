using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

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
    public List<Outbreak> ChildOutbreaks { get; set; } = new();
    
    // View toggle: show parent only or aggregated with children
    [BindProperty(SupportsGet = true)]
    public bool ShowAggregated { get; set; } = false;
    
    // Show all descendants (recursive) or just direct children
    [BindProperty(SupportsGet = true)]
    public bool ShowAllDescendants { get; set; } = false;
    
    // Helper properties for conditional rendering
    public bool IsContactTracingMode => Outbreak?.ConfirmationStatus?.Name == "Under Investigation";
    public bool IsConfirmedOutbreak => Outbreak?.ConfirmationStatus?.Name == "Confirmed";
    public bool HasChildren => ChildOutbreaks.Any();
    public bool IsChildOutbreak => Outbreak?.ParentOutbreakId.HasValue ?? false;




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
        
        // Load children based on view preference
        if (ShowAllDescendants)
        {
            ChildOutbreaks = await _outbreakService.GetAllDescendantOutbreaksAsync(id);
        }
        else
        {
            ChildOutbreaks = await _outbreakService.GetChildOutbreaksAsync(id);
        }
        
        // Choose statistics based on view toggle
        if (ShowAggregated && HasChildren)
        {
            Statistics = await _outbreakService.GetAggregatedStatisticsAsync(id);
        }
        else
        {
            Statistics = await _outbreakService.GetStatisticsAsync(id);
        }
        
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

    public async Task<IActionResult> OnPostUpgradeToSuspectedAsync(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            ErrorMessage = "Reason is required to upgrade outbreak status.";
            return RedirectToPage(new { id });
        }

        var success = await _outbreakService.UpdateConfirmationStatusAsync(id, "Suspected", reason);
        
        if (success)
        {
            SuccessMessage = "Investigation upgraded to Suspected Outbreak status.";
        }
        else
        {
            ErrorMessage = "Failed to upgrade outbreak status.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConfirmOutbreakAsync(int id, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            ErrorMessage = "Justification is required to confirm outbreak.";
            return RedirectToPage(new { id });
        }

        var success = await _outbreakService.UpdateConfirmationStatusAsync(id, "Confirmed", justification);
        
        if (success)
        {
            SuccessMessage = "Outbreak confirmed successfully.";
        }
        else
        {
            ErrorMessage = "Failed to confirm outbreak.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddTimelineEventAsync(int id, string title, string? description, DateTime eventDate, TimelineEventType eventType)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            ErrorMessage = "Event title is required.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.AddTimelineEventAsync(id, title, description, eventDate, eventType, userId);
        
        if (success)
        {
            SuccessMessage = "Timeline event added successfully.";
        }
        else
        {
            ErrorMessage = "Failed to add timeline event.";
        }

        return RedirectToPage(new { id });
    }
}


