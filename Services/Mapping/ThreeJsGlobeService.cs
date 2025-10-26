using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ZealousMindedPeopleGeo.Services.Mapping;

public class ThreeJsGlobeService : IThreeJsGlobeService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ThreeJsGlobeService> _logger;
    private IJSObjectReference? _module;
    private readonly Dictionary<string, (Func<GlobeState, Task> callback, DotNetObjectReference<CallbackWrapper> reference)> _callbacks = new();

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
            _logger.LogInformation("Loading globe module for container: {ContainerId}", containerId);

            // Инициализируем модуль если еще не сделали
            if (_module == null)
            {
                _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", 
                    "/_content/ZealousMindedPeopleGeo/js/community-globe.js");
                _logger.LogInformation("Globe module loaded successfully");
            }

            // Создаем глобус
            var success = await _module.InvokeAsync<bool>("createGlobe", containerId, options);
            if (!success)
            {
                return new GlobeInitializationResult { Success = false, ErrorMessage = "Failed to create globe instance" };
            }

            // Устанавливаем callback если он был сохранен
            if (_callbacks.TryGetValue(containerId, out var callbackData))
            {
                await _module.InvokeVoidAsync("setGlobeReadyCallbackDirect", containerId, callbackData.reference);
                _logger.LogInformation("Callback установлен для {ContainerId}", containerId);
            }

            // Модуль уже сохранен в _module

            _logger.LogInformation("3D globe initialized for container: {ContainerId}", containerId);

            // Получаем версию Three.js
            var version = "unknown";
            try
            {
                version = await _module.InvokeAsync<string>("getThreeJsVersion");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not get Three.js version: {Message}", ex.Message);
            }
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

    public async ValueTask<GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        if (participants == null)
            return new GlobeOperationResult { Success = false, ErrorMessage = "Participants cannot be null" };

        try
        {
            if (_module == null)
                return new GlobeOperationResult { Success = false, ErrorMessage = "Globe module not initialized" };

            var participantsArray = participants.Select(p => new
            {
                id = p.Id.ToString(),
                p.Name,
                p.Latitude,
                p.Longitude,
                location = $"{p.Name} ({p.Latitude:F4}, {p.Longitude:F4})"
            }).ToArray();

            var success = await _module.InvokeAsync<bool>("addParticipants", containerId, (object)participantsArray);
            
            if (success)
            {
                _logger.LogInformation("✅ Добавлено {Count} участников в глобус {ContainerId}", participantsArray.Length, containerId);
                return new GlobeOperationResult { Success = true, ProcessedCount = participantsArray.Length };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Ошибка при добавлении участников в контейнер {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> UpdateParticipantPositionAsync(string containerId, Guid participantId, double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var success = await _module.InvokeAsync<bool>("updateParticipantPosition", containerId, participantId.ToString(), latitude, longitude);
                return new GlobeOperationResult { Success = success, ProcessedCount = success ? 1 : 0 };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position for participant {ParticipantId}", participantId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var result = await _module.InvokeAsync<bool>("removeParticipant", containerId, participantId.ToString());
                return result
                    ? new GlobeOperationResult { Success = true, ProcessedCount = 1 }
                    : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to remove participant" };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing participant {ParticipantId}", participantId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var success = await _module.InvokeAsync<bool>("centerOn", containerId, latitude, longitude, zoom);
                return new GlobeOperationResult { Success = success };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error centering globe on coordinates {Latitude}, {Longitude}", latitude, longitude);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var success = await _module.InvokeAsync<bool>("setLevelOfDetail", containerId, lod);
                return new GlobeOperationResult { Success = success };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting level of detail {Lod}", lod);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var success = await _module.InvokeAsync<bool>("setAutoRotation", containerId, enabled, speed);
                return new GlobeOperationResult { Success = success };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting auto-rotation {Enabled}", enabled);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> LoadCountriesDataAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var result = await _module.InvokeAsync<bool>("loadCountriesData", containerId);
                return result
                    ? new GlobeOperationResult { Success = true }
                    : new GlobeOperationResult { Success = false, ErrorMessage = "Failed to load countries data" };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading countries data");
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeOperationResult> ClearAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                var success = await _module.InvokeAsync<bool>("clear", containerId);
                return new GlobeOperationResult { Success = success };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing globe");
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async ValueTask<GlobeState> GetStateAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модуль напрямую
            if (_module != null)
            {
                try
                {
                    var state = await _module.InvokeAsync<GlobeState>("getState", containerId);
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
                    _logger.LogWarning("Could not get globe state: {Message}", ex.Message);
                    return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };
                }
            }

            return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting globe state");
            return new GlobeState { IsInitialized = false, ParticipantCount = 0, CountryCount = 0, Camera = new CameraState() };
        }
    }

    public async ValueTask<GlobeOperationResult> DisposeAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("🚮 DisposeAsync вызван для контейнера: {ContainerId}", containerId);
            _logger.LogInformation("🚮 Проверка доступности модуля: {_module != null}", _module != null);

            // Используем модуль напрямую с правильным containerId
            if (_module != null)
            {
                _logger.LogInformation("🚮 Вызов JavaScript dispose для контейнера: {ContainerId}", containerId);
                var success = await _module.InvokeAsync<bool>("dispose", containerId);
                _logger.LogInformation("🚮 JavaScript dispose вернул: {Success} для контейнера: {ContainerId}", success, containerId);
                return new GlobeOperationResult { Success = success };
            }

            _logger.LogWarning("🚮 Модуль недоступен для контейнера: {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = "Globe instance not found" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🚮 Критическая ошибка при dispose контейнера {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
            _module = null;
        }
    }

    public async ValueTask<GlobeOperationResult> SetReadyCallbackAsync(string containerId, Func<GlobeState, Task> callback, CancellationToken ct = default)
    {
        try
        {
            var wrapper = new CallbackWrapper(callback);
            var reference = DotNetObjectReference.Create(wrapper);
            _callbacks[containerId] = (callback, reference);
            _logger.LogInformation("Callback сохранен для {ContainerId}", containerId);
            return new GlobeOperationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ready callback for globe {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public class CallbackWrapper
    {
        private readonly Func<GlobeState, Task> _callback;
        public CallbackWrapper(Func<GlobeState, Task> callback) => _callback = callback;

        [JSInvokable]
        public async Task Invoke(GlobeState state)
        {
            Console.WriteLine($"📞 CallbackWrapper.Invoke вызван");
            await _callback(state);
        }
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        return await IsGlobeAvailableAsync(null, ct);
    }

    public async ValueTask<bool> IsGlobeAvailableAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Сначала проверяем базовую доступность модуля
            if (_module == null)
            {
                _logger.LogDebug("Globe module not initialized");
                return false;
            }

            // Проверяем базовую поддержку WebGL
            bool isWebGLSupported = true;
            try
            {
                var webGLCheck = await _jsRuntime.InvokeAsync<bool>("eval", @"
                    (function() {
                        try {
                            var canvas = document.createElement('canvas');
                            return !!(window.WebGLRenderingContext &&
                                     (canvas.getContext('webgl') || canvas.getContext('experimental-webgl')));
                        } catch (e) {
                            return false;
                        }
                    })()
                ");
                isWebGLSupported = webGLCheck;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cannot check WebGL support: {Message}", ex.Message);
                isWebGLSupported = true; // Предполагаем поддержку
            }

            if (!isWebGLSupported)
            {
                _logger.LogWarning("WebGL is not supported");
                return false;
            }

            // Если containerId не указан, проверяем только модуль
            if (string.IsNullOrEmpty(containerId))
            {
                return true;
            }

            // Проверяем состояние конкретного глобуса
            try
            {
                var state = await GetStateAsync(containerId, ct);
                var isInitialized = state?.IsInitialized ?? false;

                _logger.LogDebug("Globe {ContainerId} initialized: {IsInitialized}", containerId, isInitialized);
                return isInitialized;
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cannot get state for globe {ContainerId}: {Message}", containerId, ex.Message);
                // Если не можем получить состояние, предполагаем что глобус не готов
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking globe availability");
            return false;
        }
    }

}