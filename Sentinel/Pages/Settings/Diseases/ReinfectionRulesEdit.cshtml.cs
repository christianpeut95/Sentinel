using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class ReinfectionRulesEditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReinfectionRulesEditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DiseaseReinfectionRule Rule { get; set; } = new DiseaseReinfectionRule
        {
            IsActive = true,
            RuleType = ReinfectionRuleType.TimeWindow,
            ReinfectionWindowDays = 90
        };

        public SelectList DiseaseSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid? id, Guid? diseaseId)
        {
            if (id.HasValue)
            {
                // Edit mode
                var rule = await _context.DiseaseReinfectionRules.FindAsync(id.Value);
                if (rule == null)
                {
                    return NotFound();
                }
                Rule = rule;
            }
            else if (diseaseId.HasValue)
            {
                // Create mode with pre-selected disease
                Rule.DiseaseId = diseaseId.Value;
            }

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Always set rule type to TimeWindow since that's the only type we use
            Rule.RuleType = ReinfectionRuleType.TimeWindow;

            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // Check if rule already exists for this disease (excluding current rule in edit mode)
            var existingRule = await _context.DiseaseReinfectionRules
                .Where(r => r.DiseaseId == Rule.DiseaseId && r.Id != Rule.Id)
                .FirstOrDefaultAsync();

            if (existingRule != null)
            {
                ModelState.AddModelError("Rule.DiseaseId", "A reinfection rule already exists for this disease. Each disease can have only one rule.");
                await LoadSelectListsAsync();
                return Page();
            }

            // Validate reinfection window is provided
            if (!Rule.ReinfectionWindowDays.HasValue)
            {
                ModelState.AddModelError("Rule.ReinfectionWindowDays", "Reinfection window (in days) is required.");
                await LoadSelectListsAsync();
                return Page();
            }

            if (Rule.ReinfectionWindowDays.Value < 0)
            {
                ModelState.AddModelError("Rule.ReinfectionWindowDays", "Reinfection window must be 0 or greater.");
                await LoadSelectListsAsync();
                return Page();
            }

            if (Rule.Id == Guid.Empty)
            {
                // Create new rule
                Rule.CreatedAt = DateTime.UtcNow;
                Rule.ModifiedAt = DateTime.UtcNow;
                _context.DiseaseReinfectionRules.Add(Rule);
                TempData["SuccessMessage"] = "Reinfection rule has been created successfully.";
            }
            else
            {
                // Update existing rule
                var existingRuleToUpdate = await _context.DiseaseReinfectionRules
                    .FirstOrDefaultAsync(r => r.Id == Rule.Id);

                if (existingRuleToUpdate == null)
                {
                    return NotFound();
                }

                // Update properties
                existingRuleToUpdate.DiseaseId = Rule.DiseaseId;
                existingRuleToUpdate.RuleType = Rule.RuleType;
                existingRuleToUpdate.ReinfectionWindowDays = Rule.ReinfectionWindowDays;
                existingRuleToUpdate.RequireConfirmationForNewCase = Rule.RequireConfirmationForNewCase;
                existingRuleToUpdate.Description = Rule.Description;
                existingRuleToUpdate.NotificationMessage = Rule.NotificationMessage;
                existingRuleToUpdate.IsActive = Rule.IsActive;
                existingRuleToUpdate.Notes = Rule.Notes;
                existingRuleToUpdate.ModifiedAt = DateTime.UtcNow;

                TempData["SuccessMessage"] = "Reinfection rule has been updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("./ReinfectionRules", new { diseaseId = Rule.DiseaseId });
        }

        private async Task LoadSelectListsAsync()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            DiseaseSelectList = new SelectList(diseases, "Id", "Name");
        }
    }
}
