# Zealous Minded People Geography Library

Библиотека для создания интерактивных 3D географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

[![NuGet](https://img.shields.io/nuget/v/ZealousMindedPeopleGeo.svg)](https://www.nuget.org/packages/ZealousMindedPeopleGeo/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🌟 Возможности

- **3D глобус сообщества** - Трехмерная визуализация планеты с интерактивными точками участников
- **Настройки глобуса** - Полная настройка параметров глобуса (размеры, освещение, атмосфера, облака)
- **Динамическое управление** - Включение/выключение атмосферы и облаков в реальном времени
- **Множественные глобусы** - Поддержка нескольких независимых 3D глобусов на одной странице
- **Именованные контейнеры гео-данных** - Любые точки (`GeoPoint`: офисы, события, датчики, участники) в именованных наборах для разных глобусов и карт, в памяти или в БД
- **Управление состоянием** - Централизованное управление состоянием всех глобусов
- **Загрузка/сохранение данных** - Удобные интерфейсы для инициализации и сохранения данных из JSON и других источников
- **In-Memory репозиторий** - Простое хранение данных участников в памяти
- **Модульная архитектура** - Четкое разделение ответственности между компонентами
- **Адаптивный дизайн** - Адаптация под различные размеры экрана и устройства

## 🚀 Быстрый старт

### 1. Установка

```bash
dotnet add package ZealousMindedPeopleGeo
```

### Тестовая среда

В репозитории есть хост `hosts/ZealousMindedPeopleGeo.Showcase`, чтобы посмотреть
компоненты без боевого сайта. Библиотека подключена к нему ссылкой на проект,
поэтому правки компонентов, CSS и JS видны после пересборки.

```bash
dotnet watch --project hosts/ZealousMindedPeopleGeo.Showcase
```

http://localhost:5290 — обзор, `/map` — 2D карта с переключением проекции,
центрального меридиана и набора данных, `/globe` — 3D глобус, `/showcase` — все
компоненты на одной странице.

Хост настроен так, как достаточно любому сайту: ссылка на библиотеку и
`AddZealousMindedPeopleGeo()` в `Program.cs`, в `Components/App.razor` подключений
библиотеки нет (см. «Встраивание в проект»).

### Тесты

Два набора: .NET-тесты библиотеки и Node-тесты JavaScript, CSS и разметки.

```bash
dotnet test tests/ZealousMindedPeopleGeo.Tests
node --test experiments/*.test.mjs
```

`node --test experiments/` без маски на Node 22 не работает: Node принимает папку за модуль.

Что проверяют .NET-тесты (`tests/ZealousMindedPeopleGeo.Tests`):

- `GeoData/` — общий контракт хранилищ гео-данных. Одни и те же тесты прогоняются
  на хранилище в памяти и в БД (SQLite in-memory), поэтому реализации не расходятся
  в результатах операций и событиях `OnDataChanged`;
- `Components/` — Razor-компоненты через [bUnit](https://bunit.dev): какие параметры
  уходят в JavaScript и что видит пользователь;
- `Validation/` — правила `ParticipantValidator` и связанных валидаторов;
- `Models/` — демонстрационные наборы, в том числе что они проходят валидацию;
- `Services/` — регистрация сервисов и `PwaService`.

Покрытие кода (отчёт Cobertura появится в `tests/ZealousMindedPeopleGeo.Tests/TestResults/`):

```bash
dotnet test tests/ZealousMindedPeopleGeo.Tests --collect:"XPlat Code Coverage"
```

## 📦 Встраивание в проект

Достаточно двух шагов.

1. **Установите пакет**
   ```bash
   dotnet add package ZealousMindedPeopleGeo
   ```

2. **Зарегистрируйте сервисы в `Program.cs`**
   ```csharp
   builder.Services.AddZealousMindedPeopleGeo();
   ```

Этого хватает всем компонентам. Варианты с настройками регистрируют тот же набор сервисов:

```csharp
// секция "ZealousMindedPeopleGeo" в appsettings.json
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);

// настройки в коде
builder.Services.AddZealousMindedPeopleGeo(options => options.EnableGeocoding = false);
```

Какие настройки есть и когда они нужны — в разделе «Конфигурация».

Участники хранятся в памяти, пока в настройках не указан `GoogleSheetId`, тогда — в Google
Sheets. Свой `IParticipantRepository` или хранилище гео-данных в БД (`AddGeoDataDatabase`)
можно зарегистрировать до или после: библиотека их не перезапишет, а повторный вызов
`AddZealousMindedPeopleGeo` ничего не дублирует.

Компоненты интерактивные, поэтому приложению нужен интерактивный рендеринг Blazor
(`AddInteractiveServerComponents()` и `@rendermode="InteractiveServer"`, как в шаблоне
Blazor Web App).

### Что библиотека подключает сама

- **Стили** (`zealous-geo.css`). Blazor сам загружает JS-инициализатор библиотеки
  `ZealousMindedPeopleGeo.lib.module.js`, и тот добавляет стили в `<head>` после Bootstrap
  и до стилей приложения. Чтобы стили были уже при первой отрисовке, без мелькания, их
  можно подключить и вручную, второй раз они не добавятся:
  ```html
  <link rel="stylesheet" href="_content/ZealousMindedPeopleGeo/css/zealous-geo.css" />
  ```
- **Скрипты** карты, глобуса и PWA: компоненты загружают их модулями.
- **Манифест PWA, цвет темы и иконку** добавляет `PwaManagerComponent`, если у приложения
  нет своего манифеста.

### Необязательно

- **Bootstrap 5.** Компоненты размечены его классами и с ним выглядят как задумано; без него
  остаются рабочими, но оформлены проще.
- **Офлайн-кэш.** Сервис-воркер библиотеки регистрируется, только если сервер отдаёт `sw.js`
  с заголовком `Service-Worker-Allowed` (см. «PWA и сервис-воркер»). Без заголовка сайт и
  установка приложения работают.

### Использование компонентов

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

#### 2D карта: проекция и центральный меридиан

`CommunityMapComponent` рисует карту в равновеликой проекции
[Equal Earth](https://doi.org/10.1080/13658816.2018.1504949): площади материков
сохраняются, а карта выглядит привычно. При `zoom = 1` мир целиком вписан в окно;
при приближении карту можно двигать, пока её край не упрётся в край окна.

Приближают карту колесом мыши, кнопками «+» и «−», а на телефоне и тачпаде — щипком
двумя пальцами: место между пальцами остаётся под ними, и двумя же пальцами карту можно
двигать. Страница при этом не масштабируется.

Как опция доступна прежняя равнопромежуточная проекция (`Equirectangular`) —
прямоугольная карта, которая бесконечно прокручивается по горизонтали.
Центральный меридиан задаётся в градусах: `0` — Гринвич, `150` — Тихий океан в центре.

```razor
@using ZealousMindedPeopleGeo.Models

<!-- Equal Earth с Тихим океаном в центре -->
<CommunityMapComponent MapId="map-pacific" CentralMeridian="150" />

<!-- Прежняя прямоугольная проекция -->
<CommunityMapComponent MapId="map-flat" Projection="MapProjection.Equirectangular" />

<!-- Начальный вид: весь мир -->
<CommunityMapComponent MapId="map-world" Zoom="1" />
```

Начальный вид задают параметры `Zoom`, `CenterLatitude` и `CenterLongitude`; без них
центр по долготе совпадает с центральным меридианом.

Список точек стоит справа от карты, а в узком контейнере (до 640 px) — под ней, и
карту не перекрывает. Кнопка в шапке («Members», подпись задаёт `ListTitle`) скрывает и
показывает его, карта при этом занимает освободившееся место. `ParticipantsListOpen="false"`
открывает карту со свёрнутым списком, `ShowParticipantsList="false"` убирает список и
кнопку совсем.

#### Любые точки: категории, легенда, карточка

Карта показывает не только участников, а любые точки (`GeoPoint`, см. «Именованные
контейнеры гео-данных»). Источник — первый заданный из `Points`, `Participants`,
`DataContainerId`, иначе общий репозиторий участников.

```razor
<CommunityMapComponent MapId="offices-map"
                       Points="_offices"
                       Title="Офисы"
                       ListTitle="Офисы"
                       OnPointClick="OpenOffice" />

<!-- Точки из контейнера: карта сама обновится, когда в нём что-то изменится -->
<CommunityMapComponent MapId="events-map" DataContainerId="events" />
```

Новые `Points` или `Participants` карта принимает без пересоздания.

- **Цвета.** Маркер красится собственным `Color` точки, иначе цветом её категории.
  Цвета категорий можно задать словарём `CategoryColors`, иначе первые три категории
  в порядке появления получают синий, оранжевый и бирюзовый. Только три цвета
  одновременно на тёмной карте остаются различимыми попарно для людей с любым
  цветовосприятием. Остальные категории и точки без категории — серые. Если категорий
  нет совсем, все маркеры фирменного бирюзового цвета. Те же цвета у глобуса
  (`GeoPointPalette` — одна логика на оба).
- **Легенда.** Над картой, если у точек есть категории, с числом точек в каждой.
  Щелчок по категории скрывает и показывает её точки на карте и в списке, цвета
  остальных при этом не меняются. `ShowLegend="false"` убирает легенду.
- **Маркер.** Внутри — короткая `Icon` (символ, эмодзи) или первая буква `Title`.
- **Карточка.** Открывается по клику на маркер или строку списка. Встроенная показывает
  категорию, описание, свойства, координаты и ссылку `Url` (только `http`, `https` и
  `mailto`). Свою разметку задаёт `PointTemplate`:

```razor
<CommunityMapComponent Points="_offices">
    <PointTemplate Context="point">
        <h5>@point.Title</h5>
        <p>Сотрудников: @point.Properties.GetValueOrDefault("staff")</p>
    </PointTemplate>
</CommunityMapComponent>
```

`OnPointClick` получает копию точки, `OnMarkerClick` — её же в виде участника.

#### Группы близких точек

Маркеры, которые на текущем масштабе налезли бы друг на друга, сливаются в значок
с числом точек. Кольцо значка поделено между цветами его точек, а подсказка
перечисляет первые восемь заголовков. Клик по значку приближает карту ровно
настолько, чтобы группа распалась. Точки с одинаковыми или почти одинаковыми
координатами не разойдутся и на наибольшем приближении: клик раскладывает их
веером вокруг значка, клик мимо сворачивает веер. При перетаскивании группы не
пересчитываются и не прыгают.

```razor
<!-- Сливать маркеры, центры которых ближе 60 пикселей -->
<CommunityMapComponent Points="_offices" ClusterRadius="60" />

<!-- Без групп -->
<CommunityMapComponent Points="_offices" ClusterPoints="false" />
```

`ClusterRadius` — расстояние между центрами маркеров в пикселях (0–200). По
умолчанию 24: сливаются только маркеры, которые иначе соприкоснулись бы, и меньшее
значение этого не отменяет. Оба параметра меняются на лету, без пересоздания
карты. Точка, на которую сфокусирована карта (`focusOnParticipant`), в группу не
прячется.

Значения по умолчанию для всех карт задаются в `ZealousMindedPeopleGeoOptions.Map`.
Центр и зум стоит указать явно: у `MapConfiguration` они по умолчанию равны
Москве и `DefaultZoom = 10`.

```csharp
builder.Services.Configure<ZealousMindedPeopleGeoOptions>(options =>
{
    options.Map = new MapConfiguration
    {
        Projection = MapProjection.EqualEarth,
        CentralMeridian = 150,
        DefaultLatitude = 20,
        DefaultLongitude = 150,
        DefaultZoom = 1
    };
});
```

Без Blazor карта инициализируется напрямую из JavaScript:

```js
initializeCommunityMap('', 20, 0, 1, 'map', { projection: 'equalEarth', centralMeridian: 150, clusterRadius: 40 });
loadPointsOnMap([{ id: 'berlin', latitude: 52.52, longitude: 13.405, title: 'Berlin', color: '#3987e5' }], 'map');
setCommunityMapClustering({ clustering: false }, 'map');
```

`loadPointsOnMap` принимает и GeoJSON (`FeatureCollection` или `Feature`) по тем же
правилам, что `GeoPointGeoJson.Read` на сервере. `loadParticipantsOnMap` по-прежнему
принимает участников в прежнем формате.

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

#### Глобус с точками из контейнера

Глобус читает точки контейнера и красит маркеры по категориям теми же цветами, что и
2D-карта. Если у точек нет ни категорий, ни своих цветов, маркеры остаются цвета из
настроек глобуса.

```razor
<CommunityGlobeViewer GlobeId="offices" DataContainerId="offices"
                      CategoryColors="@(new Dictionary<string, string> { ["hq"] = "#ffcf5a" })" />
```

Подписи точек на глобусе не налезают друг на друга. Подпись встаёт над маркером, а если
там занято — под ним или сбоку; если места нет нигде, плавно гаснет и появляется снова,
когда место освободится. Подпись маркера под курсором показывается всегда, подписи на
видимой стороне Земли важнее подписей у горизонта и за ним. Уже стоящая подпись держит
своё место, поэтому при вращении подписи не прыгают. Включено по умолчанию; выключить
или изменить зазор между подписями можно опциями JS-глобуса
`participantLabelCollisionAvoidance` (`true`) и `participantLabelCollisionPadding`
(в пикселях, `4`).

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

Настройки необязательны: без них работают глобус, карта, контейнеры гео-данных и PWA,
участники хранятся в памяти. Ключ Google нужен только для поиска координат по адресу,
таблица Google — только чтобы хранить участников в ней.

Библиотека читает секцию `ZealousMindedPeopleGeo`, если сервисы зарегистрированы с
конфигурацией:

```csharp
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);
```

```json
{
  "ZealousMindedPeopleGeo": {
    "GoogleMapsApiKey": "",
    "EnableGeocoding": true,
    "GoogleSheetId": "",
    "Map": {
      "Projection": "EqualEarth",
      "CentralMeridian": 0,
      "DefaultLatitude": 20,
      "DefaultLongitude": 0,
      "DefaultZoom": 1
    }
  }
}
```

| Ключ | По умолчанию | Что делает |
|---|---|---|
| `GoogleMapsApiKey` | пусто | Ключ Google Maps Platform с включённым Geocoding API. По нему `GeoDataParticipantForm` и `IParticipantService.RegisterParticipantAsync` находят координаты адреса. Без ключа форма сообщает, что ключ не настроен, и точку не добавляет. Карта и глобус ключ не используют. |
| `EnableGeocoding` | `true` | `false` выключает запросы к Google, даже если ключ задан. |
| `GoogleSheetId` | пусто | ID таблицы Google — часть адреса между `/d/` и `/edit`. Если задан, участники (`IParticipantRepository`) хранятся в этой таблице, иначе в памяти. |
| `GoogleServiceAccountKey` | пусто | Содержимое JSON-ключа сервисного аккаунта Google (сам JSON, не путь к файлу). Обязателен вместе с `GoogleSheetId`. |
| `Map:Projection` | `EqualEarth` | Проекция 2D-карты: `EqualEarth` или `Equirectangular`. |
| `Map:CentralMeridian` | `0` | Центральный меридиан 2D-карты в градусах, от −180 до 180. |
| `Map:DefaultLatitude`, `Map:DefaultLongitude`, `Map:DefaultZoom` | см. ниже | Начальный вид 2D-карты; `DefaultZoom = 1` — мир целиком. |
| `Map:ClusterPoints` | `true` | Сливать близкие маркеры 2D-карты в группы (см. «Группы близких точек»). |
| `Map:ClusterRadius` | `24` | Расстояние между центрами маркеров в пикселях (0–200), ближе которого они сливаются. |

Без секции `Map` карта открывается на широте 20 и центральном меридиане с зумом 2. Если
секция `Map` задана, укажите в ней центр и зум явно: иначе действуют значения класса
`MapConfiguration` — Москва и `DefaultZoom = 10`. Параметры `CommunityMapComponent`
(`Projection`, `CentralMeridian`, `Zoom`, `CenterLatitude`, `CenterLongitude`,
`ClusterPoints`, `ClusterRadius`) важнее настроек.

В `ZealousMindedPeopleGeoOptions` есть ещё `EnableParticipantValidation`,
`EnableRateLimiting`, `MaxParticipantsPerHour`, `DefaultCulture` и `Map:MapTheme`, но
библиотека их пока не читает. Глобус настраивается параметрами компонентов и панелью
настроек (см. «Настройки глобуса»), длительность кэша — в коде (см. «Кэширование»).

### Ключи и секреты

Не храните ключ Google и JSON сервисного аккаунта в `appsettings.json` в репозитории.
Для разработки подойдут user secrets, на сервере — переменные окружения (двоеточие в
имени ключа заменяется на `__`):

```bash
dotnet user-secrets init
dotnet user-secrets set "ZealousMindedPeopleGeo:GoogleMapsApiKey" "<ключ>"
dotnet user-secrets set "ZealousMindedPeopleGeo:GoogleServiceAccountKey" "$(cat service-account.json)"

export ZealousMindedPeopleGeo__GoogleMapsApiKey="<ключ>"
```

Ключ используется только на сервере: компоненты не передают его в браузер. В Blazor
WebAssembly настройки загружает сам браузер, поэтому ключ из них виден пользователям.

### Участники в Google Sheets

1. Создайте сервисный аккаунт в Google Cloud, включите для проекта Google Sheets API и
   скачайте JSON-ключ аккаунта.
2. Откройте таблицу на редактирование для адреса аккаунта — поле `client_email` в JSON.
3. Задайте `GoogleSheetId` и `GoogleServiceAccountKey`.

Участники пишутся на лист `Sheet1`, столбцы A–I: время регистрации, имя, адрес, широта,
долгота, город, страна, соцсети, сообщение. В русской локали Google называет первый лист
«Лист1» — переименуйте его в `Sheet1`. Первая строка — заголовки, данные читаются со
второй. Заголовки записывает `IParticipantService.InitializeStorageAsync()`; без его
вызова оставьте первую строку под заголовки сами. Таблица поддерживает только добавление
и чтение: изменение и удаление участников возвращают ошибку.

## 📱 PWA и сервис-воркер

Офлайн-кэш необязателен. `PwaManagerComponent` регистрирует сервис-воркер библиотеки
`/_content/ZealousMindedPeopleGeo/sw.js` со scope приложения (`/` или `<base href>`), иначе он
не обслуживал бы страницы. Браузер разрешает такой scope, только если сервер отдаёт `sw.js`
с заголовком `Service-Worker-Allowed`; библиотека проверяет заголовок заранее и без него
воркер не регистрирует. Чтобы включить офлайн-кэш, добавьте в `Program.cs` до
`UseStaticFiles`/`MapStaticAssets`:

```csharp
app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/_content/ZealousMindedPeopleGeo/sw.js", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Service-Worker-Allowed"] = "/";
        context.Response.Headers.CacheControl = "no-cache";
    }

    await next();
});
```

Без заголовка в консоли браузера одно информационное сообщение, а сайт и установка
приложения работают как обычно.

### Установка приложения

Браузер предлагает установить сайт как приложение без дополнительной настройки:
`PwaManagerComponent` добавляет в `<head>` манифест библиотеки, цвет темы и иконку для iOS,
если у приложения нет своего манифеста. Сервис-воркер для установки не нужен. Свой манифест
приложение подключает как обычно, и тогда библиотека его не трогает. Подключить манифест
библиотеки можно и вручную, чтобы он был на всех страницах, а не только там, где есть
`PwaManagerComponent`:

```html
<meta name="theme-color" content="#0e1013" />
<link rel="manifest" href="_content/ZealousMindedPeopleGeo/manifest.json" />
<link rel="apple-touch-icon" href="_content/ZealousMindedPeopleGeo/icons/icon-192x192.png" />
```

Манифест ссылается на иконки из `wwwroot/icons/`: `icon-192x192.png` и `icon-512x512.png`
(обычные), `icon-maskable-512x512.png` (для круглых и других масок Android) и
`badge-72x72.png` (значок уведомлений). PNG отрисованы из SVG-исходников в той же папке.
`start_url` и `scope` манифеста равны `/`, как и scope воркера. Ярлыки ведут на `/map`
и `/globe`; если в вашем приложении других маршрутов, подключите собственный манифест.
Что все картинки из манифеста, `sw.js` и `PwaService` существуют и совпадают по размеру,
проверяет `node --test experiments/pwa-assets.test.mjs`.

### Как воркер кэширует

| Запросы | Стратегия |
|---|---|
| JS, CSS и данные библиотеки, скрипты и стили сайта | Network First: изменённый файл приходит при следующей загрузке страницы, кэш нужен только без сети |
| Картинки и шрифты библиотеки (текстуры Земли, иконки) | Cache First, обновляются со сменой версии кэша |
| Страницы, `/api/...` и прочие GET-запросы | Network First, без сети — из кэша |

Не-GET запросы, соединение Blazor Server (`/_blazor`) и SSE воркер не перехватывает.

При каждом запуске `PwaService` проверяет, не изменился ли `sw.js`. Новый воркер активируется сразу (`skipWaiting`), удаляет кэши прошлой версии и предлагает перезагрузить страницу.

### Версия кэшей

Кэши называются `zealous-geo-static-v<версия>` и `zealous-geo-dynamic-v<версия>`, где версия — константа `SW_VERSION` в `wwwroot/sw.js`.

**Правило: `SW_VERSION` равна `<Version>` в `ZealousMindedPeopleGeo.csproj` — поднимая версию пакета, поднимайте и её.** Изменённый `sw.js` браузер ставит как новый воркер, а тот при активации удаляет все кэши `zealous-geo-*` других версий, в том числе закэшированные текстуры и иконки. Совпадение версий проверяет `node --test experiments/service-worker-cache.test.mjs`.

## 🏗️ Архитектура

### Модели данных

#### Participant
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
   builder.Services.AddZealousMindedPeopleGeo();
   ```
   Стили и скрипты библиотека подключит сама.

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
точки (см. «Любые точки: категории, легенда, карточка»).

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
  пользователя (см. [«Язык сообщений»](#язык-сообщений)).
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

Точки лежат в таблице `GeoPoints`: общие поля — в своих столбцах, `Properties` — одним
JSON в `PropertiesJson`, поэтому схема не меняется, какие бы поля ни хранило приложение.
`EnsureGeoDataDatabaseCreatedAsync` создаёт эту таблицу и в уже существующей БД, например
общей с таблицами приложения: сам `EnsureCreated` из EF такую БД не трогает.

#### База прежних версий

Прежние версии хранили участников в таблице `GeoDataParticipants`. При первом
`EnsureGeoDataDatabaseCreatedAsync` библиотека в одной транзакции переносит их в
`GeoPoints` и переименовывает старую таблицу в `GeoDataParticipants_Migrated`. Удалённые
потом точки не вернутся. Строки без координат точкой не станут и остаются в
переименованной таблице. Если схемой управляют миграции EF Core, после их применения
вызовите перенос сами:

```csharp
await using var context = await dbFactory.CreateDbContextAsync();
await GeoDataDatabaseInitializer.MigrateLegacyParticipantsAsync(context);
```

### Работа с несколькими глобусами

Каждый именованный контейнер (`containerId`) соответствует отдельному глобусу или карте. Данные разных контейнеров изолированы друг от друга в одной таблице за счёт колонки `ContainerId` (составной ключ `ContainerId` + `Id`), поэтому одна и та же точка может присутствовать в разных контейнерах.

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
// Загрузка массива точек или участников из JSON-строки прямо в БД
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

### Язык сообщений

Причины пропуска при импорте (`GeoJsonReadResult.Skipped`, `ErrorMessage` загрузки JSON),
ошибки `GeoPoint.Validate()` и `JsonException` из `GeoPointGeoJson.Read` выходят на языке
`CultureInfo.CurrentUICulture`, а числа в них форматируются по `CultureInfo.CurrentCulture`.
Переводы есть на английский (он же для языков без перевода) и русский:

| `CurrentUICulture` | Сообщение |
|---|---|
| `ru`, `ru-RU` | Объект 1 («road»): геометрия LineString — не точка |
| `en` и остальные | Feature 1 ('road'): geometry LineString is not a point |

Язык выбирает приложение, например middleware локализации ASP.NET Core:

```csharp
app.UseRequestLocalization(new RequestLocalizationOptions
{
    // Язык браузера, если для него есть перевод, иначе русский. Формат чисел не меняется.
    DefaultRequestCulture = new RequestCulture(CultureInfo.CurrentCulture, new CultureInfo("ru")),
    SupportedCultures = new[] { CultureInfo.CurrentCulture },
    SupportedUICultures = new[] { new CultureInfo("ru"), new CultureInfo("en") }
});
```

Интерактивные компоненты Blazor Server работают через WebSocket, а в его запросе
заголовка `Accept-Language` может не быть. Поэтому выбранный при загрузке страницы язык
стоит запомнить в cookie, как это делает `App.razor` витрины. Подробности, в том числе
для WebAssembly, — в [документации ASP.NET Core](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization).

Новый язык — файл `Resources/Messages.<код>.resx` с теми же ключами, что в `Messages.resx`.
Тест `MessagesTests` сверяет ключи и подстановки `{0}` каждого перевода с английским, если
добавить код языка в его список `Translations`. Надписи самих компонентов (карты, формы,
глобуса) пока не переводятся.

## 🔒 Безопасность

Рекомендации по безопасности:

1. **API ключи** - Храните ключи Google вне репозитория (см. «Конфигурация» → «Ключи и секреты»)
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
│   │   ├── ParticipantGeoDataExtensions.cs # Методы участников поверх точек
│   │   ├── Persistence/                  # Хранение в БД: таблица GeoPoints, перенос старой таблицы
│   │   └── GeoDataLoaderExtensions.cs    # Расширения для загрузки
│   └── Repositories/                     # Репозитории данных
│       └── InMemoryParticipantRepository.cs # In-Memory хранилище
├── Models/              # Модели данных
│   ├── GeoPoint.cs                       # Точка: координаты, подпись, категория, свои поля
│   ├── ParticipantGeoPointExtensions.cs  # Участник ↔ точка
│   ├── Participant.cs                    # Модель участника
│   ├── GlobeOptions.cs                   # Настройки глобуса
│   └── GlobeState.cs                     # Состояние глобуса
├── Resources/           # Сообщения: Messages.resx (английский) и Messages.ru.resx
└── wwwroot/             # Статические ресурсы
    ├── ZealousMindedPeopleGeo.lib.module.js # JS-инициализатор: Blazor загружает его сам, он подключает стили
    ├── js/              # JavaScript модули
    │   └── community-globe.js            # Основной модуль глобуса
    ├── css/             # Стили
    │   ├── zealous-geo.css               # Все стили одним файлом, в <head> его добавляет инициализатор
    │   ├── zealous-ui.css                # Общие токены темной темы
    │   └── community-globe.css и др.     # Стили отдельных компонентов
    └── assets/          # Ресурсы
        └── earth/       # 8K текстуры Земли
```

### Сборка проекта

Нужен .NET SDK 10.

```bash
dotnet build ZealousMindedPeopleGeo.csproj
```

Тесты и тестовая среда описаны в «Быстром старте».


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
