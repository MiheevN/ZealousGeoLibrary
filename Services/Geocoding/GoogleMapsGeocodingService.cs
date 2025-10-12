using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;

namespace ZealousMindedPeopleGeo.Services.Geocoding
{
    /// <summary>
    /// Реализация сервиса геокодирования с использованием Google Maps API и кэшированием
    /// </summary>
    public class GoogleMapsGeocodingService : IGeocodingService
    {
        private readonly IGoogleMapsService _googleMapsService;
        private readonly ICachingService _cachingService;
        private readonly ILogger<GoogleMapsGeocodingService> _logger;

        public GoogleMapsGeocodingService(
            IGoogleMapsService googleMapsService,
            ICachingService cachingService,
            ILogger<GoogleMapsGeocodingService> logger)
        {
            _googleMapsService = googleMapsService;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default)
        {
            try
            {
                // Используем кэширование для избежания повторных запросов к API
                return await _cachingService.GetOrCreateGeocodingResultAsync(
                    address,
                    async (cancellationToken) =>
                    {
                        _logger.LogDebug("Geocoding address (not cached): {Address}", address);
                        return await _googleMapsService.GeocodeAddressAsync(address);
                    },
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка геокодирования адреса: {Address}", address);

                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
        {
            // Google Maps API не имеет встроенного обратного геокодирования в бесплатной версии
            // Можно реализовать через прямой HTTP запрос к Geocoding API

            try
            {
                // Заглушка - в будущем можно реализовать через HTTP клиент
                // к https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}

                await Task.CompletedTask;
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Обратное геокодирование не реализовано в текущей версии"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обратного геокодирования координат: {Latitude}, {Longitude}", latitude, longitude);

                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public bool ValidateCoordinates(double latitude, double longitude)
        {
            return _googleMapsService.ValidateCoordinates(latitude, longitude);
        }

        public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                return await _googleMapsService.CheckConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки доступности сервиса геокодирования");
                return false;
            }
        }
    }
}