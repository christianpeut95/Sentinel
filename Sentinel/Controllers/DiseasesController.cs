using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sentinel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("workflow-api")]
    public class DiseasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public DiseasesController(ApplicationDbContext context, IDiseaseAccessService diseaseAccessService)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? term = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new object[] { });
                }

                // Get accessible diseases for the current user
                var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

                var query = _context.Diseases
                    .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id));

                // Apply search term filter if provided
                if (!string.IsNullOrWhiteSpace(term) && term.Length >= 2)
                {
                    var termLower = term.ToLower();
                    query = query.Where(d => 
                        d.Name.ToLower().Contains(termLower) ||
                        (d.Code != null && d.Code.ToLower().Contains(termLower)));
                }

                var diseases = await query
                    .OrderBy(d => d.Level)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .Take(20)
                    .Select(d => new
                    {
                        id = d.Id,
                        name = d.Name,
                        code = d.Code,
                        level = d.Level
                    })
                    .ToListAsync();

                return Ok(diseases);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in disease search: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var disease = await _context.Diseases
                .Where(d => d.Id == id && d.IsActive)
                .Select(d => new
                {
                    id = d.Id,
                    name = d.Name,
                    code = d.Code,
                    level = d.Level,
                    exposureTrackingMode = d.ExposureTrackingMode
                })
                .FirstOrDefaultAsync();

            if (disease == null)
            {
                return NotFound();
            }

            return Ok(disease);
        }
    }
}
