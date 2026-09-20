using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Locations
{
    [Authorize(Policy = "Permission.Location.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;

        public EditModel(ApplicationDbContext context, IGeocodingService geocodingService)
        {
            _context = context;
            _geocodingService = geocodingService;
        }

        [BindProperty]
        public Location Location { get; set; } = default!;

        public SelectList LocationTypesList { get; set; } = default!;
        public SelectList OrganizationsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations
                .Include(l => l.LocationType)
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null)
            {
                return NotFound();
            }

            Location = location;
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool geocode = false)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Never attach the browser-bound entity as Modified. Use the
            // persisted record for both the address comparison and the update,
            // so audit, navigation and computed geocoding fields cannot be
            // supplied by a crafted form post.
            var locationToUpdate = await _context.Locations
                .FirstOrDefaultAsync(existing => existing.Id == Location.Id);

            if (locationToUpdate == null)
            {
                return NotFound();
            }

            bool addressChanged = locationToUpdate.Address != Location.Address;

            locationToUpdate.Name = Location.Name;
            locationToUpdate.LocationTypeId = Location.LocationTypeId;
            locationToUpdate.Address = Location.Address;
            locationToUpdate.OrganizationId = Location.OrganizationId;
            locationToUpdate.IsHighRisk = Location.IsHighRisk;
            locationToUpdate.IsActive = Location.IsActive;
            locationToUpdate.Notes = Location.Notes;

            // Geocode if address changed or manual re-geocode requested
            if ((addressChanged || geocode) && !string.IsNullOrEmpty(locationToUpdate.Address))
            {
                try
                {
                    var result = await _geocodingService.GeocodeAsync(locationToUpdate.Address);
                    if (result.Latitude.HasValue && result.Longitude.HasValue)
                    {
                        locationToUpdate.Latitude = (decimal)result.Latitude.Value;
                        locationToUpdate.Longitude = (decimal)result.Longitude.Value;
                        locationToUpdate.GeocodingStatus = "Success";
                    }
                    else
                    {
                        locationToUpdate.GeocodingStatus = "Failed";
                    }
                    locationToUpdate.LastGeocoded = DateTime.UtcNow;
                }
                catch
                {
                    locationToUpdate.GeocodingStatus = "Failed";
                    locationToUpdate.LastGeocoded = DateTime.UtcNow;
                }
            }

            try
            {
                await _context.SaveChangesAsync();

                var geocodeMessage = addressChanged && locationToUpdate.Latitude.HasValue 
                    ? " (Address re-geocoded successfully)" 
                    : addressChanged && locationToUpdate.GeocodingStatus == "Failed" ? " (Geocoding failed)" : "";

                TempData["SuccessMessage"] = $"Location '{Location.Name}' updated successfully.{geocodeMessage}";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LocationExists(Location.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
                await LoadSelectLists();
                return Page();
            }
        }

        private bool LocationExists(Guid id)
        {
            return _context.Locations.Any(e => e.Id == id);
        }

        private async Task LoadSelectLists()
        {
            LocationTypesList = new SelectList(
                await _context.LocationTypes.Where(lt => lt.IsActive).OrderBy(lt => lt.DisplayOrder).ToListAsync(),
                "Id", "Name");

            OrganizationsList = new SelectList(
                await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync(),
                "Id", "Name");
        }
    }
}
