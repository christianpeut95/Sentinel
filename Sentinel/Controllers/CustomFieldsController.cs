using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("lookup-api")] // 200 per minute - metadata/lookup data
    public class CustomFieldsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomFieldsController> _logger;

        public CustomFieldsController(ApplicationDbContext context, ILogger<CustomFieldsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("GetAvailableFields")]
        public async Task<IActionResult> GetAvailableFields()
        {
            try
            {
                // Get all custom field definitions that are associated with active diseases
                var customFields = await _context.DiseaseCustomFields
                    .Include(dcf => dcf.CustomFieldDefinition)
                    .Include(dcf => dcf.Disease)
                    .Where(dcf => dcf.Disease.IsActive && dcf.CustomFieldDefinition.IsActive)
                    .Select(dcf => new
                    {
                        name = dcf.CustomFieldDefinition.Name,
                        description = dcf.CustomFieldDefinition.Label,
                        diseaseName = dcf.Disease.Name,
                        fieldType = dcf.CustomFieldDefinition.FieldType.ToString()
                    })
                    .Distinct()
                    .OrderBy(x => x.name)
                    .ToListAsync();

                return Ok(customFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom fields");
                return StatusCode(500, "Error fetching custom fields");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomField(int id)
        {
            try
            {
                var customField = await _context.CustomFieldDefinitions
                    .Include(cf => cf.LookupTable)
                        .ThenInclude(lt => lt.Values.Where(v => v.IsActive))
                    .FirstOrDefaultAsync(cf => cf.Id == id && cf.IsActive);

                if (customField == null)
                    return NotFound();

                return Ok(new
                {
                    id = customField.Id,
                    name = customField.Name,
                    label = customField.Label,
                    fieldType = customField.FieldType.ToString(),
                    lookupTableId = customField.LookupTableId,
                    hasLookupTable = customField.LookupTableId.HasValue,
                    lookupValues = customField.LookupTable?.Values
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new
                        {
                            id = v.Id,
                            value = v.Value,
                            displayText = v.DisplayText
                        })
                        .ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom field {CustomFieldId}", id);
                return StatusCode(500, "Error fetching custom field");
            }
        }

        [HttpGet("ForCaseDefinitions")]
        public async Task<IActionResult> GetCustomFieldsForCaseDefinitions()
        {
            try
            {
                var customFields = await _context.CustomFieldDefinitions
                    .Where(cf => cf.IsActive && cf.ShowOnCaseForm)
                    .OrderBy(cf => cf.DisplayOrder)
                    .ThenBy(cf => cf.Label)
                    .Select(cf => new
                    {
                        id = cf.Id,
                        name = cf.Name,
                        label = cf.Label,
                        category = cf.Category,
                        fieldType = cf.FieldType.ToString(),
                        hasLookupTable = cf.LookupTableId.HasValue
                    })
                    .ToListAsync();

                return Ok(customFields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching custom fields for case definitions");
                return StatusCode(500, "Error fetching custom fields");
            }
        }
    }
}
