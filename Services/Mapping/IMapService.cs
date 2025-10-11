using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping
{
    /// <summary>
    /// Интерфейс сервиса для работы с картами
    /// Позволяет абстрагироваться от конкретного провайдера карт (Google Maps, Leaflet, Yandex Maps и т.д.)
    /// </summary>
    public interface IMapService
    {
        /// <summary>
        /// Получает конфигурацию карты
        /// </summary>
        /// <returns>Конфигурация карты</returns>
        MapConfiguration GetMapConfiguration();

        /// <summary>
        /// Валидирует координаты
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <returns>Результат валидации</returns>
        bool ValidateCoordinates(double latitude, double longitude);

        /// <summary>
        /// Получает границы видимой области карты
        /// </summary>
        /// <returns>Границы в формате (мин. широта, мин. долгота, макс. широта, макс. долгота)</returns>
        Task<(double MinLat, double MinLng, double MaxLat, double MaxLng)> GetVisibleBoundsAsync();

        /// <summary>
        /// Центрирует карту на указанных координатах
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <param name="zoom">Уровень масштабирования</param>
        Task CenterMapAsync(double latitude, double longitude, int zoom = 15);

        /// <summary>
        /// Добавляет маркер на карту
        /// </summary>
        /// <param name="latitude">Широта</param>
        /// <param name="longitude">Долгота</param>
        /// <param name="title">Заголовок маркера</param>
        /// <param name="options">Дополнительные опции маркера</param>
        /// <returns>ID маркера</returns>
        Task<string> AddMarkerAsync(double latitude, double longitude, string title, MarkerOptions? options = null);

        /// <summary>
        /// Удаляет маркер с карты
        /// </summary>
        /// <param name="markerId">ID маркера</param>
        Task RemoveMarkerAsync(string markerId);

        /// <summary>
        /// Очищает все маркеры с карты
        /// </summary>
        Task ClearAllMarkersAsync();

        /// <summary>
        /// Проверяет доступность сервиса карт
        /// </summary>
        /// <returns>Результат проверки</returns>
        Task<bool> IsAvailableAsync();
    }

    /// <summary>
    /// Опции маркера карты
    /// </summary>
    public class MarkerOptions
    {
        public string? IconUrl { get; set; }
        public string? Color { get; set; }
        public string? CustomHtml { get; set; }
        public bool Draggable { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}