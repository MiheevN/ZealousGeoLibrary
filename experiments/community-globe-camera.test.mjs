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
        minZoom: 1.1,
        maxZoom: 4,
        enableAtmosphereGlow: true,
        enableClouds: true,
        cameraSurfaceClearance: 0.08,
        cameraZoomInMinSpeed: 0.16,
        cameraZoomInMaxSpeed: 0.9,
        cameraZoomOutSpeed: 1.15,
        cameraZoomSlowdownDistance: 1.1,
        participantMarkerReferenceDistance: 2.6,
        participantMarkerMinScale: 0.35,
        participantMarkerMaxScale: 1.2,
        participantLabelLoweringDistance: 1.1,
        ...options
    };

    return globe;
}

function round(value, digits = 2) {
    const scale = 10 ** digits;
    return Math.round(value * scale) / scale;
}

test('camera can move closer while staying outside the globe and atmosphere', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const limits = globe.getCameraDistanceLimits();

    assert.ok(limits.minDistance > 1.05, 'minimum camera distance should stay outside the atmosphere radius');
    assert.equal(round(limits.minDistance), 1.13);
    assert.equal(limits.maxDistance, 4);
});

test('zooming in slows near the surface while zooming out remains responsive', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const nearInSpeed = globe.calculateCameraZoomSpeed(1.13, true);
    const farInSpeed = globe.calculateCameraZoomSpeed(3.2, true);
    const outSpeed = globe.calculateCameraZoomSpeed(1.13, false);

    assert.ok(nearInSpeed < farInSpeed, 'zoom-in should slow down near the surface');
    assert.equal(nearInSpeed, 0.16);
    assert.equal(farInSpeed, 0.9);
    assert.equal(outSpeed, 1.15);
});

test('participant marker scale follows camera distance within configured bounds', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    assert.equal(round(globe.calculateParticipantMarkerDistanceScale(1.13)), 0.43);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(2.6), 1);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(5), 1.2);
});

test('participant label anchor lowers toward the marker base near close zoom', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const dimensions = {
        tipHeight: 0.16,
        bodyHeight: 0.06,
        labelGap: 0.038
    };

    const nearScale = globe.calculateParticipantMarkerDistanceScale(1.13);
    const farScale = globe.calculateParticipantMarkerDistanceScale(2.6);
    const nearAnchor = globe.calculateParticipantLabelAnchorHeight(1.13, dimensions, nearScale);
    const farAnchor = globe.calculateParticipantLabelAnchorHeight(2.6, dimensions, farScale);

    assert.equal(round(nearAnchor, 3), round(dimensions.labelGap * nearScale, 3));
    assert.equal(round(farAnchor, 3), round((dimensions.tipHeight + dimensions.bodyHeight + dimensions.labelGap) * farScale, 3));
    assert.ok(nearAnchor < farAnchor, 'close zoom should move label text down from marker end toward marker start');
});
