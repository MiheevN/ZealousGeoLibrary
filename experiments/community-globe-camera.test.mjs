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

    return CommunityGlobe;
}

function createGlobePrototypeInstance(CommunityGlobe, options = {}) {
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = {
        minZoom: 1.03,
        maxZoom: 4,
        enableAtmosphereGlow: true,
        enableClouds: true,
        earthRadius: 1,
        cloudsRadius: 1.01,
        atmosphereRadius: 1.05,
        cameraSurfaceClearance: 0.02,
        cameraZoomInMinSpeed: 0.16,
        cameraZoomInMaxSpeed: 0.9,
        cameraZoomOutSpeed: 1.15,
        cameraZoomSlowdownDistance: 1.1,
        cameraRotateMinSpeed: 0.18,
        cameraRotateMaxSpeed: 1.0,
        cameraRotateSlowdownDistance: 1.5,
        participantMarkerReferenceDistance: 2.6,
        participantMarkerMinScale: 0.35,
        participantMarkerMaxScale: 1.2,
        participantMarkerLabelScreenGapPixels: 10,
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

function vectorFromLatLng(globe, latitude, longitude, radius) {
    const position = globe.latLngToVector3(latitude, longitude, radius);
    return new THREE.Vector3(position.x, position.y, position.z);
}

function assertVectorClose(actual, expected, message) {
    assert.ok(
        actual.distanceTo(expected) < 0.0000001,
        `${message}: expected ${expected.toArray().join(', ')}, got ${actual.toArray().join(', ')}`
    );
}

test('camera can move much closer while staying outside the globe and cloud layer', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const limits = globe.getCameraDistanceLimits();

    assert.ok(limits.minDistance > 1.01, 'minimum camera distance should stay outside the cloud layer');
    assert.ok(limits.minDistance < 1.05, 'minimum camera distance should allow entering the transparent atmosphere glow');
    assert.equal(round(limits.minDistance), 1.03);
    assert.equal(limits.maxDistance, 4);
});

test('zooming in slows near the surface while zooming out remains responsive', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const nearInSpeed = globe.calculateCameraZoomSpeed(1.03, true);
    const farInSpeed = globe.calculateCameraZoomSpeed(3.2, true);
    const outSpeed = globe.calculateCameraZoomSpeed(1.03, false);

    assert.ok(nearInSpeed < farInSpeed, 'zoom-in should slow down near the surface');
    assert.equal(nearInSpeed, 0.16);
    assert.equal(farInSpeed, 0.9);
    assert.equal(outSpeed, 1.15);
});

test('camera rotate sensitivity decreases as the camera approaches the surface', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    const nearSpeed = globe.calculateCameraRotateSpeed(1.03);
    const midSpeed = globe.calculateCameraRotateSpeed(1.78);
    const farSpeed = globe.calculateCameraRotateSpeed(3.2);

    assert.equal(nearSpeed, 0.18);
    assert.equal(farSpeed, 1.0);
    assert.ok(nearSpeed < midSpeed && midSpeed < farSpeed, 'rotate speed should grow with camera distance');
    assert.ok(Math.abs(midSpeed - 0.59) < 0.01, 'rotate speed should be near the midpoint at the slowdown midpoint');
});

test('participant marker scale follows camera distance within configured bounds', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);

    assert.equal(round(globe.calculateParticipantMarkerDistanceScale(1.03)), 0.4);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(2.6), 1);
    assert.equal(globe.calculateParticipantMarkerDistanceScale(5), 1.2);
});

test('participant marker keeps an angled body when the camera is centered on it', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const marker = new THREE.Group();
    const visual = new THREE.Group();
    const surfacePosition = new THREE.Vector3(0, 0, 1.02);
    const cameraDistance = globe.getClampedCameraDistance(globe.options.participantClickZoom);

    marker.position.copy(surfacePosition);
    marker.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), surfacePosition.clone().normalize());
    marker.userData = { visual };
    marker.add(visual);

    globe.camera = new THREE.PerspectiveCamera(75, 1, 0.02, 1000);
    globe.camera.position.set(0, 0, cameraDistance);
    globe.camera.lookAt(0, 0, 0);
    globe.camera.updateMatrixWorld(true);
    globe.participantMarkers = [marker];

    globe.updateParticipantMarkerTransforms();

    const identity = new THREE.Quaternion();
    const expectedTilt = globe.calculateParticipantMarkerTiltAmount(cameraDistance);
    assert.ok(expectedTilt > 0, 'click zoom should request a visible marker tilt');
    assert.equal(round(visual.quaternion.angleTo(identity), 3), round(expectedTilt, 3));
});

test('participant label offset keeps text beside and above the marker at close zoom', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const dimensions = {
        radius: 0.018,
        tipHeight: 0.16,
        bodyHeight: 0.06,
        labelGap: 0
    };
    const nearLabelScale = { height: 0.006, targetPixelHeight: 34 };
    const farLabelScale = { height: 0.05, targetPixelHeight: 34 };

    const nearScale = globe.calculateParticipantMarkerDistanceScale(1.03);
    const farScale = globe.calculateParticipantMarkerDistanceScale(2.6);
    const nearOffset = globe.calculateParticipantLabelOffset(1.03, dimensions, nearScale, nearLabelScale);
    const farOffset = globe.calculateParticipantLabelOffset(2.6, dimensions, farScale, farLabelScale);
    const farMarkerTop = (dimensions.tipHeight + dimensions.bodyHeight) * farScale;
    const nearMarkerRadius = dimensions.radius * nearScale;
    const farMarkerRadius = dimensions.radius * farScale;
    const nearScreenGap = nearLabelScale.height / nearLabelScale.targetPixelHeight * 10;
    const farScreenGap = farLabelScale.height / farLabelScale.targetPixelHeight * 10;

    assert.ok(nearOffset.side >= nearMarkerRadius + nearScreenGap, 'close label should be horizontally beside the marker point');
    assert.ok(farOffset.side >= farMarkerRadius + farScreenGap, 'far label should keep horizontal clearance from the marker point');
    assert.ok(nearOffset.lift >= nearMarkerRadius + nearLabelScale.height / 2 + nearScreenGap, 'close label bottom should stay above the marker point');
    assert.ok(farOffset.lift >= farMarkerRadius + farLabelScale.height / 2 + farScreenGap, 'far label bottom should stay above the marker point');
    assert.equal(round(nearOffset.depth, 3), 0);
    assert.equal(round(farOffset.depth, 3), round(farMarkerTop, 3));
    assert.ok(nearOffset.depth < farOffset.depth, 'zooming in should move the label from the marker top toward the sharp tip');
});

test('participant label connector targets the marker vertical spline when text is offset', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const marker = new THREE.Group();
    const label = new THREE.Object3D();
    const connector = new THREE.Line(
        new THREE.BufferGeometry(),
        new THREE.LineBasicMaterial({ transparent: true, opacity: 1 })
    );

    label.position.set(0.05, 0.12, -0.03);
    marker.userData = { label, labelConnector: connector };

    globe.updateParticipantLabelConnector(marker);

    const positions = Array.from(connector.geometry.getAttribute('position').array);
    assert.deepEqual(
        positions.map(value => round(value, 3)),
        [0, 0.12, 0, 0.05, 0.12, -0.03],
        'connector should run from the marker spline target to the label center'
    );
    assert.equal(connector.visible, true);

    label.position.set(0.00001, 0.12, -0.00001);
    globe.updateParticipantLabelConnector(marker);

    assert.equal(connector.visible, false, 'connector should hide when the label center is already on the marker spline');

    const visual = new THREE.Group();
    visual.scale.setScalar(0.5);
    visual.quaternion.setFromAxisAngle(new THREE.Vector3(1, 0, 0), Math.PI / 4);
    marker.userData.visual = visual;
    label.position.set(0.05, 0.12, -0.03);

    globe.updateParticipantLabelConnector(marker);

    const tiltedPositions = Array.from(connector.geometry.getAttribute('position').array);
    const expectedTiltedAnchor = new THREE.Vector3(0, 0.12 / visual.scale.y, 0)
        .multiply(visual.scale)
        .applyQuaternion(visual.quaternion);
    assert.deepEqual(
        tiltedPositions.slice(0, 3).map(value => round(value, 3)),
        expectedTiltedAnchor.toArray().map(value => round(value, 3)),
        'connector anchor should follow the tilted marker body axis'
    );
});

test('participant marker click centers camera on the rendered marker position at close zoom', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const participant = {
        id: 'participant-1',
        name: 'Test participant',
        latitude: 55.7558,
        longitude: 37.6173
    };
    let callbackMetadata = null;
    let targetPosition = null;
    let animationDuration = null;

    globe.state = { isInitialized: true };
    globe.earthGroup = new THREE.Group();
    globe.earthGroup.rotation.y = Math.PI / 2;
    globe.callbacks = {
        onParticipantClick: metadata => {
            callbackMetadata = metadata;
        }
    };
    globe.getIntersectedParticipantMarker = () => ({
        userData: { participant }
    });
    globe.updateMousePosition = () => {};
    globe.animateCameraTo = (position, duration) => {
        targetPosition = new THREE.Vector3(position.x, position.y, position.z);
        animationDuration = duration;
    };

    globe.onMouseClick({});

    const clickZoom = globe.getClampedCameraDistance(globe.options.participantClickZoom);
    const expectedRenderedPosition = vectorFromLatLng(globe, participant.latitude, participant.longitude, clickZoom);
    globe.earthGroup.localToWorld(expectedRenderedPosition);
    const staleUnrotatedPosition = vectorFromLatLng(globe, participant.latitude, participant.longitude, clickZoom);

    assert.deepEqual(callbackMetadata, participant);
    assert.equal(animationDuration, 1000);
    assertVectorClose(targetPosition, expectedRenderedPosition, 'click should center the rendered marker position');
    assert.ok(
        targetPosition.distanceTo(staleUnrotatedPosition) > 0.1,
        'regression check should distinguish the rendered marker from the unrotated coordinate vector'
    );
});
