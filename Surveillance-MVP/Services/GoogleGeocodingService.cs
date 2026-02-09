using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Surveillance_MVP.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string? _countryCode;

        public GoogleGeocodingService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Geocoding:ApiKey"] ?? string.Empty;
            _countryCode = config["Organization:CountryCode"];
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, null);
            if (string.IsNullOrWhiteSpace(_apiKey)) return (null, null);

            var url = $"geocode/json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(_apiKey)}";
            
            // Add country code bias for more relevant results if configured
            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                url += $"&components=country:{Uri.EscapeDataString(_countryCode)}";
            }
            
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return (null, null);

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;
            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
            {
                var first = results[0];
                if (first.TryGetProperty("geometry", out var geom) && geom.TryGetProperty("location", out var loc))
                {
                    double? lat = null, lon = null;
                    if (loc.TryGetProperty("lat", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                        lat = latEl.GetDouble();
                    if (loc.TryGetProperty("lng", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                        lon = lonEl.GetDouble();
                    return (lat, lon);
                }
            }

            return (null, null);
        }
    }
}
