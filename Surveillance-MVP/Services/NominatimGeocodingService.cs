using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Surveillance_MVP.Services
{
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _http;
        private readonly string? _countryCode;

        public NominatimGeocodingService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _countryCode = config["Organization:CountryCode"];
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, null);

            var url = $"search?format=json&limit=1&q={Uri.EscapeDataString(address)}";
            
            // Add country code bias for more relevant results if configured
            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                url += $"&countrycodes={Uri.EscapeDataString(_countryCode.ToLowerInvariant())}";
            }
            
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return (null, null);

            using var stream = await resp.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var arr = doc.RootElement;
            if (arr.GetArrayLength() == 0) return (null, null);

            var first = arr[0];
            var latStr = first.GetProperty("lat").GetString();
            var lonStr = first.GetProperty("lon").GetString();

            if (double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }

            return (null, null);
        }
    }
}