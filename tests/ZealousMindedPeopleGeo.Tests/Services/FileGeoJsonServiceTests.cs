using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.Geocoding;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// FileGeoJsonService: после чтения из JSON координаты геометрии — <see cref="JsonElement"/>,
/// приведение к double[][][] давало null, и страна по координатам не находилась никогда.
/// Данные подаются через подменный HttpMessageHandler, в сеть тесты не ходят.
/// </summary>
public class FileGeoJsonServiceTests
{
    // Квадрат 0..10 с дырой 4..6 и два острова-квадрата 20..22 и 30..32 (долгота, широта).
    private const string CountriesJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "id": "SQL",
              "properties": { "name": "Squareland" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [
                  [[0, 0], [10, 0], [10, 10], [0, 10], [0, 0]],
                  [[4, 4], [6, 4], [6, 6], [4, 6], [4, 4]]
                ]
              }
            },
            {
              "type": "Feature",
              "id": "ISL",
              "properties": { "name": "Islands" },
              "geometry": {
                "type": "MultiPolygon",
                "coordinates": [
                  [[[20, 0], [22, 0], [22, 2], [20, 2], [20, 0]]],
                  [[[30, 0], [32, 0], [32, 2], [30, 2], [30, 0]]]
                ]
              }
            }
          ]
        }
        """;

    // Как в lutangar/cities.json: координаты строками, без населения.
    private const string CitiesJson = """
        [
          { "name": "Berlin", "country": "DE", "lat": "52.52437", "lng": "13.41053" },
          { "name": "Bern", "country": "CH", "lat": "46.94809", "lng": "7.44744" }
        ]
        """;

    [Theory]
    [InlineData(2, 2, "Squareland")]
    [InlineData(1, 21, "Islands")]
    [InlineData(1, 31, "Islands")]
    public async Task GetCountryByCoordinates_PointInside_ReturnsCountry(double latitude, double longitude, string expected)
    {
        var service = CreateService();

        var country = await service.GetCountryByCoordinatesAsync(latitude, longitude);

        Assert.NotNull(country);
        Assert.Equal(expected, country.Name);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(1, 26)]
    [InlineData(5, 5)] // дыра в полигоне
    public async Task GetCountryByCoordinates_PointOutside_ReturnsNull(double latitude, double longitude)
    {
        var service = CreateService();

        Assert.Null(await service.GetCountryByCoordinatesAsync(latitude, longitude));
    }

    [Fact]
    public async Task GetCountryByCoordinates_TakesIsoCodeFromFeatureId_WhenPropertiesHaveNone()
    {
        var service = CreateService();

        var country = await service.GetCountryByCoordinatesAsync(2, 2);

        Assert.Equal("SQL", country?.IsoCode);
    }

    [Fact]
    public void TryGetPolygons_ReadsJsonElementAndArrays_Alike()
    {
        var parsed = JsonSerializer.Deserialize<GeoJsonFeatureCollection>(CountriesJson)!;
        Assert.True(parsed.Features[0].Geometry!.TryGetPolygons(out var polygon));
        Assert.True(parsed.Features[1].Geometry!.TryGetPolygons(out var multiPolygon));

        var built = GeoJsonGeometry.CreatePolygon(new[]
        {
            new[] { new GeoJsonCoordinates(0, 0), new GeoJsonCoordinates(1, 0), new GeoJsonCoordinates(0, 1) }
        });
        Assert.True(built.TryGetPolygons(out var builtPolygon));

        Assert.Equal(2, Assert.Single(polygon).Length);
        Assert.Equal(2, multiPolygon.Count);
        Assert.Equal(new[] { 30d, 0d }, multiPolygon[1][0][0]);
        Assert.Equal(3, Assert.Single(builtPolygon)[0].Length);
        Assert.False(new GeoJsonGeometry { Type = "Point", Coordinates = new[] { 1d, 2d } }.TryGetPolygons(out _));
    }

    [Fact]
    public async Task Cities_UsePointGeometry_RegardlessOfCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
        try
        {
            var service = CreateService();

            var nearest = await service.GetNearestCityAsync(52.5, 13.4);
            var found = (await service.SearchCitiesAsync("ber")).ToList();

            Assert.Equal("Berlin", nearest?.Name);
            Assert.Equal(52.52437, nearest!.Latitude, 5);
            Assert.Equal(13.41053, nearest.Longitude, 5);
            Assert.Equal(new[] { "Berlin", "Bern" }, found.Select(c => c.Name));
            Assert.Equal(7.44744, found[1].Longitude, 5);
            Assert.Null(await service.GetNearestCityAsync(0, 0));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    private static FileGeoJsonService CreateService() =>
        new(new HttpClient(new FakeHandler()),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<FileGeoJsonService>.Instance);

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = path.EndsWith("countries.geo.json") ? CountriesJson
                : path.EndsWith("cities.json") ? CitiesJson
                : null;

            return Task.FromResult(body is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
        }
    }
}
