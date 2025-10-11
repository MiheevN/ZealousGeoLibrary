using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services
{
    /// <summary>
    /// Реализация сервиса для работы с Google Maps API
    /// </summary>
    public class GoogleMapsService : IGoogleMapsService
    {
        private readonly ZealousMindedPeopleGeoOptions _options;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleMapsService> _logger;

        public GoogleMapsService(
            IOptions<ZealousMindedPeopleGeoOptions> options,
            HttpClient httpClient,
            ILogger<GoogleMapsService> logger)
        {
            _options = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<GeocodingResult> GeocodeAddressAsync(string address)
        {
            if (!_options.EnableGeocoding)
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Геокодирование отключено в конфигурации"
                };
            }

            if (string.IsNullOrWhiteSpace(_options.GoogleMapsApiKey))
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Google Maps API ключ не настроен"
                };
            }

            try
            {
                var encodedAddress = Uri.EscapeDataString(address);
                var requestUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_options.GoogleMapsApiKey}";

                var response = await _httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Ошибка HTTP при геокодировании: {StatusCode}", response.StatusCode);

                    return new GeocodingResult
                    {
                        Success = false,
                        ErrorMessage = $"HTTP ошибка: {response.StatusCode}"
                    };
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(jsonContent);

                if (geocodingResponse?.Results == null || geocodingResponse.Results.Length == 0)
                {
                    return new GeocodingResult
                    {
                        Success = false,
                        ErrorMessage = "Адрес не найден"
                    };
                }

                var result = geocodingResponse.Results[0];
                var location = result.Geometry?.Location;

                if (location == null)
                {
                    return new GeocodingResult
                    {
                        Success = false,
                        ErrorMessage = "Не удалось получить координаты"
                    };
                }

                return new GeocodingResult
                {
                    Success = true,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    FormattedAddress = result.FormattedAddress ?? string.Empty
                };
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

        public bool ValidateCoordinates(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        public MapConfiguration GetMapConfiguration()
        {
            return _options.Map ?? new MapConfiguration
            {
                DefaultLatitude = 55.7558, // Москва
                DefaultLongitude = 37.6176, // Москва
                DefaultZoom = 10,
                MapTheme = "default"
            };
        }

        public async Task<bool> CheckConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_options.GoogleMapsApiKey))
            {
                return false;
            }

            try
            {
                // Простая проверка доступности API через геокодирование тестового адреса
                var result = await GeocodeAddressAsync("Москва, Красная площадь");

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки подключения к Google Maps API");
                return false;
            }
        }

        /// <summary>
        /// Классы для десериализации ответа Google Maps API
        /// </summary>
        private class GeocodingResponse
        {
            public GeocodingResultItem[]? Results { get; set; }
            public string? Status { get; set; }
        }

        private class GeocodingResultItem
        {
            public string? FormattedAddress { get; set; }
            public Geometry? Geometry { get; set; }
        }

        private class Geometry
        {
            public LocationInfo? Location { get; set; }
        }

        private class LocationInfo
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}