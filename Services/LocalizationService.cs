using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Сервис для управления локализацией и региональными настройками
/// </summary>
public class LocalizationService : ILocalizationService, IDisposable
{
    private readonly ILogger<LocalizationService> _logger;
    private CultureInfo _currentCulture = CultureInfo.CurrentCulture;
    private readonly HashSet<CultureInfo> _supportedCultures;
    private readonly Dictionary<string, Dictionary<string, string>> _localizationData;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;

        // Определяем поддерживаемые культуры
        _supportedCultures = new HashSet<CultureInfo>
        {
            new CultureInfo("en-US"),
            new CultureInfo("ru-RU")
        };

        // Инициализируем данные локализации программно
        _localizationData = new Dictionary<string, Dictionary<string, string>>
        {
            ["en-US"] = new Dictionary<string, string>
            {
                ["Name"] = "Name",
                ["Email"] = "Email",
                ["Address"] = "Address",
                ["Message"] = "Message",
                ["Submit"] = "Submit",
                ["Cancel"] = "Cancel",
                ["Save"] = "Save",
                ["Delete"] = "Delete",
                ["Edit"] = "Edit",
                ["Close"] = "Close",
                ["Home"] = "Home",
                ["Map"] = "Map",
                ["Globe"] = "Globe",
                ["Register"] = "Register",
                ["About"] = "About",
                ["JoinCommunity"] = "Join Our Community",
                ["RegistrationSuccessful"] = "Registration successful! Welcome to our community!",
                ["RegistrationFailed"] = "Registration failed. Please try again.",
                ["FindingLocation"] = "Finding your location...",
                ["LocationFound"] = "Location Found",
                ["SocialMedia"] = "Social Media",
                ["ShareThoughts"] = "Share your thoughts or goals",
                ["Registering"] = "Registering...",
                ["JoinCommunityButton"] = "Join Community",
                ["CommunityMap"] = "Community Map",
                ["Participants"] = "Participants",
                ["Countries"] = "Countries",
                ["CenterOnUser"] = "Center on User",
                ["YourLocation"] = "Your Location",
                ["LocationPermissionDenied"] = "Location permission denied. Please check your browser settings.",
                ["CommunityGlobe"] = "Community Globe",
                ["LoadingGlobe"] = "Loading Globe",
                ["GlobeReady"] = "Globe Ready",
                ["GlobeError"] = "Globe Error",
                ["Rotation"] = "Rotation",
                ["LevelOfDetail"] = "Level of Detail",
                ["Low"] = "Low",
                ["Medium"] = "Medium",
                ["High"] = "High",
                ["Ultra"] = "Ultra",
                ["RefreshData"] = "Refresh Data",
                ["ParticipantInfo"] = "Participant Information",
                ["Location"] = "Location",
                ["Coordinates"] = "Coordinates",
                ["RegistrationDate"] = "Registration Date",
                ["Goals"] = "Goals",
                ["InstallApp"] = "Install App",
                ["InstallAppDescription"] = "Get quick access to the community geography right from your desktop",
                ["Install"] = "Install",
                ["Later"] = "Later",
                ["PWAInfo"] = "PWA Information",
                ["Status"] = "Status",
                ["Installed"] = "Installed",
                ["NotInstalled"] = "Not Installed",
                ["Cache"] = "Cache",
                ["CheckUpdates"] = "Check for Updates",
                ["ClearCache"] = "Clear Cache",
                ["CacheInfo"] = "Cache Information",
                ["CacheDetails"] = "Cache Details",
                ["TestNotification"] = "Test Notification",
                ["SendNotification"] = "Send Notification",
                ["NotificationTitle"] = "Notification Title",
                ["NotificationMessage"] = "Notification Message",
                ["Error"] = "Error",
                ["Success"] = "Success",
                ["Warning"] = "Warning",
                ["Info"] = "Information",
                ["Loading"] = "Loading...",
                ["RequiredField"] = "This field is required",
                ["InvalidEmail"] = "Please enter a valid email address",
                ["InvalidUrl"] = "Please enter a valid URL",
                ["MinLength"] = "Minimum length is {0} characters",
                ["MaxLength"] = "Maximum length is {0} characters"
            },
            ["ru-RU"] = new Dictionary<string, string>
            {
                ["Name"] = "Имя",
                ["Email"] = "Электронная почта",
                ["Address"] = "Адрес",
                ["Message"] = "Сообщение",
                ["Submit"] = "Отправить",
                ["Cancel"] = "Отмена",
                ["Save"] = "Сохранить",
                ["Delete"] = "Удалить",
                ["Edit"] = "Редактировать",
                ["Close"] = "Закрыть",
                ["Home"] = "Главная",
                ["Map"] = "Карта",
                ["Globe"] = "Глобус",
                ["Register"] = "Регистрация",
                ["About"] = "О проекте",
                ["JoinCommunity"] = "Присоединяйтесь к нашему сообществу",
                ["RegistrationSuccessful"] = "Регистрация успешна! Добро пожаловать в наше сообщество!",
                ["RegistrationFailed"] = "Регистрация не удалась. Пожалуйста, попробуйте еще раз.",
                ["FindingLocation"] = "Поиск вашего местоположения...",
                ["LocationFound"] = "Местоположение найдено",
                ["SocialMedia"] = "Социальные сети",
                ["ShareThoughts"] = "Поделитесь своими мыслями или целями",
                ["Registering"] = "Регистрация...",
                ["JoinCommunityButton"] = "Присоединиться к сообществу",
                ["CommunityMap"] = "Карта сообщества",
                ["Participants"] = "Участники",
                ["Countries"] = "Страны",
                ["CenterOnUser"] = "Центрировать на пользователе",
                ["YourLocation"] = "Ваше местоположение",
                ["LocationPermissionDenied"] = "Доступ к геолокации запрещен. Проверьте настройки браузера.",
                ["CommunityGlobe"] = "Глобус сообщества",
                ["LoadingGlobe"] = "Загрузка глобуса",
                ["GlobeReady"] = "Глобус готов",
                ["GlobeError"] = "Ошибка глобуса",
                ["Rotation"] = "Вращение",
                ["LevelOfDetail"] = "Уровень детализации",
                ["Low"] = "Низкий",
                ["Medium"] = "Средний",
                ["High"] = "Высокий",
                ["Ultra"] = "Ультра",
                ["RefreshData"] = "Обновить данные",
                ["ParticipantInfo"] = "Информация об участнике",
                ["Location"] = "Местоположение",
                ["Coordinates"] = "Координаты",
                ["RegistrationDate"] = "Дата регистрации",
                ["Goals"] = "Цели",
                ["InstallApp"] = "Установить приложение",
                ["InstallAppDescription"] = "Получите быстрый доступ к географии сообщества прямо с рабочего стола",
                ["Install"] = "Установить",
                ["Later"] = "Позже",
                ["PWAInfo"] = "Информация о PWA",
                ["Status"] = "Статус",
                ["Installed"] = "Установлено",
                ["NotInstalled"] = "Не установлено",
                ["Cache"] = "Кэш",
                ["CheckUpdates"] = "Проверить обновления",
                ["ClearCache"] = "Очистить кэш",
                ["CacheInfo"] = "Информация о кэше",
                ["CacheDetails"] = "Детали кэша",
                ["TestNotification"] = "Тестовое уведомление",
                ["SendNotification"] = "Отправить уведомление",
                ["NotificationTitle"] = "Заголовок уведомления",
                ["NotificationMessage"] = "Сообщение уведомления",
                ["Error"] = "Ошибка",
                ["Success"] = "Успешно",
                ["Warning"] = "Предупреждение",
                ["Info"] = "Информация",
                ["Loading"] = "Загрузка...",
                ["RequiredField"] = "Это поле обязательно для заполнения",
                ["InvalidEmail"] = "Пожалуйста, введите корректный адрес электронной почты",
                ["InvalidUrl"] = "Пожалуйста, введите корректный URL",
                ["MinLength"] = "Минимальная длина {0} символов",
                ["MaxLength"] = "Максимальная длина {0} символов"
            }
        };

        _logger.LogInformation("LocalizationService initialized with {CultureCount} cultures", _localizationData.Count);
    }

    /// <summary>
    /// Текущая культура приложения
    /// </summary>
    public CultureInfo CurrentCulture => _currentCulture;

    /// <summary>
    /// Поддерживаемые культуры
    /// </summary>
    public IEnumerable<CultureInfo> SupportedCultures => _supportedCultures;

    /// <summary>
    /// Получить локализованную строку
    /// </summary>
    public string GetString(string key)
    {
        try
        {
            if (_localizationData.TryGetValue(_currentCulture.Name, out var cultureData) &&
                cultureData.TryGetValue(key, out var localizedString))
            {
                return localizedString;
            }

            // Fallback: пробуем найти в английской локализации
            if (_localizationData.TryGetValue("en-US", out var englishData) &&
                englishData.TryGetValue(key, out var englishString))
            {
                return englishString;
            }

            // Ultimate fallback: возвращаем ключ
            _logger.LogWarning("Localized string not found for key: {Key} in culture: {Culture}", key, _currentCulture.Name);
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get localized string for key: {Key}", key);
            return key; // Возвращаем ключ как fallback
        }
    }

    /// <summary>
    /// Получить локализованную строку с параметрами
    /// </summary>
    public string GetString(string key, params object[] arguments)
    {
        try
        {
            var localizedString = GetString(key);
            return string.Format(localizedString, arguments);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get localized string for key: {Key} with arguments: {Arguments}", key, arguments);
            return string.Format(key, arguments); // Возвращаем форматированный ключ как fallback
        }
    }

    /// <summary>
    /// Установить культуру приложения
    /// </summary>
    public bool SetCulture(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);

            if (!_supportedCultures.Contains(culture))
            {
                _logger.LogWarning("Unsupported culture: {CultureCode}", cultureCode);
                return false;
            }

            _currentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            _logger.LogInformation("Culture changed to: {CultureCode}", cultureCode);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set culture: {CultureCode}", cultureCode);
            return false;
        }
    }

    /// <summary>
    /// Получить культуру по коду страны или языка
    /// </summary>
    public CultureInfo? GetCultureByCode(string code)
    {
        return _supportedCultures.FirstOrDefault(c =>
            c.Name.Equals(code, StringComparison.OrdinalIgnoreCase) ||
            c.TwoLetterISOLanguageName.Equals(code, StringComparison.OrdinalIgnoreCase) ||
            c.ThreeLetterISOLanguageName.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Определить культуру автоматически на основе браузера пользователя
    /// </summary>
    public CultureInfo DetectCultureFromBrowser(string acceptLanguageHeader)
    {
        if (string.IsNullOrEmpty(acceptLanguageHeader))
        {
            return _supportedCultures.First(); // Возвращаем первую поддерживаемую культуру по умолчанию
        }

        // Разбираем заголовок Accept-Language
        var languagePreferences = acceptLanguageHeader
            .Split(',')
            .Select(lang => lang.Split(';')[0].Trim())
            .Select(lang =>
            {
                var parts = lang.Split('-');
                return parts.Length == 2 ? $"{parts[0].ToLower()}-{parts[1].ToUpper()}" : lang.ToLower();
            })
            .ToList();

        // Ищем наиболее подходящую культуру
        foreach (var preference in languagePreferences)
        {
            var culture = GetCultureByCode(preference);
            if (culture != null)
            {
                return culture;
            }
        }

        return _supportedCultures.First(); // Fallback на первую поддерживаемую культуру
    }

    /// <summary>
    /// Получить направление текста для текущей культуры
    /// </summary>
    public string GetTextDirection()
    {
        return _currentCulture.TextInfo.IsRightToLeft ? "rtl" : "ltr";
    }

    /// <summary>
    /// Получить форматированную дату для текущей культуры
    /// </summary>
    public string FormatDate(DateTime date, string? format = null)
    {
        try
        {
            var dateFormat = format ?? _currentCulture.DateTimeFormat.ShortDatePattern;
            return date.ToString(dateFormat, _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format date: {Date}", date);
            return date.ToString(format ?? "d");
        }
    }

    /// <summary>
    /// Получить форматированное число для текущей культуры
    /// </summary>
    public string FormatNumber(double number, int decimalPlaces = 2)
    {
        try
        {
            return number.ToString($"N{decimalPlaces}", _currentCulture);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format number: {Number}", number);
            return number.ToString($"N{decimalPlaces}");
        }
    }

    /// <summary>
    /// Получить форматированный размер файла
    /// </summary>
    public string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:F1} {GetString(sizes[order])}";
    }

    /// <summary>
    /// Проверить, является ли текущая культура RTL
    /// </summary>
    public bool IsRightToLeft()
    {
        return _currentCulture.TextInfo.IsRightToLeft;
    }

    /// <summary>
    /// Получить название языка на текущей культуре
    /// </summary>
    public string GetLanguageDisplayName()
    {
        return _currentCulture.DisplayName;
    }

    /// <summary>
    /// Получить название языка в родном написании
    /// </summary>
    public string GetNativeLanguageName()
    {
        return _currentCulture.NativeName;
    }

    /// <summary>
    /// Событие изменения культуры
    /// </summary>
    public event EventHandler<CultureInfo>? CultureChanged;

    protected virtual void OnCultureChanged(CultureInfo newCulture)
    {
        CultureChanged?.Invoke(this, newCulture);
    }

    public void Dispose()
    {
        CultureChanged = null;
    }
}

/// <summary>
/// Интерфейс для сервиса локализации
/// </summary>
public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }
    IEnumerable<CultureInfo> SupportedCultures { get; }
    string GetString(string key);
    string GetString(string key, params object[] arguments);
    bool SetCulture(string cultureCode);
    CultureInfo? GetCultureByCode(string code);
    CultureInfo DetectCultureFromBrowser(string acceptLanguageHeader);
    string GetTextDirection();
    string FormatDate(DateTime date, string? format = null);
    string FormatNumber(double number, int decimalPlaces = 2);
    string FormatFileSize(long bytes);
    bool IsRightToLeft();
    string GetLanguageDisplayName();
    string GetNativeLanguageName();
    event EventHandler<CultureInfo>? CultureChanged;
}