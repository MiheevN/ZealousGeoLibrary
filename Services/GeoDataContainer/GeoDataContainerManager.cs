using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Services.Synchronization;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Менеджер именованных контейнеров гео-данных, хранящихся в памяти.
/// Обеспечивает централизованное управление несколькими контейнерами данных.
/// </summary>
public class GeoDataContainerManager : GeoDataContainerManagerBase
{
    private readonly ConcurrentDictionary<string, IGeoDataContainer> _containers = new();
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Создает новый менеджер контейнеров
    /// </summary>
    /// <param name="logger">Логгер</param>
    /// <param name="loggerFactory">Фабрика логгеров для создания логгеров контейнеров</param>
    /// <param name="changeNotifier">Общий уведомитель изменений гео-данных</param>
    public GeoDataContainerManager(
        ILogger<GeoDataContainerManager> logger,
        ILoggerFactory loggerFactory,
        IGeoDataChangeNotifier? changeNotifier = null)
        : base(logger, changeNotifier)
    {
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override IGeoDataContainer GetOrCreateContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentNullException(nameof(containerId));
        }

        return _containers.GetOrAdd(containerId, id =>
        {
            Logger.LogInformation("Creating new geo-data container: {ContainerId}", id);
            var containerLogger = _loggerFactory.CreateLogger<InMemoryGeoDataContainer>();
            return new InMemoryGeoDataContainer(id, containerLogger, HandleDataChanged);
        });
    }

    /// <inheritdoc />
    public override IGeoDataContainer? GetContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return null;
        }

        _containers.TryGetValue(containerId, out var container);
        return container;
    }

    /// <inheritdoc />
    public override bool ContainerExists(string containerId)
    {
        return !string.IsNullOrWhiteSpace(containerId) && _containers.ContainsKey(containerId);
    }

    /// <inheritdoc />
    public override bool RemoveContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return false;
        }

        if (_containers.TryRemove(containerId, out _))
        {
            Logger.LogInformation("Removed geo-data container: {ContainerId}", containerId);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetContainerIds()
    {
        return _containers.Keys.ToList();
    }
}
