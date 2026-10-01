using Microsoft.Extensions.Logging;

namespace ZealousMindedPeopleGeo.Services.Synchronization;

/// <summary>
/// Process-wide change bus for geo-data updates.
/// In Blazor Server, component subscribers are re-rendered through the normal circuit transport.
/// </summary>
public sealed class GeoDataChangeNotifier : IGeoDataChangeNotifier
{
    private readonly ILogger<GeoDataChangeNotifier> _logger;

    public GeoDataChangeNotifier(ILogger<GeoDataChangeNotifier> logger)
    {
        _logger = logger;
    }

    public event Action<GeoDataChangeNotification>? DataChanged;

    public void Publish(GeoDataChangeNotification notification)
    {
        var subscribers = DataChanged;
        if (subscribers == null)
        {
            return;
        }

        foreach (var subscriber in subscribers.GetInvocationList().Cast<Action<GeoDataChangeNotification>>())
        {
            try
            {
                subscriber(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Geo-data change subscriber failed for source {Source} and change {ChangeType}",
                    notification.Source,
                    notification.ChangeType);
            }
        }
    }
}
