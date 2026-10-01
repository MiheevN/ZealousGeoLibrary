using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Services.Geocoding;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo
{
    /// <summary>
    /// Расширения для настройки сервисов ZealousMindedPeopleGeo в DI контейнере
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет всё, что нужно компонентам ZealousMindedPeopleGeo, без настроек:
        /// участники хранятся в памяти, стили и скрипты библиотека подключает сама.
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeo(this IServiceCollection services)
        {
            return services.AddZealousMindedPeopleGeoCore();
        }

        /// <summary>
        /// Добавляет сервисы ZealousMindedPeopleGeo с настройками из секции
        /// <see cref="ZealousMindedPeopleGeoOptions.SectionName"/> конфигурации.
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeo(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ZealousMindedPeopleGeoOptions>(
                configuration.GetSection(ZealousMindedPeopleGeoOptions.SectionName));
            return services.AddZealousMindedPeopleGeoCore();
        }

        /// <summary>
        /// Добавляет сервисы ZealousMindedPeopleGeo с настройками из делегата.
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configureOptions">Делегат для настройки опций</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeo(
            this IServiceCollection services,
            Action<ZealousMindedPeopleGeoOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddZealousMindedPeopleGeoCore();
        }

        /// <summary>
        /// То же, что <see cref="AddZealousMindedPeopleGeo(IServiceCollection)"/>; оставлено
        /// для совместимости.
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeoServices(
            this IServiceCollection services)
        {
            return services.AddZealousMindedPeopleGeoCore();
        }

        // Общий набор для всех вариантов регистрации. TryAdd оставляет сервисы, которые
        // приложение зарегистрировало раньше (например, свой IParticipantRepository или
        // AddGeoDataDatabase), а повторный вызов ничего не дублирует.
        private static IServiceCollection AddZealousMindedPeopleGeoCore(this IServiceCollection services)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(ZealousMindedPeopleGeoMarker)))
            {
                return services;
            }
            services.AddSingleton<ZealousMindedPeopleGeoMarker>();

            // Без appsettings опции пустые: карта и геокодирование всё равно читают IOptions.
            services.AddOptions<ZealousMindedPeopleGeoOptions>();
            services.AddMemoryCache();

            services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZealousMindedPeopleGeo/1.0");
            });
            services.TryAddScoped<IGeocodingService, GoogleMapsGeocodingService>();
            services.TryAddScoped<IMapService, GoogleMapsServiceAdapter>();
            services.TryAddScoped<IGoogleSheetsService, GoogleSheetsService>();
            services.TryAddScoped<IParticipantRepository>(CreateParticipantRepository);
            services.TryAddScoped<IParticipantService, ParticipantService>();
            services.TryAddScoped<ICachingService, CachingService>();
            services.TryAddScoped<IGeoJsonService, FileGeoJsonService>();
            services.TryAddScoped<IPwaService, PwaService>();

            // 3D глобус и именованные контейнеры гео-данных (в памяти, если приложение
            // не выбрало хранение в БД через AddGeoDataDatabase).
            services.TryAddScoped<IThreeJsGlobeService, ThreeJsGlobeService>();
            services.TryAddScoped<IGlobeMediator, GlobeMediatorService>();
            services.TryAddScoped<GlobeStateService>();
            services.TryAddSingleton<IGeoDataContainerManager, GeoDataContainerManager>();
            services.TryAddScoped<GlobeDataInitializer>();

            return services;
        }

        // Google Sheets — только если в настройках указана таблица; иначе участники
        // хранятся в памяти, и компоненты работают без внешних сервисов.
        private static IParticipantRepository CreateParticipantRepository(IServiceProvider provider)
        {
            var options = provider.GetRequiredService<IOptions<ZealousMindedPeopleGeoOptions>>().Value;
            return string.IsNullOrWhiteSpace(options.GoogleSheetId)
                ? ActivatorUtilities.CreateInstance<InMemoryParticipantRepository>(provider)
                : ActivatorUtilities.CreateInstance<GoogleSheetsParticipantRepository>(provider);
        }

        private sealed class ZealousMindedPeopleGeoMarker
        {
        }

        /// <summary>
        /// Инициализирует хранилище данных для ZealousMindedPeopleGeo
        /// </summary>
        /// <param name="serviceProvider">Провайдер сервисов</param>
        /// <returns>Задача инициализации</returns>
        public static async Task InitializeZealousMindedPeopleGeoAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var participantService = scope.ServiceProvider.GetRequiredService<IParticipantService>();

            var success = await participantService.InitializeStorageAsync();

            if (success)
            {
                var logger = scope.ServiceProvider.GetService<ILogger<IParticipantService>>();
                logger?.LogInformation("ZealousMindedPeopleGeo хранилище данных инициализировано успешно");
            }
            else
            {
                var logger = scope.ServiceProvider.GetService<ILogger<IParticipantService>>();
                logger?.LogError("Ошибка инициализации ZealousMindedPeopleGeo хранилища данных");
            }
        }
    }
}