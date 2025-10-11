using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Geocoding;

/// <summary>
/// Интерфейс сервиса геокодирования
/// Позволяет абстрагироваться от конкретного провайдера геокодирования
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Геокодирует адрес в координаты
    /// </summary>
    /// <param name="address">Адрес для геокодирования</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат геокодирования</returns>
    ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default);

    /// <summary>
    /// Выполняет обратное геокодирование - координаты в адрес
    /// </summary>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Адрес по координатам</returns>
    ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>
    /// Валидирует координаты
    /// </summary>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <returns>Результат валидации</returns>
    bool ValidateCoordinates(double latitude, double longitude);

    /// <summary>
    /// Проверяет доступность сервиса геокодирования
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат проверки</returns>
    ValueTask<bool> IsAvailableAsync(CancellationToken ct = default);
}