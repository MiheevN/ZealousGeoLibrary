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

    return { CommunityGlobe, source };
}

test('participant visuals use compact faceted pyramid markers instead of yellow point sprites', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.doesNotMatch(source, /new THREE\.Points\(/, 'participants should no longer be rendered as point sprites');
    assert.match(source, /participantMarkerFacets:\s*4/, 'default marker should read as a pyramid instead of a round pencil');
    assert.match(source, /const markerFacets = this\.getParticipantMarkerFacetCount\(\);/, 'marker should use the configured faceted profile');
    assert.match(source, /new THREE\.CylinderGeometry\([^;]*markerFacets/s, 'marker cap should use the configured faceted profile');
    assert.match(source, /new THREE\.ConeGeometry\([^;]*markerFacets/s, 'marker tip should use the configured faceted profile');
    assert.match(source, /participantPointColor:\s*['"]#(?:20d6df|24dce7|00cfd8)['"]/i, 'default marker color should be turquoise');
    assert.match(source, /participantPointSize:\s*0\.06/, 'default marker size should stay small');
    assert.match(source, /participantMarkerHeight:\s*0\.06/, 'marker cap should be short rather than pencil-like');
    assert.match(source, /participantMarkerTipHeight:\s*0\.16/, 'marker tip should be longer and sharper');
});

test('participant labels remain billboards outside the scaled marker body', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /marker\.add\(label\)/, 'labels should be attached to the surface marker root');
    assert.doesNotMatch(source, /visual\.add\(label\)/, 'labels should not inherit the marker body tilt or distance scale');
    assert.match(source, /updateParticipantLabelBillboards\(\);/, 'labels should be oriented toward the camera every frame');
});

test('participant labels keep screen-space clearance from marker points by default', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /participantMarkerLabelGap:\s*0/, 'default label anchor should sit on the marker with no extra normal gap');
    assert.match(source, /participantMarkerLabelScreenGapPixels:\s*10/, 'default label should keep a small pixel gap from the marker');
    assert.match(source, /calculateParticipantLabelSideClearance/, 'label side offset should account for marker radius');
    assert.match(source, /calculateParticipantLabelLiftClearance/, 'label lift should account for marker radius and label height');
    assert.match(source, /getNonNegativeNumber\(this\.options\.participantMarkerLabelGap,\s*0\)/, 'zero label gap should be accepted');
});

test('participant labels stay renderable during close zoom', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /cameraNearPlane:\s*0\.02/, 'camera near plane should allow labels to render at the closer zoom floor');
    assert.match(source, /new THREE\.PerspectiveCamera\(75,\s*aspect,\s*this\.getPositiveNumber\(this\.options\.cameraNearPlane,\s*0\.02\),\s*1000\)/s);
    assert.match(source, /depthTest:\s*false/, 'labels should not disappear behind marker or globe depth during close zoom');
    assert.match(source, /label\.frustumCulled\s*=\s*false/, 'labels should not be culled when their anchor moves near the frame edge');
    assert.match(source, /label\.renderOrder\s*=/, 'labels should render after the 3D marker body');
});

test('participant labels fade when their markers are hidden behind the globe', async () => {
    const { CommunityGlobe, source } = await loadCommunityGlobeClass();
    const globe = Object.create(CommunityGlobe.prototype);

    globe.options = {
        participantLabelHiddenOpacity: 0.12,
        participantLabelHorizonFade: 0.25
    };

    const frontOpacity = globe.calculateParticipantLabelVisibilityOpacity(
        { x: 0, y: 0, z: 1 },
        { x: 0, y: 0, z: 2.5 }
    );
    const hiddenOpacity = globe.calculateParticipantLabelVisibilityOpacity(
        { x: 0, y: 0, z: -1 },
        { x: 0, y: 0, z: 2.5 }
    );
    const horizonOpacity = globe.calculateParticipantLabelVisibilityOpacity(
        { x: 1.02, y: 0, z: 0 },
        { x: 0, y: 0, z: 2.5 }
    );

    assert.equal(frontOpacity, 1, 'front-side labels should remain fully bright');
    assert.equal(hiddenOpacity, 0.12, 'back-side labels should become much dimmer');
    assert.ok(horizonOpacity > hiddenOpacity, 'horizon labels should be brighter than hidden labels');
    assert.ok(horizonOpacity < frontOpacity, 'horizon labels should fade before becoming fully bright');
    assert.match(source, /updateParticipantLabelOpacity\(label\);/, 'label opacity should update every frame as the globe rotates');
});

test('marker facet count is configurable and pyramid-like by default', async () => {
    const { CommunityGlobe } = await loadCommunityGlobeClass();
    const globe = Object.create(CommunityGlobe.prototype);

    globe.options = { participantMarkerFacets: 4 };
    assert.equal(globe.getParticipantMarkerFacetCount(), 4);

    globe.options = { participantMarkerFacets: 20 };
    assert.equal(globe.getParticipantMarkerFacetCount(), 12);

    globe.options = { participantMarkerFacets: 2 };
    assert.equal(globe.getParticipantMarkerFacetCount(), 4);
});

test('participant ripple timing is globally configurable and can be overridden per point', async () => {
    const { CommunityGlobe } = await loadCommunityGlobeClass();
    const globe = Object.create(CommunityGlobe.prototype);

    globe.options = {
        participantMarkerRippleIntervalMs: 2000,
        participantMarkerRippleDurationMs: 500
    };

    assert.deepEqual(
        globe.getParticipantRippleTiming({}),
        { intervalMs: 2000, durationMs: 500 }
    );

    assert.deepEqual(
        globe.getParticipantRippleTiming({ rippleIntervalMs: 900, rippleDurationMs: 250 }),
        { intervalMs: 900, durationMs: 250 }
    );

    assert.deepEqual(
        globe.getParticipantRippleTiming({ rippleIntervalMs: -1, rippleDurationMs: Number.NaN }),
        { intervalMs: 2000, durationMs: 500 }
    );
});

test('marker tilt increases only when the camera is near the globe', async () => {
    const { CommunityGlobe } = await loadCommunityGlobeClass();
    const globe = Object.create(CommunityGlobe.prototype);

    globe.options = {
        participantMarkerTiltStartDistance: 2.2,
        participantMarkerTiltFullDistance: 1.1,
        participantMarkerMaxTiltDegrees: 42
    };

    assert.equal(globe.calculateParticipantMarkerTiltAmount(3), 0);
    assert.equal(globe.calculateParticipantMarkerTiltAmount(1.1), 42 * Math.PI / 180);

    const midTilt = globe.calculateParticipantMarkerTiltAmount(1.65);
    assert.ok(midTilt > 0);
    assert.ok(midTilt < 42 * Math.PI / 180);
});
