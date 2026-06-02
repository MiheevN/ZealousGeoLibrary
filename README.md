# Zealous Minded People Geography Library

Библиотека для создания интерактивных 3D географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

[![NuGet](https://img.shields.io/nuget/v/ZealousMindedPeopleGeo.svg)](https://www.nuget.org/packages/ZealousMindedPeopleGeo/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🌟 Возможности

- **3D глобус сообщества** - Трехмерная визуализация планеты с интерактивными точками участников
- **Настройки глобуса** - Полная настройка параметров глобуса (размеры, освещение, атмосфера, облака)
- **Динамическое управление** - Включение/выключение атмосферы и облаков в реальном времени
- **Множественные глобусы** - Поддержка нескольких независимых 3D глобусов на одной странице
- **Именованные контейнеры гео-данных** - Удобная организация данных в именованных контейнерах для разных глобусов
- **Управление состоянием** - Централизованное управление состоянием всех глобусов
- **Загрузка/сохранение данных** - Удобные интерфейсы для инициализации и сохранения данных из JSON и других источников
- **In-Memory репозиторий** - Простое хранение данных участников в памяти
- **Модульная архитектура** - Четкое разделение ответственности между компонентами
- **Адаптивный дизайн** - Адаптация под различные размеры экрана и устройства
- **Нулевые предупреждения** - Код соответствует современным стандартам .NET 9

## 🚀 Быстрый старт

### 1. Установка

```bash
dotnet add package ZealousMindedPeopleGeo
```

## 📦 Встраивание в проект

### Пошаговая интеграция:

1. **Установите пакет NuGet**
   ```bash
   dotnet add package ZealousMindedPeopleGeo
   ```

2. **Зарегистрируйте сервисы в Program.cs**
   ```csharp
   builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);
   builder.Services.AddSingleton<IParticipantRepository, InMemoryParticipantRepository>();
   ```

3. **Добавьте using в Razor страницу**
   ```razor
   @using ZealousMindedPeopleGeo.Components
   ```

4. **Используйте компонент**
   ```razor
   <CommunityGlobeComponent Width="800" Height="600" ShowControls="true" />
   ```

5. **Настройте appsettings.json** (опционально)
   ```json
   {
     "GoogleMaps": {
       "ApiKey": "YOUR_API_KEY"
     }
   }
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

#### Витрина всех возможностей (LibraryShowcaseComponent)

`LibraryShowcaseComponent` — единый компонент, который демонстрирует **всю**
функциональность библиотеки на одной странице: два независимых 3D глобуса, 2D карту,
панели управления и настроек, форму регистрации участника, PWA-менеджер и экспорт данных.

Каждое представление настроено собственным изолированным набором демонстрационных
данных (именованный контейнер гео-данных или явный список участников), поэтому изменения
в одном инстансе **не влияют** на другие.

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>Возможности библиотеки</PageTitle>

<LibraryShowcaseComponent />
```

Готовые наборы данных доступны через `DemoDataSets` (`RussianCities`, `WorldCapitals`,
`TechHubs`) — каждый вызов возвращает свежую независимую копию участников.

Изоляция обеспечивается двумя новыми параметрами:

- `CommunityGlobeViewer.DataContainerId` — загружает участников из указанного именованного
  контейнера вместо общего репозитория;
- `CommunityMapComponent.Participants` — отображает явный список участников без обращения
  к общему репозиторию.

```razor
<!-- Два глобуса с независимыми данными -->
<CommunityGlobeViewer GlobeId="globe-a" DataContainerId="data-a" Width="600" Height="400" />
<CommunityGlobeViewer GlobeId="globe-b" DataContainerId="data-b" Width="600" Height="400" />

<!-- Карта с явным набором участников -->
<CommunityMapComponent MapId="map-c" Participants="@myParticipants" />
```

#### Одиночный 3D глобус

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>3D глобус сообщества</PageTitle>

<CommunityGlobeComponent
    Width="800"
    Height="600"
    CurrentLatitude="@CurrentLatitude"
    CurrentLongitude="@CurrentLongitude"
    ShowControls="true"
    ShowParticipantManagement="true" />

@code {
    private double? CurrentLatitude = 55.7558;
    private double? CurrentLongitude = 37.6176;
}
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
    <CommunityGlobeControls
        GlobeId="main"
        CurrentLatitude="@CurrentLatitude"
        CurrentLongitude="@CurrentLongitude" />

    <!-- Панель управления участниками -->
    <CommunityGlobeParticipantManager GlobeId="main" />

    <!-- Панель настроек глобуса -->
    <CommunityGlobeSettings GlobeId="main" />
</div>

@code {
    private double? CurrentLatitude = 55.7558;
    private double? CurrentLongitude = 37.6176;
}
```

#### Настройки глобуса

```razor
@using ZealousMindedPeopleGeo.Components

<PageTitle>Настройки 3D глобуса</PageTitle>

<!-- Глобус с панелью настроек -->
<CommunityGlobeComponent
    GlobeId="configurable"
    Width="800"
    Height="600"
    ShowSettings="true" />
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
    public double MinZoom { get; set; } = 1.03;
    public double MaxZoom { get; set; } = 4.0;
    public int LevelOfDetail { get; set; } = 2;
    public bool SunLightFollowCamera { get; set; } = true;
    public double SunLightDistance { get; set; } = 6.0;
    public double SunLightIntensity { get; set; } = 2.8;
    public double AmbientLightIntensity { get; set; } = 1.2;
    public double HemisphereLightIntensity { get; set; } = 0.8;
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

## 📦 Именованные контейнеры гео-данных

Библиотека предоставляет удобные интерфейсы для загрузки и сохранения данных в именованные контейнеры гео-данных, что упрощает работу с несколькими глобусами и разными наборами данных.

### Регистрация сервисов

```csharp
// В Program.cs
builder.Services.AddZealousMindedPeopleGeoServices(); // Автоматически регистрирует контейнеры
// или отдельно:
builder.Services.AddGeoDataContainers();
```

### Работа с контейнерами

#### Получение и создание контейнеров

```csharp
@inject IGeoDataContainerManager ContainerManager

// Создание или получение контейнера
var container = ContainerManager.GetOrCreateContainer("europe-participants");

// Проверка существования
if (ContainerManager.ContainerExists("europe-participants"))
{
    // Контейнер существует
}

// Получение списка всех контейнеров
var containerIds = ContainerManager.GetContainerIds();
```

#### Добавление участников

```csharp
// Добавление одного участника
var participant = new Participant
{
    Name = "Иван Иванов",
    Email = "ivan@example.com",
    Address = "Москва, Россия",
    Latitude = 55.7558,
    Longitude = 37.6176
};

var result = await container.AddParticipantAsync(participant);

// Добавление нескольких участников
var participants = new List<Participant> { ... };
var result = await container.AddParticipantsAsync(participants);
```

#### Получение данных

```csharp
// Получение всех участников из контейнера
var allParticipants = await container.GetAllParticipantsAsync();

// Получение участника по ID
var participant = await container.GetParticipantByIdAsync(participantId);

// Получение количества участников
int count = container.Count;
```

### Загрузка данных из JSON

```csharp
@inject IGeoDataContainerManager ContainerManager

// Загрузка из JSON файла
var result = await ContainerManager.LoadFromJsonFileAsync("my-container", "data/participants.json");

// Загрузка из JSON строки
var jsonContent = "[{\"name\": \"Test\", \"latitude\": 55.7558, \"longitude\": 37.6176}]";
var result = await ContainerManager.LoadFromJsonAsync("my-container", jsonContent);
```

### Сохранение данных в JSON

```csharp
// Экспорт в JSON строку
var json = await ContainerManager.ExportToJsonAsync("my-container");

// Сохранение в файл
var result = await ContainerManager.SaveToJsonFileAsync("my-container", "data/export.json");
```

### Интеграция с глобусом

#### Использование GlobeDataInitializer

```csharp
@inject GlobeDataInitializer DataInitializer

// Инициализация глобуса с данными из контейнера
var result = await DataInitializer.InitializeGlobeWithDataAsync(
    globeId: "europe",
    htmlContainerId: "globe-europe",
    dataContainerId: "europe-participants",
    options: new GlobeOptions { Width = 800, Height = 600 }
);

// Добавление участника через форму с автоматическим отображением на глобусе
var addResult = await DataInitializer.AddParticipantToGlobeAsync(
    globeId: "europe",
    htmlContainerId: "globe-europe",
    dataContainerId: "europe-participants",
    participant: newParticipant
);
```

#### Прямая загрузка данных в глобус

```csharp
@inject IGeoDataContainerManager ContainerManager
@inject IGlobeMediator GlobeMediator

// Загрузка данных из контейнера в глобус
var result = await ContainerManager.LoadToGlobeAsync(
    containerId: "europe-participants",
    globeMediator: GlobeMediator,
    globeContainerId: "globe-europe"
);
```

### Использование формы для добавления точек

```razor
@using ZealousMindedPeopleGeo.Components

<GeoDataParticipantForm
    DataContainerId="europe-participants"
    GlobeId="europe"
    Title="Add Point to Europe Map"
    SubmitButtonText="Add to Map"
    OnParticipantAdded="HandleParticipantAdded" />

@code {
    private async Task HandleParticipantAdded(Participant participant)
    {
        Console.WriteLine($"Added: {participant.Name}");
    }
}
```

### Подписка на изменения данных

```csharp
@inject IGeoDataContainerManager ContainerManager

protected override void OnInitialized()
{
    ContainerManager.OnDataChanged += HandleDataChanged;
}

private void HandleDataChanged(string containerId, GeoDataChangeType changeType)
{
    Console.WriteLine($"Container '{containerId}' changed: {changeType}");
    // GeoDataChangeType: Added, Updated, Removed, Cleared, BulkLoaded
}
```

## 🗄️ Хранение гео-данных в базе данных

Помимо хранения в памяти, библиотека поддерживает постоянное хранение гео-данных в реляционной базе данных через [Entity Framework Core](https://learn.microsoft.com/ef/core/). Реализация **не зависит от конкретного провайдера БД** — вы выбираете провайдер (SQLite, PostgreSQL, SQL Server и т. д.) при регистрации сервисов.

`DatabaseGeoDataContainerManager` реализует тот же интерфейс `IGeoDataContainerManager`, что и хранилище в памяти, поэтому весь код работы с контейнерами (добавление по одному, загрузка массивом, загрузка/выгрузка JSON, несколько глобусов, подписка на изменения) остаётся неизменным — меняется только способ регистрации.

### Регистрация сервисов

```csharp
// В Program.cs — выберите любой провайдер EF Core
builder.Services.AddGeoDataDatabase(options =>
    options.UseSqlite("Data Source=geodata.db"));

// Пример для PostgreSQL:
// builder.Services.AddGeoDataDatabase(options =>
//     options.UseNpgsql(builder.Configuration.GetConnectionString("GeoData")));

var app = builder.Build();

// Создание схемы БД при старте (для разработки/демо).
// В продакшене используйте миграции EF Core.
await app.Services.EnsureGeoDataDatabaseCreatedAsync();
```

Для управления схемой в продакшене подключите [миграции EF Core](https://learn.microsoft.com/ef/core/managing-schemas/migrations/) к контексту `GeoDataDbContext` вместо `EnsureGeoDataDatabaseCreatedAsync`.

### Работа с несколькими глобусами

Каждый именованный контейнер (`containerId`) соответствует отдельному глобусу. Данные разных глобусов изолированы друг от друга в одной таблице за счёт колонки `ContainerId` (составной ключ `ContainerId` + `Id`), поэтому один и тот же участник может присутствовать в разных глобусах.

```csharp
@inject IGeoDataContainerManager ContainerManager

// Данные сохраняются в БД и переживают перезапуск приложения
var europe = ContainerManager.GetOrCreateContainer("europe");
var asia = ContainerManager.GetOrCreateContainer("asia");

// Добавление по одному
await europe.AddParticipantAsync(new Participant { Name = "Берлин", Latitude = 52.52, Longitude = 13.405 });

// Загрузка массивом (дубликаты по Id пропускаются)
await asia.AddParticipantsAsync(new[]
{
    new Participant { Name = "Токио", Latitude = 35.6762, Longitude = 139.6503 },
    new Participant { Name = "Дели", Latitude = 28.6139, Longitude = 77.2090 }
});

// Список всех глобусов, для которых есть данные в БД
var globeIds = ContainerManager.GetContainerIds();
```

### Загрузка и выгрузка JSON

API загрузки/выгрузки JSON идентичен хранилищу в памяти, но данные читаются и пишутся в БД:

```csharp
// Загрузка массива участников из JSON-строки прямо в БД
await ContainerManager.LoadFromJsonAsync("europe", jsonContent);

// Загрузка из файла
await ContainerManager.LoadFromJsonFileAsync("europe", "data/europe.json");

// Выгрузка содержимого глобуса из БД в JSON
var json = await ContainerManager.ExportToJsonAsync("europe");
```

> 💡 Полный пример Blazor-страницы см. в [`examples/GeoDataDatabaseExample.razor`](examples/GeoDataDatabaseExample.razor).

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
- **CommunityGlobeSettings** - Панель настроек глобуса с JSON сохранением/загрузкой

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
│   ├── CommunityGlobeParticipantManager.razor # Управление участниками
│   └── GeoDataParticipantForm.razor       # Форма для добавления точек
├── Services/            # Бизнес-логика сервисы
│   ├── Mapping/                          # Сервисы для работы с картами
│   │   ├── ThreeJsGlobeService.cs        # Управление 3D сценой
│   │   ├── GlobeMediatorService.cs       # Посредник Blazor-JavaScript
│   │   └── IGlobeMediator.cs             # Интерфейс посредника
│   ├── GeoDataContainer/                 # Именованные контейнеры данных
│   │   ├── IGeoDataContainer.cs          # Интерфейс контейнера
│   │   ├── IGeoDataContainerManager.cs   # Менеджер контейнеров
│   │   ├── InMemoryGeoDataContainer.cs   # In-Memory реализация
│   │   ├── GeoDataContainerManager.cs    # Реализация менеджера
│   │   └── GeoDataLoaderExtensions.cs    # Расширения для загрузки
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

# Результат: 0 предупреждений, 0 ошибок
```

### Качество кода

- ✅ Нулевые предупреждения компилятора
- ✅ Соответствие стандартам .NET 9
- ✅ Правильная обработка nullable типов
- ✅ Оптимизированные async/await паттерны
- ✅ Корректная работа с памятью и ресурсами


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
