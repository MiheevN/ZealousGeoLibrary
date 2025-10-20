# Zealous Minded People Geography Library

Библиотека для создания интерактивных 3D географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

[![NuGet](https://img.shields.io/nuget/v/ZealousMindedPeopleGeo.svg)](https://www.nuget.org/packages/ZealousMindedPeopleGeo/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🌟 Возможности

- **3D глобус сообщества** - Трехмерная визуализация планеты с интерактивными точками участников
- **Множественные глобусы** - Поддержка нескольких независимых 3D глобусов на одной странице
- **Управление состоянием** - Централизованное управление состоянием всех глобусов
- **In-Memory репозиторий** - Простое хранение данных участников в памяти
- **Модульная архитектура** - Четкое разделение ответственности между компонентами
- **Адаптивный дизайн** - Адаптация под различные размеры экрана и устройства

## 🚀 Быстрый старт

### 1. Установка

```bash
dotnet add package ZealousMindedPeopleGeo
```

### 2. Регистрация сервисов

В файле `Program.cs`:

```csharp
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Services.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Регистрация сервисов библиотеки
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);

// Репозиторий участников (для тестирования)
builder.Services.AddSingleton<IParticipantRepository, InMemoryParticipantRepository>();

var app = builder.Build();
```

### 3. Использование компонентов в Razor Pages

#### Одиночный 3D глобус

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>3D глобус сообщества</PageTitle>

<CommunityGlobeComponent
    Width="800"
    Height="600"
    ShowControls="true"
    ShowParticipantManagement="true" />
```

#### Множественные 3D глобусы

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>Множественные 3D глобусы</PageTitle>

<div style="display: flex; gap: 20px;">
    <div>
        <h4>Глобус Европы</h4>
        <CommunityGlobeComponent
            GlobeId="europe"
            Width="400"
            Height="300"
            ShowControls="true" />
    </div>

    <div>
        <h4>Глобус Азии</h4>
        <CommunityGlobeComponent
            GlobeId="asia"
            Width="400"
            Height="300"
            ShowControls="true" />
    </div>
</div>
```

#### Использование отдельных компонентов

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>Кастомная компоновка</PageTitle>

<div style="display: flex; flex-direction: column; gap: 20px;">
    <!-- Компонент отображения глобуса -->
    <CommunityGlobeViewer GlobeId="main" Width="800" Height="600" />

    <!-- Панель управления -->
    <CommunityGlobeControls GlobeId="main" />

    <!-- Панель управления участниками -->
    <CommunityGlobeParticipantManager GlobeId="main" />
</div>
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

## 🌟 Множественные глобусы

Библиотека поддерживает создание нескольких независимых 3D глобусов на одной странице благодаря модульной архитектуре.

### Преимущества модульного подхода:

- ✅ **Независимые экземпляры** - каждый глобус работает автономно
- ✅ **Изолированные ресурсы** - отдельные сцены, камеры и рендереры
- ✅ **Параллельные операции** - одновременная работа с разными глобусами
- ✅ **Гибкая конфигурация** - разные настройки для каждого глобусa
- ✅ **Оптимальная производительность** - нет конфликтов между экземплярами

## 🎯 Использование в Blazor проектах

### Минимальная настройка:

1. **Добавьте в `Program.cs`:**
   ```csharp
   // Базовая регистрация сервисов библиотеки
   builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);

   // Репозиторий участников (для тестирования)
   builder.Services.AddSingleton<IParticipantRepository, InMemoryParticipantRepository>();
   ```

2. **Используйте компонент в Razor странице:**
   ```razor
   @page "/globe"
   @using ZealousMindedPeopleGeo.Components

   <h3>Интерактивный 3D глобус</h3>

   <CommunityGlobeComponent
       Width="800"
       Height="600"
       ShowControls="true"
       ShowParticipantManagement="true">
   </CommunityGlobeComponent>
   ```

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

### Проверка функциональности

Библиотека протестирована и готова к использованию. Для проверки работоспособности:

1. Соберите проект: `dotnet build`
2. Используйте компоненты в вашем Blazor приложении
3. Проверьте консоль браузера на отсутствие ошибок
4. Убедитесь что 3D глобус корректно отображается и интерактивен

## 📦 Архитектура

### Модульная система

Библиотека использует модульную архитектуру с четким разделением ответственности:

- **CommunityGlobeComponent** - Главный компонент-обертка
- **CommunityGlobeViewer** - Компонент отображения 3D глобуса
- **CommunityGlobeControls** - Панель управления глобуса
- **CommunityGlobeParticipantManager** - Панель управления участниками

### Сервисы

- **ThreeJsGlobeService** - Управление 3D сценой и рендерингом
- **GlobeMediatorService** - Посредник между Blazor и JavaScript
- **InMemoryParticipantRepository** - Хранение данных участников в памяти

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

## 🛠️ Разработка

### Структура проекта

```
ZealousMindedPeopleGeo/
├── Components/           # Blazor компоненты
│   ├── CommunityGlobeComponent.razor      # Главный компонент-обертка
│   ├── CommunityGlobeViewer.razor         # Компонент отображения глобуса
│   ├── CommunityGlobeControls.razor       # Панель управления
│   └── CommunityGlobeParticipantManager.razor # Управление участниками
├── Services/            # Бизнес-логика сервисы
│   ├── Mapping/                          # Сервисы для работы с картами
│   │   ├── ThreeJsGlobeService.cs        # Управление 3D сценой
│   │   ├── GlobeMediatorService.cs       # Посредник Blazor-JavaScript
│   │   └── IGlobeMediator.cs             # Интерфейс посредника
│   └── Repositories/                     # Репозитории данных
│       └── InMemoryParticipantRepository.cs # In-Memory хранилище
├── Models/              # Модели данных
│   ├── Participant.cs                    # Модель участника
│   ├── GlobeOptions.cs                   # Настройки глобуса
│   └── GlobeState.cs                     # Состояние глобуса
└── wwwroot/             # Статические ресурсы
    ├── js/              # JavaScript модули
    │   └── community-globe.js            # Основной модуль глобуса
    ├── css/             # Стили
    │   └── community-globe.css           # Стили компонентов
    └── assets/          # Ресурсы
        └── earth/       # 8K текстуры Земли
```

### Сборка проекта

```bash
# Сборка библиотеки
dotnet build ZealousMindedPeopleGeo
```


## 🤝 Вклад в развитие

Мы приветствуем вклад в развитие проекта! Пожалуйста, ознакомьтесь с руководством по вкладу:

1. Fork проект
2. Создайте feature branch (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте в branch (`git push origin feature/AmazingFeature`)
5. Создайте Pull Request


## 🙏 Благодарности

- **Three.js** - Библиотека для 3D графики
- **Google Maps API** - Картографические сервисы
- **OpenStreetMap** - Бесплатные географические данные
- **FluentValidation** - Библиотека валидации
- **ASP.NET Core** - Платформа веб-разработки

---

*"География людей, объединенных стремлением сделать мир добрее и гармоничнее"*