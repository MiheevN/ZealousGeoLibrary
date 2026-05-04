import assert from 'node:assert/strict';
import { calculateLabelScaleForCamera } from '../wwwroot/js/label-scale.js';

const cameraFov = 75;
const viewportHeight = 600;
const labelAspectRatio = 4;
const targetPixelHeight = 34;

const nearScale = calculateLabelScaleForCamera(1, cameraFov, viewportHeight, labelAspectRatio, targetPixelHeight);
const farScale = calculateLabelScaleForCamera(4, cameraFov, viewportHeight, labelAspectRatio, targetPixelHeight);

assert.ok(nearScale.height > 0, 'near label height should be positive');
assert.ok(farScale.height > nearScale.height, 'far label should get larger world scale to preserve screen size');
assert.equal(Math.round((farScale.height / nearScale.height) * 100) / 100, 4);
assert.equal(Math.round((farScale.width / nearScale.width) * 100) / 100, 4);
assert.equal(Math.round((nearScale.width / nearScale.height) * 100) / 100, labelAspectRatio);

console.log('Label scale regression checks passed');
