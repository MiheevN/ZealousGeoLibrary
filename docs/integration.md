# Встраивание в приложение

[← Документация](README.md) · Раздел для C# / Blazor

Как подключить библиотеку к приложению Blazor. Node.js и npm приложению не нужны: JavaScript библиотеки уже лежит в ней и загружается сам.

Достаточно двух шагов.

1. **Подключите проект библиотеки.** В NuGet библиотека не публикуется: приложение
   ссылается на её проект. Удобнее всего держать исходники подмодулем git. Из корня
   репозитория приложения:
   ```bash
   git submodule add https://github.com/MiheevN/ZealousGeoLibrary.git libs/ZealousGeoLibrary
   dotnet add MyApp/MyApp.csproj reference libs/ZealousGeoLibrary/ZealousMindedPeopleGeo.csproj
   ```

   Получится такая раскладка:
   ```
   my-repo/
   ├── MyApp/                    # проект приложения, в нём MyApp.csproj
   └── libs/ZealousGeoLibrary/   # библиотека, в ней ZealousMindedPeopleGeo.csproj
   ```

   **Библиотека не должна лежать внутри папки проекта приложения** (например,
   `MyApp/libs/...`). SDK собирает в проект все `.cs` и `.razor` из его папки и подпапок,
   и в приложение попали бы тесты и витрина библиотеки — сборка упадёт.

   Подойдёт и обычная копия репозитория вместо подмодуля, важна только ссылка
   `<ProjectReference>` на `ZealousMindedPeopleGeo.csproj`. Стили, скрипты и текстуры
   приходят по ней сами, по адресам `_content/ZealousMindedPeopleGeo/...`.

   - Обновить библиотеку: `git submodule update --remote libs/ZealousGeoLibrary`.
   - Клонировать приложение: `git clone --recurse-submodules ...` (или после клонирования
     `git submodule update --init`).

2. **Зарегистрируйте сервисы в `Program.cs`**
   ```csharp
   using ZealousMindedPeopleGeo;

   builder.Services.AddZealousMindedPeopleGeo();
   ```

Этого хватает всем компонентам. Варианты с настройками регистрируют тот же набор сервисов:

```csharp
// секция "ZealousMindedPeopleGeo" в appsettings.json
builder.Services.AddZealousMindedPeopleGeo(builder.Configuration);

// настройки в коде
builder.Services.AddZealousMindedPeopleGeo(options => options.EnableGeocoding = false);
```

Какие настройки есть и когда они нужны — в [Конфигурации](configuration.md).

Точки и участники по умолчанию хранятся в памяти и пропадают при перезапуске. Постоянное
хранение — база данных, таблица, внешний API — библиотека не навязывает: приложение
реализует интерфейсы `IGeoDataContainerManager` и `IParticipantRepository` и регистрирует
свои реализации до или после вызова (см. [Своё хранилище](storage.md)). Библиотека их не перезапишет,
а повторный вызов `AddZealousMindedPeopleGeo` ничего не дублирует.

Компоненты интерактивные, поэтому приложению нужен интерактивный рендеринг Blazor
(`AddInteractiveServerComponents()` и `@rendermode="InteractiveServer"`, как в шаблоне
Blazor Web App).

## Что библиотека подключает сама

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

## Необязательно

- **Bootstrap 5.** Компоненты размечены его классами и с ним выглядят как задумано; без него
  остаются рабочими, но оформлены проще.
- **Офлайн-кэш.** Сервис-воркер библиотеки регистрируется, только если сервер отдаёт `sw.js`
  с заголовком `Service-Worker-Allowed` (см. [PWA и сервис-воркер](pwa.md)). Без заголовка сайт и
  установка приложения работают.
