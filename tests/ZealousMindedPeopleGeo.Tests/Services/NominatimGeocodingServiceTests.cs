using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.Geocoding;

namespace ZealousMindedPeopleGeo.Tests.Services;

/// <summary>
/// Поиск адреса без ключа: NominatimGeocodingService читает ответы Nominatim (координаты
/// строками, display_name, licence), соблюдает лимит запросов и кэширует ответы.
/// </summary>
public class NominatimGeocodingServiceTests
{
    // Сокращённые ответы из https://nominatim.org/release-docs/latest/api/Search/ и /Reverse/.
    private const string SearchAnswer = """
        [{
          "place_id": 132467,
          "licence": "Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright",
          "lat": "52.5219184",
          "lon": "13.4132147",
          "display_name": "Alexanderplatz 1, Mitte, Berlin, 10178, Deutschland"
        }]
        """;

    private const string ReverseAnswer = """
        {
          "licence": "Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright",
          "lat": "55.7539303",
          "lon": "37.620795",
          "display_name": "Красная площадь, Москва, Россия"
        }
        """;

    // Лимит «запрос в секунду» общий для сервера, поэтому у каждого теста свой сервер.
    private readonly string _server = $"https://nominatim-{Guid.NewGuid():N}.test/";
    private readonly StubHandler _handler = new();
    private readonly ZealousMindedPeopleGeoOptions _options = new();

    public NominatimGeocodingServiceTests()
    {
        _options.NominatimUrl = _server;
    }

    [Fact]
    public async Task GeocodeAddressAsync_ReadsTheFirstPlace()
    {
        using var russian = new CultureScope("ru");
        _handler.Answer(SearchAnswer);

        var result = await Service().GeocodeAddressAsync(" Berlin, Alexanderplatz 1 ");

        Assert.True(result.Success);
        Assert.Equal(GeocodingError.None, result.Error);
        Assert.Equal(52.5219184, result.Latitude);
        Assert.Equal(13.4132147, result.Longitude);
        Assert.Equal("Alexanderplatz 1, Mitte, Berlin, 10178, Deutschland", result.FormattedAddress);
        Assert.StartsWith("Data © OpenStreetMap contributors", result.Attribution);

        var request = Assert.Single(_handler.Requests);
        Assert.StartsWith($"{_server}search?", request.AbsoluteUri);
        Assert.Contains("q=Berlin%2C%20Alexanderplatz%201&", request.AbsoluteUri);
        Assert.Contains("format=jsonv2", request.Query);
        Assert.Contains("limit=1", request.Query);
        // Адрес приходит на языке пользователя.
        Assert.Contains("accept-language=ru", request.Query);
    }

    [Fact]
    public async Task GeocodeAddressAsync_NothingFound_IsNotFound()
    {
        _handler.Answer("[]");

        var result = await Service().GeocodeAddressAsync("Nowhere 1");

        Assert.False(result.Success);
        Assert.Equal(GeocodingError.NotFound, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, GeocodingError.Failed)]
    [InlineData(HttpStatusCode.ServiceUnavailable, GeocodingError.Failed)]
    [InlineData(HttpStatusCode.Forbidden, GeocodingError.Unavailable)]
    public async Task GeocodeAddressAsync_HttpError_ReasonFollowsStatus(HttpStatusCode status, GeocodingError expected)
    {
        _handler.Answer("", status);

        var result = await Service().GeocodeAddressAsync("Berlin");

        Assert.False(result.Success);
        Assert.Equal(expected, result.Error);
    }

    [Fact]
    public async Task GeocodeAddressAsync_Disabled_DoesNotCallTheServer()
    {
        _options.EnableGeocoding = false;

        var result = await Service().GeocodeAddressAsync("Berlin");

        Assert.Equal(GeocodingError.Unavailable, result.Error);
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task GeocodeAddressAsync_SameAddressAndLanguage_AsksOnce()
    {
        _handler.Answer(SearchAnswer);
        var cache = NewCache();

        using (new CultureScope("en"))
        {
            await Service(cache).GeocodeAddressAsync("Berlin, Alexanderplatz 1");
            await Service(cache).GeocodeAddressAsync("Berlin, Alexanderplatz 1");
        }
        Assert.Single(_handler.Requests);

        // На другом языке адрес звучит иначе — это другой ответ.
        using (new CultureScope("ru"))
        {
            await Service(cache).GeocodeAddressAsync("Berlin, Alexanderplatz 1");
        }
        Assert.Equal(2, _handler.Requests.Count);
    }

    [Fact]
    public async Task Requests_ToOneServer_AreAtLeastASecondApart()
    {
        _handler.Answer(SearchAnswer);
        var service = Service();

        var watch = Stopwatch.StartNew();
        await service.GeocodeAddressAsync("Berlin");
        await service.GeocodeAddressAsync("Paris");

        Assert.Equal(2, _handler.Requests.Count);
        Assert.True(watch.Elapsed >= TimeSpan.FromMilliseconds(950), $"{watch.Elapsed}");
    }

    [Fact]
    public async Task ReverseGeocodeAsync_ReadsThePlace()
    {
        _handler.Answer(ReverseAnswer);

        var result = await Service().ReverseGeocodeAsync(55.75393, 37.620795);

        Assert.True(result.Success);
        Assert.Equal("Красная площадь, Москва, Россия", result.FormattedAddress);
        Assert.Equal(55.7539303, result.Latitude);
        var request = Assert.Single(_handler.Requests);
        Assert.StartsWith($"{_server}reverse?lat=55.75393&lon=37.620795&format=jsonv2", request.AbsoluteUri);
    }

    [Fact]
    public async Task ReverseGeocodeAsync_InTheOcean_IsNotFound()
    {
        _handler.Answer("""{ "error": "Unable to geocode" }""");

        var result = await Service().ReverseGeocodeAsync(0, -30);

        Assert.False(result.Success);
        Assert.Equal(GeocodingError.NotFound, result.Error);
    }

    [Fact]
    public void EmptyServerSetting_UsesThePublicServer()
    {
        Assert.Equal("https://nominatim.openstreetmap.org/", NominatimGeocodingService.PublicServerUrl);
    }

    private NominatimGeocodingService Service(ICachingService? cache = null) => new(
        new HttpClient(_handler),
        cache ?? NewCache(),
        Options.Create(_options),
        NullLogger<NominatimGeocodingService>.Instance);

    private static CachingService NewCache() => new(
        new MemoryCache(new MemoryCacheOptions()),
        Options.Create(new CachingOptions()),
        NullLogger<CachingService>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "[]";

        public List<Uri> Requests { get; } = new();

        public void Answer(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _body = body;
            _status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
        }
    }
}
