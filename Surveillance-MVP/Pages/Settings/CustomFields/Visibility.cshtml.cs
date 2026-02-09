using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Settings.CustomFields
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class VisibilityModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public VisibilityModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();

        public async Task OnGetAsync()
        {
            CustomFields = await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();

            FieldsByCategory = CustomFields
                .GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<IActionResult> OnPostToggleAsync(int fieldId, string property)
        {
            var field = await _context.CustomFieldDefinitions.FindAsync(fieldId);
            if (field == null)
            {
                return NotFound();
            }

            switch (property)
            {
                case "ShowOnCreateEdit":
                    field.ShowOnCreateEdit = !field.ShowOnCreateEdit;
                    break;
                case "ShowOnDetails":
                    field.ShowOnDetails = !field.ShowOnDetails;
                    break;
                case "ShowOnList":
                    field.ShowOnList = !field.ShowOnList;
                    break;
                default:
                    return BadRequest();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Updated visibility for '{field.Label}'.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostBulkUpdateAsync(string category, string property, bool value)
        {
            var fieldsToUpdate = await _context.CustomFieldDefinitions
                .Where(f => f.Category == category)
                .ToListAsync();

            foreach (var field in fieldsToUpdate)
            {
                switch (property)
                {
                    case "ShowOnCreateEdit":
                        field.ShowOnCreateEdit = value;
                        break;
                    case "ShowOnDetails":
                        field.ShowOnDetails = value;
                        break;
                    case "ShowOnList":
                        field.ShowOnList = value;
                        break;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Updated {fieldsToUpdate.Count} field(s) in category '{category}'.";
            return RedirectToPage();
        }
    }
}
