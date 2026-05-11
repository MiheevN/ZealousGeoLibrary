import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

async function loadMapModule() {
    // community-map.js предполагает наличие window/document, поэтому подгружаем
    // источник в data: URL после внедрения минимальных DOM-заглушек. Этого
    // достаточно для проверки чистых функций проекции и зумирования.
    const source = await readText('wwwroot/js/community-map.js');
    const harness = [
        'const documentStub = { createElement: () => ({ classList: {add(){},remove(){}}, setAttribute(){}, addEventListener(){}, appendChild(){}, style: {}, getContext: () => ({}) }), getElementById: () => null };',
        'const windowStub = { devicePixelRatio: 1, addEventListener: () => {}, removeEventListener: () => {}, clearTimeout: () => {}, setTimeout: () => 0 };',
        'const ResizeObserverStub = class { constructor() {} observe() {} disconnect() {} };',
        'const navigatorStub = { geolocation: undefined };',
        'globalThis.window = globalThis.window || windowStub;',
        'globalThis.document = globalThis.document || documentStub;',
        'globalThis.navigator = globalThis.navigator || navigatorStub;',
        'globalThis.ResizeObserver = globalThis.ResizeObserver || ResizeObserverStub;'
    ].join('\n');

    const exportFooter = [
        '',
        'export { clampZoom, clampLat, wrapLng, projectionScale, projectToCanvas, projectPolygonPoint, canvasToLatLng, zoomAt, isFinitePair, escapeHtml, buildWorldLandPaths };',
        ''
    ].join('\n');

    const moduleSource = `${harness}\n${source}\n${exportFooter}`;
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    return import(moduleUrl);
}

test('2D map JS exposes statically-renderable public API', async () => {
    const source = await readText('wwwroot/js/community-map.js');

    assert.match(source, /window\.initializeCommunityMap\s*=/, 'still exposes initializeCommunityMap');
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

test('2D map projection round-trips between lat/lng and screen coordinates', async () => {
    const { projectToCanvas, canvasToLatLng } = await loadMapModule();

    const state = {
        width: 800,
        height: 600,
        zoom: 2,
        centerLat: 0,
        centerLng: 0
    };

    const center = projectToCanvas(state, 0, 0);
    assert.equal(center.x, 400);
    assert.equal(center.y, 300);

    const moscow = projectToCanvas(state, 55.75, 37.62);
    const back = canvasToLatLng(state, moscow.x, moscow.y);
    assert.ok(Math.abs(back.lat - 55.75) < 1e-6, `lat round-trips, got ${back.lat}`);
    assert.ok(Math.abs(back.lng - 37.62) < 1e-6, `lng round-trips, got ${back.lng}`);
});

test('zoom clamps within sane limits and keeps focal point stable', async () => {
    const { zoomAt, clampZoom, canvasToLatLng } = await loadMapModule();

    assert.equal(clampZoom(0.1), 1, 'zoom never drops below 1');
    assert.equal(clampZoom(999), 20, 'zoom never exceeds 20');
    assert.equal(clampZoom(Number.NaN), 2, 'invalid zoom falls back to 2');

    const state = {
        width: 800,
        height: 600,
        zoom: 2,
        centerLat: 0,
        centerLng: 0
    };
    const pointer = { x: 600, y: 200 };
    const before = canvasToLatLng(state, pointer.x, pointer.y);
    zoomAt(state, 2, pointer);
    const after = canvasToLatLng(state, pointer.x, pointer.y);
    assert.ok(Math.abs(before.lat - after.lat) < 1e-6, 'lat under pointer preserved after zoom');
    assert.ok(Math.abs(before.lng - after.lng) < 1e-6, 'lng under pointer preserved after zoom');
    assert.equal(state.zoom, 4);
});

test('lat/lng helpers clamp to map domain', async () => {
    const { clampLat, wrapLng } = await loadMapModule();

    assert.equal(clampLat(120), 85, 'lat is clamped to safe display range');
    assert.equal(clampLat(-200), -85);
    assert.equal(clampLat(Number.NaN), 0);
    assert.equal(wrapLng(190), -170, 'lng wraps around the date line');
    assert.equal(wrapLng(-190), 170);
    assert.equal(wrapLng(0), 0);
});

test('polygon projection keeps adjacent vertices monotonic across the date line', async () => {
    const { projectPolygonPoint } = await loadMapModule();

    const state = {
        width: 800,
        height: 600,
        zoom: 1,
        centerLat: 0,
        centerLng: 0
    };

    // Камчатка → Алеуты: соседние точки расположены по разные стороны линии
    // перемены даты. wrapLng свернул бы их к противоположным краям карты,
    // создавая ложную линию через всю карту.
    const first = projectPolygonPoint(state, 55, 170, null);
    const second = projectPolygonPoint(state, 55, -170, first.dx);
    assert.ok(Math.abs(second.x - first.x) < 100, 'adjacent vertices stay close on screen');
    assert.equal(second.dx, 190, 'dx is unwrapped to preserve continuity');
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
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\(\s*"initializeCommunityMap"[\s\S]*MapId\)/, 'initializeCommunityMap receives MapId');
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\("loadParticipantsOnMap", participantsJson, MapId\)/, 'loadParticipantsOnMap receives MapId');
    assert.match(codeBehind, /JSRuntime\.InvokeVoidAsync\("disposeCommunityMap", MapId\)/, 'DisposeAsync releases the JS map');
    assert.match(razor, /id="@MapId"/, 'container uses MapId in markup');
});

test('CSS supports the new canvas-based map UI', async () => {
    const css = await readText('wwwroot/css/community-map.css');

    assert.match(css, /\.community-map-canvas\s*\{/, 'canvas element has dedicated styles');
    assert.match(css, /\.community-map-controls\s*\{/, 'zoom controls are styled');
    assert.match(css, /\.community-map-control-btn\s*\{/, 'zoom control buttons are styled');
    assert.match(css, /\.community-map-tooltip\s*\{/, 'tooltip is styled');
});
