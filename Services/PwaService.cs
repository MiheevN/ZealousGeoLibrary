using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Сервис для управления PWA функциональностью
/// </summary>
public class PwaService : IPwaService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PwaService> _logger;
    private IJSObjectReference? _pwaHelper;
    private bool _isInitialized = false;

    public PwaService(IJSRuntime jsRuntime, ILogger<PwaService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Инициализировать PWA функциональность
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            await _jsRuntime.InvokeVoidAsync("eval",
                @"if ('serviceWorker' in navigator) {
                    window.addEventListener('load', async () => {
                        try {
                            const registration = await navigator.serviceWorker.register('/_content/ZealousMindedPeopleGeo/sw.js');
                            console.log('Service Worker registered successfully:', registration.scope);

                            // Проверяем обновления
                            registration.addEventListener('updatefound', () => {
                                const newWorker = registration.installing;
                                if (newWorker) {
                                    newWorker.addEventListener('statechange', () => {
                                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                                            // Новый контент доступен, предлагаем обновление
                                            if (confirm('Доступно обновление приложения. Обновить сейчас?')) {
                                                window.location.reload();
                                            }
                                        }
                                    });
                                }
                            });

                            // Слушаем сообщения от сервис-воркера
                            navigator.serviceWorker.addEventListener('message', (event) => {
                                if (event.data && event.data.type === 'SW_UPDATE_READY') {
                                    if (confirm('Доступно обновление приложения. Обновить сейчас?')) {
                                        window.location.reload();
                                    }
                                }
                            });

                        } catch (error) {
                            console.error('Service Worker registration failed:', error);
                        }
                    });
                }");

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
            return await _jsRuntime.InvokeAsync<bool>("eval",
                @"return 'serviceWorker' in navigator && 'PushManager' in window");
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
            return await _jsRuntime.InvokeAsync<bool>("eval",
                @"return window.matchMedia('(display-mode: standalone)').matches ||
                        window.navigator.standalone === true");
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
            var isInstallable = await _jsRuntime.InvokeAsync<bool>("eval",
                @"return 'beforeinstallprompt' in window");

            var isInstalled = await IsRunningAsPwaAsync();

            return new PwaInstallInfo
            {
                CanInstall = isInstallable && !isInstalled,
                IsInstalled = isInstalled,
                IsSupported = await IsPwaSupportedAsync()
            };
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
            var result = await _jsRuntime.InvokeAsync<bool>("eval",
                @"return new Promise((resolve) => {
                    if ('beforeinstallprompt' in window) {
                        const promptEvent = window.beforeinstallprompt;
                        if (promptEvent) {
                            promptEvent.prompt();
                            promptEvent.userChoice.then((choiceResult) => {
                                resolve(choiceResult.outcome === 'accepted');
                                delete window.beforeinstallprompt;
                            });
                        } else {
                            resolve(false);
                        }
                    } else {
                        resolve(false);
                    }
                })");

            return result;
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
            await _jsRuntime.InvokeVoidAsync("eval",
                @"if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                    navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHE' });
                }");
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
            await _jsRuntime.InvokeVoidAsync("eval",
                @"if ('serviceWorker' in navigator) {
                    navigator.serviceWorker.getRegistration().then(registration => {
                        if (registration) {
                            registration.update();
                        }
                    });
                }");
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
            var cacheInfo = await _jsRuntime.InvokeAsync<string>("eval",
                @"return new Promise(async (resolve) => {
                    if ('caches' in window) {
                        try {
                            const cacheNames = await caches.keys();
                            let totalSize = 0;
                            const cacheDetails = [];

                            for (const name of cacheNames) {
                                const cache = await caches.open(name);
                                const keys = await cache.keys();
                                let cacheSize = 0;

                                for (const request of keys) {
                                    try {
                                        const response = await cache.match(request);
                                        if (response) {
                                            const blob = await response.blob();
                                            cacheSize += blob.size;
                                        }
                                    } catch (e) {
                                        // Игнорируем ошибки при подсчете размера
                                    }
                                }

                                cacheDetails.push({
                                    name: name,
                                    size: cacheSize,
                                    itemCount: keys.length
                                });

                                totalSize += cacheSize;
                            }

                            resolve(JSON.stringify({
                                totalSize: totalSize,
                                cacheCount: cacheNames.length,
                                caches: cacheDetails
                            }));
                        } catch (error) {
                            resolve(JSON.stringify({
                                totalSize: 0,
                                cacheCount: 0,
                                caches: []
                            }));
                        }
                    } else {
                        resolve(JSON.stringify({
                            totalSize: 0,
                            cacheCount: 0,
                            caches: []
                        }));
                    }
                })");

            var info = JsonSerializer.Deserialize<PwaCacheInfo>(cacheInfo) ?? new PwaCacheInfo();
            return info;
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

            var result = await _jsRuntime.InvokeAsync<bool>("eval",
                $@"return new Promise((resolve) => {{
                    if ('Notification' in window && 'serviceWorker' in navigator) {{
                        if (Notification.permission === 'granted') {{
                            navigator.serviceWorker.getRegistration().then(registration => {{
                                if (registration) {{
                                    registration.showNotification('{title}', {{
                                        body: '{body}',
                                        icon: '{notificationOptions.Icon ?? "/_content/ZealousMindedPeopleGeo/icons/icon-192x192.png"}',
                                        badge: '{notificationOptions.Badge ?? "/_content/ZealousMindedPeopleGeo/icons/badge-72x72.png"}',
                                        tag: '{notificationOptions.Tag ?? "general"}',
                                        requireInteraction: {notificationOptions.RequireInteraction.ToString().ToLower()},
                                        silent: {notificationOptions.Silent.ToString().ToLower()}
                                    }});
                                    resolve(true);
                                }} else {{
                                    resolve(false);
                                }}
                            }});
                        }} else if (Notification.permission !== 'denied') {{
                            Notification.requestPermission().then(permission => {{
                                if (permission === 'granted') {{
                                    navigator.serviceWorker.getRegistration().then(registration => {{
                                        if (registration) {{
                                            registration.showNotification('{title}', {{
                                                body: '{body}',
                                                icon: '{notificationOptions.Icon ?? "/_content/ZealousMindedPeopleGeo/icons/icon-192x192.png"}',
                                                badge: '{notificationOptions.Badge ?? "/_content/ZealousMindedPeopleGeo/icons/badge-72x72.png"}',
                                                tag: '{notificationOptions.Tag ?? "general"}',
                                                requireInteraction: {notificationOptions.RequireInteraction.ToString().ToLower()},
                                                silent: {notificationOptions.Silent.ToString().ToLower()}
                                            }});
                                            resolve(true);
                                        }} else {{
                                            resolve(false);
                                        }}
                                    }});
                                }} else {{
                                    resolve(false);
                                }}
                            }});
                        }} else {{
                            resolve(false);
                        }}
                    }} else {{
                        resolve(false);
                    }}
                }})");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending PWA notification");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_pwaHelper != null)
        {
            try
            {
                await _pwaHelper.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing PWA helper");
            }
        }
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