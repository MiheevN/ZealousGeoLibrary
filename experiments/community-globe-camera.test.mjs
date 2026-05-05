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
        participantMarkerLabelSideOffset: 0,
        participantMarkerLabelUpOffset: 0,
        participantMarkerLabelCloseLift: 0,
        participantLabelLiftDistance: 1.1,
        participantClickZoom: 1.35,
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

test('participant label offset keeps text on the marker and moves it along marker height', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const dimensions = {
        radius: 0.018,
        tipHeight: 0.16,
        bodyHeight: 0.06,
        labelGap: 0
    };

    const nearScale = globe.calculateParticipantMarkerDistanceScale(1.13);
    const farScale = globe.calculateParticipantMarkerDistanceScale(2.6);
    const nearOffset = globe.calculateParticipantLabelOffset(1.13, dimensions, nearScale);
    const farOffset = globe.calculateParticipantLabelOffset(2.6, dimensions, farScale);
    const farMarkerTop = (dimensions.tipHeight + dimensions.bodyHeight) * farScale;

    assert.equal(nearOffset.side, 0, 'close label should stay centered on the marker');
    assert.equal(farOffset.side, 0, 'far label should stay centered on the marker');
    assert.equal(nearOffset.lift, 0, 'close label should not lift away from the marker in screen space');
    assert.equal(farOffset.lift, 0, 'far label should not lift away from the marker in screen space');
    assert.equal(round(nearOffset.depth, 3), 0);
    assert.equal(round(farOffset.depth, 3), round(farMarkerTop, 3));
    assert.ok(nearOffset.depth < farOffset.depth, 'zooming in should move the label from the marker top toward the sharp tip');
});

test('participant marker click centers camera on marker at close zoom', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const participant = {
        id: 'participant-1',
        name: 'Test participant',
        latitude: 55.7558,
        longitude: 37.6173
    };
    let callbackMetadata = null;
    let centeredCall = null;

    globe.state = { isInitialized: true };
    globe.callbacks = {
        onParticipantClick: metadata => {
            callbackMetadata = metadata;
        }
    };
    globe.getIntersectedParticipantMarker = () => ({
        userData: { participant }
    });
    globe.updateMousePosition = () => {};
    globe.centerOn = (latitude, longitude, zoom) => {
        centeredCall = { latitude, longitude, zoom };
        return true;
    };

    globe.onMouseClick({});

    assert.deepEqual(callbackMetadata, participant);
    assert.deepEqual(centeredCall, {
        latitude: participant.latitude,
        longitude: participant.longitude,
        zoom: 1.35
    });
});
