import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

// Глобус и карта пишут подробный журнал только при globalThis.zealousGeoDebug = true:
// в обычной работе консоль браузера остаётся чистой.
for (const file of ['community-globe.js', 'community-map.js']) {
    test(`${file} logs details only in debug mode`, async () => {
        const source = await readFile(new URL(`../wwwroot/js/${file}`, import.meta.url), 'utf8');
        // debugGlobeState вызывают вручную ради вывода — он пишет всегда.
        const withoutDebugApi = source.replace(/export function debugGlobeState[\s\S]*?\nexport /, 'export ');
        const calls = withoutDebugApi.match(/\bconsole\.log\(/g) ?? [];

        assert.equal(calls.length, 1, 'console.log is called only from debugLog');
        assert.match(source, /function debugLog\(\.\.\.args\) \{\s*if \(globalThis\.zealousGeoDebug === true\) \{\s*console\.log\(\.\.\.args\);/);
    });
}
