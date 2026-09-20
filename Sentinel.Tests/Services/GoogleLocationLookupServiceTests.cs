using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class GoogleLocationLookupServiceTests
{
    [Fact]
    public async Task GeocodeAddress_UsesV4HeaderAuthenticationAndDoesNotPlaceTheKeyInTheUrl()
    {
        var handler = new RecordingHandler("""
            { "results": [{ "location": { "latitude": -34.9285, "longitude": 138.6007 } }] }
            """);
        var service = CreateService(handler);

        var result = await service.GeocodeAddressAsync("1 King William Street, Adelaide");

        Assert.Equal(-34.9285, result.Latitude);
        Assert.Equal(138.6007, result.Longitude);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.StartsWith("https://geocode.googleapis.com/v4/geocode/address/", request.Uri.AbsoluteUri);
        Assert.Equal("test-server-key", request.Headers["X-Goog-Api-Key"]);
        Assert.Equal("results.location", request.Headers["X-Goog-FieldMask"]);
        Assert.DoesNotContain("key=", request.Uri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("test-server-key", request.Uri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAddresses_UsesPlacesNewAutocompleteAndDetailsWithHeaderAuthentication()
    {
        var handler = new RecordingHandler(
            """
            { "suggestions": [{ "placePrediction": { "placeId": "abc123" } }] }
            """,
            """
            {
              "id": "abc123",
              "formattedAddress": "1 King William Street, Adelaide SA 5000, Australia",
              "location": { "latitude": -34.9285, "longitude": 138.6007 },
              "addressComponents": [{ "longText": "Adelaide", "types": ["locality", "political"] }]
            }
            """);
        var service = CreateService(handler);

        var results = await service.SearchAddressesAsync("1 King William Street", biasLatitude: -34.9, biasLongitude: 138.6);

        var result = Assert.Single(results);
        Assert.Equal("1 King William Street, Adelaide SA 5000, Australia", result.Display);
        Assert.Equal(-34.9285, result.Latitude);
        Assert.Equal(138.6007, result.Longitude);
        Assert.Equal("Adelaide", result.AddressComponents["locality"]);

        Assert.Collection(
            handler.Requests,
            autocomplete =>
            {
                Assert.Equal(HttpMethod.Post, autocomplete.Method);
                Assert.Equal("https://places.googleapis.com/v1/places:autocomplete", autocomplete.Uri.AbsoluteUri);
                Assert.Equal("test-server-key", autocomplete.Headers["X-Goog-Api-Key"]);
                Assert.Equal("suggestions.placePrediction.placeId", autocomplete.Headers["X-Goog-FieldMask"]);
                Assert.DoesNotContain("test-server-key", autocomplete.Body, StringComparison.Ordinal);
                Assert.DoesNotContain("key=", autocomplete.Uri.Query, StringComparison.OrdinalIgnoreCase);
            },
            details =>
            {
                Assert.Equal(HttpMethod.Get, details.Method);
                Assert.Equal("https://places.googleapis.com/v1/places/abc123", details.Uri.AbsoluteUri);
                Assert.Equal("test-server-key", details.Headers["X-Goog-Api-Key"]);
                Assert.Equal("id,formattedAddress,location,addressComponents", details.Headers["X-Goog-FieldMask"]);
                Assert.DoesNotContain("key=", details.Uri.Query, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public async Task SearchPlaces_UsesPlacesTextSearchWithAFieldMaskAndAHeaderKey()
    {
        var handler = new RecordingHandler("""
            {
              "places": [{
                "id": "place-1",
                "displayName": { "text": "Sentinel Test Clinic" },
                "formattedAddress": "2 Test Street, Adelaide SA",
                "location": { "latitude": -34.92, "longitude": 138.61 }
              }]
            }
            """);
        var service = CreateService(handler);

        var results = await service.SearchPlacesAsync("Sentinel Test Clinic", 3, -34.9, 138.6);

        var result = Assert.Single(results);
        Assert.Equal("place-1", result.PlaceId);
        Assert.Equal("Sentinel Test Clinic", result.Name);
        Assert.Equal("2 Test Street, Adelaide SA", result.Address);
        Assert.Equal(-34.92, result.Latitude);
        Assert.Equal(138.61, result.Longitude);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://places.googleapis.com/v1/places:searchText", request.Uri.AbsoluteUri);
        Assert.Equal("test-server-key", request.Headers["X-Goog-Api-Key"]);
        Assert.Equal("places.id,places.displayName,places.formattedAddress,places.location", request.Headers["X-Goog-FieldMask"]);
        Assert.DoesNotContain("test-server-key", request.Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("test-server-key", request.Body, StringComparison.Ordinal);
    }

    private static GoogleLocationLookupService CreateService(RecordingHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Geocoding:ApiKey"] = "test-server-key",
                ["Organization:CountryCode"] = "AU"
            })
            .Build();

        return new GoogleLocationLookupService(new HttpClient(handler), configuration);
    }

    private sealed class RecordingHandler(params string[] responses) : HttpMessageHandler
    {
        private readonly Queue<string> _responses = new(responses);

        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri!, headers, body));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        IReadOnlyDictionary<string, string> Headers,
        string Body);
}
