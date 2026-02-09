using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Locations
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

            // Get original location to check if address changed
            var originalLocation = await _context.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == Location.Id);

            if (originalLocation == null)
            {
                return NotFound();
            }

            // Geocode if address changed or manual re-geocode requested
            bool addressChanged = originalLocation.Address != Location.Address;
            if ((addressChanged || geocode) && !string.IsNullOrEmpty(Location.Address))
            {
                try
                {
                    var result = await _geocodingService.GeocodeAsync(Location.Address);
                    if (result.Latitude.HasValue && result.Longitude.HasValue)
                    {
                        Location.Latitude = (decimal)result.Latitude.Value;
                        Location.Longitude = (decimal)result.Longitude.Value;
                        Location.GeocodingStatus = "Success";
                    }
                    else
                    {
                        Location.GeocodingStatus = "Failed";
                    }
                    Location.LastGeocoded = DateTime.UtcNow;
                }
                catch
                {
                    Location.GeocodingStatus = "Failed";
                    Location.LastGeocoded = DateTime.UtcNow;
                }
            }

            _context.Attach(Location).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                var geocodeMessage = addressChanged && Location.Latitude.HasValue 
                    ? " (Address re-geocoded successfully)" 
                    : addressChanged && Location.GeocodingStatus == "Failed" ? " (Geocoding failed)" : "";

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
                TempData["ErrorMessage"] = $"An error occurred while updating the location: {ex.Message}";
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
