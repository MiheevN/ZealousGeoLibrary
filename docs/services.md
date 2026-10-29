# Модели, сервисы и ошибки

[← Документация](README.md) · Раздел для C# / Blazor

Модели данных и сервисы, которые можно получить через DI, кэширование, коды ошибок и журнал.

## Модели данных

### Participant
Основная модель участника сообщества:
```csharp
public class Participant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";   // необязателен, указанный виден всем на карте
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

### GlobeOptions
Настройки для 3D глобуса:
```csharp
public class GlobeOptions
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public string BackgroundColor { get; set; } = "#000011";
    public bool AutoRotate { get; set; } = true;
    public double AutoRotateSpeed { get; set; } = 0.1;      // радиан в секунду
    public bool EnableMouseControls { get; set; } = true;
    public bool EnableZoom { get; set; } = true;
    public double MinZoom { get; set; } = 1.03;
    public double MaxZoom { get; set; } = 4.0;
    public int LevelOfDetail { get; set; } = 2;
    public string TextureQuality { get; set; } = "auto";   // auto | high (8K) | standard (4K)
    public double CloudsOpacity { get; set; } = 0.85;       // плотность облаков
    public string AtmosphereColor { get; set; } = "#4fb4ff";
    public double AtmosphereOpacity { get; set; } = 0.7;    // яркость свечения по краю
    public bool EnableStars { get; set; } = true;
    public bool SunLightFollowCamera { get; set; } = true;
    public double SunLightOffsetDegrees { get; set; } = 30; // свет сбоку, а не из-за камеры
    public double SunLightDistance { get; set; } = 6.0;
    public double SunLightIntensity { get; set; } = 2.6;
    public double AmbientLightIntensity { get; set; } = 0.3;
    public double HemisphereLightIntensity { get; set; } = 0.25;
}
```

Текстуры Земли встроены: карта дня 8K на компьютере и 4K на телефоне и слабых видеокартах
(`TextureQuality = "auto"`; 8K-текстура занимает в видеопамяти около 180 МБ), рельеф,
шероховатость (суша матовая, океан блестит) и маска облаков — 4K. Свои текстуры задаются
в `EarthTextureUrl`, `NormalTextureUrl`, `RoughnessTextureUrl` и `CloudsTextureUrl`; маска
облаков — белые облака на чёрном. Другую планету целиком задаёт `GlobeOptions.Planet`
(см. [Своя планета](components.md#своя-планета)).

## Сервисы

### IGeoDataContainerManager и IParticipantRepository
Хранилища точек и участников. Встроены реализации в памяти, постоянное хранение
реализует приложение (см. [Своё хранилище](storage.md)).

### IGeocodingService
Поиск адреса для `GeoDataParticipantForm`:
```csharp
public interface IGeocodingService
{
    ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default);
    ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);
    bool ValidateCoordinates(double latitude, double longitude);
    ValueTask<bool> IsAvailableAsync(CancellationToken ct = default);
}
```

Без ключа Google работает `NominatimGeocodingService` (OpenStreetMap): адрес приходит на
языке `CurrentUICulture`, ответы кэшируются на сутки, к одному серверу уходит не больше
запроса в секунду, а в `GeocodingResult.Attribution` лежит подпись источника
(«Data © OpenStreetMap contributors, ODbL 1.0») — форма показывает её под найденным адресом.
С ключом работает `GoogleMapsGeocodingService`. Свой геокодер зарегистрируйте как
`IGeocodingService` до вызова `AddZealousMindedPeopleGeo` — библиотека его не заменит.

### IThreeJsGlobeService
Мост из C# в скрипт глобуса `community-globe.js`: каждый метод вызывает одноимённую функцию
модуля (см. [JavaScript в браузере](javascript.md)). Компоненты пользуются им через
`IGlobeMediator`, напрямую он нужен для своего интерфейса глобуса. Основные методы:

```csharp
public interface IThreeJsGlobeService
{
    ValueTask<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options, CancellationToken ct = default);
    ValueTask<GlobeOperationResult> AddPointsAsync(string containerId, IEnumerable<GeoPoint> points, IReadOnlyDictionary<string, string>? categoryColors = null, CancellationToken ct = default);
    ValueTask<GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Participant> participants, CancellationToken ct = default);
    ValueTask<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0, CancellationToken ct = default);
    ValueTask<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5, CancellationToken ct = default);
    ValueTask<GlobeState> GetStateAsync(string containerId, CancellationToken ct = default);
    ValueTask<GlobeOperationResult> DisposeAsync(string containerId, CancellationToken ct = default);
    // ...и другие: IThreeJsGlobeService.cs
}
```

### Состав библиотеки

| Компонент или сервис | Что делает |
|---|---|
| `CommunityGlobeComponent` | Глобус вместе с панелями: управление, участники, настройки |
| `CommunityGlobeViewer` | Только глобус |
| `CommunityGlobeControls` | Панель управления глобусом |
| `CommunityGlobeParticipantManager` | Панель участников |
| `CommunityGlobeSettings` | Панель настроек глобуса, сохранение и загрузка в JSON |
| `CommunityMapComponent` | 2D-карта |
| `GeoDataParticipantForm` | Форма добавления точки с поиском адреса |
| `PwaManagerComponent` | Манифест, установка приложения, сервис-воркер |
| `ThreeJsGlobeService` | Вызывает скрипт глобуса из C# |
| `GlobeMediatorService` (`IGlobeMediator`) | Посредник между компонентами и глобусом: состояние, события готовности |
| `InMemoryParticipantRepository` | Участники в памяти |

## Кэширование

`ICachingService` хранит ответы в `IMemoryCache`; через него библиотека кэширует, например,
найденные адреса. Готовые методы расширения:

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

## Обработка ошибок

Операции с данными не бросают исключений на неверный ввод, а возвращают результат с причиной:

```csharp
var result = await ContainerManager.GetOrCreateContainer("team").AddPointAsync(point);

if (!result.Success)
{
    // Почему точку не приняли: неверные координаты, слишком длинное поле, повтор Id
    var errorMessage = result.ErrorMessage;
}
```

### Глобус и геокодирование

Если 3D-глобус не запустился или адрес не нашёлся, компоненты показывают понятную причину
на языке пользователя. Технические подробности уходят только в журнал. Без WebGL глобус
просит включить аппаратное ускорение или открыть страницу в другом браузере и не предлагает
«Повторить». Свой интерфейс строится по кодам:

```csharp
var started = await GlobeMediator.InitializeGlobeAsync("globe-team", options);
if (started.Error == GlobeInitializationError.WebGlUnavailable)
{
    // Показать 2D-карту вместо глобуса. started.ErrorMessage — технический текст для журнала.
}

var found = await GeocodingService.GeocodeAddressAsync(address);
// found.Error: NotFound — уточнить адрес; Unavailable — поиск выключен или сервер отказал
// приложению (неверный ключ Google, блокировка Nominatim); Failed — сбой сети или сервиса, можно повторить (такой ответ не кэшируется).
```

### Журнал

Библиотека пишет в `ILogger`: ошибки и предупреждения — на уровнях `Error` и `Warning`,
ход обычных операций (загрузка точек, инициализация глобуса) — на уровне `Debug`.
Чтобы увидеть подробности, включите `Debug` для категории `ZealousMindedPeopleGeo`:

```json
"Logging": { "LogLevel": { "ZealousMindedPeopleGeo": "Debug" } }
```

Журнал скриптов в консоли браузера описан в [JavaScript в браузере](javascript.md#журнал-в-консоли-браузера).
