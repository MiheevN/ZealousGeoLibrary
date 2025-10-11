using System.Text.Json.Serialization;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Модели данных для работы с GeoJSON
/// </summary>

/// <summary>
/// Геометрия в формате GeoJSON
/// </summary>
public class GeoJsonGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public object? Coordinates { get; set; }
}

/// <summary>
/// Свойства географического объекта
/// </summary>
public class GeoJsonProperties
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("iso_a3")]
    public string? IsoCode { get; set; }

    [JsonPropertyName("name_ru")]
    public string? NameRu { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("population")]
    public long? Population { get; set; }

    [JsonPropertyName("admin")]
    public string? AdminRegion { get; set; }
}

/// <summary>
/// GeoJSON объект
/// </summary>
public class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Feature";

    [JsonPropertyName("geometry")]
    public GeoJsonGeometry? Geometry { get; set; }

    [JsonPropertyName("properties")]
    public GeoJsonProperties? Properties { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Коллекция GeoJSON объектов
/// </summary>
public class GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "FeatureCollection";

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; } = new();
}

/// <summary>
/// Информация о стране
/// </summary>
public class CountryInfo
{
    public string? Name { get; set; }
    public string? IsoCode { get; set; }
    public string? NameRu { get; set; }
    public GeoJsonGeometry? Geometry { get; set; }
}

/// <summary>
/// Информация о городе
/// </summary>
public class CityInfo
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public long? Population { get; set; }
    public string? AdminRegion { get; set; }
    public double Distance { get; set; } // Расстояние до целевой точки в км
}