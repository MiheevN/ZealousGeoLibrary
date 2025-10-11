using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping;

public class ThreeJsGlobeService : IThreeJsGlobeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ThreeJsGlobeService> _logger;
    private IJSObjectReference? _globeModule;

    public ThreeJsGlobeService(IJSRuntime jsRuntime, ILogger<ThreeJsGlobeService> logger)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(containerId))
            return new GlobeInitializationResult { Success = false, ErrorMessage = "ContainerId cannot be null or empty" };

        if (options == null)
            return new GlobeInitializationResult { Success = false, ErrorMessage = "Options cannot be null" };

        try
        {
            _logger.LogInformation("Attempting to import community-globe.js");
            _globeModule = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "/_content/ZealousMindedPeopleGeo/js/community-globe.js");
            _logger.LogInformation("Module imported successfully");

            _logger.LogInformation("Initializing scripts...");
            await _globeModule.InvokeVoidAsync("initializeScripts");
            _logger.LogInformation("Scripts initialized");

            _logger.LogInformation("Creating globe for container: {ContainerId}", containerId);
            await _globeModule.InvokeVoidAsync("createGlobe", containerId, options);

            _logger.LogInformation("3D globe initialized for container: {ContainerId}", containerId);

            var version = await _globeModule.InvokeAsync<string>("getThreeJsVersion");
            return new GlobeInitializationResult
            {
                Success = true,
                GlobeId = containerId,
                ThreeJsVersion = version
            };
        }
        catch (JSException ex)
        {
            _logger.LogError(ex, "JavaScript error initializing 3D globe for container: {ContainerId}", containerId);
            return new GlobeInitializationResult { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing 3D globe for container: {ContainerId}", containerId);
            return new GlobeInitializationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    // Остальные методы остаются без изменений, так как они корректны
    public async ValueTask<GlobeOperationResult> AddParticipantsAsync(IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        if (participants == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Participants cannot be null" };

        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var participantsArray = participants.Select(p => new
            {
                id = p.Id.ToString(), // Добавляем id для update/remove
                p.Name,
                p.Latitude,
                p.Longitude
            }).ToArray();

            var result = await _globeModule.InvokeAsync<bool>("addParticipants", participantsArray);

            if (result)
            {
                _logger.LogInformation("Added {Count} participants to the globe", participantsArray.Length);
                return new GlobeOperationResult { Success = true, ProcessedCount = participantsArray.Length };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Failed to add participants" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding {Count} participants to the globe", participants.Count());
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> UpdateParticipantPositionAsync(Guid participantId, double latitude, double longitude, CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("updateParticipantPosition", participantId.ToString(), latitude, longitude);
            return result
                ? new GlobeOperationResult { Success = true, ProcessedCount = 1 }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to update participant position" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position for participant {ParticipantId}", participantId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> RemoveParticipantAsync(Guid participantId, CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("removeParticipant", participantId.ToString());
            return result
                ? new GlobeOperationResult { Success = true, ProcessedCount = 1 }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to remove participant" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {ParticipantId}", participantId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> CenterOnAsync(double latitude, double longitude, double zoom = 2.0, CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("centerOn", latitude, longitude, zoom);
            return result
                ? new GlobeOperationResult { Success = true }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to center globe" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error centering globe on coordinates {Latitude}, {Longitude}", latitude, longitude);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> SetLevelOfDetailAsync(int lod, CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("setLevelOfDetail", lod);
            return result
                ? new GlobeOperationResult { Success = true }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to set level of detail" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting level of detail {Lod}", lod);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> SetAutoRotationAsync(bool enabled, double speed = 0.5, CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("setAutoRotation", enabled, speed);
            return result
                ? new GlobeOperationResult { Success = true }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to set auto-rotation" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting auto-rotation {Enabled}", enabled);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> LoadCountriesDataAsync(CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("loadCountriesData");
            return result
                ? new GlobeOperationResult { Success = true }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to load countries data" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries data");
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> ClearAsync(CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

        try
        {
            var result = await _globeModule.InvokeAsync<bool>("clear");
            return result
                ? new GlobeOperationResult { Success = true }
                : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to clear globe" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing globe");
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeState> GetStateAsync(CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };

        try
        {
            var state = await _globeModule.InvokeAsync<GlobeState>("getState");
            if (state == null)
            {
                _logger.LogWarning("Globe state returned null");
                return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };
            }

            state.IsInitialized = true;
            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting globe state");
            return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };
        }
    }

    public async ValueTask<GlobeOperationResult> DisposeAsync(CancellationToken ct = default)
    {
        if (_globeModule == null)
            return new GlobeOperationResult { Success = true };

        try
        {
            await _globeModule.InvokeVoidAsync("dispose");
            await _globeModule.DisposeAsync();
            _globeModule = null;
            _logger.LogInformation("Globe module disposed");
            return new GlobeOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing globe module");
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var isWebGLSupported = await _jsRuntime.InvokeAsync<bool>(
                "eval",
                "(function() { try { var canvas = document.createElement('canvas'); return !!(window.WebGLRenderingContext && (canvas.getContext('webgl') || canvas.getContext('experimental-webgl'))); } catch (e) { return false; } })()");
            if (!isWebGLSupported)
            {
                _logger.LogWarning("WebGL is not supported");
                return false;
            }

            try
            {
                await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "/_content/ZealousMindedPeopleGeo/js/community-globe.js");
                _logger.LogInformation("Module community-globe.js is available");
                return true;
            }
            catch (JSException ex)
            {
                _logger.LogError(ex, "Error loading community-globe.js");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking globe service availability");
            return false;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await DisposeAsync();
    }
}