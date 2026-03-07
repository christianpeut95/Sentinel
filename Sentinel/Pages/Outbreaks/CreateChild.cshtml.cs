using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Create")]
public class CreateChildModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public CreateChildModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    [BindProperty(SupportsGet = true)]
    public int ParentId { get; set; }

    public Outbreak ParentOutbreak { get; set; } = default!;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList Locations { get; set; } = null!;
    public SelectList Events { get; set; } = null!;
    public SelectList Users { get; set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Sub-Investigation Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Display(Name = "Lead Investigator")]
        public string? LeadInvestigatorId { get; set; }

        [Display(Name = "Location")]
        public Guid? PrimaryLocationId { get; set; }

        [Display(Name = "Event")]
        public Guid? PrimaryEventId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        ParentOutbreak = await _outbreakService.GetByIdAsync(ParentId);
        if (ParentOutbreak == null)
            return NotFound();

        // Pre-fill some values from parent
        Input.StartDate = ParentOutbreak.StartDate;
        Input.Description = $"Sub-investigation under {ParentOutbreak.Name}";

        await LoadSelectListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ParentOutbreak = await _outbreakService.GetByIdAsync(ParentId);
        if (ParentOutbreak == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadSelectListsAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        try
        {
            var childOutbreak = new Outbreak
            {
                Name = Input.Name,
                Description = Input.Description,
                Type = Input.PrimaryEventId.HasValue ? OutbreakType.EventBased : OutbreakType.LocationBased,
                Status = OutbreakStatus.Active,
                ConfirmationStatusId = ParentOutbreak.ConfirmationStatusId, // Inherit from parent
                StartDate = Input.StartDate,
                PrimaryDiseaseId = ParentOutbreak.PrimaryDiseaseId, // Inherit from parent
                PrimaryLocationId = Input.PrimaryLocationId,
                PrimaryEventId = Input.PrimaryEventId,
                LeadInvestigatorId = Input.LeadInvestigatorId
            };

            var created = await _outbreakService.CreateChildOutbreakAsync(ParentId, childOutbreak, userId);

            SuccessMessage = $"Sub-investigation '{created.Name}' created successfully.";
            return RedirectToPage("Details", new { id = ParentId });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating sub-investigation: {ex.Message}";
            await LoadSelectListsAsync();
            return Page();
        }
    }

    private async Task LoadSelectListsAsync()
    {
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
    }
}
