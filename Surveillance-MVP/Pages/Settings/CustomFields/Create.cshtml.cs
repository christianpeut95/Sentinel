using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Pages.Settings.CustomFields
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
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
            [Required]
            [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Name must be lowercase letters, numbers, and underscores only")]
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
            public bool ShowOnPatientForm { get; set; } = true;
            public bool ShowOnCaseForm { get; set; }

            [Range(0, 1000)]
            public int DisplayOrder { get; set; } = 10;

            public int? LookupTableId { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            // Check if name already exists
            if (await _context.CustomFieldDefinitions.AnyAsync(f => f.Name == Input.Name))
            {
                ModelState.AddModelError("Input.Name", "A field with this name already exists");
                await LoadDataAsync();
                return Page();
            }

            // Validate lookup table for dropdown fields
            if (Input.FieldType == CustomFieldType.Dropdown && !Input.LookupTableId.HasValue)
            {
                ModelState.AddModelError("Input.LookupTableId", "Lookup table is required for dropdown fields");
                await LoadDataAsync();
                return Page();
            }

            var field = new CustomFieldDefinition
            {
                Name = Input.Name,
                Label = Input.Label,
                Category = Input.Category,
                FieldType = Input.FieldType,
                IsRequired = Input.IsRequired,
                IsSearchable = Input.IsSearchable,
                ShowOnList = Input.ShowOnList,
                ShowOnPatientForm = Input.ShowOnPatientForm,
                ShowOnCaseForm = Input.ShowOnCaseForm,
                DisplayOrder = Input.DisplayOrder,
                LookupTableId = Input.LookupTableId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomFieldDefinitions.Add(field);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Custom field '{field.Label}' has been created successfully.";
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
