using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sentinel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("workflow-api")]
    public class LocationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Ok(new object[] { });
                }

                var termLower = term.ToLower();

                var locations = await _context.Locations
                    .Where(l => l.IsActive && 
                               (l.Name.ToLower().Contains(termLower) ||
                                (l.Address != null && l.Address.ToLower().Contains(termLower))))
                    .OrderBy(l => l.Name)
                    .Take(20)
                    .Select(l => new
                    {
                        id = l.Id,
                        name = l.Name,
                        address = l.Address
                    })
                    .ToListAsync();

                return Ok(locations);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in locations search: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var location = await _context.Locations
                .Include(l => l.LocationType)
                .Where(l => l.Id == id && l.IsActive)
                .Select(l => new
                {
                    id = l.Id,
                    name = l.Name,
                    address = l.Address,
                    latitude = l.Latitude,
                    longitude = l.Longitude,
                    locationTypeName = l.LocationType != null ? l.LocationType.Name : null
                })
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }
    }
}
