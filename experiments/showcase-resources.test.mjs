import assert from 'node:assert/strict';
import { readFile, readdir } from 'node:fs/promises';
import test from 'node:test';

// Надписи витрины лежат в Site.resx (английский) и Site.ru.resx (русский); разметка
// обращается к ним через Site.Ключ.
const root = new URL('../hosts/ZealousMindedPeopleGeo.Showcase/', import.meta.url);
const keysOf = source => [...source.matchAll(/<data name="([^"]+)"/g)].map(match => match[1]);

async function razorFiles(dir) {
    const entries = await readdir(dir, { withFileTypes: true });
    const files = await Promise.all(entries.map(entry => {
        const url = new URL(entry.name + (entry.isDirectory() ? '/' : ''), dir);
        return entry.isDirectory() ? razorFiles(url) : entry.name.endsWith('.razor') ? [url] : [];
    }));
    return files.flat();
}

test('the showcase has every text in English and Russian', async () => {
    const english = keysOf(await readFile(new URL('Resources/Site.resx', root), 'utf8'));
    const russian = keysOf(await readFile(new URL('Resources/Site.ru.resx', root), 'utf8'));

    assert.ok(english.length > 0);
    assert.deepEqual([...russian].sort(), [...english].sort());
});

test('pages use only existing texts and no hard-coded Russian', async () => {
    const keys = new Set(keysOf(await readFile(new URL('Resources/Site.resx', root), 'utf8')));
    for (const file of await razorFiles(new URL('Components/', root))) {
        const source = await readFile(file, 'utf8');
        for (const [, key] of source.matchAll(/\bSite\.(\w+)/g)) {
            assert.ok(keys.has(key), `${file.pathname}: Site.${key}`);
        }
        // Комментарии @* *@ и // остаются по-русски, разметка и строки — нет.
        const code = source.replace(/@\*[\s\S]*?\*@/g, '').replace(/^\s*\/\/.*$/gm, '');
        assert.doesNotMatch(code, /[А-Яа-яЁё]/, `${file.pathname} содержит русский текст вне ресурсов`);
    }
});
