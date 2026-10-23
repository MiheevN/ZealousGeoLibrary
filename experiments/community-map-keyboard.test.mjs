import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

// Холст 800×500 в левом верхнем углу окна.
const RECT = { left: 0, top: 0, width: 800, height: 500, right: 800, bottom: 500 };

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

// Элемент DOM: запоминает атрибуты и подписки и умеет вызвать их как браузер.
function element() {
    const listeners = new Map();
    return {
        style: {},
        attributes: {},
        className: '',
        innerHTML: '',
        textContent: '',
        children: [],
        classList: { add() {}, remove() {}, toggle() {} },
        setAttribute(name, value) {
            this.attributes[name] = String(value);
        },
        removeAttribute(name) {
            delete this.attributes[name];
        },
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
            const full = { button: 0, pointerId: 1, defaultPrevented: false, ...event };
            full.preventDefault = () => {
                full.defaultPrevented = true;
            };
            for (const handler of listeners.get(type) ?? []) {
                handler(full);
            }
            return full;
        },
        getBoundingClientRect: () => RECT,
        getContext: () => contextStub(),
        setPointerCapture() {},
        releasePointerCapture() {}
    };
}

async function loadMapModule() {
    const source = await readText('wwwroot/js/community-map.js');
    const stateHook = 'return {\n        draw,';
    assert.ok(source.includes(stateHook), 'createMapInstance возвращает объект, начинающийся с draw');
    const harness = [
        'if (!("window" in globalThis)) globalThis.window = { devicePixelRatio: 1, addEventListener() {}, removeEventListener() {}, setTimeout, clearTimeout };',
        'if (!("ResizeObserver" in globalThis)) globalThis.ResizeObserver = class { observe() {} disconnect() {} };'
    ].join('\n');
    const exportFooter = '\nexport { canvasToLatLng, ZOOM_STEP };\n';
    const instrumented = source.replace(stateHook, `(globalThis.__mapStates ??= []).push(state);\n    ${stateHook}`);
    return import(`data:text/javascript;base64,${Buffer.from(`${harness}\n${instrumented}\n${exportFooter}`).toString('base64')}`);
}

globalThis.document = {
    createElement: () => element(),
    getElementById: (id) => globalThis.__containers?.get(id) ?? null
};

const map = await loadMapModule();

// Новая карта 800×500 с центром (20, 0); clicks — id точек, которые «открыли».
function createMap({ zoom = 2, points = [], clustering = false, texts } = {}) {
    const id = `map-${(globalThis.__containers ??= new Map()).size + 1}`;
    const container = element();
    container.id = id;
    globalThis.__containers.set(id, container);
    const clicks = [];
    const dotNetHelper = { invokeMethodAsync: (method, pointId) => clicks.push(pointId) };
    window.initializeCommunityMap(null, 20, 0, zoom, id, { dotNetHelper, clustering, texts });
    if (points.length > 0) {
        window.loadPointsOnMap(points, id);
    }
    const state = globalThis.__mapStates.at(-1);
    const press = (key, extra = {}) => state.canvas.dispatch('keydown', { key, code: '', ...extra });
    return { state, container, canvas: state.canvas, clicks, press };
}

const near = (actual, expected, tolerance, message) =>
    assert.ok(Math.abs(actual - expected) <= tolerance, `${message}: ожидалось ${expected}, получено ${actual}`);

// Точки вдоль экватора: на экране слева направо — west, middle, east.
const ROW = [
    { id: 'east', latitude: 20, longitude: 40, title: 'East', category: 'Office' },
    { id: 'west', latitude: 20, longitude: -40, title: 'West' },
    { id: 'middle', latitude: 20, longitude: 0, title: 'Middle' }
];

test('the map canvas is focusable and tells screen readers which keys it understands', () => {
    const { canvas, container } = createMap();

    assert.equal(canvas.tabIndex, 0, 'холст в порядке Tab');
    assert.equal(canvas.attributes.role, 'application');
    const hint = container.children.find((child) => child.id === canvas.attributes['aria-describedby']);
    assert.ok(hint, 'aria-describedby указывает на подсказку внутри карты');
    assert.match(hint.textContent, /Arrow keys/);
    assert.equal(hint.className, 'community-map-sr-only');
});

test('the keyboard hint comes in the language the component passes', () => {
    const { canvas, container } = createMap({ texts: { keyboardHint: 'Стрелки сдвигают карту' } });

    const hint = container.children.find((child) => child.id === canvas.attributes['aria-describedby']);
    assert.equal(hint.textContent, 'Стрелки сдвигают карту');
});

test('arrow keys pan the map by a fifth of the view and do not scroll the page', () => {
    const { state, press } = createMap({ zoom: 4 });
    const centre = map.canvasToLatLng(state, 400, 250);

    const right = press('ArrowRight');
    assert.ok(right.defaultPrevented, 'стрелка не прокручивает страницу');
    const afterRight = map.canvasToLatLng(state, 400, 250);
    assert.ok(afterRight.lng > centre.lng, 'вправо — восточнее');
    const shifted = map.canvasToLatLng(state, 400 - 800 * 0.2, 250);
    near(shifted.lng, centre.lng, 1e-6, 'место из центра ушло на пятую часть ширины влево');

    press('ArrowLeft');
    near(map.canvasToLatLng(state, 400, 250).lng, centre.lng, 1e-6, 'влево — обратно');

    press('ArrowUp');
    assert.ok(map.canvasToLatLng(state, 400, 250).lat > centre.lat, 'вверх — севернее');
    press('ArrowDown');
    near(map.canvasToLatLng(state, 400, 250).lat, centre.lat, 1e-6, 'вниз — обратно');
});

test('plus and minus zoom around the centre, Home restores the initial view', () => {
    const { state, press } = createMap({ zoom: 3 });
    const centre = map.canvasToLatLng(state, 400, 250);

    press('+');
    near(state.zoom, 3 * map.ZOOM_STEP, 1e-9, '+ приближает');
    press('=');
    near(state.zoom, 3 * map.ZOOM_STEP ** 2, 1e-9, '= (та же клавиша без Shift) тоже');
    press('-');
    press('-', { code: 'NumpadSubtract' });
    near(state.zoom, 3, 1e-9, '− отдаляет');
    near(map.canvasToLatLng(state, 400, 250).lng, centre.lng, 1e-6, 'центр на месте');

    press('ArrowRight');
    press('+');
    press('Home');
    assert.equal(state.zoom, 3, 'Home — начальный масштаб');
    near(map.canvasToLatLng(state, 400, 250).lng, centre.lng, 1e-6, 'и начальный центр');
});

test('keys with Ctrl, Cmd or Alt stay with the browser', () => {
    const { state, press } = createMap({ zoom: 3 });

    const zoom = press('+', { ctrlKey: true });
    const back = press('ArrowLeft', { altKey: true });

    assert.equal(zoom.defaultPrevented, false, 'Ctrl + — масштаб страницы');
    assert.equal(back.defaultPrevented, false, 'Alt ← — назад в истории');
    assert.equal(state.zoom, 3);
});

test('N and P walk through visible markers left to right and announce them', () => {
    const { state, press } = createMap({ points: ROW });

    press('n', { code: 'KeyN' });
    assert.equal(state.selectedKey, 'p:1', 'первым — самый западный');
    assert.equal(state.announcer.textContent, 'West');
    assert.equal(state.tooltip.style.display, 'block', 'подсказка выбранной точки на экране');

    press('n', { code: 'KeyN' });
    assert.equal(state.selectedKey, 'p:2');
    press('n', { code: 'KeyN' });
    assert.equal(state.selectedKey, 'p:0');
    assert.equal(state.announcer.textContent, 'East, Office', 'название и категория');

    press('n', { code: 'KeyN' });
    assert.equal(state.selectedKey, 'p:1', 'по кругу');
    press('p', { code: 'KeyP' });
    assert.equal(state.selectedKey, 'p:0', 'P — назад');
});

test('N works by the physical key, so a Russian layout selects markers too', () => {
    const { state, press } = createMap({ points: ROW });

    const event = press('т', { code: 'KeyN' });

    assert.ok(event.defaultPrevented);
    assert.equal(state.selectedKey, 'p:1');
});

test('Enter opens the selected point, Escape clears the selection', () => {
    const { state, clicks, press } = createMap({ points: ROW });

    const idle = press('Enter');
    assert.equal(idle.defaultPrevented, false, 'без выбора Enter не перехватывается');
    assert.deepEqual(clicks, []);

    press('n', { code: 'KeyN' });
    press('Enter');
    assert.deepEqual(clicks, ['west'], 'как клик по маркеру');

    press('Escape');
    assert.equal(state.selectedKey, null);
    assert.equal(state.tooltip.style.display, 'none');
    assert.equal(state.announcer.textContent, '');
    assert.equal(press('Escape').defaultPrevented, false, 'нечего закрывать — Escape остаётся странице');
});

test('Enter on a group of points at one place fans it out and selects its first point', () => {
    const points = [
        { id: 'a', latitude: 10, longitude: 10, title: 'Alpha' },
        { id: 'b', latitude: 10, longitude: 10, title: 'Beta' },
        { id: 'c', latitude: 10, longitude: 10, title: 'Gamma' }
    ];
    const { state, clicks, press } = createMap({ points, clustering: true });

    press('n', { code: 'KeyN' });
    assert.match(state.selectedKey, /^c:/, 'выбрана группа');
    assert.match(state.announcer.textContent, /^3 points: Alpha, Beta, Gamma$/);

    press('Enter');
    assert.ok(state.spider, 'одинаковые координаты раскрываются веером');
    assert.equal(state.selectedKey, 'p:0');
    assert.equal(state.announcer.textContent, 'Alpha');

    press('Enter');
    assert.deepEqual(clicks, ['a']);

    press('Escape');
    press('Escape');
    assert.equal(state.spider, null, 'второй Escape сворачивает веер');
});

test('panning, a mouse press and leaving the map clear the keyboard selection', () => {
    const { state, canvas, press } = createMap({ points: ROW });

    press('n', { code: 'KeyN' });
    press('ArrowLeft');
    assert.equal(state.selectedKey, null, 'маркер уехал — выбор снят');

    press('n', { code: 'KeyN' });
    canvas.dispatch('pointerdown', { clientX: 10, clientY: 10 });
    assert.equal(state.selectedKey, null, 'мышь — выбор снят');
    canvas.dispatch('pointerup', { clientX: 10, clientY: 10 });

    press('n', { code: 'KeyN' });
    canvas.dispatch('blur');
    assert.equal(state.selectedKey, null, 'фокус ушёл — выбор снят');
    assert.equal(state.tooltip.style.display, 'none');
});

test('N on a map without visible markers does nothing', () => {
    const { state, press } = createMap();

    press('n', { code: 'KeyN' });

    assert.equal(state.selectedKey, null);
    assert.equal(state.announcer.textContent, '');
});

