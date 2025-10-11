using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Geocoding;

/// <summary>
/// Реализация сервиса GeoJSON с загрузкой данных из файлов и внешних источников
/// </summary>
public class FileGeoJsonService : IGeoJsonService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FileGeoJsonService> _logger;

    // Ключи кэша
    private const string CountriesCacheKey = "geojson_countries";
    private const string CitiesCacheKey = "geojson_cities";

    // Время жизни кэша (24 часа)
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    public FileGeoJsonService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<FileGeoJsonService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<Models.GeoJsonFeatureCollection> GetCountriesDataAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CountriesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            _logger.LogInformation("Загрузка данных стран из внешнего источника");

            try
            {
                // Используем открытые данные Natural Earth
                var url = "https://raw.githubusercontent.com/johan/world.geo.json/master/countries.geo.json";
                var response = await _httpClient.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ошибка загрузки данных стран: {response.StatusCode}");
                }

                var data = await response.Content.ReadFromJsonAsync<Models.GeoJsonFeatureCollection>();

                if (data == null)
                {
                    throw new InvalidOperationException("Получены пустые данные стран");
                }

                // Обогащаем данные русскими названиями стран
                await EnrichCountriesWithRussianNames(data);

                _logger.LogInformation("Загружено {Count} стран", data.Features.Count);
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки данных стран");
                throw;
            }
        });
    }

    public async ValueTask<Models.GeoJsonFeatureCollection> GetCitiesDataAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(CitiesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
            _logger.LogInformation("Загрузка данных городов из внешнего источника");

            try
            {
                // Используем открытые данные о городах мира
                var url = "https://raw.githubusercontent.com/lutangar/cities.json/master/cities.json";
                var response = await _httpClient.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Ошибка загрузки данных городов: {response.StatusCode}");
                }

                var cities = await response.Content.ReadFromJsonAsync<List<CityData>>();

                if (cities == null)
                {
                    throw new InvalidOperationException("Получены пустые данные городов");
                }

                // Преобразуем данные в GeoJSON формат
                var geoJsonData = ConvertCitiesToGeoJson(cities);

                _logger.LogInformation("Загружено {Count} городов", geoJsonData.Features.Count);
                return geoJsonData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки данных городов");
                throw;
            }
        });
    }

    public async ValueTask<Models.CountryInfo?> GetCountryByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var countries = await GetCountriesDataAsync(ct);

        // Используем алгоритм point-in-polygon для определения страны
        foreach (var feature in countries.Features)
        {
            if (feature.Geometry?.Type == "Polygon")
            {
                if (IsPointInPolygon(latitude, longitude, feature.Geometry.Coordinates as double[][][]))
                {
                    return new Models.CountryInfo
                    {
                        Name = feature.Properties?.Name,
                        IsoCode = feature.Properties?.IsoCode,
                        NameRu = feature.Properties?.NameRu,
                        Geometry = feature.Geometry
                    };
                }
            }
            else if (feature.Geometry?.Type == "MultiPolygon")
            {
                if (IsPointInMultiPolygon(latitude, longitude, feature.Geometry.Coordinates as double[][][][]))
                {
                    return new Models.CountryInfo
                    {
                        Name = feature.Properties?.Name,
                        IsoCode = feature.Properties?.IsoCode,
                        NameRu = feature.Properties?.NameRu,
                        Geometry = feature.Geometry
                    };
                }
            }
        }

        return null;
    }

    public async ValueTask<Models.CityInfo?> GetNearestCityAsync(double latitude, double longitude, double maxDistance = 50, CancellationToken ct = default)
    {
        var cities = await GetCitiesDataAsync(ct);

        Models.CityInfo? nearestCity = null;
        double minDistance = double.MaxValue;

        foreach (var feature in cities.Features)
        {
            if (feature.Properties?.City == null) continue;

            var cityLat = feature.Properties.City.Contains(",") ?
                double.Parse(feature.Properties.City.Split(',')[0]) : 0;
            var cityLon = feature.Properties.City.Contains(",") ?
                double.Parse(feature.Properties.City.Split(',')[1]) : 0;

            var distance = CalculateDistance(latitude, longitude, cityLat, cityLon);

            if (distance < minDistance && distance <= maxDistance)
            {
                minDistance = distance;
                nearestCity = new Models.CityInfo
                {
                    Name = feature.Properties.Name,
                    Country = feature.Properties.Country,
                    Latitude = cityLat,
                    Longitude = cityLon,
                    Population = feature.Properties.Population,
                    Distance = distance
                };
            }
        }

        return nearestCity;
    }

    public async ValueTask<IEnumerable<Models.CityInfo>> SearchCitiesAsync(string name, int limit = 10, CancellationToken ct = default)
    {
        var cities = await GetCitiesDataAsync(ct);

        return cities.Features
            .Where(f => f.Properties?.Name != null &&
                       f.Properties.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(f => new Models.CityInfo
            {
                Name = f.Properties?.Name,
                Country = f.Properties?.Country,
                Latitude = f.Properties?.City?.Contains(",") == true ?
                    double.Parse(f.Properties.City.Split(',')[0]) : 0,
                Longitude = f.Properties?.City?.Contains(",") == true ?
                    double.Parse(f.Properties.City.Split(',')[1]) : 0,
                Population = f.Properties?.Population
            });
    }

    public void ClearCache()
    {
        _cache.Remove(CountriesCacheKey);
        _cache.Remove(CitiesCacheKey);
        _logger.LogInformation("Кэш GeoJSON данных очищен");
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://httpbin.org/status/200", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Обогащает данные стран русскими названиями
    /// </summary>
    private async Task EnrichCountriesWithRussianNames(Models.GeoJsonFeatureCollection countries)
    {
        // Простая карта соответствий названий стран
        var russianNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Russia", "Россия"},
            {"United States", "Соединенные Штаты"},
            {"China", "Китай"},
            {"Germany", "Германия"},
            {"France", "Франция"},
            {"United Kingdom", "Великобритания"},
            {"Japan", "Япония"},
            {"Italy", "Италия"},
            {"Spain", "Испания"},
            {"Canada", "Канада"},
            {"Australia", "Австралия"},
            {"Brazil", "Бразилия"},
            {"India", "Индия"},
            {"South Korea", "Южная Корея"},
            {"Netherlands", "Нидерланды"},
            {"Belgium", "Бельгия"},
            {"Switzerland", "Швейцария"},
            {"Austria", "Австрия"},
            {"Sweden", "Швеция"},
            {"Norway", "Норвегия"},
            {"Denmark", "Дания"},
            {"Finland", "Финляндия"},
            {"Poland", "Польша"},
            {"Czech Republic", "Чехия"},
            {"Hungary", "Венгрия"},
            {"Romania", "Румыния"},
            {"Bulgaria", "Болгария"},
            {"Greece", "Греция"},
            {"Turkey", "Турция"},
            {"Egypt", "Египет"},
            {"South Africa", "ЮАР"},
            {"Mexico", "Мексика"},
            {"Argentina", "Аргентина"},
            {"Chile", "Чили"},
            {"Peru", "Перу"},
            {"Colombia", "Колумбия"},
            {"Venezuela", "Венесуэла"},
            {"Ecuador", "Эквадор"},
            {"Bolivia", "Боливия"},
            {"Uruguay", "Уругвай"},
            {"Paraguay", "Парагвай"},
            {"Guyana", "Гайана"},
            {"Suriname", "Суринам"},
            {"Belize", "Белиз"},
            {"Guatemala", "Гватемала"},
            {"Honduras", "Гондурас"},
            {"El Salvador", "Сальвадор"},
            {"Nicaragua", "Никарагуа"},
            {"Costa Rica", "Коста-Рика"},
            {"Panama", "Панама"},
            {"Cuba", "Куба"},
            {"Jamaica", "Ямайка"},
            {"Haiti", "Гаити"},
            {"Dominican Republic", "Доминиканская Республика"},
            {"Puerto Rico", "Пуэрто-Рико"},
            {"Trinidad and Tobago", "Тринидад и Тобаго"},
            {"Bahamas", "Багамы"},
            {"Barbados", "Барбадос"},
            {"Saint Lucia", "Сент-Люсия"},
            {"Saint Vincent and the Grenadines", "Сент-Винсент и Гренадины"},
            {"Grenada", "Гренада"},
            {"Antigua and Barbuda", "Антигуа и Барбуда"},
            {"Dominica", "Доминика"},
            {"Saint Kitts and Nevis", "Сент-Китс и Невис"}
        };

        foreach (var feature in countries.Features)
        {
            if (feature.Properties?.Name != null &&
                russianNames.TryGetValue(feature.Properties.Name, out var russianName))
            {
                feature.Properties.NameRu = russianName;
            }
        }
    }

    /// <summary>
    /// Преобразует данные городов в формат GeoJSON
    /// </summary>
    private Models.GeoJsonFeatureCollection ConvertCitiesToGeoJson(List<CityData> cities)
    {
        var features = cities
            .Where(c => c.lat != 0 && c.lng != 0)
            .Select(c => new Models.GeoJsonFeature
            {
                Type = "Feature",
                Geometry = new Models.GeoJsonGeometry
                {
                    Type = "Point",
                    Coordinates = new[] { c.lng, c.lat }
                },
                Properties = new Models.GeoJsonProperties
                {
                    Name = c.name,
                    Country = c.country,
                    City = $"{c.lat},{c.lng}",
                    Population = c.population
                }
            })
            .ToList();

        return new Models.GeoJsonFeatureCollection { Features = features };
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри полигона
    /// </summary>
    private bool IsPointInPolygon(double latitude, double longitude, double[][][]? polygon)
    {
        if (polygon == null || polygon.Length == 0) return false;

        return polygon.Any(ring =>
            IsPointInRing(latitude, longitude, ring));
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри полигона с отверстиями
    /// </summary>
    private bool IsPointInMultiPolygon(double latitude, double longitude, double[][][][]? multiPolygon)
    {
        if (multiPolygon == null || multiPolygon.Length == 0) return false;

        return multiPolygon.Any(polygon =>
            IsPointInPolygon(latitude, longitude, polygon));
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри кольца полигона
    /// </summary>
    private bool IsPointInRing(double latitude, double longitude, double[][] ring)
    {
        int i, j;
        bool inside = false;

        for (i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
        {
            if (((ring[i][1] > latitude) != (ring[j][1] > latitude)) &&
                (longitude < (ring[j][0] - ring[i][0]) * (latitude - ring[i][1]) / (ring[j][1] - ring[i][1]) + ring[i][0]))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Вычисляет расстояние между двумя точками в км
    /// </summary>
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371; // Радиус Земли в км

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Модель данных города из внешнего источника
    /// </summary>
    private class CityData
    {
        public string name { get; set; } = string.Empty;
        public string country { get; set; } = string.Empty;
        public double lat { get; set; }
        public double lng { get; set; }
        public long population { get; set; }
    }
}