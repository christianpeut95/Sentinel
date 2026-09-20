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
    [Authorize(Policy = "Permission.Location.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocodingService;
        private readonly ILocationDuplicateCheckService _duplicateChecker;

        public CreateModel(ApplicationDbContext context, IGeocodingService geocodingService, ILocationDuplicateCheckService duplicateChecker)
        {
            _context = context;
            _geocodingService = geocodingService;
            _duplicateChecker = duplicateChecker;
        }

        [BindProperty]
        public Location Location { get; set; } = new Location { IsActive = true };

        [BindProperty]
        public bool ConfirmDuplicate { get; set; } = false;

        public List<LocationDuplicate> PotentialDuplicates { get; set; } = new();
        public bool ShowDuplicateWarning { get; set; } = false;

        public SelectList LocationTypesList { get; set; } = default!;
        public SelectList OrganizationsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // The browser is not allowed to supply audit/navigation/geocoding fields.
            // Coordinates are obtained from server-side geocoding of the submitted address.
            var locationToCreate = new Location
            {
                Name = Location.Name,
                LocationTypeId = Location.LocationTypeId,
                Address = Location.Address,
                OrganizationId = Location.OrganizationId,
                IsHighRisk = Location.IsHighRisk,
                IsActive = Location.IsActive,
                Notes = Location.Notes
            };

            // Check for potential duplicates unless user has confirmed
            if (!ConfirmDuplicate)
            {
                PotentialDuplicates = await _duplicateChecker.FindPotentialDuplicatesAsync(locationToCreate);
                
                if (PotentialDuplicates.Any())
                {
                    // Show duplicate warning and stay on page
                    ShowDuplicateWarning = true;
                    await LoadSelectLists();
                    return Page();
                }
            }

            // Geocode address if provided
            if (!string.IsNullOrEmpty(locationToCreate.Address))
            {
                try
                {
                    var result = await _geocodingService.GeocodeAsync(locationToCreate.Address);
                    if (result.Latitude.HasValue && result.Longitude.HasValue)
                    {
                        locationToCreate.Latitude = (decimal)result.Latitude.Value;
                        locationToCreate.Longitude = (decimal)result.Longitude.Value;
                        locationToCreate.GeocodingStatus = "Success";
                    }
                    else
                    {
                        locationToCreate.GeocodingStatus = "Failed";
                    }
                    locationToCreate.LastGeocoded = DateTime.UtcNow;
                }
                catch
                {
                    locationToCreate.GeocodingStatus = "Failed";
                    locationToCreate.LastGeocoded = DateTime.UtcNow;
                }
            }

            try
            {
                _context.Locations.Add(locationToCreate);
                await _context.SaveChangesAsync();

                var geocodeMessage = locationToCreate.Latitude.HasValue
                    ? " (Address geocoded successfully)" 
                    : locationToCreate.GeocodingStatus == "Failed" ? " (Geocoding failed)" : "";

                TempData["SuccessMessage"] = $"Location '{locationToCreate.Name}' created successfully.{geocodeMessage}";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
                await LoadSelectLists();
                return Page();
            }
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
