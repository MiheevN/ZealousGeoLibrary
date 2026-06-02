using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using Xunit;

namespace ZealousMindedPeopleGeo.Tests;

/// <summary>
/// Тесты менеджера контейнеров гео-данных с хранением в БД.
/// Проверяют работу с несколькими глобусами и загрузку/выгрузку данных (массивы, JSON).
/// </summary>
public class DatabaseGeoDataContainerManagerTests : DatabaseGeoDataTestBase
{
    [Fact]
    public async Task MultipleGlobes_DataIsIsolatedPerContainer()
    {
        var europe = Manager.GetOrCreateContainer("europe");
        var asia = Manager.GetOrCreateContainer("asia");

        await europe.AddParticipantAsync(CreateParticipant("Berlin"));
        await europe.AddParticipantAsync(CreateParticipant("Paris"));
        await asia.AddParticipantAsync(CreateParticipant("Tokyo"));

        Assert.Equal(2, europe.Count);
        Assert.Equal(1, asia.Count);

        var europeNames = (await europe.GetAllParticipantsAsync()).Select(p => p.Name).OrderBy(n => n).ToList();
        Assert.Equal(new[] { "Berlin", "Paris" }, europeNames);
    }

    [Fact]
    public async Task SameParticipantId_CanExistInDifferentGlobes()
    {
        var id = Guid.NewGuid();
        var europe = Manager.GetOrCreateContainer("europe");
        var asia = Manager.GetOrCreateContainer("asia");

        var r1 = await europe.AddParticipantAsync(CreateParticipant("Shared", id: id));
        var r2 = await asia.AddParticipantAsync(CreateParticipant("Shared", id: id));

        Assert.True(r1.Success);
        Assert.True(r2.Success);
        Assert.Equal(1, europe.Count);
        Assert.Equal(1, asia.Count);
    }

    [Fact]
    public async Task LoadDataAsync_LoadsArrayAndReplacesExisting()
    {
        await Manager.GetOrCreateContainer("globe").AddParticipantAsync(CreateParticipant("Old"));

        var participants = new[]
        {
            CreateParticipant("New1"),
            CreateParticipant("New2")
        };
        var result = await Manager.LoadDataAsync("globe", participants);

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);

        var names = (await Manager.GetContainer("globe")!.GetAllParticipantsAsync()).Select(p => p.Name).ToList();
        Assert.DoesNotContain("Old", names);
        Assert.Equal(2, names.Count);
    }

    [Fact]
    public async Task LoadFromJsonAsync_PersistsToDatabase()
    {
        var json = """
        [
            { "name": "JsonOne", "email": "j1@example.com", "address": "A", "location": "L", "latitude": 10.0, "longitude": 20.0 },
            { "name": "JsonTwo", "email": "j2@example.com", "address": "A", "location": "L", "latitude": 30.0, "longitude": 40.0 }
        ]
        """;

        var result = await Manager.LoadFromJsonAsync("json-globe", json);

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(2, Manager.GetContainer("json-globe")!.Count);
    }

    [Fact]
    public async Task ExportToJsonAsync_ReturnsStoredData()
    {
        await Manager.GetOrCreateContainer("globe").AddParticipantAsync(CreateParticipant("Exported"));

        var json = await Manager.ExportToJsonAsync("globe");

        Assert.Contains("Exported", json);
    }

    [Fact]
    public async Task GetContainerIds_ReturnsAllGlobesWithData()
    {
        await Manager.GetOrCreateContainer("g1").AddParticipantAsync(CreateParticipant("A"));
        await Manager.GetOrCreateContainer("g2").AddParticipantAsync(CreateParticipant("B"));

        var ids = Manager.GetContainerIds().OrderBy(i => i).ToList();

        Assert.Equal(new[] { "g1", "g2" }, ids);
    }

    [Fact]
    public async Task ContainerExists_TrueOnlyWhenDataPresent()
    {
        Assert.False(Manager.ContainerExists("empty"));

        await Manager.GetOrCreateContainer("filled").AddParticipantAsync(CreateParticipant("A"));

        Assert.True(Manager.ContainerExists("filled"));
        Assert.Null(Manager.GetContainer("empty"));
        Assert.NotNull(Manager.GetContainer("filled"));
    }

    [Fact]
    public async Task RemoveContainer_DeletesAllGlobeData()
    {
        var container = Manager.GetOrCreateContainer("to-remove");
        await container.AddParticipantsAsync(new[] { CreateParticipant("A"), CreateParticipant("B") });

        var removed = Manager.RemoveContainer("to-remove");

        Assert.True(removed);
        Assert.False(Manager.ContainerExists("to-remove"));
        Assert.False(Manager.RemoveContainer("never-existed"));
    }

    [Fact]
    public async Task OnDataChanged_RaisedForDatabaseOperations()
    {
        var events = new List<(string ContainerId, GeoDataChangeType Type)>();
        Manager.OnDataChanged += (id, type) => events.Add((id, type));

        var container = Manager.GetOrCreateContainer("events");
        await container.AddParticipantAsync(CreateParticipant("A"));

        Assert.Contains(events, e => e.ContainerId == "events" && e.Type == GeoDataChangeType.Added);
    }
}
