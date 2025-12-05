using System.Text.Json.Serialization;

namespace ZealousMindedPeopleGeo.Models;

#region Core GeoJSON Models

/// <summary>
/// Базовые координаты GeoJSON [longitude, latitude] или [longitude, latitude, elevation]
/// </summary>
public record GeoJsonCoordinates(double Longitude, double Latitude, double? Elevation = null)
{
    /// <summary>
    /// Преобразует координаты в массив для JSON сериализации
    /// </summary>
    public double[] ToArray() => Elevation.HasValue
        ? new[] { Longitude, Latitude, Elevation.Value }
        : new[] { Longitude, Latitude };

    /// <summary>
    /// Создает из массива координат
    /// </summary>
    public static GeoJsonCoordinates FromArray(double[] coords)
    {
        return coords.Length switch
        {
            2 => new GeoJsonCoordinates(coords[0], coords[1]),
            3 => new GeoJsonCoordinates(coords[0], coords[1], coords[2]),
            _ => throw new ArgumentException("Координаты должны содержать 2 или 3 элемента")
        };
    }
}

/// <summary>
/// Геометрия в формате GeoJSON с поддержкой различных типов
/// </summary>
public class GeoJsonGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public object? Coordinates { get; set; }

    /// <summary>
    /// Создает Point геометрию
    /// </summary>
    public static GeoJsonGeometry CreatePoint(GeoJsonCoordinates coords)
    {
        return new GeoJsonGeometry
        {
            Type = "Point",
            Coordinates = coords.ToArray()
        };
    }

    /// <summary>
    /// Создает LineString геометрию
    /// </summary>
    public static GeoJsonGeometry CreateLineString(IEnumerable<GeoJsonCoordinates> coords)
    {
        return new GeoJsonGeometry
        {
            Type = "LineString",
            Coordinates = coords.Select(c => c.ToArray()).ToArray()
        };
    }

    /// <summary>
    /// Создает Polygon геометрию
    /// </summary>
    public static GeoJsonGeometry CreatePolygon(IEnumerable<IEnumerable<GeoJsonCoordinates>> rings)
    {
        return new GeoJsonGeometry
        {
            Type = "Polygon",
            Coordinates = rings.Select(ring => ring.Select(c => c.ToArray()).ToArray()).ToArray()
        };
    }
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

    // Дополнительные свойства для участников
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("social_media")]
    public string? SocialMedia { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("life_goals")]
    public string? LifeGoals { get; set; }

    [JsonPropertyName("skills")]
    public string? Skills { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("registered_at")]
    public DateTime? RegisteredAt { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}

/// <summary>
/// GeoJSON Feature объект
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

    /// <summary>
    /// Создает Feature из Participant с координатами
    /// </summary>
    public static GeoJsonFeature FromParticipant(Participant participant)
    {
        if (!participant.Latitude.HasValue || !participant.Longitude.HasValue)
        {
            throw new ArgumentException("У участника должны быть указаны координаты");
        }

        return new GeoJsonFeature
        {
            Id = participant.Id.ToString(),
            Geometry = GeoJsonGeometry.CreatePoint(
                new GeoJsonCoordinates(participant.Longitude.Value, participant.Latitude.Value)),
            Properties = new GeoJsonProperties
            {
                Name = participant.Name,
                Email = participant.Email,
                Address = participant.Address,
                City = participant.City,
                Country = participant.Country,
                SocialMedia = participant.SocialMedia,
                Message = participant.Message,
                LifeGoals = participant.LifeGoals,
                Skills = participant.Skills,
                RegisteredAt = participant.RegisteredAt,
                Timestamp = participant.RegisteredAt
            }
        };
    }
}

/// <summary>
/// Коллекция GeoJSON Feature объектов
/// </summary>
public class GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "FeatureCollection";

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; } = new();

    [JsonPropertyName("metadata")]
    public GeoJsonMetadata? Metadata { get; set; }

    /// <summary>
    /// Создает FeatureCollection из списка участников
    /// </summary>
    public static GeoJsonFeatureCollection FromParticipants(IEnumerable<Participant> participants)
    {
        var collection = new GeoJsonFeatureCollection
        {
            Features = participants
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                .Select(p => GeoJsonFeature.FromParticipant(p))
                .ToList(),
            Metadata = new GeoJsonMetadata
            {
                GeneratedAt = DateTime.UtcNow,
                TotalFeatures = participants.Count(),
                ValidFeatures = participants.Count(p => p.Latitude.HasValue && p.Longitude.HasValue)
            }
        };

        return collection;
    }

    /// <summary>
    /// Извлекает участников из FeatureCollection
    /// </summary>
    public IEnumerable<Participant> ToParticipants()
    {
        foreach (var feature in Features)
        {
            if (feature.Geometry?.Type == "Point" &&
                feature.Geometry.Coordinates is double[] coords &&
                coords.Length >= 2)
            {
                var coordsObj = GeoJsonCoordinates.FromArray(coords);
                
                var participant = new Participant
                {
                    Id = Guid.TryParse(feature.Id, out var id) ? id : Guid.NewGuid(),
                    Name = feature.Properties?.Name ?? "Unknown",
                    Email = feature.Properties?.Email ?? "",
                    Address = feature.Properties?.Address ?? "",
                    Latitude = coordsObj.Latitude,
                    Longitude = coordsObj.Longitude,
                    City = feature.Properties?.City,
                    Country = feature.Properties?.Country,
                    SocialMedia = feature.Properties?.SocialMedia,
                    Message = feature.Properties?.Message,
                    LifeGoals = feature.Properties?.LifeGoals,
                    Skills = feature.Properties?.Skills,
                    RegisteredAt = feature.Properties?.RegisteredAt ?? feature.Properties?.Timestamp ?? DateTime.UtcNow
                };

                yield return participant;
            }
        }
    }
}

/// <summary>
/// Метаданные для FeatureCollection
/// </summary>
public class GeoJsonMetadata
{
    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("total_features")]
    public int TotalFeatures { get; set; }

    [JsonPropertyName("valid_features")]
    public int ValidFeatures { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("crs")]
    public string? CoordinateReferenceSystem { get; set; } = "EPSG:4326";

    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "1.0.0";
}

#endregion

#region Legacy Support Models

/// <summary>
/// Информация о стране (совместимость с существующим кодом)
/// </summary>
public class CountryInfo
{
    public string? Name { get; set; }
    public string? IsoCode { get; set; }
    public string? NameRu { get; set; }
    public GeoJsonGeometry? Geometry { get; set; }
}

/// <summary>
/// Информация о городе (совместимость с существующим кодом)
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

#endregion

#region Helper Extensions

/// <summary>
/// Методы расширения для работы с GeoJSON
/// </summary>
public static class GeoJsonExtensions
{
    /// <summary>
    /// Преобразует координаты в GeoJSON Point
    /// </summary>
    public static GeoJsonFeature ToGeoJsonPoint(this Participant participant)
    {
        if (!participant.Latitude.HasValue || !participant.Longitude.HasValue)
        {
            throw new ArgumentException("Участник должен иметь координаты для преобразования в GeoJSON");
        }

        return GeoJsonFeature.FromParticipant(participant);
    }

    /// <summary>
    /// Проверяет, является ли Feature допустимой точкой участника
    /// </summary>
    public static bool IsValidParticipantPoint(this GeoJsonFeature feature)
    {
        return feature.Geometry?.Type == "Point" &&
               feature.Geometry.Coordinates is double[] coords &&
               coords.Length >= 2 &&
               coords[0] >= -180 && coords[0] <= 180 && // Longitude
               coords[1] >= -90 && coords[1] <= 90;      // Latitude
    }

    /// <summary>
    /// Валидирует координаты точки
    /// </summary>
    public static bool IsValidCoordinates(double longitude, double latitude)
    {
        return longitude >= -180 && longitude <= 180 &&
               latitude >= -90 && latitude <= 90;
    }
}

#endregion