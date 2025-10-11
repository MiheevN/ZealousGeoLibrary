using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping
{
    /// <summary>
    /// Адаптер для использования существующего Google Maps сервиса через интерфейс IMapService
    /// </summary>
    public class GoogleMapsServiceAdapter : IMapService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ILogger<GoogleMapsServiceAdapter> _logger;

        public GoogleMapsServiceAdapter(
            IGoogleMapsService googleMapsService,
            ILogger<GoogleMapsServiceAdapter> logger)
        {
            _googleMapsService = googleMapsService;
            _logger = logger;
        }

        public MapConfiguration GetMapConfiguration()
        {
            return _googleMapsService.GetMapConfiguration();
        }

        public bool ValidateCoordinates(double latitude, double longitude)
        {
            return _googleMapsService.ValidateCoordinates(latitude, longitude);
        }

        public async Task<(double MinLat, double MinLng, double MaxLat, double MaxLng)> GetVisibleBoundsAsync()
        {
            // В текущей реализации Google Maps сервис не предоставляет эту функциональность
            // Можно реализовать через JavaScript интероп

            var config = GetMapConfiguration();
            await Task.CompletedTask;
            return (config.DefaultLatitude - 0.1, config.DefaultLongitude - 0.1,
                   config.DefaultLatitude + 0.1, config.DefaultLongitude + 0.1);
        }

        public async Task CenterMapAsync(double latitude, double longitude, int zoom = 15)
        {
            // Центрирование карты реализовано через JavaScript
            // Здесь можно добавить вызов JavaScript функции через IJSRuntime
            _logger.LogInformation("Центрирование карты на координатах: {Latitude}, {Longitude}", latitude, longitude);
            await Task.CompletedTask;
        }

        public async Task<string> AddMarkerAsync(double latitude, double longitude, string title, MarkerOptions? options = null)
        {
            var markerId = Guid.NewGuid().ToString();
            _logger.LogInformation("Добавлен маркер {MarkerId} на координатах: {Latitude}, {Longitude}", markerId, latitude, longitude);

            // В реальном сценарии здесь был бы вызов JavaScript для добавления маркера на карту
            await Task.CompletedTask;
            return markerId;
        }

        public async Task RemoveMarkerAsync(string markerId)
        {
            _logger.LogInformation("Удален маркер: {MarkerId}", markerId);
            // В реальном сценарии здесь был бы вызов JavaScript для удаления маркера
            await Task.CompletedTask;
        }

        public async Task ClearAllMarkersAsync()
        {
            _logger.LogInformation("Очищены все маркеры с карты");
            // В реальном сценарии здесь был бы вызов JavaScript для очистки маркеров
            await Task.CompletedTask;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                return await _googleMapsService.CheckConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки доступности сервиса карт");
                return false;
            }
        }
    }
}