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

test('overlay indicators are wired into participant marker creation', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /participantOverlayIndicatorPixelSize:\s*14/, 'in-frame indicator pixel size has a sensible default');
    assert.match(source, /participantOverlayIndicatorEdgePixelSize:\s*9/, 'edge indicator pixel size has a sensible default');
    assert.match(source, /participantOverlayIndicatorActivationDistance:\s*1\.6/, 'activation distance defaults to close zoom');
    assert.match(source, /participantOverlayIndicatorFullDistance:\s*1\.15/, 'full opacity reached near the minimum zoom');
    assert.match(source, /createParticipantOverlayIndicator\(participant\)/, 'each marker creates an overlay indicator at construction');
    assert.match(source, /this\.participantMarkerOverlayIndicators\.push\(overlayIndicator\)/, 'overlay indicators are tracked in a per-instance list');
    assert.match(source, /this\.updateParticipantMarkerOverlayIndicators\(\);/, 'overlay indicators are updated every frame');
});

test('overlay indicator activation progress fades smoothly between thresholds', async () => {
    const { CommunityGlobe } = await loadCommunityGlobeClass();
    const globe = Object.create(CommunityGlobe.prototype);
    globe.options = {
        participantOverlayIndicatorActivationDistance: 1.6,
        participantOverlayIndicatorFullDistance: 1.15
    };

    assert.equal(globe.calculateOverlayIndicatorActivationProgress(3), 0, 'far distance disables overlay');
    assert.equal(globe.calculateOverlayIndicatorActivationProgress(1.6), 0, 'activation distance exactly is still off');
    assert.equal(globe.calculateOverlayIndicatorActivationProgress(1.15), 1, 'full distance reaches full opacity');
    assert.equal(globe.calculateOverlayIndicatorActivationProgress(1.05), 1, 'closer than full distance stays full');

    const midProgress = globe.calculateOverlayIndicatorActivationProgress(1.375);
    assert.ok(midProgress > 0 && midProgress < 1, 'mid-range distance fades partially');
});

test('overlay indicators are cleared with participants', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(
        source,
        /clearParticipants\(\)\s*\{[\s\S]*?this\.participantMarkerOverlayIndicators\.forEach\(indicator => \{[\s\S]*?this\.participantMarkerOverlayIndicators = \[\];/,
        'clearParticipants disposes and resets the overlay indicator list'
    );
});

test('overlay indicators ignore depth and stay outside frustum culling', async () => {
    const { source } = await loadCommunityGlobeClass();

    assert.match(source, /createParticipantOverlayIndicator\(participant = \{\}\)\s*\{[\s\S]*?depthTest:\s*false/, 'overlay indicators bypass depth so close geometry does not hide them');
    assert.match(source, /createParticipantOverlayIndicator\(participant = \{\}\)\s*\{[\s\S]*?indicator\.frustumCulled = false/, 'indicators are not culled when their anchor moves off-screen');
});
