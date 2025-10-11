using FluentValidation;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;

namespace ZealousMindedPeopleGeo.Validation;

/// <summary>
/// Валидатор для модели участника
/// </summary>
public class ParticipantValidator : AbstractValidator<Participant>
{
    public ParticipantValidator(ILocalizationService localizationService)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .MinimumLength(2)
            .WithMessage(localizationService.GetString("MinLength", 2))
            .MaximumLength(100)
            .WithMessage(localizationService.GetString("MaxLength", 100))
            .Matches(@"^[a-zA-Zа-яА-Я\s\-']+$")
            .WithMessage("Имя может содержать только буквы, пробелы, дефисы и апострофы");

        RuleFor(p => p.Email)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .EmailAddress()
            .WithMessage(localizationService.GetString("InvalidEmail"))
            .MaximumLength(254)
            .WithMessage(localizationService.GetString("MaxLength", 254));

        RuleFor(p => p.Location)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .MinimumLength(3)
            .WithMessage(localizationService.GetString("MinLength", 3))
            .MaximumLength(200)
            .WithMessage(localizationService.GetString("MaxLength", 200));

        RuleFor(p => p.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Широта должна быть в диапазоне от -90 до 90 градусов");

        RuleFor(p => p.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Долгота должна быть в диапазоне от -180 до 180 градусов");

        RuleFor(p => p.City)
            .MaximumLength(100)
            .WithMessage(localizationService.GetString("MaxLength", 100));

        RuleFor(p => p.Country)
            .MaximumLength(100)
            .WithMessage(localizationService.GetString("MaxLength", 100));

        RuleFor(p => p.Message)
            .MaximumLength(1000)
            .WithMessage(localizationService.GetString("MaxLength", 1000));

        RuleFor(p => p.LifeGoals)
            .MaximumLength(500)
            .WithMessage(localizationService.GetString("MaxLength", 500));

        RuleFor(p => p.Skills)
            .MaximumLength(300)
            .WithMessage(localizationService.GetString("MaxLength", 300));

        // Валидация социальных контактов
        RuleFor(p => p.SocialContacts)
            .Must(HaveValidSocialContacts)
            .When(p => p.SocialContacts != null)
            .WithMessage("Некоторые социальные контакты имеют некорректный формат");

        // Проверка что координаты соответствуют адресу (если координаты указаны)
        RuleFor(p => p)
            .Must(HaveConsistentLocationData)
            .When(p => p.Latitude.HasValue && p.Longitude.HasValue && !string.IsNullOrEmpty(p.Location))
            .WithMessage("Координаты не соответствуют указанному адресу");

        // Проверка возраста регистрации (не более 1 года в будущем)
        RuleFor(p => p.RegisteredAt)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Дата регистрации не может быть в будущем");

        // Проверка что регистрация не слишком старая (более 10 лет)
        RuleFor(p => p.RegisteredAt)
            .GreaterThan(DateTime.UtcNow.AddYears(-10))
            .WithMessage("Дата регистрации слишком старая");
    }

    private bool HaveValidSocialContacts(SocialContacts? contacts)
    {
        if (contacts == null) return true;

        // Валидация URL социальных сетей
        if (!string.IsNullOrEmpty(contacts.Discord))
        {
            if (!Uri.TryCreate(contacts.Discord, UriKind.Absolute, out var discordUri) ||
                (discordUri.Scheme != Uri.UriSchemeHttp && discordUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(contacts.Telegram))
        {
            if (!Uri.TryCreate(contacts.Telegram, UriKind.Absolute, out var telegramUri) ||
                (telegramUri.Scheme != Uri.UriSchemeHttp && telegramUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(contacts.Vk))
        {
            if (!Uri.TryCreate(contacts.Vk, UriKind.Absolute, out var vkUri) ||
                (vkUri.Scheme != Uri.UriSchemeHttp && vkUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(contacts.Website))
        {
            if (!Uri.TryCreate(contacts.Website, UriKind.Absolute, out var websiteUri) ||
                (websiteUri.Scheme != Uri.UriSchemeHttp && websiteUri.Scheme != Uri.UriSchemeHttps))
            {
                return false;
            }
        }

        return true;
    }

    private bool HaveConsistentLocationData(Participant participant)
    {
        // Здесь можно добавить более сложную логику проверки соответствия координат адресу
        // Например, используя сервисы геокодирования или простые проверки

        if (string.IsNullOrEmpty(participant.Location)) return true;
        if (!participant.Latitude.HasValue || !participant.Longitude.HasValue) return true;

        // Простая проверка: если адрес содержит название страны/города,
        // координаты должны быть в разумных пределах
        var locationLower = participant.Location.ToLower();

        // Проверка для России
        if (locationLower.Contains("россия") || locationLower.Contains("russia"))
        {
            return participant.Longitude.Value >= 19 && participant.Longitude.Value <= 180 &&
                   participant.Latitude.Value >= 41 && participant.Latitude.Value <= 82;
        }

        // Проверка для США
        if (locationLower.Contains("сша") || locationLower.Contains("usa") ||
            locationLower.Contains("united states"))
        {
            return participant.Longitude.Value >= -125 && participant.Longitude.Value <= -67 &&
                   participant.Latitude.Value >= 25 && participant.Latitude.Value <= 49;
        }

        // Для других стран возвращаем true (требуется более сложная логика)
        return true;
    }
}

/// <summary>
/// Валидатор для модели регистрации участника
/// </summary>
public class ParticipantRegistrationValidator : AbstractValidator<ParticipantRegistrationModel>
{
    public ParticipantRegistrationValidator(ILocalizationService localizationService)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .MinimumLength(2)
            .WithMessage(localizationService.GetString("MinLength", 2))
            .MaximumLength(100)
            .WithMessage(localizationService.GetString("MaxLength", 100))
            .Matches(@"^[a-zA-Zа-яА-Я\s\-']+$")
            .WithMessage("Имя может содержать только буквы, пробелы, дефисы и апострофы");

        RuleFor(p => p.Email)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .EmailAddress()
            .WithMessage(localizationService.GetString("InvalidEmail"))
            .MaximumLength(254)
            .WithMessage(localizationService.GetString("MaxLength", 254));

        RuleFor(p => p.Address)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .MinimumLength(10)
            .WithMessage(localizationService.GetString("MinLength", 10))
            .MaximumLength(200)
            .WithMessage(localizationService.GetString("MaxLength", 200));

        RuleFor(p => p.SocialMedia)
            .MaximumLength(500)
            .WithMessage(localizationService.GetString("MaxLength", 500));

        RuleFor(p => p.Message)
            .MaximumLength(1000)
            .WithMessage(localizationService.GetString("MaxLength", 1000));

        // Проверка что адрес содержит достаточно информации для геокодирования
        RuleFor(p => p.Address)
            .Must(AddressContainsLocationInfo)
            .WithMessage("Адрес должен содержать название города и страны для точного определения местоположения");
    }

    private bool AddressContainsLocationInfo(string address)
    {
        if (string.IsNullOrEmpty(address)) return false;

        var addressLower = address.ToLower();

        // Проверяем наличие ключевых слов, указывающих на географическое положение
        var locationKeywords = new[]
        {
            "город", "city", "town", "village",
            "страна", "country",
            "область", "region", "state", "province",
            "улица", "street", "avenue", "road",
            "дом", "house", "building"
        };

        return locationKeywords.Any(keyword => addressLower.Contains(keyword));
    }
}

/// <summary>
/// Валидатор для географических координат
/// </summary>
public class CoordinateValidator : AbstractValidator<(double Latitude, double Longitude)>
{
    public CoordinateValidator()
    {
        RuleFor(coord => coord.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Широта должна быть в диапазоне от -90 до 90 градусов");

        RuleFor(coord => coord.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Долгота должна быть в диапазоне от -180 до 180 градусов");
    }
}

/// <summary>
/// Валидатор для модели геокодирования
/// </summary>
public class GeocodingRequestValidator : AbstractValidator<GeocodingRequest>
{
    public GeocodingRequestValidator(ILocalizationService localizationService)
    {
        RuleFor(r => r.Address)
            .NotEmpty()
            .WithMessage(localizationService.GetString("RequiredField"))
            .MinimumLength(3)
            .WithMessage(localizationService.GetString("MinLength", 3))
            .MaximumLength(200)
            .WithMessage(localizationService.GetString("MaxLength", 200));

        RuleFor(r => r.Language)
            .Must(lang => string.IsNullOrEmpty(lang) || lang.Length == 2)
            .WithMessage("Код языка должен состоять из 2 символов");

        RuleFor(r => r.CountryCode)
            .Must(code => string.IsNullOrEmpty(code) || code.Length == 2)
            .WithMessage("Код страны должен состоять из 2 символов");
    }
}

/// <summary>
/// Валидатор для модели GlobeOptions
/// </summary>
public class GlobeOptionsValidator : AbstractValidator<GlobeOptions>
{
    public GlobeOptionsValidator()
    {
        RuleFor(o => o.Width)
            .GreaterThan(0)
            .WithMessage("Ширина должна быть больше 0")
            .LessThanOrEqualTo(4096)
            .WithMessage("Ширина не должна превышать 4096 пикселей");

        RuleFor(o => o.Height)
            .GreaterThan(0)
            .WithMessage("Высота должна быть больше 0")
            .LessThanOrEqualTo(4096)
            .WithMessage("Высота не должна превышать 4096 пикселей");

        RuleFor(o => o.LevelOfDetail)
            .InclusiveBetween(0, 3)
            .WithMessage("Уровень детализации должен быть в диапазоне от 0 до 3");

        RuleFor(o => o.MinZoom)
            .GreaterThan(0)
            .WithMessage("Минимальный зум должен быть больше 0");

        RuleFor(o => o.MaxZoom)
            .GreaterThan(o => o.MinZoom)
            .WithMessage("Максимальный зум должен быть больше минимального");

        RuleFor(o => o.AutoRotateSpeed)
            .InclusiveBetween(-5, 5)
            .WithMessage("Скорость автоповорота должна быть в диапазоне от -5 до 5");
    }
}

/// <summary>
/// Модель запроса геокодирования
/// </summary>
public class GeocodingRequest
{
    public string Address { get; set; } = "";
    public string? Language { get; set; }
    public string? CountryCode { get; set; }
    public bool IncludeCountryInfo { get; set; } = true;
}