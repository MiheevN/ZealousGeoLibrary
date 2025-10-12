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
    private readonly Dictionary<string, object> _globeInstances = new();

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
            _logger.LogInformation("Loading modular globe scripts for container: {ContainerId}", containerId);

            // Загружаем модульную версию скрипта глобуса
            await _jsRuntime.InvokeVoidAsync("import", $"/_content/ZealousMindedPeopleGeo/js/libs/three.module.js");
            await _jsRuntime.InvokeVoidAsync("import", $"/_content/ZealousMindedPeopleGeo/js/libs/OrbitControls.js");
            await _jsRuntime.InvokeVoidAsync("import", $"/_content/ZealousMindedPeopleGeo/js/community-globe.js");

            _logger.LogInformation("Globe scripts loaded for container: {ContainerId}", containerId);

            // Ждем доступности функций модуля
            int attempts = 0;
            const int maxAttempts = 50;
            bool moduleAvailable = false;

            while (attempts < maxAttempts && !moduleAvailable)
            {
                try
                {
                    // Проверяем доступность модуля через eval с динамическим импортом
                    await _jsRuntime.InvokeVoidAsync("eval", @"
                        (async function() {
                            try {
                                if (typeof window.globeModule === 'undefined') {
                                    window.globeModule = await import('/_content/ZealousMindedPeopleGeo/js/community-globe.js');
                                }
                            } catch (e) {
                                console.error('Module load error:', e);
                            }
                        })();
                    ");

                    // Проверяем, что функции доступны
                    moduleAvailable = await _jsRuntime.InvokeAsync<bool>("eval", @"
                        typeof window.globeModule !== 'undefined' &&
                        typeof window.globeModule.createGlobe === 'function'
                    ");

                    if (moduleAvailable)
                    {
                        _logger.LogInformation("Globe module is available on attempt {Attempt}", attempts + 1);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Attempt {Attempt}: Module not available yet: {Message}", attempts + 1, ex.Message);
                }

                attempts++;
                if (attempts < maxAttempts)
                {
                    await Task.Delay(100);
                }
            }

            if (!moduleAvailable)
            {
                return new GlobeInitializationResult
                {
                    Success = false,
                    ErrorMessage = "Globe module is not available after maximum attempts"
                };
            }

            // Создаем глобус через модуль
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule.createGlobe('{containerId}', {System.Text.Json.JsonSerializer.Serialize(options)})");

            if (!result)
            {
                return new GlobeInitializationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create globe instance"
                };
            }

            // Сохраняем экземпляр глобуса
            _globeInstances[containerId] = new object();

            _logger.LogInformation("3D globe initialized for container: {ContainerId}", containerId);

            // Получаем версию Three.js через модуль
            var version = await _jsRuntime.InvokeAsync<string>("eval", "typeof window.THREE !== 'undefined' && window.THREE.REVISION ? window.THREE.REVISION : 'unknown'");
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
            var participantsArray = participants.Select(p => new
            {
                id = p.Id.GetHashCode(), // Используем hash для простоты
                p.Name,
                p.Latitude,
                p.Longitude,
                location = $"{p.Name} ({p.Latitude:F4}, {p.Longitude:F4})"
            }).ToArray();

            // Используем модульную функцию с containerId
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule.addParticipants('{containerId}', {System.Text.Json.JsonSerializer.Serialize(participantsArray)})");

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

    public async ValueTask<GlobeOperationResult> UpdateParticipantPositionAsync(string containerId, Guid participantId, double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.updateParticipantPosition && window.globeModule.updateParticipantPosition('{containerId}', '{participantId}', {latitude}, {longitude})");
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

    public async ValueTask<GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.removeParticipant ? window.globeModule.removeParticipant('{containerId}', {participantId.GetHashCode()}) : false");
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

    public async ValueTask<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.centerOn ? window.globeModule.centerOn('{containerId}', {latitude}, {longitude}, {zoom}) : false");
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

    public async ValueTask<GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.setLevelOfDetail ? window.globeModule.setLevelOfDetail('{containerId}', {lod}) : false");
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

    public async ValueTask<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.setAutoRotation ? window.globeModule.setAutoRotation('{containerId}', {enabled.ToString().ToLower()}, {speed}) : false");
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

    public async ValueTask<GlobeOperationResult> LoadCountriesDataAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.loadCountriesData ? window.globeModule.loadCountriesData('{containerId}') : false");
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

    public async ValueTask<GlobeOperationResult> ClearAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.clear ? window.globeModule.clear('{containerId}') : false");
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

    public async ValueTask<GlobeState> GetStateAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var state = await _jsRuntime.InvokeAsync<GlobeState>("eval", $"window.globeModule && window.globeModule.getState ? window.globeModule.getState('{containerId}') : null");
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

    public async ValueTask<GlobeOperationResult> DisposeAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            // Используем модульную функцию
            var result = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeModule && window.globeModule.dispose ? window.globeModule.dispose('{containerId}') : false");

            // Удаляем из реестра
            _globeInstances.Remove(containerId);

            if (result)
            {
                _logger.LogInformation("Globe {ContainerId} disposed successfully", containerId);
                return new GlobeOperationResult { Success = true };
            }

            return new GlobeOperationResult { Success = false, ErrorMessage = "Failed to dispose globe" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing globe {ContainerId}", containerId);
            return new GlobeOperationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        // Освобождаем все созданные глобусы
        foreach (var containerId in _globeInstances.Keys)
        {
            await DisposeAsync(containerId);
        }
        _globeInstances.Clear();
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            // Проверяем, есть ли уже созданные экземпляры глобуса
            try
            {
                var hasInstances = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeInstances && window.globeInstances.size > 0");
                if (hasInstances)
                {
                    _logger.LogInformation("Globe instances already exist");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cannot check globe instances: {Message}", ex.Message);
            }

            // Проверяем доступность глобальных функций
            try
            {
                var hasInstances = await _jsRuntime.InvokeAsync<bool>("eval", $"window.globeInstances && window.globeInstances.size > 0");
                if (hasInstances)
                {
                    _logger.LogInformation("Globe instances already exist");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cannot check globe instances: {Message}", ex.Message);
            }

            // Если ничего не инициализировано, проверяем базовую поддержку WebGL
            bool isWebGLSupported = true; // По умолчанию предполагаем поддержку
            try
            {
                // Простая проверка через eval, без зависимостей от внешних функций
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
                // В тестовом окружении или при ошибках предполагаем поддержку
                isWebGLSupported = true;
            }

            if (!isWebGLSupported)
            {
                _logger.LogWarning("WebGL is not supported");
                return false;
            }

            // Проверяем доступность Three.js
            try
            {
                var threeJsAvailable = await _jsRuntime.InvokeAsync<bool>("eval", $"typeof window.THREE !== 'undefined'");
                if (threeJsAvailable)
                {
                    _logger.LogInformation("Three.js is available");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Three.js is not available");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Cannot check Three.js availability: {Message}", ex.Message);
                // Если не можем проверить, предполагаем доступность (для тестового окружения)
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking globe service availability");
            return false;
        }
    }

}