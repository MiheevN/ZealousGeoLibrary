using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Менеджер именованных контейнеров гео-данных с хранением в базе данных.
/// Каждый контейнер соответствует набору строк с одинаковым
/// <see cref="GeoDataParticipantEntity.ContainerId"/>, что позволяет хранить
/// данные множества глобусов в одной базе данных.
/// </summary>
public class DatabaseGeoDataContainerManager : GeoDataContainerManagerBase
{
    private readonly IDbContextFactory<GeoDataDbContext> _contextFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IGeoDataContainer> _containerCache = new();

    /// <summary>
    /// Создает новый менеджер контейнеров с хранением в БД
    /// </summary>
    /// <param name="contextFactory">Фабрика контекстов БД</param>
    /// <param name="logger">Логгер</param>
    /// <param name="loggerFactory">Фабрика логгеров для создания логгеров контейнеров</param>
    public DatabaseGeoDataContainerManager(
        IDbContextFactory<GeoDataDbContext> contextFactory,
        ILogger<DatabaseGeoDataContainerManager> logger,
        ILoggerFactory loggerFactory)
        : base(logger)
    {
        _contextFactory = contextFactory;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public override IGeoDataContainer GetOrCreateContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            throw new ArgumentNullException(nameof(containerId));
        }

        return _containerCache.GetOrAdd(containerId, id =>
        {
            Logger.LogInformation("Creating database geo-data container accessor: {ContainerId}", id);
            var containerLogger = _loggerFactory.CreateLogger<DatabaseGeoDataContainer>();
            return new DatabaseGeoDataContainer(id, _contextFactory, containerLogger, HandleDataChanged);
        });
    }

    /// <inheritdoc />
    public override IGeoDataContainer? GetContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return null;
        }

        return ContainerExists(containerId) ? GetOrCreateContainer(containerId) : null;
    }

    /// <inheritdoc />
    public override bool ContainerExists(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return false;
        }

        using var context = _contextFactory.CreateDbContext();
        return context.Participants.Any(p => p.ContainerId == containerId);
    }

    /// <inheritdoc />
    public override bool RemoveContainer(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return false;
        }

        using var context = _contextFactory.CreateDbContext();
        var removed = context.Participants
            .Where(p => p.ContainerId == containerId)
            .ExecuteDelete();

        _containerCache.TryRemove(containerId, out _);

        if (removed > 0)
        {
            Logger.LogInformation("Removed database geo-data container: {ContainerId}", containerId);
            HandleDataChanged(containerId, GeoDataChangeType.Cleared);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override IEnumerable<string> GetContainerIds()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Participants
            .Select(p => p.ContainerId)
            .Distinct()
            .ToList();
    }
}
