using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Sentinel.Pages.Settings.Mappings
{
    [Authorize(Policy = "Permission.Survey.Edit")]
    [Route("Settings/Mappings")]
    [EnableRateLimiting("lookup-api")] // 200 per minute - mostly metadata, config saves less frequent
    public class SaveCollectionConfigController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICollectionMappingService _collectionService;
        private readonly CollectionMappingValidationService _validationService;

        public SaveCollectionConfigController(
            ApplicationDbContext context,
            ICollectionMappingService collectionService,
            CollectionMappingValidationService validationService)
        {
            _context = context;
            _collectionService = collectionService;
            _validationService = validationService;
        }

        [HttpPost("SaveCollectionConfig")]
        public async Task<IActionResult> SaveCollectionConfig([FromBody] SaveConfigRequest request)
        {
            if (request.MappingId == Guid.Empty)
                return BadRequest(new { error = "Mapping ID is required. Please save the mapping first before configuring collection settings." });

            var mapping = await _context.SurveyFieldMappings
                .FirstOrDefaultAsync(m => m.Id == request.MappingId);

            if (mapping == null)
                return NotFound(new { error = $"Mapping not found with ID: {request.MappingId}" });

            // ? VALIDATE CONFIG BEFORE SAVING
            try
            {
                var config = JsonSerializer.Deserialize<CollectionMappingConfig>(request.ConfigJson);
                if (config != null)
                {
                    var validationResult = _validationService.ValidateConfig(config);
                    
                    if (!validationResult.IsValid)
                    {
                        return BadRequest(new 
                        { 
                            error = "Configuration validation failed",
                            errors = validationResult.Errors,
                            warnings = validationResult.Warnings,
                            suggestions = validationResult.Suggestions,
                            summary = validationResult.GetSummary()
                        });
                    }
                    
                    // Show warnings but allow save
                    if (validationResult.Warnings.Any())
                    {
                        mapping.CollectionConfigJson = request.ConfigJson;
                        mapping.LastModified = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        return Ok(new 
                        { 
                            success = true, 
                            message = "Configuration saved with warnings!",
                            warnings = validationResult.Warnings,
                            suggestions = validationResult.Suggestions
                        });
                    }
                }
            }
            catch (JsonException jsonEx)
            {
                return BadRequest(new { error = $"Invalid JSON format: {jsonEx.Message}" });
            }

            mapping.CollectionConfigJson = request.ConfigJson;
            mapping.LastModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Configuration saved successfully!" });
        }

        [HttpGet("GetEntityFields/{entityType}")]
        public async Task<IActionResult> GetEntityFields(string entityType)
        {
            try
            {
                var fields = await _collectionService.GetEntityFieldsAsync(entityType);
                return Ok(fields);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("GetContactClassifications")]
        public async Task<IActionResult> GetContactClassifications()
        {
            try
            {
                var classifications = await _context.ContactClassifications
                    .Where(cc => cc.IsActive)
                    .OrderBy(cc => cc.DisplayOrder)
                    .Select(cc => new { id = cc.Id, name = cc.Name })
                    .ToListAsync();
                
                return Ok(classifications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SaveConfigRequest
    {
        public Guid MappingId { get; set; }
        public string ConfigJson { get; set; } = string.Empty;
    }
}

