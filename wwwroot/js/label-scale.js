export const DEFAULT_LABEL_PIXEL_HEIGHT = 34;

export function calculateLabelScaleForCamera(distanceToCamera, cameraFovDegrees, viewportHeight, labelAspectRatio, targetPixelHeight = DEFAULT_LABEL_PIXEL_HEIGHT) {
    const safeDistance = Math.max(Number(distanceToCamera) || 0, 0.0001);
    const safeViewportHeight = Math.max(Number(viewportHeight) || 0, 1);
    const safeAspectRatio = Math.max(Number(labelAspectRatio) || 1, 0.01);
    const safePixelHeight = Math.max(Number(targetPixelHeight) || DEFAULT_LABEL_PIXEL_HEIGHT, 1);
    const fovRadians = (Number(cameraFovDegrees) || 75) * Math.PI / 180;
    const visibleHeightAtDistance = 2 * safeDistance * Math.tan(fovRadians / 2);
    const worldHeight = visibleHeightAtDistance * safePixelHeight / safeViewportHeight;
    const worldWidth = worldHeight * safeAspectRatio;

    return { width: worldWidth, height: worldHeight };
}
