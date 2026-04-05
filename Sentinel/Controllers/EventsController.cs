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
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? term = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Ok(new object[] { });
                }

                var termLower = term.ToLower();

                var events = await _context.Events
                    .Include(e => e.Location)
                    .Where(e => e.IsActive && 
                               (e.Name.ToLower().Contains(termLower) ||
                                (e.Location != null && e.Location.Name.ToLower().Contains(termLower))))
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .Select(e => new
                    {
                        id = e.Id,
                        name = e.Name,
                        location = e.Location != null ? e.Location.Name : null,
                        date = e.StartDateTime.ToString("dd MMM yyyy")
                    })
                    .ToListAsync();

                return Ok(events);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in events search: {ex.Message}");
                return Ok(new object[] { });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Location)
                .Where(e => e.Id == id && e.IsActive)
                .Select(e => new
                {
                    id = e.Id,
                    name = e.Name,
                    locationId = e.LocationId,
                    locationName = e.Location != null ? e.Location.Name : null,
                    date = e.StartDateTime,
                    description = e.Description
                })
                .FirstOrDefaultAsync();

            if (eventItem == null)
            {
                return NotFound();
            }

            return Ok(eventItem);
        }
    }
}
