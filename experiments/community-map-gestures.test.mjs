import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

// Холст в окне: левый верхний угол в (10, 20), размер 800×500.
const RECT = { left: 10, top: 20, width: 800, height: 500, right: 810, bottom: 520 };

// Холст, который всё принимает и ничего не рисует.
function contextStub() {
    return new Proxy({}, {
        get: (target, name) => (name in target ? target[name] : () => ({ width: 0, addColorStop() {} })),
        set: (target, name, value) => {
            target[name] = value;
            return true;
        }
    });
}

// Элемент DOM, который запоминает подписки и умеет вызвать их как браузер.
function element() {
    const listeners = new Map();
    return {
        style: {},
        className: '',
        innerHTML: '',
        textContent: '',
        children: [],
        classList: { add() {}, remove() {}, toggle() {} },
        setAttribute() {},
        removeAttribute() {},
        appendChild(child) {
            this.children.push(child);
            return child;
        },
        addEventListener(type, handler) {
            listeners.set(type, [...(listeners.get(type) ?? []), handler]);
        },
        removeEventListener(type, handler) {
            listeners.set(type, (listeners.get(type) ?? []).filter((item) => item !== handler));
        },
        dispatch(type, event = {}) {
            for (const handler of listeners.get(type) ?? []) {
                handler({ button: 0, pointerId: 1, preventDefault() {}, ...event });
            }
        },
        getBoundingClientRect: () => RECT,
        getContext: () => contextStub(),
        setPointerCapture() {},
        releasePointerCapture() {}
    };
}

// Скрипт карты рассчитан на браузер: подгружаем его в data: URL с заглушками DOM
// и достаём внутреннее состояние каждой созданной карты.
async function loadMapModule() {
    const source = await readText('wwwroot/js/community-map.js');
    const stateHook = 'return {\n        draw,';
    assert.ok(source.includes(stateHook), 'createMapInstance возвращает объект, начинающийся с draw');
    const harness = [
        'if (!("window" in globalThis)) globalThis.window = { devicePixelRatio: 1, addEventListener() {}, removeEventListener() {} };',
        'if (!("ResizeObserver" in globalThis)) globalThis.ResizeObserver = class { observe() {} disconnect() {} };'
    ].join('\n');
    const exportFooter = '\nexport { canvasToLatLng, wheelZoomFactor, ZOOM_STEP, MIN_ZOOM, MAX_ZOOM };\n';
    const instrumented = source.replace(stateHook, `(globalThis.__mapStates ??= []).push(state);\n    ${stateHook}`);
    const moduleSource = `${harness}\n${instrumented}\n${exportFooter}`;
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

globalThis.document = {
    createElement: () => element(),
    getElementById: (id) => globalThis.__containers?.get(id) ?? null
};

const map = await loadMapModule();

// Новая карта 800×500 с точками; clicks — id точек, по которым кликнули.
function createMap({ zoom = 2, points = [] } = {}) {
    const id = `map-${(globalThis.__containers ??= new Map()).size + 1}`;
    const container = element();
    globalThis.__containers.set(id, container);
    const clicks = [];
    const dotNetHelper = { invokeMethodAsync: (method, pointId) => clicks.push(pointId) };
    window.initializeCommunityMap(null, 20, 0, zoom, id, { dotNetHelper, clustering: false });
    if (points.length > 0) {
        window.loadPointsOnMap(points, id);
    }
    const state = globalThis.__mapStates.at(-1);
    return { state, canvas: state.canvas, clicks };
}

// Событие указателя в координатах холста (clientX/clientY с поправкой на RECT).
const at = (pointerId, x, y, extra = {}) => ({ pointerId, clientX: RECT.left + x, clientY: RECT.top + y, ...extra });

const near = (actual, expected, tolerance, message) =>
    assert.ok(Math.abs(actual - expected) <= tolerance, `${message}: ожидалось ${expected}, получено ${actual}`);

const assertSamePlace = (actual, expected, message) => {
    near(actual.lat, expected.lat, 1e-6, `${message} (широта)`);
    near(actual.lng, expected.lng, 1e-6, `${message} (долгота)`);
};

test('spreading two fingers zooms in by their distance ratio around the midpoint', () => {
    const { state, canvas } = createMap();
    const before = map.canvasToLatLng(state, 400, 250);

    canvas.dispatch('pointerdown', at(1, 350, 250));
    canvas.dispatch('pointerdown', at(2, 450, 250));
    canvas.dispatch('pointermove', at(1, 250, 250));
    canvas.dispatch('pointermove', at(2, 550, 250));

    near(state.zoom, 2 * 3, 1e-9, 'пальцы разошлись втрое — масштаб втрое больше');
    assertSamePlace(map.canvasToLatLng(state, 400, 250), before, 'место между пальцами не сдвинулось');

    canvas.dispatch('pointermove', at(1, 375, 250));
    canvas.dispatch('pointermove', at(2, 425, 250));
    near(state.zoom, 2 / 2, 1e-9, 'сведённые пальцы отдаляют карту');
});

test('two fingers moved together pan the map and keep the same place between them', () => {
    const { state, canvas } = createMap({ zoom: 4 });
    const before = map.canvasToLatLng(state, 400, 250);

    canvas.dispatch('pointerdown', at(1, 350, 250));
    canvas.dispatch('pointerdown', at(2, 450, 250));
    canvas.dispatch('pointermove', at(1, 290, 210));
    canvas.dispatch('pointermove', at(2, 390, 210));

    near(state.zoom, 4, 1e-9, 'расстояние не изменилось — масштаб тоже');
    assertSamePlace(map.canvasToLatLng(state, 340, 210), before, 'место ушло вместе с пальцами');
});

test('pinch zoom stops at the map zoom limits', () => {
    const { state, canvas } = createMap({ zoom: 10 });

    canvas.dispatch('pointerdown', at(1, 300, 250));
    canvas.dispatch('pointerdown', at(2, 500, 250));
    canvas.dispatch('pointermove', at(1, 0, 250));
    canvas.dispatch('pointermove', at(2, 800, 250));
    assert.equal(state.zoom, map.MAX_ZOOM, 'вчетверо от 10 — упёрлись в предел');

    canvas.dispatch('pointermove', at(1, 400, 250));
    canvas.dispatch('pointermove', at(2, 401, 250));
    assert.equal(state.zoom, map.MIN_ZOOM, 'в 200 раз меньше — упёрлись в предел');
});

test('lifting one finger after a pinch continues as a drag without a jump', () => {
    const { state, canvas } = createMap({ zoom: 3 });

    canvas.dispatch('pointerdown', at(1, 300, 250));
    canvas.dispatch('pointerdown', at(2, 500, 250));
    canvas.dispatch('pointermove', at(2, 600, 250));
    canvas.dispatch('pointerup', at(2, 600, 250));
    assert.equal(state.pinch, null);

    const zoom = state.zoom;
    const underFinger = map.canvasToLatLng(state, 300, 250);
    canvas.dispatch('pointermove', at(1, 360, 280));

    assert.equal(state.zoom, zoom, 'одним пальцем масштаб не меняется');
    assertSamePlace(map.canvasToLatLng(state, 360, 280), underFinger, 'место под пальцем едет вместе с ним');
});

test('when a third finger stays, the pinch continues from the new pair without a jump', () => {
    const { state, canvas } = createMap({ zoom: 3 });

    canvas.dispatch('pointerdown', at(1, 300, 250));
    canvas.dispatch('pointerdown', at(2, 500, 250));
    canvas.dispatch('pointerdown', at(3, 400, 350));
    canvas.dispatch('pointerup', at(2, 500, 250));

    const zoom = state.zoom;
    const view = { x: state.centerX, y: state.centerY };
    // Тот же кадр: пальцы на месте — карта не прыгает.
    canvas.dispatch('pointermove', at(1, 300, 250));
    assert.equal(state.zoom, zoom);
    near(state.centerX, view.x, 1e-9, 'центр по X');
    near(state.centerY, view.y, 1e-9, 'центр по Y');

    const distance = Math.hypot(100, 100);
    canvas.dispatch('pointermove', at(3, 300 + 2 * 100, 250 + 2 * 100));
    near(state.zoom, zoom * (2 * distance) / distance, 1e-9, 'новая пара пальцев масштабирует');
});

test('a pinch over a marker does not count as a tap on it, a plain tap still does', () => {
    const point = { id: 'here', latitude: 20, longitude: 0, title: 'Здесь' };
    const { canvas, clicks } = createMap({ points: [point] });

    canvas.dispatch('pointerdown', at(1, 390, 250));
    canvas.dispatch('pointerdown', at(2, 410, 250));
    canvas.dispatch('pointerup', at(2, 410, 250));
    canvas.dispatch('pointerup', at(1, 390, 250));
    canvas.dispatch('click', at(1, 400, 250));
    assert.deepEqual(clicks, [], 'щипок над маркером не открывает точку');

    // После двух пальцев Chrome клик обычно не шлёт: следующее касание всё равно — клик.
    canvas.dispatch('pointerdown', at(3, 390, 250));
    canvas.dispatch('pointerdown', at(4, 410, 250));
    canvas.dispatch('pointerup', at(4, 410, 250));
    canvas.dispatch('pointerup', at(3, 390, 250));
    canvas.dispatch('pointerdown', at(5, 400, 250));
    canvas.dispatch('pointerup', at(5, 400, 250));
    canvas.dispatch('click', at(5, 400, 250));
    assert.deepEqual(clicks, ['here']);
});

test('a finger whose capture was lost does not turn the next touch into a pinch', () => {
    const { state, canvas } = createMap();

    canvas.dispatch('pointerdown', at(1, 300, 250));
    canvas.dispatch('lostpointercapture', at(1, 300, 250));
    canvas.dispatch('pointerdown', at(2, 500, 250));

    assert.equal(state.pinch, null);
    assert.equal(state.dragging, true);
});

test('the mouse wheel zooms one step per notch, a trackpad zooms by fractions of a step', () => {
    const step = map.ZOOM_STEP;
    const factor = (event) => map.wheelZoomFactor({ deltaMode: 0, ctrlKey: false, ...event });

    near(factor({ deltaY: -100 }), step, 1e-12, 'щелчок колеса вверх (Windows)');
    near(factor({ deltaY: 100 }), 1 / step, 1e-12, 'щелчок колеса вниз');
    near(factor({ deltaY: 53 }), 1 / step, 1e-12, 'щелчок колеса в Chrome на Linux — тоже целый шаг');
    near(factor({ deltaMode: 1, deltaY: -3 }), step, 1e-12, 'Firefox считает строками');
    near(factor({ deltaY: -10 }), step ** 0.2, 1e-12, 'мелкий сдвиг тачпада — пятая часть шага');
    assert.equal(factor({ deltaY: 0, deltaX: 40 }), 1, 'сдвиг вбок масштаб не меняет');
});

test('a trackpad pinch (ctrl + wheel) follows the fingers', () => {
    const factor = (deltaY) => map.wheelZoomFactor({ deltaMode: 0, ctrlKey: true, deltaY });

    near(factor(-100 * Math.log(1.1)), 1.1, 1e-12, 'пальцы разошлись на 10%');
    near(factor(100 * Math.log(1.1)), 1 / 1.1, 1e-12, 'свели на 10%');
    near(factor(-500), map.ZOOM_STEP, 1e-12, 'Ctrl с колесом мыши — не больше шага');
});

test('wheel events zoom around the cursor; a sideways scroll leaves the map as it is', () => {
    const { state, canvas } = createMap({ zoom: 3 });
    const underCursor = map.canvasToLatLng(state, 200, 120);

    canvas.dispatch('wheel', at(0, 200, 120, { deltaMode: 0, deltaY: -100 }));
    near(state.zoom, 3 * map.ZOOM_STEP, 1e-9, 'приблизили на шаг');
    assertSamePlace(map.canvasToLatLng(state, 200, 120), underCursor, 'место под курсором');

    const zoom = state.zoom;
    canvas.dispatch('wheel', at(0, 200, 120, { deltaMode: 0, deltaY: 0, deltaX: 30 }));
    assert.equal(state.zoom, zoom);
});
