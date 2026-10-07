import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

// Тот же приём, что в community-map-2d.test.mjs: скрипт карты рассчитан на
// браузер, поэтому подгружаем его в data: URL с минимальными заглушками DOM и
// экспортируем внутренние функции группировки.
async function loadMapModule() {
    const source = await readText('wwwroot/js/community-map.js');
    const harness = [
        'const documentStub = { createElement: () => ({ classList: {add(){},remove(){}}, setAttribute(){}, addEventListener(){}, appendChild(){}, style: {}, getContext: () => ({}) }), getElementById: () => null };',
        'const windowStub = { devicePixelRatio: 1, addEventListener: () => {}, removeEventListener: () => {}, clearTimeout: () => {}, setTimeout: () => 0 };',
        'if (!("window" in globalThis)) globalThis.window = windowStub;',
        'if (!("document" in globalThis)) globalThis.document = documentStub;'
    ].join('\n');
    const exportFooter = `
export {
    clusterMarkers, clusterBadgeRadius, spiderOffsets, getMarkerGroups, clusterExpansion, expandCluster,
    renderClusterTooltip, pluralizePoints, formatClusterCount, clusterColorShares, normalizeClusterOptions,
    markerColor, drawMarkers, findTargetAt, resolveProjection, getView, setCenter, projectPoint,
    POINT_RADIUS, POINT_HOVER_RADIUS, CLUSTER_GAP, CLUSTER_MAX_RADIUS, DEFAULT_CLUSTER_RADIUS,
    SPIDER_FOOT_SPACING, MAX_ZOOM, ZOOM_STEP, DEFAULT_MARKER_COLOR
};
`;
    const moduleSource = `${harness}\n${source}\n${exportFooter}`;
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const map = await loadMapModule();

// Детерминированный генератор: тесты не зависят от запуска.
function random(seed) {
    let state = seed;
    return () => (state = (state * 16807) % 2147483647) / 2147483647;
}

const separation = (a, b, radius = map.DEFAULT_CLUSTER_RADIUS) =>
    Math.max(radius, map.clusterBadgeRadius(a.count) + map.clusterBadgeRadius(b.count) + map.CLUSTER_GAP);

const membersOf = (groups) => groups.map((group) => group.members.join(',')).sort();

// Холст, который всё принимает и ничего не рисует: для проверки раскладки маркеров.
function canvasContextStub() {
    return new Proxy({}, {
        get: (target, name) => (name in target ? target[name] : () => {}),
        set: (target, name, value) => {
            target[name] = value;
            return true;
        }
    });
}

function createState(points, overrides = {}) {
    const { projection, lat = 20, lng = 0, ...rest } = overrides;
    const state = {
        width: 810,
        height: 544,
        zoom: 1,
        centerX: 0,
        centerY: 0,
        centralMeridian: 0,
        projection: map.resolveProjection(projection),
        ctx: canvasContextStub(),
        points,
        pointsVersion: 1,
        clustering: map.normalizeClusterOptions(undefined),
        groupCache: null,
        spider: null,
        hitTargets: [],
        hoveredKey: null,
        focused: null,
        ...rest
    };
    map.setCenter(state, lat, lng);
    return state;
}

const point = (id, latitude, longitude, extra = {}) => ({ id, latitude, longitude, title: id, color: null, ...extra });

const LONDON = point('London', 51.5074, -0.1278);
const PARIS = point('Paris', 48.8566, 2.3522);
const TOKYO = point('Tokyo', 35.6762, 139.6503);

test('markers closer than the radius merge into one group at their mean position', () => {
    const groups = map.clusterMarkers([{ x: 0, y: 0 }, { x: 10, y: 0 }, { x: 200, y: 0 }]);

    assert.equal(groups.length, 2);
    const pair = groups.find((group) => group.count === 2);
    assert.deepEqual(pair.members, [0, 1]);
    assert.equal(pair.x, 5);
    assert.equal(pair.y, 0);
    assert.deepEqual(groups.find((group) => group.count === 1).members, [2]);
});

test('by default only markers that would touch merge; a larger radius merges more', () => {
    const touching = map.DEFAULT_CLUSTER_RADIUS;
    assert.equal(touching, 2 * map.POINT_RADIUS + map.CLUSTER_GAP);

    assert.equal(map.clusterMarkers([{ x: 0, y: 0 }, { x: touching - 0.1, y: 0 }]).length, 1);
    assert.equal(map.clusterMarkers([{ x: 0, y: 0 }, { x: touching + 0.1, y: 0 }]).length, 2);
    assert.equal(map.clusterMarkers([{ x: 0, y: 0 }, { x: 50, y: 0 }], { radius: 60 }).length, 1);
    // Радиус меньше «касания» маркеры всё равно не даёт наложить.
    assert.equal(map.clusterMarkers([{ x: 0, y: 0 }, { x: 10, y: 0 }], { radius: 0 }).length, 1);
});

test('no two groups end up overlapping, and every marker lands in exactly one group', () => {
    const next = random(7);
    for (const [count, radius] of [[60, 24], [800, 24], [3000, 24], [800, 60]]) {
        const items = Array.from({ length: count }, () => ({ x: next() * 1200, y: next() * 700 }));
        const groups = map.clusterMarkers(items, { radius });

        const seen = groups.flatMap((group) => group.members).sort((a, b) => a - b);
        assert.deepEqual(seen, items.map((_, index) => index), `${count} точек: каждая ровно в одной группе`);
        groups.forEach((group) => assert.equal(group.count, group.members.length));

        for (let i = 0; i < groups.length; i += 1) {
            for (let j = i + 1; j < groups.length; j += 1) {
                const distance = Math.hypot(groups[i].x - groups[j].x, groups[i].y - groups[j].y);
                assert.ok(distance >= separation(groups[i], groups[j], radius) - 1e-9,
                    `${count} точек: группы ${i} и ${j} на расстоянии ${distance.toFixed(1)}`);
            }
        }
    }
});

test('groups do not depend on where the map is panned', () => {
    const next = random(11);
    const items = Array.from({ length: 400 }, () => ({ x: next() * 900, y: next() * 500 }));
    const shifted = items.map(({ x, y }) => ({ x: x + 1234.5, y: y - 987.25 }));

    assert.deepEqual(membersOf(map.clusterMarkers(shifted)), membersOf(map.clusterMarkers(items)));
});

test('on a wrapping map markers merge across the date line', () => {
    const items = [{ x: -499, y: 0 }, { x: 499, y: 0 }];

    assert.equal(map.clusterMarkers(items).length, 2, 'без склейки они на разных краях');
    const [group] = map.clusterMarkers(items, { period: 1000 });
    assert.equal(group.count, 2);
    assert.ok(Math.abs(Math.abs(group.x) - 500) < 1e-9, `центр на шве, получено ${group.x}`);
});

test('badge grows with the number of points and stays bounded', () => {
    assert.equal(map.clusterBadgeRadius(1), map.POINT_RADIUS);
    let previous = map.POINT_RADIUS;
    for (const count of [2, 3, 5, 10, 16]) {
        const radius = map.clusterBadgeRadius(count);
        assert.ok(radius > previous, `${count}: ${radius}`);
        previous = radius;
    }
    for (const count of [20, 50, 100000]) {
        assert.equal(map.clusterBadgeRadius(count), map.CLUSTER_MAX_RADIUS, `${count}`);
    }
});

test('London and Paris form one group on the world map and split when zoomed in', () => {
    const state = createState([LONDON, PARIS, TOKYO]);

    const world = map.getMarkerGroups(state, map.getView(state));
    assert.deepEqual(membersOf(world), ['0,1', '2']);

    const zoomed = createState([LONDON, PARIS, TOKYO], { zoom: 4, lat: 50, lng: 1 });
    assert.deepEqual(membersOf(map.getMarkerGroups(zoomed, map.getView(zoomed))), ['0', '1', '2']);
});

test('groups are cached while the map is panned and rebuilt on zoom, data or settings change', () => {
    const state = createState([LONDON, PARIS, TOKYO]);
    const first = map.getMarkerGroups(state, map.getView(state));

    state.centerX += 0.3;
    assert.equal(map.getMarkerGroups(state, map.getView(state)), first, 'сдвиг карты группы не пересчитывает');

    state.zoom = 3;
    const zoomed = map.getMarkerGroups(state, map.getView(state));
    assert.notEqual(zoomed, first);

    state.pointsVersion += 1;
    assert.notEqual(map.getMarkerGroups(state, map.getView(state)), zoomed);

    const beforeSettings = map.getMarkerGroups(state, map.getView(state));
    state.clustering = map.normalizeClusterOptions({ clusterRadius: 80 }, state.clustering);
    assert.notEqual(map.getMarkerGroups(state, map.getView(state)), beforeSettings);
});

test('clustering can be turned off, and the focused point is never hidden in a group', () => {
    const off = createState([LONDON, PARIS, TOKYO], { clustering: map.normalizeClusterOptions({ clustering: false }) });
    assert.deepEqual(membersOf(map.getMarkerGroups(off, map.getView(off))), ['0', '1', '2']);

    const focused = createState([LONDON, PARIS, TOKYO], { focused: { lat: PARIS.latitude, lng: PARIS.longitude, name: 'Paris' } });
    assert.deepEqual(membersOf(map.getMarkerGroups(focused, map.getView(focused))), ['0', '1', '2']);
});

test('date line neighbours merge on the rectangular map but not on Equal Earth', () => {
    const points = [point('East', 10, 179.9), point('West', 10, -179.9)];

    const rectangular = createState(points, { projection: 'equirectangular' });
    assert.deepEqual(membersOf(map.getMarkerGroups(rectangular, map.getView(rectangular))), ['0,1']);

    // У Equal Earth они на противоположных краях карты.
    const equalEarth = createState(points);
    assert.deepEqual(membersOf(map.getMarkerGroups(equalEarth, map.getView(equalEarth))), ['0', '1']);
});

test('clicking a group zooms in just enough for it to split, centred between its points', () => {
    const state = createState([LONDON, PARIS, TOKYO]);
    const group = map.getMarkerGroups(state, map.getView(state)).find((candidate) => candidate.count === 2);

    const expansion = map.clusterExpansion(state, group);
    assert.equal(expansion.mode, 'zoom');
    assert.ok(expansion.zoom > state.zoom && expansion.zoom < map.MAX_ZOOM, `zoom ${expansion.zoom}`);
    assert.equal(map.clusterExpansion(state, group), expansion, 'результат запоминается в группе');

    const splitsAt = (zoom) => {
        const probe = createState([LONDON, PARIS, TOKYO], { zoom });
        return map.getMarkerGroups(probe, map.getView(probe)).every((candidate) => candidate.count === 1);
    };
    assert.ok(splitsAt(expansion.zoom), 'на выбранном приближении группа распалась');
    assert.ok(!splitsAt(expansion.zoom / map.ZOOM_STEP), 'шагом меньше — ещё нет: приближаем не больше нужного');

    const [londonX, londonY] = map.projectPoint(state, LONDON.latitude, LONDON.longitude);
    const [parisX, parisY] = map.projectPoint(state, PARIS.latitude, PARIS.longitude);
    assert.ok(Math.abs(expansion.centerX - (londonX + parisX) / 2) < 1e-12);
    assert.ok(Math.abs(expansion.centerY - (londonY + parisY) / 2) < 1e-12);

    map.expandCluster(state, group);
    assert.equal(state.zoom, expansion.zoom);
    assert.equal(state.spider, null);
});

test('points that never separate open as a fan instead of zooming', () => {
    const twins = [point('a', 55.75, 37.61), point('b', 55.75, 37.61), point('c', 55.750001, 37.610001)];
    const state = createState(twins);
    const [group] = map.getMarkerGroups(state, map.getView(state));
    assert.equal(group.count, 3);

    assert.deepEqual(map.clusterExpansion(state, group), { mode: 'spider' });
    map.expandCluster(state, group);
    assert.equal(state.zoom, 1, 'карта не приближается');
    assert.deepEqual(state.spider, { key: group.key });

    // На наибольшем приближении любая группа раскрывается веером.
    const london = createState([LONDON, point('Near London', 51.50741, -0.12781)], { zoom: map.MAX_ZOOM, lat: 51.5, lng: -0.13 });
    const [close] = map.getMarkerGroups(london, map.getView(london));
    assert.equal(close.count, 2);
    assert.equal(map.clusterExpansion(london, close).mode, 'spider');
});

test('fan places points apart from each other and from the centre, compactly', () => {
    const minimum = 2 * map.POINT_HOVER_RADIUS + 2;
    for (let count = 2; count <= 120; count += 1) {
        const offsets = map.spiderOffsets(count);
        assert.equal(offsets.length, count);
        for (let i = 0; i < count; i += 1) {
            assert.ok(Math.hypot(offsets[i].x, offsets[i].y) >= minimum, `${count}: точка ${i} у центра`);
            for (let j = i + 1; j < count; j += 1) {
                const distance = Math.hypot(offsets[i].x - offsets[j].x, offsets[i].y - offsets[j].y);
                assert.ok(distance >= minimum, `${count}: точки ${i} и ${j} на расстоянии ${distance.toFixed(1)}`);
            }
        }
    }
    const reach = (count) => Math.max(...map.spiderOffsets(count).map(({ x, y }) => Math.hypot(x, y)));
    assert.ok(reach(9) < 60, `9 точек: ${reach(9)}`);
    assert.ok(reach(100) < 170, `100 точек: ${reach(100)}`);
});

test('drawn markers become click targets; an open fan replaces its badge with its points', () => {
    const twins = [point('a', 40.71, -74), point('b', 40.71, -74), point('c', 40.71, -74), TOKYO];
    const state = createState(twins);

    map.drawMarkers(state, map.getView(state));
    const badge = state.hitTargets.find((target) => target.type === 'cluster');
    assert.equal(badge.group.count, 3);
    assert.equal(map.findTargetAt(state, { x: badge.x + 3, y: badge.y - 3 }), badge);
    assert.equal(map.findTargetAt(state, { x: badge.x + 200, y: badge.y }), null);

    map.expandCluster(state, badge.group);
    map.drawMarkers(state, map.getView(state));
    assert.ok(!state.hitTargets.some((target) => target.type === 'cluster'), 'значка раскрытой группы больше нет');
    const fan = state.hitTargets.filter((target) => target.type === 'point' && target.pointIndex !== 3);
    assert.deepEqual(fan.map((target) => target.pointIndex).sort(), [0, 1, 2]);
    const offsets = map.spiderOffsets(3);
    fan.forEach((target) => {
        const offset = offsets[[0, 1, 2].indexOf(target.pointIndex)];
        assert.ok(Math.abs(target.x - (badge.x + offset.x)) < 1e-9);
        assert.ok(Math.abs(target.y - (badge.y + offset.y)) < 1e-9);
    });
    // Точку веера находит клик, хотя под ней на карте ничего нет.
    assert.equal(map.findTargetAt(state, { x: fan[0].x, y: fan[0].y }).pointIndex, fan[0].pointIndex);

    // Группа исчезла (новые данные) — веер сворачивается сам.
    state.points = [TOKYO];
    state.pointsVersion += 1;
    map.drawMarkers(state, map.getView(state));
    assert.equal(state.spider, null);
});

test('group tooltip lists escaped titles, counts the rest and says what a click does', () => {
    const points = Array.from({ length: 11 }, (_, index) =>
        point(`p${index}`, 0, 0, { title: index === 0 ? '<img src=x onerror=alert(1)>' : `Точка ${index}`, color: index === 1 ? 'red;background:url(x)' : '#3987e5' }));
    const group = { key: 'all', count: 11, members: points.map((_, index) => index) };

    const zoom = map.renderClusterTooltip(group, points, 'zoom');
    assert.match(zoom, /11 точек/);
    assert.doesNotMatch(zoom, /<img/);
    assert.match(zoom, /&lt;img src=x onerror=alert\(1\)&gt;/);
    assert.doesNotMatch(zoom, /url\(x\)/, 'небезопасный цвет не попадает в style');
    assert.match(zoom, /Точка 7/);
    assert.doesNotMatch(zoom, /Точка 8/, 'показываются первые восемь');
    assert.match(zoom, /и ещё 3/);
    assert.match(zoom, /приблизить/);
    assert.match(map.renderClusterTooltip(group, points, 'spider'), /раскрыть/);
});

test('Russian plural, compact counts, colour shares and options', () => {
    const plural = (count) => `${count} ${map.pluralizePoints(count)}`;
    assert.deepEqual([1, 2, 4, 5, 11, 14, 21, 22, 25, 101, 112].map(plural), [
        '1 точка', '2 точки', '4 точки', '5 точек', '11 точек', '14 точек', '21 точка', '22 точки', '25 точек', '101 точка', '112 точек'
    ]);

    assert.deepEqual([7, 999, 1000, 1234, 9999, 12345].map(map.formatClusterCount), ['7', '999', '1k', '1.2k', '9.9k', '12k']);

    const shares = map.clusterColorShares([0, 1, 2, 3], [
        { color: '#d95926' }, { color: '#3987e5' }, { color: '#d95926' }, { color: 'javascript:alert(1)' }
    ]);
    assert.deepEqual(shares, [
        { color: '#d95926', count: 2 }, { color: '#3987e5', count: 1 }, { color: map.DEFAULT_MARKER_COLOR, count: 1 }
    ]);
    assert.equal(map.markerColor({ color: ' rgb(10, 20, 30) ' }), 'rgb(10, 20, 30)');

    assert.deepEqual(map.normalizeClusterOptions(undefined), { enabled: true, radius: map.DEFAULT_CLUSTER_RADIUS });
    assert.deepEqual(map.normalizeClusterOptions({ clustering: false, clusterRadius: 500 }), { enabled: false, radius: 200 });
    assert.deepEqual(map.normalizeClusterOptions({ clusterRadius: -3 }), { enabled: true, radius: 0 });
    // Обновление только одного поля оставляет второе.
    assert.deepEqual(map.normalizeClusterOptions({ clusterRadius: 40 }, { enabled: false, radius: 24 }), { enabled: false, radius: 40 });
    assert.deepEqual(map.normalizeClusterOptions({ clusterRadius: null }, { enabled: true, radius: 70 }), { enabled: true, radius: 70 });
});

test('map script wires clustering into the instance and the click handler', async () => {
    const source = await readText('wwwroot/js/community-map.js');

    assert.match(source, /clustering: normalizeClusterOptions\(options\)/, 'initializeCommunityMap читает настройки группировки');
    assert.match(source, /window\.setCommunityMapClustering\s*=/, 'настройки меняются без пересоздания карты');
    assert.match(source, /drawMarkers\(state, view\);/, 'кадр рисует точки группами');
    assert.match(source, /if \(state\.dragMoved\) \{/, 'отпускание после перетаскивания — не клик');
    assert.match(source, /expandCluster\(state, target\.group\);/, 'клик по группе её раскрывает');
});
