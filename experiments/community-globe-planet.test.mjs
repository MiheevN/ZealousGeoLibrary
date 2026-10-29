import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-visuals.test.mjs, наружу выходит ещё и класс.
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
        'export { CommunityGlobe };'
    ].join('\n');
    return import(`data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`);
}

const globe3d = await loadGlobeModule();

// Настройки, с которыми глобус приходит из C#: Земля со всеми слоями.
const earthOptions = {
    earthTextureUrl: null,
    normalTextureUrl: 'earth-normal.jpg',
    roughnessTextureUrl: 'earth-roughness.jpg',
    cloudsTextureUrl: 'earth-clouds.jpg',
    nightTextureUrl: null,
    enableClouds: true,
    enableAtmosphereGlow: true,
    atmosphereColor: '#4fb4ff',
    atmosphereOpacity: 0.7
};

test('without a planet, or without its day map, the globe stays the Earth', () => {
    const { applyPlanetOptions: apply } = globe3d;

    for (const planet of [undefined, null, {}, { dayTextureUrl: '  ' }, { normalTextureUrl: 'mars-normal.jpg' }]) {
        const result = apply({ ...earthOptions, planet });
        assert.equal(result.customPlanet, false);
        assert.equal(result.cloudsTextureUrl, 'earth-clouds.jpg');
        assert.equal(result.roughnessTextureUrl, 'earth-roughness.jpg');
        assert.equal(result.enableAtmosphereGlow, true);
    }
});

test('a planet with only a day map gets no Earth layers: no relief, clouds, lights or atmosphere', () => {
    const result = globe3d.applyPlanetOptions({ ...earthOptions, planet: { dayTextureUrl: ' /maps/mars.jpg ' } });

    assert.equal(result.customPlanet, true);
    assert.equal(result.earthTextureUrl, '/maps/mars.jpg');
    assert.equal(result.normalTextureUrl, null);
    assert.equal(result.roughnessTextureUrl, null);
    assert.equal(result.cloudsTextureUrl, null);
    assert.equal(result.enableClouds, false);
    assert.equal(result.nightTextureUrl, null);
    assert.equal(result.enableAtmosphereGlow, false);
    // Без карты шероховатости — ровная матовая поверхность, без блеска океана.
    assert.equal(result.surfaceRoughness, 0.9);
});

test('every layer the planet brings is used, and its atmosphere wins over the default colour', () => {
    const result = globe3d.applyPlanetOptions({
        ...earthOptions,
        planet: {
            dayTextureUrl: 'venus.jpg',
            normalTextureUrl: 'venus-normal.jpg',
            roughnessTextureUrl: 'venus-roughness.jpg',
            cloudsTextureUrl: 'venus-clouds.jpg',
            nightTextureUrl: 'venus-night.jpg',
            atmosphereColor: '#ffcc88',
            atmosphereOpacity: 0.4,
            roughness: 0.3
        }
    });

    assert.equal(result.normalTextureUrl, 'venus-normal.jpg');
    assert.equal(result.roughnessTextureUrl, 'venus-roughness.jpg');
    assert.equal(result.cloudsTextureUrl, 'venus-clouds.jpg');
    assert.equal(result.enableClouds, true);
    assert.equal(result.nightTextureUrl, 'venus-night.jpg');
    assert.equal(result.enableAtmosphereGlow, true);
    assert.equal(result.atmosphereColor, '#ffcc88');
    assert.equal(result.atmosphereOpacity, 0.4);
    assert.equal(result.surfaceRoughness, 0.3);
});

test('switched-off clouds and atmosphere stay off, out-of-range numbers are clamped', () => {
    const result = globe3d.applyPlanetOptions({
        ...earthOptions,
        enableClouds: false,
        enableAtmosphereGlow: false,
        planet: { dayTextureUrl: 'p.jpg', cloudsTextureUrl: 'c.jpg', atmosphereColor: '#fff', atmosphereOpacity: 5, roughness: -1 }
    });

    assert.equal(result.enableClouds, false);
    assert.equal(result.enableAtmosphereGlow, false);
    assert.equal(result.atmosphereOpacity, 1);
    assert.equal(result.surfaceRoughness, 0);
});

test('the input options are left untouched', () => {
    const options = { ...earthOptions, planet: { dayTextureUrl: 'p.jpg' } };
    const copy = structuredClone(options);

    globe3d.applyPlanetOptions(options);

    assert.deepEqual(options, copy);
});

// Глобус без сцены: только то, что трогает applyDayNightMode.
function dayNightGlobe(options) {
    const globe = Object.create(globe3d.CommunityGlobe.prototype);
    globe.options = { nightLightsIntensity: 1.6, ambientLightIntensity: 0.3, hemisphereLightIntensity: 0.25, dayNightMode: 'realtime', ...options };
    globe.nightUniforms = { nightMap: { value: null }, nightStrength: { value: 0 }, sunDirectionView: { value: null } };
    globe.syncSunLightWithCamera = () => {};
    return globe;
}

test('at night a planet without its own lights map shows no lights, not the Earth cities', () => {
    const globe = dayNightGlobe(globe3d.applyPlanetOptions({ ...earthOptions, planet: { dayTextureUrl: 'mars.jpg' } }));

    globe.applyDayNightMode();

    assert.equal(globe.nightUniforms.nightMap.value, null);
    assert.equal(globe.nightUniforms.nightStrength.value, 0);
});
