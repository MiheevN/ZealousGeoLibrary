using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.Mapping;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// Что сервис глобуса передаёт в community-globe.js: точки и цвета маркеров по категориям.
/// </summary>
public class ThreeJsGlobeServiceTests : BunitContext
{
    private const string GlobeScript = "/_content/ZealousMindedPeopleGeo/js/community-globe.js";

    private readonly BunitJSModuleInterop _module;
    private readonly ThreeJsGlobeService _service;

    public ThreeJsGlobeServiceTests()
    {
        _module = JSInterop.SetupModule(GlobeScript);
        _module.Setup<bool>("createGlobe", _ => true).SetResult(true);
        _module.Setup<string>("getThreeJsVersion").SetResult("test");
        _module.Setup<bool>("addParticipants", _ => true).SetResult(true);
        _service = new ThreeJsGlobeService(JSInterop.JSRuntime, NullLogger<ThreeJsGlobeService>.Instance);
    }

    [Fact]
    public async Task AddPointsAsync_SendsCategoryColorsLikeTheMap()
    {
        await _service.InitializeGlobeAsync("globe-a", new GlobeOptions());
        var points = new[]
        {
            Point("berlin", "office", 52.52, 13.405), Point("tokyo", "event", 35.68, 139.69), Point("oslo", null, 59.91, 10.75)
        };

        var result = await _service.AddPointsAsync("globe-a", points);

        Assert.True(result.Success);
        var sent = SentMarkers();
        Assert.Equal(new[] { "berlin", "tokyo", "oslo" }, sent.Select(m => Field<string>(m, "id")));
        Assert.Equal(
            new[] { GeoPointPalette.CategoryColors[0], GeoPointPalette.CategoryColors[1], GeoPointPalette.OtherColor },
            sent.Select(m => Field<string?>(m, "markerColor")));
        Assert.Equal("berlin title", Field<string>(sent[0], "name"));
        Assert.Equal(52.52, Field<double>(sent[0], "latitude"));
        Assert.Equal("berlin title (52.5200, 13.4050)", Field<string>(sent[0], "location"));
    }

    [Fact]
    public async Task AddPointsAsync_WithoutCategories_LeavesColorToGlobeSettings()
    {
        await _service.InitializeGlobeAsync("globe-a", new GlobeOptions());
        var own = Point("own", null, 1, 1);
        own.Color = "#ff00ff";

        await _service.AddPointsAsync("globe-a", new[] { Point("plain", null, 2, 2), own, Point("bad", null, 95, 0) });

        var sent = SentMarkers();
        Assert.Equal(new[] { "plain", "own" }, sent.Select(m => Field<string>(m, "id")));
        Assert.Null(Field<string?>(sent[0], "markerColor"));
        Assert.Equal("#ff00ff", Field<string?>(sent[1], "markerColor"));
    }

    [Fact]
    public async Task AddParticipantsAsync_SendsParticipantsAsPointsWithoutColors()
    {
        await _service.InitializeGlobeAsync("globe-a", new GlobeOptions());
        var anna = CreateParticipant("Anna", 55.75, 37.62);

        var result = await _service.AddParticipantsAsync("globe-a", new[] { anna, CreateParticipant("Nowhere", latitude: null) });

        Assert.Equal(1, result.ProcessedCount);
        var marker = Assert.Single(SentMarkers());
        Assert.Equal(anna.Id.ToString(), Field<string>(marker, "id"));
        Assert.Equal("Anna", Field<string>(marker, "name"));
        Assert.Null(Field<string?>(marker, "markerColor"));
    }

    [Fact]
    public async Task AddPointsAsync_BeforeInitialization_Fails()
    {
        var result = await _service.AddPointsAsync("globe-a", new[] { Point("a", null, 1, 1) });

        Assert.False(result.Success);
        Assert.Empty(_module.Invocations["addParticipants"]);
    }

    private object[] SentMarkers()
    {
        var invocation = Assert.Single(_module.Invocations["addParticipants"]);
        Assert.Equal("globe-a", invocation.Arguments[0]);
        return ((System.Collections.IEnumerable)invocation.Arguments[1]!).Cast<object>().ToArray();
    }

    private static T Field<T>(object marker, string name) => (T)marker.GetType().GetProperty(name)!.GetValue(marker)!;

    private static GeoPoint Point(string id, string? category, double latitude, double longitude) => new()
    {
        Id = id,
        Latitude = latitude,
        Longitude = longitude,
        Title = $"{id} title",
        Category = category
    };
}
