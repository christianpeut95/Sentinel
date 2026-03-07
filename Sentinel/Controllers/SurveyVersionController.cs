using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("workflow-api-moderate")] // 60 per minute - survey versioning
    public class SurveyVersionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurveyVersionController> _logger;

        public SurveyVersionController(ApplicationDbContext context, ILogger<SurveyVersionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("SaveAsNewVersion")]
        public async Task<IActionResult> SaveAsNewVersion([FromBody] SaveAsVersionRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to create new version from parent survey {ParentSurveyId}", request.ParentSurveyId);
                
                // Validate request
                if (request.ParentSurveyId == Guid.Empty)
                {
                    _logger.LogWarning("Attempted to create version with empty ParentSurveyId");
                    return BadRequest("Cannot create a version from an unsaved survey. Please save the survey first.");
                }
                
                // Get the survey to version from
                var sourceSurvey = await _context.SurveyTemplates
                    .FirstOrDefaultAsync(st => st.Id == request.ParentSurveyId);

                if (sourceSurvey == null)
                {
                    _logger.LogWarning("Source survey {ParentSurveyId} not found in database", request.ParentSurveyId);
                    return NotFound($"Source survey not found. The survey may have been deleted or the ID is invalid.");
                }

                // Determine root parent: if source has a parent, use that; otherwise source IS the root
                var rootParentId = sourceSurvey.ParentSurveyTemplateId ?? sourceSurvey.Id;

                // Check if version number already exists in this survey family
                var existingVersion = await _context.SurveyTemplates
                    .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                    .AnyAsync(st => st.VersionNumber == request.VersionNumber);

                if (existingVersion)
                    return BadRequest($"Version {request.VersionNumber} already exists");

                // Validate survey JSON
                if (string.IsNullOrWhiteSpace(request.SurveyDefinitionJson))
                    return BadRequest("Survey definition is required");

                // Create new version
                var newVersion = new SurveyTemplate
                {
                    Id = Guid.NewGuid(),
                    ParentSurveyTemplateId = rootParentId, // Link to root parent
                    Name = sourceSurvey.Name, // Inherit name
                    Description = sourceSurvey.Description,
                    Category = sourceSurvey.Category,
                    Tags = sourceSurvey.Tags,
                    VersionNumber = request.VersionNumber,
                    VersionStatus = request.PublishImmediately ?
                        SurveyVersionStatus.Active :
                        SurveyVersionStatus.Draft,
                    VersionNotes = request.VersionNotes,
                    SurveyDefinitionJson = request.SurveyDefinitionJson,
                    DefaultInputMappingJson = request.InputMappingJson,
                    DefaultOutputMappingJson = request.OutputMappingJson,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name,
                    IsActive = true
                };

                // If publishing immediately, archive current active version
                if (request.PublishImmediately)
                {
                    var currentActive = await _context.SurveyTemplates
                        .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                        .FirstOrDefaultAsync(st => st.VersionStatus == SurveyVersionStatus.Active);

                    if (currentActive != null)
                    {
                        currentActive.VersionStatus = SurveyVersionStatus.Archived;
                    }

                    newVersion.PublishedAt = DateTime.UtcNow;
                    newVersion.PublishedBy = User.Identity?.Name;
                }

                _context.SurveyTemplates.Add(newVersion);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Created survey version {VersionNumber} from source {SourceId}, root parent {RootParentId}, Status: {Status}",
                    request.VersionNumber, request.ParentSurveyId, rootParentId, newVersion.VersionStatus);

                return Ok(new { newVersionId = newVersion.Id, versionNumber = newVersion.VersionNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new survey version");
                return StatusCode(500, "Error creating new version: " + ex.Message);
            }
        }

        [HttpPost("PublishVersion/{id}")]
        public async Task<IActionResult> PublishVersion(Guid id)
        {
            try
            {
                var version = await _context.SurveyTemplates
                    .FirstOrDefaultAsync(st => st.Id == id);

                if (version == null)
                    return NotFound();

                if (version.VersionStatus == SurveyVersionStatus.Active)
                    return BadRequest("Version is already active");

                // Find root parent
                var rootParentId = version.ParentSurveyTemplateId ?? version.Id;

                // Archive current active version
                var currentActive = await _context.SurveyTemplates
                    .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                    .FirstOrDefaultAsync(st => st.VersionStatus == SurveyVersionStatus.Active);

                if (currentActive != null)
                {
                    currentActive.VersionStatus = SurveyVersionStatus.Archived;
                }

                // Activate this version
                version.VersionStatus = SurveyVersionStatus.Active;
                version.PublishedAt = DateTime.UtcNow;
                version.PublishedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Published survey version {VersionNumber} (Id: {Id})",
                    version.VersionNumber, id);

                return Ok(new { message = "Version published successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing version {VersionId}", id);
                return StatusCode(500, "Error publishing version: " + ex.Message);
            }
        }

        [HttpPost("ArchiveVersion/{id}")]
        public async Task<IActionResult> ArchiveVersion(Guid id)
        {
            try
            {
                var version = await _context.SurveyTemplates
                    .FirstOrDefaultAsync(st => st.Id == id);

                if (version == null)
                    return NotFound();

                if (version.VersionStatus == SurveyVersionStatus.Archived)
                    return BadRequest("Version is already archived");

                if (version.VersionStatus == SurveyVersionStatus.Active)
                    return BadRequest("Cannot archive active version. Publish another version first.");

                version.VersionStatus = SurveyVersionStatus.Archived;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Archived survey version {VersionId}", id);

                return Ok(new { message = "Version archived successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving version {VersionId}", id);
                return StatusCode(500, "Error archiving version: " + ex.Message);
            }
        }

        [HttpGet("GetVersions/{surveyId}")]
        public async Task<IActionResult> GetVersions(Guid surveyId)
        {
            try
            {
                // Get all versions for this survey family
                var survey = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == surveyId);

                if (survey == null)
                    return NotFound();

                var rootParentId = survey.ParentSurveyTemplateId ?? survey.Id;

                var versions = await _context.SurveyTemplates
                    .AsNoTracking()
                    .Where(st => st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId)
                    .OrderByDescending(st => st.CreatedAt)
                    .Select(st => new
                    {
                        st.Id,
                        st.VersionNumber,
                        st.VersionStatus,
                        st.VersionNotes,
                        st.CreatedAt,
                        st.CreatedBy,
                        st.PublishedAt,
                        st.PublishedBy,
                        st.ModifiedAt,
                        st.ModifiedBy
                    })
                    .ToListAsync();

                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching versions for survey {SurveyId}", surveyId);
                return StatusCode(500, "Error fetching versions");
            }
        }
    }

    public class SaveAsVersionRequest
    {
        public Guid ParentSurveyId { get; set; }
        public string VersionNumber { get; set; } = string.Empty;
        public string? VersionNotes { get; set; }
        public bool PublishImmediately { get; set; }
        public string SurveyDefinitionJson { get; set; } = string.Empty;
        public string? InputMappingJson { get; set; }
        public string? OutputMappingJson { get; set; }
    }
}
