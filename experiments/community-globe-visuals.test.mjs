import assert from 'node:assert/strict';
import { readFile, access } from 'node:fs/promises';
import test from 'node:test';

// Модуль глобуса подгружается как в community-globe-keyboard.test.mjs.
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
const DEG = Math.PI / 180;

test('phones and weak GPUs get 4K textures, desktops 8K, and an explicit choice wins', () => {
    const { chooseGlobeTextureQuality: choose } = globe3d;

    assert.equal(choose({ maxTextureSize: 16384 }), 'high');
    assert.equal(choose({ maxTextureSize: 16384, coarsePointer: true }), 'standard');
    assert.equal(choose({ maxTextureSize: 4096 }), 'standard');
    assert.equal(choose({ maxTextureSize: 16384, deviceMemory: 2 }), 'standard');
    assert.equal(choose({ quality: 'high', coarsePointer: true, maxTextureSize: 4096 }), 'high');
    assert.equal(choose({ quality: 'standard', maxTextureSize: 16384 }), 'standard');
});

test('the camera distance makes the globe fill most of the smaller side of the canvas', () => {
    const { fitGlobeCameraDistance: fit } = globe3d;
    const fov = 75;
    const filled = (distance, aspect) => {
        const half = Math.min(fov / 2 * DEG, Math.atan(Math.tan(fov / 2 * DEG) * aspect));
        return Math.tan(Math.asin(1 / distance)) / Math.tan(half);
    };

    for (const aspect of [4 / 3, 16 / 9, 1, 0.6]) {
        assert.ok(Math.abs(filled(fit(fov, aspect), aspect) - 0.72) < 1e-9, `aspect ${aspect}`);
    }
    // На узком холсте шар ограничивает ширина, и камера отходит дальше.
    assert.ok(fit(fov, 0.6) > fit(fov, 4 / 3));
    // Раньше камера стояла на 2.5 и шар занимал чуть больше половины высоты.
    assert.ok(fit(fov, 4 / 3) < 2.5);
});

test('the sun shines from the camera side, offset up and to the left', () => {
    const { sunDirectionFromCamera: sun } = globe3d;
    const camera = { x: 0, y: 0, z: 2 };

    assert.deepEqual(sun(camera, 0), { x: 0, y: 0, z: 1 });

    const offset = sun(camera, 30);
    const angle = Math.acos(offset.z) / DEG;
    assert.ok(Math.abs(angle - 30) < 1e-9, `angle ${angle}`);
    assert.ok(offset.y > 0, 'from above');
    assert.ok(offset.x < 0, 'from the left');
    assert.ok(Math.abs(Math.hypot(offset.x, offset.y, offset.z) - 1) < 1e-12);

    // Над полюсом направление тоже определено.
    const pole = sun({ x: 0, y: 3, z: 0 }, 30);
    assert.ok(Object.values(pole).every(Number.isFinite));
    assert.ok(Math.abs(Math.acos(pole.y) / DEG - 30) < 1e-9);
});

test('stars are the same on every load, mostly dim, and lie far behind the globe', () => {
    const { starfield } = globe3d;
    const first = starfield(500);
    const second = starfield(500);

    assert.deepEqual(first.positions, second.positions);
    assert.equal(first.positions.length, 1500);
    assert.equal(first.colors.length, 1500);
    for (let i = 0; i < 500; i++) {
        const distance = Math.hypot(first.positions[i * 3], first.positions[i * 3 + 1], first.positions[i * 3 + 2]);
        assert.ok(distance > 40 && distance < 80, `star ${i} at ${distance}`);
    }
    const brightness = Array.from({ length: 500 }, (_, i) => first.colors[i * 3 + 1]);
    const dim = brightness.filter(value => value < 0.5).length;
    assert.ok(dim > 300, `${dim} dim stars of 500`);
    assert.equal(starfield(0).positions.length, 0);
});

test('built-in textures are formats a browser can decode and ship with the library', async () => {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');

    assert.doesNotMatch(source, /\.tiff?['"`]/, 'browsers cannot decode TIFF');
    const names = [...source.matchAll(/\$\{EARTH_ASSETS\}([\w$.{}' ?:=]+?\.jpg)/g)].map(match => match[1]);
    const files = new Set();
    for (const name of names) {
        if (name.includes("'8k' : '4k'")) {
            files.add(name.replace(/\$\{[^}]+\}/, '8k'));
            files.add(name.replace(/\$\{[^}]+\}/, '4k'));
        } else {
            files.add(name);
        }
    }
    assert.ok(files.size >= 5, [...files].join(', '));
    for (const file of files) {
        await access(new URL(`../wwwroot/assets/earth/${file}`, import.meta.url));
    }
});

test('clouds are an alpha mask and the day map is read as sRGB', async () => {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');

    assert.match(source, /alphaMap: loadCloudTexture\(/, 'clouds should mask, not veil the planet');
    assert.match(source, /prepare\(loadTexture\(this\.resolveEarthTextureUrl\(\)\), THREE\.SRGBColorSpace\)/);
});
