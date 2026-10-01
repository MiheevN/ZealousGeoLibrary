import assert from 'node:assert/strict';
import { readdir, readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');
const listFiles = (path) => readdir(new URL(`../${path}`, import.meta.url));

const BUNDLE = 'zealous-geo.css';

async function componentSheets() {
    const files = await listFiles('wwwroot/css');
    return files.filter(name => name.endsWith('.css') && name !== BUNDLE).sort();
}

// Селекторы правил верхнего уровня; @media, @keyframes и прочие at-правила пропускаются.
function topLevelSelectors(css) {
    const source = css.replace(/\/\*[\s\S]*?\*\//g, '');
    const selectors = [];
    let depth = 0;
    let prelude = '';
    for (const ch of source) {
        if (ch === '{') {
            const text = prelude.trim();
            if (depth === 0 && text && !text.startsWith('@')) {
                selectors.push(...text.split(',').map(s => s.trim().replace(/\s+/g, ' ')));
            }
            depth++;
            prelude = '';
        } else if (ch === '}') {
            depth--;
            prelude = '';
        } else if (ch === ';' && depth === 0) {
            prelude = '';
        } else {
            prelude += ch;
        }
    }
    return selectors;
}

test('zealous-geo.css imports the tokens first and every component stylesheet once', async () => {
    const bundle = await readText(`wwwroot/css/${BUNDLE}`);
    const imports = [...bundle.matchAll(/@import url\('\.\/([^']+)'\);/g)].map(m => m[1]);

    assert.equal(imports[0], 'zealous-ui.css', 'tokens are imported before the components that use them');
    assert.deepEqual([...imports].sort(), await componentSheets(), 'every stylesheet in wwwroot/css is in the bundle');
    assert.equal(new Set(imports).size, imports.length, 'no stylesheet is imported twice');
});

test('components do not add stylesheets through HeadContent', async () => {
    // <HeadOutlet> выводит в <head> только последний отрисованный <HeadContent>,
    // поэтому на странице с несколькими компонентами стили остальных терялись.
    const files = (await listFiles('Components')).filter(name => name.endsWith('.razor'));
    assert.ok(files.length > 0);

    for (const name of files) {
        const source = await readText(`Components/${name}`);
        assert.doesNotMatch(source, /<HeadContent>/, `${name} uses <HeadContent>`);
        assert.doesNotMatch(source, /<link[^>]+\.css/, `${name} links a stylesheet`);
    }
});

test('component stylesheets do not redefine each other\'s top-level selectors', async () => {
    // В zealous-geo.css все файлы загружаются вместе, и одинаковый селектор
    // из файла ниже молча переопределил бы чужой компонент.
    const owners = new Map();
    for (const name of await componentSheets()) {
        if (name === 'zealous-ui.css') continue;
        for (const selector of new Set(topLevelSelectors(await readText(`wwwroot/css/${name}`)))) {
            const owner = owners.get(selector);
            assert.ok(!owner, `${selector} is defined in both ${owner} and ${name}`);
            owners.set(selector, name);
        }
    }
    assert.ok(owners.has('.zgl-tab-panel'), 'selectors were collected');
});
