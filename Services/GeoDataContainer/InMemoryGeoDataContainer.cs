using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// In-memory реализация контейнера гео-данных.
/// Thread-safe реализация с использованием ConcurrentDictionary.
/// </summary>
public class InMemoryGeoDataContainer : IGeoDataContainer
{
    private readonly ConcurrentDictionary<Guid, Participant> _participants = new();
    private readonly ILogger<InMemoryGeoDataContainer>? _logger;
    private readonly Action<string, GeoDataChangeType>? _onDataChanged;

    /// <inheritdoc />
    public string ContainerId { get; }

    /// <inheritdoc />
    public int Count => _participants.Count;

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
    public ValueTask<GeoDataOperationResult> AddParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        if (participant == null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail("Participant is null"));
        }

        try
        {
            if (_participants.ContainsKey(participant.Id))
            {
                return ValueTask.FromResult(GeoDataOperationResult.Fail($"Participant with ID {participant.Id} already exists in container '{ContainerId}'"));
            }

            _participants[participant.Id] = participant;
            _logger?.LogInformation("Container '{ContainerId}': Added participant {Name} with ID {Id}", ContainerId, participant.Name, participant.Id);

            NotifyDataChanged(GeoDataChangeType.Added);

            return ValueTask.FromResult(GeoDataOperationResult.Ok(1, participant.Id));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding participant {Name}", ContainerId, participant.Name);
            return ValueTask.FromResult(GeoDataOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> AddParticipantsAsync(IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        if (participants == null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail("Participants collection is null"));
        }

        try
        {
            var participantList = participants.ToList();
            var addedCount = 0;

            foreach (var participant in participantList)
            {
                if (participant != null && !_participants.ContainsKey(participant.Id))
                {
                    _participants[participant.Id] = participant;
                    addedCount++;
                }
            }

            _logger?.LogInformation("Container '{ContainerId}': Added {Count} participants", ContainerId, addedCount);

            if (addedCount > 0)
            {
                NotifyDataChanged(GeoDataChangeType.BulkLoaded);
            }

            return ValueTask.FromResult(GeoDataOperationResult.Ok(addedCount));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error adding multiple participants", ContainerId);
            return ValueTask.FromResult(GeoDataOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default)
    {
        try
        {
            var participants = _participants.Values.ToList();
            _logger?.LogDebug("Container '{ContainerId}': Retrieved {Count} participants", ContainerId, participants.Count);
            return ValueTask.FromResult<IEnumerable<Participant>>(participants);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting all participants", ContainerId);
            return ValueTask.FromResult<IEnumerable<Participant>>(Enumerable.Empty<Participant>());
        }
    }

    /// <inheritdoc />
    public ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _participants.TryGetValue(id, out var participant);
            if (participant != null)
            {
                _logger?.LogDebug("Container '{ContainerId}': Found participant {Name} with ID {Id}", ContainerId, participant.Name, id);
            }
            return ValueTask.FromResult(participant);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error getting participant by ID {Id}", ContainerId, id);
            return ValueTask.FromResult<Participant?>(null);
        }
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        if (participant == null)
        {
            return ValueTask.FromResult(GeoDataOperationResult.Fail("Participant is null"));
        }

        try
        {
            if (!_participants.ContainsKey(participant.Id))
            {
                return ValueTask.FromResult(GeoDataOperationResult.Fail($"Participant with ID {participant.Id} not found in container '{ContainerId}'"));
            }

            _participants[participant.Id] = participant;
            _logger?.LogInformation("Container '{ContainerId}': Updated participant {Name} with ID {Id}", ContainerId, participant.Name, participant.Id);

            NotifyDataChanged(GeoDataChangeType.Updated);

            return ValueTask.FromResult(GeoDataOperationResult.Ok(1, participant.Id));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error updating participant {Name}", ContainerId, participant.Name);
            return ValueTask.FromResult(GeoDataOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> RemoveParticipantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (_participants.TryRemove(id, out var removedParticipant))
            {
                _logger?.LogInformation("Container '{ContainerId}': Removed participant {Name} with ID {Id}", ContainerId, removedParticipant.Name, id);
                NotifyDataChanged(GeoDataChangeType.Removed);
                return ValueTask.FromResult(GeoDataOperationResult.Ok(1, id));
            }

            return ValueTask.FromResult(GeoDataOperationResult.Fail($"Participant with ID {id} not found in container '{ContainerId}'"));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error removing participant with ID {Id}", ContainerId, id);
            return ValueTask.FromResult(GeoDataOperationResult.Fail(ex.Message));
        }
    }

    /// <inheritdoc />
    public ValueTask<GeoDataOperationResult> ClearAsync(CancellationToken ct = default)
    {
        try
        {
            var count = _participants.Count;
            _participants.Clear();
            _logger?.LogInformation("Container '{ContainerId}': Cleared {Count} participants", ContainerId, count);

            // Как и хранилище в БД: нет изменений — нет события.
            if (count > 0)
            {
                NotifyDataChanged(GeoDataChangeType.Cleared);
            }

            return ValueTask.FromResult(GeoDataOperationResult.Ok(count));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Container '{ContainerId}': Error clearing container", ContainerId);
            return ValueTask.FromResult(GeoDataOperationResult.Fail(ex.Message));
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
