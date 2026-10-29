namespace ZealousMindedPeopleGeo.Models
{
    /// <summary>
    /// Конфигурация для ZealousMindedPeopleGeo библиотеки
    /// </summary>
    public class ZealousMindedPeopleGeoOptions
    {
        public const string SectionName = "ZealousMindedPeopleGeo";

        /// <summary>
        /// Необязательный ключ Google Maps Platform с включённым Geocoding API. Если он задан,
        /// адреса ищет Google; иначе — бесплатный Nominatim (OpenStreetMap), ключ ему не нужен.
        /// Карта и глобус ключ не используют.
        /// </summary>
        public string GoogleMapsApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Сервер Nominatim для поиска адресов без ключа Google. Пусто — публичный
        /// https://nominatim.openstreetmap.org/ (не больше запроса в секунду на приложение).
        /// Для большой нагрузки укажите свой сервер или платного поставщика с API Nominatim.
        /// </summary>
        public string NominatimUrl { get; set; } = string.Empty;

        /// <summary>
        /// <c>false</c> выключает поиск адресов совсем: форма сообщает, что он недоступен.
        /// </summary>
        public bool EnableGeocoding { get; set; } = true;

        /// <summary>
        /// Значения по умолчанию для всех 2D-карт.
        /// </summary>
        public MapConfiguration? Map { get; set; }
    }

    /// <summary>
    /// Конфигурация карты
    /// </summary>
    public class MapConfiguration
    {
        /// <summary>
        /// Широта начального центра 2D-карты. Не задана — 20: мир целиком, без полярных пустот.
        /// </summary>
        public double? DefaultLatitude { get; set; }

        /// <summary>
        /// Долгота начального центра 2D-карты. Не задана — совпадает с <see cref="CentralMeridian"/>.
        /// </summary>
        public double? DefaultLongitude { get; set; }

        /// <summary>
        /// Начальное приближение 2D-карты: 1 — мир целиком. Не задано — 2.
        /// </summary>
        public int? DefaultZoom { get; set; }

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

}