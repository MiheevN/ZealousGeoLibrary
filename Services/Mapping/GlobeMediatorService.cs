using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping;

/// <summary>
/// Посредник для работы с 3D глобусом
/// Обеспечивает безопасную интеграцию между Blazor и JavaScript
/// </summary>
public class GlobeMediatorService : IGlobeMediator
{
    private readonly IThreeJsGlobeService _globeService;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<GlobeMediatorService> _logger;

    public GlobeMediatorService(
        IThreeJsGlobeService globeService,
        IJSRuntime jsRuntime,
        ILogger<GlobeMediatorService> logger)
    {
        _globeService = globeService;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options)
    {
        try
        {
            _logger.LogDebug("Initializing globe in container {ContainerId}", containerId);

            var result = await _globeService.InitializeGlobeAsync(containerId, options);

            if (result.Success)
            {
                _logger.LogDebug("Globe initialized successfully");
            }
            else
            {
                _logger.LogWarning("Globe initialization failed: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing globe");
            return new GlobeInitializationResult
            {
                Success = false,
                Error = GlobeInitializationError.Unknown,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> AddParticipantAsync(string containerId, Participant participant)
    {
        try
        {
            // Проверяем доступность конкретного 3D глобуса
            var isGlobeAvailable = await _globeService.IsGlobeAvailableAsync(containerId);

            GlobeOperationResult result;
            if (isGlobeAvailable)
            {
                // 3D глобус доступен - добавляем через него
                result = await _globeService.AddParticipantAsync(containerId, participant);
                if (result.Success)
                {
                    _logger.LogDebug("Участник {Name} успешно добавлен в глобус {ContainerId}", participant.Name, containerId);
                }
                else
                {
                    _logger.LogWarning("Не удалось добавить участника {Name} в глобус {ContainerId}: {Error}", participant.Name, containerId, result.ErrorMessage);
                }
            }
            else
            {
                // 3D глобус недоступен - участник будет храниться только в репозитории
                _logger.LogDebug("Глобус {ContainerId} недоступен, участник {Name} сохранен только в репозитории", containerId, participant.Name);
                result = new GlobeOperationResult
                {
                    Success = true,
                    ProcessedCount = 1
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка в GlobeMediator.AddParticipantAsync для контейнера {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> AddPointsAsync(string containerId, IEnumerable<GeoPoint> points, IReadOnlyDictionary<string, string>? categoryColors = null)
    {
        try
        {
            var pointList = points.ToList();
            _logger.LogDebug("Adding {Count} points to globe {ContainerId}", pointList.Count, containerId);

            if (!await _globeService.IsGlobeAvailableAsync(containerId))
            {
                // Как и для участников: глобуса ещё нет, данные уже лежат в своём хранилище.
                _logger.LogDebug("3D globe {ContainerId} not available, points are kept in their storage only", containerId);
                return new GlobeOperationResult { Success = true, ProcessedCount = pointList.Count };
            }

            var result = await _globeService.AddPointsAsync(containerId, pointList, categoryColors);
            if (!result.Success)
            {
                _logger.LogWarning("Failed to add points to 3D globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding points to globe {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Participant> participants)
    {
        try
        {
            var participantList = participants.ToList();
            _logger.LogDebug("Adding {Count} participants to globe {ContainerId}", participantList.Count, containerId);

            // Проверяем доступность конкретного 3D глобуса
            var isGlobeAvailable = await _globeService.IsGlobeAvailableAsync(containerId);

            GlobeOperationResult result;
            if (isGlobeAvailable)
            {
                // 3D глобус доступен - добавляем через него
                result = await _globeService.AddParticipantsAsync(containerId, participantList);
                if (result.Success)
                {
                    _logger.LogDebug("Participants added to 3D globe {ContainerId} successfully", containerId);
                }
                else
                {
                    _logger.LogWarning("Failed to add participants to 3D globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
                }
            }
            else
            {
                // 3D глобус недоступен - участники будут храниться только в репозитории
                _logger.LogDebug("3D globe {ContainerId} not available, participants stored in repository only", containerId);

                // Участники уже должны быть в репозитории, просто возвращаем успех
                // (предполагаем, что вызывающий код уже сохранил их в репозиторий)
                result = new GlobeOperationResult
                {
                    Success = true,
                    ProcessedCount = participantList.Count
                };
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participants to globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId)
    {
        try
        {
            _logger.LogDebug("Removing participant {Id} from globe {ContainerId}", participantId, containerId);

            var result = await _globeService.RemoveParticipantAsync(containerId, participantId);

            if (result.Success)
            {
                _logger.LogDebug("Participant {Id} removed from globe {ContainerId} successfully", participantId, containerId);
            }
            else
            {
                _logger.LogWarning("Failed to remove participant {Id} from globe {ContainerId}: {Error}", participantId, containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {Id} from globe {ContainerId}", participantId, containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0)
    {
        try
        {
            _logger.LogDebug("Centering globe {ContainerId} on {Lat}, {Lng} with zoom {Zoom}", containerId, latitude, longitude, zoom);

            var result = await _globeService.CenterOnAsync(containerId, latitude, longitude, zoom);

            if (result.Success)
            {
                _logger.LogDebug("Globe {ContainerId} centered successfully", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to center globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error centering globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod)
    {
        try
        {
            _logger.LogDebug("Setting level of detail to {Lod} for globe {ContainerId}", lod, containerId);

            var result = await _globeService.SetLevelOfDetailAsync(containerId, lod);

            if (result.Success)
            {
                _logger.LogDebug("Level of detail set successfully for globe {ContainerId}", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to set level of detail for globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting level of detail for globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5)
    {
        try
        {
            _logger.LogDebug("Setting auto rotation to {Enabled} with speed {Speed} for globe {ContainerId}", enabled, speed, containerId);

            var result = await _globeService.SetAutoRotationAsync(containerId, enabled, speed);

            if (result.Success)
            {
                _logger.LogDebug("Auto rotation set successfully for globe {ContainerId}", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to set auto rotation for globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting auto rotation for globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeState> GetStateAsync(string containerId)
    {
        try
        {
            var state = await _globeService.GetStateAsync(containerId);
            _logger.LogDebug("Retrieved globe {ContainerId} state: {Participants} participants, {Countries} countries",
                containerId, state.ParticipantCount, state.CountryCount);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting globe {ContainerId} state", containerId);
            return new GlobeState(); // Return default state
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> ClearAsync(string containerId)
    {
        try
        {
            _logger.LogDebug("Clearing globe {ContainerId}", containerId);

            var result = await _globeService.ClearAsync(containerId);

            if (result.Success)
            {
                _logger.LogDebug("Globe {ContainerId} cleared successfully", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to clear globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> DisposeAsync(string containerId)
    {
        try
        {
            _logger.LogDebug("Disposing globe {ContainerId}", containerId);

            var result = await _globeService.DisposeAsync(containerId);

            if (result.Success)
            {
                _logger.LogDebug("Globe {ContainerId} disposed successfully", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to dispose globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing globe {ContainerId}", containerId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await _globeService.IsAvailableAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking globe availability");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsGlobeAvailableAsync(string containerId)
    {
        try
        {
            return await _globeService.IsGlobeAvailableAsync(containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking globe {ContainerId} availability", containerId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> SetReadyCallbackAsync(string containerId, Func<GlobeState, Task> callback)
    {
        try
        {
            _logger.LogDebug("Setting ready callback for globe {ContainerId}", containerId);
            var result = await _globeService.SetReadyCallbackAsync(containerId, callback);

            if (result.Success)
            {
                _logger.LogDebug("Ready callback set successfully for globe {ContainerId}", containerId);
            }
            else
            {
                _logger.LogWarning("Failed to set ready callback for globe {ContainerId}: {Error}", containerId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ready callback for globe {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}