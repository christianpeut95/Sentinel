using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Create")]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public CreateModel(ApplicationDbContext context, IOutbreakService outbreakService)
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

    public async Task OnGetAsync()
    {
        await LoadSelectListsAsync();
        Outbreak.StartDate = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        await _outbreakService.CreateAsync(Outbreak, userId);

        if (!string.IsNullOrEmpty(Outbreak.LeadInvestigatorId))
        {
            await _outbreakService.AddTeamMemberAsync(
                Outbreak.Id, 
                Outbreak.LeadInvestigatorId, 
                OutbreakRole.LeadInvestigator, 
                userId);
        }

        TempData["SuccessMessage"] = $"Outbreak '{Outbreak.Name}' has been declared.";
        return RedirectToPage("Details", new { id = Outbreak.Id });
    }

    private async Task LoadSelectListsAsync()
    {
        Diseases = new SelectList(
            await _context.Diseases.OrderBy(d => d.Name).ToListAsync(),
            nameof(Disease.Id),
            nameof(Disease.Name));

        Locations = new SelectList(
            await _context.Locations.OrderBy(l => l.Name).ToListAsync(),
            nameof(Location.Id),
            nameof(Location.Name));

        Events = new SelectList(
            await _context.Events.OrderBy(e => e.Name).ToListAsync(),
            nameof(Event.Id),
            nameof(Event.Name));

        Users = new SelectList(
            await _context.Users.OrderBy(u => u.UserName).ToListAsync(),
            nameof(ApplicationUser.Id),
            nameof(ApplicationUser.UserName));
    }
}
