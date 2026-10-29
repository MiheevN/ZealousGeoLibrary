# PWA и сервис-воркер

[← Документация](README.md) · Раздел для C# / Blazor

Установка сайта как приложения и офлайн-кэш.

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

## Установка приложения

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

## Как воркер кэширует

| Запросы | Стратегия |
|---|---|
| JS, CSS и данные библиотеки, скрипты и стили сайта | Network First: изменённый файл приходит при следующей загрузке страницы, кэш нужен только без сети |
| Картинки и шрифты библиотеки (текстуры Земли, иконки) | Cache First, обновляются со сменой версии кэша |
| Страницы, `/api/...` и прочие GET-запросы | Network First, без сети — из кэша |

Не-GET запросы, соединение Blazor Server (`/_blazor`) и SSE воркер не перехватывает.

При каждом запуске `PwaService` проверяет, не изменился ли `sw.js`. Новый воркер активируется сразу (`skipWaiting`), удаляет кэши прошлой версии и предлагает перезагрузить страницу.

## Версия кэшей

Кэши называются `zealous-geo-static-v<версия>` и `zealous-geo-dynamic-v<версия>`, где версия — константа `SW_VERSION` в `wwwroot/sw.js`.

**Правило: `SW_VERSION` равна `<Version>` в `ZealousMindedPeopleGeo.csproj` — поднимая версию пакета, поднимайте и её.** Изменённый `sw.js` браузер ставит как новый воркер, а тот при активации удаляет все кэши `zealous-geo-*` других версий, в том числе закэшированные текстуры и иконки. Совпадение версий проверяет `node --test experiments/service-worker-cache.test.mjs`.
