using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class TaskTemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TaskTemplatesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TaskTemplate> TaskTemplates { get; set; } = new();
        public List<TaskType> TaskTypes { get; set; } = new();
        public List<DiseaseTaskTemplate> DiseaseAssignments { get; set; } = new();

        public async Task OnGetAsync()
        {
            TaskTemplates = await _context.TaskTemplates
                .Include(t => t.TaskType)
                .OrderBy(t => t.TaskType!.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            TaskTypes = await _context.TaskTypes
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            DiseaseAssignments = await _context.DiseaseTaskTemplates
                .Include(d => d.Disease)
                .Where(d => !d.IsInherited)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var template = await _context.TaskTemplates.FindAsync(id);
            
            if (template == null)
            {
                return NotFound();
            }

            // Remove disease assignments first
            var diseaseAssignments = await _context.DiseaseTaskTemplates
                .Where(dt => dt.TaskTemplateId == id)
                .ToListAsync();
            
            _context.DiseaseTaskTemplates.RemoveRange(diseaseAssignments);

            // Remove the template
            _context.TaskTemplates.Remove(template);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Task template '{template.Name}' has been deleted.";
            return RedirectToPage();
        }
    }
}
