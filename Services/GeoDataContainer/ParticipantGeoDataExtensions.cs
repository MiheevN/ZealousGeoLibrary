using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Участники сообщества в контейнерах гео-данных. Контейнер хранит участника как
/// точку (<see cref="ParticipantGeoPointExtensions.ToGeoPoint"/>), а эти методы
/// переводят его туда и обратно, поэтому код, написанный для участников, работает
/// без изменений.
/// </summary>
public static class ParticipantGeoDataExtensions
{
    /// <summary>
    /// Добавляет участника. Участник без координат не добавляется.
    /// </summary>
    public static ValueTask<GeoDataOperationResult> AddParticipantAsync(
        this IGeoDataContainer container, Participant participant, CancellationToken ct = default)
    {
        var error = CheckParticipant(participant);
        return error is null
            ? container.AddPointAsync(participant.ToGeoPoint(), ct)
            : ValueTask.FromResult(GeoDataOperationResult.Fail(error));
    }

    /// <summary>
    /// Добавляет участников. Пропускает <c>null</c>, участников без координат и повторы.
    /// </summary>
    public static async ValueTask<GeoDataOperationResult> AddParticipantsAsync(
        this IGeoDataContainer container, IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        if (participants is null)
        {
            return GeoDataOperationResult.Fail("Participants collection is null");
        }

        var points = new List<GeoPoint>();
        var skipped = 0;
        foreach (var participant in participants)
        {
            if (CheckParticipant(participant) is null)
            {
                points.Add(participant.ToGeoPoint());
            }
            else
            {
                skipped++;
            }
        }

        var result = await container.AddPointsAsync(points, ct);
        result.SkippedCount += skipped;
        return result;
    }

    /// <summary>
    /// Все точки контейнера в виде участников
    /// </summary>
    public static async ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(
        this IGeoDataContainer container, CancellationToken ct = default)
    {
        var points = await container.GetPointsAsync(ct);
        return points.Select(point => point.ToParticipant()).ToList();
    }

    /// <summary>
    /// Участник по идентификатору или <c>null</c>
    /// </summary>
    public static async ValueTask<Participant?> GetParticipantByIdAsync(
        this IGeoDataContainer container, Guid id, CancellationToken ct = default)
    {
        var point = await FindPointAsync(container, id, ct);
        return point?.ToParticipant();
    }

    /// <summary>
    /// Заменяет данные участника с тем же идентификатором
    /// </summary>
    public static async ValueTask<GeoDataOperationResult> UpdateParticipantAsync(
        this IGeoDataContainer container, Participant participant, CancellationToken ct = default)
    {
        var error = CheckParticipant(participant);
        if (error is not null)
        {
            return GeoDataOperationResult.Fail(error);
        }

        var point = participant.ToGeoPoint();
        var stored = await FindPointAsync(container, participant.Id, ct);
        if (stored is not null)
        {
            // Точка могла быть добавлена с идентификатором не в виде GUID.
            point.Id = stored.Id;
        }

        return await container.UpdatePointAsync(point, ct);
    }

    /// <summary>
    /// Удаляет участника
    /// </summary>
    public static async ValueTask<GeoDataOperationResult> RemoveParticipantAsync(
        this IGeoDataContainer container, Guid id, CancellationToken ct = default)
    {
        var stored = await FindPointAsync(container, id, ct);
        return await container.RemovePointAsync(stored?.Id ?? id.ToString(), ct);
    }

    /// <summary>
    /// Заменяет данные контейнера участниками
    /// </summary>
    public static async ValueTask<GeoDataOperationResult> LoadDataAsync(
        this IGeoDataContainerManager manager,
        string containerId,
        IEnumerable<Participant> participants,
        CancellationToken ct = default)
    {
        if (participants is null)
        {
            return GeoDataOperationResult.Fail("Participants collection is null");
        }

        var list = participants.ToList();
        var points = list.Where(p => CheckParticipant(p) is null).Select(p => p.ToGeoPoint()).ToList();

        var result = await manager.LoadPointsAsync(containerId, points, ct);
        result.SkippedCount += list.Count - points.Count;
        return result;
    }

    private static string? CheckParticipant(Participant? participant)
    {
        if (participant is null)
        {
            return "Participant is null";
        }

        return participant.Latitude is null || participant.Longitude is null
            ? $"Participant '{participant.Name}' ({participant.Id}) has no coordinates"
            : null;
    }

    // Участник видит точку под GUID: самим идентификатором точки или вычисленным из него
    // (ParticipantIdFor). Сначала ищем напрямую, затем среди точек с иным видом Id.
    private static async ValueTask<GeoPoint?> FindPointAsync(IGeoDataContainer container, Guid id, CancellationToken ct)
    {
        var point = await container.GetPointAsync(id.ToString(), ct);
        if (point is not null)
        {
            return point;
        }

        var points = await container.GetPointsAsync(ct);
        return points.FirstOrDefault(p => ParticipantGeoPointExtensions.ParticipantIdFor(p.Id) == id);
    }
}
