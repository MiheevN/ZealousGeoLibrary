# Zealous Minded People Geography Library

Библиотека для создания интерактивных 3D географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

[![NuGet](https://img.shields.io/nuget/v/ZealousMindedPeopleGeo.svg)](https://www.nuget.org/packages/ZealousMindedPeopleGeo/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 🌟 Возможности

- **3D глобус сообщества** - Трехмерная визуализация планеты с интерактивными точками участников
- **Настройки глобуса** - Полная настройка параметров глобуса (размеры, освещение, атмосфера, облака)
- **Динамическое управление** - Включение/выключение атмосферы и облаков в реальном времени
- **Множественные глобусы** - Поддержка нескольких независимых 3D глобусов на одной странице
- **Именованные контейнеры гео-данных** - Любые точки (`GeoPoint`: офисы, события, датчики, участники) в именованных наборах для разных глобусов и карт; хранятся в памяти или в хранилище, которое подключает приложение
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

Витрина говорит по-русски и по-английски: язык берётся из браузера, а переключатель
RU / EN в меню (или `?ui-culture=en` в адресе) выбирает его явно и запоминается в cookie.
Надписи витрины лежат в `hosts/ZealousMindedPeopleGeo.Showcase/Resources/Site.resx`
и `Site.ru.resx`, надписи компонентов — в ресурсах библиотеки.

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

Оба набора запускает GitHub Actions (`.github/workflows/ci.yml`) на каждый PR и push в
`master`; .NET-тесты — дважды, второй раз с русской локалью системы (`LANG=ru_RU.UTF-8`),
потому что сообщения библиотеки зависят от языка.

Что проверяют .NET-тесты (`tests/ZealousMindedPeopleGeo.Tests`):

- `GeoData/` — контракт хранилищ гео-данных (`GeoDataStorageContractTests`): результаты
  операций и события `OnDataChanged`. Прогоняется на хранилище в памяти; своё хранилище
  проверяется тем же набором, если унаследовать класс (см. «Своё хранилище»);
- `Components/` — Razor-компоненты через [bUnit](https://bunit.dev): какие параметры
  уходят в JavaScript и что видит пользователь;
- `Validation/` — атрибуты, которыми форма проверяет ввод участника;
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

Точки и участники по умолчанию хранятся в памяти и пропадают при перезапуске. Постоянное
хранение — база данных, таблица, внешний API — библиотека не навязывает: приложение
реализует интерфейсы `IGeoDataContainerManager` и `IParticipantRepository` и регистрирует
свои реализации до или после вызова (см. «Своё хранилище»). Библиотека их не перезапишет,
а повторный вызов `AddZealousMindedPeopleGeo` ничего не дублирует.

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

С клавиатуры карта доступна так же, как мышью. До холста доходят клавишей Tab, затем:

| Клавиша | Действие |
| --- | --- |
| ← → ↑ ↓ | сдвинуть карту на пятую часть окна |
| `+` / `−` | приблизить / отдалить |
| Home | вернуть начальный вид |
| N / P | выбрать следующий / предыдущий маркер (слева направо) |
| Enter или пробел | открыть выбранную точку, как кликом; группу — раскрыть |
| Esc | снять выбор, свернуть веер |

N и P срабатывают по физической клавише, поэтому работают и в русской раскладке. Выбранный маркер
обведён кольцом, рядом показана его подсказка, а экранный диктор зачитывает название
и категорию. Подсказку про клавиши диктор читает при входе на карту на языке пользователя.

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
карту не перекрывает. Кнопка в шапке («Points» / «Точки», подпись задаёт `ListTitle`) скрывает и
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
Не заданные центр и зум показывают мир целиком.

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

Надписи скрипта (кнопки масштаба, подсказки, «5 points») без Blazor английские. Свои
передаются в `texts`, а `locale` выбирает формы числа (`Intl.PluralRules`): ключи —
`DEFAULT_TEXTS` в `community-map.js`, `{0}` — число. Компонент Blazor передаёт их сам на
языке пользователя.

```js
initializeCommunityMap('', 20, 0, 1, 'map', {
    locale: 'ru',
    texts: { zoomIn: 'Приблизить', clusterOne: '{0} точка', clusterFew: '{0} точки', clusterMany: '{0} точек' }
});
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

Близкие маркеры глобус, как и 2D-карта, сливает в один с числом точек вместо подписи
и цветом, который чаще всего встречается у его точек. Группы пересобираются, когда
камера заметно приблизилась или отдалилась (вращение их не трогает); вплотную к
поверхности видны все точки, даже с одинаковыми координатами. Клик по группе
подводит к ней камеру, и группа распадается на точки или меньшие группы. Обработчик
клика по точке на группу не срабатывает.

```razor
<!-- Сливать маркеры ближе 60 пикселей; ClusterPoints="false" — без групп -->
<CommunityGlobeViewer GlobeId="offices" DataContainerId="offices" ClusterRadius="60" />
```

`ClusterRadius` — расстояние между маркерами на экране в пикселях (0–200, по
умолчанию 40). Те же параметры есть у `CommunityGlobeComponent`, а у JS-глобуса —
опции `clusterPoints` и `clusterRadius`. `participantCount` в состоянии глобуса —
по-прежнему число точек, а не маркеров.

На телефоне глобус вращают пальцем, а приближают щипком: раздвинутые вдвое пальцы вдвое
приближают поверхность, так что щипок не проскакивает к самой земле. Быстрый вертикальный
взмах по глобусу прокручивает страницу (с инерцией), а медленное движение и взмах вбок
вращают глобус — страницу можно листать, даже если глобус занимает весь экран. Подписи
на маленьком холсте мельче (от 15 px), а не помещающиеся в холст не показываются.

Глобусом можно управлять с клавиатуры, как и 2D-картой. До холста доходят клавишей Tab,
затем:

| Клавиша | Действие |
| --- | --- |
| ← → ↑ ↓ | повернуть глобус (у поверхности — на меньший угол) |
| `+` / `−` | приблизить / отдалить |
| Home | вернуть начальный вид |
| N / P | выбрать следующий / предыдущий видимый маркер (слева направо) |
| Enter или пробел | подлететь к выбранной точке, как кликом; к группе — приблизиться |
| Esc | снять выбор |

Пока глобус в фокусе, он сам не вращается. Выбранный маркер обведён кольцом, экранный
диктор зачитывает название и категорию точки или число точек в группе и их первые имена.
`CommunityGlobeViewer` передаёт эти надписи и подсказку про клавиши на языке пользователя
(`GlobeOptions.Texts` и `Locale`); у JS-глобуса это опции `texts` и `locale`.

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
участники хранятся в памяти, а адреса ищутся в OpenStreetMap без ключа. Ключ Google нужен,
только если адреса должен искать Google.

Библиотека читает секцию `ZealousMindedPeopleGeo`, если сервисы зарегистрированы с
конфигурацией:

```csharp
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);
```

```json
{
  "ZealousMindedPeopleGeo": {
    "GoogleMapsApiKey": "",
    "NominatimUrl": "",
    "EnableGeocoding": true,
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
| `GoogleMapsApiKey` | пусто | Необязательный ключ Google Maps Platform с включённым Geocoding API. Задан — адреса для `GeoDataParticipantForm` ищет Google, не задан — Nominatim (OpenStreetMap). Карта и глобус ключ не используют. |
| `NominatimUrl` | пусто | Сервер Nominatim для поиска без ключа Google. Пусто — публичный `https://nominatim.openstreetmap.org/`: бесплатно, но не больше запроса в секунду на приложение. Для большой нагрузки укажите свой сервер или платного поставщика с API Nominatim. |
| `EnableGeocoding` | `true` | `false` выключает поиск адресов совсем: форма сообщает, что он недоступен, и точку не добавляет. |
| `Map:Projection` | `EqualEarth` | Проекция 2D-карты: `EqualEarth` или `Equirectangular`. |
| `Map:CentralMeridian` | `0` | Центральный меридиан 2D-карты в градусах, от −180 до 180. |
| `Map:DefaultLatitude` | `20` | Широта начального центра 2D-карты. |
| `Map:DefaultLongitude` | `Map:CentralMeridian` | Долгота начального центра 2D-карты. |
| `Map:DefaultZoom` | `2` | Начальное приближение 2D-карты; `1` — мир целиком. |
| `Map:ClusterPoints` | `true` | Сливать близкие маркеры 2D-карты в группы (см. «Группы близких точек»). |
| `Map:ClusterRadius` | `24` | Расстояние между центрами маркеров в пикселях (0–200), ближе которого они сливаются. |

Не заданные ключи берут значения по умолчанию из таблицы, есть секция `Map` или нет: без
центра и зума карта открывается на весь мир. Параметры `CommunityMapComponent`
(`Projection`, `CentralMeridian`, `Zoom`, `CenterLatitude`, `CenterLongitude`,
`ClusterPoints`, `ClusterRadius`) важнее настроек.

Глобус настраивается параметрами компонентов и панелью
настроек (см. «Настройки глобуса»), длительность кэша — в коде (см. «Кэширование»).

### Ключи и секреты

Не храните ключ Google в `appsettings.json` в репозитории.
Для разработки подойдут user secrets, на сервере — переменные окружения (двоеточие в
имени ключа заменяется на `__`):

```bash
dotnet user-secrets init
dotnet user-secrets set "ZealousMindedPeopleGeo:GoogleMapsApiKey" "<ключ>"

export ZealousMindedPeopleGeo__GoogleMapsApiKey="<ключ>"
```

Ключ используется только на сервере: компоненты не передают его в браузер. В Blazor
WebAssembly настройки загружает сам браузер, поэтому ключ из них виден пользователям.

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
облаков — белые облака на чёрном.

### Сервисы

#### IGeoDataContainerManager и IParticipantRepository
Хранилища точек и участников. Встроены реализации в памяти, постоянное хранение
реализует приложение (см. «Своё хранилище»).

#### IGeocodingService
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

Точку проверяет `GeoPoint.Validate()`: `null`, если всё в порядке, иначе текст ошибки
(см. «Язык сообщений»). Контейнеры вызывают её сами — `AddPointAsync` и `UpdatePointAsync`
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
- Со своим хранилищем (см. «Своё хранилище») то же самое работает, если оно вызывает
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

## 🗄️ Своё хранилище

Библиотека хранит точки и участников только в памяти. Чтобы данные переживали
перезапуск или жили в базе приложения, реализуйте интерфейс и зарегистрируйте его —
компоненты, загрузка и выгрузка JSON/GeoJSON и синхронизация между пользователями
работают с ним так же, как со встроенным хранилищем.

| Интерфейс | Что хранит | Встроенная реализация |
|---|---|---|
| `IGeoDataContainerManager` + `IGeoDataContainer` | точки (`GeoPoint`) в именованных контейнерах | `GeoDataContainerManager` (в памяти, singleton) |
| `IParticipantRepository` | участников для компонентов без `DataContainerId` | `InMemoryParticipantRepository` (в памяти, scoped) |

Для контейнеров проще всего унаследовать `GeoDataContainerManagerBase`: загрузку и
выгрузку JSON и GeoJSON и рассылку `OnDataChanged` он уже умеет, остаётся управлять
контейнерами. Сам контейнер реализует `IGeoDataContainer` поверх вашей базы:

```csharp
public sealed class DbGeoDataContainerManager : GeoDataContainerManagerBase
{
    private readonly IDbContextFactory<AppDbContext> _db;

    public DbGeoDataContainerManager(IDbContextFactory<AppDbContext> db, ILogger<DbGeoDataContainerManager> logger)
        : base(logger) => _db = db;

    // Контейнер сообщает об изменениях через HandleDataChanged — так их увидят все
    // открытые глобусы и карты.
    public override IGeoDataContainer GetOrCreateContainer(string containerId) =>
        new DbGeoDataContainer(containerId, _db, HandleDataChanged);

    public override IGeoDataContainer? GetContainer(string containerId) => ContainerExists(containerId) ? GetOrCreateContainer(containerId) : null;
    public override bool ContainerExists(string containerId) { /* SELECT по ContainerId */ }
    public override bool RemoveContainer(string containerId) { /* DELETE по ContainerId */ }
    public override IEnumerable<string> GetContainerIds() { /* SELECT DISTINCT ContainerId */ }
}

// Program.cs — до или после AddZealousMindedPeopleGeo
builder.Services.AddSingleton<IGeoDataContainerManager, DbGeoDataContainerManager>();
builder.Services.AddZealousMindedPeopleGeo();
```

Что должно соблюдаться (так ведёт себя встроенное хранилище):

- контейнер хранит **копии** точек: изменения объекта после `AddPointAsync` его не трогают;
- точка с занятым `Id` не добавляется, `GeoPoint.Validate()` с ошибкой — тоже;
- `OnDataChanged` вызывается после успешного изменения и только если что-то изменилось;
- `Properties` точки — произвольные строки: удобно хранить одним JSON-столбцом,
  тогда схема таблицы не зависит от полей приложения.

Проверить своё хранилище можно готовым набором тестов: скопируйте в свой тестовый проект
`GeoDataStorageContractTests*.cs` из `tests/ZealousMindedPeopleGeo.Tests/GeoData`,
унаследуйте класс и верните свой менеджер из `Manager` (пример —
`InMemoryGeoDataStorageTests`).

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

Скрипты глобуса и 2D-карты в консоли браузера показывают только предупреждения и ошибки.
Подробный журнал включается в консоли браузера: `globalThis.zealousGeoDebug = true`
(до загрузки страницы — например, через закладку или `addInitScript` в тестах).

### Язык сообщений

Надписи всех компонентов — формы, 2D-карты (вместе с кнопками и подсказками внутри неё),
глобуса и его панелей, `PwaManagerComponent` (и вопроса об обновлении приложения),
`LibraryShowcaseComponent`, — сообщения проверки полей формы, причины пропуска при импорте
(`GeoJsonReadResult.Skipped`, `ErrorMessage` загрузки JSON), ошибки `GeoPoint.Validate()` и
`JsonException` из `GeoPointGeoJson.Read` выходят на языке `CultureInfo.CurrentUICulture`, а
числа в сообщениях форматируются по `CultureInfo.CurrentCulture`. Переводы есть на
английский (он же для языков без перевода) и русский:

| `CurrentUICulture` | Сообщение |
|---|---|
| `ru`, `ru-RU` | Объект 1 («road»): геометрия LineString — не точка |
| `en` и остальные | Feature 1 ('road'): geometry LineString is not a point |

Параметры с текстом (`Title`, `ListTitle`, `SubmitButtonText`) по умолчанию тоже берут
перевод; заданное значение показывается как есть.

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

Новый язык — два файла с теми же ключами, что у английских: `Resources/Messages.<код>.resx`
(сообщения) и `Resources/Ui.<код>.resx` (надписи компонентов). Тест `MessagesTests` сверяет
ключи и подстановки `{0}` каждого перевода с английским, если добавить код языка в его
список `Translations`.

## 🔒 Безопасность

Рекомендации по безопасности:

1. **API ключи** - Храните ключи Google вне репозитория (см. «Конфигурация» → «Ключи и секреты»)
2. **Валидация** - Контейнеры не сохраняют точки, которые не проходят `GeoPoint.Validate()` (см. «Валидация»)
3. **Доступ и частота** - Библиотека не ограничивает, кто и сколько точек добавляет; для формы на открытой странице задайте `OnSubmitting` (см. «Кто может добавлять точки»)
4. **CORS** - Настройте политику CORS для защиты от CSRF атак
5. **HTTPS** - Используйте HTTPS для всех запросов

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
│   │   ├── IGeoDataChangeBus.cs          # Шина изменений между серверами (реализует приложение)
│   │   ├── InMemoryGeoDataContainer.cs   # In-Memory реализация
│   │   ├── GeoDataContainerManager.cs    # Реализация менеджера
│   │   ├── ParticipantGeoDataExtensions.cs # Методы участников поверх точек
│   │   └── GeoDataLoaderExtensions.cs    # Расширения для загрузки
│   └── Repositories/                     # Репозитории данных
│       └── InMemoryParticipantRepository.cs # In-Memory хранилище
├── Models/              # Модели данных
│   ├── GeoPoint.cs                       # Точка: координаты, подпись, категория, свои поля
│   ├── ParticipantGeoPointExtensions.cs  # Участник ↔ точка
│   ├── Participant.cs                    # Модель участника
│   ├── GlobeOptions.cs                   # Настройки глобуса
│   └── GlobeState.cs                     # Состояние глобуса
├── Resources/           # Сообщения (Messages*.resx) и надписи компонентов (Ui*.resx): английский и русский
└── wwwroot/             # Статические ресурсы
    ├── ZealousMindedPeopleGeo.lib.module.js # JS-инициализатор: Blazor загружает его сам, он подключает стили
    ├── js/              # JavaScript модули
    │   └── community-globe.js            # Основной модуль глобуса
    ├── css/             # Стили
    │   ├── zealous-geo.css               # Все стили одним файлом, в <head> его добавляет инициализатор
    │   ├── zealous-ui.css                # Общие токены темной темы
    │   └── community-globe.css и др.     # Стили отдельных компонентов
    └── assets/          # Ресурсы
        └── earth/       # Текстуры Земли: день 8K и 4K, рельеф, шероховатость, облака 4K
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
- **OpenStreetMap и Nominatim** - Поиск адресов без ключа
- **Google Maps API** - Поиск адресов по ключу
- **ASP.NET Core** - Платформа веб-разработки

---

*"География людей, объединенных стремлением сделать мир добрее и гармоничнее"*
