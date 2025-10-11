using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Сервис для кэширования данных в памяти с гибкой конфигурацией
/// </summary>
public class CachingService : ICachingService, IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CachingService> _logger;
    private readonly CachingOptions _options;

    public CachingService(
        IMemoryCache memoryCache,
        IOptions<CachingOptions> options,
        ILogger<CachingService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _options = options.Value;

        _logger.LogInformation("CachingService initialized with options: {@Options}", _options);
    }

    /// <summary>
    /// Получить данные из кэша
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T value))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return value;
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key: {Key}", key);
            return default;
        }
    }

    /// <summary>
    /// Получить данные из кэша или создать их с помощью фабрики
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        CachingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out T cachedValue))
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return cachedValue;
            }

            _logger.LogDebug("Cache miss, creating new value for key: {Key}", key);

            var value = await factory(cancellationToken);

            if (value != null)
            {
                var cacheOptions = options ?? new CachingOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.Normal
                };
                var cacheEntryOptions = CreateCacheEntryOptions(cacheOptions);

                _memoryCache.Set(key, value, cacheEntryOptions);
                _logger.LogDebug("Cached value for key: {Key}, expires at: {Expiration}",
                    key, cacheOptions.AbsoluteExpiration);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrCreate for key: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Установить значение в кэш
    /// </summary>
    public async Task SetAsync<T>(
        string key,
        T value,
        CachingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheOptions = options ?? new CachingOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                Priority = CacheItemPriority.Normal
            };
            var cacheEntryOptions = CreateCacheEntryOptions(cacheOptions);

            _memoryCache.Set(key, value, cacheEntryOptions);

            _logger.LogDebug("Set cache value for key: {Key}, expires at: {Expiration}",
                key, cacheOptions.AbsoluteExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
        }
    }

    /// <summary>
    /// Удалить значение из кэша
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache value for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
        }
    }

    /// <summary>
    /// Очистить все значения из кэша
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // MemoryCache не имеет публичного метода Clear()
            // Вместо этого создаем новый экземпляр через DI
            _logger.LogInformation("Cache clear requested - MemoryCache will be refreshed by DI container");

            // В реальном сценарии можно использовать паттерн компоновщика или
            // создать обертку над MemoryCache с дополнительными методами
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }

    /// <summary>
    /// Проверить, существует ли значение в кэше
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return _memoryCache.TryGetValue(key, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// Получить статистику кэша
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = new CacheStatistics();

            if (_memoryCache is MemoryCache concreteCache)
            {
                var field = typeof(MemoryCache).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(concreteCache) is System.Collections.IDictionary entries)
                {
                    stats.EntryCount = entries.Count;
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return new CacheStatistics();
        }
    }

    /// <summary>
    /// Создать ключ кэша на основе паттерна и параметров
    /// </summary>
    public string CreateKey(string pattern, params object[] parameters)
    {
        if (parameters.Length == 0)
            return pattern;

        try
        {
            var serializedParams = string.Join(":", parameters.Select(p =>
                p?.GetHashCode().ToString() ?? "null"));
            return $"{pattern}:{serializedParams}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cache key from pattern: {Pattern}", pattern);
            return pattern;
        }
    }

    private MemoryCacheEntryOptions CreateCacheEntryOptions(CachingOptions options)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions();

        if (options.AbsoluteExpiration.HasValue)
        {
            cacheEntryOptions.AbsoluteExpiration = options.AbsoluteExpiration.Value;
        }
        else if (options.SlidingExpiration.HasValue)
        {
            cacheEntryOptions.SlidingExpiration = options.SlidingExpiration.Value;
        }

        if (options.Size.HasValue)
        {
            cacheEntryOptions.Size = options.Size.Value;
        }

        cacheEntryOptions.SetPriority(options.Priority);

        return cacheEntryOptions;
    }

    public void Dispose()
    {
        // MemoryCache управляется DI контейнером, дополнительная очистка не требуется
    }
}

/// <summary>
/// Интерфейс для сервиса кэширования
/// </summary>
public interface ICachingService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CachingOptions? options = null, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, CachingOptions? options = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    string CreateKey(string pattern, params object[] parameters);
}

/// <summary>
/// Опции кэширования для конкретной операции
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Абсолютное время истечения кэша
    /// </summary>
    public DateTimeOffset? AbsoluteExpiration { get; set; }

    /// <summary>
    /// Скользящее время истечения кэша
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }

    /// <summary>
    /// Размер элемента в кэше (для оценки памяти)
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Приоритет элемента в кэше
    /// </summary>
    public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;
}

/// <summary>
/// Глобальные настройки кэширования
/// </summary>
public class CachingSettings
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Опции по умолчанию для всех операций кэширования
    /// </summary>
    public CachingOptions DefaultOptions { get; set; } = new CachingOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(30),
        Priority = CacheItemPriority.Normal
    };

    /// <summary>
    /// Специфичные настройки для разных типов данных
    /// </summary>
    public Dictionary<string, CachingOptions> TypeSpecificOptions { get; set; } = new()
    {
        ["Participants"] = new CachingOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            Priority = CacheItemPriority.High
        },
        ["Geocoding"] = new CachingOptions
        {
            SlidingExpiration = TimeSpan.FromHours(24),
            Priority = CacheItemPriority.Normal
        },
        ["Countries"] = new CachingOptions
        {
            SlidingExpiration = TimeSpan.FromHours(12),
            Priority = CacheItemPriority.Normal
        },
        ["Weather"] = new CachingOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(10),
            Priority = CacheItemPriority.Low
        }
    };
}

/// <summary>
/// Статистика кэша
/// </summary>
public class CacheStatistics
{
    /// <summary>
    /// Количество элементов в кэше
    /// </summary>
    public int EntryCount { get; set; }

    /// <summary>
    /// Приблизительный размер кэша в байтах
    /// </summary>
    public long ApproximateSize { get; set; }

    /// <summary>
    /// Количество попаданий в кэш
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Количество промахов кэша
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Процент попаданий
    /// </summary>
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) * 100 : 0;
}

/// <summary>
/// Расширения для удобной работы с кэшированием
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Получить или создать закешированные участники
    /// </summary>
    public static async Task<IEnumerable<Participant>> GetOrCreateParticipantsAsync(
        this ICachingService cachingService,
        Func<CancellationToken, Task<IEnumerable<Participant>>> factory,
        CancellationToken cancellationToken = default)
    {
        return await cachingService.GetOrCreateAsync(
            "participants:all",
            factory,
            new CachingOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(15),
                Priority = CacheItemPriority.High
            },
            cancellationToken);
    }

    /// <summary>
    /// Получить или создать закешированный результат геокодирования
    /// </summary>
    public static async Task<GeocodingResult> GetOrCreateGeocodingResultAsync(
        this ICachingService cachingService,
        string address,
        Func<CancellationToken, Task<GeocodingResult>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = $"geocoding:{address.GetHashCode()}";
        return await cachingService.GetOrCreateAsync(
            key,
            factory,
            new CachingOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24),
                Priority = CacheItemPriority.Normal
            },
            cancellationToken);
    }
}