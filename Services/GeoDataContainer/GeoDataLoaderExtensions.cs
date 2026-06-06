using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.Synchronization;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Расширения для удобной работы с загрузкой и сохранением гео-данных.
/// Обеспечивает интеграцию между контейнерами данных и 3D глобусами.
/// </summary>
public static class GeoDataLoaderExtensions
{
    /// <summary>
    /// Регистрирует сервисы для работы с именованными контейнерами гео-данных (хранение в памяти)
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    public static IServiceCollection AddGeoDataContainers(this IServiceCollection services)
    {
        services.AddGeoDataChangeNotifications();
        services.TryAddSingleton<IGeoDataContainerManager, GeoDataContainerManager>();
        return services;
    }

    /// <summary>
    /// Регистрирует сервисы для работы с именованными контейнерами гео-данных
    /// с хранением в базе данных. Менеджер контейнеров (<see cref="IGeoDataContainerManager"/>)
    /// будет использовать БД вместо памяти, что позволяет сохранять данные множества
    /// глобусов между запусками приложения.
    /// </summary>
    /// <param name="services">Коллекция сервисов</param>
    /// <param name="configureDbContext">
    /// Делегат настройки контекста БД. Провайдер выбирается потребителем библиотеки,
    /// например: <c>options =&gt; options.UseSqlite("Data Source=geodata.db")</c>.
    /// </param>
    /// <returns>Коллекция сервисов для цепочки вызовов</returns>
    /// <remarks>
    /// После построения провайдера сервисов вызовите
    /// <see cref="EnsureGeoDataDatabaseCreatedAsync"/>, чтобы создать схему БД,
    /// либо примените миграции EF Core самостоятельно.
    /// </remarks>
    public static IServiceCollection AddGeoDataDatabase(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        services.AddGeoDataChangeNotifications();

        // Фабрика контекстов безопасна для многопоточной среды (в т.ч. Blazor)
        services.AddDbContextFactory<GeoDataDbContext>(configureDbContext);

        // Менеджер контейнеров с хранением в БД заменяет реализацию по умолчанию
        services.AddSingleton<IGeoDataContainerManager, DatabaseGeoDataContainerManager>();

        return services;
    }

    /// <summary>
    /// Создает схему базы данных гео-данных, если она ещё не существует
    /// (через <c>EnsureCreated</c>). Удобно для разработки, тестов и сценариев
    /// без миграций. Для продакшена рекомендуется использовать миграции EF Core.
    /// </summary>
    /// <param name="serviceProvider">Провайдер сервисов</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Задача инициализации</returns>
    public static async Task EnsureGeoDataDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct = default)
    {
        using var scope = serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GeoDataDbContext>>();
        await using var context = await factory.CreateDbContextAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
    }

    /// <summary>
    /// Загружает данные из контейнера в глобус
    /// </summary>
    /// <param name="containerManager">Менеджер контейнеров</param>
    /// <param name="containerId">ID контейнера данных</param>
    /// <param name="globeMediator">Посредник глобуса</param>
    /// <param name="globeContainerId">ID HTML-контейнера глобуса</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    public static async Task<GeoDataOperationResult> LoadToGlobeAsync(
        this IGeoDataContainerManager containerManager,
        string containerId,
        IGlobeMediator globeMediator,
        string globeContainerId,
        CancellationToken ct = default)
    {
        var container = containerManager.GetContainer(containerId);
        if (container == null)
        {
            return GeoDataOperationResult.Fail($"Container '{containerId}' not found");
        }

        var participants = await container.GetAllParticipantsAsync(ct);
        var participantsList = participants.ToList();

        var result = await globeMediator.AddParticipantsAsync(globeContainerId, participantsList);

        if (result.Success)
        {
            return GeoDataOperationResult.Ok(participantsList.Count);
        }

        return GeoDataOperationResult.Fail(result.ErrorMessage ?? "Failed to add participants to globe");
    }
}

/// <summary>
/// Помощник для инициализации глобуса с данными из контейнера.
/// Упрощает процесс создания и наполнения 3D глобуса.
/// </summary>
public class GlobeDataInitializer
{
    private readonly IGeoDataContainerManager _containerManager;
    private readonly IGlobeMediator _globeMediator;
    private readonly GlobeStateService _globeStateService;
    private readonly ILogger<GlobeDataInitializer> _logger;

    /// <summary>
    /// Создает новый инициализатор данных глобуса
    /// </summary>
    public GlobeDataInitializer(
        IGeoDataContainerManager containerManager,
        IGlobeMediator globeMediator,
        GlobeStateService globeStateService,
        ILogger<GlobeDataInitializer> logger)
    {
        _containerManager = containerManager;
        _globeMediator = globeMediator;
        _globeStateService = globeStateService;
        _logger = logger;
    }

    /// <summary>
    /// Инициализирует глобус и загружает в него данные из контейнера
    /// </summary>
    /// <param name="globeId">ID глобуса (используется для идентификации в GlobeStateService)</param>
    /// <param name="htmlContainerId">ID HTML-контейнера для рендеринга глобуса</param>
    /// <param name="dataContainerId">ID контейнера данных для загрузки</param>
    /// <param name="options">Настройки глобуса</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат инициализации</returns>
    public async Task<GlobeInitializationWithDataResult> InitializeGlobeWithDataAsync(
        string globeId,
        string htmlContainerId,
        string dataContainerId,
        GlobeOptions? options = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing globe '{GlobeId}' with data from container '{DataContainerId}'", globeId, dataContainerId);

        var result = new GlobeInitializationWithDataResult { GlobeId = globeId };

        try
        {
            // 1. Инициализируем глобус
            var initResult = await _globeMediator.InitializeGlobeAsync(
                htmlContainerId,
                options ?? new GlobeOptions());

            if (!initResult.Success)
            {
                result.ErrorMessage = $"Globe initialization failed: {initResult.ErrorMessage}";
                _logger.LogError("Globe initialization failed: {Error}", initResult.ErrorMessage);
                return result;
            }

            // 2. Обновляем состояние глобуса
            _globeStateService.UpdateState(globeId, s => s.IsInitialized = true);

            // 3. Получаем данные из контейнера
            var dataContainer = _containerManager.GetContainer(dataContainerId);
            if (dataContainer == null)
            {
                // Если контейнер не существует, создаем пустой
                dataContainer = _containerManager.GetOrCreateContainer(dataContainerId);
                _logger.LogInformation("Created new empty container '{DataContainerId}'", dataContainerId);
            }

            var participants = await dataContainer.GetAllParticipantsAsync(ct);
            var participantsList = participants.ToList();

            // 4. Загружаем данные в глобус
            if (participantsList.Count > 0)
            {
                var addResult = await _globeMediator.AddParticipantsAsync(htmlContainerId, participantsList);

                if (!addResult.Success)
                {
                    _logger.LogWarning("Failed to add participants to globe: {Error}", addResult.ErrorMessage);
                }

                // 5. Обновляем состояние
                _globeStateService.UpdateState(globeId, s =>
                {
                    s.Participants = participantsList;
                    s.ParticipantCount = participantsList.Count;
                });
            }

            result.Success = true;
            result.ParticipantCount = participantsList.Count;

            _logger.LogInformation("Globe '{GlobeId}' initialized successfully with {Count} participants", globeId, participantsList.Count);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error initializing globe '{GlobeId}'", globeId);
        }

        return result;
    }

    /// <summary>
    /// Загружает данные из JSON в контейнер и отображает на глобусе
    /// </summary>
    /// <param name="globeId">ID глобуса</param>
    /// <param name="htmlContainerId">ID HTML-контейнера</param>
    /// <param name="dataContainerId">ID контейнера данных</param>
    /// <param name="jsonContent">JSON с данными участников</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    public async Task<GeoDataOperationResult> LoadJsonDataToGlobeAsync(
        string globeId,
        string htmlContainerId,
        string dataContainerId,
        string jsonContent,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Загружаем данные в контейнер
            var loadResult = await _containerManager.LoadFromJsonAsync(dataContainerId, jsonContent, ct);

            if (!loadResult.Success)
            {
                return loadResult;
            }

            // 2. Загружаем данные в глобус
            return await _containerManager.LoadToGlobeAsync(dataContainerId, _globeMediator, htmlContainerId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON data to globe '{GlobeId}'", globeId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Добавляет участника в контейнер и обновляет отображение на глобусе
    /// </summary>
    /// <param name="globeId">ID глобуса</param>
    /// <param name="htmlContainerId">ID HTML-контейнера</param>
    /// <param name="dataContainerId">ID контейнера данных</param>
    /// <param name="participant">Данные участника</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    public async Task<GeoDataOperationResult> AddParticipantToGlobeAsync(
        string globeId,
        string htmlContainerId,
        string dataContainerId,
        Participant participant,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Получаем или создаем контейнер
            var container = _containerManager.GetOrCreateContainer(dataContainerId);

            // 2. Добавляем участника в контейнер
            var addResult = await container.AddParticipantAsync(participant, ct);

            if (!addResult.Success)
            {
                return addResult;
            }

            // 3. Обновляем глобус полным текущим набором, чтобы удалить устаревшие точки
            var participants = (await container.GetAllParticipantsAsync(ct)).ToList();
            var globeResult = await _globeMediator.AddParticipantsAsync(htmlContainerId, participants);

            if (globeResult.Success)
            {
                // 4. Обновляем состояние
                _globeStateService.UpdateState(globeId, s =>
                {
                    s.Participants = participants;
                    s.ParticipantCount = participants.Count;
                });
            }

            return GeoDataOperationResult.Ok(1, participant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding participant to globe '{GlobeId}'", globeId);
            return GeoDataOperationResult.Fail(ex.Message);
        }
    }
}

/// <summary>
/// Результат инициализации глобуса с данными
/// </summary>
public class GlobeInitializationWithDataResult
{
    /// <summary>
    /// Успешность операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID глобуса
    /// </summary>
    public string GlobeId { get; set; } = string.Empty;

    /// <summary>
    /// Количество загруженных участников
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public string? ErrorMessage { get; set; }
}
