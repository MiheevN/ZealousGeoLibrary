import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const ORIGIN = 'https://site.example';
const SCRIPT = '/_content/ZealousMindedPeopleGeo/sw.js';

// Минимальный <head> с поиском по тегу и атрибутам, которые использует pwa.js.
function createDocument(elements = []) {
    const head = { children: [...elements], appendChild(node) { this.children.push(node); } };
    const matches = (element, selector) => {
        const [, tag, attribute, value] = selector.match(/^(\w+)\[(\w+)="([^"]+)"\]$/);
        return element.tag === tag && element[attribute] === value;
    };
    return {
        baseURI: `${ORIGIN}/`,
        readyState: 'complete',
        head,
        createElement: (tag) => ({ tag }),
        querySelector: (selector) => head.children.find((element) => matches(element, selector)) ?? null
    };
}

// pwa.js — ES-модуль для браузера: перед загрузкой подменяем window, navigator, document и fetch.
async function loadPwaModule({ document = createDocument(), swAllowedHeader = null } = {}) {
    const registrations = [];
    const info = [];
    const errors = [];
    Object.assign(globalThis, {
        window: { addEventListener() { }, matchMedia: () => ({ matches: false }) },
        document,
        fetch: async (url, init) => ({
            ok: true,
            url,
            method: init?.method,
            headers: { get: (name) => (name === 'Service-Worker-Allowed' ? swAllowedHeader : null) }
        })
    });
    Object.defineProperty(globalThis, 'navigator', {
        configurable: true,
        value: {
            serviceWorker: {
                controller: null,
                addEventListener() { },
                register: async (url, options) => {
                    registrations.push({ url, options });
                    return { scope: `${ORIGIN}${options.scope}`, addEventListener() { }, update: async () => { } };
                }
            }
        }
    });
    const originalConsole = { info: console.info, error: console.error, log: console.log };
    console.info = (...args) => info.push(args.join(' '));
    console.error = (...args) => errors.push(args.join(' '));
    console.log = () => { };

    const source = await readFile(new URL('../wwwroot/js/pwa.js', import.meta.url), 'utf8');
    // Каждая загрузка — отдельный экземпляр модуля, иначе состояние модуля общее между тестами.
    const module = await import(`data:text/javascript;base64,${Buffer.from(`${source}\n// ${Math.random()}`).toString('base64')}`);
    return {
        module,
        document,
        registrations,
        info,
        errors,
        restore: () => Object.assign(console, originalConsole)
    };
}

test('manifest, theme color and touch icon are added once when the app has none', async () => {
    const pwa = await loadPwaModule();
    try {
        assert.equal(pwa.module.ensureManifest(), true);
        assert.equal(pwa.module.ensureManifest(), false, 'second call adds nothing');

        const head = pwa.document.head.children;
        assert.deepEqual(head.map((element) => element.rel ?? element.name), ['manifest', 'theme-color', 'apple-touch-icon']);
        assert.equal(head[0].href, `${ORIGIN}/_content/ZealousMindedPeopleGeo/manifest.json`);
        assert.equal(head[2].href, `${ORIGIN}/_content/ZealousMindedPeopleGeo/icons/icon-192x192.png`);
    } finally {
        pwa.restore();
    }
});

test('application manifest is left alone', async () => {
    const own = { tag: 'link', rel: 'manifest', href: `${ORIGIN}/site.webmanifest` };
    const pwa = await loadPwaModule({ document: createDocument([own]) });
    try {
        assert.equal(pwa.module.ensureManifest(), false);
        assert.deepEqual(pwa.document.head.children, [own]);
    } finally {
        pwa.restore();
    }
});

test('without Service-Worker-Allowed header the worker is not registered and nothing is logged as an error', async () => {
    const pwa = await loadPwaModule({ swAllowedHeader: null });
    try {
        const registered = await pwa.module.registerServiceWorker(SCRIPT);

        assert.equal(registered, false);
        assert.deepEqual(pwa.registrations, []);
        assert.deepEqual(pwa.errors, []);
        assert.match(pwa.info.join('\n'), /Service-Worker-Allowed/);
        assert.ok(pwa.document.head.children.some((element) => element.rel === 'manifest'), 'manifest is still added');
    } finally {
        pwa.restore();
    }
});

test('header that allows the root registers the worker for the whole app', async () => {
    const pwa = await loadPwaModule({ swAllowedHeader: '/' });
    try {
        const registered = await pwa.module.registerServiceWorker(SCRIPT);

        assert.equal(registered, true);
        assert.deepEqual(pwa.registrations, [{ url: SCRIPT, options: { scope: '/', updateViaCache: 'none' } }]);
    } finally {
        pwa.restore();
    }
});

test('header narrower than the app root does not register the worker', async () => {
    const pwa = await loadPwaModule({ swAllowedHeader: '/_content/' });
    try {
        assert.equal(await pwa.module.registerServiceWorker(SCRIPT), false);
        assert.deepEqual(pwa.registrations, []);
    } finally {
        pwa.restore();
    }
});
