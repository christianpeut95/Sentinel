using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;

namespace Surveillance_MVP.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
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
    }
}
