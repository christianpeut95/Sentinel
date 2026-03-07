using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.CustomFields
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> FieldTypeOptions { get; set; } = new();
        public List<SelectListItem> LookupTableOptions { get; set; } = new();
        public List<string> ExistingCategories { get; set; } = new();

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            public string Name { get; set; } = string.Empty;

            [Required]
            public string Label { get; set; } = string.Empty;

            [Required]
            public string Category { get; set; } = "General";

            [Required]
            public CustomFieldType FieldType { get; set; }

            public bool IsRequired { get; set; }
            public bool IsSearchable { get; set; }
            public bool ShowOnList { get; set; }
            public bool ShowOnPatientForm { get; set; }
            public bool ShowOnCaseForm { get; set; }

            [Range(0, 1000)]
            public int DisplayOrder { get; set; } = 10;

            public int? LookupTableId { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var field = await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null)
                return NotFound();

            Input = new InputModel
            {
                Id = field.Id,
                Name = field.Name,
                Label = field.Label,
                Category = field.Category,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                IsSearchable = field.IsSearchable,
                ShowOnList = field.ShowOnList,
                DisplayOrder = field.DisplayOrder,
                LookupTableId = field.LookupTableId,
                IsActive = field.IsActive
            };

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var field = await _context.CustomFieldDefinitions.FindAsync(Input.Id);
            if (field == null)
                return NotFound();

            // Check if name changed and conflicts
            if (field.Name != Input.Name && await _context.CustomFieldDefinitions.AnyAsync(f => f.Name == Input.Name && f.Id != Input.Id))
            {
                ModelState.AddModelError("Input.Name", "A field with this name already exists");
                await LoadDataAsync();
                return Page();
            }

            field.Name = Input.Name;
            field.Label = Input.Label;
            field.Category = Input.Category;
            field.FieldType = Input.FieldType;
            field.IsRequired = Input.IsRequired;
            field.IsSearchable = Input.IsSearchable;
            field.ShowOnList = Input.ShowOnList;
            field.ShowOnPatientForm = Input.ShowOnPatientForm;
            field.ShowOnCaseForm = Input.ShowOnCaseForm;
            field.DisplayOrder = Input.DisplayOrder;
            field.LookupTableId = Input.LookupTableId;
            field.IsActive = Input.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Custom field '{field.Label}' has been updated successfully.";
            return RedirectToPage("./Index");
        }

        private async Task LoadDataAsync()
        {
            FieldTypeOptions = Enum.GetValues<CustomFieldType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();

            var lookupTables = await _context.LookupTables
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayName)
                .ToListAsync();

            LookupTableOptions = lookupTables
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.DisplayName
                })
                .ToList();

            ExistingCategories = await _context.CustomFieldDefinitions
                .Select(f => f.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
