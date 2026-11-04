import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса с настоящим Three.js, как в community-globe-planet.test.mjs.
async function loadGlobeModule() {
    const source = await readFile(new URL('../../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const layoutSource = await readFile(new URL('../../wwwroot/js/label-layout.js', import.meta.url), 'utf8');
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
    const threeModuleUrl = new URL('../../wwwroot/js/libs/three.module.js', import.meta.url).href;
    const moduleSource = [
        `import * as TestThree from '${threeModuleUrl}';`,
        testSource.replace('let THREE, OrbitControls;', 'let THREE = TestThree, OrbitControls;'),
        'export { CommunityGlobe, TestThree };'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const { CommunityGlobe, TestThree: THREE } = await loadGlobeModule();

function globeWith(options) {
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = { participantMarkerRadius: 0.018, ...options };
    return globe;
}

const dimensions = { radius: 0.02 };

test('the anchor lies flat on the surface under the marker tip, in the marker colour with a dark rim', () => {
    const anchor = globeWith({}).createParticipantMarkerAnchor(new THREE.Color('#ff0000'), dimensions);

    assert.equal(anchor.children.length, 2);
    const [rim, dot] = anchor.children;
    assert.equal(dot.material.color.getHexString(), 'ff0000');
    assert.equal(rim.material.color.getHexString(), '000000');
    assert.ok(dot.renderOrder > rim.renderOrder, 'the dot is drawn over its rim');

    for (const mesh of anchor.children) {
        mesh.geometry.computeBoundingBox();
        const box = mesh.geometry.boundingBox;
        // Плоскость XZ маркера — касательная к сфере: по Y (нормали) почти нулевая толщина.
        assert.ok(box.max.y - box.min.y < 1e-6, 'flat');
        assert.ok(box.min.y > 0 && box.max.y < 0.005, 'just above the marker origin, at the ripple height');
        assert.ok(Math.abs(box.max.x + box.min.x) < 1e-6 && Math.abs(box.max.z + box.min.z) < 1e-6, 'centred under the tip');
        assert.equal(mesh.material.depthWrite, false);
    }

    rim.geometry.computeBoundingSphere();
    dot.geometry.computeBoundingSphere();
    assert.ok(rim.geometry.boundingSphere.radius > dot.geometry.boundingSphere.radius, 'the rim surrounds the dot');
    assert.ok(dot.geometry.boundingSphere.radius > dimensions.radius, 'the dot is a little wider than the marker');
});

test('participantMarkerAnchor: false turns the anchor off', () => {
    assert.equal(globeWith({ participantMarkerAnchor: false }).createParticipantMarkerAnchor(new THREE.Color('#ffffff'), dimensions), null);
});

test('the anchor is wired into marker creation and scales with the marker', async () => {
    const source = await readFile(new URL('../../wwwroot/js/community-globe.js', import.meta.url), 'utf8');

    assert.match(source, /participantMarkerAnchor: true/, 'on by default');
    assert.match(source, /const anchor = this\.createParticipantMarkerAnchor\(color, dimensions\);[\s\S]*?marker\.add\(anchor\)/);
    assert.match(source, /marker\.userData\.anchor\?\.scale\.setScalar\(markerScale\)/);
    assert.match(source, /'participantMarkerAnchor',/, 'can be changed from the settings panel');
});
