using System.Text.Json;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Контракт хранилищ для произвольных точек: не только участников.
/// </summary>
public abstract partial class GeoDataStorageContractTests
{
    [Fact]
    public async Task AddPointAsync_StoresEveryField()
    {
        var container = Manager.GetOrCreateContainer("offices");
        var point = CreatePoint("moscow-office", 55.7558, 37.6176);
        point.Description = "Главный офис";
        point.Category = "office";
        point.Color = "#24dce7";
        point.Icon = "🏢";
        point.Url = "https://example.com/offices/moscow";
        point.Properties["staff"] = "120";
        point.Properties["адрес"] = "Тверская, 1";

        var result = await container.AddPointAsync(point);

        Assert.True(result.Success);
        Assert.Equal("moscow-office", result.PointId);
        Assert.Null(result.RecordId); // идентификатор не GUID
        var loaded = await container.GetPointAsync("moscow-office");
        Assert.NotNull(loaded);
        Assert.Equal(55.7558, loaded!.Latitude);
        Assert.Equal(37.6176, loaded.Longitude);
        Assert.Equal("moscow-office title", loaded.Title);
        Assert.Equal("Главный офис", loaded.Description);
        Assert.Equal("office", loaded.Category);
        Assert.Equal("#24dce7", loaded.Color);
        Assert.Equal("🏢", loaded.Icon);
        Assert.Equal("https://example.com/offices/moscow", loaded.Url);
        Assert.Equal(new Dictionary<string, string> { ["staff"] = "120", ["адрес"] = "Тверская, 1" }, loaded.Properties);
    }

    [Fact]
    public async Task AddPointAsync_WithoutOptionalFields_ReadsBackEmptyProperties()
    {
        var container = Manager.GetOrCreateContainer("sensors");

        await container.AddPointAsync(new GeoPoint { Id = "s-1", Latitude = -33.86, Longitude = 151.21 });

        var loaded = await container.GetPointAsync("s-1");
        Assert.NotNull(loaded);
        Assert.Equal(string.Empty, loaded!.Title);
        Assert.Null(loaded.Category);
        Assert.Empty(loaded.Properties);
    }

    [Theory]
    [InlineData("", 10, 10)]
    [InlineData(" ", 10, 10)]
    [InlineData("p", 90.5, 0)]
    [InlineData("p", -91, 0)]
    [InlineData("p", 0, 180.1)]
    [InlineData("p", 0, -181)]
    [InlineData("p", double.NaN, 0)]
    [InlineData("p", 0, double.PositiveInfinity)]
    public async Task AddPointAsync_InvalidPoint_IsRejected(string id, double latitude, double longitude)
    {
        var container = Manager.GetOrCreateContainer("strict");

        var result = await container.AddPointAsync(new GeoPoint { Id = id, Latitude = latitude, Longitude = longitude });

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal(0, container.Count);
    }

    [Fact]
    public async Task AddPointAsync_TooLongValues_AreRejectedByEveryStorage()
    {
        var container = Manager.GetOrCreateContainer("strict");

        var longId = await container.AddPointAsync(CreatePoint(new string('x', GeoPoint.MaxIdLength + 1), 0, 0));
        var longTitle = CreatePoint("t", 0, 0);
        longTitle.Title = new string('x', GeoPoint.MaxTitleLength + 1);
        var longCategory = CreatePoint("c", 0, 0);
        longCategory.Category = new string('x', GeoPoint.MaxCategoryLength + 1);

        Assert.False(longId.Success);
        Assert.False((await container.AddPointAsync(longTitle)).Success);
        Assert.False((await container.AddPointAsync(longCategory)).Success);
        Assert.True((await container.AddPointAsync(CreatePoint(new string('x', GeoPoint.MaxIdLength), 0, 0))).Success);
        Assert.Equal(1, container.Count);
    }

    [Fact]
    public async Task AddPointAsync_DuplicateId_Fails()
    {
        var container = Manager.GetOrCreateContainer("offices");

        var first = await container.AddPointAsync(CreatePoint("hq", 1, 1));
        var second = await container.AddPointAsync(CreatePoint("hq", 2, 2));

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(1, (await container.GetPointAsync("hq"))!.Latitude);
    }

    [Fact]
    public async Task AddPointsAsync_CountsAddedAndSkipped()
    {
        var container = Manager.GetOrCreateContainer("offices");
        await container.AddPointAsync(CreatePoint("existing", 0, 0));

        var result = await container.AddPointsAsync(new[]
        {
            CreatePoint("existing", 1, 1),   // уже есть
            CreatePoint("a", 1, 1),
            CreatePoint("a", 2, 2),          // повтор в наборе
            null!,
            CreatePoint("bad", 95, 0),       // неверная широта
            CreatePoint("b", 3, 3)
        });

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(4, result.SkippedCount);
        Assert.Equal(3, container.Count);
        Assert.Equal(1, (await container.GetPointAsync("a"))!.Latitude);
    }

    [Fact]
    public async Task Points_AreCopies_NotSharedReferences()
    {
        var container = Manager.GetOrCreateContainer("offices");
        var point = CreatePoint("hq", 10, 20);
        point.Properties["floor"] = "3";
        await container.AddPointAsync(point);

        // Изменения исходного и полученного объектов не попадают в хранилище.
        point.Title = "changed";
        point.Properties["floor"] = "9";
        var loaded = await container.GetPointAsync("hq");
        loaded!.Properties["floor"] = "7";
        (await container.GetPointsAsync())[0].Latitude = 0;

        var again = await container.GetPointAsync("hq");
        Assert.Equal("hq title", again!.Title);
        Assert.Equal("3", again.Properties["floor"]);
        Assert.Equal(10, again.Latitude);
    }

    [Fact]
    public async Task UpdatePointAsync_ReplacesAllFields()
    {
        var container = Manager.GetOrCreateContainer("offices");
        var original = CreatePoint("hq", 10, 20);
        original.Category = "office";
        original.Properties["floor"] = "3";
        await container.AddPointAsync(original);

        var changed = CreatePoint("hq", 11, 21);
        changed.Title = "Moved HQ";
        changed.Properties["rooms"] = "40";
        var result = await container.UpdatePointAsync(changed);

        Assert.True(result.Success);
        var loaded = await container.GetPointAsync("hq");
        Assert.Equal("Moved HQ", loaded!.Title);
        Assert.Equal(11, loaded.Latitude);
        Assert.Null(loaded.Category);
        Assert.Equal(new Dictionary<string, string> { ["rooms"] = "40" }, loaded.Properties);
        Assert.Equal(1, container.Count);
    }

    [Fact]
    public async Task UpdatePointAsync_UnknownOrInvalid_Fails()
    {
        var container = Manager.GetOrCreateContainer("offices");
        await container.AddPointAsync(CreatePoint("hq", 10, 20));

        Assert.False((await container.UpdatePointAsync(CreatePoint("ghost", 0, 0))).Success);
        Assert.False((await container.UpdatePointAsync(CreatePoint("hq", 100, 0))).Success);
        Assert.Equal(10, (await container.GetPointAsync("hq"))!.Latitude);
    }

    [Fact]
    public async Task RemovePointAsync_DeletesOnlyThatPoint()
    {
        var container = Manager.GetOrCreateContainer("offices");
        await container.AddPointsAsync(new[] { CreatePoint("a", 1, 1), CreatePoint("b", 2, 2) });

        var removed = await container.RemovePointAsync("a");
        var again = await container.RemovePointAsync("a");

        Assert.True(removed.Success);
        Assert.Equal("a", removed.PointId);
        Assert.False(again.Success);
        Assert.Null(await container.GetPointAsync("a"));
        Assert.NotNull(await container.GetPointAsync("b"));
    }

    [Fact]
    public async Task PointIds_AreScopedToContainer()
    {
        var offices = Manager.GetOrCreateContainer("offices");
        var events = Manager.GetOrCreateContainer("events");

        Assert.True((await offices.AddPointAsync(CreatePoint("hq", 1, 1))).Success);
        Assert.True((await events.AddPointAsync(CreatePoint("hq", 2, 2))).Success);

        Assert.Equal(1, (await offices.GetPointAsync("hq"))!.Latitude);
        Assert.Equal(2, (await events.GetPointAsync("hq"))!.Latitude);
    }

    [Fact]
    public async Task PointOperations_RaiseSameEventsAsParticipants()
    {
        var events = RecordEvents();
        var container = Manager.GetOrCreateContainer("points");

        await container.AddPointAsync(CreatePoint("a", 1, 1));
        await container.AddPointsAsync(new[] { CreatePoint("b", 2, 2) });
        await container.UpdatePointAsync(CreatePoint("a", 3, 3));
        await container.RemovePointAsync("a");
        await container.ClearAsync();
        await container.RemovePointAsync("missing");
        await container.AddPointsAsync(new[] { CreatePoint("bad", 99, 0) });

        Assert.Equal(
            new[]
            {
                ("points", GeoDataChangeType.Added),
                ("points", GeoDataChangeType.BulkLoaded),
                ("points", GeoDataChangeType.Updated),
                ("points", GeoDataChangeType.Removed),
                ("points", GeoDataChangeType.Cleared)
            },
            events);
    }

    [Fact]
    public async Task LoadPointsAsync_ReplacesExistingData()
    {
        await Manager.GetOrCreateContainer("map").AddPointAsync(CreatePoint("old", 0, 0));

        var result = await Manager.LoadPointsAsync("map", new[] { CreatePoint("n1", 1, 1), CreatePoint("n2", 2, 2) });

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(new[] { "n1", "n2" }, await PointIdsAsync("map"));
    }

    [Fact]
    public async Task LoadFromJsonAsync_ReadsPointsAndLegacyParticipantsTogether()
    {
        var json = """
        [
            { "id": "office-1", "latitude": 52.52, "longitude": 13.405, "title": "Berlin", "category": "office",
              "properties": { "staff": "40" } },
            { "name": "Legacy Person", "email": "legacy@example.com", "latitude": 48.85, "longitude": 2.35 },
            { "id": "no-coordinates", "title": "Nowhere" },
            { "name": "No Coordinates Person" },
            42
        ]
        """;

        var result = await Manager.LoadFromJsonAsync("mixed", json);

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(3, result.SkippedCount);
        var points = await Manager.GetContainer("mixed")!.GetPointsAsync();
        var office = Assert.Single(points, p => p.Id == "office-1");
        Assert.Equal("office", office.Category);
        Assert.Equal("40", office.Properties["staff"]);
        var person = Assert.Single(points, p => p.Title == "Legacy Person");
        Assert.Equal("legacy@example.com", person.Properties[ParticipantPointProperties.Email]);
    }

    [Fact]
    public async Task LoadFromJsonAsync_NullTitle_ReadsBackAsEmptyString()
    {
        await Manager.LoadFromJsonAsync("json", """[{ "id": "a", "latitude": 1, "longitude": 1, "title": null, "properties": {} }]""");

        Assert.Equal(string.Empty, (await Manager.GetContainer("json")!.GetPointAsync("a"))!.Title);
    }

    [Fact]
    public async Task LoadFromJsonAsync_NotAnArray_Fails()
    {
        var result = await Manager.LoadFromJsonAsync("json", """{ "id": "a", "latitude": 1, "longitude": 1 }""");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExportToJsonAsync_WritesPointsAndRoundTrips()
    {
        var berlin = CreatePoint("berlin", 52.52, 13.405);
        berlin.Category = "office";
        berlin.Properties["staff"] = "40";
        await Manager.LoadPointsAsync("source", new[] { berlin });
        await Manager.GetContainer("source")!.AddParticipantAsync(CreateParticipant("Person", 10, 20));

        var json = await Manager.ExportToJsonAsync("source");
        var result = await Manager.LoadFromJsonAsync("copy", json);

        // Экспорт — массив точек: поля участника лежат в properties, null не пишется.
        using (var document = JsonDocument.Parse(json))
        {
            var first = document.RootElement.EnumerateArray().First();
            Assert.True(first.TryGetProperty("title", out _));
            Assert.True(first.TryGetProperty("properties", out _));
            Assert.False(first.TryGetProperty("email", out _));
            Assert.DoesNotContain("null", json);
        }

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        var copy = await Manager.GetContainer("copy")!.GetPointAsync("berlin");
        Assert.Equal("office", copy!.Category);
        Assert.Equal("40", copy.Properties["staff"]);
        var person = Assert.Single(await Manager.GetContainer("copy")!.GetAllParticipantsAsync(), p => p.Name == "Person");
        Assert.Equal("person@example.com", person.Email);
    }

    private static GeoPoint CreatePoint(string id, double latitude, double longitude) => new()
    {
        Id = id,
        Latitude = latitude,
        Longitude = longitude,
        Title = $"{id} title"
    };

    private async Task<string[]> PointIdsAsync(string containerId)
    {
        var points = await Manager.GetOrCreateContainer(containerId).GetPointsAsync();
        return points.Select(p => p.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }
}
