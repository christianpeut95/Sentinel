using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.CustomFields
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int Id { get; set; }
        public CustomFieldDefinition Field { get; set; } = null!;
        public int DataCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var field = await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (field == null)
                return NotFound();

            Field = field;
            Id = field.Id;

            // Count how much data exists for this field
            DataCount = field.FieldType switch
            {
                CustomFieldType.Text or CustomFieldType.TextArea or CustomFieldType.Email or CustomFieldType.Phone 
                    => await _context.PatientCustomFieldStrings.CountAsync(p => p.FieldDefinitionId == id),
                CustomFieldType.Number 
                    => await _context.PatientCustomFieldNumbers.CountAsync(p => p.FieldDefinitionId == id),
                CustomFieldType.Date 
                    => await _context.PatientCustomFieldDates.CountAsync(p => p.FieldDefinitionId == id),
                CustomFieldType.Checkbox 
                    => await _context.PatientCustomFieldBooleans.CountAsync(p => p.FieldDefinitionId == id),
                CustomFieldType.Dropdown 
                    => await _context.PatientCustomFieldLookups.CountAsync(p => p.FieldDefinitionId == id),
                _ => 0
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var field = await _context.CustomFieldDefinitions.FindAsync(Id);
            if (field == null)
                return NotFound();

            // Delete associated data
            switch (field.FieldType)
            {
                case CustomFieldType.Text:
                case CustomFieldType.TextArea:
                case CustomFieldType.Email:
                case CustomFieldType.Phone:
                    var strings = await _context.PatientCustomFieldStrings
                        .Where(p => p.FieldDefinitionId == Id)
                        .ToListAsync();
                    _context.PatientCustomFieldStrings.RemoveRange(strings);
                    break;

                case CustomFieldType.Number:
                    var numbers = await _context.PatientCustomFieldNumbers
                        .Where(p => p.FieldDefinitionId == Id)
                        .ToListAsync();
                    _context.PatientCustomFieldNumbers.RemoveRange(numbers);
                    break;

                case CustomFieldType.Date:
                    var dates = await _context.PatientCustomFieldDates
                        .Where(p => p.FieldDefinitionId == Id)
                        .ToListAsync();
                    _context.PatientCustomFieldDates.RemoveRange(dates);
                    break;

                case CustomFieldType.Checkbox:
                    var booleans = await _context.PatientCustomFieldBooleans
                        .Where(p => p.FieldDefinitionId == Id)
                        .ToListAsync();
                    _context.PatientCustomFieldBooleans.RemoveRange(booleans);
                    break;

                case CustomFieldType.Dropdown:
                    var lookups = await _context.PatientCustomFieldLookups
                        .Where(p => p.FieldDefinitionId == Id)
                        .ToListAsync();
                    _context.PatientCustomFieldLookups.RemoveRange(lookups);
                    break;
            }

            _context.CustomFieldDefinitions.Remove(field);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Custom field '{field.Label}' and all associated data has been deleted.";
            return RedirectToPage("./Index");
        }
    }
}
