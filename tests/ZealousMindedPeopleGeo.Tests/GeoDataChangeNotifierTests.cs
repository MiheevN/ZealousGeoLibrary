using Microsoft.Extensions.DependencyInjection;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Services.Synchronization;
using Xunit;

namespace ZealousMindedPeopleGeo.Tests;

public class GeoDataChangeNotifierTests
{
    [Fact]
    public async Task InMemoryContainerChange_PublishesContainerNotification()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataContainers();

        using var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<IGeoDataChangeNotifier>();
        var notifications = new List<GeoDataChangeNotification>();
        notifier.DataChanged += notifications.Add;

        var manager = provider.GetRequiredService<IGeoDataContainerManager>();
        await manager.GetOrCreateContainer("shared").AddParticipantAsync(CreateParticipant("Shared"));

        Assert.Contains(notifications, notification =>
            notification.Source == GeoDataChangeSource.Container &&
            notification.ContainerId == "shared" &&
            notification.ChangeType == GeoDataChangeType.Added);
    }

    [Fact]
    public async Task InMemoryContainerChange_PublishesNotificationWhenLegacySubscriberThrows()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataContainers();

        using var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<IGeoDataChangeNotifier>();
        var notifications = new List<GeoDataChangeNotification>();
        notifier.DataChanged += notifications.Add;

        var manager = provider.GetRequiredService<IGeoDataContainerManager>();
        manager.OnDataChanged += (_, _) => throw new InvalidOperationException("Legacy subscriber failed");

        await manager.GetOrCreateContainer("shared").AddParticipantAsync(CreateParticipant("Shared"));

        Assert.Contains(notifications, notification =>
            notification.Source == GeoDataChangeSource.Container &&
            notification.ContainerId == "shared" &&
            notification.ChangeType == GeoDataChangeType.Added);
    }

    [Fact]
    public async Task InMemoryParticipantRepositoryChanges_PublishRepositoryNotifications()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataChangeNotifications();
        services.AddSingleton<IParticipantRepository, InMemoryParticipantRepository>();

        using var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<IGeoDataChangeNotifier>();
        var notifications = new List<GeoDataChangeNotification>();
        notifier.DataChanged += notifications.Add;

        var repository = provider.GetRequiredService<IParticipantRepository>();
        var participant = CreateParticipant("Repository");

        await repository.AddParticipantAsync(participant);
        participant.Name = "Repository Updated";
        await repository.UpdateParticipantAsync(participant);
        await repository.DeleteParticipantAsync(participant.Id);

        Assert.Contains(notifications, notification =>
            notification.Source == GeoDataChangeSource.ParticipantRepository &&
            notification.ChangeType == GeoDataChangeType.Added &&
            notification.ParticipantId == participant.Id);
        Assert.Contains(notifications, notification =>
            notification.Source == GeoDataChangeSource.ParticipantRepository &&
            notification.ChangeType == GeoDataChangeType.Updated &&
            notification.ParticipantId == participant.Id);
        Assert.Contains(notifications, notification =>
            notification.Source == GeoDataChangeSource.ParticipantRepository &&
            notification.ChangeType == GeoDataChangeType.Removed &&
            notification.ParticipantId == participant.Id);
    }

    [Fact]
    public void Publish_ContinuesWhenSubscriberThrows()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGeoDataChangeNotifications();

        using var provider = services.BuildServiceProvider();
        var notifier = provider.GetRequiredService<IGeoDataChangeNotifier>();
        GeoDataChangeNotification? received = null;

        notifier.DataChanged += _ => throw new InvalidOperationException("Subscriber failed");
        notifier.DataChanged += notification => received = notification;

        var expected = GeoDataChangeNotification.ForContainer("shared", GeoDataChangeType.Updated);
        notifier.Publish(expected);

        Assert.Same(expected, received);
    }

    private static Participant CreateParticipant(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = $"{name.ToLowerInvariant()}@example.com",
        Address = "Test Address",
        Location = "Test Location",
        Latitude = 55.7558,
        Longitude = 37.6176
    };
}
