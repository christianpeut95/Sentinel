using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Locations
{
    [Authorize(Policy = "Permission.Location.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;

        public IndexModel(ApplicationDbContext context, IGeocodingService geocodingService)
        {
            _context = context;
            _geocodingService = geocodingService;
        }

        public IList<Location> Locations { get; set; } = default!;
        public IList<LocationType> LocationTypes { get; set; } = default!;
        public Dictionary<Guid, int> EventCounts { get; set; } = new();
        public Dictionary<Guid, int> ExposureCounts { get; set; } = new();
        public int PendingGeocodeCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LocationTypeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? GeocodingStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool HighRiskOnly { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ActiveOnly { get; set; } = true;

        public async Task OnGetAsync()
        {
            // Load location types for filter dropdown
            LocationTypes = await _context.LocationTypes
                .Where(lt => lt.IsActive)
                .OrderBy(lt => lt.DisplayOrder)
                .ThenBy(lt => lt.Name)
                .ToListAsync();

            // Build query
            var query = _context.Locations
                .Include(l => l.LocationType)
                .Include(l => l.Organization)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(l => 
                    l.Name.Contains(SearchTerm) || 
                    (l.Address != null && l.Address.Contains(SearchTerm)));
            }

            if (LocationTypeId.HasValue)
            {
                query = query.Where(l => l.LocationTypeId == LocationTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(GeocodingStatus))
            {
                if (GeocodingStatus == "Success")
                {
                    query = query.Where(l => l.Latitude != null && l.Longitude != null);
                }
                else if (GeocodingStatus == "Failed")
                {
                    query = query.Where(l => l.GeocodingStatus == "Failed");
                }
                else if (GeocodingStatus == "Pending")
                {
                    query = query.Where(l => l.GeocodingStatus == "Pending" || 
                        (l.GeocodingStatus == null && l.Latitude == null && l.Address != null));
                }
            }

            if (HighRiskOnly)
            {
                query = query.Where(l => l.IsHighRisk);
            }

            if (ActiveOnly)
            {
                query = query.Where(l => l.IsActive);
            }

            Locations = await query
                .OrderBy(l => l.Name)
                .ToListAsync();

            // Get usage counts
            var eventCounts = await _context.Events
                .GroupBy(e => e.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToListAsync();
            EventCounts = eventCounts.ToDictionary(x => x.LocationId, x => x.Count);

            var exposureCounts = await _context.ExposureEvents
                .Where(ee => ee.LocationId != null)
                .GroupBy(ee => ee.LocationId!.Value)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToListAsync();
            ExposureCounts = exposureCounts.ToDictionary(x => x.LocationId, x => x.Count);

            // Count pending geocodes
            PendingGeocodeCount = await _context.Locations
                .CountAsync(l => l.IsActive && 
                    !string.IsNullOrEmpty(l.Address) && 
                    l.Latitude == null &&
                    (l.GeocodingStatus == null || l.GeocodingStatus == "Pending"));
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var location = await _context.Locations
                .Include(l => l.Events)
                .Include(l => l.ExposureEvents)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null)
            {
                TempData["ErrorMessage"] = "Location not found.";
                return RedirectToPage();
            }

            // Check for dependencies
            if (location.Events.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{location.Name}' because it has {location.Events.Count} event(s) associated with it.";
                return RedirectToPage();
            }

            if (location.ExposureEvents.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{location.Name}' because it has {location.ExposureEvents.Count} exposure(s) associated with it.";
                return RedirectToPage();
            }

            try
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Location '{location.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the location: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostGeocodeAllAsync()
        {
            var pendingLocations = await _context.Locations
                .Where(l => l.IsActive && 
                    !string.IsNullOrEmpty(l.Address) && 
                    l.Latitude == null)
                .ToListAsync();

            int successCount = 0;
            int failCount = 0;

            foreach (var location in pendingLocations)
            {
                try
                {
                    var result = await _geocodingService.GeocodeAsync(location.Address!);
                    location.Latitude = result.Latitude.HasValue ? (decimal)result.Latitude.Value : null;
                    location.Longitude = result.Longitude.HasValue ? (decimal)result.Longitude.Value : null;
                    location.GeocodingStatus = result.Latitude.HasValue ? "Success" : "Failed";
                    location.LastGeocoded = DateTime.UtcNow;

                    if (result.Latitude.HasValue && result.Longitude.HasValue)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                catch
                {
                    location.GeocodingStatus = "Failed";
                    location.LastGeocoded = DateTime.UtcNow;
                    failCount++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Geocoding completed: {successCount} successful, {failCount} failed.";
            return RedirectToPage();
        }
    }
}
