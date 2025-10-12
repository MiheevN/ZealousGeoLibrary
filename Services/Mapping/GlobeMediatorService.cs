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
            _logger.LogInformation("Initializing globe in container {ContainerId}", containerId);

            var result = await _globeService.InitializeGlobeAsync(containerId, options);

            if (result.Success)
            {
                _logger.LogInformation("Globe initialized successfully");
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
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> AddParticipantAsync(Participant participant)
    {
        try
        {
            _logger.LogInformation("Adding participant {Name} to globe", participant.Name);

            // Проверяем доступность 3D глобуса
            var isGlobeAvailable = await _globeService.IsAvailableAsync();

            GlobeOperationResult result;
            if (isGlobeAvailable)
            {
                // 3D глобус доступен - добавляем через него
                result = await _globeService.AddParticipantsAsync("default", new[] { participant });
                if (result.Success)
                {
                    _logger.LogInformation("Participant {Name} added to 3D globe successfully", participant.Name);
                }
                else
                {
                    _logger.LogWarning("Failed to add participant {Name} to 3D globe: {Error}", participant.Name, result.ErrorMessage);
                }
            }
            else
            {
                // 3D глобус недоступен - участник будет храниться только в репозитории
                _logger.LogInformation("3D globe not available, participant {Name} stored in repository only", participant.Name);
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
            _logger.LogError(ex, "Error adding participant {Name}", participant.Name);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> AddParticipantsAsync(IEnumerable<Participant> participants)
    {
        try
        {
            var participantList = participants.ToList();
            _logger.LogInformation("Adding {Count} participants to globe", participantList.Count);

            // Проверяем доступность 3D глобуса
            var isGlobeAvailable = await _globeService.IsAvailableAsync();

            GlobeOperationResult result;
            if (isGlobeAvailable)
            {
                // 3D глобус доступен - добавляем через него
                result = await _globeService.AddParticipantsAsync("default", participantList);
                if (result.Success)
                {
                    _logger.LogInformation("Participants added to 3D globe successfully");
                }
                else
                {
                    _logger.LogWarning("Failed to add participants to 3D globe: {Error}", result.ErrorMessage);
                }
            }
            else
            {
                // 3D глобус недоступен - участники будут храниться только в репозитории
                _logger.LogInformation("3D globe not available, participants stored in repository only");

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
            _logger.LogError(ex, "Error adding participants");
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> RemoveParticipantAsync(Guid participantId)
    {
        try
        {
            _logger.LogInformation("Removing participant {Id} from globe", participantId);

            var result = await _globeService.RemoveParticipantAsync("default", participantId);

            if (result.Success)
            {
                _logger.LogInformation("Participant {Id} removed successfully", participantId);
            }
            else
            {
                _logger.LogWarning("Failed to remove participant {Id}: {Error}", participantId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {Id}", participantId);
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> CenterOnAsync(double latitude, double longitude, double zoom = 2.0)
    {
        try
        {
            _logger.LogInformation("Centering globe on {Lat}, {Lng} with zoom {Zoom}", latitude, longitude, zoom);

            var result = await _globeService.CenterOnAsync("default", latitude, longitude, zoom);

            if (result.Success)
            {
                _logger.LogInformation("Globe centered successfully");
            }
            else
            {
                _logger.LogWarning("Failed to center globe: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error centering globe");
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> SetLevelOfDetailAsync(int lod)
    {
        try
        {
            _logger.LogInformation("Setting level of detail to {Lod}", lod);

            var result = await _globeService.SetLevelOfDetailAsync("default", lod);

            if (result.Success)
            {
                _logger.LogInformation("Level of detail set successfully");
            }
            else
            {
                _logger.LogWarning("Failed to set level of detail: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting level of detail");
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> SetAutoRotationAsync(bool enabled, double speed = 0.5)
    {
        try
        {
            _logger.LogInformation("Setting auto rotation to {Enabled} with speed {Speed}", enabled, speed);

            var result = await _globeService.SetAutoRotationAsync("default", enabled, speed);

            if (result.Success)
            {
                _logger.LogInformation("Auto rotation set successfully");
            }
            else
            {
                _logger.LogWarning("Failed to set auto rotation: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting auto rotation");
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeState> GetStateAsync()
    {
        try
        {
            var state = await _globeService.GetStateAsync("default");
            _logger.LogDebug("Retrieved globe state: {Participants} participants, {Countries} countries",
                state.ParticipantCount, state.CountryCount);
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting globe state");
            return new GlobeState(); // Return default state
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> ClearAsync()
    {
        try
        {
            _logger.LogInformation("Clearing globe");

            var result = await _globeService.ClearAsync("default");

            if (result.Success)
            {
                _logger.LogInformation("Globe cleared successfully");
            }
            else
            {
                _logger.LogWarning("Failed to clear globe: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing globe");
            return new GlobeOperationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<GlobeOperationResult> DisposeAsync()
    {
        try
        {
            _logger.LogInformation("Disposing globe");

            var result = await _globeService.DisposeAsync("default");

            if (result.Success)
            {
                _logger.LogInformation("Globe disposed successfully");
            }
            else
            {
                _logger.LogWarning("Failed to dispose globe: {Error}", result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing globe");
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
}