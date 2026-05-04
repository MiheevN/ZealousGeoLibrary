import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

async function loadCommunityGlobeClass() {
    const source = await readFile(new URL('../wwwroot/js/community-globe.js', import.meta.url), 'utf8');
    const testSource = source.replace(
        /initializeDependencies\(\)\.then\(success => \{[\s\S]*?\n\}\);\n\n\/\/ Глобальный реестр/,
        'dependenciesLoaded = true;\n\n// Глобальный реестр'
    );
    const moduleSource = `${testSource}\nexport { CommunityGlobe };\n`;
    const moduleUrl = `data:text/javascript;base64,${Buffer.from(moduleSource).toString('base64')}`;
    const { CommunityGlobe } = await import(moduleUrl);

    return CommunityGlobe;
}

function createFakeTimers() {
    let nextTimerId = 1;
    const timers = new Map();

    return {
        setTimeout(callback, delay) {
            const timerId = nextTimerId++;
            timers.set(timerId, { callback, delay, cancelled: false });
            return timerId;
        },
        clearTimeout(timerId) {
            const timer = timers.get(timerId);
            if (timer) {
                timer.cancelled = true;
            }
        },
        get activeTimers() {
            return Array.from(timers.values()).filter(timer => !timer.cancelled);
        },
        runTimer(timer) {
            if (!timer.cancelled) {
                timer.callback();
            }
        }
    };
}

function createGlobePrototypeInstance(CommunityGlobe) {
    const globe = Object.create(CommunityGlobe.prototype);

    globe.options = {
        autoRotate: true,
        autoRotateResumeDelay: 1200,
        autoRotateSpeed: 0.1
    };
    globe.state = {
        isAutoRotating: true,
        isUserInteracting: false
    };
    globe.controls = {
        autoRotate: true,
        autoRotateSpeed: 0.1
    };

    return globe;
}

test('automatic rotation pauses during interaction and resumes after the configured idle delay', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const fakeTimers = createFakeTimers();
    const originalSetTimeout = globalThis.setTimeout;
    const originalClearTimeout = globalThis.clearTimeout;

    globalThis.setTimeout = fakeTimers.setTimeout;
    globalThis.clearTimeout = fakeTimers.clearTimeout;

    try {
        globe.initializeAutoRotationInteractionState();

        globe.pauseAutoRotationForInteraction();

        assert.equal(globe.state.isAutoRotating, false);
        assert.equal(globe.state.isUserInteracting, true);
        assert.equal(globe.controls.autoRotate, false);

        globe.scheduleAutoRotationResume();

        assert.equal(globe.state.isAutoRotating, false);
        assert.equal(globe.state.isUserInteracting, false);
        assert.equal(fakeTimers.activeTimers.length, 1);
        assert.equal(fakeTimers.activeTimers[0].delay, 1200);

        fakeTimers.runTimer(fakeTimers.activeTimers[0]);

        assert.equal(globe.state.isAutoRotating, true);
        assert.equal(globe.controls.autoRotate, true);
        assert.equal(globe.controls.autoRotateSpeed, 0.1);
    } finally {
        globalThis.setTimeout = originalSetTimeout;
        globalThis.clearTimeout = originalClearTimeout;
    }
});

test('a new interaction cancels the pending automatic-rotation resume timer', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const fakeTimers = createFakeTimers();
    const originalSetTimeout = globalThis.setTimeout;
    const originalClearTimeout = globalThis.clearTimeout;

    globalThis.setTimeout = fakeTimers.setTimeout;
    globalThis.clearTimeout = fakeTimers.clearTimeout;

    try {
        globe.initializeAutoRotationInteractionState();

        globe.pauseAutoRotationForInteraction();
        globe.scheduleAutoRotationResume();

        const firstResumeTimer = fakeTimers.activeTimers[0];

        globe.pauseAutoRotationForInteraction();
        globe.scheduleAutoRotationResume();

        assert.equal(firstResumeTimer.cancelled, true);
        assert.equal(fakeTimers.activeTimers.length, 1);

        fakeTimers.runTimer(firstResumeTimer);

        assert.equal(globe.state.isAutoRotating, false);

        fakeTimers.runTimer(fakeTimers.activeTimers[0]);

        assert.equal(globe.state.isAutoRotating, true);
    } finally {
        globalThis.setTimeout = originalSetTimeout;
        globalThis.clearTimeout = originalClearTimeout;
    }
});

test('automatic rotation stays disabled when the user turns it off before the idle timer fires', async () => {
    const CommunityGlobe = await loadCommunityGlobeClass();
    const globe = createGlobePrototypeInstance(CommunityGlobe);
    const fakeTimers = createFakeTimers();
    const originalSetTimeout = globalThis.setTimeout;
    const originalClearTimeout = globalThis.clearTimeout;

    globalThis.setTimeout = fakeTimers.setTimeout;
    globalThis.clearTimeout = fakeTimers.clearTimeout;

    try {
        globe.initializeAutoRotationInteractionState();

        globe.pauseAutoRotationForInteraction();
        globe.scheduleAutoRotationResume();
        globe.setAutoRotation(false, 0.1);

        assert.equal(fakeTimers.activeTimers.length, 0);
        assert.equal(globe.options.autoRotate, false);
        assert.equal(globe.state.isAutoRotating, false);
        assert.equal(globe.controls.autoRotate, false);
    } finally {
        globalThis.setTimeout = originalSetTimeout;
        globalThis.clearTimeout = originalClearTimeout;
    }
});
