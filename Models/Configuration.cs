using System.ComponentModel.DataAnnotations;

namespace ZealousMindedPeopleGeo.Models
{
    /// <summary>
    /// Конфигурация для ZealousMindedPeopleGeo библиотеки
    /// </summary>
    public class ZealousMindedPeopleGeoOptions
    {
        public const string SectionName = "ZealousMindedPeopleGeo";

        [Required(ErrorMessage = "Google Maps API ключ обязателен")]
        public string GoogleMapsApiKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID Google Sheet обязателен")]
        public string GoogleSheetId { get; set; } = string.Empty;

        public string? GoogleServiceAccountKey { get; set; }

        public bool EnableGeocoding { get; set; } = true;

        public bool EnableParticipantValidation { get; set; } = true;

        public bool EnableRateLimiting { get; set; } = true;

        public int MaxParticipantsPerHour { get; set; } = 100;

        public string? DefaultCulture { get; set; } = "en-US";

        public MapConfiguration? Map { get; set; }
    }

    /// <summary>
    /// Конфигурация карты
    /// </summary>
    public class MapConfiguration
    {
        public double DefaultLatitude { get; set; } = 55.7558; // Москва
        public double DefaultLongitude { get; set; } = 37.6176; // Москва
        public int DefaultZoom { get; set; } = 10;
        public string MapTheme { get; set; } = "default";

        /// <summary>
        /// Проекция 2D-карты. По умолчанию — равновеликая Equal Earth.
        /// </summary>
        public MapProjection Projection { get; set; } = MapProjection.EqualEarth;

        /// <summary>
        /// Центральный меридиан 2D-карты в градусах (−180…180): 0 — Гринвич,
        /// 150 — Тихий океан в центре карты.
        /// </summary>
        public double CentralMeridian { get; set; }

        /// <summary>
        /// Сливать близкие маркеры 2D-карты в группы со счётчиком. По клику группа
        /// приближается, а точки с одинаковыми координатами раскрываются веером.
        /// </summary>
        public bool ClusterPoints { get; set; } = true;

        /// <summary>
        /// Расстояние между центрами маркеров в пикселях, ближе которого они сливаются
        /// в группу (0–200). Маркеры, которые иначе налезли бы друг на друга, сливаются
        /// при любом значении; по умолчанию — только они.
        /// </summary>
        public int ClusterRadius { get; set; } = DefaultClusterRadius;

        /// <summary>
        /// Радиус группировки по умолчанию: маркеры диаметром 18 пикселей с просветом 6.
        /// </summary>
        public const int DefaultClusterRadius = 24;
    }

    /// <summary>
    /// Картографическая проекция 2D-карты сообщества
    /// </summary>
    public enum MapProjection
    {
        /// <summary>
        /// Равновеликая псевдоцилиндрическая проекция Equal Earth (2018): площади
        /// материков сохраняются, карта имеет края и не прокручивается по кругу.
        /// </summary>
        EqualEarth,

        /// <summary>
        /// Равнопромежуточная цилиндрическая проекция (плате-карре): прямоугольная
        /// карта, которая бесконечно прокручивается по горизонтали.
        /// </summary>
        Equirectangular
    }


    /// <summary>
    /// Результат регистрации участника
    /// </summary>
    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Participant? Participant { get; set; }
        public int? SheetRowNumber { get; set; }
    }
}