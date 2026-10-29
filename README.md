# Zealous Minded People Geography Library

Библиотека для создания интерактивных 3D географических приложений сообщества людей, объединенных стремлением сделать мир добрее и гармоничнее.

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

## Из чего состоит

| Часть | Что это | Что нужно |
|---|---|---|
| C# / Blazor | Razor-компоненты, сервисы, модели (.NET 10) | .NET SDK 10 |
| JavaScript | Глобус на Three.js, 2D-карта, PWA в `wwwroot/`. Входят в библиотеку и загружаются сами | Ничего: работают в браузере |
| Тесты | C#: `dotnet test`. JavaScript: `node --test` | .NET SDK 10; Node.js 22 только для тестов JavaScript |

## Быстрый старт

1. Подключите библиотеку ссылкой на проект: в NuGet она не публикуется. Библиотека должна
   лежать рядом с проектом приложения, а не внутри его папки (почему — в
   [Встраивании](docs/integration.md)). Из корня репозитория приложения:
   ```bash
   git submodule add https://github.com/MiheevN/ZealousGeoLibrary.git libs/ZealousGeoLibrary
   dotnet add MyApp/MyApp.csproj reference libs/ZealousGeoLibrary/ZealousMindedPeopleGeo.csproj
   ```
2. Зарегистрируйте сервисы в `Program.cs`:
   ```csharp
   using ZealousMindedPeopleGeo;

   builder.Services.AddZealousMindedPeopleGeo();
   ```
3. Добавьте компонент на страницу с интерактивным рендерингом:
   ```razor
   @page "/globe"
   @rendermode InteractiveServer
   @using ZealousMindedPeopleGeo.Components

   <CommunityGlobeComponent Width="800" Height="600" ShowControls="true" />
   <CommunityMapComponent MapId="map" />
   ```

Стили и скрипты библиотека подключит сама. Подробности — в разделе
[Встраивание в приложение](docs/integration.md).

## Документация

Вся документация лежит в [docs/](docs/README.md).

**Для приложения на C# / Blazor**

- [Встраивание в приложение](docs/integration.md)
- [Компоненты](docs/components.md): карта, глобус, день и ночь, панели
- [Гео-данные: точки и контейнеры](docs/geo-data.md)
- [Своё хранилище](docs/storage.md)
- [Конфигурация и безопасность](docs/configuration.md)
- [Модели, сервисы и ошибки](docs/services.md)
- [Язык сообщений](docs/localization.md)
- [PWA и сервис-воркер](docs/pwa.md)

**Для JavaScript**

- [JavaScript в браузере](docs/javascript.md): карта и глобус без Blazor, опции, отладка

**Для разработки библиотеки**

- [Разработка](docs/development.md): витрина, тесты C# и JavaScript, CI

```bash
dotnet test tests/ZealousMindedPeopleGeo.Tests   # тесты C#
node --test experiments/*.test.mjs               # тесты JavaScript (Node 22+)
```

## 🙏 Благодарности

- **Three.js** - Библиотека для 3D графики
- **OpenStreetMap и Nominatim** - Поиск адресов без ключа
- **Google Maps API** - Поиск адресов по ключу
- **ASP.NET Core** - Платформа веб-разработки

---

*"География людей, объединенных стремлением сделать мир добрее и гармоничнее"*

