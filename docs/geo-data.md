# Гео-данные: точки и контейнеры

[← Документация](README.md) · Раздел для C# / Blazor

Точки (`GeoPoint`), именованные контейнеры, загрузка и выгрузка JSON и GeoJSON, проверка точек и синхронизация между пользователями.

## Точки и контейнеры

Контейнер — именованный набор точек: данные одного глобуса, одной карты или одного
контекста («офисы», «события», «датчики»). Точка (`GeoPoint`) — это что угодно с
координатами, не обязательно человек:

| Поле | Что это |
|---|---|
| `Id` | Строка до 128 символов: GUID, номер из вашей системы, `moscow-office`. По умолчанию — новый GUID |
| `Latitude`, `Longitude` | Координаты, −90…90 и −180…180 |
| `Title`, `Description` | Подпись и описание |
| `Category` | Группа точки: `office`, `event`, `sensor` |
| `Color`, `Icon`, `Url` | Цвет маркера (CSS), иконка, ссылка |
| `Properties` | Любые свои поля, `Dictionary<string, string>` |

Карта и глобус красят маркеры по категориям, карта показывает легенду и карточку
точки (см. [Любые точки: категории, легенда, карточка](components.md#любые-точки-категории-легенда-карточка)).

### Регистрация сервисов

```csharp
// В Program.cs
builder.Services.AddZealousMindedPeopleGeo(); // Регистрирует и контейнеры гео-данных
// или только контейнеры:
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

#### Точки

```csharp
var offices = ContainerManager.GetOrCreateContainer("offices");

await offices.AddPointAsync(new GeoPoint
{
    Id = "moscow",
    Latitude = 55.7558,
    Longitude = 37.6176,
    Title = "Офис в Москве",
    Category = "office",
    Url = "https://example.com/offices/moscow",
    Properties = { ["staff"] = "120", ["opened"] = "2019-04-01" }
});

// Массивом: null, точки с неверными координатами и повторы Id пропускаются
var result = await offices.AddPointsAsync(points);
Console.WriteLine($"Добавлено {result.ProcessedCount}, пропущено {result.SkippedCount}");

var all = await offices.GetPointsAsync();          // копии: их изменение не трогает контейнер
var moscow = await offices.GetPointAsync("moscow");
moscow!.Properties["staff"] = "130";
await offices.UpdatePointAsync(moscow);            // заменяет точку целиком
await offices.RemovePointAsync("moscow");

// Заменить всё содержимое контейнера
await ContainerManager.LoadPointsAsync("offices", points);
```

Точку с неверными координатами (вне диапазона, `NaN`), пустым или слишком длинным `Id`
контейнер не сохранит и вернёт ошибку в `GeoDataOperationResult.ErrorMessage`.

#### Участники сообщества

Участника (`Participant`) контейнер хранит как точку: имя становится `Title`, сообщение —
`Description`, остальные поля — `Properties` (ключи в `ParticipantPointProperties`). Код,
написанный для участников, работает как раньше:

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

// Заменить содержимое контейнера участниками
await ContainerManager.LoadDataAsync("europe-participants", participants);
```

Участник без координат в контейнер не попадает. Точку, добавленную не как участник,
методы участников тоже видят: если её `Id` не GUID, участник получает GUID, вычисленный
из `Id`, и по нему же её можно найти, изменить или удалить. Перевести участника в точку и
обратно можно напрямую: `participant.ToGeoPoint()` и `point.ToParticipant()`.

#### Получение участников

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
var result = await ContainerManager.LoadFromJsonAsync("my-container", jsonContent);
```

JSON — массив точек. Участники в прежнем формате тоже читаются, форматы можно смешивать:
элемент с полем `title` или `properties` — точка, остальные — участники.

```json
[
  { "id": "berlin", "latitude": 52.52, "longitude": 13.405, "title": "Berlin",
    "category": "office", "properties": { "staff": "40" } },
  { "name": "Иван", "email": "ivan@example.com", "latitude": 55.75, "longitude": 37.62 }
]
```

Элементы без `latitude` и `longitude` пропускаются (`SkippedCount`): иначе точка молча
оказалась бы в (0, 0).

### Сохранение данных в JSON

```csharp
// Экспорт в JSON строку — массив точек; поля участников лежат в "properties"
var json = await ContainerManager.ExportToJsonAsync("my-container");

// Сохранение в файл
var result = await ContainerManager.SaveToJsonFileAsync("my-container", "data/export.json");
```

### GeoJSON

Точки контейнера выгружаются в GeoJSON ([RFC 7946](https://datatracker.ietf.org/doc/html/rfc7946)),
а `LoadFromJsonAsync` и `LoadFromJsonFileAsync` принимают GeoJSON так же, как массив точек:
формат определяется по содержимому.

```csharp
// FeatureCollection строкой и в файл
var geoJson = await ContainerManager.ExportToGeoJsonAsync("offices");
await ContainerManager.SaveToGeoJsonFileAsync("offices", "data/offices.geojson");

// Обратно — тем же методом, что и JSON
var result = await ContainerManager.LoadFromJsonFileAsync("offices", "data/offices.geojson");
// result.SkippedCount — линии, полигоны и объекты с неверными координатами

// Без контейнера
string text = GeoPointGeoJson.Write(points);
GeoJsonReadResult read = GeoPointGeoJson.Read(text); // read.Points, read.Skipped — причины пропусков
```

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "id": "berlin",
      "geometry": { "type": "Point", "coordinates": [13.405, 52.52] },
      "properties": { "title": "Berlin", "category": "office", "marker-color": "#3987e5", "staff": "40" }
    }
  ]
}
```

- **Координаты** в GeoJSON идут в порядке [долгота, широта]. Высота (третья координата)
  отбрасывается, а долгота за ±180° сворачивается в диапазон.
- **Поля точки** записываются в `properties` под именами `title`, `description`, `category`,
  `marker-color`, `icon` и `url`. Заголовок, описание и цвет названы по
  [simplestyle-spec](https://github.com/mapbox/simplestyle-spec), поэтому
  [geojson.io](https://geojson.io) и подобные редакторы сразу показывают подписи и цвета
  маркеров. Остальные `Properties` лежат рядом как есть.
- **Чужие файлы:**
  - `name` подставляется в заголовок, если `title` нет;
  - `properties.id` подставляется в идентификатор, если у объекта нет `id`; без того и
    другого точка получает новый GUID;
  - числа, `true`/`false`, массивы и объекты в свойствах сохраняются строками с их
    JSON-записью.
- **Геометрия.** `MultiPoint` даёт по точке на каждую позицию (`id#1`, `id#2`, …).
  Линии, полигоны, объекты без геометрии и точки, не прошедшие `GeoPoint.Validate()`,
  пропускаются, и причина каждого пропуска есть в `GeoJsonReadResult.Skipped` — на языке
  пользователя (см. [Язык сообщений](localization.md)).
- **Совпадение имён.** Если имя свойства в `Properties` совпадает с полем точки (`title`,
  `url`…), оно записывается, только когда само поле пустое.

На странице `/map` витрины точки можно скачать в GeoJSON и загрузить свой файл.

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

Обязательны имя и адрес. Email можно не указывать; указанный адрес виден всем в подсказке
и карточке точки на карте, о чём форма предупреждает под полем.

#### Кто может добавлять точки

Библиотека не ограничивает добавление точек: контейнеры, база и загрузка JSON принимают
сколько угодно точек — например, сервер может заполнить карту при запуске. Решать, кто и
как часто добавляет точки, должно приложение. Для формы на открытой странице для этого
есть `OnSubmitting`: форма вызывает его с готовым участником (адрес уже найден) перед
сохранением. `null` — сохранить, строка — не сохранять и показать её пользователю; поля
формы при отказе остаются заполненными.

```razor
<GeoDataParticipantForm DataContainerId="community" OnSubmitting="CheckAsync" />

@code {
    [CascadingParameter] private Task<AuthenticationState>? Auth { get; set; }

    private async Task<string?> CheckAsync(Participant participant)
    {
        var user = Auth is null ? null : (await Auth).User;
        if (user?.Identity?.IsAuthenticated != true)
            return "Войдите, чтобы добавить точку";

        return Limiter.TryAdd(user.Identity.Name!) ? null : "Не больше 5 точек в час";
    }
}
```

`Limiter` здесь — ваш сервис; подойдёт и
[`System.Threading.RateLimiting`](https://learn.microsoft.com/dotnet/api/system.threading.ratelimiting).
Если точки приходят через ваш HTTP API, ограничивайте сам эндпоинт —
[middleware ограничения частоты](https://learn.microsoft.com/aspnet/core/performance/rate-limit)
ASP.NET Core.

### Синхронизация между пользователями

Менеджер контейнеров один на всё приложение, поэтому изменения контейнера видят все,
кто открыл страницу. Опроса по таймеру нет: Blazor Server доставляет обновление
по уже открытому соединению.

- `CommunityGlobeViewer` и `CommunityMapComponent` с `DataContainerId` перерисовываются
  сами, как только в контейнер добавили, изменили или удалили точку — неважно, кто:
  этот пользователь, другой или фоновая служба сервера.
- Форма `GeoDataParticipantForm` и панель `CommunityGlobeParticipantManager` с
  `DataContainerId` пишут в контейнер — новая точка появляется у всех.
- Со своим хранилищем (см. [Своё хранилище](storage.md)) то же самое работает, если оно вызывает
  `HandleDataChanged`.
- Несколько серверов с общим хранилищем связывает шина `IGeoDataChangeBus` (ниже).
- Без `DataContainerId` компоненты читают `IParticipantRepository`. Встроенный
  репозиторий в памяти у каждого пользователя свой, поэтому синхронизировать там нечего.

```razor
@* Оба пользователя видят точки друг друга сразу *@
<CommunityGlobeComponent GlobeId="team" DataContainerId="team-points" ShowParticipantManagement="true" />
<CommunityMapComponent MapId="team-map" DataContainerId="team-points" />
```

#### Несколько серверов

Если приложение запущено на нескольких серверах за балансировщиком и хранит точки в
общей базе, каждый сервер узнаёт только о своих изменениях. Связать их можно шиной:
реализуйте `IGeoDataChangeBus` поверх того, что у вас уже есть, — Redis pub/sub,
SignalR backplane, очередь — и зарегистрируйте её. Менеджер контейнеров (встроенный или
унаследованный от `GeoDataContainerManagerBase`) сам отправляет в шину каждое изменение
и передаёт подписчикам `OnDataChanged` изменения с других серверов; свои сообщения,
вернувшиеся из шины, он узнаёт по `Origin` и не повторяет. Без шины всё работает в
пределах одного сервера.

```csharp
// Пример на StackExchange.Redis: одно сообщение — одно изменение.
public sealed class RedisGeoDataChangeBus : IGeoDataChangeBus
{
    private static readonly RedisChannel Channel = RedisChannel.Literal("geo-data-changes");
    private readonly ISubscriber _subscriber;

    public RedisGeoDataChangeBus(IConnectionMultiplexer redis)
    {
        _subscriber = redis.GetSubscriber();
        _subscriber.Subscribe(Channel, (_, message) =>
            ChangeReceived?.Invoke(JsonSerializer.Deserialize<GeoDataChange>(message.ToString())!));
    }

    public event Action<GeoDataChange>? ChangeReceived;

    public async ValueTask PublishAsync(GeoDataChange change, CancellationToken ct = default) =>
        await _subscriber.PublishAsync(Channel, JsonSerializer.Serialize(change));
}

// Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis:6379"));
builder.Services.AddSingleton<IGeoDataChangeBus, RedisGeoDataChangeBus>();
builder.Services.AddZealousMindedPeopleGeo();
```

Сбой шины не мешает сохранению: изменение остаётся в хранилище, ошибка пишется в лог.
С хранилищем в памяти шина бесполезна — у каждого сервера свои данные.

### Подписка на изменения данных

Событие приходит из потока того, кто изменил данные, поэтому в компоненте обновляйтесь
через `InvokeAsync`. Исключение в одном подписчике не мешает остальным.

```csharp
@inject IGeoDataContainerManager ContainerManager

protected override void OnInitialized()
{
    ContainerManager.OnDataChanged += HandleDataChanged;
}

private void HandleDataChanged(string containerId, GeoDataChangeType changeType)
{
    _ = InvokeAsync(StateHasChanged);
    // GeoDataChangeType: Added, Updated, Removed, Cleared, BulkLoaded
}
```

## Валидация

Точку проверяет `GeoPoint.Validate()`: `null`, если всё в порядке, иначе текст ошибки
(см. [Язык сообщений](localization.md)). Контейнеры вызывают её сами — `AddPointAsync` и `UpdatePointAsync`
не сохраняют некорректную точку и возвращают ошибку в `ErrorMessage`, а `AddPointsAsync`
пропускает такие точки.

```csharp
var error = point.Validate(); // широта −90…90, долгота −180…180, Id и длины полей
if (error is not null)
{
    // показать пользователю
}
```

`GeoDataParticipantForm` проверяет ввод атрибутами `ParticipantRegistrationModel`
(`DataAnnotationsValidator`): имя и адрес обязательны, email необязателен, но если указан —
должен быть похож на адрес.
