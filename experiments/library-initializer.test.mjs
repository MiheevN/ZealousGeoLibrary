import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const STYLESHEET = '_content/ZealousMindedPeopleGeo/css/zealous-geo.css';

async function loadInitializer() {
    // Blazor находит инициализатор по имени файла {сборка}.lib.module.js и вызывает его экспорты.
    const source = await readFile(new URL('../wwwroot/ZealousMindedPeopleGeo.lib.module.js', import.meta.url), 'utf8');
    return import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
}

// Минимальный <head>: только то, что трогает инициализатор.
function createDocument(stylesheets, baseURI = 'https://site.example/') {
    const head = {
        children: [],
        insertBefore(node, reference) {
            const index = reference ? this.children.indexOf(reference) : -1;
            if (index < 0) {
                this.children.push(node);
            } else {
                this.children.splice(index, 0, node);
            }
        }
    };
    const createLink = () => {
        const link = {
            rel: '',
            attributes: {},
            setAttribute(name, value) { this.attributes[name] = value; },
            get nextSibling() { return head.children[head.children.indexOf(this) + 1] ?? null; }
        };
        let href = '';
        Object.defineProperty(link, 'href', {
            get: () => href,
            set: (value) => { href = new URL(value, baseURI).href; }
        });
        return link;
    };
    for (const href of stylesheets) {
        const link = createLink();
        link.rel = 'stylesheet';
        link.href = href;
        head.insertBefore(link, null);
    }
    return {
        baseURI,
        head,
        createElement: createLink,
        querySelectorAll: () => head.children.filter((element) => element.rel === 'stylesheet'),
        names: () => head.children.map((element) => element.href.split('/').pop())
    };
}

test('initializer exports the callbacks Blazor looks for', async () => {
    const initializer = await loadInitializer();

    assert.equal(typeof initializer.beforeWebStart, 'function', 'Blazor Web App');
    assert.equal(typeof initializer.afterWebStarted, 'function', 'Blazor Web App, enhanced navigation');
    assert.equal(typeof initializer.beforeStart, 'function', 'Blazor Server and WebAssembly');
});

test('library stylesheet goes after Bootstrap and before application styles', async () => {
    const { ensureStylesheet } = await loadInitializer();
    const doc = createDocument(['https://cdn.example/bootstrap@5.3.3/dist/css/bootstrap.min.css', 'app.css']);

    ensureStylesheet(doc);

    assert.deepEqual(doc.names(), ['bootstrap.min.css', 'zealous-geo.css', 'app.css']);
    assert.ok('data-zealous-geo' in doc.head.children[1].attributes, 'injected link is marked');
});

test('without Bootstrap the library stylesheet goes first, without any styles it is appended', async () => {
    const { ensureStylesheet } = await loadInitializer();

    const withAppStyles = createDocument(['app.css']);
    ensureStylesheet(withAppStyles);
    assert.deepEqual(withAppStyles.names(), ['zealous-geo.css', 'app.css']);

    const empty = createDocument([]);
    ensureStylesheet(empty);
    assert.deepEqual(empty.names(), ['zealous-geo.css']);
});

test('stylesheet linked by the application is not added again', async () => {
    const { ensureStylesheet } = await loadInitializer();
    const doc = createDocument([STYLESHEET, 'app.css']);

    ensureStylesheet(doc);
    ensureStylesheet(doc);

    assert.deepEqual(doc.names(), ['zealous-geo.css', 'app.css']);
});

test('stylesheet path follows <base href> of the application', async () => {
    const { ensureStylesheet } = await loadInitializer();
    const doc = createDocument([], 'https://site.example/geo/');

    ensureStylesheet(doc);

    assert.equal(doc.head.children[0].href, `https://site.example/geo/${STYLESHEET}`);
});

test('stylesheet is restored after enhanced navigation replaces <head>', async () => {
    const { afterWebStarted } = await loadInitializer();
    const doc = createDocument(['app.css']);
    const previousDocument = globalThis.document;
    globalThis.document = doc;
    try {
        const listeners = {};
        afterWebStarted({ addEventListener: (name, handler) => { listeners[name] = handler; } });

        listeners.enhancedload();

        assert.deepEqual(doc.names(), ['zealous-geo.css', 'app.css']);
    } finally {
        globalThis.document = previousDocument;
    }
});
