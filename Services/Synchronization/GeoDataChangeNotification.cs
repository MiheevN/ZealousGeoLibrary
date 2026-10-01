using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo.Services.Synchronization;

/// <summary>
/// Describes a geo-data change that active Blazor components can react to.
/// </summary>
/// <param name="Source">Origin of the change.</param>
/// <param name="ChangeType">Kind of data mutation.</param>
/// <param name="ContainerId">Changed geo-data container ID, when the source is a container.</param>
/// <param name="ParticipantId">Changed participant ID, when available.</param>
/// <param name="ChangedAtUtc">UTC timestamp when the notification was created.</param>
public sealed record GeoDataChangeNotification(
    GeoDataChangeSource Source,
    GeoDataChangeType ChangeType,
    string? ContainerId,
    Guid? ParticipantId,
    DateTimeOffset ChangedAtUtc)
{
    public static GeoDataChangeNotification ForContainer(
        string containerId,
        GeoDataChangeType changeType,
        Guid? participantId = null)
        => new(
            GeoDataChangeSource.Container,
            changeType,
            containerId,
            participantId,
            DateTimeOffset.UtcNow);

    public static GeoDataChangeNotification ForParticipantRepository(
        GeoDataChangeType changeType,
        Guid? participantId = null)
        => new(
            GeoDataChangeSource.ParticipantRepository,
            changeType,
            null,
            participantId,
            DateTimeOffset.UtcNow);
}

public enum GeoDataChangeSource
{
    Container,
    ParticipantRepository
}
