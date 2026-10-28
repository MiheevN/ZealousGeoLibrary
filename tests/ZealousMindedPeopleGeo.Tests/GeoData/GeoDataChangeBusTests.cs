using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo.Tests.GeoData;

/// <summary>
/// Шина изменений связывает серверы: изменение на одном доходит до подписчиков
/// OnDataChanged на другом, без повторов и петель.
/// </summary>
public class GeoDataChangeBusTests
{
    private readonly SharedBus _bus = new();

    [Fact]
    public async Task ChangeOnOneServer_ReachesSubscribersOnAnother()
    {
        var first = Server();
        var second = Server();
        var onFirst = Record(first);
        var onSecond = Record(second);

        await first.GetOrCreateContainer("team").AddPointAsync(Point("berlin"));

        Assert.Equal(new[] { ("team", GeoDataChangeType.Added) }, onFirst);
        Assert.Equal(new[] { ("team", GeoDataChangeType.Added) }, onSecond);
        // Сервер-получатель не пересылает чужое изменение дальше.
        Assert.Single(_bus.Published);
    }

    [Fact]
    public async Task OwnMessageComingBackFromTheBus_IsNotRepeated()
    {
        var server = Server();
        var events = Record(server);

        await server.GetOrCreateContainer("team").AddPointAsync(Point("berlin"));
        await server.GetOrCreateContainer("team").RemovePointAsync("berlin");

        Assert.Equal(new[] { ("team", GeoDataChangeType.Added), ("team", GeoDataChangeType.Removed) }, events);
    }

    [Fact]
    public async Task RemovedContainer_IsReportedToOtherServers()
    {
        var first = Server();
        var second = Server();
        await first.GetOrCreateContainer("team").AddPointAsync(Point("berlin"));
        var onSecond = Record(second);

        first.RemoveContainer("team");

        Assert.Equal(new[] { ("team", GeoDataChangeType.Cleared) }, onSecond);
    }

    [Fact]
    public async Task BrokenBus_DoesNotBreakTheChangeOrLocalSubscribers()
    {
        _bus.Fail = true;
        var server = Server();
        var events = Record(server);

        var result = await server.GetOrCreateContainer("team").AddPointAsync(Point("berlin"));

        Assert.True(result.Success);
        Assert.Equal(new[] { ("team", GeoDataChangeType.Added) }, events);
        Assert.Single(await server.GetOrCreateContainer("team").GetPointsAsync());
    }

    [Fact]
    public void BusRegisteredByTheApplication_IsUsedByTheBuiltInManager()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IGeoDataChangeBus>(_bus);
        services.AddZealousMindedPeopleGeo();
        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<IGeoDataContainerManager>();
        var events = Record(manager);

        _bus.Deliver(new GeoDataChange("team", GeoDataChangeType.BulkLoaded, Guid.NewGuid()));

        Assert.Equal(new[] { ("team", GeoDataChangeType.BulkLoaded) }, events);
    }

    private GeoDataContainerManager Server() =>
        new(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance, _bus);

    private static List<(string, GeoDataChangeType)> Record(IGeoDataContainerManager manager)
    {
        var events = new List<(string, GeoDataChangeType)>();
        manager.OnDataChanged += (containerId, type) => events.Add((containerId, type));
        return events;
    }

    private static GeoPoint Point(string id) => new() { Id = id, Title = id, Latitude = 52.52, Longitude = 13.40 };

    /// <summary>
    /// Шина как Redis pub/sub: сообщение получают все подписчики, отправитель тоже.
    /// </summary>
    private sealed class SharedBus : IGeoDataChangeBus
    {
        public List<GeoDataChange> Published { get; } = new();

        public bool Fail { get; set; }

        public event Action<GeoDataChange>? ChangeReceived;

        public ValueTask PublishAsync(GeoDataChange change, CancellationToken ct = default)
        {
            if (Fail)
            {
                throw new InvalidOperationException("bus is down");
            }

            Published.Add(change);
            Deliver(change);
            return ValueTask.CompletedTask;
        }

        public void Deliver(GeoDataChange change) => ChangeReceived?.Invoke(change);
    }
}
