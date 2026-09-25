using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.DiseaseCategories
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
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
            public bool IsActive { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Input.DisplayOrder = await _context.DiseaseCategories
                .Select(c => (int?)c.DisplayOrder)
                .MaxAsync() is int highestDisplayOrder
                    ? highestDisplayOrder + 10
                    : 10;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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

            if (await _context.DiseaseCategories.AnyAsync(c => c.Name.ToUpper() == name.ToUpper()))
            {
                ModelState.AddModelError("Input.Name", "A category with this name already exists.");
                return Page();
            }

            if (await _context.DiseaseCategories.AnyAsync(c => c.ReportingId.ToUpper() == reportingId))
            {
                ModelState.AddModelError("Input.ReportingId", "A category with this Reporting ID already exists.");
                return Page();
            }

            var category = new DiseaseCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                ReportingId = reportingId,
                Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim(),
                DisplayOrder = Input.DisplayOrder,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.DiseaseCategories.Add(category);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Category '{category.Name}' has been created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
