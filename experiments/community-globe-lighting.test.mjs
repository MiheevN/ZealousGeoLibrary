import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import * as THREE from '../wwwroot/js/libs/three.module.js';

async function loadCommunityGlobeClass() {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const testSource = source
        .replace(
            /import \{ DEFAULT_LABEL_PIXEL_HEIGHT, calculateLabelScaleForCamera \} from '\.\/label-scale\.js';\n/,
            [
                'const DEFAULT_LABEL_PIXEL_HEIGHT = 34;',
                'const calculateLabelScaleForCamera = () => ({ width: 1, height: 1 });'
            ].join('\n')
        )
        .replace(
            /initializeDependencies\(\)\.then\(success => \{[\s\S]*?\n\}\);\n\n\/\/ Глобальный реестр/,
            'dependenciesLoaded = true;\n\n// Глобальный реестр'
        );
    const threeModuleUrl = new URL('../wwwroot/js/libs/three.module.js', import.meta.url).href;
    const moduleSource = [
        `import * as TestThree from '${threeModuleUrl}';`,
        testSource.replace('let THREE, OrbitControls;', 'let THREE = TestThree, OrbitControls;'),
        'export { CommunityGlobe };'
    ].join('\n');
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    const { CommunityGlobe } = await import(moduleUrl);

    return { CommunityGlobe, source };
}

function createLightingGlobe(CommunityGlobe) {
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = {
        sunLightFollowCamera: true,
        sunLightDistance: 6,
        sunLightIntensity: 2.8,
        sunLightColor: '#ffffff',
        ambientLightIntensity: 1.2,
        ambientLightColor: '#9db7d1',
        hemisphereLightIntensity: 0.8,
        hemisphereSkyColor: '#d8f1ff',
        hemisphereGroundColor: '#253042',
        atmosphereLightIntensity: 0.7,
        atmosphereLightColor: '#8fdcff'
    };
    globe.scene = new THREE.Scene();
    globe.camera = new THREE.PerspectiveCamera(75, 1, 0.02, 1000);

    return globe;
}

function assertSameDirection(actual, expected, message) {
    const actualDirection = actual.clone().normalize();
    const expectedDirection = expected.clone().normalize();

    assert.ok(
        actualDirection.distanceTo(expectedDirection) < 0.0000001,
        `${message}: expected ${expectedDirection.toArray().join(', ')}, got ${actualDirection.toArray().join(', ')}`
    );
}

test('sun light is aligned with the camera so the visible globe face stays lit', async () => {
    const { CommunityGlobe } = await loadCommunityGlobeClass();
    const globe = createLightingGlobe(CommunityGlobe);

    globe.camera.position.set(-2, 1, 4);
    globe.setupLighting();

    const sunLight = globe.scene.children.find(child => child instanceof THREE.DirectionalLight);

    assert.ok(sunLight, 'globe should create a directional sun light');
    assertSameDirection(sunLight.position, globe.camera.position, 'initial sun direction should follow the camera');

    globe.camera.position.set(3, -2, 1);
    globe.syncSunLightWithCamera();

    assertSameDirection(sunLight.position, globe.camera.position, 'sun direction should update after camera movement');
    assert.equal(Math.round(sunLight.position.length()), 6, 'sun should keep a stable configured distance for shadows');
});

test('lighting defaults describe a clear camera-facing day setup', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /sunLightFollowCamera:\s*true/, 'camera-aligned sunlight should be the default');
    assert.match(source, /hemisphereLightIntensity:\s*0\.[0-9]+/, 'a soft sky/ground fill should support clear daytime lighting');
    assert.match(source, /this\.syncSunLightWithCamera\(\);/, 'sun direction should be synchronized during animation');
    assert.match(source, /earth\.receiveShadow\s*=\s*true/, 'the globe should be able to receive subtle marker shadows');
    assert.match(source, /\[tip,\s*body\]\.forEach\(mesh => \{[\s\S]*mesh\.castShadow\s*=\s*true/, '3D participant markers should cast subtle shadows');
});
