using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.Text.Json;

namespace Surveillance_MVP.Pages.Settings.Surveys
{
    [Authorize]
    [IgnoreAntiforgeryToken] // Allow AJAX POST without antiforgery token
    public class DesignSurveyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DesignSurveyModel> _logger;

        public DesignSurveyModel(ApplicationDbContext context, ILogger<DesignSurveyModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string SurveyTemplateId { get; set; } = string.Empty;
        public string SurveyName { get; set; } = "New Survey";
        public string? Category { get; set; }
        public string? SurveyDefinitionJson { get; set; }
        public string? OutputMappingJson { get; set; }
        public string? InputMappingJson { get; set; }
        public string ReturnUrl { get; set; } = "/Settings/Surveys/SurveyTemplates";

        public async Task<IActionResult> OnGetAsync(Guid? id, string? returnUrl)
        {
            if (returnUrl != null)
            {
                ReturnUrl = returnUrl;
            }

            if (id == null)
            {
                // New survey
                SurveyTemplateId = Guid.Empty.ToString();
                SurveyName = "New Survey Template";
                SurveyDefinitionJson = "null";
                return Page();
            }

            var surveyTemplate = await _context.SurveyTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == id);

            if (surveyTemplate == null)
            {
                return NotFound();
            }

            SurveyTemplateId = surveyTemplate.Id.ToString();
            SurveyName = surveyTemplate.Name;
            Category = surveyTemplate.Category;

            // Pass the survey JSON directly (will be serialized in the view)
            if (!string.IsNullOrWhiteSpace(surveyTemplate.SurveyDefinitionJson))
            {
                try
                {
                    // Validate it's proper JSON
                    var jsonDoc = JsonDocument.Parse(surveyTemplate.SurveyDefinitionJson);
                    SurveyDefinitionJson = surveyTemplate.SurveyDefinitionJson;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Invalid survey JSON for template {TemplateId}", id);
                    SurveyDefinitionJson = "null";
                }
            }
            else
            {
                SurveyDefinitionJson = "null";
            }

            // Load existing mappings
            OutputMappingJson = surveyTemplate.DefaultOutputMappingJson ?? "{}";
            InputMappingJson = surveyTemplate.DefaultInputMappingJson ?? "{}";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id, [FromBody] SaveSurveyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SurveyDefinitionJson))
            {
                return BadRequest("Survey definition is required");
            }

            // Validate JSON
            try
            {
                var jsonDoc = JsonDocument.Parse(request.SurveyDefinitionJson);
                
                if (!jsonDoc.RootElement.TryGetProperty("title", out _) &&
                    !jsonDoc.RootElement.TryGetProperty("elements", out _) &&
                    !jsonDoc.RootElement.TryGetProperty("pages", out _))
                {
                    return BadRequest("Survey JSON must contain a 'title' and either 'elements' or 'pages' property");
                }
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON format: {ex.Message}");
            }

            if (id == null || id == Guid.Empty)
            {
                return BadRequest("Survey template ID is required");
            }

            var surveyTemplate = await _context.SurveyTemplates
                .FirstOrDefaultAsync(st => st.Id == id);

            if (surveyTemplate == null)
            {
                return NotFound();
            }

            // Check if definition changed
            var oldJson = surveyTemplate.SurveyDefinitionJson ?? string.Empty;
            var newJson = request.SurveyDefinitionJson;

            if (oldJson != newJson)
            {
                // Definition changed - increment version
                surveyTemplate.Version++;
                _logger.LogInformation(
                    "Survey template {TemplateId} definition changed, incrementing version to {Version}",
                    surveyTemplate.Id, surveyTemplate.Version);
            }

            surveyTemplate.SurveyDefinitionJson = newJson;
            surveyTemplate.ModifiedAt = DateTime.UtcNow;
            surveyTemplate.ModifiedBy = User.Identity?.Name;

            // Update mappings if provided
            if (!string.IsNullOrWhiteSpace(request.OutputMappingJson))
            {
                try
                {
                    // Validate JSON
                    JsonDocument.Parse(request.OutputMappingJson);
                    surveyTemplate.DefaultOutputMappingJson = request.OutputMappingJson;
                    _logger.LogInformation("Output mapping updated for template {TemplateId}", surveyTemplate.Id);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid output mapping JSON for template {TemplateId}, skipping", surveyTemplate.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.InputMappingJson))
            {
                try
                {
                    // Validate JSON
                    JsonDocument.Parse(request.InputMappingJson);
                    surveyTemplate.DefaultInputMappingJson = request.InputMappingJson;
                    _logger.LogInformation("Input mapping updated for template {TemplateId}", surveyTemplate.Id);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid input mapping JSON for template {TemplateId}, skipping", surveyTemplate.Id);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Survey template {TemplateId} saved successfully", surveyTemplate.Id);
                return new JsonResult(new { success = true, version = surveyTemplate.Version });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving survey template {TemplateId}", surveyTemplate.Id);
                return StatusCode(500, $"Error saving survey: {ex.Message}");
            }
        }

        public class SaveSurveyRequest
        {
            public string SurveyDefinitionJson { get; set; } = string.Empty;
            public string? OutputMappingJson { get; set; }
            public string? InputMappingJson { get; set; }
        }
    }
}
