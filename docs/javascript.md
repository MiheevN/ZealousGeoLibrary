# JavaScript в браузере

[← Документация](README.md) · Раздел для JavaScript

Карта, глобус, PWA и подключение стилей работают в браузере, их код лежит в `wwwroot/`.
Скрипты — обычные ES-модули. Они входят в библиотеку и отдаются как статические файлы
`/_content/ZealousMindedPeopleGeo/...`. Сборки нет: нет ни `package.json`, ни npm, ни
бандлера. Three.js r180 тоже лежит в библиотеке (`js/libs/`), а не берётся с CDN.

Приложению на Blazor эта страница не нужна: компоненты загружают скрипты сами, а C#
вызывает их через JS interop. Она пригодится, если вы:

- используете карту или глобус на странице без Blazor;
- задаёте опции JS-глобуса, которых нет в `GlobeOptions`;
- отлаживаете скрипты в консоли браузера;
- меняете скрипты самой библиотеки (тогда см. ещё [Разработку](development.md#тесты-javascript-node)).

Node.js нужен только для тестов этих скриптов при разработке библиотеки. Ни приложению,
ни браузеру он не нужен.

## Файлы

| Файл | Что делает | Кто загружает |
|---|---|---|
| `ZealousMindedPeopleGeo.lib.module.js` | JS-инициализатор: добавляет `css/zealous-geo.css` в `<head>` | Blazor, сам, в каждом приложении с библиотекой |
| `js/community-globe.js` | 3D-глобус на Three.js: текстуры, атмосфера, день и ночь, маркеры, подписи, группы, касания и клавиатура | `ThreeJsGlobeService` (`import`) |
| `js/label-layout.js`, `js/label-scale.js` | Раскладка подписей глобуса без наложений и их размер на экране | `community-globe.js` |
| `js/libs/three.module.js`, `three.core.js`, `OrbitControls.js` | Three.js r180 | `community-globe.js` |
| `js/community-map.js` | 2D-карта на Canvas: проекции, группы, жесты, клавиатура | `CommunityMapComponent` (`import`); функции кладёт в `window` |
| `js/pwa.js` | Манифест, установка приложения, регистрация сервис-воркера | `PwaService` (`import`) |
| `sw.js` | Сервис-воркер: офлайн-кэш (см. [PWA](pwa.md)) | `pwa.js` |

## 2D-карта (`community-map.js`)

Без Blazor карта инициализируется так:

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

## 3D-глобус (`community-globe.js`)

Глобус создаётся в элементе с заданным `id`. `createGlobe` ждёт загрузки текстур и
возвращает `{ success, errorCode, errorMessage }`; коды ошибок — `GLOBE_ERROR_CODES`
(например, `webgl-unavailable`).

```js
import { createGlobe, addParticipants, centerOn, dispose } from '/_content/ZealousMindedPeopleGeo/js/community-globe.js';

const result = await createGlobe('globe', { dayNightMode: 'realTime', clusterRadius: 60 });
if (result.success) {
    addParticipants('globe', [{ id: 'berlin', name: 'Berlin', latitude: 52.52, longitude: 13.405, markerColor: '#3987e5' }]);
    centerOn('globe', 52.52, 13.405, 2);
}
// dispose('globe') — освободить WebGL, когда глобус больше не нужен
```

Опции называются как свойства `GlobeOptions` в camelCase (`autoRotateSpeed`,
`textureQuality`, `nightLightsIntensity`…). Кроме них есть опции, которых в C# нет, в том
числе:

| Опция | По умолчанию | Что делает |
|---|---|---|
| `participantLabelCollisionAvoidance` | `true` | Не давать подписям точек налезать друг на друга |
| `participantLabelCollisionPadding` | `4` | Зазор между подписями в пикселях |
| `clusterPoints`, `clusterRadius` | `true`, `40` | Группы близких маркеров |
| `texts`, `locale` | английский | Надписи для экранного диктора и подсказки про клавиши |

Полный список — `defaultOptions` в конструкторе класса `CommunityGlobe` в `community-globe.js`.

## Журнал в консоли браузера

Скрипты глобуса и 2D-карты в консоли браузера показывают только предупреждения и ошибки.
Подробный журнал включается в консоли браузера: `globalThis.zealousGeoDebug = true`
(до загрузки страницы — например, через закладку или `addInitScript` в тестах).
`debugGlobeState(containerId)` печатает состояние глобуса.

Журнал C#-части настраивается через `ILogger`, см. [Журнал](services.md#журнал).
