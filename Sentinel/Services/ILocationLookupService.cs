using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface ILocationLookupService
    {
        /// <summary>
        /// Returns (latitude, longitude) or (null, null) if not found.
        /// </summary>
        Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address);

        /// <summary>
        /// Search for address/place suggestions with detailed information.
        /// Returns a list of results with display text, coordinates, and address components.
        /// </summary>
        Task<List<AddressLookupResult>> SearchAddressesAsync(string query, int limit = 5);

        /// <summary>
        /// Search for business/place suggestions.
        /// Returns a list of results with business name, address, and coordinates.
        /// </summary>
        Task<List<PlaceLookupResult>> SearchPlacesAsync(string query, int limit = 5);
    }

    public class AddressLookupResult
    {
        public string? Display { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public Dictionary<string, string?> AddressComponents { get; set; } = new();
    }

    public class PlaceLookupResult
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? PlaceId { get; set; }
    }
}
