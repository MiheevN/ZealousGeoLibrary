using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Geocoding;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.Repositories;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// Витрина и страница глобуса падают на первом рендере, если служба не зарегистрирована.
/// </summary>
public class ServiceRegistrationTests
{
    [Fact]
    public void AddZealousMindedPeopleGeoServices_ResolvesShowcaseDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddZealousMindedPeopleGeoServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scoped = scope.ServiceProvider;

        Assert.NotNull(scoped.GetRequiredService<IGeoDataContainerManager>());
        Assert.NotNull(scoped.GetRequiredService<IOptions<ZealousMindedPeopleGeoOptions>>());
        Assert.NotNull(scoped.GetRequiredService<IParticipantRepository>());
        Assert.NotNull(scoped.GetRequiredService<IGeocodingService>());
        Assert.NotNull(scoped.GetRequiredService<GlobeStateService>());
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IGlobeMediator));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IPwaService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IThreeJsGlobeService));
    }
}
