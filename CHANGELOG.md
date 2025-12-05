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
