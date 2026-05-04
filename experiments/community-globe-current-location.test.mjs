import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const componentSourceUrl = new URL('../Components/CommunityGlobeComponent.razor', import.meta.url);
const controlsSourceUrl = new URL('../Components/CommunityGlobeControls.razor', import.meta.url);

test('CommunityGlobeComponent exposes optional current-location coordinates to controls', async () => {
    const source = await readFile(componentSourceUrl, 'utf8');

    assert.match(source, /\[Parameter\]\s+public\s+double\?\s+CurrentLatitude\s*\{\s*get;\s*set;\s*\}/);
    assert.match(source, /\[Parameter\]\s+public\s+double\?\s+CurrentLongitude\s*\{\s*get;\s*set;\s*\}/);
    assert.match(source, /<CommunityGlobeControls[\s\S]*CurrentLatitude="@CurrentLatitude"[\s\S]*CurrentLongitude="@CurrentLongitude"/);
});

test('CommunityGlobeControls returns to provided current location instead of Moscow default', async () => {
    const source = await readFile(controlsSourceUrl, 'utf8');

    assert.match(source, /\[Parameter\]\s+public\s+double\?\s+CurrentLatitude\s*\{\s*get;\s*set;\s*\}/);
    assert.match(source, /\[Parameter\]\s+public\s+double\?\s+CurrentLongitude\s*\{\s*get;\s*set;\s*\}/);
    assert.match(source, /CenterOnCurrentLocationAsync/);
    assert.match(source, /var\s+latitude\s*=\s*CurrentLatitude\.Value;/);
    assert.match(source, /var\s+longitude\s*=\s*CurrentLongitude\.Value;/);
    assert.match(source, /GlobeMediator\.CenterOnAsync\(ContainerId,\s*latitude,\s*longitude/);
    assert.doesNotMatch(source, /CenterOnAsync\(ContainerId,\s*55\.7558,\s*37\.6176/);
});
