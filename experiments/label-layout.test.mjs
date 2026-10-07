import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { layoutLabels, labelTextRect, rectsOverlap } from '../wwwroot/js/label-layout.js';

// --- Чистая раскладка -------------------------------------------------------------

const rect = (x, y, width = 100, height = 20) => ({ x, y, width, height });

test('labels that do not touch keep their preferred place', () => {
    const layout = layoutLabels([
        { id: 'a', priority: 0, placements: [rect(0, 0), rect(-110, 0)] },
        { id: 'b', priority: 0, placements: [rect(0, 50), rect(-110, 50)] }
    ]);

    assert.deepEqual([...layout], [['a', 0], ['b', 0]]);
});

test('an overlapping label moves to its other side, then hides when both sides are taken', () => {
    const layout = layoutLabels([
        { id: 'first', priority: 0, placements: [rect(0, 0)] },
        { id: 'moves', priority: 0, placements: [rect(50, 5), rect(-120, 5)] },
        { id: 'hidden', priority: 0, placements: [rect(10, 10), rect(-100, 10)] }
    ]);

    assert.equal(layout.get('first'), 0);
    assert.equal(layout.get('moves'), 1);
    assert.equal(layout.get('hidden'), -1);
});

test('priority beats list order, and list order breaks ties', () => {
    const layout = layoutLabels([
        { id: 'early-low', priority: 0, placements: [rect(0, 0)] },
        { id: 'late-high', priority: 2, placements: [rect(0, 0)] },
        { id: 'early-tie', priority: 1, placements: [rect(500, 0)] },
        { id: 'late-tie', priority: 1, placements: [rect(500, 0)] }
    ]);

    assert.equal(layout.get('late-high'), 0);
    assert.equal(layout.get('early-low'), -1);
    assert.equal(layout.get('early-tie'), 0);
    assert.equal(layout.get('late-tie'), -1);
});

test('padding keeps a gap between labels; off-screen places are skipped', () => {
    const touching = [
        { id: 'a', priority: 0, placements: [rect(0, 0)] },
        { id: 'b', priority: 0, placements: [rect(103, 0)] }
    ];

    assert.equal(layoutLabels(touching, 0).get('b'), 0);
    assert.equal(layoutLabels(touching, 4).get('b'), -1);
    assert.equal(layoutLabels([{ id: 'c', priority: 0, placements: [null, rect(0, 0)] }]).get('c'), 1);
    assert.equal(layoutLabels([{ id: 'd', priority: 0, placements: [rect(0, 0, 0, 20)] }]).get('d'), -1);
});

test('a visible label keeps its place from the previous frame', () => {
    const layout = layoutLabels([
        { id: 'stays-below', priority: 0, placements: [rect(0, 0), rect(0, 40)], previous: 1 },
        { id: 'new', priority: 0, placements: [rect(0, 40)] }
    ]);

    // Место над маркером свободно, но подпись не прыгает туда из-под маркера.
    assert.equal(layout.get('stays-below'), 1);
    assert.equal(layout.get('new'), -1);
});

test('an incumbent wins ties against an earlier newcomer, but not against higher priority', () => {
    const tie = layoutLabels([
        { id: 'newcomer', priority: 2, placements: [rect(0, 0)] },
        { id: 'incumbent', priority: 2, placements: [rect(0, 0)], previous: 0 }
    ]);
    const stronger = layoutLabels([
        { id: 'hovered', priority: 5, placements: [rect(0, 0)] },
        { id: 'incumbent', priority: 2, placements: [rect(0, 0)], previous: 0 }
    ]);

    assert.equal(tie.get('incumbent'), 0);
    assert.equal(tie.get('newcomer'), -1);
    assert.equal(stronger.get('hovered'), 0);
    assert.equal(stronger.get('incumbent'), -1);
});

test('a previously hidden or out-of-range previous place gets no bonus', () => {
    const layout = layoutLabels([
        { id: 'first', priority: 0, placements: [rect(0, 0)], previous: -1 },
        { id: 'second', priority: 0, placements: [rect(0, 0)], previous: 7 }
    ]);

    assert.equal(layout.get('first'), 0);
    assert.equal(layout.get('second'), -1);
});

test('label text rectangle excludes the transparent canvas padding', () => {
    const text = labelTextRect(100, 50, 40, 4, 0.1, 0.2);

    assert.deepEqual(text, { x: 36, y: 38, width: 128, height: 24 });
    assert.ok(rectsOverlap(text, rect(160, 40, 10, 10)));
    assert.ok(!rectsOverlap(text, rect(170, 40, 10, 10)));
});

// --- Глобус с настоящей камерой three.js -----------------------------------------

async function loadCommunityGlobeClass() {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const layoutSource = await readFile(new URL('../wwwroot/js/label-layout.js', import.meta.url), 'utf8');
    const scaleSource = await readFile(new URL('../wwwroot/js/label-scale.js', import.meta.url), 'utf8');
    const testSource = source
        .replace(
            /import \{ layoutLabels, labelTextRect \} from '\.\/label-layout\.js';\n/,
            () => `${layoutSource.replace(/^export /gm, '')}\n`
        )
        .replace(
            /import \{ DEFAULT_LABEL_PIXEL_HEIGHT, calculateLabelScaleForCamera \} from '\.\/label-scale\.js';\n/,
            () => `${scaleSource.replace(/^export /gm, '')}\n`
        )
        .replace(
            /initializeDependencies\(\)\.then\(success => \{[\s\S]*?\n\}\);\n\n\/\/ Глобальный реестр/,
            'dependenciesLoaded = true;\n\n// Глобальный реестр'
        );
    const threeModuleUrl = new URL('../wwwroot/js/libs/three.module.js', import.meta.url).href;
    const moduleSource = [
        `import * as TestThree from '${threeModuleUrl}';`,
        testSource.replace('let THREE, OrbitControls;', 'let THREE = TestThree, OrbitControls;'),
        'export { CommunityGlobe, TestThree };'
    ].join('\n');
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    return import(moduleUrl);
}

const { CommunityGlobe, TestThree: THREE } = await loadCommunityGlobeClass();
const VIEWPORT = { width: 900, height: 640 };

// Камера как у глобуса по умолчанию: угол обзора 75°, расстояние 2.5.
function createGlobe(options = {}, lookAt = { lat: 48, lng: 8 }) {
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = {
        earthRadius: 1,
        minZoom: 1.03,
        maxZoom: 4,
        participantPointOffset: 0.005,
        participantMarkerLabelScreenGapPixels: 10,
        participantLabelHiddenOpacity: 0.12,
        participantLabelHorizonFade: 0.25,
        participantLabelCollisionAvoidance: true,
        participantLabelCollisionPadding: 4,
        ...options
    };
    globe.camera = new THREE.PerspectiveCamera(75, VIEWPORT.width / VIEWPORT.height, 0.02, 1000);
    globe.renderer = { getSize: (target) => target.set(VIEWPORT.width, VIEWPORT.height) };
    globe.participantMarkers = [];
    globe.hoveredParticipantMarker = null;
    globe.labelTargetPixelHeight = 34;
    lookFrom(globe, lookAt.lat, lookAt.lng);
    return globe;
}

function lookFrom(globe, lat, lng) {
    const eye = globe.latLngToVector3(lat, lng, 2.5);
    globe.camera.position.set(eye.x, eye.y, eye.z);
    globe.camera.lookAt(0, 0, 0);
    globe.camera.updateMatrixWorld();
}

// Маркер как в createParticipantMarker, без мешей; подпись — холст 24px Arial с полями 14/8.
function addMarkerAt(globe, name, position) {
    const normal = position.clone().normalize();
    const marker = new THREE.Group();
    marker.position.copy(position);
    marker.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), normal);
    const visual = new THREE.Group();
    marker.add(visual);
    const textWidth = 13.5 * name.length;
    const label = new THREE.Mesh(new THREE.PlaneGeometry(1, 1));
    label.userData = {
        labelAspectRatio: (textWidth + 28) / 40,
        targetPixelHeight: 34,
        textInsetX: 14 / (textWidth + 28),
        textInsetY: 8 / 40
    };
    marker.add(label);
    marker.userData = { participant: { name }, normal, dimensions: globe.getParticipantMarkerDimensions(), visual, label };
    marker.updateWorldMatrix(true, true);
    globe.participantMarkers.push(marker);
    return marker;
}

function addMarker(globe, name, lat, lng) {
    const p = globe.latLngToVector3(lat, lng, 1 + globe.options.participantPointOffset);
    return addMarkerAt(globe, name, new THREE.Vector3(p.x, p.y, p.z));
}

function frame(globe) {
    globe.updateParticipantMarkerTransforms();
    globe.updateParticipantLabelLayout();
}

const viewport = () => new THREE.Vector2(VIEWPORT.width, VIEWPORT.height);
const rectAt = (globe, marker, index) =>
    globe.getParticipantLabelScreenRect(marker, marker.userData.label, marker.userData.labelPlacements[index], viewport());

function visibleRects(globe) {
    return globe.participantMarkers
        .filter(marker => marker.userData.labelLayoutVisible)
        .map(marker => ({ name: marker.userData.participant.name, rect: rectAt(globe, marker, marker.userData.labelPlacementIndex ?? 0) }));
}

function overlappingPairs(globe) {
    const rects = visibleRects(globe);
    const pairs = [];
    for (let i = 0; i < rects.length; i += 1) {
        for (let j = i + 1; j < rects.length; j += 1) {
            if (rects[i].rect && rects[j].rect && rectsOverlap(rects[i].rect, rects[j].rect)) {
                pairs.push(`${rects[i].name} × ${rects[j].name}`);
            }
        }
    }
    return pairs;
}

test('London and Paris no longer write over each other', () => {
    const globe = createGlobe();
    const london = addMarker(globe, 'London', 51.5074, -0.1278);
    const paris = addMarker(globe, 'Paris', 48.8566, 2.3522);
    const cairo = addMarker(globe, 'Cairo', 30.0444, 31.2357);

    frame(globe);

    // Без раскладки обе подписи стоят над своими маркерами и перекрываются («LParisn»).
    assert.ok(rectsOverlap(rectAt(globe, london, 0), rectAt(globe, paris, 0)), 'scenario must start with overlapping labels');

    assert.equal(london.userData.labelPlacementIndex, 0);
    assert.equal(paris.userData.labelLayoutVisible, true, 'Paris finds another place instead of disappearing');
    assert.ok(paris.userData.labelPlacementIndex > 0);
    assert.ok(paris.userData.label.position.equals(paris.userData.labelPlacements[paris.userData.labelPlacementIndex]));
    assert.equal(cairo.userData.labelLayoutVisible, true);
    assert.deepEqual(overlappingPairs(globe), []);
});

test('a label on the far side never covers a label on the visible side', () => {
    const globe = createGlobe();
    const raw = globe.latLngToVector3(51.5, -0.13, 1.005);
    const front = new THREE.Vector3(raw.x, raw.y, raw.z);
    // Луч камеры через Лондон второй раз пересекает Землю на обратной стороне.
    const direction = front.clone().sub(globe.camera.position).normalize();
    const b = globe.camera.position.dot(direction);
    const c = globe.camera.position.lengthSq() - 1.005 * 1.005;
    const behind = globe.camera.position.clone().addScaledVector(direction, -b + Math.sqrt(b * b - c));

    const hidden = addMarkerAt(globe, 'Antipode', behind);   // раньше в списке, но за горизонтом
    const london = addMarkerAt(globe, 'London', front);
    frame(globe);

    // Подписи совпадают на экране: без приоритета лицевой стороны место занял бы «Antipode».
    assert.ok(rectsOverlap(rectAt(globe, hidden, 0), rectAt(globe, london, 0)), 'scenario must start with overlapping labels');
    assert.equal(london.userData.labelLayoutVisible, true);
    assert.equal(london.userData.labelPlacementIndex, 0);
    assert.deepEqual(overlappingPairs(globe), []);
});

test('in a dense cluster some labels hide, and the hovered marker always gets its label', () => {
    const globe = createGlobe();
    const cluster = [];
    for (let i = 0; i < 9; i += 1) {
        cluster.push(addMarker(globe, `Office ${i}`, 50 + (i % 3) * 0.15, 8 + Math.floor(i / 3) * 0.2));
    }

    frame(globe);
    const hidden = cluster.filter(marker => !marker.userData.labelLayoutVisible);
    assert.ok(hidden.length > 0, 'nine labels cannot all fit around one spot');
    assert.deepEqual(overlappingPairs(globe), []);

    globe.hoveredParticipantMarker = hidden.at(-1);
    frame(globe);
    assert.equal(hidden.at(-1).userData.labelLayoutVisible, true);
    assert.deepEqual(overlappingPairs(globe), []);
});

test('demo data sets never show overlapping labels while the globe turns', () => {
    const places = [
        ['London', 51.5074, -0.1278], ['Paris', 48.8566, 2.3522], ['Tokyo', 35.6762, 139.6503],
        ['Washington', 38.9072, -77.0369], ['Brasília', -15.7939, -47.8828], ['Cairo', 30.0444, 31.2357],
        ['Canberra', -35.2809, 149.13], ['Москва', 55.7558, 37.6173], ['Санкт-Петербург', 59.9343, 30.3351],
        ['Казань', 55.7961, 49.1064], ['Краснодар', 45.0355, 38.9753], ['Екатеринбург', 56.8389, 60.6057],
        ['Новосибирск', 55.0084, 82.9357], ['Владивосток', 43.1198, 131.8869], ['Berlin', 52.52, 13.405],
        ['Tel Aviv', 32.0853, 34.7818], ['San Francisco', 37.7749, -122.4194], ['Seattle', 47.6062, -122.3321]
    ];
    const globe = createGlobe();
    places.forEach(([name, lat, lng]) => addMarker(globe, name, lat, lng));

    for (let lng = -180; lng < 180; lng += 30) {
        for (const lat of [-30, 20, 55]) {
            lookFrom(globe, lat, lng);
            frame(globe);
            assert.deepEqual(overlappingPairs(globe), [], `camera over ${lat}, ${lng}`);
        }
    }
});

test('labels stay put while the globe turns instead of flickering', () => {
    const cities = [
        ['Москва', 55.7558, 37.6173], ['Санкт-Петербург', 59.9343, 30.3351], ['Новосибирск', 55.0084, 82.9357],
        ['Екатеринбург', 56.8389, 60.6057], ['Казань', 55.7961, 49.1064], ['Краснодар', 45.0355, 38.9753],
        ['Владивосток', 43.1198, 131.8869]
    ];
    const globe = createGlobe();
    cities.forEach(([name, lat, lng]) => addMarker(globe, name, lat, lng));

    // Камера проходит над Россией с запада на восток на высоте экватора, как при автовращении.
    const changes = new Map();
    const last = new Map();
    for (let step = 0; step <= 360; step += 1) {
        lookFrom(globe, 0, -20 + step * 0.5);
        frame(globe);
        globe.participantMarkers.forEach(marker => {
            const name = marker.userData.participant.name;
            const state = marker.userData.labelLayoutVisible ? marker.userData.labelPlacementIndex : -1;
            if (last.has(name) && last.get(name) !== state) {
                changes.set(name, (changes.get(name) ?? 0) + 1);
            }
            last.set(name, state);
        });
        assert.deepEqual(overlappingPairs(globe), [], `camera over 0, ${-20 + step * 0.5}`);
    }

    // Подпись меняет решение, когда входит в зону у горизонта или уходит из неё, а не на
    // каждом шаге. Без памяти о прошлом кадре здесь было 81 переключение, до 19 у одной подписи.
    const total = [...changes.values()].reduce((sum, count) => sum + count, 0);
    assert.ok(total <= 50, `labels changed place ${total} times: ${JSON.stringify(Object.fromEntries(changes))}`);
    assert.ok([...changes.values()].every(count => count <= 10), JSON.stringify(Object.fromEntries(changes)));
});

test('collision avoidance can be turned off', () => {
    const globe = createGlobe({ participantLabelCollisionAvoidance: false });
    addMarker(globe, 'London', 51.5074, -0.1278);
    addMarker(globe, 'Paris', 48.8566, 2.3522);

    frame(globe);

    assert.ok(globe.participantMarkers.every(marker => marker.userData.labelLayoutVisible === true));
});

test('a label without room fades out over a few frames instead of popping', () => {
    const globe = createGlobe();
    globe.lastFrameDeltaSeconds = 1 / 60;
    const marker = addMarker(globe, 'Paris', 48.8566, 2.3522);
    const label = marker.userData.label;

    marker.userData.labelLayoutVisible = true;
    assert.equal(globe.updateParticipantLabelLayoutAlpha(label), 1);

    marker.userData.labelLayoutVisible = false;
    const steps = [];
    for (let i = 0; i < 20 && label.userData.layoutAlpha > 0; i += 1) {
        steps.push(globe.updateParticipantLabelLayoutAlpha(label));
    }

    assert.ok(steps.length > 3, 'fade takes several frames');
    assert.ok(steps.every((value, i) => i === 0 || value < steps[i - 1]), 'opacity only decreases');
    assert.equal(steps.at(-1), 0);
});

test('fade time does not depend on frame rate, and hiding is faster than showing', () => {
    const globe = createGlobe();
    const marker = addMarker(globe, 'Paris', 48.8566, 2.3522);
    const label = marker.userData.label;
    const framesTo = (visible, deltaSeconds) => {
        globe.lastFrameDeltaSeconds = deltaSeconds;
        marker.userData.labelLayoutVisible = visible;
        let frames = 0;
        while (label.userData.layoutAlpha !== (visible ? 1 : 0) && frames < 1000) {
            globe.updateParticipantLabelLayoutAlpha(label);
            frames += 1;
        }
        return frames;
    };

    label.userData.layoutAlpha = 1;
    const hideAt60 = framesTo(false, 1 / 60);
    const showAt60 = framesTo(true, 1 / 60);
    label.userData.layoutAlpha = 1;
    const hideAt7 = framesTo(false, 1 / 7);

    // 0.12 с на исчезновение и 0.2 с на появление при любой частоте кадров.
    assert.ok(hideAt60 >= 7 && hideAt60 <= 8, `hide took ${hideAt60} frames at 60 fps`);
    assert.ok(showAt60 >= 12 && showAt60 <= 13, `show took ${showAt60} frames at 60 fps`);
    assert.equal(hideAt7, 1, 'at 7 fps the label is gone by the next frame');
});

test('globe wires layout between marker transforms and label opacity, and exposes the options', async () => {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const sw = await readFile(new URL('../wwwroot/sw.js', import.meta.url), 'utf8');

    assert.match(source, /this\.updateParticipantMarkerTransforms\(\);\s*this\.updateParticipantLabelLayout\(\);\s*this\.updateParticipantLabelBillboards\(\);/);
    assert.match(source, /participantLabelCollisionAvoidance: true/);
    assert.match(source, /'participantLabelCollisionAvoidance',\s*'participantLabelCollisionPadding'/);
    assert.match(sw, /js\/label-layout\.js/, 'service worker caches the new module for offline use');
});
