import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-keyboard.test.mjs. В Node нет document,
// поэтому WebGL «не поддерживается» — как в браузере с выключенным ускорением.
async function loadGlobeModule() {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const layoutSource = await readFile(new URL('../wwwroot/js/label-layout.js', import.meta.url), 'utf8');
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
    const threeModuleUrl = new URL('../wwwroot/js/libs/three.module.js', import.meta.url).href;
    const moduleSource = [
        `import * as TestThree from '${threeModuleUrl}';`,
        testSource.replace('let THREE, OrbitControls;', 'let THREE = TestThree, OrbitControls;'),
        'export const Three = TestThree;'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const globe3d = await loadGlobeModule();

test('a failure carries a known reason code and the technical text', () => {
    const { GLOBE_ERROR_CODES, globeCreationResult } = globe3d;
    const error = new Error('WebGL is not supported in this browser');
    error.code = GLOBE_ERROR_CODES.webGlUnavailable;

    assert.deepEqual(globeCreationResult(error), {
        success: false,
        errorCode: 'webgl-unavailable',
        errorMessage: 'WebGL is not supported in this browser'
    });
    assert.deepEqual(globeCreationResult(null), { success: true, errorCode: null, errorMessage: null });
});

test('an unexpected error is reported as unknown', () => {
    const { globeCreationResult } = globe3d;

    assert.equal(globeCreationResult(new TypeError('x is undefined')).errorCode, 'unknown');
    const odd = new Error('odd');
    odd.code = 'ENOENT';
    assert.equal(globeCreationResult(odd).errorCode, 'unknown');
    assert.equal(globeCreationResult('plain text').errorMessage, 'plain text');
});

test('createGlobe waits for the start and tells .NET that WebGL is missing', async () => {
    const originalError = console.error;
    console.error = () => {};
    try {
        const result = await globe3d.createGlobe('globe-without-webgl', { width: 300, height: 200 });

        assert.equal(result.success, false);
        assert.equal(result.errorCode, 'webgl-unavailable');
        assert.match(result.errorMessage, /WebGL/);
        // Неудавшийся глобус не остаётся в реестре: повтор создаёт его заново.
        assert.equal(globe3d.setGlobeReadyCallbackDirect('globe-without-webgl', {}), false);
    } finally {
        console.error = originalError;
    }
});
