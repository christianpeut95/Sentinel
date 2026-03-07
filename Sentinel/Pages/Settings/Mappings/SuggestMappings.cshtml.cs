using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Mappings
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class SuggestMappingsModel : PageModel
    {
        private readonly ISurveyMappingService _mappingService;

        public SuggestMappingsModel(ISurveyMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        [BindProperty(SupportsGet = true)]
        public MappingConfigurationType ConfigurationType { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ConfigurationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? SurveyTemplateId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty]
        public List<Guid> SelectedMappingIds { get; set; } = new();

        public List<SurveyFieldMapping> SuggestedMappings { get; set; } = new();
        public bool IsGenerated { get; set; }

        public IActionResult OnGet()
        {
            if (!SurveyTemplateId.HasValue)
            {
                TempData["ErrorMessage"] = "No survey template specified.";
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            if (!SurveyTemplateId.HasValue)
            {
                TempData["ErrorMessage"] = "No survey template specified.";
                return Page();
            }

            try
            {
                // Generate suggestions
                await _mappingService.SuggestMappingsAsync(
                    SurveyTemplateId.Value,
                    ConfigurationType,
                    ConfigurationId,
                    diseaseId: null);

                // Load the generated suggestions
                var allMappings = await _mappingService.GetMappingsByTypeAsync(ConfigurationType, ConfigurationId);
                SuggestedMappings = allMappings.Where(m => !m.IsActive).ToList(); // Only show inactive (new suggestions)

                IsGenerated = true;
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating suggestions: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            try
            {
                // Reload suggestions for display if there's an error
                var allMappings = await _mappingService.GetMappingsByTypeAsync(ConfigurationType, ConfigurationId);
                SuggestedMappings = allMappings.Where(m => !m.IsActive).ToList();
                IsGenerated = SuggestedMappings.Any();

                if (action == "activate")
                {
                    if (!SelectedMappingIds.Any())
                    {
                        TempData["ErrorMessage"] = "No mappings selected.";
                        return Page();
                    }

                    // Activate selected mappings by ID (not by index)
                    var activatedCount = 0;
                    foreach (var mappingId in SelectedMappingIds)
                    {
                        var mapping = await _mappingService.GetMappingByIdAsync(mappingId);
                        if (mapping != null && !mapping.IsActive)
                        {
                            mapping.IsActive = true;
                            await _mappingService.UpdateMappingAsync(mapping);
                            activatedCount++;
                        }
                    }

                    TempData["SuccessMessage"] = $"Activated {activatedCount} mapping(s).";
                }
                else if (action == "discard")
                {
                    // Delete all inactive mappings
                    var inactiveMappings = allMappings.Where(m => !m.IsActive).ToList();

                    foreach (var mapping in inactiveMappings)
                    {
                        await _mappingService.DeleteMappingAsync(mapping.Id);
                    }

                    TempData["SuccessMessage"] = "Discarded all suggestions.";
                }

                // Redirect back
                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    return Redirect(ReturnUrl + (ReturnUrl.Contains('?') ? "&" : "?") + "refresh=" + DateTime.UtcNow.Ticks);
                }

                return RedirectToPage("/Settings/Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                
                // Reload suggestions on error
                try
                {
                    var allMappings = await _mappingService.GetMappingsByTypeAsync(ConfigurationType, ConfigurationId);
                    SuggestedMappings = allMappings.Where(m => !m.IsActive).ToList();
                    IsGenerated = SuggestedMappings.Any();
                }
                catch
                {
                    // Ignore reload errors
                }
                
                return Page();
            }
        }

        public double GetMatchConfidence(SurveyFieldMapping mapping)
        {
            // Simple heuristic - can be improved
            var questionName = mapping.SurveyQuestionName?.ToLowerInvariant() ?? "";
            var fieldPath = mapping.TargetFieldPath?.ToLowerInvariant() ?? "";

            // Exact match
            if (questionName == fieldPath) return 1.0;

            // Contains match
            if (questionName.Contains(fieldPath) || fieldPath.Contains(questionName))
                return 0.8;

            // Partial word match
            var questionWords = questionName.Split('_', '-', ' ');
            var fieldWords = fieldPath.Split('.', '_');

            var matches = questionWords.Intersect(fieldWords).Count();
            if (matches > 0)
                return 0.5 + (matches * 0.1);

            return 0.3; // Low confidence
        }
    }
}
