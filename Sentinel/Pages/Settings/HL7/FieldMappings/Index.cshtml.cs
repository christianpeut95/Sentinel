using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.HL7;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<HL7CustomFieldMapping> Mappings { get; set; } = new();
        public List<Disease> Diseases { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedDiseaseId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Load diseases for filter
            Diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Load mappings with filter
            var query = _context.HL7CustomFieldMappings
                .Include(m => m.Disease)
                .Include(m => m.CustomFieldDefinition)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SelectedDiseaseId) && Guid.TryParse(SelectedDiseaseId, out var diseaseGuid))
            {
                query = query.Where(m => m.DiseaseId == diseaseGuid);
            }

            Mappings = await query
                .OrderBy(m => m.Disease!.Name)
                .ThenBy(m => m.Priority)
                .ThenBy(m => m.HL7TestCode)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var mapping = await _context.HL7CustomFieldMappings.FindAsync(id);
            if (mapping == null)
            {
                return NotFound();
            }

            _context.HL7CustomFieldMappings.Remove(mapping);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Mapping for test code '{mapping.HL7TestCode}' has been deleted.";
            return RedirectToPage();
        }
    }
}
