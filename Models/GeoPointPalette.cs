using System.Text.RegularExpressions;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Категория точек на карте или глобусе: её цвет и сколько в ней точек.
/// </summary>
/// <param name="Name">Название категории; пустая строка — точки без категории.</param>
/// <param name="Color">Цвет маркеров категории (CSS).</param>
/// <param name="Count">Сколько точек в категории.</param>
/// <param name="IsOther">
/// Цвет категории — общий серый «прочих»: автоматических цветов на неё не хватило
/// или у точек нет категории.
/// </param>
public sealed record GeoPointCategory(string Name, string Color, int Count, bool IsOther);

/// <summary>
/// Цвета маркеров по категориям, одни и те же для карты и глобуса.
/// </summary>
/// <remarks>
/// Порядок выбора цвета точки: её собственный <see cref="GeoPoint.Color"/>, затем цвет
/// категории из словаря приложения, затем автоматический цвет категории. Автоматических
/// цветов три (<see cref="CategoryColors"/>): на карте любые две точки могут оказаться
/// рядом, а различимыми для людей с любым цветовосприятием на тёмном фоне карты
/// остаются только три цвета одновременно. Их получают первые три категории в порядке
/// появления точек, поэтому новая категория или фильтр не перекрашивают прежние.
/// Остальные категории и точки без категории получают серый <see cref="OtherColor"/>;
/// различить их помогают подписи в легенде и в подсказке. Если категорий нет ни у одной
/// точки, все маркеры получают фирменный <see cref="DefaultColor"/>.
/// </remarks>
public sealed class GeoPointPalette
{
    /// <summary>Цвет маркеров, когда категорий нет.</summary>
    public const string DefaultColor = "#24dce7";

    /// <summary>Цвет категорий, на которые не хватило автоматических цветов, и точек без категории.</summary>
    public const string OtherColor = "#8a94a6";

    /// <summary>
    /// Автоматические цвета категорий по порядку: синий, оранжевый, бирюзовый. Различимы
    /// попарно при любом цветовосприятии и контрастны (не ниже 3:1) к океану и суше карты.
    /// </summary>
    public static IReadOnlyList<string> CategoryColors { get; } = new[] { "#3987e5", "#d95926", "#199e70" };

    private readonly Dictionary<string, GeoPointCategory> _categories;

    private GeoPointPalette(IReadOnlyList<GeoPointCategory> categories)
    {
        Categories = categories;
        _categories = categories.ToDictionary(c => c.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Категории в порядке появления; точки без категории — последней записью с пустым
    /// именем. Пусто, если категорий нет ни у одной точки.
    /// </summary>
    public IReadOnlyList<GeoPointCategory> Categories { get; }

    /// <summary>
    /// Есть ли у точек категории (и, значит, смысл показывать легенду).
    /// </summary>
    public bool HasCategories => Categories.Count > 0;

    /// <summary>
    /// Строит палитру для набора точек.
    /// </summary>
    /// <param name="points">Точки, которые будут показаны (до фильтрации).</param>
    /// <param name="categoryColors">Цвета категорий от приложения; важнее автоматических.</param>
    public static GeoPointPalette For(IEnumerable<GeoPoint> points, IReadOnlyDictionary<string, string>? categoryColors = null)
    {
        ArgumentNullException.ThrowIfNull(points);

        var order = new List<string>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var uncategorized = 0;

        foreach (var point in points)
        {
            if (point is null)
            {
                continue;
            }

            var name = CategoryOf(point);
            if (name.Length == 0)
            {
                uncategorized++;
                continue;
            }

            if (counts.TryGetValue(name, out var count))
            {
                counts[name] = count + 1;
            }
            else
            {
                counts[name] = 1;
                order.Add(name);
            }
        }

        if (order.Count == 0)
        {
            return new GeoPointPalette(Array.Empty<GeoPointCategory>());
        }

        var categories = new List<GeoPointCategory>(order.Count + 1);
        var nextSlot = 0;
        foreach (var name in order)
        {
            if (categoryColors is not null && categoryColors.TryGetValue(name, out var custom) && IsCssColor(custom))
            {
                categories.Add(new GeoPointCategory(name, custom, counts[name], IsOther: false));
            }
            else if (nextSlot < CategoryColors.Count)
            {
                categories.Add(new GeoPointCategory(name, CategoryColors[nextSlot++], counts[name], IsOther: false));
            }
            else
            {
                categories.Add(new GeoPointCategory(name, OtherColor, counts[name], IsOther: true));
            }
        }

        if (uncategorized > 0)
        {
            categories.Add(new GeoPointCategory(string.Empty, OtherColor, uncategorized, IsOther: true));
        }

        return new GeoPointPalette(categories);
    }

    /// <summary>
    /// Цвет маркера точки.
    /// </summary>
    public string ColorFor(GeoPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        if (IsCssColor(point.Color))
        {
            return point.Color!.Trim();
        }

        if (!HasCategories)
        {
            return DefaultColor;
        }

        return _categories.TryGetValue(CategoryOf(point), out var category) ? category.Color : OtherColor;
    }

    /// <summary>
    /// Похоже ли значение на цвет CSS: <c>#rgb</c>, <c>#rrggbb</c> (и с прозрачностью),
    /// <c>rgb()</c>, <c>hsl()</c> или имя цвета. Цвет из данных попадает в атрибут
    /// <c>style</c>, поэтому всё остальное отбрасывается.
    /// </summary>
    public static bool IsCssColor(string? value) =>
        !string.IsNullOrWhiteSpace(value) && CssColorPattern.IsMatch(value.Trim());

    private static readonly Regex CssColorPattern = new(
        @"^(#[0-9a-fA-F]{3,4}|#[0-9a-fA-F]{6}|#[0-9a-fA-F]{8}|[a-zA-Z]{3,30}|(rgb|rgba|hsl|hsla)\([0-9.,%\s/deg]+\))$",
        RegexOptions.CultureInvariant);

    /// <summary>
    /// Название категории точки; пробелы по краям не учитываются, пустая строка — без категории.
    /// </summary>
    public static string CategoryOf(GeoPoint point) => point.Category?.Trim() ?? string.Empty;
}
