using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// Кэш геокодирования помнит найденные и ненайденные адреса, но не сбои сервиса.
/// </summary>
public class GeocodingCacheTests
{
    private readonly CachingService _cache = new(
        new MemoryCache(new MemoryCacheOptions()),
        Options.Create(new CachingOptions()),
        NullLogger<CachingService>.Instance);

    [Theory]
    [InlineData(GeocodingError.None, 1)]
    [InlineData(GeocodingError.NotFound, 1)]
    [InlineData(GeocodingError.Failed, 2)]
    [InlineData(GeocodingError.Unavailable, 2)]
    public async Task SecondLookup_AsksTheServiceAgainOnlyAfterAFailure(GeocodingError error, int expectedCalls)
    {
        var calls = 0;
        Task<GeocodingResult> Lookup(CancellationToken _)
        {
            calls++;
            return Task.FromResult(new GeocodingResult { Success = error == GeocodingError.None, Error = error });
        }

        await _cache.GetOrCreateGeocodingResultAsync("Berlin", Lookup);
        await _cache.GetOrCreateGeocodingResultAsync("Berlin", Lookup);

        Assert.Equal(expectedCalls, calls);
    }
}
