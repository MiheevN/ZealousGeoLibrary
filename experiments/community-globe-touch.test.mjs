import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-keyboard.test.mjs, наружу выходят
// функции для телефона и класс.
async function loadGlobeModule() {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const layoutSource = await readFile(new URL('../wwwroot/js/label-layout.js', import.meta.url), 'utf8');
    const testSource = source
        .replace(
            /import \{ layoutLabels, labelTextRect \} from '\.\/label-layout\.js';\n/,
            () => `${layoutSource.replace(/^export /gm, '')}\n`
        )
        .replace(
            /import \{ DEFAULT_LABEL_PIXEL_HEIGHT, calculateLabelScaleForCamera \} from '\.\/label-scale\.js';\n/,
            'const DEFAULT_LABEL_PIXEL_HEIGHT = 34;\nconst calculateLabelScaleForCamera = () => ({ width: 1, height: 1 });\n'
        )
        .replace(
            /initializeDependencies\(\)\.then\(success => \{[\s\S]*?\n\}\);\n\n\/\/ Глобальный реестр/,
            'dependenciesLoaded = true;\n\n// Глобальный реестр'
        );
    const threeModuleUrl = new URL('../wwwroot/js/libs/three.module.js', import.meta.url).href;
    const moduleSource = [
        `import * as TestThree from '${threeModuleUrl}';`,
        testSource.replace('let THREE, OrbitControls;', 'let THREE = TestThree, OrbitControls;'),
        'export { CommunityGlobe, classifyTouchGesture, labelPixelHeightForViewport, touchZoomSpeed, labelRectFitsViewport };',
        'export const Three = TestThree;'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const globe3d = await loadGlobeModule();

test('a quick vertical flick scrolls the page, everything else turns the globe', () => {
    assert.equal(globe3d.classifyTouchGesture(0, -6, 5), 'pending', 'палец ещё почти не сдвинулся');
    assert.equal(globe3d.classifyTouchGesture(2, -30, 20), 'page', 'быстро вверх — страница');
    assert.equal(globe3d.classifyTouchGesture(-3, 40, 30), 'page', 'быстро вниз — страница');
    assert.equal(globe3d.classifyTouchGesture(0, -30, 200), 'globe', 'медленно — глобус');
    assert.equal(globe3d.classifyTouchGesture(-40, 2, 20), 'globe', 'быстро вбок — глобус');
    assert.equal(globe3d.classifyTouchGesture(-25, -25, 20), 'globe', 'по диагонали — глобус');
});

test('labels shrink with a small canvas but stay readable', () => {
    assert.equal(globe3d.labelPixelHeightForViewport(600), 34);
    assert.equal(globe3d.labelPixelHeightForViewport(1200), 34, 'на большом холсте не крупнее прежнего');
    assert.equal(globe3d.labelPixelHeightForViewport(269), 15, 'телефон');
    assert.equal(globe3d.labelPixelHeightForViewport(400), 23);
    assert.equal(globe3d.labelPixelHeightForViewport(100), 15, 'не мельче 15 px');
    assert.equal(globe3d.labelPixelHeightForViewport(0), 34);
});

test('a label sticking out of the canvas has no place there', () => {
    const viewport = { x: 378, y: 269 };

    assert.equal(globe3d.labelRectFitsViewport({ x: 10, y: 10, width: 100, height: 15 }, viewport), true);
    assert.equal(globe3d.labelRectFitsViewport({ x: -40, y: 10, width: 100, height: 15 }, viewport), false);
    assert.equal(globe3d.labelRectFitsViewport({ x: 300, y: 10, width: 100, height: 15 }, viewport), false);
    assert.equal(globe3d.labelRectFitsViewport(null, viewport), false);
});

test('pinching fingers twice apart brings the surface twice closer, not the centre', () => {
    // OrbitControls делит расстояние на k^zoomSpeed; маленькими шагами это (d − 1) / k.
    let distance = 2.5;
    const steps = 100;
    for (let i = 0; i < steps; i++) {
        distance /= Math.pow(2 ** (1 / steps), globe3d.touchZoomSpeed(distance));
    }

    assert.ok(Math.abs((distance - 1) - 0.75) < 0.01, `поверхность: 1.5 → ${distance - 1}`);
    assert.ok(globe3d.touchZoomSpeed(1.05) < globe3d.touchZoomSpeed(2.5), 'у поверхности щипок мягче');
});

function createGlobe() {
    const globe = Object.create(globe3d.CommunityGlobe.prototype);
    globe.renderer = { domElement: { id: 'canvas' } };
    globe.state = { isInitialized: true };
    globe.touchGesture = null;
    globe.pageScrollInertia = null;
    globe.scrolled = 0;
    globe.scrollPageBy = dy => { globe.scrolled += dy; };
    globe.inertia = [];
    globe.startPageScrollInertia = velocity => globe.inertia.push(velocity);
    return globe;
}

const pointer = (type, x, y, timeStamp, extra = {}) => ({
    type, pointerType: 'touch', pointerId: 1, clientX: x, clientY: y, timeStamp,
    target: { id: 'canvas' },
    stopped: false,
    stopImmediatePropagation() { this.stopped = true; },
    ...extra
});

test('a flick moves the page with the finger and lets it coast, the globe never sees it', () => {
    const globe = createGlobe();
    globe.renderer.domElement = pointer('x', 0, 0, 0).target;
    const down = pointer('pointerdown', 100, 300, 1000, { target: globe.renderer.domElement });
    globe.handleTouchPointerDown(down);

    const moves = [pointer('pointermove', 100, 270, 1016), pointer('pointermove', 100, 230, 1032)];
    moves.forEach(event => globe.handleTouchPointerMove(event));

    assert.ok(moves.every(event => event.stopped), 'OrbitControls движения не получает');
    assert.equal(globe.scrolled, 70, 'палец ушёл вверх на 70 px — страница на 70 px вниз');
    globe.handleTouchPointerEnd(pointer('pointerup', 100, 230, 1040));
    assert.equal(globe.inertia.length, 1);
    assert.ok(globe.inertia[0] > 1, `инерция вниз: ${globe.inertia[0]} px/мс`);
    assert.equal(globe.touchGesture, null);
});

test('a slow drag stays with the globe', () => {
    const globe = createGlobe();
    globe.renderer.domElement = pointer('x', 0, 0, 0).target;
    globe.handleTouchPointerDown(pointer('pointerdown', 100, 300, 1000, { target: globe.renderer.domElement }));

    const move = pointer('pointermove', 100, 280, 1300);
    globe.handleTouchPointerMove(move);
    globe.handleTouchPointerEnd(pointer('pointerup', 100, 280, 1320));

    assert.equal(move.stopped, false);
    assert.equal(globe.scrolled, 0);
    assert.deepEqual(globe.inertia, []);
});

test('mouse and pen are left to OrbitControls', () => {
    const globe = createGlobe();
    globe.handleTouchPointerDown(pointer('pointerdown', 100, 300, 1000, { pointerType: 'mouse', target: globe.renderer.domElement }));

    assert.equal(globe.touchGesture, null);
});
