# История изменений / Changelog

Все значимые изменения в проекте ZealousMindedPeopleGeo документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/),
и проект придерживается [Semantic Versioning](https://semver.org/lang/ru/).

## [Unreleased] - 2024-12-XX

### Удалено
- **Устранены дубликаты функциональности (Issue #4)**
  - Удален компонент `ParticipantRegistrationComponent.razor` - заменен на более продвинутый `GeoDataParticipantForm.razor`
  - Удалено дублирующееся поле `Timestamp` из модели `Participant.cs` - используется `RegisteredAt`
  - Мигрированы все ссылки на `Timestamp` на `RegisteredAt` в:
    - `GoogleSheetsService.cs`
    - `community-map.js`
    - `GeoJson.cs`

### Добавлено
- **Тестовый проект `tests/ZealousMindedPeopleGeo.Tests`: 21 → 102 теста**
  - Общий контракт хранилищ гео-данных: одни и те же тесты прогоняются на хранилище в памяти и в БД (раньше хранилище в памяти не тестировалось)
  - Тесты `CommunityMapComponent` на bUnit: параметры проекции, центра и зума, загрузка скрипта модулем, список участников, клик по маркеру, освобождение
  - Тесты `ParticipantValidator`, `ParticipantRegistrationValidator`, `CoordinateValidator` и демонстрационных наборов
  - Тесты разложены по папкам `GeoData/`, `Components/`, `Validation/`, `Models/`, `Services/`; сбор покрытия через `coverlet.collector`; раздел «Тесты» в README
  - Обновлены `Microsoft.NET.Test.Sdk`, xUnit и раннер

- **Оформление тестовой среды `hosts/ZealousMindedPeopleGeo.Showcase`**
  - Тёмная оболочка на токенах `zealous-ui.css`: боковое меню с текущим разделом, заголовки страниц, адаптивная вёрстка
  - Обзор с картой мира в Equal Earth, ссылками на страницы, демонстрационными наборами и командами запуска
  - `/map`: переключение проекции, центрального меридиана и набора данных; `/globe`: выбор набора и список точек; `/showcase`: `LibraryShowcaseComponent`
  - Страница 404 и панель необработанных ошибок Blazor

- **Начальный вид 2D-карты**: параметры `Zoom`, `CenterLatitude` и `CenterLongitude` у `CommunityMapComponent`; без них центр по долготе совпадает с центральным меридианом

- **Проекция Equal Earth для 2D-карты**
  - `community-map.js` по умолчанию рисует карту в равновеликой проекции Equal Earth (Šavrič, Patterson, Jenny, 2018); прямое и обратное преобразования сверены с d3-geo
  - Прежняя равнопромежуточная проекция доступна опцией `MapProjection.Equirectangular`
  - Настраиваемый центральный меридиан: параметры `Projection` и `CentralMeridian` у `CommunityMapComponent` и одноимённые свойства `MapConfiguration`
  - `initializeCommunityMap` принимает шестой необязательный аргумент `{ projection, centralMeridian }`
  - Камера работает в плоскости проекции: при `zoom = 1` мир целиком вписан в окно, при приближении край карты не уходит от края окна; прямоугольная проекция по-прежнему бесконечно прокручивается по горизонтали
  - Кнопка «⌂» возвращает к виду, с которым карта была инициализирована

- **Хранение гео-данных в базе данных через EF Core (Issue #55)**
  - Провайдеро-независимое хранилище на базе Entity Framework Core (SQLite, PostgreSQL, SQL Server и др.)
  - `DatabaseGeoDataContainerManager` и `DatabaseGeoDataContainer` реализуют существующие интерфейсы `IGeoDataContainerManager` / `IGeoDataContainer` — API не меняется
  - Поддержка нескольких глобусов в одной таблице с изоляцией по `ContainerId` (составной ключ `ContainerId` + `Id`)
  - Добавление участников по одному и пакетная загрузка из массива объектов с пропуском дубликатов
  - Загрузка и выгрузка JSON напрямую в БД (`LoadFromJsonAsync`, `LoadFromJsonFileAsync`, `ExportToJsonAsync`)
  - Регистрация через `AddGeoDataDatabase(configureDbContext)` и инициализация схемы через `EnsureGeoDataDatabaseCreatedAsync()`
  - Общая логика загрузки/JSON вынесена в базовый класс `GeoDataContainerManagerBase` (общий для in-memory и БД менеджеров)
  - Сущность `GeoDataParticipantEntity` и контекст `GeoDataDbContext` для маппинга `Participant`
  - Тестовый проект `ZealousMindedPeopleGeo.Tests` с покрытием сценариев БД (SQLite in-memory)
  - Пример `examples/GeoDataDatabaseExample.razor`

- **Компонент настроек глобуса** (`CommunityGlobeSettings.razor`)
  - Полная настройка параметров глобуса через UI
  - Аккордеон с группировкой настроек (размеры, точки, вращение, освещение, атмосфера, облака, камера, цвета)
  - Сохранение и загрузка конфигурации в JSON формате
  - Предпросмотр JSON конфигурации в реальном времени
  - Кнопка сброса к настройкам по умолчанию

- **Динамическое управление атмосферой и облаками**
  - Метод `toggleAtmosphere(enabled)` в JavaScript для включения/выключения атмосферы
  - Метод `toggleClouds(enabled)` в JavaScript для включения/выключения облаков
  - Правильная очистка ресурсов (geometry, material) при удалении объектов
  - Интеграция в метод `updateSettings()` для применения настроек в реальном времени

- **Метод применения настроек** (`updateSettings()` в JavaScript)
  - Применение всех параметров глобуса (размеры, освещение, вращение, камера)
  - Обновление прозрачности атмосферы и облаков
  - Перерисовка точек участников с новыми параметрами
  - Экспорт функции для вызова из Blazor

### Исправлено
- **Хранилище гео-данных в памяти ведёт себя так же, как хранилище в БД**
  - `ClearAsync` пустого контейнера больше не рассылает событие `Cleared`
  - `RemoveContainer` для контейнера с данными рассылает `Cleared`, чтобы подписчики (например, глобусы) узнали, что данные пропали

- **Валидация имени участника принимает буквы любого алфавита**
  - Правило `[a-zA-Zа-яА-Я]` отклоняло «ё» («Алёна», «Фёдор») и диакритику («José», «François»), в том числе «Brasília» из демонстрационного набора «Столицы мира». Теперь `[\p{L}\p{M}]`; цифры и спецсимволы по-прежнему запрещены

- **Уязвимость в зависимости тестового проекта**: `Microsoft.EntityFrameworkCore.Sqlite` 10.0.12 подтягивает `SQLitePCLRaw` 2.1.12 вместо 2.1.11 (GHSA-2m69-gcr7-jv3q)

- **Стили на странице с несколькими компонентами библиотеки**
  - Компоненты подключали свои стили через `<HeadContent>`, а `<HeadOutlet>` выводит в `<head>` только последний отрисованный из них. На `LibraryShowcaseComponent` оставался один `pwa-manager.css`: вкладки были без оформления, и все панели показывались разом
  - Теперь стили подключаются одним файлом `zealous-geo.css` (см. «Изменено»), блоки `<HeadContent>` из компонентов удалены
  - `.loading-spinner` карты ограничен `.map-loading-overlay`: при общем подключении он переопределял индикатор загрузки глобуса

- **2D-карта: загрузка скрипта и аватары**
  - `CommunityMapComponent` загружает `community-map.js` модулем через `import()` вместо `<script>` в `<HeadContent>`: скрипт больше не исполняется дважды (ошибка «Identifier 'mapInstances' has already been declared») и не теряется, когда на странице есть другие компоненты библиотеки
  - Карта не вызывает JS при освобождении после пререндера (ошибка «JavaScript interop calls cannot be issued at this time»)
  - В списке участников вместо первой буквы имени выводился текст «.ToString().ToUpper()»

- **2D-карта: пропадающие материки**
  - Северная Америка исчезала, если центр карты был далеко от неё: копии полигонов со сдвигом ±360° совпадали и не переносили фигуру через линию перемены даты
  - Антарктида не рисовалась совсем: полоса от −180° до 180° схлопывалась в нулевую ширину
  - Тесты `experiments/community-map-2d.test.mjs` падали на Node 21+, где `globalThis.navigator` доступен только для чтения

- **PwaService: "Illegal return statement" при каждом открытии витрины**
  - Скрипты для `eval` с `return` на верхнем уровне заменены функциями ES-модуля `wwwroot/js/pwa.js`, модуль подключается через `import`
  - `PwaManagerComponent` получает реальное состояние установки и кэша; публичный API `PwaService` не изменился
  - Заголовок и текст уведомления передаются аргументами, а не подставляются в текст скрипта
  - `PwaManagerComponent` реализует `IDisposable`, таймер проверки обновлений останавливается вместе с компонентом

- **Пользователи с установленным сервис-воркером не получали обновлённые JS/CSS**
  - Имена кэшей привязаны к версии пакета: `SW_VERSION` в `wwwroot/sw.js` равна `<Version>` в csproj и поднимается вместе с ней (проверяет `experiments/service-worker-cache.test.mjs`); при активации удаляются кэши `zealous-geo-*` всех других версий
  - JS, CSS и данные отдаются по Network First с перепроверкой у сервера, из кэша — только без сети; Cache First остался для картинок и шрифтов библиотеки
  - Установку воркера больше не срывает отсутствующий `css/site.css`: ресурсы кэшируются по одному, `skipWaiting` вызывается всегда
  - Воркер больше не перехватывает POST-запросы, соединение Blazor Server (`/_blazor`) и SSE: незавершённый long polling не давал бы новой версии активироваться
  - `PwaService.InitializeAsync` регистрирует воркер со scope `/`, не ждёт уже прошедшего события `load` и при каждом запуске проверяет новую версию `sw.js`; `PwaManagerComponent` вызывает его сам
  - Хосту нужен заголовок `Service-Worker-Allowed: /` для `sw.js` (README, раздел «PWA и сервис-воркер»)

- **Ошибка "Cannot read properties of null (reading 'removeChild')"**
  - Добавлена проверка `contains()` перед вызовом `removeChild()` в `setupScene()`
  - Добавлен флаг `_isRendering` для предотвращения конфликтов рендеринга
  - Обернуты вызовы `StateHasChanged()` в `InvokeAsync()` для потокобезопасности

- **Все предупреждения компилятора (31 → 0)**
  - **NETSDK1080**: Удалена лишняя ссылка на Microsoft.AspNetCore.App
  - **NU5125**: Заменен PackageLicenseUrl на PackageLicenseExpression (MIT)
  - **CS0649**: Инициализировано поле `_pwaHelper = null` в PwaService
  - **CS1587**: Удален неправильно размещенный XML комментарий в GeoJson.cs
  - **CS8600, CS8603, CS8602, CS8604, CS8625**: Исправлены все nullable reference warnings
    - Добавлены явные проверки на null
    - Использованы nullable типы где необходимо
    - Добавлены операторы `??` для значений по умолчанию
  - **CS1998**: Убран async из методов без await
    - CachingService: все методы используют Task.FromResult/Task.CompletedTask
    - InMemoryParticipantRepository: все методы используют ValueTask.FromResult
    - FileGeoJsonService: EnrichCountriesWithRussianNames возвращает Task.CompletedTask
    - ParticipantService: ValidateRegistrationAsync возвращает Task.FromResult
    - ThreeJsGlobeService: SetReadyCallbackAsync использует ValueTask.FromResult
    - PwaManagerComponent: ShowNotificationTestAsync возвращает Task.CompletedTask

- **Ошибки сборки**
  - Удалены файлы с отсутствующими интерфейсами:
    - `ParticipantDataSourceManagerSimple.cs`
    - `JsonFileParticipantDataSource.cs`
    - `InMemoryParticipantDataSource.cs`
    - `NominatimGeocodingServiceSimple.cs`
  - Исправлена ссылка в ServiceCollectionExtensions.cs с NominatimGeocodingService на GoogleMapsGeocodingService

### Изменено
- **Стили подключаются одной строкой `zealous-geo.css`**
  - Новый `wwwroot/css/zealous-geo.css` импортирует токены `zealous-ui.css` и стили всех компонентов
  - Компоненты больше не добавляют стили в `<head>` сами. Приложению нужно один раз подключить `<link rel="stylesheet" href="_content/ZealousMindedPeopleGeo/css/zealous-geo.css" />` в `App.razor` или `index.html`, после Bootstrap и до своих стилей
  - Хост витрины подключает этот файл вместо шести отдельных
  - Тест `experiments/library-stylesheet-bundle.test.mjs` проверяет, что в `zealous-geo.css` есть каждый файл стилей, компоненты не используют `<HeadContent>`, а файлы компонентов не переопределяют селекторы друг друга

- **2D-карта: единый масштаб по осям**
  - Прямоугольная проекция больше не растягивается под пропорции контейнера: масштаб по горизонтали и вертикали одинаковый, при `zoom = 1` мир вписан в окно
  - Центр карты можно сдвинуть до полюсов (раньше широта центра ограничивалась ±85°)

- **Единый визуальный стиль UI-компонентов (Issue #15)**
  - Добавлена общая темная тема для Razor-компонентов с мягкими рамками и едиными токенами цветов
  - Обновлены стили карты, глобуса, настроек, форм участников и PWA-панелей
  - Приведены поля ввода, чекбоксы, цветовые поля и слайдеры к единому темному оформлению
  - Темизированы JS-создаваемые Google Maps маркеры и информационные окна

- **Качество кода**
  - Проект теперь собирается с 0 предупреждениями и 0 ошибками
  - Код соответствует современным стандартам .NET 9
  - Правильная обработка nullable типов во всех сервисах
  - Оптимизированы async/await паттерны

- **Документация**
  - Обновлен README.md с актуальным состоянием проекта
  - Добавлена информация о компоненте настроек
  - Добавлена информация о качестве кода (0 предупреждений)
  - Создан CHANGELOG.md для отслеживания истории изменений

## [1.0.0] - 2024-12-XX (Предыдущие версии)

### Добавлено
- **Базовая функциональность 3D глобуса**
  - Интерактивный 3D глобус на базе Three.js
  - Поддержка множественных независимых глобусов
  - Управление камерой (вращение, масштабирование, перемещение)
  - Автоматическое вращение глобуса

- **Компоненты Blazor**
  - `CommunityGlobeComponent` - главный компонент-обертка
  - `CommunityGlobeViewer` - компонент отображения глобуса
  - `CommunityGlobeControls` - панель управления
  - `CommunityGlobeParticipantManager` - управление участниками
  - `CommunityMapComponent` - компонент карты Google Maps
  - `GeoDataParticipantForm` - форма для добавления точек на глобус
  - `PwaManagerComponent` - управление PWA функциональностью

- **Сервисы**
  - `ThreeJsGlobeService` - управление 3D сценой
  - `GlobeMediatorService` - посредник между Blazor и JavaScript
  - `InMemoryParticipantRepository` - хранение данных в памяти
  - `GoogleMapsService` - интеграция с Google Maps API
  - `GoogleSheetsService` - интеграция с Google Sheets API
  - `CachingService` - кэширование данных
  - `PwaService` - PWA функциональность
  - `ValidationService` - валидация данных
  - `LocalizationService` - локализация

- **Модели данных**
  - `Participant` - модель участника сообщества
  - `GlobeOptions` - настройки глобуса
  - `GlobeState` - состояние глобуса
  - `GeoJsonFeatureCollection` - GeoJSON данные
  - `ServiceResult<T>` - результат операций сервисов

- **Текстуры и ресурсы**
  - 8K текстуры Земли (daymap, normal map, specular map)
  - Текстуры облаков
  - CSS стили для компонентов
  - PWA манифест и service worker

### Технические детали

#### Архитектура
- Модульная архитектура с четким разделением ответственности
- Паттерн Mediator для связи Blazor-JavaScript
- Dependency Injection для всех сервисов
- Централизованное управление состоянием глобусов

#### Технологии
- .NET 9.0
- Blazor Server
- Three.js для 3D графики
- Google Maps API для геокодирования
- Google Sheets API для хранения данных
- FluentValidation для валидации
- MemoryCache для кэширования

#### Производительность
- Оптимизированный рендеринг 3D сцены
- Кэширование геокодирования (24 часа)
- Кэширование данных участников (15 минут)
- Ленивая загрузка текстур
- Оптимизация памяти при dispose

---

## Формат записей

### Типы изменений
- **Добавлено** - новая функциональность
- **Изменено** - изменения в существующей функциональности
- **Устарело** - функциональность, которая скоро будет удалена
- **Удалено** - удаленная функциональность
- **Исправлено** - исправления багов
- **Безопасность** - исправления уязвимостей

### Формат даты
Используется формат ISO 8601: YYYY-MM-DD

### Ссылки
- [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/)
- [Semantic Versioning](https://semver.org/lang/ru/)
