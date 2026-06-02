using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using Xunit;

namespace ZealousMindedPeopleGeo.Tests;

/// <summary>
/// Тесты контейнера гео-данных с хранением в базе данных.
/// Проверяют сценарии из задачи #55: добавление объектов по очереди,
/// загрузка из массива объектов и работа с несколькими глобусами.
/// </summary>
public class DatabaseGeoDataContainerTests : DatabaseGeoDataTestBase
{
    [Fact]
    public async Task AddParticipantAsync_PersistsSingleParticipant()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("Alice");

        var result = await container.AddParticipantAsync(participant);

        Assert.True(result.Success);
        Assert.Equal(participant.Id, result.RecordId);
        Assert.Equal(1, container.Count);

        var loaded = await container.GetParticipantByIdAsync(participant.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Alice", loaded!.Name);
    }

    [Fact]
    public async Task AddParticipantAsync_AddingObjectsOneByOne_AccumulatesData()
    {
        var container = Manager.GetOrCreateContainer("globe-a");

        await container.AddParticipantAsync(CreateParticipant("First"));
        await container.AddParticipantAsync(CreateParticipant("Second"));
        await container.AddParticipantAsync(CreateParticipant("Third"));

        var all = (await container.GetAllParticipantsAsync()).ToList();
        Assert.Equal(3, all.Count);
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
        Assert.Equal(1, container.Count);
    }

    [Fact]
    public async Task AddParticipantsAsync_LoadsFromArray()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participants = new[]
        {
            CreateParticipant("One"),
            CreateParticipant("Two"),
            CreateParticipant("Three")
        };

        var result = await container.AddParticipantsAsync(participants);

        Assert.True(result.Success);
        Assert.Equal(3, result.ProcessedCount);
        Assert.Equal(3, container.Count);
    }

    [Fact]
    public async Task AddParticipantsAsync_SkipsDuplicatesWithinBatchAndExisting()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var shared = CreateParticipant("Shared");
        await container.AddParticipantAsync(shared);

        var newA = CreateParticipant("NewA");
        var newB = CreateParticipant("NewB");
        var batch = new[]
        {
            shared, // уже существует в БД -> пропускается
            newA,
            newA,   // дубль внутри набора -> учитывается один раз
            newB
        };

        var result = await container.AddParticipantsAsync(batch);

        Assert.True(result.Success);
        Assert.Equal(2, result.ProcessedCount); // newA + newB
        Assert.Equal(3, container.Count);       // shared + newA + newB
    }

    [Fact]
    public async Task UpdateParticipantAsync_ChangesStoredData()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("Original");
        await container.AddParticipantAsync(participant);

        participant.Name = "Updated";
        participant.City = "Moscow";
        var result = await container.UpdateParticipantAsync(participant);

        Assert.True(result.Success);
        var loaded = await container.GetParticipantByIdAsync(participant.Id);
        Assert.Equal("Updated", loaded!.Name);
        Assert.Equal("Moscow", loaded.City);
    }

    [Fact]
    public async Task RemoveParticipantAsync_DeletesData()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("ToRemove");
        await container.AddParticipantAsync(participant);

        var result = await container.RemoveParticipantAsync(participant.Id);

        Assert.True(result.Success);
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

    [Fact]
    public async Task SocialContacts_RoundTripsThroughDatabase()
    {
        var container = Manager.GetOrCreateContainer("globe-a");
        var participant = CreateParticipant("Social");
        participant.SocialContacts = new SocialContacts
        {
            Telegram = "https://t.me/example",
            Website = "https://example.com"
        };
        await container.AddParticipantAsync(participant);

        var loaded = await container.GetParticipantByIdAsync(participant.Id);

        Assert.NotNull(loaded!.SocialContacts);
        Assert.Equal("https://t.me/example", loaded.SocialContacts!.Telegram);
        Assert.Equal("https://example.com", loaded.SocialContacts.Website);
        Assert.Null(loaded.SocialContacts.Discord);
    }
}
