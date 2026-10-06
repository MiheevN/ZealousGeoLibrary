namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Точка на карте или глобусе: офис, событие, датчик, участник сообщества — что угодно.
/// Общие поля нужны для отображения, всё остальное хранится в <see cref="Properties"/>.
/// </summary>
public class GeoPoint
{
    /// <summary>
    /// Наибольшая длина <see cref="Id"/>: идентификатор входит в ключ таблицы БД.
    /// </summary>
    public const int MaxIdLength = 128;

    /// <summary>Наибольшая длина <see cref="Title"/>.</summary>
    public const int MaxTitleLength = 500;

    /// <summary>Наибольшая длина <see cref="Category"/>.</summary>
    public const int MaxCategoryLength = 100;

    /// <summary>Наибольшая длина <see cref="Color"/>.</summary>
    public const int MaxColorLength = 50;

    /// <summary>Наибольшая длина <see cref="Icon"/>.</summary>
    public const int MaxIconLength = 500;

    /// <summary>Наибольшая длина <see cref="Url"/>.</summary>
    public const int MaxUrlLength = 2000;

    /// <summary>
    /// Идентификатор точки внутри контейнера. Любая строка до <see cref="MaxIdLength"/>
    /// символов: GUID, номер из вашей системы, «moscow-office». По умолчанию — новый GUID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Широта в градусах, от −90 до 90.</summary>
    public double Latitude { get; set; }

    /// <summary>Долгота в градусах, от −180 до 180.</summary>
    public double Longitude { get; set; }

    /// <summary>Подпись точки. Не бывает <c>null</c>: <c>null</c> становится пустой строкой.</summary>
    public string Title
    {
        get => _title;
        set => _title = value ?? string.Empty;
    }

    private string _title = string.Empty;

    /// <summary>Описание для карточки точки.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Категория для группировки, фильтра и легенды: «office», «event», «sensor».
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Цвет маркера в формате CSS, например <c>#24dce7</c>.</summary>
    public string? Color { get; set; }

    /// <summary>Иконка маркера: символ, имя или адрес картинки.</summary>
    public string? Icon { get; set; }

    /// <summary>Ссылка, связанная с точкой.</summary>
    public string? Url { get; set; }

    /// <summary>
    /// Любые дополнительные поля точки. Значения — строки: числа и даты храните в
    /// инвариантном формате.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Проверяет, можно ли сохранить точку.
    /// </summary>
    /// <returns><c>null</c>, если точка корректна, иначе текст ошибки.</returns>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            return "Point Id is required";
        }

        if (Id.Length > MaxIdLength)
        {
            return $"Point Id is longer than {MaxIdLength} characters";
        }

        if (!double.IsFinite(Latitude) || Latitude is < -90 or > 90)
        {
            return $"Point '{Id}': latitude {Latitude} is outside -90..90";
        }

        if (!double.IsFinite(Longitude) || Longitude is < -180 or > 180)
        {
            return $"Point '{Id}': longitude {Longitude} is outside -180..180";
        }

        // Те же пределы, что у столбцов БД: хранилища принимают одни и те же точки.
        return TooLong(nameof(Title), Title, MaxTitleLength)
            ?? TooLong(nameof(Category), Category, MaxCategoryLength)
            ?? TooLong(nameof(Color), Color, MaxColorLength)
            ?? TooLong(nameof(Icon), Icon, MaxIconLength)
            ?? TooLong(nameof(Url), Url, MaxUrlLength);
    }

    /// <summary>
    /// Копия точки, включая словарь <see cref="Properties"/>: изменения копии не
    /// затрагивают оригинал.
    /// </summary>
    public GeoPoint Clone()
    {
        var copy = (GeoPoint)MemberwiseClone();
        copy.Properties = Properties is null ? new() : new Dictionary<string, string>(Properties);
        return copy;
    }

    private string? TooLong(string field, string? value, int maxLength) =>
        value is not null && value.Length > maxLength
            ? $"Point '{Id}': {field} is longer than {maxLength} characters"
            : null;
}
