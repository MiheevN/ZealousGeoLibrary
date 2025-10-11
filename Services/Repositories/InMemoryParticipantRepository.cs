using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Repositories;

/// <summary>
/// Реализация репозитория участников в памяти (In-Memory)
/// Thread-safe реализация с использованием ConcurrentDictionary
/// </summary>
public class InMemoryParticipantRepository : IParticipantRepository
{
    private readonly ConcurrentDictionary<Guid, Participant> _participants = new();
    private readonly ILogger<InMemoryParticipantRepository> _logger;

    public InMemoryParticipantRepository(ILogger<InMemoryParticipantRepository> logger)
    {
        _logger = logger;
    }

    public async ValueTask<RepositoryResult> AddParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        try
        {
            if (participant == null)
            {
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = "Participant is null"
                };
            }

            // Проверяем, существует ли уже участник с таким ID
            if (_participants.ContainsKey(participant.Id))
            {
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {participant.Id} already exists"
                };
            }

            _participants[participant.Id] = participant;

            _logger.LogInformation("Participant {Name} added with ID {Id}", participant.Name, participant.Id);

            return new RepositoryResult
            {
                Success = true,
                RecordId = participant.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant {Name}", participant?.Name);

            return new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default)
    {
        try
        {
            var participants = _participants.Values.ToList();
            _logger.LogInformation("Retrieved {Count} participants", participants.Count);

            return participants;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all participants");
            return Enumerable.Empty<Participant>();
        }
    }

    public async ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default)
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

            return participant;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting participant by ID {Id}", id);
            return null;
        }
    }

    public async ValueTask<RepositoryResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default)
    {
        try
        {
            if (participant == null)
            {
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = "Participant is null"
                };
            }

            if (!_participants.ContainsKey(participant.Id))
            {
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {participant.Id} not found"
                };
            }

            _participants[participant.Id] = participant;

            _logger.LogInformation("Participant {Name} updated with ID {Id}", participant.Name, participant.Id);

            return new RepositoryResult
            {
                Success = true,
                RecordId = participant.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating participant {Name}", participant?.Name);

            return new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async ValueTask<RepositoryResult> DeleteParticipantAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            if (_participants.TryRemove(id, out var removedParticipant))
            {
                _logger.LogInformation("Participant {Name} deleted with ID {Id}", removedParticipant.Name, id);

                return new RepositoryResult
                {
                    Success = true,
                    RecordId = id
                };
            }
            else
            {
                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = $"Participant with ID {id} not found"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting participant with ID {Id}", id);

            return new RepositoryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // In-Memory репозиторий всегда доступен
        return true;
    }

    /// <summary>
    /// Получает статистику репозитория (для отладки)
    /// </summary>
    public int GetParticipantCount() => _participants.Count;

    /// <summary>
    /// Очищает все данные (для тестирования)
    /// </summary>
    public void ClearAll() => _participants.Clear();
}