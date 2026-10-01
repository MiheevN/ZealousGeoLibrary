import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import vm from 'node:vm';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

const ORIGIN = 'https://geo.example';
const LIBRARY = `${ORIGIN}/_content/ZealousMindedPeopleGeo`;
const flush = () => new Promise((resolve) => setImmediate(resolve));

async function swVersion() {
    return (await readText('wwwroot/sw.js')).match(/const SW_VERSION = '([^']+)';/)[1];
}

// sw.js исполняется в песочнице с CacheStorage в памяти и подменяемой сетью.
async function loadServiceWorker({ caches: initialCaches = {}, network }) {
    const stores = new Map(Object.entries(initialCaches).map(([name, entries]) => [name, new Map(Object.entries(entries))]));
    const keyOf = (request) => new URL(typeof request === 'string' ? request : request.url, ORIGIN).href;
    const storeFor = (name) => {
        if (!stores.has(name)) stores.set(name, new Map());
        return stores.get(name);
    };
    const fetchCalls = [];
    const listeners = {};
    let skippedWaiting = false;

    class SandboxRequest extends Request {
        constructor(input, init) {
            super(typeof input === 'string' ? new URL(input, ORIGIN) : input, init);
        }
    }

    const sandbox = {
        URL,
        Request: SandboxRequest,
        Response,
        console: { log() { }, warn() { }, error() { } },
        location: { origin: ORIGIN },
        clients: { claim: async () => { } },
        skipWaiting: async () => { skippedWaiting = true; },
        addEventListener: (type, handler) => { (listeners[type] ??= []).push(handler); },
        fetch: async (request, init) => {
            fetchCalls.push({ url: keyOf(request), init });
            return network(keyOf(request));
        },
        caches: {
            keys: async () => [...stores.keys()],
            delete: async (name) => stores.delete(name),
            open: async (name) => ({
                put: async (request, response) => { storeFor(name).set(keyOf(request), await response.text()); },
                add: async (request) => {
                    const response = await sandbox.fetch(request);
                    if (!response.ok) throw new TypeError(`Request failed: ${response.status}`);
                    storeFor(name).set(keyOf(request), await response.text());
                }
            }),
            match: async (request) => {
                for (const store of stores.values()) {
                    if (store.has(keyOf(request))) return new Response(store.get(keyOf(request)));
                }
                return undefined;
            }
        }
    };
    sandbox.self = sandbox;
    vm.runInNewContext(await readText('wwwroot/sw.js'), sandbox);

    const runExtendable = async (type) => {
        const pending = [];
        listeners[type].forEach((handler) => handler({ waitUntil: (promise) => pending.push(promise) }));
        await Promise.all(pending);
    };

    return {
        stores,
        fetchCalls,
        get skippedWaiting() { return skippedWaiting; },
        install: () => runExtendable('install'),
        activate: () => runExtendable('activate'),
        // Возвращает текст ответа воркера или null, если запрос он не перехватил
        async request(url, { destination = '', method = 'GET', mode = 'no-cors', headers = {} } = {}) {
            let responded = null;
            listeners.fetch.forEach((handler) => handler({
                request: { url, method, mode, destination, headers: new Headers(headers) },
                respondWith: (promise) => { responded = promise; }
            }));
            if (!responded) return null;
            const text = await (await responded).text();
            await flush();
            return text;
        }
    };
}

test('SW_VERSION in sw.js matches the package <Version>', async () => {
    const csproj = await readText('ZealousMindedPeopleGeo.csproj');
    const packageVersion = csproj.match(/<Version>([^<]+)<\/Version>/)[1];

    assert.equal(await swVersion(), packageVersion,
        'Поднимите SW_VERSION в wwwroot/sw.js вместе с <Version> в ZealousMindedPeopleGeo.csproj');
});

test('activate deletes zealous-geo caches of other versions and keeps the site caches', async () => {
    const version = await swVersion();
    const sw = await loadServiceWorker({
        caches: {
            'zealous-geo-static-v1.0.0': {},
            'zealous-geo-dynamic-v1.0.0': {},
            [`zealous-geo-static-v${version}`]: {},
            'host-app-cache': {}
        },
        network: () => { throw new TypeError('offline'); }
    });

    await sw.activate();

    assert.deepEqual([...sw.stores.keys()].sort(), ['host-app-cache', `zealous-geo-static-v${version}`]);
});

test('install skips a missing asset and still calls skipWaiting', async () => {
    const version = await swVersion();
    const sw = await loadServiceWorker({
        network: (url) => url.endsWith('manifest.json') ? new Response('', { status: 404 }) : new Response(`body of ${url}`)
    });

    await sw.install();

    const precached = sw.stores.get(`zealous-geo-static-v${version}`);
    assert.equal(sw.skippedWaiting, true);
    assert.ok(precached.has(`${LIBRARY}/js/community-map.js`));
    assert.ok(!precached.has(`${LIBRARY}/manifest.json`));
    assert.ok(sw.fetchCalls.every((call) => call.url.startsWith(ORIGIN)));
});

test('library JS and CSS are network-first, revalidated, and served from cache offline', async () => {
    const version = await swVersion();
    let online = true;
    const sw = await loadServiceWorker({
        caches: {
            [`zealous-geo-static-v${version}`]: {
                [`${LIBRARY}/js/community-map.js`]: 'old map js',
                [`${LIBRARY}/css/community-map.css`]: 'old map css'
            }
        },
        network: (url) => {
            if (!online) throw new TypeError('offline');
            return new Response(`new ${url.split('/').pop()}`);
        }
    });

    assert.equal(await sw.request(`${LIBRARY}/js/community-map.js`, { destination: 'script' }), 'new community-map.js');
    assert.equal(await sw.request(`${LIBRARY}/css/community-map.css`, { destination: 'style' }), 'new community-map.css');
    assert.equal(sw.fetchCalls.at(-1).init?.cache, 'no-cache');

    online = false;
    assert.equal(await sw.request(`${LIBRARY}/js/community-map.js`, { destination: 'script' }), 'new community-map.js');
    assert.equal(await sw.request(`${LIBRARY}/css/community-map.css`, { destination: 'style' }), 'new community-map.css');
});

test('Blazor connection and SSE requests are not intercepted', async () => {
    const sw = await loadServiceWorker({ network: () => new Response('network') });

    // Незавершённый long polling блокировал бы активацию новой версии воркера
    assert.equal(await sw.request(`${ORIGIN}/_blazor?id=abc`), null);
    assert.equal(await sw.request(`${ORIGIN}/events`, { headers: { Accept: 'text/event-stream' } }), null);
    assert.equal(await sw.request(`${ORIGIN}/api/participants`), 'network');
});

test('library images stay cache-first and non-GET requests are not intercepted', async () => {
    const version = await swVersion();
    const texture = `${LIBRARY}/assets/earth/8k_earth_daymap.jpg`;
    const sw = await loadServiceWorker({
        caches: { [`zealous-geo-static-v${version}`]: { [texture]: 'cached texture' } },
        network: () => new Response('network texture')
    });

    assert.equal(await sw.request(texture, { destination: 'image' }), 'cached texture');
    assert.equal(sw.fetchCalls.length, 0);
    assert.equal(await sw.request(`${ORIGIN}/_blazor/negotiate`, { method: 'POST' }), null);
});
