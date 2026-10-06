using System.Text.Encodings.Web;
using System.Text.Json;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Строка таблицы <c>GeoPoints</c>: точка (<see cref="GeoPoint"/>) с привязкой к
/// именованному контейнеру. Общие поля лежат в своих столбцах, произвольные свойства —
/// одним JSON в <see cref="PropertiesJson"/>, поэтому схема не зависит от того,
/// какие данные хранит приложение.
/// </summary>
public class GeoPointEntity
{
    // Кириллица в БД остаётся читаемой; JSON не попадает в HTML, экранировать её незачем.
    private static readonly JsonSerializerOptions PropertiesJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Идентификатор контейнера (глобуса, карты), которому принадлежит точка</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Идентификатор точки внутри контейнера</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Широта</summary>
    public double Latitude { get; set; }

    /// <summary>Долгота</summary>
    public double Longitude { get; set; }

    /// <summary>Подпись</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Описание</summary>
    public string? Description { get; set; }

    /// <summary>Категория</summary>
    public string? Category { get; set; }

    /// <summary>Цвет маркера</summary>
    public string? Color { get; set; }

    /// <summary>Иконка маркера</summary>
    public string? Icon { get; set; }

    /// <summary>Ссылка</summary>
    public string? Url { get; set; }

    /// <summary>Свойства точки в JSON-объекте; <c>null</c>, если их нет</summary>
    public string? PropertiesJson { get; set; }

    /// <summary>
    /// Строка таблицы для точки
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="point">Точка</param>
    public static GeoPointEntity FromPoint(string containerId, GeoPoint point)
    {
        var entity = new GeoPointEntity { ContainerId = containerId, Id = point.Id };
        entity.UpdateFrom(point);
        return entity;
    }

    /// <summary>
    /// Переносит в строку данные точки (контейнер и идентификатор не меняются)
    /// </summary>
    /// <param name="point">Точка</param>
    public void UpdateFrom(GeoPoint point)
    {
        Latitude = point.Latitude;
        Longitude = point.Longitude;
        Title = point.Title;
        Description = point.Description;
        Category = point.Category;
        Color = point.Color;
        Icon = point.Icon;
        Url = point.Url;
        PropertiesJson = point.Properties is { Count: > 0 }
            ? JsonSerializer.Serialize(point.Properties, PropertiesJsonOptions)
            : null;
    }

    /// <summary>
    /// Точка из строки таблицы
    /// </summary>
    public GeoPoint ToPoint()
    {
        return new GeoPoint
        {
            Id = Id,
            Latitude = Latitude,
            Longitude = Longitude,
            Title = Title,
            Description = Description,
            Category = Category,
            Color = Color,
            Icon = Icon,
            Url = Url,
            Properties = string.IsNullOrEmpty(PropertiesJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(PropertiesJson) ?? new Dictionary<string, string>()
        };
    }
}
