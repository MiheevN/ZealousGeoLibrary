import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

async function loadMapModule() {
    // community-map.js предполагает наличие window/document, поэтому подгружаем
    // источник в data: URL после внедрения минимальных DOM-заглушек. Этого
    // достаточно для проверки чистых функций проекции и зумирования.
    // В Node 21+ globalThis.navigator — свойство только для чтения, поэтому
    // заглушки ставим только там, где глобального объекта ещё нет.
    const source = await readText('wwwroot/js/community-map.js');
    const harness = [
        'const documentStub = { createElement: () => ({ classList: {add(){},remove(){}}, setAttribute(){}, addEventListener(){}, appendChild(){}, style: {}, getContext: () => ({}) }), getElementById: () => null };',
        'const windowStub = { devicePixelRatio: 1, addEventListener: () => {}, removeEventListener: () => {}, clearTimeout: () => {}, setTimeout: () => 0 };',
        'const ResizeObserverStub = class { constructor() {} observe() {} disconnect() {} };',
        'const navigatorStub = { geolocation: undefined };',
        'if (!("window" in globalThis)) globalThis.window = windowStub;',
        'if (!("document" in globalThis)) globalThis.document = documentStub;',
        'if (!("navigator" in globalThis)) globalThis.navigator = navigatorStub;',
        'if (!("ResizeObserver" in globalThis)) globalThis.ResizeObserver = ResizeObserverStub;'
    ].join('\n');

    const exportFooter = [
        '',
        'export { clampZoom, clampLat, wrapLng, viewScale, getView, setCenter, projectToCanvas, canvasToLatLng, zoomAt, commitView, isFinitePair, escapeHtml, buildWorldLandPaths, buildProjectedLand, densifyRing, resolveProjection, equalEarthForward, equalEarthInverse, PROJECTIONS, normalizePoint, participantToPoint, markerLabel, renderPointTooltip, DEFAULT_MARKER_COLOR };',
        ''
    ].join('\n');

    const moduleSource = `${harness}\n${source}\n${exportFooter}`;
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    return import(moduleUrl);
}

function createState(map, overrides = {}) {
    const { projection, ...rest } = overrides;
    return {
        width: 800,
        height: 600,
        zoom: 2,
        centerX: 0,
        centerY: 0,
        centralMeridian: 0,
        projection: map.resolveProjection(projection),
        ...rest
    };
}

const near = (actual, expected, tolerance, message) =>
    assert.ok(Math.abs(actual - expected) <= tolerance, `${message}: ожидалось ${expected}, получено ${actual}`);

test('2D map JS exposes statically-renderable public API', async () => {
    const source = await readText('wwwroot/js/community-map.js');

    assert.match(source, /window\.initializeCommunityMap\s*=/, 'still exposes initializeCommunityMap');
    assert.match(source, /window\.loadPointsOnMap\s*=/, 'exposes loadPointsOnMap');
    assert.match(source, /window\.loadParticipantsOnMap\s*=/, 'still exposes loadParticipantsOnMap');
    assert.match(source, /window\.centerMapOnUserLocation\s*=/, 'still exposes centerMapOnUserLocation');
    assert.match(source, /window\.focusOnParticipant\s*=/, 'still exposes focusOnParticipant');
    assert.match(source, /window\.disposeCommunityMap\s*=/, 'exposes disposeCommunityMap for cleanup');
    assert.match(source, /createMapInstance\(container,/, 'builds an instance per container');
    assert.match(source, /document\.createElement\(['"]canvas['"]\)/, 'renders into a canvas element');
    // Google Maps API больше не загружается: реализация полностью локальная.
    assert.doesNotMatch(source, /maps\.googleapis\.com/, 'must not load Google Maps script');
    assert.doesNotMatch(source, /new google\.maps\.Map\(/, 'must not instantiate google.maps.Map');
});

test('Equal Earth is the default projection and names are case-insensitive', async () => {
    const map = await loadMapModule();

    assert.equal(map.resolveProjection(undefined).name, 'equalEarth');
    assert.equal(map.resolveProjection('EqualEarth').name, 'equalEarth', 'C# enum name is accepted');
    assert.equal(map.resolveProjection('equal-earth').name, 'equalEarth');
    assert.equal(map.resolveProjection('Equirectangular').name, 'equirectangular');
    assert.equal(map.resolveProjection('mercator').name, 'equalEarth', 'unknown name falls back to Equal Earth');
});

test('Equal Earth forward projection matches the d3-geo reference implementation', async () => {
    const { equalEarthForward } = await loadMapModule();

    // Значения получены из geoEqualEarthRaw пакета d3-geo@3.
    const reference = [
        { lng: 180, lat: 0, x: 2.7066299836960748, y: 0 },
        { lng: 0, lat: 90, x: 0, y: 1.3173627591574133 },
        { lng: 180, lat: 90, x: 1.6035886482840516, y: 1.3173627591574133 },
        { lng: 37.6176, lat: 55.7558, x: 0.4438475389191948, y: 1.0288875940336335 },
        { lng: -74.006, lat: 40.7128, x: -0.9818614499858501, y: 0.7870642200491075 },
        { lng: 151.2093, lat: -33.8688, x: 2.087106438222146, y: -0.6646837767161223 },
        { lng: -179.5, lat: -85, x: -1.6163886680475374, y: -1.3099478794260733 }
    ];
    for (const point of reference) {
        const [x, y] = equalEarthForward(point.lng, point.lat);
        near(x, point.x, 1e-12, `x для (${point.lng}, ${point.lat})`);
        near(y, point.y, 1e-12, `y для (${point.lng}, ${point.lat})`);
    }
});

test('Equal Earth inverse round-trips over the whole globe', async () => {
    const { equalEarthForward, equalEarthInverse } = await loadMapModule();

    let maxError = 0;
    for (let lat = -90; lat <= 90; lat += 5) {
        for (let lng = -180; lng <= 180; lng += 7.5) {
            const [x, y] = equalEarthForward(lng, lat);
            const [lng2, lat2] = equalEarthInverse(x, y);
            maxError = Math.max(maxError, Math.abs(lat2 - lat));
            // На полюсах долгота вырождается: полюс — отрезок, но x на нём линеен по долготе.
            maxError = Math.max(maxError, Math.abs(lng2 - lng));
        }
    }
    assert.ok(maxError < 1e-9, `max round-trip error ${maxError}°`);
});

test('Equal Earth preserves areas', async () => {
    const { equalEarthForward } = await loadMapModule();

    // Площадь трапеции 10°×10° на сфере пропорциональна sin(lat2) − sin(lat1).
    // В равновеликой проекции отношение площади на карте к площади на сфере
    // одинаково для экватора и для высоких широт.
    const projectedArea = (lat1, lat2) => {
        const ring = [];
        for (let lng = 0; lng <= 10; lng += 0.5) ring.push(equalEarthForward(lng, lat1));
        for (let lat = lat1; lat <= lat2; lat += 0.5) ring.push(equalEarthForward(10, lat));
        for (let lng = 10; lng >= 0; lng -= 0.5) ring.push(equalEarthForward(lng, lat2));
        for (let lat = lat2; lat >= lat1; lat -= 0.5) ring.push(equalEarthForward(0, lat));
        let area = 0;
        for (let i = 0; i < ring.length - 1; i += 1) {
            area += ring[i][0] * ring[i + 1][1] - ring[i + 1][0] * ring[i][1];
        }
        return Math.abs(area) / 2;
    };
    const sphereArea = (lat1, lat2) => Math.sin(lat2 * Math.PI / 180) - Math.sin(lat1 * Math.PI / 180);

    const equator = projectedArea(0, 10) / sphereArea(0, 10);
    const arctic = projectedArea(70, 80) / sphereArea(70, 80);
    near(arctic / equator, 1, 1e-3, 'отношение площадей у полюса и у экватора');
});

test('2D map projection round-trips between lat/lng and screen coordinates', async () => {
    const map = await loadMapModule();

    for (const projection of ['equalEarth', 'equirectangular']) {
        for (const centralMeridian of [0, 150, -100]) {
            const state = createState(map, { projection, centralMeridian, zoom: 3 });
            map.setCenter(state, 30, centralMeridian + 20);

            for (const [lat, lng] of [[55.75, 37.62], [-33.87, 151.21], [64.15, -21.94], [35.68, 139.65]]) {
                const point = map.projectToCanvas(state, lat, lng);
                const back = map.canvasToLatLng(state, point.x, point.y);
                assert.ok(back, `${projection}/${centralMeridian}: точка (${lat}, ${lng}) лежит на карте`);
                near(back.lat, lat, 1e-6, `${projection}/${centralMeridian}: широта`);
                near(map.wrapLng(back.lng - lng), 0, 1e-6, `${projection}/${centralMeridian}: долгота`);
            }
        }
    }
});

test('central meridian sits in the middle of the map', async () => {
    const map = await loadMapModule();

    const state = createState(map, { centralMeridian: 150, zoom: 1 });
    const center = map.projectToCanvas(state, 0, 150);
    near(center.x, 400, 1e-9, 'центральный меридиан по центру');
    near(center.y, 300, 1e-9, 'экватор по центру');

    // Тихий океан не разрезан: Токио и Гавайи по разные стороны от центра.
    const tokyo = map.projectToCanvas(state, 35.68, 139.65);
    const honolulu = map.projectToCanvas(state, 21.31, -157.86);
    assert.ok(tokyo.x < 400 && honolulu.x > 400, 'Токио левее центра, Гавайи правее');
});

test('Equal Earth camera keeps the map inside the viewport', async () => {
    const map = await loadMapModule();

    // При zoom = 1 карта целиком вписана в окно и не сдвигается.
    const whole = createState(map, { zoom: 1 });
    map.setCenter(whole, 60, 100);
    const wholeView = map.getView(whole);
    assert.equal(wholeView.centerX, 0);
    assert.equal(wholeView.centerY, 0);
    assert.equal(map.canvasToLatLng(whole, 2, 2), null, 'угол окна вне овала карты');

    // При приближении край карты не отрывается от края окна.
    const zoomed = createState(map, { zoom: 4 });
    zoomed.centerX = 100;
    zoomed.centerY = -100;
    map.commitView(zoomed);
    const view = map.getView(zoomed);
    const { xMax, yMax } = zoomed.projection;
    near(view.centerX + 400 / view.scale, xMax, 1e-12, 'правый край карты у правого края окна');
    near(view.centerY - 300 / view.scale, -yMax, 1e-12, 'южный край карты у нижнего края окна');
});

test('equirectangular map wraps horizontally', async () => {
    const map = await loadMapModule();

    const state = createState(map, { projection: 'equirectangular', zoom: 2 });
    map.setCenter(state, 0, 170);
    const before = map.canvasToLatLng(state, 400, 300);
    state.centerX += 2 * Math.PI;
    map.commitView(state);
    const after = map.canvasToLatLng(state, 400, 300);
    near(after.lng, before.lng, 1e-9, 'полный оборот возвращает тот же вид');

    // Точка за линией перемены даты берётся из ближайшей копии мира.
    const fiji = map.projectToCanvas(state, -17, -179);
    assert.ok(Math.abs(fiji.x - 400) < 100, `Фиджи рядом с центром, x = ${fiji.x}`);
});

test('zoom clamps within sane limits and keeps focal point stable', async () => {
    const map = await loadMapModule();

    assert.equal(map.clampZoom(0.1), 1, 'zoom never drops below 1');
    assert.equal(map.clampZoom(999), 20, 'zoom never exceeds 20');
    assert.equal(map.clampZoom(Number.NaN), 2, 'invalid zoom falls back to 2');

    for (const projection of ['equalEarth', 'equirectangular']) {
        const state = createState(map, { projection, zoom: 2 });
        const pointer = { x: 520, y: 220 };
        const before = map.canvasToLatLng(state, pointer.x, pointer.y);
        map.zoomAt(state, 2, pointer);
        const after = map.canvasToLatLng(state, pointer.x, pointer.y);
        near(after.lat, before.lat, 1e-9, `${projection}: широта под указателем`);
        near(after.lng, before.lng, 1e-9, `${projection}: долгота под указателем`);
        assert.equal(state.zoom, 4);
    }
});

test('lat/lng helpers clamp to map domain', async () => {
    const { clampLat, wrapLng } = await loadMapModule();

    assert.equal(clampLat(120), 90, 'lat is clamped to the pole');
    assert.equal(clampLat(-200), -90);
    assert.equal(clampLat(Number.NaN), 0);
    assert.equal(wrapLng(190), -170, 'lng wraps around the date line');
    assert.equal(wrapLng(-190), 170);
    assert.equal(wrapLng(0), 0);
});

test('land polygons stay on the map for any central meridian', async () => {
    const map = await loadMapModule();
    const shapes = map.buildWorldLandPaths();
    const projection = map.resolveProjection('equalEarth');

    const vertexCount = shapes.reduce((sum, shape) => sum + map.densifyRing(shape, 2).length, 0);
    // Обратное преобразование линейно продолжается за ±180°, поэтому по нему
    // видно, попадает ли вершина копии внутрь контура карты.
    const isInside = ([x, y]) => Math.abs(projection.inverse(x, y)[0]) <= 180 + 1e-9;
    const [, edgeY] = projection.forward(0, -65);
    const [edgeX] = projection.forward(180, -65);

    // Раньше Северная Америка уезжала за край карты при центре 20°,
    // а Антарктида схлопывалась в полосу нулевой ширины.
    for (const centralMeridian of [-180, -150, -90, -20, 0, 20, 90, 150, 180]) {
        const rings = map.buildProjectedLand(projection, centralMeridian);

        // Каждая вершина каждой фигуры видна хотя бы в одной из копий.
        const insideCount = rings.reduce((sum, ring) => sum + ring.filter(isInside).length, 0);
        assert.ok(insideCount >= vertexCount, `${centralMeridian}: видно ${insideCount} из ${vertexCount} вершин`);

        // Антарктида — полоса вдоль всей карты: её куски вместе покрывают
        // всю ширину карты на широте −65°.
        const coverage = rings
            .map((ring) => ring.filter(([, y]) => Math.abs(y - edgeY) < 1e-12).map(([x]) => x))
            .filter((xs) => xs.length > 0)
            .reduce((sum, xs) => {
                const left = Math.max(-edgeX, Math.min(...xs));
                const right = Math.min(edgeX, Math.max(...xs));
                return sum + Math.max(0, right - left);
            }, 0);
        near(coverage, 2 * edgeX, 0.02 * edgeX, `${centralMeridian}: ширина Антарктиды`);
    }
});

test('densified rings mark date line edges as seams', async () => {
    const { densifyRing } = await loadMapModule();

    const band = densifyRing([[-180, -65], [180, -65], [180, -85], [-180, -85], [-180, -65]], 2);
    assert.ok(band.length > 360, 'long edges are split into short segments');
    assert.ok(band.every(([lng]) => lng >= -180 && lng <= 180), 'edges are straight in lng/lat');
    const seams = band.filter(([, , seam]) => seam);
    assert.ok(seams.every(([lng]) => Math.abs(lng) === 180), 'only date line vertices are seams');
});

test('world land shapes cover all main continents', async () => {
    const { buildWorldLandPaths } = await loadMapModule();

    const shapes = buildWorldLandPaths();
    assert.ok(shapes.length >= 8, 'at least eight continental/island polygons');
    for (const shape of shapes) {
        assert.ok(shape.length >= 4, 'each polygon has enough points to be visible');
        for (const point of shape) {
            assert.ok(Array.isArray(point) && point.length === 2, 'point is [lng, lat]');
            const [lng, lat] = point;
            assert.ok(lng >= -180 && lng <= 180, `lng ${lng} in range`);
            assert.ok(lat >= -90 && lat <= 90, `lat ${lat} in range`);
        }
    }
});

test('Razor component supports multiple instances via MapId parameter', async () => {
    const codeBehind = await readText('Components/CommunityMapComponent.razor.cs');
    const razor = await readText('Components/CommunityMapComponent.razor');

    assert.match(codeBehind, /\[Parameter\] public string MapId/, 'MapId parameter declared');
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\(\s*"initializeCommunityMap"[\s\S]*MapId,/, 'initializeCommunityMap receives MapId');
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\("loadPointsOnMap", JsonSerializer\.Serialize\(payload, PointJsonOptions\), MapId\)/, 'loadPointsOnMap receives MapId');
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\("disposeCommunityMap", MapId\)/, 'DisposeAsync releases the JS map');
    assert.match(razor, /id="@MapId"/, 'container uses MapId in markup');
});

test('Razor component loads the map script once as a module', async () => {
    const codeBehind = await readText('Components/CommunityMapComponent.razor.cs');
    const razor = await readText('Components/CommunityMapComponent.razor');

    // <script> в <HeadContent> исполнялся дважды при пререндере и терялся,
    // если на странице был ещё один компонент со своим HeadContent.
    assert.doesNotMatch(razor, /<script[^>]*community-map\.js/, 'no script tag in HeadContent');
    assert.match(codeBehind, /InvokeAsync<IJSObjectReference>\("import", MapScriptPath\)/, 'script is imported as a module');
    assert.match(codeBehind, /MapScriptPath = "\/_content\/ZealousMindedPeopleGeo\/js\/community-map\.js"/, 'module path points to the static asset');
});

test('Razor component passes projection and central meridian to JS', async () => {
    const codeBehind = await readText('Components/CommunityMapComponent.razor.cs');
    const configuration = await readText('Models/Configuration.cs');

    assert.match(codeBehind, /\[Parameter\] public MapProjection\? Projection/, 'Projection parameter declared');
    assert.match(codeBehind, /\[Parameter\] public double\? CentralMeridian/, 'CentralMeridian parameter declared');
    assert.match(codeBehind,
        /new\s*\{\s*projection = projection\.ToString\(\),\s*centralMeridian,\s*clustering = _appliedClustering\.Enabled,\s*clusterRadius = _appliedClustering\.Radius,\s*dotNetHelper = _dotNetRef\s*\}/,
        'options object is passed to JS');
    assert.match(configuration, /public MapProjection Projection \{ get; set; \} = MapProjection\.EqualEarth;/, 'Equal Earth is the configured default');
    assert.match(configuration, /enum MapProjection\s*\{[\s\S]*EqualEarth,[\s\S]*Equirectangular/, 'both projections are available');
});

test('CSS supports the new canvas-based map UI', async () => {
    const css = await readText('wwwroot/css/community-map.css');

    assert.match(css, /\.community-map-canvas\s*\{/, 'canvas element has dedicated styles');
    assert.match(css, /\.community-map-controls\s*\{/, 'zoom controls are styled');
    assert.match(css, /\.community-map-control-btn\s*\{/, 'zoom control buttons are styled');
    assert.match(css, /\.community-map-tooltip\s*\{/, 'tooltip is styled');
});

test('points are normalized and points without coordinates are dropped', async () => {
    const map = await loadMapModule();

    const point = map.normalizePoint({ id: 7, latitude: '52.5', longitude: 13.4, title: 'Berlin', properties: { staff: '40' } });
    assert.equal(point.id, '7');
    assert.equal(point.latitude, 52.5);
    assert.deepEqual(point.properties, { staff: '40' });
    assert.equal(point.color, null);
    assert.equal(map.normalizePoint({ id: 'x', title: 'Nowhere' }), null);
    assert.equal(map.normalizePoint(null), null);
});

test('participants in the old format become the same points as on the server', async () => {
    const map = await loadMapModule();

    const point = map.participantToPoint({
        Id: 'a1', Name: 'Anna', Message: 'Hi', Latitude: 55.7, Longitude: 37.6,
        Email: 'anna@example.com', Address: '', City: 'Moscow',
        SocialContacts: { Telegram: '@anna' }, RegisteredAt: '2025-01-01T00:00:00Z'
    });
    assert.equal(point.id, 'a1');
    assert.equal(point.title, 'Anna');
    assert.equal(point.description, 'Hi');
    assert.deepEqual(point.properties, {
        email: 'anna@example.com', city: 'Moscow', telegram: '@anna', registeredAt: '2025-01-01T00:00:00Z'
    });
    // camelCase из JSON-сериализатора Blazor тоже читается
    assert.equal(map.participantToPoint({ id: 'b', name: 'Bob', latitude: 1, longitude: 2 }).title, 'Bob');
    assert.equal(map.participantToPoint({ Id: 'c', Name: 'No coordinates' }), null);
});

test('marker label is a short icon or the first letter of the title', async () => {
    const map = await loadMapModule();

    assert.equal(map.markerLabel({ title: 'berlin' }), 'B');
    assert.equal(map.markerLabel({ title: '', icon: null }), '?');
    assert.equal(map.markerLabel({ title: 'Event', icon: '🎤' }), '🎤');
    assert.equal(map.markerLabel({ title: 'Event', icon: 'https://example.com/icon.png' }), 'E');
    assert.equal(map.markerLabel({ title: 'ёлка' }), 'Ё');
});

test('tooltip escapes everything that comes from data, including property names', async () => {
    const map = await loadMapModule();

    const html = map.renderPointTooltip({
        id: '1', latitude: 1, longitude: 2, title: '<img src=x onerror=alert(1)>', category: 'office',
        properties: { '<b>key</b>': '<script>x</script>', email: 'a@example.com', city: 'Berlin', country: 'Germany' }
    });
    assert.doesNotMatch(html, /<img|<script|<b>/);
    assert.match(html, /&lt;img/);
    assert.match(html, /🏷 Категория:/);
    assert.match(html, /📧 Email:/);
    assert.match(html, /Berlin, Germany/);
});
