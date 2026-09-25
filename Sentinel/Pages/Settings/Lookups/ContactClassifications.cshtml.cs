using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.Lookups;

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class ContactClassificationsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public ContactClassificationsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<ContactClassification> ContactClassifications { get; private set; } = new List<ContactClassification>();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsEditing => Input.Id.HasValue;

    public class InputModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Display order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int? editId)
    {
        if (editId.HasValue)
        {
            var classification = await _context.ContactClassifications.FindAsync(editId.Value);
            if (classification == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = classification.Id,
                Name = classification.Name,
                Description = classification.Description,
                DisplayOrder = classification.DisplayOrder,
                IsActive = classification.IsActive
            };
        }
        else
        {
            Input.DisplayOrder = await _context.ContactClassifications
                .Select(c => (int?)c.DisplayOrder)
                .MaxAsync() is int highestDisplayOrder
                    ? highestDisplayOrder + 10
                    : 10;
        }

        await LoadClassificationsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadClassificationsAsync();
            return Page();
        }

        var name = Input.Name.Trim();
        var normalizedName = name.ToUpperInvariant();
        var duplicateExists = await _context.ContactClassifications.AnyAsync(c =>
            c.Id != Input.Id && c.Name.ToUpper() == normalizedName);

        if (duplicateExists)
        {
            ModelState.AddModelError("Input.Name", "A contact classification with this name already exists.");
            await LoadClassificationsAsync();
            return Page();
        }

        if (Input.Id.HasValue)
        {
            var classification = await _context.ContactClassifications.FindAsync(Input.Id.Value);
            if (classification == null)
            {
                return NotFound();
            }

            classification.Name = name;
            classification.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
            classification.DisplayOrder = Input.DisplayOrder;
            classification.IsActive = Input.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Contact classification '{classification.Name}' has been updated.";
        }
        else
        {
            var classification = new ContactClassification
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                DisplayOrder = Input.DisplayOrder,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactClassifications.Add(classification);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Contact classification '{classification.Name}' has been created.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var classification = await _context.ContactClassifications.FindAsync(id);
        if (classification == null)
        {
            return NotFound();
        }

        classification.IsActive = !classification.IsActive;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Contact classification '{classification.Name}' has been {(classification.IsActive ? "activated" : "deactivated")}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var classification = await _context.ContactClassifications.FindAsync(id);
        if (classification == null)
        {
            return NotFound();
        }

        var isInUse = await _context.ExposureEvents.AnyAsync(e => e.ContactClassificationId == id);
        if (isInUse)
        {
            TempData["ErrorMessage"] = $"'{classification.Name}' cannot be deleted because it is used by one or more exposure records. You can deactivate it instead.";
            return RedirectToPage();
        }

        _context.ContactClassifications.Remove(classification);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Contact classification '{classification.Name}' has been deleted.";
        return RedirectToPage();
    }

    private async Task LoadClassificationsAsync()
    {
        ContactClassifications = await _context.ContactClassifications
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
}
