using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Реализация контейнера гео-данных с хранением в базе данных.
/// Каждый экземпляр работает с точками одного именованного контейнера,
/// отфильтрованными по <see cref="IGeoDataContainer.ContainerId"/>.
/// Для безопасной работы в многопоточной среде (в т.ч. в Blazor)
/// используется <see cref="IDbContextFactory{TContext}"/> — контекст создается
/// на время каждой операции.
/// </summary>
public class DatabaseGeoDataContainer : IGeoDataContainer
{
    private readonly IDbContextFactory<GeoDataDbContext> _contextFactory;
    private readonly ILogger<DatabaseGeoDataContainer>? _logger;
    private readonly Action<string, GeoDataChangeType>? _onDataChanged;

    /// <inheritdoc />
    public string ContainerId { get; }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            using var context = _contextFactory.CreateDbContext();
            return context.Points.Count(p => p.ContainerId == ContainerId);
        }
    }

    /// <summary>
    /// Создает контейнер гео-данных, хранящихся в базе данных
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="contextFactory">Фабрика контекстов БД</param>
    /// <param name="logger">Логгер (опционально)</param>
    /// <param name="onDataChanged">Callback при изменении данных (опционально)</param>
    public DatabaseGeoDataContainer(
        string containerId,
        IDbContextFactory<GeoDataDbContext> contextFactory,
        ILogger<DatabaseGeoDataContainer>? logger = null,
        Action<string, GeoDataChangeType>? onDataChanged = null)
    {
        ContainerId = containerId ?? throw new ArgumentNullException(nameof(containerId));
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger;
        _onDataChanged = onDataChanged;
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> AddPointAsync(GeoPoint point, CancellationToken ct = default)
    {
        var error = point is null ? "Point is null" : point.Validate();
        if (error is not null)
        {
            return GeoDataOperationResult.Fail(error);
        }

        var valid = point!;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var exists = await context.Points
                .AnyAsync(p => p.ContainerId == ContainerId && p.Id == valid.Id, ct);

            if (exists)
            {
                return GeoDataOperationResult.Fail($"Point with ID {valid.Id} already exists in container '{ContainerId}'");
            }

            context.Points.Add(GeoPointEntity.FromPoint(ContainerId, valid));
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Added point {Title} with ID {Id}", ContainerId, valid.Title, valid.Id);

            NotifyDataChanged(GeoDataChangeType.Added);

            return GeoDataOperationResult.OkPoint(valid.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding point {Id}", ContainerId, valid.Id);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> AddPointsAsync(IEnumerable<GeoPoint> points, CancellationToken ct = default)
    {
        if (points is null)
        {
            return GeoDataOperationResult.Fail("Points collection is null");
        }

        try
        {
            var incoming = points.ToList();
            var valid = incoming.Where(p => p is not null && p.Validate() is null).ToList();

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var incomingIds = valid.Select(p => p.Id).Distinct().ToList();
            var existingIds = incomingIds.Count == 0
                ? new List<string>()
                : await context.Points
                    .Where(p => p.ContainerId == ContainerId && incomingIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync(ct);

            // Пропускаем уже существующие в БД и повторы внутри входного набора
            var taken = new HashSet<string>(existingIds, StringComparer.Ordinal);
            var added = 0;

            foreach (var point in valid)
            {
                if (taken.Add(point.Id))
                {
                    context.Points.Add(GeoPointEntity.FromPoint(ContainerId, point));
                    added++;
                }
            }

            if (added > 0)
            {
                await context.SaveChangesAsync(ct);
                NotifyDataChanged(GeoDataChangeType.BulkLoaded);
            }

            var skipped = incoming.Count - added;
            _logger?.LogInformation("Container '{ContainerId}': Added {Added} points, skipped {Skipped}", ContainerId, added, skipped);

            var result = GeoDataOperationResult.Ok(added);
            result.SkippedCount = skipped;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding multiple points", ContainerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<GeoPoint>> GetPointsAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entities = await context.Points
                .AsNoTracking()
                .Where(p => p.ContainerId == ContainerId)
                .ToListAsync(ct);

            return entities.Select(e => e.ToPoint()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting points", ContainerId);
            return Array.Empty<GeoPoint>();
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoPoint?> GetPointAsync(string id, CancellationToken ct = default)
    {
        if (id is null)
        {
            return null;
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = await context.Points
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ContainerId == ContainerId && p.Id == id, ct);

            return entity?.ToPoint();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting point {Id}", ContainerId, id);
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> UpdatePointAsync(GeoPoint point, CancellationToken ct = default)
    {
        var error = point is null ? "Point is null" : point.Validate();
        if (error is not null)
        {
            return GeoDataOperationResult.Fail(error);
        }

        var valid = point!;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = await context.Points
                .FirstOrDefaultAsync(p => p.ContainerId == ContainerId && p.Id == valid.Id, ct);

            if (entity == null)
            {
                return GeoDataOperationResult.Fail($"Point with ID {valid.Id} not found in container '{ContainerId}'");
            }

            entity.UpdateFrom(valid);
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Updated point {Title} with ID {Id}", ContainerId, valid.Title, valid.Id);

            NotifyDataChanged(GeoDataChangeType.Updated);

            return GeoDataOperationResult.OkPoint(valid.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error updating point {Id}", ContainerId, valid.Id);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> RemovePointAsync(string id, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var removed = id is null
                ? 0
                : await context.Points
                    .Where(p => p.ContainerId == ContainerId && p.Id == id)
                    .ExecuteDeleteAsync(ct);

            if (removed == 0)
            {
                return GeoDataOperationResult.Fail($"Point with ID {id} not found in container '{ContainerId}'");
            }

            _logger?.LogInformation("Container '{ContainerId}': Removed point with ID {Id}", ContainerId, id);

            NotifyDataChanged(GeoDataChangeType.Removed);

            return GeoDataOperationResult.OkPoint(id!);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error removing point {Id}", ContainerId, id);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> ClearAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var count = await context.Points
                .Where(p => p.ContainerId == ContainerId)
                .ExecuteDeleteAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Cleared {Count} points", ContainerId, count);

            if (count > 0)
            {
                NotifyDataChanged(GeoDataChangeType.Cleared);
            }

            return GeoDataOperationResult.Ok(count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error clearing container", ContainerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
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
