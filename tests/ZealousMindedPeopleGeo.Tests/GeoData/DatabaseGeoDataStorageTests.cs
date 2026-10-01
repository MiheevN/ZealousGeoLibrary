using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Хранилище гео-данных в БД (SQLite in-memory): общий контракт плюс собственная
/// семантика — контейнер существует, пока в нём есть данные, и данные переживают
/// пересоздание менеджера.
/// </summary>
public sealed class DatabaseGeoDataStorageTests : GeoDataStorageContractTests
{
    // In-memory база SQLite живёт, пока открыто соединение.
    private readonly SqliteConnection _connection;
    private readonly List<ServiceProvider> _providers = new();

    public DatabaseGeoDataStorageTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Manager = CreateManager();
    }

    protected override IGeoDataContainerManager Manager { get; }

    [Fact]
    public async Task ContainerExists_OnlyWhileItHasData()
    {
        Manager.GetOrCreateContainer("empty");
        await Manager.GetOrCreateContainer("filled").AddParticipantAsync(CreateParticipant("A"));

        Assert.False(Manager.ContainerExists("empty"));
        Assert.Null(Manager.GetContainer("empty"));
        Assert.DoesNotContain("empty", Manager.GetContainerIds());
        Assert.True(Manager.ContainerExists("filled"));
        Assert.NotNull(Manager.GetContainer("filled"));
        Assert.False(Manager.RemoveContainer("empty"));
    }

    [Fact]
    public async Task Data_SurvivesNewManagerInstance()
    {
        await Manager.LoadDataAsync("persisted", new[] { CreateParticipant("Berlin"), CreateParticipant("Tokyo") });

        var restarted = CreateManager();

        Assert.True(restarted.ContainerExists("persisted"));
        Assert.Equal(2, restarted.GetContainer("persisted")!.Count);
    }

    [Fact]
    public async Task SocialContacts_RoundTripThroughDatabase()
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

        Assert.NotNull(loaded?.SocialContacts);
        Assert.Equal("https://t.me/example", loaded!.SocialContacts!.Telegram);
        Assert.Equal("https://example.com", loaded.SocialContacts.Website);
        Assert.Null(loaded.SocialContacts.Discord);
    }

    public override void Dispose()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
        _connection.Dispose();
        base.Dispose();
    }

    private IGeoDataContainerManager CreateManager()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataDatabase(options => options.UseSqlite(_connection));

        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        provider.EnsureGeoDataDatabaseCreatedAsync().GetAwaiter().GetResult();
        return provider.GetRequiredService<IGeoDataContainerManager>();
    }
}
