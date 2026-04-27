using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Services
{
    public class NominatimLocationLookupService : ILocationLookupService
    {
        private readonly HttpClient _http;
        private readonly string? _countryCode;

        public NominatimLocationLookupService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _countryCode = config["Organization:CountryCode"];
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, null);

            var url = $"search?format=json&limit=1&q={Uri.EscapeDataString(address)}";
            
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

        public async Task<List<AddressLookupResult>> SearchAddressesAsync(string query, int limit = 5, double? biasLatitude = null, double? biasLongitude = null)
        {
            var results = new List<AddressLookupResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"search?format=json&limit={limit}&addressdetails=1&q={Uri.EscapeDataString(query)}";

            // Add location bias if provided (prioritize nearby results)
            if (biasLatitude.HasValue && biasLongitude.HasValue)
            {
                url += $"&viewbox={biasLongitude.Value - 0.5},{biasLatitude.Value + 0.5},{biasLongitude.Value + 0.5},{biasLatitude.Value - 0.5}&bounded=0";
            }

            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                url += $"&countrycodes={Uri.EscapeDataString(_countryCode.ToLowerInvariant())}";
            }
            
            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return results;

            using var stream = await resp.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var arr = doc.RootElement;

            foreach (var item in arr.EnumerateArray())
            {
                var result = new AddressLookupResult
                {
                    Display = item.TryGetProperty("display_name", out var dn) ? dn.GetString() : null
                };

                var latStr = item.TryGetProperty("lat", out var latEl) ? latEl.GetString() : null;
                var lonStr = item.TryGetProperty("lon", out var lonEl) ? lonEl.GetString() : null;

                if (double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                    result.Latitude = lat;
                if (double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                    result.Longitude = lon;

                if (item.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in addr.EnumerateObject())
                    {
                        result.AddressComponents[prop.Name] = prop.Value.GetString();
                    }
                }

                results.Add(result);
            }

            return results;
        }

        public async Task<List<PlaceLookupResult>> SearchPlacesAsync(string query, int limit = 5, double? biasLatitude = null, double? biasLongitude = null)
        {
            var results = new List<PlaceLookupResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;

            var url = $"search?format=json&limit={limit}&q={Uri.EscapeDataString(query)}";

            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                url += $"&countrycodes={Uri.EscapeDataString(_countryCode.ToLowerInvariant())}";
            }

            // Add location bias if coordinates provided (Nominatim uses viewbox for biasing)
            if (biasLatitude.HasValue && biasLongitude.HasValue)
            {
                // Create a bounding box around the point (approx 10km radius)
                var latOffset = 0.09; // ~10km
                var lonOffset = 0.09;
                var left = biasLongitude.Value - lonOffset;
                var top = biasLatitude.Value + latOffset;
                var right = biasLongitude.Value + lonOffset;
                var bottom = biasLatitude.Value - latOffset;
                url += $"&viewbox={left},{top},{right},{bottom}&bounded=0"; // bounded=0 allows results outside box
            }

            using var resp = await _http.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return results;

            using var stream = await resp.Content.ReadAsStreamAsync();
            var doc = await JsonDocument.ParseAsync(stream);
            var arr = doc.RootElement;

            foreach (var item in arr.EnumerateArray())
            {
                var result = new PlaceLookupResult
                {
                    Name = item.TryGetProperty("name", out var name) ? name.GetString() : null,
                    Address = item.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                    PlaceId = item.TryGetProperty("place_id", out var pid) ? pid.GetInt64().ToString() : null
                };

                var latStr = item.TryGetProperty("lat", out var latEl) ? latEl.GetString() : null;
                var lonStr = item.TryGetProperty("lon", out var lonEl) ? lonEl.GetString() : null;

                if (double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                    result.Latitude = lat;
                if (double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                    result.Longitude = lon;

                results.Add(result);
            }

            return results;
        }
    }
}
