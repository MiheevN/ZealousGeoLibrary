import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

test('globe settings range inputs keep their dragged value while editing', async () => {
    const source = await readText('Components/CommunityGlobeSettings.razor');
    const rangeInputs = source.match(/<input\s+type="range"[^>]*>/g) ?? [];

    assert.ok(rangeInputs.length > 0, 'expected globe settings to include range inputs');
    assert.match(source, /@using\s+System\.Globalization/);

    for (const input of rangeInputs) {
        assert.match(input, /@bind:event="oninput"/, `${input} should update the bound setting during drag`);
        assert.match(input, /@bind:culture="CultureInfo\.InvariantCulture"/, `${input} should parse dot-decimal range values consistently`);
    }
});
