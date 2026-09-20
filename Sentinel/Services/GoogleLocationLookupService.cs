using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Services;

/// <summary>
/// Server-side integration with the current Google Geocoding API v4 and
/// Places API (New). The server key is deliberately passed only in the
/// <c>X-Goog-Api-Key</c> request header; it must never be placed in a URL,
/// browser response, or log entry.
/// </summary>
public sealed class GoogleLocationLookupService : ILocationLookupService
{
    private const string ApiKeyHeader = "X-Goog-Api-Key";
    private const string FieldMaskHeader = "X-Goog-FieldMask";
    private const string GoogleGeocodingBaseUrl = "https://geocode.googleapis.com/v4/geocode";
    private const string GooglePlacesBaseUrl = "https://places.googleapis.com/v1";

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
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return (null, null);
        }

        var requestUrl = $"{GoogleGeocodingBaseUrl}/address/{Uri.EscapeDataString(address)}";
        if (!string.IsNullOrWhiteSpace(_countryCode))
        {
            requestUrl += $"?regionCode={Uri.EscapeDataString(_countryCode)}";
        }

        using var request = CreateRequest(HttpMethod.Get, requestUrl, fieldMask: "results.location");
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return (null, null);
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!document.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            return (null, null);
        }

        var location = results[0].TryGetProperty("location", out var locationElement)
            ? locationElement
            : default;

        return (
            GetDouble(location, "latitude"),
            GetDouble(location, "longitude"));
    }

    public async Task<List<AddressLookupResult>> SearchAddressesAsync(
        string query,
        int limit = 5,
        double? biasLatitude = null,
        double? biasLongitude = null)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return [];
        }

        var placeIds = await GetAutocompletePlaceIdsAsync(query, limit, biasLatitude, biasLongitude);
        var results = new List<AddressLookupResult>(placeIds.Count);

        foreach (var placeId in placeIds)
        {
            var place = await GetPlaceDetailsAsync(placeId, "id,formattedAddress,location,addressComponents");
            if (place is null)
            {
                continue;
            }

            var result = new AddressLookupResult
            {
                Display = GetString(place.Value, "formattedAddress"),
                Latitude = GetLocationCoordinate(place.Value, "latitude"),
                Longitude = GetLocationCoordinate(place.Value, "longitude")
            };

            AddAddressComponents(result.AddressComponents, place.Value);
            results.Add(result);
        }

        return results;
    }

    public async Task<List<PlaceLookupResult>> SearchPlacesAsync(
        string query,
        int limit = 5,
        double? biasLatitude = null,
        double? biasLongitude = null)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return [];
        }

        var payload = new Dictionary<string, object?>
        {
            ["textQuery"] = query,
            ["maxResultCount"] = Math.Clamp(limit, 1, 20)
        };

        if (!string.IsNullOrWhiteSpace(_countryCode))
        {
            payload["regionCode"] = _countryCode;
        }

        if (biasLatitude.HasValue && biasLongitude.HasValue)
        {
            payload["locationBias"] = new
            {
                circle = new
                {
                    center = new { latitude = biasLatitude.Value, longitude = biasLongitude.Value },
                    radius = 10_000d
                }
            };
        }

        using var content = CreateJsonContent(payload);
        using var request = CreateRequest(
            HttpMethod.Post,
            $"{GooglePlacesBaseUrl}/places:searchText",
            content,
            "places.id,places.displayName,places.formattedAddress,places.location");
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!document.RootElement.TryGetProperty("places", out var places) || places.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<PlaceLookupResult>();
        foreach (var place in places.EnumerateArray())
        {
            var displayName = place.TryGetProperty("displayName", out var displayNameElement)
                ? GetString(displayNameElement, "text")
                : null;

            results.Add(new PlaceLookupResult
            {
                PlaceId = GetString(place, "id"),
                Name = displayName,
                Address = GetString(place, "formattedAddress"),
                Latitude = GetLocationCoordinate(place, "latitude"),
                Longitude = GetLocationCoordinate(place, "longitude")
            });
        }

        return results;
    }

    private async Task<List<string>> GetAutocompletePlaceIdsAsync(
        string input,
        int limit,
        double? biasLatitude,
        double? biasLongitude)
    {
        var payload = new Dictionary<string, object?> { ["input"] = input };

        if (!string.IsNullOrWhiteSpace(_countryCode))
        {
            payload["includedRegionCodes"] = new[] { _countryCode.ToLowerInvariant() };
            payload["regionCode"] = _countryCode.ToLowerInvariant();
        }

        if (biasLatitude.HasValue && biasLongitude.HasValue)
        {
            payload["locationBias"] = new
            {
                circle = new
                {
                    center = new { latitude = biasLatitude.Value, longitude = biasLongitude.Value },
                    radius = 50_000d
                }
            };
        }

        using var content = CreateJsonContent(payload);
        using var request = CreateRequest(
            HttpMethod.Post,
            $"{GooglePlacesBaseUrl}/places:autocomplete",
            content,
            "suggestions.placePrediction.placeId");
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        if (!document.RootElement.TryGetProperty("suggestions", out var suggestions) ||
            suggestions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return suggestions
            .EnumerateArray()
            .Select(suggestion => suggestion.TryGetProperty("placePrediction", out var prediction)
                ? GetString(prediction, "placeId")
                : null)
            .Where(placeId => !string.IsNullOrWhiteSpace(placeId))
            .Select(placeId => placeId!)
            .Take(Math.Clamp(limit, 1, 5))
            .ToList();
    }

    private async Task<JsonElement?> GetPlaceDetailsAsync(string placeId, string fieldMask)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            $"{GooglePlacesBaseUrl}/places/{Uri.EscapeDataString(placeId)}",
            fieldMask: fieldMask);
        using var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        HttpContent? content = null,
        string? fieldMask = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, _apiKey);

        if (!string.IsNullOrWhiteSpace(fieldMask))
        {
            request.Headers.TryAddWithoutValidation(FieldMaskHeader, fieldMask);
        }

        return request;
    }

    private static StringContent CreateJsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string? GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static double? GetDouble(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.Number
            ? property.GetDouble()
            : null;

    private static double? GetLocationCoordinate(JsonElement element, string coordinateName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty("location", out var location)
            ? GetDouble(location, coordinateName)
            : null;

    private static void AddAddressComponents(Dictionary<string, string?> components, JsonElement place)
    {
        if (!place.TryGetProperty("addressComponents", out var addressComponents) ||
            addressComponents.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var component in addressComponents.EnumerateArray())
        {
            var longText = GetString(component, "longText");
            if (!component.TryGetProperty("types", out var types) || types.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var type in types.EnumerateArray())
            {
                if (type.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(type.GetString()))
                {
                    continue;
                }

                components.TryAdd(type.GetString()!, longText);
            }
        }
    }
}
