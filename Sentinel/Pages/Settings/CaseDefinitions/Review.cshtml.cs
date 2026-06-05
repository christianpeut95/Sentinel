using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize]
    public class ReviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReviewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public CaseDefinition Definition { get; set; } = null!;
        public List<CaseDefinitionCriteria> Criteria { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Definition = await _context.CaseDefinitions
                .Include(cd => cd.Disease)
                .Include(cd => cd.ConfirmationStatus)
                .Include(cd => cd.Criteria)
                    .ThenInclude(c => c.ChildCriteria)
                .FirstOrDefaultAsync(cd => cd.Id == Id);

            if (Definition == null)
            {
                return NotFound();
            }

            Criteria = Definition.Criteria?.OrderBy(c => c.GroupNumber).ThenBy(c => c.DisplayOrder).ToList() ?? new();

            return Page();
        }

        public async Task<IActionResult> OnPostActivateAsync()
        {
            var definition = await _context.CaseDefinitions
                .FirstOrDefaultAsync(cd => cd.Id == Id);

            if (definition == null)
            {
                return NotFound();
            }

            // Update definition to Current status
            definition.Status = CaseDefinitionStatus.Current;
            definition.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            definition.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Case definition '{definition.Name}' is now active and ready to evaluate cases.";

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostSaveDraftAsync()
        {
            var definition = await _context.CaseDefinitions
                .FirstOrDefaultAsync(cd => cd.Id == Id);

            if (definition == null)
            {
                return NotFound();
            }

            // Keep as Draft status
            definition.Status = CaseDefinitionStatus.Draft;
            definition.ModifiedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
            definition.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Case definition '{definition.Name}' saved as draft.";

            return RedirectToPage("./Index");
        }
    }
}
