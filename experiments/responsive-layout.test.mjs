import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

test('globe viewer exposes configured size as responsive CSS variables', async () => {
    const component = await readText('Components/CommunityGlobeViewer.razor');
    const css = await readText('wwwroot/css/community-globe.css');

    assert.doesNotMatch(
        component,
        /style="width:\s*@\(Width\)px;\s*height:\s*@\(Height\)px;"/,
        'globe viewer should not force fixed inline pixel dimensions'
    );
    assert.match(component, /--zgl-globe-width:/);
    assert.match(component, /--zgl-globe-height:/);
    assert.match(component, /--zgl-globe-aspect-ratio:/);
    assert.match(css, /\.globe-canvas-container\s*\{[\s\S]*width:\s*min\(100%,\s*var\(--zgl-globe-width,\s*800px\)\)/);
    assert.match(css, /\.globe-canvas-container\s*\{[\s\S]*aspect-ratio:\s*var\(--zgl-globe-aspect-ratio,\s*4 \/ 3\)/);
    assert.match(css, /\.globe-canvas-container\s*>\s*canvas\s*\{[\s\S]*width:\s*100% !important/);
    assert.match(css, /\.globe-canvas-container\s*>\s*canvas\s*\{[\s\S]*height:\s*100% !important/);
});

test('globe renderer follows the actual responsive container size', async () => {
    const source = await readText('wwwroot/js/community-globe.js');

    assert.match(source, /getRenderSize\(\)/);
    assert.match(source, /applyConfiguredContainerSize\(width,\s*height\)/);
    assert.match(source, /new ResizeObserver\(/);
    assert.match(source, /this\.renderer\.setSize\(renderSize\.width,\s*renderSize\.height,\s*false\)/);
    assert.match(source, /this\.renderer\.setSize\(size\.width,\s*size\.height,\s*false\)/);
});

test('map canvas height is constrained for mobile viewports', async () => {
    const component = await readText('Components/CommunityMapComponent.razor');
    const css = await readText('wwwroot/css/community-map.css');

    assert.doesNotMatch(component, /style="height:\s*@Height;\s*width:\s*100%;"/);
    assert.match(component, /--zgl-map-height:\s*@Height/);
    assert.match(css, /\.map-canvas\s*\{[\s\S]*height:\s*min\(var\(--zgl-map-height,\s*500px\),\s*72svh\)/);
    assert.match(css, /@media \(max-width:\s*768px\)\s*\{[\s\S]*\.map-canvas\s*\{[\s\S]*height:\s*min\(var\(--zgl-map-height,\s*500px\),\s*58svh\)/);
});
