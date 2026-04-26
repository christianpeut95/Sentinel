using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Services
{
    public class GoogleLocationLookupService : ILocationLookupService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string? _countryCode;

        public GoogleLocationLookupService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _apiKey = config["Geocoding:ApiKey"] ?? string.Empty;
            _countryCode = config["Organization:CountryCode"];
        }

        public async Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(string address)
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

        public async Task<List<AddressLookupResult>> SearchAddressesAsync(string query, int limit = 5, double? biasLatitude = null, double? biasLongitude = null)
        {
            var results = new List<AddressLookupResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;
            if (string.IsNullOrWhiteSpace(_apiKey)) return results;

            // 1) Place Autocomplete to get place_ids
            var autoUrl = $"place/autocomplete/json?input={Uri.EscapeDataString(query)}&types=address&key={Uri.EscapeDataString(_apiKey)}";

            // Add location bias if provided (prioritize nearby results)
            if (biasLatitude.HasValue && biasLongitude.HasValue)
            {
                autoUrl += $"&location={biasLatitude.Value.ToString(CultureInfo.InvariantCulture)},{biasLongitude.Value.ToString(CultureInfo.InvariantCulture)}&radius=50000";
            }

            // Add country code bias if configured
            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                autoUrl += $"&components=country:{Uri.EscapeDataString(_countryCode)}";
            }
            
            var autoResp = await _http.GetAsync(autoUrl);
            if (!autoResp.IsSuccessStatusCode)
                return results;

            var autoBody = await autoResp.Content.ReadAsStringAsync();
            using var autoDoc = JsonDocument.Parse(autoBody);
            var preds = autoDoc.RootElement.TryGetProperty("predictions", out var predEl) ? predEl : default;

            if (preds.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var p in preds.EnumerateArray())
                {
                    if (i++ >= limit) break;
                    var placeId = p.GetProperty("place_id").GetString();
                    if (string.IsNullOrWhiteSpace(placeId)) continue;

                    // 2) Place Details to get geometry and address components
                    var detailsUrl = $"place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=formatted_address,geometry,address_component&key={Uri.EscapeDataString(_apiKey)}";
                    var detResp = await _http.GetAsync(detailsUrl);
                    if (!detResp.IsSuccessStatusCode) continue;

                    var detBody = await detResp.Content.ReadAsStringAsync();
                    using var detDoc = JsonDocument.Parse(detBody);
                    var root = detDoc.RootElement;
                    if (!root.TryGetProperty("result", out var res)) continue;

                    var result = new AddressLookupResult
                    {
                        Display = res.TryGetProperty("formatted_address", out var fa) ? fa.GetString() : null
                    };

                    // Extract coordinates
                    if (res.TryGetProperty("geometry", out var geom) && geom.TryGetProperty("location", out var loc))
                    {
                        if (loc.TryGetProperty("lat", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                            result.Latitude = latEl.GetDouble();
                        if (loc.TryGetProperty("lng", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                            result.Longitude = lonEl.GetDouble();
                    }

                    // Extract address components
                    if (res.TryGetProperty("address_components", out var ac) && ac.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var comp in ac.EnumerateArray())
                        {
                            var longName = comp.TryGetProperty("long_name", out var ln) ? ln.GetString() : null;
                            if (comp.TryGetProperty("types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var t in typesEl.EnumerateArray())
                                {
                                    var type = t.GetString();
                                    if (string.IsNullOrWhiteSpace(type)) continue;
                                    // prefer the first value for a type
                                    if (!result.AddressComponents.ContainsKey(type))
                                        result.AddressComponents[type] = longName;
                                }
                            }
                        }
                    }

                    results.Add(result);
                }
            }

            return results;
        }

        public async Task<List<PlaceLookupResult>> SearchPlacesAsync(string query, int limit = 5, double? biasLatitude = null, double? biasLongitude = null)
        {
            var results = new List<PlaceLookupResult>();

            if (string.IsNullOrWhiteSpace(query)) return results;
            if (string.IsNullOrWhiteSpace(_apiKey)) return results;

            // 1) Place Autocomplete for establishments/businesses
            var autoUrl = $"place/autocomplete/json?input={Uri.EscapeDataString(query)}&types=establishment&key={Uri.EscapeDataString(_apiKey)}";

            // Add country code bias if configured
            if (!string.IsNullOrWhiteSpace(_countryCode))
            {
                autoUrl += $"&components=country:{Uri.EscapeDataString(_countryCode)}";
            }

            // Add location bias if coordinates provided (bias results near patient's address)
            if (biasLatitude.HasValue && biasLongitude.HasValue)
            {
                autoUrl += $"&location={biasLatitude.Value},{biasLongitude.Value}&radius=10000"; // 10km radius
            }

            var autoResp = await _http.GetAsync(autoUrl);
            if (!autoResp.IsSuccessStatusCode)
                return results;

            var autoBody = await autoResp.Content.ReadAsStringAsync();
            using var autoDoc = JsonDocument.Parse(autoBody);
            var preds = autoDoc.RootElement.TryGetProperty("predictions", out var predEl) ? predEl : default;

            if (preds.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var p in preds.EnumerateArray())
                {
                    if (i++ >= limit) break;
                    var placeId = p.GetProperty("place_id").GetString();
                    if (string.IsNullOrWhiteSpace(placeId)) continue;

                    // 2) Place Details to get full information
                    var detailsUrl = $"place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=name,formatted_address,geometry&key={Uri.EscapeDataString(_apiKey)}";
                    var detResp = await _http.GetAsync(detailsUrl);
                    if (!detResp.IsSuccessStatusCode) continue;

                    var detBody = await detResp.Content.ReadAsStringAsync();
                    using var detDoc = JsonDocument.Parse(detBody);
                    var root = detDoc.RootElement;
                    if (!root.TryGetProperty("result", out var res)) continue;

                    var result = new PlaceLookupResult
                    {
                        PlaceId = placeId,
                        Name = res.TryGetProperty("name", out var name) ? name.GetString() : null,
                        Address = res.TryGetProperty("formatted_address", out var fa) ? fa.GetString() : null
                    };

                    // Extract coordinates
                    if (res.TryGetProperty("geometry", out var geom) && geom.TryGetProperty("location", out var loc))
                    {
                        if (loc.TryGetProperty("lat", out var latEl) && latEl.ValueKind == JsonValueKind.Number)
                            result.Latitude = latEl.GetDouble();
                        if (loc.TryGetProperty("lng", out var lonEl) && lonEl.ValueKind == JsonValueKind.Number)
                            result.Longitude = lonEl.GetDouble();
                    }

                    results.Add(result);
                }
            }

            return results;
        }
    }
}
