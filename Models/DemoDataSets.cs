namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Готовые тематические наборы демонстрационных точек для витрины возможностей библиотеки.
///
/// Это обычные точки (<see cref="GeoPoint"/>), а не люди: у каждой есть категория для
/// цвета и легенды, город и страна в свойствах. Каждый вызов возвращает новые экземпляры,
/// поэтому изменение точек одного глобуса или карты не затрагивает другие.
/// Идентификаторы постоянные (<c>moscow</c>, <c>london</c>): один и тот же набор можно
/// загрузить в разные контейнеры, Id внутри контейнера уникальны.
/// </summary>
public static class DemoDataSets
{
    /// <summary>
    /// Описание именованного демонстрационного набора данных.
    /// </summary>
    /// <param name="Key">Уникальный ключ набора (используется для имён контейнеров и глобусов).</param>
    /// <param name="Title">Человекочитаемое название набора.</param>
    /// <param name="Description">Краткое описание набора.</param>
    /// <param name="Factory">Фабрика, создающая свежую копию точек набора.</param>
    public sealed record DataSetInfo(string Key, string Title, string Description, Func<List<GeoPoint>> Factory)
    {
        /// <summary>
        /// Создаёт новую независимую копию точек набора.
        /// </summary>
        public List<GeoPoint> Create() => Factory();
    }

    /// <summary>
    /// Все доступные демонстрационные наборы данных.
    /// </summary>
    public static IReadOnlyList<DataSetInfo> All { get; } = new List<DataSetInfo>
    {
        new("russian-cities", "Города России", "Крупные города России по частям страны", RussianCities),
        new("world-capitals", "Столицы мира", "Столицы пяти континентов", WorldCapitals),
        new("tech-hubs", "Технологические хабы", "Центры технологий по регионам мира", TechHubs),
    };

    /// <summary>
    /// Возвращает набор данных по ключу или <c>null</c>, если он не найден.
    /// </summary>
    public static DataSetInfo? FindByKey(string key) =>
        All.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Крупные города России; категория — часть страны.
    /// </summary>
    public static List<GeoPoint> RussianCities() => new()
    {
        Create("moscow", "Москва", "Европейская часть", "Россия", 55.7558, 37.6173, "Сообщество, события"),
        Create("saint-petersburg", "Санкт-Петербург", "Европейская часть", "Россия", 59.9343, 30.3351, "Культура, искусство"),
        Create("novosibirsk", "Новосибирск", "Урал и Сибирь", "Россия", 55.0084, 82.9357, "Наука, образование"),
        Create("yekaterinburg", "Екатеринбург", "Урал и Сибирь", "Россия", 56.8389, 60.6057, "Промышленность"),
        Create("kazan", "Казань", "Европейская часть", "Россия", 55.7961, 49.1064, "IT, спорт"),
        Create("krasnodar", "Краснодар", "Европейская часть", "Россия", 45.0355, 38.9753, "Сельское хозяйство"),
        Create("vladivostok", "Владивосток", "Дальний Восток", "Россия", 43.1198, 131.8869, "Логистика, море"),
    };

    /// <summary>
    /// Столицы мира; категория — континент. Континентов пять, автоматических цветов
    /// три: Африка и Океания получают серый цвет «прочих» (см. <see cref="GeoPointPalette"/>).
    /// </summary>
    public static List<GeoPoint> WorldCapitals() => new()
    {
        Create("london", "London", "Europe", "United Kingdom", 51.5074, -0.1278, "Finance, culture"),
        Create("paris", "Paris", "Europe", "France", 48.8566, 2.3522, "Art, design"),
        Create("tokyo", "Tokyo", "Asia", "Japan", 35.6762, 139.6503, "Robotics, design"),
        Create("washington", "Washington", "Americas", "USA", 38.9072, -77.0369, "Policy, research"),
        Create("brasilia", "Brasília", "Americas", "Brazil", -15.7939, -47.8828, "Architecture"),
        Create("cairo", "Cairo", "Africa", "Egypt", 30.0444, 31.2357, "History, trade"),
        Create("canberra", "Canberra", "Oceania", "Australia", -35.2809, 149.1300, "Education"),
    };

    /// <summary>
    /// Известные мировые технологические центры; категория — регион.
    /// </summary>
    public static List<GeoPoint> TechHubs() => new()
    {
        Create("san-francisco", "San Francisco", "North America", "USA", 37.7749, -122.4194, "Startups, AI"),
        Create("seattle", "Seattle", "North America", "USA", 47.6062, -122.3321, "Cloud, software"),
        Create("berlin", "Berlin", "Europe & Middle East", "Germany", 52.5200, 13.4050, "Startups, open source"),
        Create("tel-aviv", "Tel Aviv", "Europe & Middle East", "Israel", 32.0853, 34.7818, "Cybersecurity"),
        Create("bangalore", "Bangalore", "Asia", "India", 12.9716, 77.5946, "Software, services"),
        Create("singapore", "Singapore", "Asia", "Singapore", 1.3521, 103.8198, "Fintech, logistics"),
    };

    private static GeoPoint Create(
        string id,
        string city,
        string category,
        string country,
        double latitude,
        double longitude,
        string focus) => new()
    {
        Id = id,
        Title = city,
        Category = category,
        Latitude = latitude,
        Longitude = longitude,
        Description = focus,
        Properties =
        {
            [ParticipantPointProperties.City] = city,
            [ParticipantPointProperties.Country] = country
        }
    };
}
