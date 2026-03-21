using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using Newtonsoft.Json;

namespace Sentinel.Pages.Settings.Mappings;

[Authorize]
public class ConfigureCollectionModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ICollectionMappingService _collectionMappingService;
    private readonly ILogger<ConfigureCollectionModel> _logger;

    public ConfigureCollectionModel(
        ApplicationDbContext context,
        ICollectionMappingService collectionMappingService,
        ILogger<ConfigureCollectionModel> logger)
    {
        _context = context;
        _collectionMappingService = collectionMappingService;
        _logger = logger;
    }

    public SurveyFieldMapping? Mapping { get; set; }
    public SurveyTemplate? SurveyTemplate { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid? mappingId, Guid? surveyTemplateId, string? returnUrl)
    {
        ReturnUrl = returnUrl;

        try
        {
            // Load mapping if editing existing
            if (mappingId.HasValue)
            {
                Mapping = await _context.SurveyFieldMappings
                    .FirstOrDefaultAsync(m => m.Id == mappingId.Value);

                if (Mapping == null)
                {
                    ErrorMessage = "Mapping not found.";
                    return Page();
                }

                // Get survey template from mapping - resolve to active version
                if (Mapping.ConfigurationType == MappingConfigurationType.Survey && Mapping.ConfigurationId != Guid.Empty)
                {
                    SurveyTemplate = await ResolveActiveSurveyTemplateAsync(Mapping.ConfigurationId);
                }
            }

            // If survey template ID provided directly, resolve to active version
            if (surveyTemplateId.HasValue && SurveyTemplate == null)
            {
                SurveyTemplate = await ResolveActiveSurveyTemplateAsync(surveyTemplateId.Value);
            }

            // If creating new mapping, initialize empty
            if (Mapping == null)
            {
                Mapping = new SurveyFieldMapping
                {
                    Complexity = MappingComplexity.Collection,
                    CollectionConfigJson = string.Empty
                };
            }

            if (SurveyTemplate == null)
            {
                ErrorMessage = "Survey template not found. Collection mappings require a survey template.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading collection mapping configuration");
            ErrorMessage = $"Error loading configuration: {ex.Message}";
        }

        return Page();
    }

    private async Task<SurveyTemplate?> ResolveActiveSurveyTemplateAsync(Guid surveyTemplateId)
    {
        var original = await _context.SurveyTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == surveyTemplateId);

        if (original == null) return null;

        var rootParentId = original.ParentSurveyTemplateId ?? original.Id;
        var active = await _context.SurveyTemplates
            .AsNoTracking()
            .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
            .Where(st => st.VersionStatus == SurveyVersionStatus.Active)
            .FirstOrDefaultAsync();

        return active ?? original;
    }

    public async Task<IActionResult> SaveConfigurationAsync(CollectionMappingConfig config)
    {
        try
        {
            // Validate configuration
            var validation = await _collectionMappingService.ValidateMappingConfigAsync(config);

            if (!validation.IsValid)
            {
                ErrorMessage = "Configuration validation failed: " + string.Join(", ", validation.Errors);
                return Page();
            }

            // Serialize configuration to JSON
            var configJson = JsonConvert.SerializeObject(config, Formatting.Indented);

            // Update mapping (if editing) or prepare for creation
            if (Mapping != null)
            {
                Mapping.CollectionConfigJson = configJson;
                Mapping.Complexity = MappingComplexity.Collection;

                // Update target field path to match entity type
                Mapping.TargetFieldPath = config.TargetEntityType;

                // If it's an existing mapping in database, save it
                if (Mapping.Id != Guid.Empty)
                {
                    _context.Entry(Mapping).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Collection mapping configuration saved successfully!";
                }
            }

            // Notify parent window via JavaScript
            return new JsonResult(new { success = true, configJson });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving collection mapping configuration");
            ErrorMessage = $"Error saving configuration: {ex.Message}";
            return Page();
        }
    }
}
