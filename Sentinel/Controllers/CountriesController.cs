using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sentinel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Permission.ReferenceData.View")]
    [EnableRateLimiting("workflow-api")]
    public class CountriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ApplicationDbContext context, ILogger<CountriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? term = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return Ok(await GetCommonCountries());
                }

                var termLower = term.ToLower();

                var countries = await _context.Countries
                    .Where(c => c.IsActive && 
                               (c.Name.ToLower().Contains(termLower) ||
                                c.Code.ToLower().Contains(termLower)))
                    .OrderBy(c => c.Name)
                    .Take(20)
                    .Select(c => new
                    {
                        id = c.Code,
                        text = c.Name,
                        code = c.Code
                    })
                    .ToListAsync();

                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Country search failed");
                return Ok(new object[] { });
            }
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetByCode(string code)
        {
            var country = await _context.Countries
                .Where(c => c.Code == code.ToUpper() && c.IsActive)
                .Select(c => new
                {
                    id = c.Code,
                    text = c.Name,
                    code = c.Code
                })
                .FirstOrDefaultAsync();

            if (country == null)
            {
                return NotFound();
            }

            return Ok(country);
        }

        private async Task<object[]> GetCommonCountries()
        {
            var commonCountryCodes = new[] { "US", "CA", "MX", "GB", "FR", "DE", "IT", "ES", "JP", "CN", "IN", "BR", "AU" };
            
            return await _context.Countries
                .Where(c => c.IsActive && commonCountryCodes.Contains(c.Code))
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Code,
                    text = c.Name,
                    code = c.Code
                })
                .ToArrayAsync();
        }
    }
}
