using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("lookup-api")] // 200 per minute - mapping configuration metadata
    public class SurveyMappingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISurveyMappingService _mappingService;

        public SurveyMappingApiController(
            ApplicationDbContext context,
            ISurveyMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
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

            return Ok(mappings);
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

            return Ok(mappings);
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
            var template = await _context.SurveyTemplates
                .FirstOrDefaultAsync(st => st.Id == surveyTemplateId);

            if (template == null)
                return NotFound();

            var questions = await _mappingService.GetSurveyQuestionsAsync(template.SurveyDefinitionJson ?? "{}");
            return Ok(questions);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMapping([FromBody] SurveyFieldMapping mapping)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validation = await _mappingService.ValidateMappingAsync(mapping);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors, warnings = validation.Warnings });
            }

            mapping.Id = Guid.NewGuid();
            mapping.Priority = (int)mapping.ConfigurationType;
            
            _context.SurveyFieldMappings.Add(mapping);
            await _context.SaveChangesAsync();

            return Ok(mapping);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMapping(Guid id, [FromBody] SurveyFieldMapping mapping)
        {
            if (id != mapping.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validation = await _mappingService.ValidateMappingAsync(mapping);
            if (!validation.IsValid)
            {
                return BadRequest(new { errors = validation.Errors, warnings = validation.Warnings });
            }

            _context.Entry(mapping).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await MappingExistsAsync(id))
                    return NotFound();
                throw;
            }

            return Ok(mapping);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMapping(Guid id)
        {
            var mapping = await _context.SurveyFieldMappings.FindAsync(id);
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
            var template = await _context.SurveyTemplates
                .FirstOrDefaultAsync(st => st.Id == request.SurveyTemplateId);

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
        public async Task<IActionResult> ValidateMapping([FromBody] SurveyFieldMapping mapping)
        {
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

        private async Task<bool> MappingExistsAsync(Guid id)
        {
            return await _context.SurveyFieldMappings.AnyAsync(m => m.Id == id);
        }
    }

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
