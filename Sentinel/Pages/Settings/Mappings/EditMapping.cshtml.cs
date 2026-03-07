using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Mappings
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditMappingModel : PageModel
    {
        private readonly ISurveyMappingService _mappingService;
        private readonly ApplicationDbContext _context;

        public EditMappingModel(ISurveyMappingService mappingService, ApplicationDbContext context)
        {
            _mappingService = mappingService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public MappingConfigurationType ConfigurationType { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid ConfigurationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? SurveyTemplateId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? MappingId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FieldCategory { get; set; }

        [BindProperty]
        public SurveyFieldMapping Mapping { get; set; } = new();

        public SelectList SurveyQuestions { get; set; } = new SelectList(Enumerable.Empty<object>());
        public SelectList AvailableFields { get; set; } = new SelectList(Enumerable.Empty<object>());
        public SelectList AvailableSymptoms { get; set; } = new SelectList(Enumerable.Empty<object>());

        public async Task<IActionResult> OnGetAsync()
        {
            if (MappingId.HasValue)
            {
                // Load existing mapping
                var mappings = await _mappingService.GetMappingsByTypeAsync(ConfigurationType, ConfigurationId);
                Mapping = mappings.FirstOrDefault(m => m.Id == MappingId.Value);

                if (Mapping == null)
                {
                    return NotFound();
                }

                // Use the mapping's category (not the query param)
                FieldCategory = Mapping.FieldCategory.ToString();
            }
            else
            {
                // New mapping - check if category was selected via query param
                MappingFieldCategory selectedCategory = MappingFieldCategory.Case; // default
                
                if (!string.IsNullOrEmpty(FieldCategory) && 
                    Enum.TryParse<MappingFieldCategory>(FieldCategory, out var parsed))
                {
                    selectedCategory = parsed;
                }

                Mapping = new SurveyFieldMapping
                {
                    Id = Guid.Empty,
                    ConfigurationType = ConfigurationType,
                    ConfigurationId = ConfigurationId,
                    Priority = (int)ConfigurationType,
                    MappingAction = MappingAction.AutoSave,
                    BusinessRule = MappingBusinessRule.AlwaysOverwrite,
                    FieldCategory = selectedCategory, // Use the selected category
                    TargetFieldType = MappingFieldType.StandardField,
                    IsActive = true,
                    ReviewPriority = 1,
                    GroupingWindowHours = 6
                };

                // Update FieldCategory to match
                FieldCategory = selectedCategory.ToString();
            }

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // ========================================
            // COLLECTION MAPPING SPECIAL VALIDATION
            // ========================================
            if (Mapping.Complexity == MappingComplexity.Collection)
            {
                // Collection mappings don't use TargetFieldPath or FieldCategory
                // These are configured in the popup via CollectionConfigJson
                ModelState.Remove("Mapping.TargetFieldPath");
                ModelState.Remove("Mapping.FieldCategory");
                
                // Set placeholder values to satisfy database constraints
                if (string.IsNullOrEmpty(Mapping.TargetFieldPath))
                {
                    Mapping.TargetFieldPath = "Collection"; // Placeholder - not actually used
                }
                if (Mapping.FieldCategory == 0)
                {
                    Mapping.FieldCategory = MappingFieldCategory.Case; // Default placeholder
                }
                
                // Optional: Validate collection config exists (commented out to allow saving without config)
                // Uncomment if you want to require configuration before saving:
                /*
                if (string.IsNullOrEmpty(Mapping.CollectionConfigJson))
                {
                    ModelState.AddModelError("", 
                        "Collection configuration is required. Click 'Configure Collection Mapping' to set up the mapping.");
                    await LoadSelectLists();
                    return Page();
                }
                */
            }
            else
            {
                // Simple mappings - keep normal validation
                // TargetFieldPath and FieldCategory ARE required
            }
            
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            try
            {
                // CRITICAL: Ensure these are set from the hidden form fields
                Mapping.ConfigurationType = ConfigurationType;
                Mapping.ConfigurationId = ConfigurationId;
                Mapping.Priority = (int)ConfigurationType;

                if (Mapping.Id == Guid.Empty)
                {
                    // Get max display order
                    var existing = await _mappingService.GetMappingsByTypeAsync(ConfigurationType, ConfigurationId);
                    Mapping.DisplayOrder = existing.Any() ? existing.Max(m => m.DisplayOrder) + 1 : 1;

                    await _mappingService.CreateMappingAsync(Mapping);
                    TempData["SuccessMessage"] = "Mapping created successfully.";
                }
                else
                {
                    await _mappingService.UpdateMappingAsync(Mapping);
                    TempData["SuccessMessage"] = "Mapping updated successfully.";
                }

                // Redirect back to return URL or appropriate page
                if (!string.IsNullOrEmpty(ReturnUrl))
                {
                    // Force full page reload to refresh Blazor component
                    return Redirect(ReturnUrl + (ReturnUrl.Contains('?') ? "&" : "?") + "refresh=" + DateTime.UtcNow.Ticks);
                }

                return RedirectToPage(GetReturnPage());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving mapping: {ex.Message}");
                await LoadSelectLists();
                return Page();
            }
        }

        private async Task LoadSelectLists()
        {
            // Load survey questions
            if (SurveyTemplateId.HasValue)
            {
                var questions = await _mappingService.GetSurveyQuestionsAsync(SurveyTemplateId.Value);
                SurveyQuestions = new SelectList(questions, "Name", "Title");
            }

            // Load available symptoms (for Symptom category)
            var symptoms = await _context.Symptoms
                .Where(s => s.IsActive && !s.IsDeleted)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            AvailableSymptoms = new SelectList(symptoms, "Id", "Name");

            // Load available fields based on category
            if (!string.IsNullOrEmpty(FieldCategory) && 
                Enum.TryParse<MappingFieldCategory>(FieldCategory, out var category))
            {
                var entityType = category switch
                {
                    MappingFieldCategory.Patient => "Patient",
                    MappingFieldCategory.Case => "Case",
                    MappingFieldCategory.Symptom => "CaseSymptom",
                    MappingFieldCategory.Exposure => "ExposureEvent",
                    MappingFieldCategory.LabResult => "LabResult",
                    MappingFieldCategory.Task => "CaseTask",
                    _ => "Case"
                };

                var fields = await _mappingService.GetAvailableFieldsAsync(entityType);
                AvailableFields = new SelectList(
                    fields.Select(f => new { Value = f.FieldPath, Text = $"{f.DisplayName} ({f.DataType})" }),
                    "Value",
                    "Text");
            }
        }

        private string GetReturnPage()
        {
            return ConfigurationType switch
            {
                MappingConfigurationType.Survey => $"/Settings/Surveys/EditSurveyTemplate?id={ConfigurationId}",
                MappingConfigurationType.Task => $"/Settings/Lookups/EditTaskTemplate?id={ConfigurationId}",
                MappingConfigurationType.Disease => $"/Settings/Diseases/Edit?id={ConfigurationId}",
                _ => "/Settings/Index"
            };
        }
    }
}
