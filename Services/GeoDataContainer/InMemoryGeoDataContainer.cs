using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Контейнер гео-данных в памяти, потокобезопасный.
/// Хранит копии точек, поэтому, как и хранилище в БД, не зависит от объектов,
/// которые приложение меняет после добавления.
/// </summary>
public class InMemoryGeoDataContainer : IGeoDataContainer
{
    private readonly ConcurrentDictionary<string, GeoPoint> _points = new(StringComparer.Ordinal);
    private readonly ILogger<InMemoryGeoDataContainer>? _logger;
    private readonly Action<string, GeoDataChangeType>? _onDataChanged;

    /// <inheritdoc />
    public string ContainerId { get; }

    /// <inheritdoc />
    public int Count => _points.Count;

    /// <summary>
    /// Создает новый контейнер гео-данных
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="logger">Логгер (опционально)</param>
    /// <param name="onDataChanged">Callback при изменении данных (опционально)</param>
    public InMemoryGeoDataContainer(
        string containerId,
        ILogger<InMemoryGeoDataContainer>? logger = null,
        Action<string, GeoDataChangeType>? onDataChanged = null)
    {
        ContainerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
        _logger = logger;
        _onDataChanged = onDataChanged;
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> AddPointAsync(GeoPoint point, CancellationToken ct = default)
    {
        var error = point is null ? "Point is null" : point.Validate();
        if (error is not null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail(error));
        }

        if (!_points.TryAdd(point!.Id, point.Clone()))
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail(
                $"Point with ID {point.Id} already exists in container '{ContainerId}'"));
        }

        _logger?.LogInformation("Container '{ContainerId}': Added point {Title} with ID {Id}", ContainerId, point.Title, point.Id);
        NotifyDataChanged(GeoDataChangeType.Added);

        return ValueTask.FromResult(GeoDataOperationResult.OkPoint(point.Id));
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> AddPointsAsync(IEnumerable<GeoPoint> points, CancellationToken ct = default)
    {
        if (points is null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail("Points collection is null"));
        }

        var added = 0;
        var skipped = 0;

        foreach (var point in points)
        {
            if (point is not null && point.Validate() is null && _points.TryAdd(point.Id, point.Clone()))
            {
                added++;
            }
            else
            {
                skipped++;
            }
        }

        _logger?.LogInformation("Container '{ContainerId}': Added {Added} points, skipped {Skipped}", ContainerId, added, skipped);

        if (added > 0)
        {
            NotifyDataChanged(GeoDataChangeType.BulkLoaded);
        }

        var result = GeoDataOperationResult.Ok(added);
        result.SkippedCount = skipped;
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<GeoPoint>> GetPointsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<GeoPoint> points = _points.Values.Select(point => point.Clone()).ToList();
        return ValueTask.FromResult(points);
    }

    /// <inheritdoc />
    public ValueTask<GeoPoint?> GetPointAsync(string id, CancellationToken ct = default)
    {
        var point = id is not null && _points.TryGetValue(id, out var stored) ? stored.Clone() : null;
        return ValueTask.FromResult(point);
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> UpdatePointAsync(GeoPoint point, CancellationToken ct = default)
    {
        var error = point is null ? "Point is null" : point.Validate();
        if (error is not null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail(error));
        }

        // Заменяем только существующую точку; если её параллельно изменили, пробуем снова.
        var replacement = point!.Clone();
        var updated = false;
        while (_points.TryGetValue(point.Id, out var current))
        {
            if (_points.TryUpdate(point.Id, replacement, current))
            {
                updated = true;
                break;
            }
        }

        if (!updated)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail(
                $"Point with ID {point.Id} not found in container '{ContainerId}'"));
        }

        _logger?.LogInformation("Container '{ContainerId}': Updated point {Title} with ID {Id}", ContainerId, point.Title, point.Id);
        NotifyDataChanged(GeoDataChangeType.Updated);

        return ValueTask.FromResult(GeoDataOperationResult.OkPoint(point.Id));
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> RemovePointAsync(string id, CancellationToken ct = default)
    {
        if (id is null || !_points.TryRemove(id, out var removed))
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail(
                $"Point with ID {id} not found in container '{ContainerId}'"));
        }

        _logger?.LogInformation("Container '{ContainerId}': Removed point {Title} with ID {Id}", ContainerId, removed.Title, id);
        NotifyDataChanged(GeoDataChangeType.Removed);

        return ValueTask.FromResult(GeoDataOperationResult.OkPoint(id));
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> ClearAsync(CancellationToken ct = default)
    {
        var count = _points.Count;
        _points.Clear();
        _logger?.LogInformation("Container '{ContainerId}': Cleared {Count} points", ContainerId, count);

        // Как и хранилище в БД: нет изменений — нет события.
        if (count > 0)
        {
            NotifyDataChanged(GeoDataChangeType.Cleared);
        }

        return ValueTask.FromResult(GeoDataOperationResult.Ok(count));
    }

    private void NotifyDataChanged(GeoDataChangeType changeType)
    {
        try
        {
            _onDataChanged?.Invoke(ContainerId, changeType);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error notifying data changed", ContainerId);
        }
    }
}
