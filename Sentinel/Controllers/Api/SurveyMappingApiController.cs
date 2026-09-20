using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Sentinel.Controllers.Api
{
    // Collection mappings change system-wide data processing rules, rather than
    // a survey definition itself. Keep this aligned with the settings workflow.
    [Authorize(Policy = "Permission.Settings.Edit")]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("lookup-api")] // 200 per minute - mapping configuration metadata
    public class SurveyMappingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISurveyMappingService _mappingService;
        private readonly ICaseAccessService _caseAccessService;

        public SurveyMappingApiController(
            ApplicationDbContext context,
            ISurveyMappingService mappingService,
            ICaseAccessService caseAccessService)
        {
            _context = context;
            _mappingService = mappingService;
            _caseAccessService = caseAccessService;
        }

        [HttpGet("configuration")]
        public async Task<IActionResult> GetMappings(
            [FromQuery] Guid? surveyTemplateId,
            [FromQuery] Guid? taskTemplateId,
            [FromQuery] Guid? diseaseId)
        {
            var mappings = await _mappingService.GetActiveMappingsAsync(
                surveyTemplateId,
                taskTemplateId,
                diseaseId);

            return Ok(mappings.Select(ToResponse));
        }

        [HttpGet("by-type")]
        public async Task<IActionResult> GetMappingsByType(
            [FromQuery] MappingConfigurationType type,
            [FromQuery] Guid configurationId)
        {
            var mappings = await _context.SurveyFieldMappings
                .Where(m => m.ConfigurationType == type && m.ConfigurationId == configurationId)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return Ok(mappings.Select(ToResponse));
        }

        [HttpGet("available-fields")]
        public async Task<IActionResult> GetAvailableFields([FromQuery] string entityType = "Case")
        {
            var fields = await _mappingService.GetAvailableFieldsAsync(entityType);
            return Ok(fields);
        }

        [HttpGet("survey-questions")]
        public async Task<IActionResult> GetSurveyQuestions([FromQuery] Guid surveyTemplateId)
        {
            var template = await ResolveActiveSurveyTemplateAsync(surveyTemplateId);

            if (template == null)
                return NotFound();

            var questions = await _mappingService.GetSurveyQuestionsAsync(template.SurveyDefinitionJson ?? "{}");
            return Ok(questions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMapping([FromBody] SurveyFieldMappingInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (input.ConfigurationId == Guid.Empty)
                return BadRequest(new { error = "A mapping configuration is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var mapping = CreateMapping(input, userId);

            var validation = await _mappingService.ValidateMappingAsync(mapping);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors, warnings = validation.Warnings });
            }

            mapping.Id = Guid.NewGuid();
            mapping.Priority = (int)mapping.ConfigurationType;
            
            _context.SurveyFieldMappings.Add(mapping);
            await _context.SaveChangesAsync();

            return Ok(ToResponse(mapping));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMapping(Guid id, [FromBody] SurveyFieldMappingInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (input.ConfigurationId == Guid.Empty)
                return BadRequest(new { error = "A mapping configuration is required." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // Do not attach the HTTP payload as an EF entity. Loading first preserves
            // immutable identifiers/audit data and prevents over-posting of navigation
            // or audit properties that are not part of the mapping edit contract.
            var mapping = await _context.SurveyFieldMappings.FirstOrDefaultAsync(m => m.Id == id);
            if (mapping == null)
                return NotFound();

            ApplyInput(mapping, input, userId);

            var validation = await _mappingService.ValidateMappingAsync(mapping);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors, warnings = validation.Warnings });
            }

            await _context.SaveChangesAsync();
            return Ok(ToResponse(mapping));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMapping(Guid id)
        {
            var mapping = await _context.SurveyFieldMappings.FirstOrDefaultAsync(m => m.Id == id);
            if (mapping == null)
                return NotFound();

            _context.SurveyFieldMappings.Remove(mapping);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("copy")]
        public async Task<IActionResult> CopyMappings([FromBody] CopyMappingsRequest request)
        {
            var count = await _mappingService.CopyMappingsAsync(
                request.SourceType,
                request.SourceId,
                request.TargetType,
                request.TargetId);

            return Ok(new { copiedCount = count });
        }

        [HttpPost("suggest")]
        public async Task<IActionResult> SuggestMappings([FromBody] SuggestMappingsRequest request)
        {
            var template = await ResolveActiveSurveyTemplateAsync(request.SurveyTemplateId);

            if (template == null)
                return NotFound();

            var suggestions = await _mappingService.GetSuggestedMappingsAsync(
                template.SurveyDefinitionJson ?? "{}",
                request.ConfigurationType,
                request.ConfigurationId,
                request.DiseaseId);

            return Ok(suggestions);
        }

        [HttpPost("preview")]
        public async Task<IActionResult> PreviewMappings([FromBody] PreviewMappingsRequest request)
        {
            // A preview can read the current Case and Patient values, so it must use
            // the same hierarchy-aware case visibility rule as case pages and APIs.
            if (request.CaseId.HasValue &&
                !await _caseAccessService.CanAccessCaseAsync(request.CaseId.Value))
            {
                return NotFound();
            }

            var mappings = await _mappingService.GetActiveMappingsAsync(
                request.SurveyTemplateId,
                request.TaskTemplateId,
                request.DiseaseId);

            var preview = await _mappingService.PreviewMappingsAsync(
                request.CaseId,
                request.SurveyResponses,
                mappings);

            return Ok(preview);
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateMapping([FromBody] SurveyFieldMappingInput input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (input.ConfigurationId == Guid.Empty)
                return BadRequest(new { error = "A mapping configuration is required." });

            // Validation must use the same allow-listed edit contract as create and
            // update. Binding a tracked entity here was unnecessary and allowed an
            // untrusted request to populate audit, identity and navigation members.
            // This object is deliberately transient and is never attached or saved.
            var mapping = new SurveyFieldMapping();
            ApplyInput(mapping, input, string.Empty);

            var validation = await _mappingService.ValidateMappingAsync(mapping);
            return Ok(validation);
        }

        [HttpPost("bulk-update-order")]
        public async Task<IActionResult> UpdateDisplayOrder([FromBody] List<MappingOrderUpdate> updates)
        {
            foreach (var update in updates)
            {
                var mapping = await _context.SurveyFieldMappings.FindAsync(update.Id);
                if (mapping != null)
                {
                    mapping.DisplayOrder = update.DisplayOrder;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
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

        private static SurveyFieldMapping CreateMapping(SurveyFieldMappingInput input, string userId)
        {
            var mapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow
            };

            ApplyInput(mapping, input, userId);
            return mapping;
        }

        private static void ApplyInput(SurveyFieldMapping mapping, SurveyFieldMappingInput input, string userId)
        {
            mapping.ConfigurationType = input.ConfigurationType;
            mapping.ConfigurationId = input.ConfigurationId;
            mapping.Priority = (int)input.ConfigurationType;
            mapping.SurveyQuestionName = input.SurveyQuestionName;
            mapping.TargetFieldPath = input.TargetFieldPath;
            mapping.TargetFieldType = input.TargetFieldType;
            mapping.FieldCategory = input.FieldCategory;
            mapping.MappingAction = input.MappingAction;
            mapping.BusinessRule = input.BusinessRule;
            mapping.TriggerReviewQueue = input.TriggerReviewQueue;
            mapping.ReviewPriority = input.ReviewPriority;
            mapping.GroupingWindowHours = input.GroupingWindowHours;
            mapping.ValidationRules = input.ValidationRules;
            mapping.TransformationScript = input.TransformationScript;
            mapping.DisplayName = input.DisplayName;
            mapping.Description = input.Description;
            mapping.IsActive = input.IsActive;
            mapping.DisplayOrder = input.DisplayOrder;
            mapping.TargetSymptomId = input.TargetSymptomId;
            mapping.Complexity = input.Complexity;
            mapping.CollectionConfigJson = input.CollectionConfigJson;
            mapping.MatchingRulesJson = input.MatchingRulesJson;
            mapping.OnDuplicateFound = input.OnDuplicateFound;
            mapping.ExecutionOrder = input.ExecutionOrder;
            mapping.LastModifiedByUserId = userId;
            mapping.LastModified = DateTime.UtcNow;
        }

        private static SurveyFieldMappingResponse ToResponse(SurveyFieldMapping mapping) => new(
            mapping.Id,
            mapping.ConfigurationType,
            mapping.ConfigurationId,
            mapping.Priority,
            mapping.SurveyQuestionName,
            mapping.TargetFieldPath,
            mapping.TargetFieldType,
            mapping.FieldCategory,
            mapping.MappingAction,
            mapping.BusinessRule,
            mapping.TriggerReviewQueue,
            mapping.ReviewPriority,
            mapping.GroupingWindowHours,
            mapping.ValidationRules,
            mapping.TransformationScript,
            mapping.DisplayName,
            mapping.Description,
            mapping.IsActive,
            mapping.DisplayOrder,
            mapping.TargetSymptomId,
            mapping.Complexity,
            mapping.CollectionConfigJson,
            mapping.MatchingRulesJson,
            mapping.OnDuplicateFound,
            mapping.ExecutionOrder);
    }

    public sealed class SurveyFieldMappingInput
    {
        [Required]
        [EnumDataType(typeof(MappingConfigurationType))]
        public MappingConfigurationType ConfigurationType { get; set; }

        [Required]
        public Guid ConfigurationId { get; set; }

        [Required]
        [StringLength(500)]
        public string SurveyQuestionName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TargetFieldPath { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(MappingFieldType))]
        public MappingFieldType TargetFieldType { get; set; }

        [Required]
        [EnumDataType(typeof(MappingFieldCategory))]
        public MappingFieldCategory FieldCategory { get; set; }

        [Required]
        [EnumDataType(typeof(MappingAction))]
        public MappingAction MappingAction { get; set; }

        [Required]
        [EnumDataType(typeof(MappingBusinessRule))]
        public MappingBusinessRule BusinessRule { get; set; }

        public bool TriggerReviewQueue { get; set; }
        [Range(1, int.MaxValue)] public int ReviewPriority { get; set; } = 1;
        [Range(0, 8760)] public int GroupingWindowHours { get; set; } = 6;
        [StringLength(2000)] public string? ValidationRules { get; set; }
        [StringLength(2000)] public string? TransformationScript { get; set; }
        [StringLength(500)] public string? DisplayName { get; set; }
        [StringLength(2000)] public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        [Range(0, int.MaxValue)] public int DisplayOrder { get; set; }
        public int? TargetSymptomId { get; set; }
        [EnumDataType(typeof(MappingComplexity))] public MappingComplexity Complexity { get; set; } = MappingComplexity.Simple;
        public string? CollectionConfigJson { get; set; }
        public string? MatchingRulesJson { get; set; }
        [EnumDataType(typeof(DuplicateHandling))] public DuplicateHandling? OnDuplicateFound { get; set; }
        [Range(0, int.MaxValue)] public int ExecutionOrder { get; set; } = 100;
    }

    public sealed record SurveyFieldMappingResponse(
        Guid Id,
        MappingConfigurationType ConfigurationType,
        Guid ConfigurationId,
        int Priority,
        string SurveyQuestionName,
        string TargetFieldPath,
        MappingFieldType TargetFieldType,
        MappingFieldCategory FieldCategory,
        MappingAction MappingAction,
        MappingBusinessRule BusinessRule,
        bool TriggerReviewQueue,
        int ReviewPriority,
        int GroupingWindowHours,
        string? ValidationRules,
        string? TransformationScript,
        string? DisplayName,
        string? Description,
        bool IsActive,
        int DisplayOrder,
        int? TargetSymptomId,
        MappingComplexity Complexity,
        string? CollectionConfigJson,
        string? MatchingRulesJson,
        DuplicateHandling? OnDuplicateFound,
        int ExecutionOrder);

    public class CopyMappingsRequest
    {
        public MappingConfigurationType SourceType { get; set; }
        public Guid SourceId { get; set; }
        public MappingConfigurationType TargetType { get; set; }
        public Guid TargetId { get; set; }
    }

    public class SuggestMappingsRequest
    {
        public Guid SurveyTemplateId { get; set; }
        public MappingConfigurationType ConfigurationType { get; set; }
        public Guid ConfigurationId { get; set; }
        public Guid? DiseaseId { get; set; }
    }

    public class PreviewMappingsRequest
    {
        public Guid? SurveyTemplateId { get; set; }
        public Guid? TaskTemplateId { get; set; }
        public Guid? DiseaseId { get; set; }
        public Guid? CaseId { get; set; }
        public Dictionary<string, object> SurveyResponses { get; set; } = new();
    }

    public class MappingOrderUpdate
    {
        public Guid Id { get; set; }
        public int DisplayOrder { get; set; }
    }
}
