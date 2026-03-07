using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Text.Json;

namespace Sentinel.Pages.Settings.Surveys
{
    [Authorize]
    public class CreateSurveyTemplateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateSurveyTemplateModel> _logger;

        public CreateSurveyTemplateModel(ApplicationDbContext context, ILogger<CreateSurveyTemplateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public SurveyTemplate SurveyTemplate { get; set; } = new();

        [BindProperty]
        public List<Guid> SelectedDiseaseIds { get; set; } = new();

        public SelectList Diseases { get; set; } = null!;

        public async Task OnGetAsync()
        {
            await LoadSelectLists();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("CreateSurveyTemplate POST started");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid. Errors: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                await LoadSelectLists();
                return Page();
            }

            try
            {
                // Create empty survey definition
                var emptySurveyJson = new
                {
                    title = SurveyTemplate.Name,
                    description = SurveyTemplate.Description ?? "Click 'Open Visual Designer' to build your survey",
                    elements = new object[] { }
                };

                SurveyTemplate.Id = Guid.NewGuid();
                SurveyTemplate.SurveyDefinitionJson = JsonSerializer.Serialize(emptySurveyJson);
                SurveyTemplate.CreatedAt = DateTime.UtcNow;
                SurveyTemplate.CreatedBy = User.Identity?.Name;
                SurveyTemplate.VersionNumber = "1.0";
                SurveyTemplate.VersionStatus = SurveyVersionStatus.Draft;
                SurveyTemplate.Version = 1;

                _logger.LogInformation("Adding survey template {Name} with ID {Id}", 
                    SurveyTemplate.Name, SurveyTemplate.Id);

                _context.SurveyTemplates.Add(SurveyTemplate);

                // Add disease associations
                foreach (var diseaseId in SelectedDiseaseIds)
                {
                    _context.SurveyTemplateDiseases.Add(new SurveyTemplateDisease
                    {
                        SurveyTemplateId = SurveyTemplate.Id,
                        DiseaseId = diseaseId
                    });
                }

                _logger.LogInformation("Saving changes to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Save successful!");

                TempData["SuccessMessage"] = $"Survey template '{SurveyTemplate.Name}' created successfully! Now design your survey.";
                _logger.LogInformation("Survey template {Name} created by {User}", 
                    SurveyTemplate.Name, User.Identity?.Name);

                // Redirect to Edit page where user can open the visual designer
                return RedirectToPage("./EditSurveyTemplate", new { id = SurveyTemplate.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating survey template {Name}", SurveyTemplate.Name);
                ModelState.AddModelError(string.Empty, $"Error creating survey: {ex.Message}");
                await LoadSelectLists();
                return Page();
            }
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
