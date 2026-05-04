import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const readText = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8');

test('map UI surfaces use the shared dark visual style', async () => {
    const css = await readText('wwwroot/css/community-map.css');

    assert.match(css, /@import url\('\.\/zealous-ui\.css'\);/);
    assert.match(css, /\.map-header\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.match(css, /\.participants-panel\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.match(css, /\.participant-modal\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.doesNotMatch(css, /background:\s*(white|#f8f9fa|#e9ecef)\b/i);
    assert.doesNotMatch(css, /border(?:-color)?:\s*(1px solid )?#dee2e6\b/i);
});

test('participant forms and globe settings do not depend on light Bootstrap surfaces', async () => {
    const form = await readText('Components/GeoDataParticipantForm.razor');
    const registrationCss = await readText('wwwroot/css/participant-registration.css');
    const settings = await readText('Components/CommunityGlobeSettings.razor');
    const globeCss = await readText('wwwroot/css/community-globe.css');

    assert.match(form, /participant-registration\.css/);
    assert.doesNotMatch(form, /\bbg-light\b/);
    assert.match(registrationCss, /@import url\('\.\/zealous-ui\.css'\);/);
    assert.match(registrationCss, /\.geodata-participant-form\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.match(registrationCss, /\.geodata-participant-form \.form-control:focus\s*\{/);

    assert.match(settings, /globe-settings-panel/);
    assert.doesNotMatch(settings, /class="card"/);
    assert.match(globeCss, /\.globe-settings-panel\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.match(globeCss, /\.globe-settings-panel \.form-range::-webkit-slider-thumb\s*\{/);
    assert.match(globeCss, /\.globe-settings-panel \.form-check-input:checked\s*\{/);
    assert.match(globeCss, /\.globe-settings-panel \.form-control-color\s*\{/);
});

test('PWA manager uses the same dark component stylesheet', async () => {
    const component = await readText('Components/PwaManagerComponent.razor');
    const css = await readText('wwwroot/css/pwa-manager.css');

    assert.match(component, /pwa-manager\.css/);
    assert.doesNotMatch(component, /<style>/);
    assert.match(css, /@import url\('\.\/zealous-ui\.css'\);/);
    assert.match(css, /\.pwa-info-panel\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
    assert.match(css, /\.notification-test\s*\{[\s\S]*background:\s*var\(--zgl-surface\)/);
});

test('JS-created Google Maps content is themed and escapes participant data', async () => {
    const source = await readText('wwwroot/js/community-map.js');

    assert.match(source, /function escapeHtml\(value\)/);
    assert.match(source, /function createUserLocationMarker\(\)/);
    assert.match(source, /function createFocusMarker\(name\)/);
    assert.match(source, /createMarkerDataUrl\(createUserLocationMarker\(\)\)/);
    assert.match(source, /createMarkerDataUrl\(createFocusMarker\(name\)\)/);
    assert.match(source, /class="zgl-info-window"/);
    assert.match(source, /background:\s*#171a1f/);
    assert.match(source, /border:\s*1px solid rgba\(148,\s*163,\s*184,\s*0\.28\)/);
    assert.doesNotMatch(source, /color:\s*#666/);
    assert.doesNotMatch(source, /color:\s*#007bff/);
    assert.doesNotMatch(source, /#28a745|#ffffff/i);
});
