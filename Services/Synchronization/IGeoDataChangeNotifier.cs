namespace ZealousMindedPeopleGeo.Services.Synchronization;

/// <summary>
/// Publishes geo-data changes to active Blazor components without polling.
/// </summary>
public interface IGeoDataChangeNotifier
{
    event Action<GeoDataChangeNotification>? DataChanged;

    void Publish(GeoDataChangeNotification notification);
}
