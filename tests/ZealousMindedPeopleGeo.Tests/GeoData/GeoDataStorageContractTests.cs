using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Поведение, общее для всех хранилищ гео-данных. Одни и те же тесты прогоняются на
/// хранилище в памяти и в БД, чтобы реализации <see cref="IGeoDataContainerManager"/>
/// не расходились. Различия реализаций проверяются в их собственных классах.
/// Этот файл — сценарии с участниками (их хранилища держат как точки), точки — в
/// GeoDataStorageContractTests.Points.cs.
/// </summary>
public abstract partial class GeoDataStorageContractTests : IDisposable
{
    /// <summary>Свежий менеджер контейнеров для каждого теста.</summary>
    protected abstract IGeoDataContainerManager Manager { get; }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    // --- Контейнер -----------------------------------------------------------

    [Fact]
    public async Task AddParticipantAsync_StoresParticipant()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("Alice");

        var result = await container.AddParticipantAsync(participant);

        Assert.True(result.Success);
        Assert.Equal(participant.Id, result.RecordId);
        Assert.Equal(1, container.Count);
        var loaded = await container.GetParticipantByIdAsync(participant.Id);
        Assert.Equal("Alice", loaded?.Name);
    }

    [Fact]
    public async Task AddParticipantAsync_OneByOne_AccumulatesData()
    {
        var container = Manager.GetOrCreateContainer("globe-a");

        await container.AddParticipantAsync(CreateParticipant("First"));
        await container.AddParticipantAsync(CreateParticipant("Second"));
        await container.AddParticipantAsync(CreateParticipant("Third"));

        Assert.Equal(3, (await container.GetAllParticipantsAsync()).Count());
        Assert.Equal(3, container.Count);
    }

    [Fact]
    public async Task AddParticipantAsync_DuplicateId_Fails()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("Alice");

        var first = await container.AddParticipantAsync(participant);
        var second = await container.AddParticipantAsync(participant);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.NotNull(second.ErrorMessage);
        Assert.Equal(1, container.Count);
    }

    [Fact]
    public async Task AddParticipantAsync_Null_Fails()
    {
        var container = Manager.GetOrCreateContainer("globe-a");

        var result = await container.AddParticipantAsync(null!);

        Assert.False(result.Success);
        Assert.Equal(0, container.Count);
    }

    [Fact]
    public async Task AddParticipantsAsync_LoadsArray()
    {
        var container = Manager.GetOrCreateContainer("globe-a");

        var result = await container.AddParticipantsAsync(new[]
        {
            CreateParticipant("One"),
            CreateParticipant("Two"),
            CreateParticipant("Three")
        });

        Assert.True(result.Success);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(3, container.Count);
    }

    [Fact]
    public async Task AddParticipantsAsync_SkipsExistingDuplicatesAndNulls()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var existing = CreateParticipant("Existing");
        await container.AddParticipantAsync(existing);
        var newA = CreateParticipant("NewA");
        var newB = CreateParticipant("NewB");

        var result = await container.AddParticipantsAsync(new[] { existing, newA, newA, null!, newB });

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount); // newA и newB
        Assert.Equal(3, container.Count);
    }

    [Fact]
    public async Task GetParticipantByIdAsync_UnknownId_ReturnsNull()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        await container.AddParticipantAsync(CreateParticipant("Alice"));

        Assert.Null(await container.GetParticipantByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateParticipantAsync_ReplacesStoredData()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var original = CreateParticipant("Original");
        await container.AddParticipantAsync(original);

        // Новый объект с тем же Id: хранилище в памяти не должно полагаться на общую ссылку.
        var changed = CreateParticipant("Updated", latitude: 59.93, longitude: 30.34, id: original.Id);
        changed.City = "Saint Petersburg";
        var result = await container.UpdateParticipantAsync(changed);

        Assert.True(result.Success);
        var loaded = await container.GetParticipantByIdAsync(original.Id);
        Assert.Equal("Updated", loaded?.Name);
        Assert.Equal("Saint Petersburg", loaded?.City);
        Assert.Equal(59.93, loaded?.Latitude);
        Assert.Equal(1, container.Count);
    }

    [Fact]
    public async Task UpdateParticipantAsync_UnknownId_Fails()
    {
        var container = Manager.GetOrCreateContainer("globe-a");

        var result = await container.UpdateParticipantAsync(CreateParticipant("Ghost"));

        Assert.False(result.Success);
        Assert.Equal(0, container.Count);
    }

    [Fact]
    public async Task RemoveParticipantAsync_DeletesParticipant()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("ToRemove");
        await container.AddParticipantAsync(participant);

        var removed = await container.RemoveParticipantAsync(participant.Id);
        var removedAgain = await container.RemoveParticipantAsync(participant.Id);

        Assert.True(removed.Success);
        Assert.False(removedAgain.Success);
        Assert.Equal(0, container.Count);
        Assert.Null(await container.GetParticipantByIdAsync(participant.Id));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllParticipants()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        await container.AddParticipantsAsync(new[] { CreateParticipant("A"), CreateParticipant("B") });

        var result = await container.ClearAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(0, container.Count);
    }

    // --- События -------------------------------------------------------------

    [Fact]
    public async Task OnDataChanged_ReportsEveryChangeWithContainerId()
    {
        var events = RecordEvents();
        var container = Manager.GetOrCreateContainer("events");
        var participant = CreateParticipant("A");

        await container.AddParticipantAsync(participant);
        await container.AddParticipantsAsync(new[] { CreateParticipant("B"), CreateParticipant("C") });
        await container.UpdateParticipantAsync(CreateParticipant("A2", id: participant.Id));
        await container.RemoveParticipantAsync(participant.Id);
        await container.ClearAsync();

        Assert.Equal(
            new[]
            {
                ("events", GeoDataChangeType.Added),
                ("events", GeoDataChangeType.BulkLoaded),
                ("events", GeoDataChangeType.Updated),
                ("events", GeoDataChangeType.Removed),
                ("events", GeoDataChangeType.Cleared)
            },
            events);
    }

    [Fact]
    public async Task OnDataChanged_SilentWhenNothingChanged()
    {
        var container = Manager.GetOrCreateContainer("events");
        var participant = CreateParticipant("A");
        await container.AddParticipantAsync(participant);
        var events = RecordEvents();

        await container.AddParticipantAsync(participant);          // дубликат
        await container.AddParticipantsAsync(new[] { participant }); // все уже есть
        await container.UpdateParticipantAsync(CreateParticipant("Ghost"));
        await container.RemoveParticipantAsync(Guid.NewGuid());
        await Manager.GetOrCreateContainer("empty").ClearAsync();

        Assert.Empty(events);
    }

    [Fact]
    public async Task RemoveContainer_WithData_ReportsCleared()
    {
        await Manager.GetOrCreateContainer("to-remove").AddParticipantAsync(CreateParticipant("A"));
        var events = RecordEvents();

        Assert.True(Manager.RemoveContainer("to-remove"));

        Assert.Equal(new[] { ("to-remove", GeoDataChangeType.Cleared) }, events);
    }

    // --- Менеджер ------------------------------------------------------------

    [Fact]
    public async Task Containers_KeepDataIsolated()
    {
        var europe = Manager.GetOrCreateContainer("europe");
        var asia = Manager.GetOrCreateContainer("asia");

        await europe.AddParticipantAsync(CreateParticipant("Berlin"));
        await europe.AddParticipantAsync(CreateParticipant("Paris"));
        await asia.AddParticipantAsync(CreateParticipant("Tokyo"));

        Assert.Equal(new[] { "Berlin", "Paris" }, await NamesAsync("europe"));
        Assert.Equal(new[] { "Tokyo" }, await NamesAsync("asia"));
    }

    [Fact]
    public async Task SameParticipantId_CanExistInDifferentContainers()
    {
        var id = Guid.NewGuid();

        var r1 = await Manager.GetOrCreateContainer("europe").AddParticipantAsync(CreateParticipant("Shared", id: id));
        var r2 = await Manager.GetOrCreateContainer("asia").AddParticipantAsync(CreateParticipant("Shared", id: id));

        Assert.True(r1.Success);
        Assert.True(r2.Success);
    }

    [Fact]
    public void BlankContainerId_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => Manager.GetOrCreateContainer(" "));
        Assert.Null(Manager.GetContainer(""));
        Assert.False(Manager.ContainerExists(""));
        Assert.False(Manager.RemoveContainer(""));
    }

    [Fact]
    public async Task LoadDataAsync_ReplacesExistingData()
    {
        await Manager.GetOrCreateContainer("globe").AddParticipantAsync(CreateParticipant("Old"));

        var result = await Manager.LoadDataAsync("globe", new[] { CreateParticipant("New1"), CreateParticipant("New2") });

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(new[] { "New1", "New2" }, await NamesAsync("globe"));
    }

    [Fact]
    public async Task LoadFromJsonAsync_ReadsCamelCaseArray()
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
        Assert.Equal(new[] { "JsonOne", "JsonTwo" }, await NamesAsync("json-globe"));
    }

    [Fact]
    public async Task LoadFromJsonAsync_InvalidJson_Fails()
    {
        var result = await Manager.LoadFromJsonAsync("json-globe", "{ not json");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExportToJsonAsync_RoundTripsThroughLoadFromJson()
    {
        await Manager.LoadDataAsync("source", new[]
        {
            CreateParticipant("Berlin", latitude: 52.52, longitude: 13.405),
            CreateParticipant("Tokyo", latitude: 35.6762, longitude: 139.6503)
        });

        var json = await Manager.ExportToJsonAsync("source");
        var result = await Manager.LoadFromJsonAsync("copy", json);

        Assert.True(result.Success);
        var copy = (await Manager.GetContainer("copy")!.GetAllParticipantsAsync()).OrderBy(p => p.Name).ToList();
        Assert.Equal(new[] { "Berlin", "Tokyo" }, copy.Select(p => p.Name));
        Assert.Equal(35.6762, copy[1].Latitude);
        Assert.Equal(139.6503, copy[1].Longitude);
    }

    [Fact]
    public async Task GetContainerIds_ListsContainersWithData()
    {
        await Manager.GetOrCreateContainer("g1").AddParticipantAsync(CreateParticipant("A"));
        await Manager.GetOrCreateContainer("g2").AddParticipantAsync(CreateParticipant("B"));

        var ids = Manager.GetContainerIds().ToList();

        Assert.Contains("g1", ids);
        Assert.Contains("g2", ids);
    }

    [Fact]
    public async Task RemoveContainer_DeletesData()
    {
        await Manager.GetOrCreateContainer("to-remove").AddParticipantsAsync(new[] { CreateParticipant("A"), CreateParticipant("B") });

        Assert.True(Manager.RemoveContainer("to-remove"));

        Assert.False(Manager.ContainerExists("to-remove"));
        Assert.Equal(0, Manager.GetOrCreateContainer("to-remove").Count);
        Assert.False(Manager.RemoveContainer("never-existed"));
    }

    private List<(string ContainerId, GeoDataChangeType Type)> RecordEvents()
    {
        var events = new List<(string, GeoDataChangeType)>();
        Manager.OnDataChanged += (containerId, type) => events.Add((containerId, type));
        return events;
    }

    private async Task<string[]> NamesAsync(string containerId)
    {
        var participants = await Manager.GetOrCreateContainer(containerId).GetAllParticipantsAsync();
        return participants.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
    }
}
