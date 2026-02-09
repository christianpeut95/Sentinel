using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.Text.Json;

namespace Surveillance_MVP.Pages.Settings.Surveys
{
    [Authorize]
    public class EditSurveyTemplateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditSurveyTemplateModel> _logger;

        public EditSurveyTemplateModel(ApplicationDbContext context, ILogger<EditSurveyTemplateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public SurveyTemplate SurveyTemplate { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedDiseaseIds { get; set; } = new();

        public SelectList Diseases { get; set; } = null!;
        public int TaskTemplateUsageCount { get; set; }
        public List<SurveyTemplate> AllVersions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var surveyTemplate = await _context.SurveyTemplates
                .Include(st => st.ApplicableDiseases)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (surveyTemplate == null)
            {
                return NotFound();
            }

            SurveyTemplate = surveyTemplate;
            SelectedDiseaseIds = surveyTemplate.ApplicableDiseases
                .Select(std => std.DiseaseId)
                .ToList();

            TaskTemplateUsageCount = await _context.TaskTemplates
                .CountAsync(tt => tt.SurveyTemplateId == id);

            // Load all versions for this survey family
            var rootParentId = surveyTemplate.ParentSurveyTemplateId ?? surveyTemplate.Id;
            AllVersions = await _context.SurveyTemplates
                .Where(st => st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId)
                .OrderByDescending(st => st.CreatedAt)
                .ToListAsync();

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate survey JSON
            if (!string.IsNullOrWhiteSpace(SurveyTemplate.SurveyDefinitionJson))
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(SurveyTemplate.SurveyDefinitionJson);
                    
                    if (!jsonDoc.RootElement.TryGetProperty("title", out _) &&
                        !jsonDoc.RootElement.TryGetProperty("elements", out _) &&
                        !jsonDoc.RootElement.TryGetProperty("pages", out _))
                    {
                        ModelState.AddModelError("SurveyTemplate.SurveyDefinitionJson", 
                            "Survey JSON must contain a 'title' and either 'elements' or 'pages' property.");
                    }
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("SurveyTemplate.SurveyDefinitionJson", 
                        $"Invalid JSON format: {ex.Message}");
                }
            }

            // Validate input mapping JSON if provided
            if (!string.IsNullOrWhiteSpace(SurveyTemplate.DefaultInputMappingJson))
            {
                try
                {
                    JsonDocument.Parse(SurveyTemplate.DefaultInputMappingJson);
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("SurveyTemplate.DefaultInputMappingJson", 
                        $"Invalid JSON format: {ex.Message}");
                }
            }

            // Validate output mapping JSON if provided
            if (!string.IsNullOrWhiteSpace(SurveyTemplate.DefaultOutputMappingJson))
            {
                try
                {
                    JsonDocument.Parse(SurveyTemplate.DefaultOutputMappingJson);
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("SurveyTemplate.DefaultOutputMappingJson", 
                        $"Invalid JSON format: {ex.Message}");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            var templateToUpdate = await _context.SurveyTemplates
                .Include(st => st.ApplicableDiseases)
                .FirstOrDefaultAsync(st => st.Id == SurveyTemplate.Id);

            if (templateToUpdate == null)
            {
                return NotFound();
            }

            // Check if system template
            if (templateToUpdate.IsSystemTemplate)
            {
                TempData["ErrorMessage"] = "Cannot edit system templates";
                return RedirectToPage("./SurveyTemplates");
            }

            // Update properties
            templateToUpdate.Name = SurveyTemplate.Name;
            templateToUpdate.Description = SurveyTemplate.Description;
            templateToUpdate.Category = SurveyTemplate.Category;
            templateToUpdate.SurveyDefinitionJson = SurveyTemplate.SurveyDefinitionJson;
            templateToUpdate.DefaultInputMappingJson = SurveyTemplate.DefaultInputMappingJson;
            templateToUpdate.DefaultOutputMappingJson = SurveyTemplate.DefaultOutputMappingJson;
            templateToUpdate.Tags = SurveyTemplate.Tags;
            templateToUpdate.IsActive = SurveyTemplate.IsActive;
            templateToUpdate.ModifiedAt = DateTime.UtcNow;
            templateToUpdate.ModifiedBy = User.Identity?.Name;

            // Update disease associations
            _context.SurveyTemplateDiseases.RemoveRange(templateToUpdate.ApplicableDiseases);
            
            foreach (var diseaseId in SelectedDiseaseIds)
            {
                _context.SurveyTemplateDiseases.Add(new SurveyTemplateDisease
                {
                    SurveyTemplateId = templateToUpdate.Id,
                    DiseaseId = diseaseId
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Survey template '{templateToUpdate.Name}' updated successfully!";

            _logger.LogInformation("Survey template {Name} updated by {User}", 
                templateToUpdate.Name, User.Identity?.Name);

            return RedirectToPage("./SurveyTemplates");
        }

        private async Task LoadSelectLists()
        {
            Diseases = new SelectList(
                await _context.Diseases
                    .OrderBy(d => d.Name)
                    .ToListAsync(),
                "Id",
                "Name");
        }
    }
}
