using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Geocoding;

/// <summary>
/// Интерфейс сервиса для работы с GeoJSON данными
/// </summary>
public interface IGeoJsonService
{
    /// <summary>
    /// Загружает данные стран мира
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Данные стран в формате GeoJSON</returns>
    ValueTask<Models.GeoJsonFeatureCollection> GetCountriesDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Загружает данные городов мира
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Данные городов в формате GeoJSON</returns>
    ValueTask<Models.GeoJsonFeatureCollection> GetCitiesDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Находит страну по координатам
    /// </summary>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Информация о стране или null если не найдена</returns>
    ValueTask<Models.CountryInfo?> GetCountryByCoordinatesAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>
    /// Находит ближайший город к указанным координатам
    /// </summary>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="maxDistance">Максимальное расстояние в км</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Информация о городе или null если не найден</returns>
    ValueTask<Models.CityInfo?> GetNearestCityAsync(double latitude, double longitude, double maxDistance = 50, CancellationToken ct = default);

    /// <summary>
    /// Ищет города по названию
    /// </summary>
    /// <param name="name">Название города</param>
    /// <param name="limit">Максимальное количество результатов</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Список найденных городов</returns>
    ValueTask<IEnumerable<Models.CityInfo>> SearchCitiesAsync(string name, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Очищает кэш данных
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Проверяет доступность сервиса
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат проверки</returns>
    ValueTask<bool> IsAvailableAsync(CancellationToken ct = default);
}