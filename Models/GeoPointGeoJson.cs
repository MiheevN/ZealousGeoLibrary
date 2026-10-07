using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Перевод точек (<see cref="GeoPoint"/>) в GeoJSON (RFC 7946) и обратно.
/// </summary>
/// <remarks>
/// <para>
/// Точка — объект <c>Feature</c> с геометрией <c>Point</c>: координаты в порядке
/// [долгота, широта], <see cref="GeoPoint.Id"/> — в <c>id</c>. Поля точки лежат в
/// <c>properties</c> под именами из <see cref="ReservedProperties"/>; заголовок,
/// описание и цвет названы по simplestyle-spec (<c>title</c>, <c>description</c>,
/// <c>marker-color</c>), поэтому geojson.io и подобные редакторы показывают их сразу.
/// Остальные <see cref="GeoPoint.Properties"/> лежат рядом как есть.
/// </para>
/// <para>
/// При чтении <c>name</c> заменяет отсутствующий <c>title</c>, а <c>properties.id</c> —
/// отсутствующий <c>id</c> объекта. <c>MultiPoint</c> даёт по точке на позицию
/// (идентификаторы <c>id#1</c>, <c>id#2</c>, …). Объекты с другой геометрией или без
/// неё, с координатами вне диапазона и с полями длиннее допустимого пропускаются,
/// причина каждого пропуска — в <see cref="GeoJsonReadResult.Skipped"/>. Числа,
/// логические значения, массивы и объекты в свойствах становятся строками с их
/// JSON-записью, а высота (третья координата) отбрасывается.
/// </para>
/// </remarks>
public static class GeoPointGeoJson
{
    /// <summary>Свойство заголовка точки (simplestyle-spec).</summary>
    public const string TitleProperty = "title";

    /// <summary>Свойство, из которого берётся заголовок, если <c>title</c> нет.</summary>
    public const string NameProperty = "name";

    /// <summary>Свойство описания точки (simplestyle-spec).</summary>
    public const string DescriptionProperty = "description";

    /// <summary>Свойство категории точки.</summary>
    public const string CategoryProperty = "category";

    /// <summary>Свойство цвета маркера (simplestyle-spec).</summary>
    public const string ColorProperty = "marker-color";

    /// <summary>Свойство значка маркера.</summary>
    public const string IconProperty = "icon";

    /// <summary>Свойство ссылки точки.</summary>
    public const string UrlProperty = "url";

    /// <summary>Свойство, из которого берётся идентификатор, если у объекта нет <c>id</c>.</summary>
    public const string IdProperty = "id";

    /// <summary>
    /// Имена свойств GeoJSON, под которыми записываются поля точки. Свойство из
    /// <see cref="GeoPoint.Properties"/> с таким же именем записывается, только если
    /// соответствующее поле пустое.
    /// </summary>
    public static IReadOnlyList<string> ReservedProperties { get; } = new[]
    {
        TitleProperty, DescriptionProperty, CategoryProperty, ColorProperty, IconProperty, UrlProperty
    };

    // Кириллица и прочий текст остаются читаемыми, а не превращаются в \uXXXX.
    private static readonly JavaScriptEncoder ReadableEncoder = JavaScriptEncoder.Create(UnicodeRanges.All);

    /// <summary>
    /// Записывает точки как <c>FeatureCollection</c>.
    /// </summary>
    /// <param name="points">Точки; <c>null</c> в коллекции пропускаются.</param>
    /// <param name="indented">Записать с отступами, для чтения человеком.</param>
    public static string Write(IEnumerable<GeoPoint> points, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(points);

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = indented, Encoder = ReadableEncoder }))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var point in points)
            {
                if (point is not null)
                {
                    WriteFeature(writer, point);
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    /// <summary>
    /// Читает точки из <c>FeatureCollection</c> или одного <c>Feature</c>.
    /// </summary>
    /// <exception cref="JsonException">Текст — не JSON или не объект GeoJSON с точками.</exception>
    public static GeoJsonReadResult Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        return Read(document.RootElement);
    }

    /// <summary>
    /// Читает точки из <c>FeatureCollection</c> или одного <c>Feature</c>.
    /// </summary>
    /// <exception cref="JsonException">Элемент — не <c>FeatureCollection</c> и не <c>Feature</c>.</exception>
    public static GeoJsonReadResult Read(JsonElement root)
    {
        var result = new GeoJsonReadResult();
        switch (TypeOf(root))
        {
            case "FeatureCollection":
                if (!root.TryGetProperty("features", out var features) || features.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException("GeoJSON FeatureCollection has no \"features\" array");
                }

                var number = 0;
                foreach (var feature in features.EnumerateArray())
                {
                    ReadFeature(feature, ++number, result);
                }

                break;
            case "Feature":
                ReadFeature(root, 1, result);
                break;
            default:
                throw new JsonException("GeoJSON must be a FeatureCollection or a Feature");
        }

        return result;
    }

    /// <summary>
    /// Похож ли элемент на GeoJSON, который читает <see cref="Read(JsonElement)"/>:
    /// объект с <c>"type": "FeatureCollection"</c> или <c>"Feature"</c>.
    /// </summary>
    public static bool IsGeoJson(JsonElement root) => TypeOf(root) is "FeatureCollection" or "Feature";

    private static string? TypeOf(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("type", out var type)
        && type.ValueKind == JsonValueKind.String
            ? type.GetString()
            : null;

    private static void WriteFeature(Utf8JsonWriter writer, GeoPoint point)
    {
        writer.WriteStartObject();
        writer.WriteString("type", "Feature");
        writer.WriteString("id", point.Id);

        writer.WriteStartObject("geometry");
        writer.WriteString("type", "Point");
        writer.WriteStartArray("coordinates");
        writer.WriteNumberValue(point.Longitude);
        writer.WriteNumberValue(point.Latitude);
        writer.WriteEndArray();
        writer.WriteEndObject();

        writer.WriteStartObject("properties");
        var written = new HashSet<string>(StringComparer.Ordinal);
        void WriteField(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                writer.WriteString(name, value);
                written.Add(name);
            }
        }

        WriteField(TitleProperty, point.Title);
        WriteField(DescriptionProperty, point.Description);
        WriteField(CategoryProperty, point.Category);
        WriteField(ColorProperty, point.Color);
        WriteField(IconProperty, point.Icon);
        WriteField(UrlProperty, point.Url);
        foreach (var (key, value) in point.Properties ?? new Dictionary<string, string>())
        {
            if (key is not null && !written.Contains(key))
            {
                writer.WriteString(key, value);
            }
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void ReadFeature(JsonElement feature, int number, GeoJsonReadResult result)
    {
        if (TypeOf(feature) != "Feature")
        {
            result.Skipped.Add($"Feature {number}: not a GeoJSON Feature");
            return;
        }

        var properties = feature.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object
            ? props
            : (JsonElement?)null;

        var id = Text(feature, "id");
        var idFromProperties = false;
        if (id is null && properties is { } source && Text(source, IdProperty) is { } propertyId)
        {
            id = propertyId;
            idFromProperties = true;
        }

        if (!feature.TryGetProperty("geometry", out var geometry) || geometry.ValueKind != JsonValueKind.Object)
        {
            result.Skipped.Add($"{Describe(number, id)}: no geometry");
            return;
        }

        geometry.TryGetProperty("coordinates", out var coordinates);
        List<(double Longitude, double Latitude)?> positions;
        var geometryType = TypeOf(geometry);
        switch (geometryType)
        {
            case "Point":
                positions = new() { Position(coordinates) };
                break;
            case "MultiPoint" when coordinates.ValueKind == JsonValueKind.Array:
                positions = coordinates.EnumerateArray().Select(Position).ToList();
                break;
            case "MultiPoint":
                positions = new();
                break;
            default:
                result.Skipped.Add($"{Describe(number, id)}: geometry {geometryType ?? "?"} is not a point");
                return;
        }

        if (positions.Count == 0)
        {
            result.Skipped.Add($"{Describe(number, id)}: no coordinates");
            return;
        }

        var template = new GeoPoint();
        if (properties is { } values)
        {
            ReadProperties(values, idFromProperties, template);
        }

        for (var index = 0; index < positions.Count; index++)
        {
            if (positions[index] is not { } position)
            {
                result.Skipped.Add(geometryType == "MultiPoint"
                    ? $"{Describe(number, id)}: position {index + 1} is not [longitude, latitude]"
                    : $"{Describe(number, id)}: coordinates are not [longitude, latitude]");
                continue;
            }

            var point = template.Clone();
            point.Id = id is null
                ? Guid.NewGuid().ToString()
                : geometryType == "MultiPoint" ? $"{id}#{index + 1}" : id;
            point.Longitude = WrapLongitude(position.Longitude);
            point.Latitude = position.Latitude;

            if (point.Validate() is { } error)
            {
                result.Skipped.Add($"{Describe(number, id)}: {error}");
            }
            else
            {
                result.Points.Add(point);
            }
        }
    }

    private static void ReadProperties(JsonElement properties, bool idConsumed, GeoPoint point)
    {
        var hasTitle = Text(properties, TitleProperty) is not null;
        foreach (var property in properties.EnumerateObject())
        {
            var value = ValueText(property.Value);
            if (value is null)
            {
                continue;
            }

            switch (property.Name)
            {
                case TitleProperty:
                    point.Title = value;
                    break;
                case NameProperty when !hasTitle:
                    point.Title = value;
                    break;
                case DescriptionProperty:
                    point.Description = value;
                    break;
                case CategoryProperty:
                    point.Category = value;
                    break;
                case ColorProperty:
                    point.Color = value;
                    break;
                case IconProperty:
                    point.Icon = value;
                    break;
                case UrlProperty:
                    point.Url = value;
                    break;
                case IdProperty when idConsumed:
                    break;
                default:
                    point.Properties[property.Name] = value;
                    break;
            }
        }
    }

    // Позиция [долгота, широта, высота?]; высота отбрасывается.
    private static (double Longitude, double Latitude)? Position(JsonElement position)
    {
        if (position.ValueKind != JsonValueKind.Array || position.GetArrayLength() < 2)
        {
            return null;
        }

        var longitude = position[0];
        var latitude = position[1];
        return longitude.ValueKind == JsonValueKind.Number && latitude.ValueKind == JsonValueKind.Number
               && longitude.TryGetDouble(out var lng) && latitude.TryGetDouble(out var lat)
               && double.IsFinite(lng) && double.IsFinite(lat)
            ? (lng, lat)
            : null;
    }

    // Долгота за ±180° (так пишут объекты, пересекающие линию перемены даты) —
    // то же место на сфере.
    private static double WrapLongitude(double longitude)
    {
        if (longitude is >= -180 and <= 180)
        {
            return longitude;
        }

        var wrapped = (longitude + 180) % 360;
        return (wrapped < 0 ? wrapped + 360 : wrapped) - 180;
    }

    private static string? Text(JsonElement owner, string name) =>
        owner.TryGetProperty(name, out var value) ? ValueText(value) : null;

    // Строка — как есть, остальное — его JSON-запись; null и отсутствие — null.
    private static string? ValueText(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Object or JsonValueKind.Array => Compact(value),
        _ => null
    };

    private static string Compact(JsonElement value)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = ReadableEncoder }))
        {
            value.WriteTo(writer);
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static string Describe(int number, string? id) => id is null ? $"Feature {number}" : $"Feature {number} ('{id}')";
}

/// <summary>
/// Результат чтения GeoJSON: прочитанные точки и причины пропусков.
/// </summary>
public sealed class GeoJsonReadResult
{
    /// <summary>Точки, прошедшие <see cref="GeoPoint.Validate"/>.</summary>
    public List<GeoPoint> Points { get; } = new();

    /// <summary>
    /// Почему пропущены объекты: «Feature 3 ('road'): geometry LineString is not a point».
    /// </summary>
    public List<string> Skipped { get; } = new();

    /// <summary>Число пропущенных объектов и позиций.</summary>
    public int SkippedCount => Skipped.Count;
}
