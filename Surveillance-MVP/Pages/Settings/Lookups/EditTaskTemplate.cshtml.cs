using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.Text.Json;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditTaskTemplateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditTaskTemplateModel> _logger;

        public EditTaskTemplateModel(ApplicationDbContext context, ILogger<EditTaskTemplateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TaskTemplate TaskTemplate { get; set; } = default!;

        [BindProperty]
        public string SurveySourceType { get; set; } = "custom"; // "library" or "custom"

        public SelectList TaskTypes { get; set; } = default!;
        public SelectList SurveyTemplates { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var taskTemplate = await _context.TaskTemplates
                .Include(t => t.TaskType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskTemplate == null)
            {
                return NotFound();
            }

            TaskTemplate = taskTemplate;
            
            // Determine survey source type
            SurveySourceType = taskTemplate.SurveyTemplateId.HasValue ? "library" : "custom";
            
            await LoadSelectLists();

            return Page();
        }

        public async Task<IActionResult> OnPostSaveBasicAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            var taskTemplateToUpdate = await _context.TaskTemplates.FindAsync(TaskTemplate.Id);

            if (taskTemplateToUpdate == null)
            {
                return NotFound();
            }

            // Update basic properties
            taskTemplateToUpdate.Name = TaskTemplate.Name;
            taskTemplateToUpdate.Description = TaskTemplate.Description;
            taskTemplateToUpdate.TaskTypeId = TaskTemplate.TaskTypeId;
            taskTemplateToUpdate.Instructions = TaskTemplate.Instructions;
            taskTemplateToUpdate.IsActive = TaskTemplate.IsActive;
            taskTemplateToUpdate.ModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Task template basic information updated successfully.";
                _logger.LogInformation("Task template {Id} basic info updated by {User}", TaskTemplate.Id, User.Identity?.Name);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskTemplateExists(TaskTemplate.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./EditTaskTemplate", new { id = TaskTemplate.Id });
        }

        public async Task<IActionResult> OnPostSaveSurveyAsync()
        {
            var taskTemplateToUpdate = await _context.TaskTemplates.FindAsync(TaskTemplate.Id);

            if (taskTemplateToUpdate == null)
            {
                return NotFound();
            }

            // Handle survey source type
            if (SurveySourceType == "library")
            {
                // Using survey library - clear custom survey and set SurveyTemplateId
                taskTemplateToUpdate.SurveyTemplateId = TaskTemplate.SurveyTemplateId;
                taskTemplateToUpdate.SurveyDefinitionJson = null;
                
                // Optional: Allow override of default mappings
                taskTemplateToUpdate.DefaultInputMappingJson = string.IsNullOrWhiteSpace(TaskTemplate.DefaultInputMappingJson)
                    ? null
                    : TaskTemplate.DefaultInputMappingJson.Trim();
                taskTemplateToUpdate.DefaultOutputMappingJson = string.IsNullOrWhiteSpace(TaskTemplate.DefaultOutputMappingJson)
                    ? null
                    : TaskTemplate.DefaultOutputMappingJson.Trim();
                
                _logger.LogInformation("Task template {Id} configured to use survey library template {SurveyTemplateId}", 
                    TaskTemplate.Id, TaskTemplate.SurveyTemplateId);
            }
            else
            {
                // Using custom survey - clear SurveyTemplateId
                taskTemplateToUpdate.SurveyTemplateId = null;

                // Validate JSON if provided
                if (!string.IsNullOrWhiteSpace(TaskTemplate.SurveyDefinitionJson))
                {
                    try
                    {
                        // Validate it's proper JSON
                        var jsonDoc = JsonDocument.Parse(TaskTemplate.SurveyDefinitionJson);
                        
                        // Basic validation - must have title and elements/pages
                        if (!jsonDoc.RootElement.TryGetProperty("title", out _) &&
                            !jsonDoc.RootElement.TryGetProperty("elements", out _) &&
                            !jsonDoc.RootElement.TryGetProperty("pages", out _))
                        {
                            ModelState.AddModelError("TaskTemplate.SurveyDefinitionJson", 
                                "Survey JSON must contain a 'title' and either 'elements' or 'pages' property.");
                            await LoadSelectLists();
                            return Page();
                        }

                        _logger.LogInformation("Survey JSON validated successfully for task template {Id}", TaskTemplate.Id);
                    }
                    catch (JsonException ex)
                    {
                        ModelState.AddModelError("TaskTemplate.SurveyDefinitionJson", 
                            $"Invalid JSON format: {ex.Message}");
                        await LoadSelectLists();
                        return Page();
                    }
                }

                // Validate Input Mapping JSON if provided
                if (!string.IsNullOrWhiteSpace(TaskTemplate.DefaultInputMappingJson))
                {
                    try
                    {
                        JsonDocument.Parse(TaskTemplate.DefaultInputMappingJson);
                    }
                    catch (JsonException ex)
                    {
                        ModelState.AddModelError("TaskTemplate.DefaultInputMappingJson", 
                            $"Invalid JSON format: {ex.Message}");
                        await LoadSelectLists();
                        return Page();
                    }
                }

                // Validate Output Mapping JSON if provided
                if (!string.IsNullOrWhiteSpace(TaskTemplate.DefaultOutputMappingJson))
                {
                    try
                    {
                        JsonDocument.Parse(TaskTemplate.DefaultOutputMappingJson);
                    }
                    catch (JsonException ex)
                    {
                        ModelState.AddModelError("TaskTemplate.DefaultOutputMappingJson", 
                            $"Invalid JSON format: {ex.Message}");
                        await LoadSelectLists();
                        return Page();
                    }
                }

                // Update survey definition and default mappings
                taskTemplateToUpdate.SurveyDefinitionJson = string.IsNullOrWhiteSpace(TaskTemplate.SurveyDefinitionJson) 
                    ? null 
                    : TaskTemplate.SurveyDefinitionJson.Trim();
                taskTemplateToUpdate.DefaultInputMappingJson = string.IsNullOrWhiteSpace(TaskTemplate.DefaultInputMappingJson)
                    ? null
                    : TaskTemplate.DefaultInputMappingJson.Trim();
                taskTemplateToUpdate.DefaultOutputMappingJson = string.IsNullOrWhiteSpace(TaskTemplate.DefaultOutputMappingJson)
                    ? null
                    : TaskTemplate.DefaultOutputMappingJson.Trim();
                
                _logger.LogInformation("Task template {Id} configured with custom survey", TaskTemplate.Id);
            }

            taskTemplateToUpdate.ModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Survey configuration saved successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskTemplateExists(TaskTemplate.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./EditTaskTemplate", new { id = TaskTemplate.Id });
        }

        private async Task LoadSelectLists()
        {
            TaskTypes = new SelectList(
                await _context.TaskTypes.OrderBy(t => t.Name).ToListAsync(),
                "Id",
                "Name",
                TaskTemplate?.TaskTypeId);

            SurveyTemplates = new SelectList(
                await _context.SurveyTemplates
                    .Where(st => st.IsActive)
                    .OrderBy(st => st.Category)
                    .ThenBy(st => st.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                TaskTemplate?.SurveyTemplateId,
                "Category");
        }

        private bool TaskTemplateExists(Guid id)
        {
            return _context.TaskTemplates.Any(e => e.Id == id);
        }
    }
}
