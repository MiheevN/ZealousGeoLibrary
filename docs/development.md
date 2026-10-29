# Разработка библиотеки

[← Документация](README.md) · Раздел для тех, кто меняет саму библиотеку

Приложению, которое только подключает библиотеку, эта страница не нужна.

## Что нужно установить

| Инструмент | Зачем | Нужен приложению? |
|---|---|---|
| .NET SDK 10 | Сборка, тесты C#, витрина | Да |
| Node.js 22 или новее | Только тесты JavaScript (`experiments/*.test.mjs`) | Нет |

Node не участвует в сборке, а npm и `package.json` в проекте не используются. Без Node
всё собирается и работает, не запустятся только тесты JavaScript.

## Структура проекта

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
    ├── js/              # JavaScript-модули (см. docs/javascript.md)
    │   ├── community-globe.js            # 3D-глобус на Three.js
    │   ├── community-map.js              # 2D-карта на Canvas
    │   ├── pwa.js                        # Манифест и сервис-воркер
    │   └── libs/                         # Three.js r180
    ├── css/             # Стили
    │   ├── zealous-geo.css               # Все стили одним файлом, в <head> его добавляет инициализатор
    │   ├── zealous-ui.css                # Общие токены темной темы
    │   └── community-globe.css и др.     # Стили отдельных компонентов
    └── assets/          # Ресурсы
        └── earth/       # Текстуры Земли: день и ночь 8K и 4K, рельеф, шероховатость, облака 4K
tests/ZealousMindedPeopleGeo.Tests/  # Тесты C#: xUnit + bUnit, запуск — dotnet test
experiments/                         # Тесты JavaScript: *.test.mjs, запуск — node --test
hosts/ZealousMindedPeopleGeo.Showcase/ # Витрина для ручной проверки
docs/                                # Документация
```

Код делится на две части, и у каждой свои тесты:

| Часть | Где лежит | Чем проверяется |
|---|---|---|
| C#: компоненты Razor, сервисы, модели, ресурсы | `Components/`, `Services/`, `Models/`, `Validation/`, `Resources/` | [Тесты C#](#тесты-c-net) |
| JavaScript, CSS и разметка | `wwwroot/`, а также разметка `.razor` и ресурсы витрины | [Тесты JavaScript](#тесты-javascript-node) |

## Сборка

```bash
dotnet build ZealousMindedPeopleGeo.sln
```

## Витрина

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
библиотеки нет (см. [Встраивание](integration.md)).

## Тесты C# (.NET)

Нужен только .NET SDK.

```bash
dotnet test tests/ZealousMindedPeopleGeo.Tests
```

Тесты на xUnit, компоненты рендерит [bUnit](https://bunit.dev). JavaScript в этих тестах не
выполняется: bUnit подменяет JS interop и проверяет, какие функции и с какими
аргументами вызвал C#.

| Папка | Что проверяет |
|---|---|
| `GeoData/` | Контракт хранилищ гео-данных (`GeoDataStorageContractTests`): результаты операций и события `OnDataChanged`, шина между серверами. Своё хранилище проверяется тем же набором (см. [Своё хранилище](storage.md)) |
| `Components/` | Razor-компоненты: какие параметры уходят в JavaScript и что видит пользователь |
| `Validation/` | Атрибуты, которыми форма проверяет ввод участника |
| `Models/` | `GeoPoint`, GeoJSON, палитра категорий, демонстрационные наборы, ключи переводов (`MessagesTests`) |
| `Services/` | Регистрация сервисов, геокодеры Nominatim и Google, кэш, `ThreeJsGlobeService`, `PwaService` |

Покрытие кода (отчёт Cobertura появится в `tests/ZealousMindedPeopleGeo.Tests/TestResults/`):

```bash
dotnet test tests/ZealousMindedPeopleGeo.Tests --collect:"XPlat Code Coverage"
```

## Тесты JavaScript (Node)

Нужен Node.js 22 или новее; ставить пакеты не нужно. Тесты используют только встроенные
модули Node (`node:test`, `node:assert`, `node:fs`, `node:vm`).

```bash
node --test experiments/*.test.mjs
```

`node --test experiments/` без маски на Node 22 не работает: Node принимает папку за модуль.

Эти тесты нужны потому, что большая часть поведения карты и глобуса написана на JavaScript,
а тесты C# его не выполняют. Скрипты из `wwwroot/` загружаются в Node с заглушками DOM,
Canvas и Three.js, браузер не нужен.

| Файлы | Что проверяют |
|---|---|
| `community-globe-*.test.mjs` | Глобус: камера, вращение, освещение, день и ночь, текстуры, группы, касания, клавиатура, ошибки запуска, настройки |
| `participant-marker-*.test.mjs`, `label-layout.test.mjs` | Маркеры и подписи глобуса |
| `community-map-*.test.mjs` | 2D-карта: проекции, группы, жесты, клавиатура |
| `pwa-*.test.mjs`, `service-worker-cache.test.mjs` | Манифест и иконки, `pwa.js`, кэши `sw.js` и совпадение `SW_VERSION` с версией библиотеки |
| `library-*.test.mjs`, `unified-ui-theme.test.mjs`, `responsive-layout.test.mjs` | JS-инициализатор, сборка стилей, тема, адаптивная вёрстка |
| `debug-logging.test.mjs` | Скрипты молчат в консоли без `zealousGeoDebug` |
| `showcase-resources.test.mjs` | У каждой надписи витрины есть английский и русский текст |

`label-scale-regression.mjs` — не тест, а скрипт для ручной проверки размера подписей.

## CI

GitHub Actions (`.github/workflows/ci.yml`) на каждый PR и push в `master` запускает оба
набора: сначала сборку и тесты C# — дважды, второй раз с русской локалью системы
(`LANG=ru_RU.UTF-8`), потому что сообщения библиотеки зависят от языка, — затем тесты
JavaScript на Node 22.

## Правила

- **Версия сервис-воркера.** `SW_VERSION` в `wwwroot/sw.js` равна `<Version>` в
  `ZealousMindedPeopleGeo.csproj`: поднимая версию библиотеки, поднимайте и её (см.
  [Версия кэшей](pwa.md#версия-кэшей)). Проверяет тест JavaScript `service-worker-cache.test.mjs`.
- **Переводы.** Новая надпись добавляется в `Resources/Ui.resx` и `Ui.ru.resx` и в `Ui.cs`
  (см. [Язык сообщений](localization.md)). Совпадение ключей проверяет тест C# `MessagesTests`.

## Вклад в развитие

1. Сделайте fork проекта
2. Создайте ветку (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request; оба набора тестов должны пройти
