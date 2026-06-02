using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Базовый класс для менеджеров именованных контейнеров гео-данных.
/// Содержит общую логику загрузки/сохранения (JSON, массивы участников),
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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
    public virtual async ValueTask<GeoDataOperationResult> LoadDataAsync(string containerId, IEnumerable<Participant> participants, CancellationToken ct = default)
    {
        try
        {
            var container = GetOrCreateContainer(containerId);

            // Очищаем контейнер перед загрузкой новых данных
            await container.ClearAsync(ct);

            // Добавляем участников
            var result = await container.AddParticipantsAsync(participants, ct);

            Logger.LogInformation("Loaded {Count} participants into container '{ContainerId}'", result.ProcessedCount, containerId);

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

            var participants = JsonSerializer.Deserialize<List<Participant>>(jsonContent, ImportJsonOptions);

            if (participants == null)
            {
                return GeoDataOperationResult.Fail("Failed to deserialize JSON content");
            }

            return await LoadDataAsync(containerId, participants, ct);
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

    /// <inheritdoc />
    public virtual async ValueTask<GeoDataOperationResult> SaveToJsonFileAsync(string containerId, string jsonFilePath, CancellationToken ct = default)
    {
        try
        {
            var jsonContent = await ExportToJsonAsync(containerId, ct);

            // Создаем директорию если не существует
            var directory = Path.GetDirectoryName(jsonFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(jsonFilePath, jsonContent, ct);

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

            var participants = await container.GetAllParticipantsAsync(ct);

            return JsonSerializer.Serialize(participants, ExportJsonOptions);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting container '{ContainerId}' to JSON", containerId);
            return "[]";
        }
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
