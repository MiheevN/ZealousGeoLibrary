using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Сервис для управления PWA функциональностью
/// </summary>
/// <remarks>
/// Все методы обращаются к JS-модулю <c>js/pwa.js</c>, поэтому вызывать их можно только
/// из интерактивного рендера (<c>OnAfterRenderAsync</c> и позже), но не при пререндеринге.
/// </remarks>
public class PwaService : IPwaService, IAsyncDisposable
{
    private const string ModulePath = "/_content/ZealousMindedPeopleGeo/js/pwa.js";
    private const string ServiceWorkerPath = "/_content/ZealousMindedPeopleGeo/sw.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PwaService> _logger;
    private Task<IJSObjectReference>? _moduleTask;
    private bool _isInitialized = false;

    public PwaService(IJSRuntime jsRuntime, ILogger<PwaService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    // Один импорт на все вызовы; неудачный импорт повторяется при следующем обращении.
    private Task<IJSObjectReference> GetModuleAsync()
    {
        if (_moduleTask == null || _moduleTask.IsFaulted || _moduleTask.IsCanceled)
        {
            _moduleTask = _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        }

        return _moduleTask;
    }

    /// <summary>
    /// Инициализировать PWA функциональность
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("registerServiceWorker", ServiceWorkerPath);

            _isInitialized = true;
            _logger.LogInformation("PWA service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize PWA service");
            throw;
        }
    }

    /// <summary>
    /// Проверить, поддерживается ли PWA в текущем браузере
    /// </summary>
    public async Task<bool> IsPwaSupportedAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("isPwaSupported");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PWA support");
            return false;
        }
    }

    /// <summary>
    /// Проверить, запущено ли приложение в режиме PWA
    /// </summary>
    public async Task<bool> IsRunningAsPwaAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("isRunningAsPwa");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PWA mode");
            return false;
        }
    }

    /// <summary>
    /// Получить информацию об установке PWA
    /// </summary>
    public async Task<PwaInstallInfo> GetInstallInfoAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<PwaInstallInfo>("getInstallInfo");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PWA install info");
            return new PwaInstallInfo
            {
                CanInstall = false,
                IsInstalled = false,
                IsSupported = false
            };
        }
    }

    /// <summary>
    /// Показать промпт установки PWA
    /// </summary>
    public async Task<bool> ShowInstallPromptAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("showInstallPrompt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing PWA install prompt");
            return false;
        }
    }

    /// <summary>
    /// Очистить кэш сервис-воркера
    /// </summary>
    public async Task ClearCacheAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("clearCache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing PWA cache");
        }
    }

    /// <summary>
    /// Обновить сервис-воркер
    /// </summary>
    public async Task UpdateServiceWorkerAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            await module.InvokeVoidAsync("updateServiceWorker");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service worker");
        }
    }

    /// <summary>
    /// Получить статистику кэша
    /// </summary>
    public async Task<PwaCacheInfo> GetCacheInfoAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<PwaCacheInfo?>("getCacheInfo") ?? new PwaCacheInfo();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache info");
            return new PwaCacheInfo();
        }
    }

    /// <summary>
    /// Отправить уведомление пользователю
    /// </summary>
    public async Task<bool> SendNotificationAsync(string title, string body, NotificationOptions? options = null)
    {
        try
        {
            var notificationOptions = options ?? new NotificationOptions();

            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("sendNotification", title, new
            {
                body,
                icon = notificationOptions.Icon ?? "/_content/ZealousMindedPeopleGeo/icons/icon-192x192.png",
                badge = notificationOptions.Badge ?? "/_content/ZealousMindedPeopleGeo/icons/badge-72x72.png",
                tag = notificationOptions.Tag ?? "general",
                requireInteraction = notificationOptions.RequireInteraction,
                silent = notificationOptions.Silent
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PWA notification");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is { IsCompletedSuccessfully: true })
        {
            try
            {
                await _moduleTask.Result.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Цепь уже закрыта, браузер освободил модуль сам.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing PWA helper");
            }
        }

        _moduleTask = null;
    }
}

/// <summary>
/// Интерфейс для сервиса PWA
/// </summary>
public interface IPwaService
{
    Task InitializeAsync();
    Task<bool> IsPwaSupportedAsync();
    Task<bool> IsRunningAsPwaAsync();
    Task<PwaInstallInfo> GetInstallInfoAsync();
    Task<bool> ShowInstallPromptAsync();
    Task ClearCacheAsync();
    Task UpdateServiceWorkerAsync();
    Task<PwaCacheInfo> GetCacheInfoAsync();
    Task<bool> SendNotificationAsync(string title, string body, NotificationOptions? options = null);
}

/// <summary>
/// Информация об установке PWA
/// </summary>
public class PwaInstallInfo
{
    public bool CanInstall { get; set; }
    public bool IsInstalled { get; set; }
    public bool IsSupported { get; set; }
}

/// <summary>
/// Информация о кэше PWA
/// </summary>
public class PwaCacheInfo
{
    public long TotalSize { get; set; }
    public int CacheCount { get; set; }
    public List<CacheDetails> Caches { get; set; } = new();
}

/// <summary>
/// Детали кэша
/// </summary>
public class CacheDetails
{
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Опции уведомления
/// </summary>
public class NotificationOptions
{
    public string? Icon { get; set; }
    public string? Badge { get; set; }
    public string? Tag { get; set; }
    public bool RequireInteraction { get; set; } = false;
    public bool Silent { get; set; } = false;
}