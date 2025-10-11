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