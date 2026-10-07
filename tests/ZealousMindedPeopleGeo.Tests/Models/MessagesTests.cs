using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// Сообщения библиотеки: язык берётся из CurrentUICulture, у каждого перевода те же
/// ключи и те же подстановки, что у английского оригинала.
/// </summary>
public sealed partial class MessagesTests
{
    // Языки, на которые переведены Resources/Messages.resx.
    private static readonly string[] Translations = { "ru" };

    private static readonly ResourceManager Resources =
        new("ZealousMindedPeopleGeo.Resources.Messages", typeof(GeoPoint).Assembly);

    private const string MixedGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            { "type": "Feature", "id": "road", "geometry": { "type": "LineString", "coordinates": [[0, 0], [1, 1]] } },
            { "type": "Feature", "id": "nowhere", "geometry": null },
            { "type": "Feature", "geometry": { "type": "Point", "coordinates": ["a", "b"] } },
            { "type": "Feature", "id": "north", "geometry": { "type": "Point", "coordinates": [10, 95.5] } },
            { "type": "Feature", "id": "many", "geometry": { "type": "MultiPoint", "coordinates": [[1, 2], ["x"]] } },
            { "type": "Point", "coordinates": [1, 2] }
          ]
        }
        """;

    [Fact]
    public void EveryTranslation_HasTheSameKeysAndPlaceholdersAsEnglish()
    {
        var english = Entries(CultureInfo.InvariantCulture);
        Assert.NotEmpty(english);

        foreach (var language in Translations)
        {
            var translated = Entries(CultureInfo.GetCultureInfo(language));
            Assert.Equal(english.Keys.Order(), translated.Keys.Order());
            foreach (var (key, text) in english)
            {
                Assert.True(Placeholders(text).SetEquals(Placeholders(translated[key])),
                    $"{language}/{key}: «{translated[key]}» и «{text}» подставляют разное");
                if (Placeholders(text).Count < text.Length / 2 && key != "GeoJsonInvalidPoint")
                {
                    Assert.NotEqual(text, translated[key]);
                }
            }
        }
    }

    [Fact]
    public void GeoJsonSkipReasons_AreInRussianForRussianUi()
    {
        using var russian = new CultureScope("ru-RU", "ru-RU");

        var read = GeoPointGeoJson.Read(MixedGeoJson);

        Assert.Equal(new[] { "many#1" }, read.Points.Select(p => p.Id));
        Assert.Equal(new[]
        {
            "Объект 1 («road»): геометрия LineString — не точка",
            "Объект 2 («nowhere»): нет геометрии",
            "Объект 3: координаты не в виде [долгота, широта]",
            "Объект 4 («north»): Точка «north»: широта 95,5 вне диапазона от -90 до 90",
            "Объект 5 («many»): позиция 2 не в виде [долгота, широта]",
            "Объект 6: это не GeoJSON Feature"
        }, read.Skipped);
    }

    [Fact]
    public void GeoJsonSkipReasons_AreInEnglishForEnglishUi()
    {
        using var english = new CultureScope("en-US", "en-US");

        var read = GeoPointGeoJson.Read(MixedGeoJson);

        Assert.Equal(new[]
        {
            "Feature 1 ('road'): geometry LineString is not a point",
            "Feature 2 ('nowhere'): no geometry",
            "Feature 3: coordinates are not [longitude, latitude]",
            "Feature 4 ('north'): Point 'north': latitude 95.5 is outside -90..90",
            "Feature 5 ('many'): position 2 is not [longitude, latitude]",
            "Feature 6: not a GeoJSON Feature"
        }, read.Skipped);
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("ja")]
    public void UntranslatedLanguage_FallsBackToEnglish(string language)
    {
        using var other = new CultureScope(language);

        Assert.Equal("Point Id is required", new GeoPoint { Id = "" }.Validate());
        Assert.Equal("GeoJSON must be a FeatureCollection or a Feature",
            Assert.Throws<JsonException>(() => GeoPointGeoJson.Read("""{ "type": "Point", "coordinates": [1, 2] }""")).Message);
    }

    [Fact]
    public void PointValidation_IsInRussianForRussianUi()
    {
        using var russian = new CultureScope("ru");

        Assert.Equal("У точки нет Id", new GeoPoint { Id = " " }.Validate());
        Assert.Equal($"Id точки длиннее {GeoPoint.MaxIdLength} символов", new GeoPoint { Id = new string('x', GeoPoint.MaxIdLength + 1) }.Validate());
        Assert.Equal("Точка «p»: долгота 181 вне диапазона от -180 до 180", new GeoPoint { Id = "p", Longitude = 181 }.Validate());
        Assert.Equal($"Точка «p»: поле Url длиннее {GeoPoint.MaxUrlLength} символов",
            new GeoPoint { Id = "p", Url = new string('u', GeoPoint.MaxUrlLength + 1) }.Validate());
        Assert.Equal("В GeoJSON FeatureCollection нет массива \"features\"",
            Assert.Throws<JsonException>(() => GeoPointGeoJson.Read("""{ "type": "FeatureCollection" }""")).Message);
    }

    [Fact]
    public async Task ContainerManagerErrors_AreInRussianForRussianUi()
    {
        using var russian = new CultureScope("ru");
        var manager = new GeoDataContainerManager(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance);

        Assert.Equal("JSON пуст", (await manager.LoadFromJsonAsync("c", " ")).ErrorMessage);
        Assert.Equal("JSON должен быть массивом точек или GeoJSON FeatureCollection", (await manager.LoadFromJsonAsync("c", "{}")).ErrorMessage);
        Assert.StartsWith("Ошибка разбора JSON: ", (await manager.LoadFromJsonAsync("c", "[")).ErrorMessage);
        Assert.Equal("Файл не найден: /nowhere.json", (await manager.LoadFromJsonFileAsync("c", "/nowhere.json")).ErrorMessage);

        var participant = TestData.CreateParticipant("Анна", latitude: null);
        var added = await manager.GetOrCreateContainer("c").AddParticipantAsync(participant);
        Assert.Equal($"У участника «Анна» ({participant.Id}) нет координат", added.ErrorMessage);
    }

    private static Dictionary<string, string> Entries(CultureInfo culture)
    {
        var set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        Assert.True(set is not null, $"нет ресурсов для «{culture.Name}»");
        return set.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value!);
    }

    private static HashSet<string> Placeholders(string text) =>
        PlaceholderPattern().Matches(text).Select(m => m.Value).ToHashSet();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}
