using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.Surveys
{
    [Authorize]
    public class SurveyTemplateDetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SurveyTemplateDetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public SurveyTemplate SurveyTemplate { get; set; } = null!;
        public List<TaskTemplate> TaskTemplatesUsing { get; set; } = new();
        public int TotalUsageCount { get; set; }
        public List<SurveyTemplate> AllVersions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var surveyTemplate = await _context.SurveyTemplates
                .Include(st => st.ApplicableDiseases)
                    .ThenInclude(std => std.Disease)
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == id);

            if (surveyTemplate == null)
            {
                return NotFound();
            }

            SurveyTemplate = surveyTemplate;

            TaskTemplatesUsing = await _context.TaskTemplates
                .Include(tt => tt.TaskType)
                .Where(tt => tt.SurveyTemplateId == id)
                .OrderBy(tt => tt.Name)
                .ToListAsync();

            TotalUsageCount = TaskTemplatesUsing.Count;

            // Load all versions for this survey family
            var rootParentId = surveyTemplate.ParentSurveyTemplateId ?? surveyTemplate.Id;
            AllVersions = await _context.SurveyTemplates
                .Where(st => st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
}
