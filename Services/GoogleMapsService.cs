using System.Text.Json;
using System.Text.Json.Serialization;
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
                    Error = GeocodingError.Unavailable,
                    ErrorMessage = "Geocoding is disabled in the configuration"
                };
            }

            if (string.IsNullOrWhiteSpace(_options.GoogleMapsApiKey))
            {
                return new GeocodingResult
                {
                    Success = false,
                    Error = GeocodingError.Unavailable,
                    ErrorMessage = "Google Maps API key is not configured"
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
                        Error = GeocodingError.Failed,
                        ErrorMessage = $"HTTP error: {response.StatusCode}"
                    };
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var geocodingResponse = JsonSerializer.Deserialize<GeocodingResponse>(jsonContent);

                if (geocodingResponse?.Results == null || geocodingResponse.Results.Length == 0)
                {
                    var status = geocodingResponse?.Status ?? "no status";
                    if (status != "ZERO_RESULTS")
                    {
                        _logger.LogError("Google geocoding answered {Status}: {Message}", status, geocodingResponse?.ErrorMessage);
                    }

                    return new GeocodingResult
                    {
                        Success = false,
                        Error = ErrorForStatus(status),
                        ErrorMessage = $"Google geocoding status: {status}"
                    };
                }

                var result = geocodingResponse.Results[0];
                var location = result.Geometry?.Location;

                if (location == null)
                {
                    return new GeocodingResult
                    {
                        Success = false,
                        Error = GeocodingError.NotFound,
                        ErrorMessage = "The response has no coordinates"
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
                    Error = GeocodingError.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        // https://developers.google.com/maps/documentation/geocoding/requests-geocoding#StatusCodes
        private static GeocodingError ErrorForStatus(string status) => status switch
        {
            "ZERO_RESULTS" => GeocodingError.NotFound,
            // Ключ не подходит или API не включён — исправить может только администратор сайта.
            "REQUEST_DENIED" => GeocodingError.Unavailable,
            _ => GeocodingError.Failed
        };

        public bool ValidateCoordinates(double latitude, double longitude)
        {
            return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
        }

        public MapConfiguration GetMapConfiguration()
        {
            return _options.Map ?? new MapConfiguration();
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
            [JsonPropertyName("results")]
            public GeocodingResultItem[]? Results { get; set; }

            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("error_message")]
            public string? ErrorMessage { get; set; }
        }

        private class GeocodingResultItem
        {
            [JsonPropertyName("formatted_address")]
            public string? FormattedAddress { get; set; }

            [JsonPropertyName("geometry")]
            public Geometry? Geometry { get; set; }
        }

        private class Geometry
        {
            [JsonPropertyName("location")]
            public LocationInfo? Location { get; set; }
        }

        private class LocationInfo
        {
            [JsonPropertyName("lat")]
            public double Latitude { get; set; }

            [JsonPropertyName("lng")]
            public double Longitude { get; set; }
        }
    }
}