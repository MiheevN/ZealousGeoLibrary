using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ZealousMindedPeopleGeo.Services.Synchronization;

public static class GeoDataSynchronizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared notifier used by Blazor components to refresh after data changes.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddGeoDataChangeNotifications(this IServiceCollection services)
    {
        services.TryAddSingleton<IGeoDataChangeNotifier, GeoDataChangeNotifier>();
        return services;
    }
}
