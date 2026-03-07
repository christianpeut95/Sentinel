using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class TaskTemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;

        public TaskTemplatesModel(ApplicationDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
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

        [Authorize(Policy = "Permission.Settings.Edit")]
        public async Task<IActionResult> OnPostPropagateAsync(Guid id)
        {
            try
            {
                // Get all disease assignments for this template
                var assignments = await _context.DiseaseTaskTemplates
                    .Where(d => d.TaskTemplateId == id && !d.IsInherited && d.ApplyToChildren)
                    .ToListAsync();

                if (!assignments.Any())
                {
                    TempData["ErrorMessage"] = "No disease assignments found with ApplyToChildren enabled for this template.";
                    return RedirectToPage();
                }

                int totalCreated = 0;
                foreach (var assignment in assignments)
                {
                    await _taskService.PropagateTaskTemplateToChildren(assignment.DiseaseId, id);
                    
                    // Count how many were created
                    var childCount = await _context.DiseaseTaskTemplates
                        .CountAsync(d => d.TaskTemplateId == id && 
                                       d.IsInherited && 
                                       d.InheritedFromDiseaseId == assignment.DiseaseId);
                    totalCreated += childCount;
                }

                TempData["SuccessMessage"] = $"Successfully propagated template to {totalCreated} child disease(s).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error propagating template: {ex.Message}";
            }

            return RedirectToPage();
        }

        [Authorize(Policy = "Permission.Settings.Edit")]
        public async Task<IActionResult> OnPostPropagateAllAsync()
        {
            try
            {
                // Get all parent disease assignments that should propagate
                var parentAssignments = await _context.DiseaseTaskTemplates
                    .Include(d => d.Disease)
                    .Where(d => !d.IsInherited && d.ApplyToChildren && d.IsActive)
                    .ToListAsync();

                int totalCreated = 0;
                int totalProcessed = 0;

                foreach (var assignment in parentAssignments)
                {
                    totalProcessed++;
                    await _taskService.PropagateTaskTemplateToChildren(assignment.DiseaseId, assignment.TaskTemplateId);
                    
                    // Count newly created
                    var childCount = await _context.DiseaseTaskTemplates
                        .CountAsync(d => d.TaskTemplateId == assignment.TaskTemplateId && 
                                       d.IsInherited && 
                                       d.InheritedFromDiseaseId == assignment.DiseaseId);
                    totalCreated += childCount;
                }

                TempData["SuccessMessage"] = $"Processed {totalProcessed} template assignments and created {totalCreated} inherited assignments for child diseases.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error propagating templates: {ex.Message}";
            }

            return RedirectToPage();
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
