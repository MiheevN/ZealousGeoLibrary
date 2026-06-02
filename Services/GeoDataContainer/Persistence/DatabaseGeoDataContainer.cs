using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Реализация контейнера гео-данных с хранением в базе данных.
/// Каждый экземпляр работает с данными одного именованного контейнера (глобуса),
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
            return context.Participants.Count(p => p.ContainerId == ContainerId);
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
    public async ValueTask<GeoDataOperationResult> AddParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        if (participant == null)
        {
            return GeoDataOperationResult.Fail("Participant is null");
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var exists = await context.Participants
                .AnyAsync(p => p.ContainerId == ContainerId && p.Id == participant.Id, ct);

            if (exists)
            {
                return GeoDataOperationResult.Fail($"Participant with ID {participant.Id} already exists in container '{ContainerId}'");
            }

            context.Participants.Add(GeoDataParticipantEntity.FromParticipant(ContainerId, participant));
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Added participant {Name} with ID {Id}", ContainerId, participant.Name, participant.Id);

            NotifyDataChanged(GeoDataChangeType.Added);

            return GeoDataOperationResult.Ok(1, participant.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding participant {Name}", ContainerId, participant.Name);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> AddParticipantsAsync(IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        if (participants == null)
        {
            return GeoDataOperationResult.Fail("Participants collection is null");
        }

        try
        {
            var participantList = participants.Where(p => p != null).ToList();
            if (participantList.Count == 0)
            {
                return GeoDataOperationResult.Ok(0);
            }

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var incomingIds = participantList.Select(p => p.Id).ToList();
            var existingIds = await context.Participants
                .Where(p => p.ContainerId == ContainerId && incomingIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);

            var existingSet = existingIds.ToHashSet();
            var seen = new HashSet<Guid>();
            var addedCount = 0;

            foreach (var participant in participantList)
            {
                // Пропускаем уже существующих в БД и дубликаты внутри входного набора
                if (existingSet.Contains(participant.Id) || !seen.Add(participant.Id))
                {
                    continue;
                }

                context.Participants.Add(GeoDataParticipantEntity.FromParticipant(ContainerId, participant));
                addedCount++;
            }

            if (addedCount > 0)
            {
                await context.SaveChangesAsync(ct);
                NotifyDataChanged(GeoDataChangeType.BulkLoaded);
            }

            _logger?.LogInformation("Container '{ContainerId}': Added {Count} participants", ContainerId, addedCount);

            return GeoDataOperationResult.Ok(addedCount);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding multiple participants", ContainerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entities = await context.Participants
                .AsNoTracking()
                .Where(p => p.ContainerId == ContainerId)
                .ToListAsync(ct);

            _logger?.LogDebug("Container '{ContainerId}': Retrieved {Count} participants", ContainerId, entities.Count);

            return entities.Select(e => e.ToParticipant()).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting all participants", ContainerId);
            return Enumerable.Empty<Participant>();
        }
    }

    /// <inheritdoc />
    public async ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = await context.Participants
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ContainerId == ContainerId && p.Id == id, ct);

            if (entity != null)
            {
                _logger?.LogDebug("Container '{ContainerId}': Found participant {Name} with ID {Id}", ContainerId, entity.Name, id);
                return entity.ToParticipant();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting participant by ID {Id}", ContainerId, id);
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        if (participant == null)
        {
            return GeoDataOperationResult.Fail("Participant is null");
        }

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = await context.Participants
                .FirstOrDefaultAsync(p => p.ContainerId == ContainerId && p.Id == participant.Id, ct);

            if (entity == null)
            {
                return GeoDataOperationResult.Fail($"Participant with ID {participant.Id} not found in container '{ContainerId}'");
            }

            entity.UpdateFrom(participant);
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Updated participant {Name} with ID {Id}", ContainerId, participant.Name, participant.Id);

            NotifyDataChanged(GeoDataChangeType.Updated);

            return GeoDataOperationResult.Ok(1, participant.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error updating participant {Name}", ContainerId, participant.Name);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> RemoveParticipantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var entity = await context.Participants
                .FirstOrDefaultAsync(p => p.ContainerId == ContainerId && p.Id == id, ct);

            if (entity == null)
            {
                return GeoDataOperationResult.Fail($"Participant with ID {id} not found in container '{ContainerId}'");
            }

            context.Participants.Remove(entity);
            await context.SaveChangesAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Removed participant {Name} with ID {Id}", ContainerId, entity.Name, id);

            NotifyDataChanged(GeoDataChangeType.Removed);

            return GeoDataOperationResult.Ok(1, id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error removing participant with ID {Id}", ContainerId, id);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async ValueTask<GeoDataOperationResult> ClearAsync(CancellationToken ct = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var count = await context.Participants
                .Where(p => p.ContainerId == ContainerId)
                .ExecuteDeleteAsync(ct);

            _logger?.LogInformation("Container '{ContainerId}': Cleared {Count} participants", ContainerId, count);

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
