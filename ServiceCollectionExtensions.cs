using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        /// точки и участники хранятся в памяти, стили и скрипты библиотека подключает сама.
        /// Своё хранилище (база данных, внешний API) приложение регистрирует до этого вызова:
        /// <see cref="IGeoDataContainerManager"/> и/или <see cref="IParticipantRepository"/>.
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
        // приложение зарегистрировало раньше (например, своё хранилище точек или участников),
        // а повторный вызов ничего не дублирует.
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
            services.TryAddScoped<IParticipantRepository, InMemoryParticipantRepository>();
            services.TryAddScoped<ICachingService, CachingService>();
            services.TryAddScoped<IPwaService, PwaService>();

            // 3D глобус и именованные контейнеры гео-данных (в памяти, если приложение
            // не зарегистрировало своё хранилище).
            services.TryAddScoped<IThreeJsGlobeService, ThreeJsGlobeService>();
            services.TryAddScoped<IGlobeMediator, GlobeMediatorService>();
            services.TryAddScoped<GlobeStateService>();
            services.TryAddSingleton<IGeoDataContainerManager, GeoDataContainerManager>();
            services.TryAddScoped<GlobeDataInitializer>();

            return services;
        }

        private sealed class ZealousMindedPeopleGeoMarker
        {
        }
    }
}