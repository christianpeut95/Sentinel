using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.DiseaseCategories;

[Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Category Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,49}$", ErrorMessage = "Reporting ID can contain letters, numbers, periods, hyphens, and underscores.")]
        [Display(Name = "Reporting ID")]
        public string ReportingId { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Range(0, 10000)]
        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var category = await _context.DiseaseCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = category.Name,
            ReportingId = category.ReportingId,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var name = Input.Name.Trim();
        var reportingId = Input.ReportingId.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Input.Name", "Category name is required.");
        }

        if (string.IsNullOrWhiteSpace(reportingId))
        {
            ModelState.AddModelError("Input.ReportingId", "Reporting ID is required.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var category = await _context.DiseaseCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        if (await _context.DiseaseCategories.AnyAsync(c => c.Id != id && c.Name.ToUpper() == name.ToUpper()))
        {
            ModelState.AddModelError("Input.Name", "A category with this name already exists.");
        }

        if (await _context.DiseaseCategories.AnyAsync(c => c.Id != id && c.ReportingId.ToUpper() == reportingId))
        {
            ModelState.AddModelError("Input.ReportingId", "A category with this Reporting ID already exists.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        category.Name = name;
        category.ReportingId = reportingId;
        category.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
        category.DisplayOrder = Input.DisplayOrder;
        category.IsActive = Input.IsActive;
        category.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Category '{category.Name}' has been updated.";
        return RedirectToPage("./Details", new { id });
    }
}
