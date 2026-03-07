using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateSymptomModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateSymptomModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Symptom Symptom { get; set; } = new Symptom();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Symptom.CreatedAt = DateTime.UtcNow;
            Symptom.CreatedBy = userId;
            Symptom.IsDeleted = false;

            _context.Symptoms.Add(Symptom);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Symptom '{Symptom.Name}' created successfully.";
            return RedirectToPage("./Symptoms");
        }
    }
}
