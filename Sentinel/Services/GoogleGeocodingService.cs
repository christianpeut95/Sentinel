using System.Threading.Tasks;

namespace Sentinel.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private readonly ILocationLookupService _locationLookup;

        public GoogleGeocodingService(ILocationLookupService locationLookup)
        {
            _locationLookup = locationLookup;
        }

        public Task<(double? Latitude, double? Longitude)> GeocodeAsync(string address)
        {
            return _locationLookup.GeocodeAddressAsync(address);
        }
    }
}
