using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Базовый класс для менеджеров именованных контейнеров гео-данных.
/// Содержит общую логику загрузки/сохранения (JSON, наборы точек),
/// не зависящую от конкретного хранилища (память, база данных и т.д.).
/// Конкретные реализации определяют только способ создания и поиска контейнеров.
/// </summary>
public abstract class GeoDataContainerManagerBase : IGeoDataContainerManager
{
    /// <summary>
    /// Логгер менеджера контейнеров
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Опции сериализации для экспорта данных в JSON
    /// </summary>
    protected static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Опции десериализации для загрузки данных из JSON
    /// </summary>
    protected static readonly JsonSerializerOptions ImportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public event Action<string, GeoDataChangeType>? OnDataChanged;

    /// <summary>
    /// Создает новый базовый менеджер контейнеров
    /// </summary>
    /// <param name="logger">Логгер</param>
    protected GeoDataContainerManagerBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract IGeoDataContainer GetOrCreateContainer(string containerId);

    /// <inheritdoc />
    public abstract IGeoDataContainer? GetContainer(string containerId);

    /// <inheritdoc />
    public abstract bool ContainerExists(string containerId);

    /// <inheritdoc />
    public abstract bool RemoveContainer(string containerId);

    /// <inheritdoc />
    public abstract IEnumerable<string> GetContainerIds();

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> LoadPointsAsync(string containerId, IEnumerable<GeoPoint> points, CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(points);
            var container = GetOrCreateContainer(containerId);

            // Очищаем контейнер перед загрузкой новых данных
            await container.ClearAsync(ct);

            var result = await container.AddPointsAsync(points, ct);

            Logger.LogInformation("Loaded {Count} points into container '{ContainerId}'", result.ProcessedCount, containerId);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading data into container '{ContainerId}'", containerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> LoadFromJsonFileAsync(string containerId, string jsonFilePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(jsonFilePath))
            {
                return GeoDataOperationResult.Fail($"File not found: {jsonFilePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(jsonFilePath, ct);
            return await LoadFromJsonAsync(containerId, jsonContent, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading data from JSON file '{FilePath}' into container '{ContainerId}'", jsonFilePath, containerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> LoadFromJsonAsync(string containerId, string jsonContent, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return GeoDataOperationResult.Fail("JSON content is empty");
            }

            using var document = JsonDocument.Parse(jsonContent);
            if (GeoPointGeoJson.IsGeoJson(document.RootElement))
            {
                return await LoadGeoJsonAsync(containerId, document.RootElement, ct);
            }

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return GeoDataOperationResult.Fail("JSON content must be an array of points or a GeoJSON FeatureCollection");
            }

            var points = new List<GeoPoint>();
            var unreadable = 0;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                var point = ReadPoint(element);
                if (point is null)
                {
                    unreadable++;
                }
                else
                {
                    points.Add(point);
                }
            }

            var result = await LoadPointsAsync(containerId, points, ct);
            result.SkippedCount += unreadable;
            return result;
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Error parsing JSON content for container '{ContainerId}'", containerId);
            return GeoDataOperationResult.Fail($"JSON parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading data from JSON into container '{ContainerId}'", containerId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    private async ValueTask<GeoDataOperationResult> LoadGeoJsonAsync(string containerId, JsonElement root, CancellationToken ct)
    {
        var read = GeoPointGeoJson.Read(root);
        if (read.SkippedCount > 0)
        {
            Logger.LogWarning(
                "GeoJSON for container '{ContainerId}': skipped {Count} objects, first: {Reasons}",
                containerId, read.SkippedCount, string.Join("; ", read.Skipped.Take(5)));
        }

        var result = await LoadPointsAsync(containerId, read.Points, ct);
        result.SkippedCount += read.SkippedCount;
        return result;
    }

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> SaveToJsonFileAsync(string containerId, string jsonFilePath, CancellationToken ct = default)
    {
        try
        {
            await WriteFileAsync(jsonFilePath, await ExportToJsonAsync(containerId, ct), ct);
            Logger.LogInformation("Saved container '{ContainerId}' data to file '{FilePath}'", containerId, jsonFilePath);
            return GeoDataOperationResult.Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving container '{ContainerId}' data to file '{FilePath}'", containerId, jsonFilePath);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> SaveToGeoJsonFileAsync(string containerId, string geoJsonFilePath, CancellationToken ct = default)
    {
        try
        {
            await WriteFileAsync(geoJsonFilePath, await ExportToGeoJsonAsync(containerId, ct), ct);
            Logger.LogInformation("Saved container '{ContainerId}' as GeoJSON to file '{FilePath}'", containerId, geoJsonFilePath);
            return GeoDataOperationResult.Ok();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error saving container '{ContainerId}' as GeoJSON to file '{FilePath}'", containerId, geoJsonFilePath);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    private static async Task WriteFileAsync(string filePath, string content, CancellationToken ct)
    {
        // Создаем директорию если не существует
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, content, ct);
    }

    /// <inheritdoc />
    public virtual async ValueTask<string> ExportToJsonAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            var container = GetContainer(containerId);
            if (container == null)
            {
                Logger.LogWarning("Container '{ContainerId}' not found for export", containerId);
                return "[]";
            }

            var points = await container.GetPointsAsync(ct);

            return JsonSerializer.Serialize(points, ExportJsonOptions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting container '{ContainerId}' to JSON", containerId);
            return "[]";
        }
    }

    /// <inheritdoc />
    public virtual async ValueTask<string> ExportToGeoJsonAsync(string containerId, CancellationToken ct = default)
    {
        var container = GetContainer(containerId);
        if (container == null)
        {
            Logger.LogWarning("Container '{ContainerId}' not found for GeoJSON export", containerId);
            return GeoPointGeoJson.Write(Array.Empty<GeoPoint>());
        }

        return GeoPointGeoJson.Write(await container.GetPointsAsync(ct));
    }

    // Элемент массива — точка, если у него есть title или properties, иначе участник в
    // прежнем формате. Без координат элемент не читается: иначе точка молча оказалась
    // бы в (0, 0).
    private static GeoPoint? ReadPoint(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !HasProperty(element, "latitude")
            || !HasProperty(element, "longitude"))
        {
            return null;
        }

        try
        {
            if (HasProperty(element, "title") || HasProperty(element, "properties"))
            {
                var point = element.Deserialize<GeoPoint>(ImportJsonOptions);
                if (point is not null)
                {
                    point.Properties ??= new Dictionary<string, string>();
                }

                return point;
            }

            var participant = element.Deserialize<Participant>(ImportJsonOptions);
            return participant?.Latitude is null || participant.Longitude is null ? null : participant.ToGeoPoint();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind != JsonValueKind.Null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Обрабатывает событие изменения данных в контейнере и ретранслирует его подписчикам
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="changeType">Тип изменения</param>
    protected void HandleDataChanged(string containerId, GeoDataChangeType changeType)
    {
        try
        {
            Logger.LogDebug("Data changed in container '{ContainerId}': {ChangeType}", containerId, changeType);
            OnDataChanged?.Invoke(containerId, changeType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling data change event for container '{ContainerId}'", containerId);
        }
    }
}
