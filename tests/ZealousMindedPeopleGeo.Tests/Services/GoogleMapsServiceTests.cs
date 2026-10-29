using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// GoogleMapsService читает настоящий ответ Geocoding API (snake_case, lat/lng)
/// и по статусу ответа решает, что сказать пользователю.
/// </summary>
public class GoogleMapsServiceTests
{
    [Fact]
    public async Task GeocodeAddressAsync_ReadsGoogleResponse()
    {
        var service = Service(HttpStatusCode.OK, """
            {
              "results": [{
                "formatted_address": "Alexanderplatz 1, 10178 Berlin, Germany",
                "geometry": { "location": { "lat": 52.5219, "lng": 13.4132 } }
              }],
              "status": "OK"
            }
            """);

        var result = await service.GeocodeAddressAsync("Berlin, Alexanderplatz 1");

        Assert.True(result.Success);
        Assert.Equal(GeocodingError.None, result.Error);
        Assert.Equal(52.5219, result.Latitude);
        Assert.Equal(13.4132, result.Longitude);
        Assert.Equal("Alexanderplatz 1, 10178 Berlin, Germany", result.FormattedAddress);
    }

    [Theory]
    [InlineData("ZERO_RESULTS", GeocodingError.NotFound)]
    [InlineData("REQUEST_DENIED", GeocodingError.Unavailable)]
    [InlineData("OVER_QUERY_LIMIT", GeocodingError.Failed)]
    [InlineData("UNKNOWN_ERROR", GeocodingError.Failed)]
    public async Task GeocodeAddressAsync_NoResults_ReasonFollowsStatus(string status, GeocodingError expected)
    {
        var service = Service(HttpStatusCode.OK, $$"""{ "results": [], "status": "{{status}}", "error_message": "detail" }""");

        var result = await service.GeocodeAddressAsync("Nowhere");

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
        Assert.Contains(status, result.ErrorMessage);
    }

    [Fact]
    public async Task GeocodeAddressAsync_HttpError_IsWorthRetrying()
    {
        var result = await Service(HttpStatusCode.BadGateway, "").GeocodeAddressAsync("Berlin");

        Assert.False(result.Success);
        Assert.Equal(GeocodingError.Failed, result.Error);
    }

    [Theory]
    [InlineData(false, "key")]
    [InlineData(true, "")]
    public async Task GeocodeAddressAsync_NotConfigured_IsUnavailable(bool enabled, string apiKey)
    {
        var service = new GoogleMapsService(
            Options.Create(new ZealousMindedPeopleGeoOptions { EnableGeocoding = enabled, GoogleMapsApiKey = apiKey }),
            new HttpClient(new StubHandler(HttpStatusCode.OK, "")),
            NullLogger<GoogleMapsService>.Instance);

        var result = await service.GeocodeAddressAsync("Berlin");

        Assert.False(result.Success);
        Assert.Equal(GeocodingError.Unavailable, result.Error);
    }

    private static GoogleMapsService Service(HttpStatusCode status, string body) => new(
        Options.Create(new ZealousMindedPeopleGeoOptions { GoogleMapsApiKey = "test-key" }),
        new HttpClient(new StubHandler(status, body)),
        NullLogger<GoogleMapsService>.Instance);

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
