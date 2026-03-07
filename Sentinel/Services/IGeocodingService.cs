using System.Threading.Tasks;

namespace Sentinel.Services
{
    public interface IGeocodingService
    {
        /// <summary>
        /// Returns (latitude, longitude) or (null, null) if not found.
        /// </summary>
        Task<(double? Latitude, double? Longitude)> GeocodeAsync(string address);
    }
}