using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class TaskTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TaskTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TaskType> TaskTypes { get; set; } = new();
        public Dictionary<Guid, int> TemplateCount { get; set; } = new();

        public async Task OnGetAsync()
        {
            TaskTypes = await _context.TaskTypes
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            // Count templates for each task type
            var templates = await _context.TaskTemplates
                .GroupBy(t => t.TaskTypeId)
                .Select(g => new { TaskTypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            TemplateCount = templates.ToDictionary(t => t.TaskTypeId, t => t.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var taskType = await _context.TaskTypes.FindAsync(id);
            
            if (taskType == null)
            {
                return NotFound();
            }

            // Check if any templates use this task type
            var hasTemplates = await _context.TaskTemplates.AnyAsync(t => t.TaskTypeId == id);
            if (hasTemplates)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{taskType.Name}' because it is being used by task templates.";
                return RedirectToPage();
            }

            _context.TaskTypes.Remove(taskType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Task type '{taskType.Name}' has been deleted.";
            return RedirectToPage();
        }
    }
}
