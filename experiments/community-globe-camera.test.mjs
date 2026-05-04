import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

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
    const moduleSource = `${testSource}\nexport { CommunityGlobe };\n`;
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    const { CommunityGlobe } = await import(moduleUrl);

    return CommunityGlobe;
}

function createGlobePrototypeInstance(CommunityGlobe, options = {}) {
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = {
        minZoom: 0.5,
        maxZoom: 4,
        enableAtmosphereGlow: true,
        enableClouds: true,
        cameraSurfaceClearance: 0.2,
        cameraZoomInMinSpeed: 0.16,
        cameraZoomInMaxSpeed: 0.9,
        cameraZoomOutSpeed: 1.15,
        cameraZoomSlowdownDistance: 1.1,
        participantMarkerReferenceDistance: 2.5,
        participantMarkerMinScale: 0.5,
        participantMarkerMaxScale: 1.7,
        ...options
    };

    return globe;
}

test('camera minimum distance is clamped outside the globe and atmosphere', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const limits = globe.getCameraDistanceLimits();

    assert.ok(limits.minDistance > 1.05, 'minimum camera distance should stay outside the atmosphere radius');
    assert.equal(limits.minDistance, 1.25);
    assert.equal(limits.maxDistance, 4);
});

test('zooming in slows near the surface while zooming out remains responsive', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const nearInSpeed = globe.calculateCameraZoomSpeed(1.25, true);
    const farInSpeed = globe.calculateCameraZoomSpeed(3.2, true);
    const outSpeed = globe.calculateCameraZoomSpeed(1.25, false);

    assert.ok(nearInSpeed < farInSpeed, 'zoom-in should slow down near the surface');
    assert.equal(nearInSpeed, 0.16);
    assert.equal(farInSpeed, 0.9);
    assert.equal(outSpeed, 1.15);
});

test('participant marker scale follows camera distance within configured bounds', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    assert.equal(globe.calculateParticipantMarkerDistanceScale(1.25), 0.5);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(2.5), 1);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(5), 1.7);
});
