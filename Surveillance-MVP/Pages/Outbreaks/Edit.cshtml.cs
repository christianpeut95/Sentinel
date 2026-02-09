using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public EditModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    [BindProperty]
    public Outbreak Outbreak { get; set; } = new();

    public SelectList Diseases { get; set; } = null!;
    public SelectList Locations { get; set; } = null!;
    public SelectList Events { get; set; } = null!;
    public SelectList Users { get; set; } = null!;
    public SelectList Statuses { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        await LoadSelectListsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.UpdateAsync(Outbreak, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "Outbreak updated successfully.";
            return RedirectToPage("Details", new { id = Outbreak.Id });
        }

        ModelState.AddModelError(string.Empty, "Failed to update outbreak.");
        await LoadSelectListsAsync();
        return Page();
    }

    private async Task LoadSelectListsAsync()
    {
        Diseases = new SelectList(
            await _context.Diseases.OrderBy(d => d.Name).ToListAsync(),
            "Id",
            "Name");

        Locations = new SelectList(
            await _context.Locations.OrderBy(l => l.Name).ToListAsync(),
            "Id",
            "Name");

        Events = new SelectList(
            await _context.Events.OrderBy(e => e.Name).ToListAsync(),
            "Id",
            "Name");

        Users = new SelectList(
            await _context.Users.OrderBy(u => u.UserName).ToListAsync(),
            "Id",
            "UserName");

        Statuses = new SelectList(Enum.GetValues(typeof(OutbreakStatus)).Cast<OutbreakStatus>());
    }
}
