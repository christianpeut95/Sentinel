using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Models.Timeline;
using Sentinel.Services;

namespace Sentinel.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/timeline")]
    public class TimelineEntryApiController : ControllerBase
    {
        private readonly INaturalLanguageParserService _parserService;
        private readonly ITimelineStorageService _storageService;
        private readonly IEntityMemoryService _memoryService;
        private readonly ILogger<TimelineEntryApiController> _logger;

        public TimelineEntryApiController(
            INaturalLanguageParserService parserService,
            ITimelineStorageService storageService,
            IEntityMemoryService memoryService,
            ILogger<TimelineEntryApiController> logger)
        {
            _parserService = parserService;
            _storageService = storageService;
            _memoryService = memoryService;
            _logger = logger;
        }

        /// <summary>
        /// Parse narrative text and extract entities
        /// </summary>
        [HttpPost("parse")]
        public IActionResult ParseNarrative([FromBody] ParseRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return Ok(new ParseResponse { Entities = new List<ExtractedEntity>() });
                }

                var entities = _parserService.ExtractEntities(request.Text);
                var relationships = _parserService.DetectRelationships(request.Text, entities);
                var uncertaintyMarkers = _parserService.DetectUncertaintyMarkers(request.Text);
                var protectiveMeasures = _parserService.DetectProtectiveMeasures(request.Text);
                var isMemoryGap = _parserService.IsMemoryGap(request.Text);

                TimelineCorrection? correction = null;
                if (!string.IsNullOrWhiteSpace(request.PreviousText))
                {
                    correction = _parserService.DetectCorrection(request.Text, request.PreviousText);
                }

                return Ok(new ParseResponse
                {
                    Entities = entities,
                    Relationships = relationships,
                    UncertaintyMarkers = uncertaintyMarkers,
                    ProtectiveMeasures = protectiveMeasures,
                    IsMemoryGap = isMemoryGap,
                    Correction = correction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing narrative text");
                return StatusCode(500, new { error = "Error parsing text" });
            }
        }

        /// <summary>
        /// Get entity memory (known people/locations) for autocomplete
        /// </summary>
        [HttpGet("memory/{caseId}")]
        public async Task<IActionResult> GetEntityMemory(Guid caseId)
        {
            try
            {
                var people = await _memoryService.GetKnownPeopleAsync(caseId);
                var locations = await _memoryService.GetKnownLocationsAsync(caseId);
                var conventions = await _memoryService.GetConventionsAsync(caseId);

                return Ok(new EntityMemoryResponse
                {
                    People = people,
                    Locations = locations,
                    Conventions = conventions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity memory for case {CaseId}", caseId);
                return StatusCode(500, new { error = "Error loading memory" });
            }
        }

        /// <summary>
        /// Get timeline data for a case
        /// </summary>
        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetTimeline(Guid caseId)
        {
            try
            {
                var timeline = await _storageService.LoadTimelineAsync(caseId);
                
                if (timeline == null)
                {
                    return Ok(new CaseTimelineData { CaseId = caseId });
                }

                return Ok(timeline);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading timeline for case {CaseId}", caseId);
                return StatusCode(500, new { error = "Error loading timeline" });
            }
        }

        /// <summary>
        /// Save timeline data
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveTimeline([FromBody] CaseTimelineData timelineData)
        {
            try
            {
                if (timelineData == null || timelineData.CaseId == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid timeline data" });
                }

                await _storageService.SaveTimelineAsync(timelineData);
                _memoryService.ClearCache(timelineData.CaseId);

                _logger.LogInformation("Timeline saved for case {CaseId}", timelineData.CaseId);
                
                return Ok(new { success = true, version = timelineData.Version });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving timeline for case {CaseId}", timelineData.CaseId);
                return StatusCode(500, new { error = "Error saving timeline" });
            }
        }

        /// <summary>
        /// Copy entries from one day to another
        /// </summary>
        [HttpPost("copy-day")]
        public async Task<IActionResult> CopyDay([FromBody] CopyDayRequest request)
        {
            try
            {
                var timeline = await _storageService.LoadTimelineAsync(request.CaseId);
                
                if (timeline == null)
                {
                    return NotFound(new { error = "Timeline not found" });
                }

                var sourceEntry = timeline.Entries.FirstOrDefault(e => e.EntryDate.Date == request.SourceDate.Date);
                
                if (sourceEntry == null)
                {
                    return NotFound(new { error = "Source date entry not found" });
                }

                // Create a copy of the entry with a new date
                var copiedEntry = new TimelineEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    EntryDate = request.TargetDate,
                    NarrativeText = sourceEntry.NarrativeText,
                    Entities = sourceEntry.Entities.Select(e => new ExtractedEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        EntityType = e.EntityType,
                        RawText = e.RawText,
                        NormalizedValue = e.NormalizedValue,
                        StartPosition = e.StartPosition,
                        EndPosition = e.EndPosition,
                        Confidence = e.Confidence,
                        IsConfirmed = e.IsConfirmed,
                        LinkedRecordType = e.LinkedRecordType,
                        LinkedRecordId = e.LinkedRecordId,
                        LinkedRecordDisplayName = e.LinkedRecordDisplayName,
                        Suggestions = new List<EntitySuggestion>(e.Suggestions),
                        Metadata = e.Metadata != null ? new Dictionary<string, object>(e.Metadata) : null,
                        Notes = e.Notes
                    }).ToList(),
                    Relationships = sourceEntry.Relationships.ToList(),
                    UncertaintyMarkers = new List<string>(sourceEntry.UncertaintyMarkers),
                    ProtectiveMeasures = new List<string>(sourceEntry.ProtectiveMeasures)
                };

                return Ok(copiedEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying day for case {CaseId}", request.CaseId);
                return StatusCode(500, new { error = "Error copying day" });
            }
        }

        /// <summary>
        /// Add or update a convention location
        /// </summary>
        [HttpPost("convention")]
        public async Task<IActionResult> AddConvention([FromBody] AddConventionRequest request)
        {
            try
            {
                await _memoryService.AddConventionAsync(
                    request.CaseId, 
                    request.ConventionName, 
                    request.Location);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding convention for case {CaseId}", request.CaseId);
                return StatusCode(500, new { error = "Error adding convention" });
            }
        }

        /// <summary>
        /// Delete timeline data
        /// </summary>
        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteTimeline(Guid caseId)
        {
            try
            {
                await _storageService.DeleteTimelineAsync(caseId);
                _memoryService.ClearCache(caseId);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting timeline for case {CaseId}", caseId);
                return StatusCode(500, new { error = "Error deleting timeline" });
            }
        }
    }

    // Request/Response DTOs
    public class ParseRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? PreviousText { get; set; }
    }

    public class ParseResponse
    {
        public List<ExtractedEntity> Entities { get; set; } = new();
        public List<EntityRelationship> Relationships { get; set; } = new();
        public List<string> UncertaintyMarkers { get; set; } = new();
        public List<string> ProtectiveMeasures { get; set; } = new();
        public bool IsMemoryGap { get; set; }
        public TimelineCorrection? Correction { get; set; }
    }

    public class EntityMemoryResponse
    {
        public List<EntitySuggestion> People { get; set; } = new();
        public List<EntitySuggestion> Locations { get; set; } = new();
        public Dictionary<string, ConventionLocation> Conventions { get; set; } = new();
    }

    public class CopyDayRequest
    {
        public Guid CaseId { get; set; }
        public DateTime SourceDate { get; set; }
        public DateTime TargetDate { get; set; }
    }

    public class AddConventionRequest
    {
        public Guid CaseId { get; set; }
        public string ConventionName { get; set; } = string.Empty;
        public ConventionLocation Location { get; set; } = new();
    }
}
