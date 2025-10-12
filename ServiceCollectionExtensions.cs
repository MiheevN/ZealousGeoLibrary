using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Services.Geocoding;
using ZealousMindedPeopleGeo.Services.Mapping;

namespace ZealousMindedPeopleGeo
{
    /// <summary>
    /// Расширения для настройки сервисов ZealousMindedPeopleGeo в DI контейнере
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет сервисы ZealousMindedPeopleGeo в DI контейнер
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configuration">Конфигурация приложения</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeo(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Настройка конфигурации
            services.Configure<ZealousMindedPeopleGeoOptions>(
                configuration.GetSection(ZealousMindedPeopleGeoOptions.SectionName));

            // Регистрация HTTP клиента для Google Maps API
            services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZealousMindedPeopleGeo/1.0");
            });

            // Регистрация основных сервисов
            services.AddScoped<IGoogleSheetsService, GoogleSheetsService>();
            services.AddScoped<IGoogleMapsService, GoogleMapsService>();
            services.AddScoped<IParticipantService, ParticipantService>();

            // Регистрация интерфейсов источников данных (с адаптерами Google)
            services.AddScoped<IParticipantRepository, GoogleSheetsParticipantRepository>();
            services.AddScoped<IGeocodingService, GoogleMapsGeocodingService>();
            services.AddScoped<IMapService, GoogleMapsServiceAdapter>();
            services.AddScoped<ICachingService, CachingService>();

            return services;
        }

        /// <summary>
        /// Добавляет сервисы ZealousMindedPeopleGeo для тестирования (без внешних зависимостей)
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeoServices(
            this IServiceCollection services)
        {
            // Регистрация HTTP клиента для геокодирования
            services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZealousMindedPeopleGeo/1.0");
            });

            // Регистрация сервисов с зависимостями
            services.AddScoped<IParticipantRepository, InMemoryParticipantRepository>();
            services.AddScoped<IThreeJsGlobeService, ThreeJsGlobeService>();
            services.AddScoped<IGlobeMediator, GlobeMediatorService>();
            services.AddScoped<ICachingService, CachingService>();
            services.AddScoped<IGeoJsonService, FileGeoJsonService>();

            // Регистрация основного сервиса участников
            services.AddScoped<IParticipantService, ParticipantService>();

            return services;
        }

        /// <summary>
        /// Добавляет сервисы ZealousMindedPeopleGeo с пользовательской конфигурацией
        /// </summary>
        /// <param name="services">Коллекция сервисов</param>
        /// <param name="configureOptions">Делегат для настройки опций</param>
        /// <returns>Коллекция сервисов для цепочки вызовов</returns>
        public static IServiceCollection AddZealousMindedPeopleGeo(
            this IServiceCollection services,
            Action<ZealousMindedPeopleGeoOptions> configureOptions)
        {
            services.Configure(configureOptions);

            // Регистрация HTTP клиента для Google Maps API
            services.AddHttpClient<IGoogleMapsService, GoogleMapsService>(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("ZealousMindedPeopleGeo/1.0");
            });

            // Регистрация основных сервисов
            services.AddScoped<IGoogleSheetsService, GoogleSheetsService>();
            services.AddScoped<IGoogleMapsService, GoogleMapsService>();
            services.AddScoped<IParticipantService, ParticipantService>();
            services.AddScoped<ICachingService, CachingService>();

            return services;
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