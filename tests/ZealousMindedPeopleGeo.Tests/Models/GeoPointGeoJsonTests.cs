using System.Text.Json;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// GeoPointGeoJson: запись точек в GeoJSON (RFC 7946) и чтение чужих файлов.
/// </summary>
public class GeoPointGeoJsonTests
{
    [Fact]
    public void Write_PutsLongitudeFirst_AndFieldsUnderSimplestyleNames()
    {
        var berlin = new GeoPoint
        {
            Id = "berlin",
            Latitude = 52.52,
            Longitude = 13.405,
            Title = "Берлин",
            Description = "Офис",
            Category = "Офисы",
            Color = "#3987e5",
            Icon = "🏢",
            Url = "https://example.org/berlin",
            Properties = { ["staff"] = "42" }
        };

        var json = GeoPointGeoJson.Write(new[] { berlin });
        var root = JsonDocument.Parse(json).RootElement;

        Assert.Equal("FeatureCollection", root.GetProperty("type").GetString());
        var feature = Assert.Single(root.GetProperty("features").EnumerateArray());
        Assert.Equal("Feature", feature.GetProperty("type").GetString());
        Assert.Equal("berlin", feature.GetProperty("id").GetString());
        var geometry = feature.GetProperty("geometry");
        Assert.Equal("Point", geometry.GetProperty("type").GetString());
        Assert.Equal(new[] { 13.405, 52.52 }, geometry.GetProperty("coordinates").EnumerateArray().Select(c => c.GetDouble()));

        var properties = feature.GetProperty("properties");
        Assert.Equal(
            new[] { "title", "description", "category", "marker-color", "icon", "url", "staff" },
            properties.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Берлин", properties.GetProperty("title").GetString());
        Assert.Equal("#3987e5", properties.GetProperty("marker-color").GetString());
        Assert.Equal("42", properties.GetProperty("staff").GetString());

        // Текст остаётся читаемым, а не превращается в \uXXXX.
        Assert.Contains("Берлин", json);
    }

    [Fact]
    public void Write_OmitsEmptyFields()
    {
        var json = GeoPointGeoJson.Write(new[] { new GeoPoint { Id = "bare", Latitude = 1, Longitude = 2 } });

        var properties = JsonDocument.Parse(json).RootElement.GetProperty("features")[0].GetProperty("properties");
        Assert.Empty(properties.EnumerateObject());
    }

    [Fact]
    public void WriteThenRead_KeepsEveryPointExactly()
    {
        var points = new[]
        {
            new GeoPoint
            {
                Id = "a",
                Latitude = -33.868820,
                Longitude = 151.209296,
                Title = "Sydney",
                Description = "Line 1\nLine 2 \"quoted\" <b>",
                Category = "Cities",
                Color = "rgb(57, 135, 229)",
                Icon = "S",
                Url = "https://example.org/?a=1&b=2",
                // Свойства с именами, которые GeoJSON-читатели понимают по-своему, тоже сохраняются.
                Properties = { ["id"] = "legacy-7", ["name"] = "Sydney NSW", ["email"] = "a@example.org", ["empty"] = "" }
            },
            new GeoPoint { Id = "b", Latitude = 0.1 + 0.2, Longitude = -179.999999, Title = "Без категории" }
        };

        var read = GeoPointGeoJson.Read(GeoPointGeoJson.Write(points));

        Assert.Empty(read.Skipped);
        Assert.Equal(points.Select(Snapshot), read.Points.Select(Snapshot));
    }

    [Fact]
    public void Read_ForeignFile_MapsCommonPropertiesAndKeepsTheRestAsText()
    {
        const string json = """
            {
              "type": "FeatureCollection",
              "features": [
                {
                  "type": "Feature",
                  "id": 42,
                  "geometry": { "type": "Point", "coordinates": [37.6176, 55.7558, 150] },
                  "properties": {
                    "name": "Москва",
                    "marker-color": "#d95926",
                    "marker-symbol": "star",
                    "population": 13010112,
                    "capital": true,
                    "tags": ["city", "capital"],
                    "address": { "country": "Россия" },
                    "note": null
                  }
                },
                {
                  "type": "Feature",
                  "geometry": { "type": "Point", "coordinates": [30.3351, 59.9343] },
                  "properties": { "id": "spb", "title": "Санкт-Петербург", "name": "Saint Petersburg" }
                }
              ]
            }
            """;

        var read = GeoPointGeoJson.Read(json);

        Assert.Empty(read.Skipped);
        var moscow = read.Points[0];
        Assert.Equal("42", moscow.Id);
        Assert.Equal(55.7558, moscow.Latitude);
        Assert.Equal(37.6176, moscow.Longitude);
        Assert.Equal("Москва", moscow.Title);
        Assert.Equal("#d95926", moscow.Color);
        Assert.Equal("13010112", moscow.Properties["population"]);
        Assert.Equal("true", moscow.Properties["capital"]);
        Assert.Equal("[\"city\",\"capital\"]", moscow.Properties["tags"]);
        Assert.Equal("{\"country\":\"Россия\"}", moscow.Properties["address"]);
        Assert.Equal("star", moscow.Properties["marker-symbol"]);
        Assert.False(moscow.Properties.ContainsKey("note"));
        Assert.False(moscow.Properties.ContainsKey("name"), "name стал заголовком");

        var petersburg = read.Points[1];
        Assert.Equal("spb", petersburg.Id);
        Assert.False(petersburg.Properties.ContainsKey("id"), "properties.id стал идентификатором");
        Assert.Equal("Санкт-Петербург", petersburg.Title);
        Assert.Equal("Saint Petersburg", petersburg.Properties["name"]);
    }

    [Fact]
    public void Read_SkipsWhatIsNotAPoint_AndSaysWhy()
    {
        const string json = """
            {
              "type": "FeatureCollection",
              "features": [
                { "type": "Feature", "id": "road", "geometry": { "type": "LineString", "coordinates": [[0, 0], [1, 1]] }, "properties": {} },
                { "type": "Feature", "id": "park", "geometry": { "type": "Polygon", "coordinates": [[[0, 0], [1, 0], [1, 1], [0, 0]]] } },
                { "type": "Feature", "id": "nowhere", "geometry": null, "properties": { "title": "Nowhere" } },
                { "type": "Feature", "id": "broken", "geometry": { "type": "Point", "coordinates": ["a", "b"] } },
                { "type": "Feature", "id": "north", "geometry": { "type": "Point", "coordinates": [10, 95] } },
                { "type": "Point", "coordinates": [1, 2] },
                { "type": "Feature", "id": "ok", "geometry": { "type": "Point", "coordinates": [1, 2] } }
              ]
            }
            """;

        var read = GeoPointGeoJson.Read(json);

        Assert.Equal("ok", Assert.Single(read.Points).Id);
        Assert.Equal(6, read.SkippedCount);
        Assert.Contains("Feature 1 ('road'): geometry LineString is not a point", read.Skipped);
        Assert.Contains("Feature 2 ('park'): geometry Polygon is not a point", read.Skipped);
        Assert.Contains("Feature 3 ('nowhere'): no geometry", read.Skipped);
        Assert.Contains("Feature 4 ('broken'): coordinates are not [longitude, latitude]", read.Skipped);
        Assert.Contains(read.Skipped, reason => reason.StartsWith("Feature 5 ('north'): ") && reason.Contains("latitude 95"));
        Assert.Contains("Feature 6: not a GeoJSON Feature", read.Skipped);
    }

    [Fact]
    public void Read_MultiPoint_GivesAPointPerPosition()
    {
        const string json = """
            {
              "type": "Feature",
              "id": "offices",
              "geometry": { "type": "MultiPoint", "coordinates": [[2.35, 48.86], ["x"], [-0.13, 51.51]] },
              "properties": { "title": "Офис", "category": "Офисы" }
            }
            """;

        var read = GeoPointGeoJson.Read(json);

        Assert.Equal(new[] { "offices#1", "offices#3" }, read.Points.Select(p => p.Id));
        Assert.All(read.Points, p => Assert.Equal(("Офис", "Офисы"), (p.Title, p.Category)));
        Assert.Equal("Feature 1 ('offices'): position 2 is not [longitude, latitude]", Assert.Single(read.Skipped));

        // Свойства у каждой точки свои: правка одной не меняет другую.
        read.Points[0].Properties["x"] = "1";
        Assert.False(read.Points[1].Properties.ContainsKey("x"));
    }

    [Fact]
    public void Read_WrapsLongitudeBeyondDateLine_AndGivesUniqueIdsWhenMissing()
    {
        const string json = """
            {
              "type": "FeatureCollection",
              "features": [
                { "type": "Feature", "geometry": { "type": "Point", "coordinates": [190, 10] }, "properties": null },
                { "type": "Feature", "geometry": { "type": "Point", "coordinates": [-540, 10] } }
              ]
            }
            """;

        var read = GeoPointGeoJson.Read(json);

        Assert.Equal(new[] { -170.0, -180.0 }, read.Points.Select(p => p.Longitude));
        Assert.Equal(2, read.Points.Select(p => p.Id).Distinct().Count());
        Assert.All(read.Points, p => Assert.True(Guid.TryParse(p.Id, out _)));
    }

    [Fact]
    public void Read_TooLongField_IsSkippedWithValidationMessage()
    {
        var json = GeoPointGeoJson.Write(new[] { new GeoPoint { Id = "long", Latitude = 1, Longitude = 1, Category = new string('x', GeoPoint.MaxCategoryLength + 1) } });

        var read = GeoPointGeoJson.Read(json);

        Assert.Empty(read.Points);
        Assert.Contains("Category is longer than 100 characters", Assert.Single(read.Skipped));
    }

    [Fact]
    public void CustomPropertyNamedLikeAField_IsWrittenOnlyWhenTheFieldIsEmpty()
    {
        var withTitle = new GeoPoint { Id = "a", Title = "Field", Properties = { ["title"] = "Property" } };
        var withoutTitle = new GeoPoint { Id = "b", Properties = { ["title"] = "Property" } };

        var features = JsonDocument.Parse(GeoPointGeoJson.Write(new[] { withTitle, withoutTitle }))
            .RootElement.GetProperty("features");

        Assert.Equal("Field", features[0].GetProperty("properties").GetProperty("title").GetString());
        Assert.Single(features[0].GetProperty("properties").EnumerateObject());
        Assert.Equal("Property", features[1].GetProperty("properties").GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("""[{ "latitude": 1, "longitude": 2 }]""")]
    [InlineData("""{ "type": "Point", "coordinates": [1, 2] }""")]
    [InlineData("""{ "type": "FeatureCollection" }""")]
    [InlineData("""{ "features": [] }""")]
    [InlineData("not json")]
    public void Read_RejectsWhatIsNotFeatureOrFeatureCollection(string json)
    {
        Assert.ThrowsAny<JsonException>(() => GeoPointGeoJson.Read(json));
    }

    [Fact]
    public void IsGeoJson_RecognizesFeatureAndFeatureCollectionOnly()
    {
        static bool Check(string json) => GeoPointGeoJson.IsGeoJson(JsonDocument.Parse(json).RootElement);

        Assert.True(Check("""{ "type": "FeatureCollection", "features": [] }"""));
        Assert.True(Check("""{ "type": "Feature", "geometry": null }"""));
        Assert.False(Check("""{ "type": "Point", "coordinates": [1, 2] }"""));
        Assert.False(Check("""[{ "type": "Feature" }]"""));
        Assert.False(Check("""{ "title": "x" }"""));
    }

    private static string Snapshot(GeoPoint point) =>
        $"{point.Id}|{point.Latitude:R}|{point.Longitude:R}|{point.Title}|{point.Description}|{point.Category}|{point.Color}|{point.Icon}|{point.Url}|"
        + string.Join(";", point.Properties.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
}

/// <summary>
/// Прежняя модель GeoJSON для участников (<see cref="GeoJsonFeatureCollection"/>).
/// </summary>
public class LegacyParticipantGeoJsonTests
{
    [Fact]
    public void FromParticipants_SurvivesJsonRoundTrip()
    {
        var participant = TestData.CreateParticipant("Anna", 59.9343, 30.3351);
        var json = JsonSerializer.Serialize(GeoJsonFeatureCollection.FromParticipants(new[] { participant }));

        var parsed = JsonSerializer.Deserialize<GeoJsonFeatureCollection>(json)!;

        // После разбора координаты — JsonElement, а не double[]: раньше участников не находилось.
        var feature = Assert.Single(parsed.Features);
        Assert.True(feature.IsValidParticipantPoint());
        var read = Assert.Single(parsed.ToParticipants());
        Assert.Equal((participant.Id, participant.Name, 59.9343, 30.3351), (read.Id, read.Name, read.Latitude!.Value, read.Longitude!.Value));
    }

    [Fact]
    public void IsValidParticipantPoint_RejectsOutOfRangeAndNonPoints()
    {
        static GeoJsonFeature Parse(string geometry) =>
            JsonSerializer.Deserialize<GeoJsonFeature>($$"""{ "type": "Feature", "geometry": {{geometry}} }""")!;

        Assert.False(Parse("""{ "type": "Point", "coordinates": [10, 95] }""").IsValidParticipantPoint());
        Assert.False(Parse("""{ "type": "LineString", "coordinates": [[0, 0], [1, 1]] }""").IsValidParticipantPoint());
        Assert.False(Parse("""{ "type": "Point", "coordinates": ["a", "b"] }""").IsValidParticipantPoint());
        Assert.True(Parse("""{ "type": "Point", "coordinates": [10, 50] }""").IsValidParticipantPoint());
    }
}
