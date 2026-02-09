using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Settings.Surveys
{
    [Authorize]
    public class SurveyTemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurveyTemplatesModel> _logger;

        public SurveyTemplatesModel(ApplicationDbContext context, ILogger<SurveyTemplatesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<SurveyTemplate> SurveyTemplates { get; set; } = new();
        public Dictionary<Guid, int> UsageCounts { get; set; } = new();
        public Dictionary<Guid, int> DiseaseCounts { get; set; } = new();

        public string? CategoryFilter { get; set; }
        public bool? ActiveFilter { get; set; }
        public string? SearchTerm { get; set; }

        public async Task OnGetAsync(string? category, bool? active, string? search)
        {
            CategoryFilter = category;
            ActiveFilter = active;
            SearchTerm = search;

            var query = _context.SurveyTemplates
                .Include(st => st.ApplicableDiseases)
                    .ThenInclude(std => std.Disease)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(st => st.Category == category);
            }

            if (active.HasValue)
            {
                query = query.Where(st => st.IsActive == active.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(st => 
                    st.Name.Contains(search) || 
                    (st.Description != null && st.Description.Contains(search)) ||
                    (st.Tags != null && st.Tags.Contains(search)));
            }

            SurveyTemplates = await query
                .OrderBy(st => st.Category)
                .ThenBy(st => st.Name)
                .ToListAsync();

            // Get usage counts
            foreach (var template in SurveyTemplates)
            {
                var count = await _context.TaskTemplates
                    .CountAsync(tt => tt.SurveyTemplateId == template.Id);
                UsageCounts[template.Id] = count;

                DiseaseCounts[template.Id] = template.ApplicableDiseases.Count;
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var template = await _context.SurveyTemplates.FindAsync(id);
            
            if (template == null)
                return NotFound();
            
            // Check if system template
            if (template.IsSystemTemplate)
            {
                TempData["ErrorMessage"] = "Cannot delete system templates";
                return RedirectToPage();
            }
            
            // Check if in use
            var usageCount = await _context.TaskTemplates
                .CountAsync(tt => tt.SurveyTemplateId == id);
            
            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete: Template is used by {usageCount} task template(s)";
                return RedirectToPage();
            }
            
            _context.SurveyTemplates.Remove(template);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Survey template '{template.Name}' deleted successfully";
            _logger.LogInformation("Survey template {Name} deleted by {User}", template.Name, User.Identity?.Name);
            
            return RedirectToPage();
        }
    }
}
