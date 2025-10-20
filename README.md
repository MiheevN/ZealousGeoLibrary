# Zealous Minded People Geography Library

Библиотека для создания интерактивных географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

[![NuGet](https://img.shields.io/nuget/v/ZealousMindedPeopleGeo.svg)](https://www.nuget.org/packages/ZealousMindedPeopleGeo/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🌟 Возможности

- **Интерактивная карта сообщества** - Отображение участников на Google Maps с информационными окнами
- **3D глобус** - Трехмерная визуализация планеты с текстовыми метками участников и интерактивными элементами управления
- **Геокодирование** - Автоматическое определение координат по адресу через Nominatim OpenStreetMap API
- **Гибкие источники данных** - Поддержка различных репозиториев (In-Memory, Google Sheets, JSON файлы)
- **PWA поддержка** - Полноценное прогрессивное веб-приложение с оффлайн функциональностью
- **Локализация** - Поддержка русского и английского языков
- **Валидация данных** - Комплексная проверка вводимых данных с FluentValidation
- **Кэширование** - Оптимизация производительности через MemoryCache
- **Интеграционные тесты** - Автоматическое тестирование всех компонентов

## 🚀 Быстрый старт

### 1. Установка

```bash
dotnet add package ZealousMindedPeopleGeo
```

или через NuGet Package Manager:
```
Install-Package ZealousMindedPeopleGeo
```

### 2. Регистрация сервисов

В файле `Program.cs` или `Startup.cs`:

```csharp
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Services.Geocoding;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов библиотеки
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);

// Дополнительная настройка при необходимости
builder.Services.Configure<CachingSettings>(builder.Configuration.GetSection("Caching"));
builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));

var app = builder.Build();
```

### 3. Использование компонентов в Razor Pages

#### Карта сообщества

```razor
@using ZealousMindedPeopleGeo.Components
@using ZealousMindedPeopleGeo.Models

<PageTitle>Карта сообщества</PageTitle>

<CommunityMapComponent
    GoogleMapsApiKey="YOUR_GOOGLE_MAPS_API_KEY"
    Width="800"
    Height="600"
    ShowControls="true"
    OnParticipantMarkerClick="HandleParticipantClick" />

@code {
    private async Task HandleParticipantClick(Participant participant)
    {
        // Обработка клика по маркеру участника
        Console.WriteLine($"Clicked on participant: {participant.Name}");
    }
}
```

#### Форма регистрации участников

```razor
@using ZealousMindedPeopleGeo.Components
@using ZealousMindedPeopleGeo.Models

<PageTitle>Регистрация участника</PageTitle>

<ParticipantRegistrationComponent
    AutoGeocode="true"
    OnParticipantRegistered="HandleParticipantRegistered" />

@code {
    private async Task HandleParticipantRegistered(Participant participant)
    {
        // Обработка успешной регистрации
        Console.WriteLine($"New participant registered: {participant.Name}");
    }
}
```

#### 3D глобус сообщества

```razor
@using ZealousMindedPeopleGeo.Components
@using ZealousMindedPeopleGeo.Services.Mapping

<PageTitle>3D глобус сообщества</PageTitle>

<CommunityGlobeComponent
    Width="800"
    Height="600"
    ShowControls="true"
    ShowParticipantManagement="true"
    OnParticipantClick="HandleParticipantClick" />

@code {
    private async Task HandleParticipantClick(Participant participant)
    {
        // Обработка клика по участнику на глобусе
        Console.WriteLine($"Clicked on participant: {participant.Name}");
    }
}
```

## 🔧 Конфигурация

### appsettings.json

```json
{
  "Caching": {
    "DefaultOptions": {
      "SlidingExpiration": "00:30:00",
      "Priority": "Normal"
    },
    "TypeSpecificOptions": {
      "Participants": {
        "SlidingExpiration": "00:15:00",
        "Priority": "High"
      },
      "Geocoding": {
        "SlidingExpiration": "24:00:00",
        "Priority": "Normal"
      }
    }
  },
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY",
    "DefaultCenterLatitude": 55.7558,
    "DefaultCenterLongitude": 37.6176,
    "DefaultZoom": 10
  },
  "GoogleSheets": {
    "CredentialsPath": "credentials.json",
    "ApplicationName": "Zealous Minded People Geography",
    "SpreadsheetId": "YOUR_SPREADSHEET_ID"
  },
  "PWA": {
    "EnableServiceWorker": true,
    "EnableNotifications": true,
    "UpdateCheckInterval": "00:05:00"
  }
}
```

## 🏗️ Архитектура

### Модели данных

#### Participant
Основная модель участника сообщества:
```csharp
public class Participant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Location { get; set; } = "";
    public string? City { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Message { get; set; }
    public string? LifeGoals { get; set; }
    public string? Skills { get; set; }
    public SocialContacts? SocialContacts { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
```

#### GlobeOptions
Настройки для 3D глобуса:
```csharp
public class GlobeOptions
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public string BackgroundColor { get; set; } = "#000011";
    public bool AutoRotate { get; set; } = true;
    public double AutoRotateSpeed { get; set; } = 0.5;
    public bool EnableMouseControls { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
    public double MinZoom { get; set; } = 0.5;
    public double MaxZoom { get; set; } = 4.0;
    public int LevelOfDetail { get; set; } = 2;
}
```

### Сервисы

#### IParticipantService
Основной сервис для работы с участниками:
```csharp
public interface IParticipantService
{
    Task<ServiceResult<Participant>> RegisterParticipantAsync(ParticipantRegistrationModel model);
    Task<ServiceResult<IEnumerable<Participant>>> GetParticipantsAsync(int page = 1, int pageSize = 50);
    Task<ServiceResult<Participant>> GetParticipantAsync(Guid id);
    Task<ServiceResult> UpdateParticipantAsync(Guid id, Participant participant);
    Task<ServiceResult> DeleteParticipantAsync(Guid id);
}
```

#### IGeocodingService
Сервис геокодирования:
```csharp
public interface IGeocodingService
{
    Task<GeocodingResult> GeocodeAddressAsync(string address, string? language = null);
    Task<ReverseGeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, string? language = null);
    Task<IEnumerable<GeocodingSuggestion>> GetSuggestionsAsync(string query, string? language = null);
}
```

#### IThreeJsGlobeService
Сервис для управления 3D глобусом:
```csharp
public interface IThreeJsGlobeService
{
    Task<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options);
    Task<ServiceResult> AddParticipantsAsync(IEnumerable<Participant> participants);
    Task<ServiceResult> SetAutoRotationAsync(bool enabled);
    Task<ServiceResult> CenterOnAsync(double latitude, double longitude);
    Task<GlobeState> GetStateAsync();
    Task DisposeAsync();
}
```

## 🌐 PWA Функциональность

Библиотека включает полноценную поддержку PWA:

### Манифест приложения
Автоматически генерируется манифест веб-приложения с настройками темы, иконками и ярлыками.

### Сервис-воркер
Включает стратегии кэширования:
- **Cache First** для статических ресурсов
- **Network First** для API запросов
- **Network First с fallback** для HTML страниц

### Уведомления
Поддержка push уведомлений и локальных уведомлений:
```csharp
// Отправка уведомления
await PwaService.SendNotificationAsync("Добро пожаловать!", "Спасибо за регистрацию в нашем сообществе");
```

### Установка
Компонент `PwaManagerComponent` предоставляет интерфейс для установки PWA:
```razor
<PwaManagerComponent ShowInstallPrompt="true" AutoCheckUpdates="true" />
```

## 🌍 Локализация

Библиотека поддерживает несколько языков через встроенную систему локализации без использования внешних файлов ресурсов.

### Поддерживаемые культуры
- **Русский (Россия)** - `ru-RU`
- **Английский (США)** - `en-US`

### Использование локализации

```csharp
@inject ILocalizationService LocalizationService

<h1>@LocalizationService.GetString("JoinCommunity")</h1>
<button>@LocalizationService.GetString("Submit")</button>

@* Локализованная дата *@
<span>@LocalizationService.FormatDate(DateTime.Now)</span>

@* Локализованное число *@
<span>@LocalizationService.FormatNumber(1234.56)</span>
```

### Настройка культуры

```csharp
// В контроллере или сервисе
await LocalizationService.SetCultureAsync("ru-RU");

// Автоматическое определение по браузеру
var culture = LocalizationService.DetectCultureFromBrowser(Request.Headers["Accept-Language"].ToString());
await LocalizationService.SetCultureAsync(culture.Name);
```

### Программная настройка строк локализации

Локализация настроена программно в `LocalizationService` и включает все необходимые строки интерфейса на русском и английском языках.

## ✅ Валидация

Комплексная валидация данных с использованием FluentValidation:

```csharp
// Валидация участника
var result = await ValidationService.ValidateParticipantAsync(participant);
if (!result.IsValid)
{
    var errors = result.Errors.Select(e => e.ErrorMessage);
    // Обработка ошибок
}

// Валидация координат
var isValid = await ValidationService.AreCoordinatesValidAsync(latitude, longitude);

// Валидация адреса
var isValid = await ValidationService.IsAddressValidForGeocodingAsync(address);
```

## 💾 Кэширование

Интеллектуальное кэширование для оптимизации производительности:

```csharp
// Получить или создать закешированные данные
var participants = await CachingService.GetOrCreateParticipantsAsync(
    async cancellationToken => await ParticipantRepository.GetAllParticipantsAsync(),
    cancellationToken);

// Специфичные настройки кэширования
var geocodingResult = await CachingService.GetOrCreateGeocodingResultAsync(
    address,
    async cancellationToken => await GeocodingService.GeocodeAddressAsync(address),
    cancellationToken);
```

## 🧪 Тестирование

Библиотека включает интеграционные тесты и скрипты автоматизации:

### Запуск тестов

```bash
# Полный цикл тестирования
.\scripts\test-complete.ps1

# Быстрое тестирование
.\scripts\test-basic.ps1

# Запуск с приложением
.\scripts\start-with-tests.ps1
```

### Health Check

Проверка состояния сервисов через HTTP endpoints:
- `GET /api/healthcheck` - Базовая проверка
- `GET /api/healthcheck/detailed` - Детальная диагностика
- `GET /api/healthcheck/geocoding` - Тест геокодирования
- `GET /api/healthcheck/repository` - Тест репозитория

## 📦 Расширенные возможности

### Гибкие репозитории данных

Библиотека поддерживает несколько источников данных:

```csharp
// In-Memory репозиторий (по умолчанию)
services.AddInMemoryParticipantRepository();

// Google Sheets репозиторий
services.AddGoogleSheetsParticipantRepository(options =>
{
    options.CredentialsPath = "credentials.json";
    options.SpreadsheetId = "YOUR_SPREADSHEET_ID";
});

// JSON файл репозиторий
services.AddJsonFileParticipantRepository(options =>
{
    options.FilePath = "participants.json";
});
```

### Кастомные реализации сервисов

```csharp
// Кастомный сервис геокодирования
services.AddTransient<IGeocodingService, CustomGeocodingService>();

// Кастомный репозиторий
services.AddTransient<IParticipantRepository, CustomParticipantRepository>();
```

## 🚨 Обработка ошибок

Библиотека предоставляет детальную информацию об ошибках:

```csharp
var result = await ParticipantService.RegisterParticipantAsync(model);

if (result.Success)
{
    var participant = result.Data;
}
else
{
    var errorMessage = result.ErrorMessage;
    var errorCode = result.ErrorCode;
}
```

## 🔒 Безопасность

Рекомендации по безопасности:

1. **API ключи** - Храните ключи Google Maps API в защищенной конфигурации
2. **Валидация** - Всегда используйте встроенную валидацию данных
3. **CORS** - Настройте политику CORS для защиты от CSRF атак
4. **HTTPS** - Используйте HTTPS для всех запросов

## 📚 Примеры проектов

Смотрите папку `samples/` для полных примеров использования:

- **Blazor Server App** - Полноценное приложение с использованием всех возможностей
- **Minimal API** - Минимальный пример с базовой функциональностью
- **Unit Tests** - Примеры тестирования компонентов

## 🛠️ Разработка

### Структура проекта

```
ZealousMindedPeopleGeo/
├── Components/           # Blazor компоненты
│   ├── CommunityGlobeComponent.razor
│   ├── CommunityMapComponent.razor
│   └── ParticipantRegistrationComponent.razor
├── Services/            # Бизнес-логика сервисы
│   ├── ParticipantService.cs
│   ├── GeocodingService.cs
│   ├── ThreeJsGlobeService.cs
│   └── LocalizationService.cs
├── Models/              # Модели данных
│   ├── Participant.cs
│   └── Configuration.cs
├── Validation/          # Валидация данных
│   └── ParticipantValidator.cs
└── wwwroot/             # Статические ресурсы
    ├── js/              # JavaScript модули
    ├── css/             # Стили
    ├── manifest.json    # PWA манифест
    └── sw.js           # Сервис-воркер
```

### Сборка и тестирование

```bash
# Восстановление пакетов
dotnet restore

# Сборка проекта
dotnet build

# Запуск тестов
dotnet test

# Создание NuGet пакета
dotnet pack -c Release -o ./artifacts
```

## 📄 Лицензия

Этот проект лицензирован под MIT License - см. файл [LICENSE](LICENSE) для подробностей.

## 🤝 Вклад в развитие

Мы приветствуем вклад в развитие проекта! Пожалуйста, ознакомьтесь с руководством по вкладу:

1. Fork проект
2. Создайте feature branch (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте в branch (`git push origin feature/AmazingFeature`)
5. Создайте Pull Request

## 📞 Контакты

- **Email**: info@zealousmindedpeople.org
- **Website**: https://zealousmindedpeople.org
- **GitHub Issues**: [Сообщить об ошибке](https://github.com/zealousmindedpeople/geo-library/issues)

## 🙏 Благодарности

- **Three.js** - Библиотека для 3D графики
- **Google Maps API** - Картографические сервисы
- **OpenStreetMap** - Бесплатные географические данные
- **FluentValidation** - Библиотека валидации
- **ASP.NET Core** - Платформа веб-разработки

---

*"География людей, объединенных стремлением сделать мир добрее и гармоничнее"*