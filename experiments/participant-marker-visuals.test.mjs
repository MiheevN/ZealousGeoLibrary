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

test('participant visuals use 3D hexagonal markers instead of yellow point sprites', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.doesNotMatch(source, /new THREE\.Points\(/, 'participants should no longer be rendered as point sprites');
    assert.match(source, /new THREE\.CylinderGeometry\([^;]*,\s*6[,\)]/s, 'marker body should be a 6-sided prism');
    assert.match(source, /new THREE\.ConeGeometry\([^;]*,\s*6[,\)]/s, 'marker tip should be a sharp 6-sided cone');
    assert.match(source, /participantPointColor:\s*['"]#(?:20d6df|24dce7|00cfd8)['"]/i, 'default marker color should be turquoise');
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
