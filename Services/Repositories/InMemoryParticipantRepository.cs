using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Synchronization;

namespace ZealousMindedPeopleGeo.Services.Repositories;

/// <summary>
/// Реализация репозитория участников в памяти (In-Memory)
/// Thread-safe реализация с использованием ConcurrentDictionary
/// </summary>
public class InMemoryParticipantRepository : IParticipantRepository
{
    private readonly ConcurrentDictionary<Guid, Participant> _participants = new();
    private readonly ILogger<InMemoryParticipantRepository> _logger;
    private readonly IGeoDataChangeNotifier? _changeNotifier;

    public InMemoryParticipantRepository(
        ILogger<InMemoryParticipantRepository> logger,
        IGeoDataChangeNotifier? changeNotifier = null)
    {
        _logger = logger;
        _changeNotifier = changeNotifier;
    }

    public ValueTask<RepositoryResult> AddParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        try
        {
            if (participant == null)
            {
                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = "Participant is null"
                });
            }

            // Проверяем, существует ли уже участник с таким ID
            if (_participants.ContainsKey(participant.Id))
            {
                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {participant.Id} already exists"
                });
            }

            _participants[participant.Id] = participant;

            _logger.LogInformation("Participant {Name} added with ID {Id}", participant.Name, participant.Id);
            PublishChange(GeoDataChangeType.Added, participant.Id);

            return ValueTask.FromResult(new RepositoryResult
            {
                Success = true,
                RecordId = participant.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant {Name}", participant?.Name);

            return ValueTask.FromResult(new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default)
    {
        try
        {
            var participants = _participants.Values.ToList();
            _logger.LogInformation("Retrieved {Count} participants", participants.Count);

            return ValueTask.FromResult<IEnumerable<Participant>>(participants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all participants");
            return ValueTask.FromResult(Enumerable.Empty<Participant>());
        }
    }

    public ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            _participants.TryGetValue(id, out var participant);
            if (participant != null)
            {
                _logger.LogInformation("Found participant {Name} with ID {Id}", participant.Name, id);
            }
            else
            {
                _logger.LogWarning("Participant with ID {Id} not found", id);
            }

            return ValueTask.FromResult<Participant?>(participant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant by ID {Id}", id);
            return ValueTask.FromResult<Participant?>(null);
        }
    }

    public ValueTask<RepositoryResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        try
        {
            if (participant == null)
            {
                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = "Participant is null"
                });
            }

            if (!_participants.ContainsKey(participant.Id))
            {
                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {participant.Id} not found"
                });
            }

            _participants[participant.Id] = participant;

            _logger.LogInformation("Participant {Name} updated with ID {Id}", participant.Name, participant.Id);
            PublishChange(GeoDataChangeType.Updated, participant.Id);

            return ValueTask.FromResult(new RepositoryResult
            {
                Success = true,
                RecordId = participant.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participant {Name}", participant?.Name);

            return ValueTask.FromResult(new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public ValueTask<RepositoryResult> DeleteParticipantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (_participants.TryRemove(id, out var removedParticipant))
            {
                _logger.LogInformation("Participant {Name} deleted with ID {Id}", removedParticipant.Name, id);
                PublishChange(GeoDataChangeType.Removed, id);

                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = true,
                    RecordId = id
                });
            }
            else
            {
                return ValueTask.FromResult(new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {id} not found"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting participant with ID {Id}", id);

            return ValueTask.FromResult(new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    public ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // In-Memory репозиторий всегда доступен
        return ValueTask.FromResult(true);
    }

    /// <summary>
    /// Получает статистику репозитория (для отладки)
    /// </summary>
    public int GetParticipantCount() => _participants.Count;

    /// <summary>
    /// Очищает все данные (для тестирования)
    /// </summary>
    public void ClearAll()
    {
        _participants.Clear();
        PublishChange(GeoDataChangeType.Cleared, null);
    }

    private void PublishChange(GeoDataChangeType changeType, Guid? participantId)
    {
        _changeNotifier?.Publish(GeoDataChangeNotification.ForParticipantRepository(changeType, participantId));
    }
}
