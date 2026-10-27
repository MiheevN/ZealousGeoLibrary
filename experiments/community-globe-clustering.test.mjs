import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-camera.test.mjs: соседние модули
// встраиваются, Three.js подставляется, наружу выходят функции группировки и класс.
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
        'export { CommunityGlobe, globeClusterAngle, clusterGlobePoints, clusterMarkerParticipant };'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const globe3d = await loadGlobeModule();
const DEG = Math.PI / 180;

const point = (id, latitude, longitude, markerColor) => ({ id, name: id, latitude, longitude, markerColor });

test('the grouping angle covers the requested screen distance and grows with camera distance', () => {
    // Камера в 3 радиусах: до поверхности 2, видимая высота 2·2·tan(37.5°) ≈ 3.07 радиуса.
    const angle = globe3d.globeClusterAngle(3, 75, 600, 40);
    assert.ok(Math.abs(angle - (40 / 600) * 4 * Math.tan(37.5 * DEG)) < 1e-12);
    assert.ok(globe3d.globeClusterAngle(4, 75, 600, 40) > angle, 'дальше — крупнее порог');
    assert.ok(Math.abs(globe3d.globeClusterAngle(3, 75, 600, 80) - 2 * angle) < 1e-12, 'порог пропорционален радиусу');
    assert.equal(globe3d.globeClusterAngle(3, 75, 600, 0), 0, 'радиус 0 — без групп');
    assert.equal(globe3d.globeClusterAngle(0.9, 75, 600, 40), 0, 'камера внутри шара — без групп');
});

test('points closer than the angle merge, distant ones stay apart', () => {
    const points = [point('berlin', 52.52, 13.40), point('potsdam', 52.39, 13.06), point('tokyo', 35.68, 139.69)];

    const groups = globe3d.clusterGlobePoints(points, 1 * DEG);

    assert.deepEqual(groups.map(g => g.members), [[0, 1], [2]]);
    // Центр группы — между её точками, одиночная точка остаётся на месте.
    assert.ok(groups[0].latitude < 52.52 && groups[0].latitude > 52.39);
    assert.ok(groups[0].longitude < 13.40 && groups[0].longitude > 13.06);
    assert.equal(groups[1].latitude, 35.68);
    assert.equal(groups[1].longitude, 139.69);
});

test('angle 0 keeps every point apart, even at the same place', () => {
    const points = [point('a', 10, 10), point('b', 10, 10)];

    assert.deepEqual(globe3d.clusterGlobePoints(points, 0).map(g => g.members), [[0], [1]]);
    assert.deepEqual(globe3d.clusterGlobePoints(points, 0.001 * DEG).map(g => g.members), [[0, 1]]);
});

test('points across the date line merge like any other neighbours', () => {
    const points = [point('west', 0, 179.9), point('east', 0, -179.9)];

    const [group] = globe3d.clusterGlobePoints(points, 1 * DEG);

    assert.deepEqual(group.members, [0, 1]);
    assert.ok(Math.abs(Math.abs(group.longitude) - 180) < 1e-6, 'центр — на линии перемены дат');
});

test('the group marker shows the count and the most common colour', () => {
    const points = [point('a', 1, 1, '#ff0000'), point('b', 1, 1, '#00ff00'), point('c', 1, 1, '#00ff00')];

    const marker = globe3d.clusterMarkerParticipant({ latitude: 1, longitude: 1, members: [0, 1, 2] }, points);

    assert.equal(marker.name, '3');
    assert.equal(marker.markerColor, '#00ff00');
    assert.equal(marker.isCluster, true);
    assert.equal(marker.id, 'cluster:a');
    assert.deepEqual(marker.members.map(m => m.id), ['a', 'b', 'c']);
    // Без своих цветов маркер группы красит глобус по своим настройкам.
    const plain = globe3d.clusterMarkerParticipant({ latitude: 1, longitude: 1, members: [0, 1] }, [point('x', 1, 1), point('y', 1, 1)]);
    assert.equal(plain.markerColor, undefined);
});

// Глобус без WebGL: маркеры — простые объекты, камера — точка на расстоянии от центра.
function createGlobe(cameraDistance, options = {}) {
    const globe = Object.create(globe3d.CommunityGlobe.prototype);
    globe.options = { minZoom: 1.03, maxZoom: 4, cameraSurfaceClearance: 0.02, earthRadius: 1, clusterPoints: true, clusterRadius: 40, ...options };
    globe.camera = { fov: 75, position: { length: () => cameraDistance } };
    globe.container = { clientWidth: 800, clientHeight: 600 };
    globe.state = { isInitialized: true, participantCount: 0 };
    globe.pointMetadata = new Map();
    globe.earthGroup = { children: [], add() {}, remove() {} };
    globe.participantMarkers = [];
    globe.participantPoints = [];
    globe.participantMarkerHitTargets = [];
    globe.participantMarkerOverlayIndicators = [];
    globe.participantLabels = [];
    globe.callbacks = {};
    globe.centered = [];
    globe.createParticipantMarker = participant => ({ userData: { participant } });
    globe.disposeObject3D = () => {};
    globe.updateParticipantMarkerTransforms = () => {};
    globe.updateParticipantLabelBillboards = () => {};
    globe.centerOn = (latitude, longitude, zoom) => globe.centered.push({ latitude, longitude, zoom });
    return globe;
}

const shown = globe => globe.participantMarkers.map(marker => marker.userData.participant.name);
const EUROPE = [point('berlin', 52.52, 13.40), point('potsdam', 52.39, 13.06), point('tokyo', 35.68, 139.69)];

test('far away the globe shows a group, close up the points themselves', () => {
    const globe = createGlobe(3);
    globe.sourceParticipants = EUROPE;
    globe.renderParticipantMarkers();
    assert.deepEqual(shown(globe), ['2', 'tokyo']);

    globe.camera.position.length = () => 1.05;
    globe.updateParticipantClusters();
    assert.deepEqual(shown(globe), ['berlin', 'potsdam', 'tokyo'], 'вплотную к поверхности группы распались');
});

test('turning grouping off shows every point', () => {
    const globe = createGlobe(3, { clusterPoints: false });
    globe.sourceParticipants = EUROPE;

    globe.renderParticipantMarkers();

    assert.deepEqual(shown(globe), ['berlin', 'potsdam', 'tokyo']);
});

test('the same zoom does not rebuild markers on every frame', () => {
    const globe = createGlobe(3);
    globe.sourceParticipants = EUROPE;
    globe.renderParticipantMarkers();
    const markers = globe.participantMarkers;

    globe.camera.position.length = () => 3.05;
    globe.updateParticipantClusters();

    assert.equal(globe.participantMarkers, markers);
});

test('clicking a group flies closer to it instead of opening a point', () => {
    const globe = createGlobe(3);
    const clicked = [];
    globe.callbacks.onParticipantClick = participant => clicked.push(participant);
    globe.sourceParticipants = EUROPE;
    globe.renderParticipantMarkers();
    globe.updateMousePosition = () => {};
    globe.getIntersectedParticipantMarker = () => globe.participantMarkers[0];

    globe.onMouseClick({});

    assert.deepEqual(clicked, [], 'группа — не точка приложения');
    const [target] = globe.centered;
    assert.ok(target.zoom < 3 && target.zoom >= 1.03, `камера подлетает: ${target.zoom}`);
    assert.ok(Math.abs(target.latitude - 52.45) < 0.1);
});

test('counts and metadata stay per point, not per marker', () => {
    const globe = createGlobe(3);
    globe.scene = { children: [] };

    assert.equal(globe.addParticipants([...EUROPE, point('bad', Number.NaN, 0)]), true);

    assert.equal(globe.state.participantCount, 3);
    assert.equal(globe.pointMetadata.size, 3);
    assert.equal(globe.participantMarkers.length, 2);
});
