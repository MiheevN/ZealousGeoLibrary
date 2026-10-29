using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Geocoding;

/// <summary>
/// Геокодирование через Nominatim (OpenStreetMap): без ключа и без оплаты. Используется,
/// когда ключ Google не задан. Публичный сервер разрешает не больше одного запроса в секунду
/// с понятным User-Agent (https://operations.osmfoundation.org/policies/nominatim/), поэтому
/// запросы к одному серверу идут не чаще раза в секунду, а ответы кэшируются. Для большой
/// нагрузки укажите свой сервер в <see cref="ZealousMindedPeopleGeoOptions.NominatimUrl"/>.
/// </summary>
public class NominatimGeocodingService : IGeocodingService
{
    /// <summary>Адрес публичного сервера Nominatim.</summary>
    public const string PublicServerUrl = "https://nominatim.openstreetmap.org/";

    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);

    // Очередь запросов к каждому серверу общая для всего приложения: лимит считается на приложение.
    private static readonly ConcurrentDictionary<string, RequestGate> Gates = new();

    private readonly HttpClient _httpClient;
    private readonly ICachingService _cachingService;
    private readonly ZealousMindedPeopleGeoOptions _options;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        HttpClient httpClient,
        ICachingService cachingService,
        IOptions<ZealousMindedPeopleGeoOptions> options,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _cachingService = cachingService;
        _options = options.Value;
        _logger = logger;
    }

    private Uri ServerUrl
    {
        get
        {
            var url = string.IsNullOrWhiteSpace(_options.NominatimUrl) ? PublicServerUrl : _options.NominatimUrl.Trim();
            return new Uri(url.EndsWith('/') ? url : url + "/");
        }
    }

    // Адрес приходит на языке пользователя; при другом языке это другой ответ.
    private static string Language => CultureInfo.CurrentUICulture.Name is { Length: > 0 } name ? name : "en";

    /// <inheritdoc />
    public async ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default)
    {
        if (!_options.EnableGeocoding)
        {
            return Unavailable("Geocoding is disabled in the configuration");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            return new GeocodingResult { Success = false, Error = GeocodingError.NotFound, ErrorMessage = "Address is empty" };
        }

        var language = Language;
        var query = $"search?q={Uri.EscapeDataString(address.Trim())}&format=jsonv2&limit=1&accept-language={Uri.EscapeDataString(language)}";
        return await _cachingService.GetOrCreateGeocodingResultAsync(
            address.Trim(),
            token => RequestAsync(query, token),
            ct,
            scope: $"nominatim:{ServerUrl.Host}:{language}");
    }

    /// <inheritdoc />
    public async ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        if (!_options.EnableGeocoding)
        {
            return Unavailable("Geocoding is disabled in the configuration");
        }

        if (!ValidateCoordinates(latitude, longitude))
        {
            return new GeocodingResult { Success = false, Error = GeocodingError.NotFound, ErrorMessage = "Coordinates are out of range" };
        }

        var language = Language;
        var lat = latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lon = longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var query = $"reverse?lat={lat}&lon={lon}&format=jsonv2&accept-language={Uri.EscapeDataString(language)}";
        return await _cachingService.GetOrCreateGeocodingResultAsync(
            $"{lat},{lon}",
            token => RequestAsync(query, token),
            ct,
            scope: $"nominatim-reverse:{ServerUrl.Host}:{language}");
    }

    /// <inheritdoc />
    public bool ValidateCoordinates(double latitude, double longitude) =>
        latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;

    /// <inheritdoc />
    /// <remarks>Сервер не опрашивается, чтобы не тратить лимит запросов.</remarks>
    public ValueTask<bool> IsAvailableAsync(CancellationToken ct = default) => ValueTask.FromResult(_options.EnableGeocoding);

    private async Task<GeocodingResult> RequestAsync(string query, CancellationToken ct)
    {
        var server = ServerUrl;
        try
        {
            await Gates.GetOrAdd(server.GetLeftPart(UriPartial.Authority), _ => new RequestGate()).WaitTurnAsync(ct);

            using var response = await _httpClient.GetAsync(new Uri(server, query), ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Nominatim answered HTTP {StatusCode} for {Query}", (int)response.StatusCode, query);
                // 403 — сервер заблокировал приложение (нарушение правил использования): чинит администратор.
                return response.StatusCode == HttpStatusCode.Forbidden
                    ? Unavailable($"HTTP error: {response.StatusCode}")
                    : Failed($"HTTP error: {response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return Read(json.RootElement);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nominatim request failed: {Query}", query);
            return Failed(ex.Message);
        }
    }

    // search отвечает массивом мест, reverse — одним местом или {"error": "..."}.
    private static GeocodingResult Read(JsonElement root)
    {
        var place = root.ValueKind switch
        {
            JsonValueKind.Array when root.GetArrayLength() > 0 => root[0],
            JsonValueKind.Object when !root.TryGetProperty("error", out _) => root,
            _ => default
        };

        if (place.ValueKind != JsonValueKind.Object
            || !TryReadCoordinate(place, "lat", out var latitude)
            || !TryReadCoordinate(place, "lon", out var longitude))
        {
            return new GeocodingResult { Success = false, Error = GeocodingError.NotFound, ErrorMessage = "Address not found" };
        }

        return new GeocodingResult
        {
            Success = true,
            Latitude = latitude,
            Longitude = longitude,
            FormattedAddress = Text(place, "display_name") ?? string.Empty,
            Attribution = Text(place, "licence")
        };
    }

    // Nominatim отдаёт координаты строками: "52.5219".
    private static bool TryReadCoordinate(JsonElement place, string name, out double value)
    {
        value = 0;
        return place.TryGetProperty(name, out var property) && property.ValueKind switch
        {
            JsonValueKind.String => double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value),
            JsonValueKind.Number => property.TryGetDouble(out value),
            _ => false
        };
    }

    private static string? Text(JsonElement place, string name) =>
        place.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static GeocodingResult Unavailable(string message) =>
        new() { Success = false, Error = GeocodingError.Unavailable, ErrorMessage = message };

    private static GeocodingResult Failed(string message) =>
        new() { Success = false, Error = GeocodingError.Failed, ErrorMessage = message };

    /// <summary>Пропускает к серверу не больше одного запроса в секунду.</summary>
    private sealed class RequestGate
    {
        private readonly SemaphoreSlim _lock = new(1, 1);
        private DateTime _nextAllowedUtc = DateTime.MinValue;

        public async Task WaitTurnAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct);
            try
            {
                var wait = _nextAllowedUtc - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    await Task.Delay(wait, ct);
                }
                _nextAllowedUtc = DateTime.UtcNow + MinimumInterval;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
