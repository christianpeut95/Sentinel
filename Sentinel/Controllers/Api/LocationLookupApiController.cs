using Microsoft.AspNetCore.Mvc;
using Sentinel.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Sentinel.Controllers.Api
{
    [ApiController]
    [Route("api/location-lookup")]
    public class LocationLookupApiController : ControllerBase
    {
        private readonly ILocationLookupService _locationLookup;

        public LocationLookupApiController(ILocationLookupService locationLookup)
        {
            _locationLookup = locationLookup;
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchLocations(
            [FromQuery] string query,
            [FromQuery] double? lat,
            [FromQuery] double? lng)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(new List<object>());
            }

            // Search BOTH places (businesses, venues) AND addresses (street addresses)
            // Location bias (lat/lng) prioritizes nearby results when available
            var placesTask = _locationLookup.SearchPlacesAsync(
                query, 
                limit: 5, 
                biasLatitude: lat, 
                biasLongitude: lng);

            var addressesTask = _locationLookup.SearchAddressesAsync(
                query, 
                limit: 5, 
                biasLatitude: lat, 
                biasLongitude: lng);

            await Task.WhenAll(placesTask, addressesTask);

            var places = placesTask.Result;
            var addresses = addressesTask.Result;

            // Map places to common format
            var mappedPlaces = places.Select(r => new
            {
                name = r.Name,
                formattedAddress = r.Address,
                latitude = r.Latitude,
                longitude = r.Longitude,
                placeId = r.PlaceId,
                type = "place",
                // Places don't have detailed address components
                city = (string?)null,
                state = (string?)null,
                postalCode = (string?)null,
                country = (string?)null
            });

            // Map addresses to common format
            var mappedAddresses = addresses.Select(r => new
            {
                name = r.Display,
                formattedAddress = r.Display,
                latitude = r.Latitude,
                longitude = r.Longitude,
                placeId = (string?)null,
                type = "address",
                // Extract address components
                city = r.AddressComponents.GetValueOrDefault("city"),
                state = r.AddressComponents.GetValueOrDefault("state"),
                postalCode = r.AddressComponents.GetValueOrDefault("postalCode"),
                country = r.AddressComponents.GetValueOrDefault("country")
            });

            // Combine results: places first, then addresses
            var combinedResults = mappedPlaces.Concat(mappedAddresses).Take(8);

            return Ok(combinedResults);
        }
    }
}
