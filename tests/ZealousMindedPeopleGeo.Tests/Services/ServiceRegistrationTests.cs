using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Geocoding;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.Repositories;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// Приложению должно хватать одного вызова AddZealousMindedPeopleGeo: любой вариант
/// регистрирует всё, что внедряют компоненты библиотеки.
/// </summary>
public class ServiceRegistrationTests
{
    public static TheoryData<string> Registrations => new()
    {
        "AddZealousMindedPeopleGeo()",
        "AddZealousMindedPeopleGeo(configuration)",
        "AddZealousMindedPeopleGeo(options => ...)",
        "AddZealousMindedPeopleGeoServices()"
    };

    // Всё, что компоненты получают через @inject, и сервисы, от которых они зависят.
    private static readonly Type[] ComponentDependencies =
    {
        typeof(IOptions<ZealousMindedPeopleGeoOptions>),
        typeof(IParticipantRepository),
        typeof(IGeoDataContainerManager),
        typeof(IGeocodingService),
        typeof(ICachingService),
        typeof(IPwaService),
        typeof(IThreeJsGlobeService),
        typeof(IGlobeMediator),
        typeof(GlobeStateService),
        typeof(GlobeDataInitializer)
    };

    [Theory]
    [MemberData(nameof(Registrations))]
    public async Task EveryRegistration_ResolvesEverythingComponentsNeed(string registration)
    {
        var services = CreateServices();
        Register(services, registration);

        // ValidateOnBuild падает, если у любого зарегистрированного сервиса нет зависимости.
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        // ThreeJsGlobeService освобождается только асинхронно.
        await using var scope = provider.CreateAsyncScope();

        foreach (var type in ComponentDependencies)
        {
            Assert.True(scope.ServiceProvider.GetService(type) is not null, $"{registration}: {type.Name} is not registered");
        }
    }

    [Fact]
    public void Storage_IsInMemoryUnlessTheApplicationRegistersItsOwn()
    {
        Assert.IsType<InMemoryParticipantRepository>(Resolve<IParticipantRepository>(s => s.AddZealousMindedPeopleGeo()));
        Assert.IsType<GeoDataContainerManager>(Resolve<IGeoDataContainerManager>(s => s.AddZealousMindedPeopleGeo()));
    }

    [Fact]
    public void Options_WithoutGoogleKey_PassValidation()
    {
        // Так их проверяет ValidateDataAnnotations(): приложение без ключа Google должно запускаться.
        var options = new ZealousMindedPeopleGeoOptions();
        var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options, new System.ComponentModel.DataAnnotations.ValidationContext(options), errors, validateAllProperties: true);

        Assert.True(valid, string.Join("; ", errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Configuration_BindsKeysDocumentedInReadme()
    {
        // Ключи из раздела README «Конфигурация».
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ZealousMindedPeopleGeo:GoogleMapsApiKey"] = "maps-key",
                ["ZealousMindedPeopleGeo:EnableGeocoding"] = "false",
                ["ZealousMindedPeopleGeo:Map:Projection"] = "Equirectangular",
                ["ZealousMindedPeopleGeo:Map:CentralMeridian"] = "150",
                ["ZealousMindedPeopleGeo:Map:DefaultLatitude"] = "20",
                ["ZealousMindedPeopleGeo:Map:DefaultLongitude"] = "150",
                ["ZealousMindedPeopleGeo:Map:DefaultZoom"] = "1"
            })
            .Build();

        var options = Resolve<IOptions<ZealousMindedPeopleGeoOptions>>(
            s => s.AddZealousMindedPeopleGeo(configuration)).Value;

        Assert.Equal("maps-key", options.GoogleMapsApiKey);
        Assert.False(options.EnableGeocoding);
        Assert.NotNull(options.Map);
        Assert.Equal(MapProjection.Equirectangular, options.Map.Projection);
        Assert.Equal(150, options.Map.CentralMeridian);
        Assert.Equal(20, options.Map.DefaultLatitude);
        Assert.Equal(150, options.Map.DefaultLongitude);
        Assert.Equal(1, options.Map.DefaultZoom);
    }

    [Fact]
    public async Task ServicesRegisteredByApplication_AreKept()
    {
        var services = CreateServices();
        services.AddScoped<IParticipantRepository, CustomRepository>();
        services.AddSingleton<IGeoDataContainerManager, CustomContainerManager>();

        services.AddZealousMindedPeopleGeo();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        Assert.IsType<CustomRepository>(scope.ServiceProvider.GetRequiredService<IParticipantRepository>());
        Assert.IsType<CustomContainerManager>(scope.ServiceProvider.GetRequiredService<IGeoDataContainerManager>());
    }

    [Fact]
    public void RepeatedRegistration_DoesNotDuplicateServices()
    {
        var services = CreateServices();

        services.AddZealousMindedPeopleGeo();
        services.AddZealousMindedPeopleGeoServices();
        services.AddZealousMindedPeopleGeo(options => options.EnableGeocoding = false);

        Assert.Single(services, d => d.ServiceType == typeof(IParticipantRepository));
        Assert.Single(services, d => d.ServiceType == typeof(IGoogleMapsService));
        Assert.Single(services, d => d.ServiceType == typeof(IGeoDataContainerManager));
        // Настройки из последнего вызова всё равно применяются.
        Assert.False(BuildScope(services).GetRequiredService<IOptions<ZealousMindedPeopleGeoOptions>>().Value.EnableGeocoding);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // IJSRuntime в приложении регистрирует Blazor.
        services.AddScoped<IJSRuntime, UnavailableJsRuntime>();
        return services;
    }

    private static void Register(IServiceCollection services, string registration)
    {
        switch (registration)
        {
            case "AddZealousMindedPeopleGeo()":
                services.AddZealousMindedPeopleGeo();
                break;
            case "AddZealousMindedPeopleGeo(configuration)":
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ZealousMindedPeopleGeo:Map:DefaultZoom"] = "1"
                    })
                    .Build();
                services.AddZealousMindedPeopleGeo(configuration);
                break;
            case "AddZealousMindedPeopleGeo(options => ...)":
                services.AddZealousMindedPeopleGeo(options => options.EnableGeocoding = false);
                break;
            case "AddZealousMindedPeopleGeoServices()":
                services.AddZealousMindedPeopleGeoServices();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(registration), registration, null);
        }
    }

    private static T Resolve<T>(Action<IServiceCollection> register) where T : notnull
    {
        var services = CreateServices();
        register(services);
        return BuildScope(services).GetRequiredService<T>();
    }

    private static IServiceProvider BuildScope(IServiceCollection services)
    {
        return services.BuildServiceProvider().CreateScope().ServiceProvider;
    }

    private sealed class CustomRepository : InMemoryParticipantRepository
    {
        public CustomRepository() : base(Microsoft.Extensions.Logging.Abstractions.NullLogger<InMemoryParticipantRepository>.Instance)
        {
        }
    }

    // Своё хранилище приложения: здесь наследник встроенного, в жизни — база данных или API.
    private sealed class CustomContainerManager : GeoDataContainerManager
    {
        public CustomContainerManager()
            : base(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<GeoDataContainerManager>.Instance,
                Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
        {
        }
    }

    private sealed class UnavailableJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new NotSupportedException();
    }
}
