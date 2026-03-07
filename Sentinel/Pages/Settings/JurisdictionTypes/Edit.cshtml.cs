using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.JurisdictionTypes
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public JurisdictionTypeInputModel Input { get; set; } = default!;

        public class JurisdictionTypeInputModel
        {
            public int Id { get; set; }

            [Required]
            [Range(1, 5)]
            public int FieldNumber { get; set; }

            [Required]
            [StringLength(100)]
            [Display(Name = "Display Name")]
            public string Name { get; set; } = string.Empty;

            [StringLength(20)]
            [Display(Name = "Short Code")]
            public string? Code { get; set; }

            [StringLength(500)]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; }

            [Display(Name = "Display Order")]
            public int DisplayOrder { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || id < 1 || id > 5)
            {
                return NotFound();
            }

            var jurisdictionType = await _context.JurisdictionTypes
                .FirstOrDefaultAsync(jt => jt.FieldNumber == id);

            if (jurisdictionType == null)
            {
                // Create default for this field number
                Input = new JurisdictionTypeInputModel
                {
                    FieldNumber = id.Value,
                    Name = $"Jurisdiction {id}",
                    IsActive = false,
                    DisplayOrder = id.Value
                };
            }
            else
            {
                Input = new JurisdictionTypeInputModel
                {
                    Id = jurisdictionType.Id,
                    FieldNumber = jurisdictionType.FieldNumber,
                    Name = jurisdictionType.Name,
                    Code = jurisdictionType.Code,
                    Description = jurisdictionType.Description,
                    IsActive = jurisdictionType.IsActive,
                    DisplayOrder = jurisdictionType.DisplayOrder
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingType = await _context.JurisdictionTypes
                .FirstOrDefaultAsync(jt => jt.FieldNumber == Input.FieldNumber);

            if (existingType == null)
            {
                // Create new
                var newType = new JurisdictionType
                {
                    FieldNumber = Input.FieldNumber,
                    Name = Input.Name,
                    Code = Input.Code,
                    Description = Input.Description,
                    IsActive = Input.IsActive,
                    DisplayOrder = Input.DisplayOrder
                };

                _context.JurisdictionTypes.Add(newType);
            }
            else
            {
                // Update existing
                existingType.Name = Input.Name;
                existingType.Code = Input.Code;
                existingType.Description = Input.Description;
                existingType.IsActive = Input.IsActive;
                existingType.DisplayOrder = Input.DisplayOrder;

                _context.JurisdictionTypes.Update(existingType);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Jurisdiction Type '{Input.Name}' saved successfully.";
            return RedirectToPage("./Index");
        }
    }
}
