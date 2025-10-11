using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Geocoding;

/// <summary>
/// Реализация сервиса геокодирования с использованием Nominatim API (OpenStreetMap)
/// Бесплатный сервис, не требующий API ключа
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Настройка базового адреса Nominatim API
        _httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ZealousMindedPeopleGeo/1.0");
    }

    public async ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Адрес не может быть пустым"
                };
            }

            // Формируем URL для прямого геокодирования
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"search?format=json&q={encodedAddress}&limit=1&addressdetails=1&extratags=1";

            _logger.LogInformation("Геокодирование адреса: {Address}", address);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ошибка HTTP при геокодировании: {StatusCode}", response.StatusCode);
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка HTTP: {response.StatusCode}"
                };
            }

            var results = await response.Content.ReadFromJsonAsync<NominatimResult[]>();

            if (results == null || results.Length == 0)
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Адрес не найден"
                };
            }

            var result = results[0];

            return new GeocodingResult
            {
                Success = true,
                Latitude = double.Parse(result.lat),
                Longitude = double.Parse(result.lon),
                FormattedAddress = result.display_name
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Операция геокодирования была отменена");
            return new GeocodingResult
            {
                Success = false,
                ErrorMessage = "Операция отменена"
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

    public async ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            if (!ValidateCoordinates(latitude, longitude))
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Неверные координаты"
                };
            }

            // Формируем URL для обратного геокодирования
            var url = $"reverse?format=json&lat={latitude}&lon={longitude}&zoom=18&addressdetails=1";

            _logger.LogInformation("Обратное геокодирование координат: {Latitude}, {Longitude}", latitude, longitude);

            var response = await _httpClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ошибка HTTP при обратном геокодировании: {StatusCode}", response.StatusCode);
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка HTTP: {response.StatusCode}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<NominatimReverseResult>();

            if (result == null)
            {
                return new GeocodingResult
                {
                    Success = false,
                    ErrorMessage = "Не удалось получить результат"
                };
            }

            return new GeocodingResult
            {
                Success = true,
                Latitude = latitude,
                Longitude = longitude,
                FormattedAddress = result.display_name
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Операция обратного геокодирования была отменена");
            return new GeocodingResult
            {
                Success = false,
                ErrorMessage = "Операция отменена"
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
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }

    public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            // Проверяем доступность сервиса простым запросом к статусу
            var response = await _httpClient.GetAsync("search?format=json&q=test&limit=1", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка проверки доступности сервиса геокодирования");
            return false;
        }
    }

    /// <summary>
    /// Модель ответа Nominatim API для прямого геокодирования
    /// </summary>
    private class NominatimResult
    {
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
        public string display_name { get; set; } = string.Empty;
        public string @class { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public float importance { get; set; }
    }

    /// <summary>
    /// Модель ответа Nominatim API для обратного геокодирования
    /// </summary>
    private class NominatimReverseResult
    {
        public string display_name { get; set; } = string.Empty;
        public string lat { get; set; } = string.Empty;
        public string lon { get; set; } = string.Empty;
    }
}