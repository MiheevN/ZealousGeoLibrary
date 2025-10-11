using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с Google Maps API
    /// </summary>
    public interface IGoogleMapsService
    {
        /// <summary>
        /// Геокодирует адрес в координаты
        /// </summary>
        /// <param name="address">Адрес для геокодирования</param>
        /// <returns>Результат геокодирования</returns>
        Task<GeocodingResult> GeocodeAddressAsync(string address);

        /// <summary>
        /// Валидирует координаты
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>Результат валидации</returns>
        bool ValidateCoordinates(double latitude, double longitude);

        /// <summary>
        /// Получает конфигурацию карты
        /// </summary>
        /// <returns>Конфигурация карты</returns>
        MapConfiguration GetMapConfiguration();

        /// <summary>
        /// Проверяет доступность Google Maps API
        /// </summary>
        /// <returns>Результат проверки</returns>
        Task<bool> CheckConnectionAsync();
    }
}