// Раскладка подписей на экране без наложений.
//
// Подписи расставляются по одной, от важной к менее важной: каждая занимает первое из
// своих мест, которое не пересекает уже занятые, а если свободного места нет — скрывается.
//
// Чтобы при вращении глобуса подписи не прыгали и не мигали, раскладка помнит прошлый
// кадр: подпись сначала пробует место, где уже стоит, а видимая подпись при равной
// важности расставляется раньше новой. Решение меняется, только когда место занято.
// При прочих равных порядок — порядок в списке.

/**
 * @typedef {{ x: number, y: number, width: number, height: number }} LabelRect
 *   Прямоугольник в пикселях экрана: x, y — левый верхний угол.
 * @typedef {{ id: *, priority: number, placements: (LabelRect|null)[], previous?: number }} LabelCandidate
 *   Подпись: чем больше priority, тем раньше она получает место; placements — возможные
 *   места в порядке предпочтения, null — место за пределами экрана; previous — индекс
 *   места в прошлом кадре (-1 — подпись была скрыта, нет — подпись новая).
 */

// Прибавка к важности подписи, видимой в прошлом кадре: меньше шага между уровнями
// важности, поэтому новая более важная подпись всё равно вытесняет старую.
const INCUMBENT_BONUS = 0.5;

/**
 * Выбирает место для каждой подписи.
 * @param {LabelCandidate[]} candidates
 * @param {number} [padding=0] Наименьший зазор между подписями в пикселях.
 * @returns {Map<*, number>} id → индекс выбранного места или -1, если подпись скрыта.
 */
export function layoutLabels(candidates, padding = 0) {
    const gap = Number.isFinite(padding) && padding > 0 ? padding : 0;
    const order = (candidates ?? [])
        .map((candidate, index) => ({ candidate, index }))
        .sort((a, b) => (effectivePriority(b.candidate) - effectivePriority(a.candidate)) || (a.index - b.index));

    const occupied = [];
    const result = new Map();

    for (const { candidate } of order) {
        const placements = Array.isArray(candidate.placements) ? candidate.placements : [];
        let chosen = -1;

        for (const i of placementOrder(candidate, placements.length)) {
            const rect = placements[i];
            if (isUsableRect(rect) && !occupied.some(other => rectsOverlap(rect, other, gap))) {
                chosen = i;
                occupied.push(rect);
                break;
            }
        }

        result.set(candidate.id, chosen);
    }

    return result;
}

/**
 * Пересекаются ли прямоугольники с учётом зазора.
 * @param {LabelRect} a
 * @param {LabelRect} b
 * @param {number} [padding=0]
 */
export function rectsOverlap(a, b, padding = 0) {
    return a.x < b.x + b.width + padding
        && b.x < a.x + a.width + padding
        && a.y < b.y + b.height + padding
        && b.y < a.y + a.height + padding;
}

/**
 * Прямоугольник текста подписи на экране по её центру.
 * @param {number} centerX Центр подписи, пиксели.
 * @param {number} centerY
 * @param {number} pixelHeight Высота всей подписи (с полями), пиксели.
 * @param {number} aspectRatio Ширина / высота всей подписи.
 * @param {number} [insetX=0] Доля ширины, занятая полем с каждой стороны.
 * @param {number} [insetY=0] Доля высоты, занятая полем сверху и снизу.
 * @returns {LabelRect}
 */
export function labelTextRect(centerX, centerY, pixelHeight, aspectRatio, insetX = 0, insetY = 0) {
    const fullHeight = Math.max(Number(pixelHeight) || 0, 0);
    const fullWidth = fullHeight * Math.max(Number(aspectRatio) || 0, 0);
    const width = fullWidth * (1 - 2 * clampFraction(insetX));
    const height = fullHeight * (1 - 2 * clampFraction(insetY));

    return { x: centerX - width / 2, y: centerY - height / 2, width, height };
}

function priorityOf(candidate) {
    const priority = Number(candidate?.priority);
    return Number.isFinite(priority) ? priority : 0;
}

function wasVisible(candidate, count) {
    return Number.isInteger(candidate?.previous) && candidate.previous >= 0 && candidate.previous < count;
}

function effectivePriority(candidate) {
    const count = Array.isArray(candidate?.placements) ? candidate.placements.length : 0;
    return priorityOf(candidate) + (wasVisible(candidate, count) ? INCUMBENT_BONUS : 0);
}

// Сначала место из прошлого кадра, затем остальные по порядку предпочтения.
function placementOrder(candidate, count) {
    const order = Array.from({ length: count }, (_, i) => i);
    if (!wasVisible(candidate, count)) {
        return order;
    }
    return [candidate.previous, ...order.filter(i => i !== candidate.previous)];
}

function isUsableRect(rect) {
    return rect !== null && rect !== undefined
        && [rect.x, rect.y, rect.width, rect.height].every(Number.isFinite)
        && rect.width > 0 && rect.height > 0;
}

function clampFraction(value) {
    const number = Number(value);
    return Number.isFinite(number) ? Math.min(Math.max(number, 0), 0.49) : 0;
}
