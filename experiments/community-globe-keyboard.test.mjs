import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-clustering.test.mjs, наружу выходят
// функции клавиатуры, класс и Three.js.
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
        'export { CommunityGlobe, keyboardRotationStep, rotateCameraPosition, orderVisibleGlobeMarkers, globeMarkerAnnouncement, globePluralRules };',
        'export const Three = TestThree;'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const globe3d = await loadGlobeModule();
const { Three } = globe3d;
const DEG = Math.PI / 180;

// Долгота и широта точки на глобусе, на которую смотрит камера (как в latLngToVector3).
function viewCenter(position) {
    const distance = Math.hypot(position.x, position.y, position.z);
    return {
        latitude: Math.asin(position.y / distance) / DEG,
        longitude: -Math.atan2(position.z, position.x) / DEG
    };
}

test('an arrow turns the globe less when the camera is close', () => {
    const far = globe3d.keyboardRotationStep(4);
    const near = globe3d.keyboardRotationStep(1.2);

    assert.ok(near < far, `${near} < ${far}`);
    assert.ok(far <= 20 * DEG + 1e-12, 'не больше 20°');
    assert.ok(globe3d.keyboardRotationStep(1) >= 0.5 * DEG - 1e-12, 'у поверхности — не меньше 0.5°');
});

test('right and up show what lies east and north, at the same distance', () => {
    const start = { x: 0, y: 0, z: 2.5 };
    const east = globe3d.rotateCameraPosition(start, 10 * DEG, 0);
    const north = globe3d.rotateCameraPosition(start, 0, 10 * DEG);

    assert.ok(Math.abs(Math.hypot(east.x, east.y, east.z) - 2.5) < 1e-12, 'расстояние не меняется');
    assert.ok(Math.abs(viewCenter(east).longitude - viewCenter(start).longitude - 10) < 1e-9, 'вправо — на восток');
    assert.ok(Math.abs(viewCenter(north).latitude - 10) < 1e-9, 'вверх — на север');

    const back = globe3d.rotateCameraPosition(east, -10 * DEG, 0);
    assert.ok(Math.abs(back.x - start.x) < 1e-12 && Math.abs(back.z - start.z) < 1e-12);
});

test('the camera never goes over a pole', () => {
    let position = { x: 0, y: 0, z: 2.5 };
    for (let i = 0; i < 20; i++) {
        position = globe3d.rotateCameraPosition(position, 0, 20 * DEG);
    }

    assert.ok(Math.abs(viewCenter(position).latitude - 80) < 1e-9);
});

test('markers on the far side or off screen are skipped, the rest go left to right', () => {
    const camera = { x: 0, y: 0, z: 3 };
    const entry = (key, world, screen) => ({ key, world, screen });
    const order = globe3d.orderVisibleGlobeMarkers([
        entry('right', { x: 0.3, y: 0, z: 0.95 }, { x: 0.4, y: 0 }),
        entry('back', { x: 0, y: 0, z: -1 }, { x: 0, y: 0 }),
        entry('left-low', { x: -0.3, y: -0.2, z: 0.93 }, { x: -0.4, y: -0.3 }),
        entry('left-high', { x: -0.3, y: 0.2, z: 0.93 }, { x: -0.4, y: 0.3 }),
        entry('outside', { x: 0, y: 0.1, z: 0.99 }, { x: 1.5, y: 0 })
    ], camera);

    assert.deepEqual(order.map(item => item.key), ['left-high', 'left-low', 'right']);
});

test('the screen reader hears the point, or the group size with its first names', () => {
    const texts = { pointFallback: 'Точка', clusterOne: '{0} точка', clusterFew: '{0} точки', clusterMany: '{0} точек', clusterOther: '{0} точки' };
    const plural = globe3d.globePluralRules('ru');
    const members = ['Берлин', 'Потсдам', 'Лейпциг', 'Дрезден', 'Йена'].map(name => ({ name }));

    assert.equal(globe3d.globeMarkerAnnouncement({ name: 'Берлин', category: 'Офис' }, texts, plural), 'Берлин, Офис');
    assert.equal(globe3d.globeMarkerAnnouncement({}, texts, plural), 'Точка');
    assert.equal(globe3d.globeMarkerAnnouncement({ isCluster: true, members }, texts, plural), '5 точек: Берлин, Потсдам, Лейпциг');
    assert.equal(globe3d.globeMarkerAnnouncement({ isCluster: true, members: members.slice(0, 2) }, texts, plural), '2 точки: Берлин, Потсдам');
});

// Глобус без WebGL: камера и группа Земли из Three.js, маркеры — простые объекты.
function createGlobe(participants) {
    const globe = Object.create(globe3d.CommunityGlobe.prototype);
    globe.options = { minZoom: 1.03, maxZoom: 4, cameraSurfaceClearance: 0.02, earthRadius: 1, participantClickZoom: 1.5 };
    globe.camera = new Three.PerspectiveCamera(75, 4 / 3, 0.02, 1000);
    globe.camera.position.set(0, 0, 2.5);
    globe.camera.lookAt(0, 0, 0);
    globe.camera.updateMatrixWorld();
    globe.earthGroup = new Three.Group();
    globe.state = { isInitialized: true };
    globe.texts = { pointFallback: 'Point', clusterOne: '{0} point', clusterOther: '{0} points' };
    globe.plural = globe3d.globePluralRules('en');
    globe.announcer = { textContent: '' };
    globe.keyboardSelectionKey = null;
    globe.callbacks = {};
    globe.centered = [];
    globe.centerOn = (latitude, longitude, zoom) => globe.centered.push({ latitude, longitude, zoom });
    globe.participantMarkers = participants.map(participant => ({ userData: { participant } }));
    return globe;
}

const key = (name, extra = {}) => ({ key: name, code: '', ...extra });
// Камера смотрит на долготу −90° (ось +z): Атлантика и Америка.
const AMERICAS = [
    { id: 'quito', name: 'Quito', latitude: -0.18, longitude: -78.47 },
    { id: 'lima', name: 'Lima', latitude: -12.05, longitude: -77.04 },
    { id: 'mexico', name: 'Mexico City', latitude: 19.43, longitude: -99.13 },
    { id: 'tokyo', name: 'Tokyo', latitude: 35.68, longitude: 139.69 }
];

test('N and P walk through the markers the camera sees and announce them', () => {
    const globe = createGlobe(AMERICAS);

    assert.equal(globe.handleKeyDown(key('n', { code: 'KeyN' })), true);
    assert.equal(globe.keyboardSelectionKey, 'mexico', 'первый слева');
    assert.equal(globe.announcer.textContent, 'Mexico City');
    assert.equal(globe.hoveredParticipantMarker, globe.participantMarkers[2], 'маркер выделен');

    globe.handleKeyDown(key('n', { code: 'KeyN' }));
    assert.equal(globe.keyboardSelectionKey, 'quito', 'Кито западнее Лимы');
    globe.handleKeyDown(key('n', { code: 'KeyN' }));
    assert.equal(globe.keyboardSelectionKey, 'lima');
    globe.handleKeyDown(key('т', { code: 'KeyN' }));
    assert.equal(globe.keyboardSelectionKey, 'mexico', 'по кругу; Токио за горизонтом не выбирается');
    globe.handleKeyDown(key('p', { code: 'KeyP' }));
    assert.equal(globe.keyboardSelectionKey, 'lima');

    assert.equal(globe.handleKeyDown(key('Escape')), true);
    assert.equal(globe.keyboardSelectionKey, null);
    assert.equal(globe.announcer.textContent, '');
    assert.equal(globe.handleKeyDown(key('Escape')), false, 'нечего снимать — Esc уходит странице');
});

test('Enter on a selected point flies to it like a click', () => {
    const globe = createGlobe(AMERICAS);
    const clicked = [];
    globe.callbacks.onParticipantClick = participant => clicked.push(participant.id);

    assert.equal(globe.handleKeyDown(key('Enter')), false, 'без выбора Enter не перехватывается');
    assert.equal(globe.handleKeyDown(key(' ')), true, 'пробел не прокручивает страницу');

    globe.handleKeyDown(key('n', { code: 'KeyN' }));
    assert.equal(globe.handleKeyDown(key('Enter')), true);

    assert.deepEqual(clicked, ['mexico']);
    assert.deepEqual(globe.centered, [{ latitude: 19.43, longitude: -99.13, zoom: 1.5 }]);
});

test('arrows rotate and + / − zoom within the camera limits', () => {
    const globe = createGlobe([]);
    const before = viewCenter(globe.camera.position);

    assert.equal(globe.handleKeyDown(key('ArrowRight')), true);
    assert.ok(viewCenter(globe.camera.position).longitude > before.longitude, 'вправо — на восток');
    assert.equal(globe.handleKeyDown(key('ArrowUp')), true);
    assert.ok(viewCenter(globe.camera.position).latitude > 0, 'вверх — на север');

    const distance = globe.camera.position.length();
    globe.handleKeyDown(key('+'));
    assert.ok(globe.camera.position.length() < distance, '+ приближает');
    for (let i = 0; i < 30; i++) {
        globe.handleKeyDown(key('-'));
    }
    assert.ok(Math.abs(globe.camera.position.length() - 4) < 1e-9, '− не дальше maxZoom');

    assert.equal(globe.handleKeyDown(key('+', { ctrlKey: true })), false, 'Ctrl + — масштаб страницы');
    assert.equal(globe.handleKeyDown(key('a')), false);
});

test('a selection survives regrouping only while its marker still exists', () => {
    const globe = createGlobe(AMERICAS);
    globe.handleKeyDown(key('n', { code: 'KeyN' }));

    globe.participantMarkers = AMERICAS.map(participant => ({ userData: { participant } }));
    globe.restoreKeyboardSelection();
    assert.equal(globe.hoveredParticipantMarker, globe.participantMarkers[2], 'тот же id — новый маркер');

    globe.participantMarkers = [{ userData: { participant: { id: 'cluster:quito', isCluster: true, members: [] } } }];
    globe.restoreKeyboardSelection();
    assert.equal(globe.keyboardSelectionKey, null);
    assert.equal(globe.announcer.textContent, '');
});

test('a ring marks the selected marker and hides when it turns away', () => {
    const globe = createGlobe(AMERICAS);
    globe.renderer = { domElement: { offsetLeft: 10, offsetTop: 20, clientWidth: 800, clientHeight: 600 } };
    globe.selectionRing = { hidden: true, style: {} };

    globe.updateKeyboardSelectionRing();
    assert.equal(globe.selectionRing.hidden, true, 'ничего не выбрано');

    globe.handleKeyDown(key('n', { code: 'KeyN' }));
    globe.updateKeyboardSelectionRing();
    assert.equal(globe.selectionRing.hidden, false);
    const left = parseFloat(globe.selectionRing.style.left);
    const top = parseFloat(globe.selectionRing.style.top);
    assert.ok(left > 10 && left < 410, `Мехико левее центра: ${left}`);
    assert.ok(top > 20 && top < 320, `и выше экватора: ${top}`);

    globe.camera.position.set(0, 0, -2.5);
    globe.camera.lookAt(0, 0, 0);
    globe.camera.updateMatrixWorld();
    globe.updateKeyboardSelectionRing();
    assert.equal(globe.selectionRing.hidden, true, 'за горизонтом кольца нет');
});
