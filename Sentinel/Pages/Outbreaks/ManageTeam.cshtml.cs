using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class ManageTeamModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public ManageTeamModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<OutbreakTeamMember> TeamMembers { get; set; } = new();
    public List<ApplicationUser> AvailableUsers { get; set; } = new();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    [BindProperty]
    public OutbreakRole SelectedRole { get; set; }

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
        TeamMembers = await _outbreakService.GetTeamMembersAsync(id);

        // Get users not already on the team
        var teamUserIds = TeamMembers.Select(tm => tm.UserId).ToList();
        AvailableUsers = await _context.Users
            .Where(u => !teamUserIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAddMemberAsync(int id)
    {
        if (string.IsNullOrEmpty(SelectedUserId))
        {
            ErrorMessage = "Please select a user to add.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.AddTeamMemberAsync(id, SelectedUserId, SelectedRole, userId);
        
        if (success)
        {
            SuccessMessage = "Team member added successfully.";
        }
        else
        {
            ErrorMessage = "User is already on the team or could not be added.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(int id, int memberId)
    {
        var member = await _context.OutbreakTeamMembers.FindAsync(memberId);
        if (member == null)
        {
            ErrorMessage = "Team member not found.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.RemoveTeamMemberAsync(id, member.UserId, userId);
        
        if (success)
        {
            SuccessMessage = "Team member removed successfully.";
        }
        else
        {
            ErrorMessage = "Failed to remove team member.";
        }

        return RedirectToPage(new { id });
    }
}
