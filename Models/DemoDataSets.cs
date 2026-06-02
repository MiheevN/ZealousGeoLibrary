namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Готовые тематические наборы демонстрационных данных для витрины возможностей библиотеки.
///
/// Каждый набор формируется заново при каждом обращении (новые экземпляры <see cref="Participant"/>
/// с уникальными идентификаторами), поэтому загрузка одного и того же набора в разные контейнеры
/// или глобусы не приводит к совместному использованию объектов и не влияет на другие инстансы.
/// </summary>
public static class DemoDataSets
{
    /// <summary>
    /// Описание именованного демонстрационного набора данных.
    /// </summary>
    /// <param name="Key">Уникальный ключ набора (используется для имён контейнеров и глобусов).</param>
    /// <param name="Title">Человекочитаемое название набора.</param>
    /// <param name="Description">Краткое описание набора.</param>
    /// <param name="Factory">Фабрика, создающая свежую копию участников набора.</param>
    public sealed record DataSetInfo(string Key, string Title, string Description, Func<List<Participant>> Factory)
    {
        /// <summary>
        /// Создаёт новую независимую копию участников набора.
        /// </summary>
        public List<Participant> Create() => Factory();
    }

    /// <summary>
    /// Все доступные демонстрационные наборы данных.
    /// </summary>
    public static IReadOnlyList<DataSetInfo> All { get; } = new List<DataSetInfo>
    {
        new("russian-cities", "Города России", "Крупные города России для одного глобуса", RussianCities),
        new("world-capitals", "Столицы мира", "Столицы разных континентов для второго глобуса", WorldCapitals),
        new("tech-hubs", "Технологические хабы", "Известные центры технологий для 2D-карты", TechHubs),
    };

    /// <summary>
    /// Возвращает набор данных по ключу или <c>null</c>, если он не найден.
    /// </summary>
    public static DataSetInfo? FindByKey(string key) =>
        All.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Крупные города России.
    /// </summary>
    public static List<Participant> RussianCities() => new()
    {
        Create("Москва", "Москва, Россия", "Россия", "Москва", 55.7558, 37.6173, "Сообщество, события", "Сделать город добрее"),
        Create("Санкт-Петербург", "Санкт-Петербург, Россия", "Россия", "Санкт-Петербург", 59.9343, 30.3351, "Культура, искусство", "Объединять людей"),
        Create("Новосибирск", "Новосибирск, Россия", "Россия", "Новосибирск", 55.0084, 82.9357, "Наука, образование", "Развивать науку"),
        Create("Екатеринбург", "Екатеринбург, Россия", "Россия", "Екатеринбург", 56.8389, 60.6057, "Промышленность", "Строить будущее"),
        Create("Казань", "Казань, Россия", "Россия", "Казань", 55.7961, 49.1064, "IT, спорт", "Соединять культуры"),
        Create("Краснодар", "Краснодар, Россия", "Россия", "Краснодар", 45.0355, 38.9753, "Сельское хозяйство", "Растить сообщество"),
        Create("Владивосток", "Владивосток, Россия", "Россия", "Владивосток", 43.1198, 131.8869, "Логистика, море", "Открывать горизонты"),
    };

    /// <summary>
    /// Столицы мира с разных континентов.
    /// </summary>
    public static List<Participant> WorldCapitals() => new()
    {
        Create("London", "London, United Kingdom", "United Kingdom", "London", 51.5074, -0.1278, "Finance, culture", "Connect communities"),
        Create("Paris", "Paris, France", "France", "Paris", 48.8566, 2.3522, "Art, design", "Inspire kindness"),
        Create("Tokyo", "Tokyo, Japan", "Japan", "Tokyo", 35.6762, 139.6503, "Robotics, design", "Build harmony"),
        Create("Washington", "Washington, USA", "USA", "Washington", 38.9072, -77.0369, "Policy, research", "Bring people together"),
        Create("Canberra", "Canberra, Australia", "Australia", "Canberra", -35.2809, 149.1300, "Education", "Grow community"),
        Create("Cairo", "Cairo, Egypt", "Egypt", "Cairo", 30.0444, 31.2357, "History, trade", "Share knowledge"),
        Create("Brasília", "Brasília, Brazil", "Brazil", "Brasília", -15.7939, -47.8828, "Architecture", "Unite continents"),
    };

    /// <summary>
    /// Известные мировые технологические центры.
    /// </summary>
    public static List<Participant> TechHubs() => new()
    {
        Create("San Francisco", "San Francisco, USA", "USA", "San Francisco", 37.7749, -122.4194, "Startups, AI", "Build for good"),
        Create("Seattle", "Seattle, USA", "USA", "Seattle", 47.6062, -122.3321, "Cloud, software", "Empower makers"),
        Create("Bangalore", "Bangalore, India", "India", "Bangalore", 12.9716, 77.5946, "Software, services", "Educate engineers"),
        Create("Berlin", "Berlin, Germany", "Germany", "Berlin", 52.5200, 13.4050, "Startups, open source", "Foster collaboration"),
        Create("Tel Aviv", "Tel Aviv, Israel", "Israel", "Tel Aviv", 32.0853, 34.7818, "Cybersecurity", "Solve hard problems"),
        Create("Singapore", "Singapore", "Singapore", "Singapore", 1.3521, 103.8198, "Fintech, logistics", "Connect Asia"),
    };

    private static Participant Create(
        string name,
        string address,
        string country,
        string city,
        double latitude,
        double longitude,
        string skills,
        string lifeGoals) => new()
    {
        Name = name,
        Address = address,
        Email = $"{Slug(name)}@example.com",
        Location = address,
        City = city,
        Country = country,
        Latitude = latitude,
        Longitude = longitude,
        Skills = skills,
        LifeGoals = lifeGoals,
        Message = $"Привет из {city}!",
        RegisteredAt = DateTime.UtcNow,
    };

    private static string Slug(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) && c < 128)
            .ToArray();

        var slug = new string(chars);
        return string.IsNullOrEmpty(slug) ? "demo" : slug;
    }
}
