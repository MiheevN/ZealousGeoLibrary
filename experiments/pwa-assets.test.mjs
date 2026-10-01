import assert from 'node:assert/strict';
import { readFile, stat } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');
const LIBRARY_PREFIX = '/_content/ZealousMindedPeopleGeo/';

// Путь вида /_content/ZealousMindedPeopleGeo/icons/x.png -> wwwroot/icons/x.png
const toWwwroot = (url) => `wwwroot/${url.slice(LIBRARY_PREFIX.length)}`;

// Ширина и высота из заголовка IHDR: PNG-подпись (8 байт), длина и тип чанка (8 байт), затем размеры.
async function pngSize(path) {
    const data = await readFile(new URL(`../${path}`, import.meta.url));
    assert.equal(data.toString('ascii', 1, 4), 'PNG', `${path} is a PNG file`);
    return `${data.readUInt32BE(16)}x${data.readUInt32BE(20)}`;
}

async function assertExists(path) {
    await assert.doesNotReject(stat(new URL(`../${path}`, import.meta.url)), `${path} exists`);
}

test('manifest meets browser installability requirements', async () => {
    const manifest = JSON.parse(await readText('wwwroot/manifest.json'));

    assert.ok(manifest.name && manifest.short_name, 'name and short_name are set');
    assert.ok(['standalone', 'fullscreen', 'minimal-ui'].includes(manifest.display), 'display mode allows installation');
    assert.equal(manifest.scope, '/', 'manifest scope matches the service worker scope');
    assert.ok(manifest.start_url.startsWith(manifest.scope), 'start_url is inside the scope');

    const any = manifest.icons.filter((icon) => (icon.purpose ?? 'any').split(' ').includes('any'));
    const sizes = any.map((icon) => icon.sizes);
    assert.ok(sizes.includes('192x192'), '192px icon for installation');
    assert.ok(sizes.includes('512x512'), '512px icon for splash screens');
    assert.ok(manifest.icons.some((icon) => icon.purpose === 'maskable'), 'maskable icon for Android masks');
});

test('every image referenced by manifest exists with the declared size', async () => {
    const manifest = JSON.parse(await readText('wwwroot/manifest.json'));
    const images = [
        ...manifest.icons,
        ...(manifest.shortcuts ?? []).flatMap((shortcut) => shortcut.icons ?? []),
        ...(manifest.screenshots ?? [])
    ];

    for (const image of images) {
        assert.ok(image.src.startsWith(LIBRARY_PREFIX), `${image.src} is a library asset`);
        const path = toWwwroot(image.src);
        await assertExists(path);
        if (image.sizes && image.type === 'image/png') {
            assert.equal(await pngSize(path), image.sizes, `${path} has declared size`);
        }
    }
});

test('images referenced by service worker and PwaService exist', async () => {
    const sources = await Promise.all([
        readText('wwwroot/sw.js'),
        readText('wwwroot/js/pwa.js'),
        readText('Services/PwaService.cs')
    ]);
    // Пути встречаются и от корня (/_content/...), и относительно <base href> (_content/...).
    const urls = sources.flatMap((source) =>
        [...source.matchAll(/\/?_content\/ZealousMindedPeopleGeo\/[\w\-./]+\.(?:png|svg|ico|webp|json)/g)]
            .map((m) => (m[0].startsWith('/') ? m[0] : `/${m[0]}`)));

    assert.ok(urls.length > 0, 'icons are referenced');
    for (const url of new Set(urls)) {
        await assertExists(toWwwroot(url));
    }
});

test('PWA module adds the library manifest itself, so the app needs no markup', async () => {
    const pwa = await readText('wwwroot/js/pwa.js');

    assert.match(pwa, /const MANIFEST = '_content\/ZealousMindedPeopleGeo\/manifest\.json';/);
    assert.match(pwa, /export async function registerServiceWorker[\s\S]*?ensureManifest\(\);/, 'registration adds the manifest');
});
