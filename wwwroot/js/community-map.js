// Статичная 2D-карта сообщества на чистом Canvas.
// По умолчанию использует равновеликую проекцию Equal Earth (Šavrič, Patterson,
// Jenny, 2018); как опция доступна равнопромежуточная цилиндрическая проекция
// (equirectangular). Центральный меридиан настраивается. Карта не зависит
// от внешних картографических API, поддерживает приближение/отдаление колесом
// мыши или кнопками и перемещение по карте перетаскиванием.

const mapInstances = new Map();
const containerStates = new WeakMap();
let dotNetHelper = null;

const DEG = Math.PI / 180;
const DEFAULT_PROJECTION = 'equalearth';
// Доля контейнера, которую карта мира занимает при zoom = 1.
const FIT_PADDING = 0.96;
// Шаг дробления рёбер полигонов и линий сетки, в градусах: прямые в градусах
// отрезки в Equal Earth становятся кривыми.
const DENSIFY_STEP = 2;

// Коэффициенты полинома Equal Earth из статьи авторов проекции.
const EE_A1 = 1.340264;
const EE_A2 = -0.081106;
const EE_A3 = 0.000893;
const EE_A4 = 0.003796;
const EE_M = Math.sqrt(3) / 2;

// Ключи — имена проекций без регистра, пробелов, дефисов и подчёркиваний:
// 'equalEarth', 'EqualEarth' и 'equal-earth' означают одно и то же.
const PROJECTIONS = {
    equalearth: createProjection('equalEarth', equalEarthForward, equalEarthInverse, false),
    // Прямоугольная карта бесшовно склеивается сама с собой, поэтому её можно
    // прокручивать по горизонтали бесконечно.
    equirectangular: createProjection('equirectangular', equirectangularForward, equirectangularInverse, true)
};

const WORLD_LAND_PATHS = buildWorldLandPaths();

window.setDotNetHelper = (helper) => {
    dotNetHelper = helper;
};

// options: { projection: 'equalEarth' | 'equirectangular', centralMeridian: градусы }.
window.initializeCommunityMap = (apiKey, centerLat, centerLng, zoom, containerId, options) => {
    // apiKey оставлен в сигнатуре для обратной совместимости и игнорируется:
    // карта работает полностью локально, без обращений к Google Maps.
    const targetId = containerId || 'map';
    const container = document.getElementById(targetId);
    if (!container) {
        console.error(`Элемент карты #${targetId} не найден`);
        return;
    }

    // Повторная инициализация того же контейнера не должна оставлять
    // подписки предыдущего экземпляра.
    mapInstances.get(targetId)?.dispose();

    const instance = createMapInstance(container, {
        centerLat: numberOrDefault(centerLat, 20),
        centerLng: numberOrDefault(centerLng, 0),
        zoom: numberOrDefault(zoom, 2),
        projection: options?.projection,
        centralMeridian: numberOrDefault(options?.centralMeridian, 0)
    });

    mapInstances.set(targetId, instance);
    instance.draw();
    console.log(`Карта сообщества инициализирована (${targetId}, ${instance.projection})`);
};

window.loadParticipantsOnMap = (participantsJson, containerId) => {
    const instance = resolveInstance(containerId);
    if (!instance) {
        return;
    }

    try {
        const participants = Array.isArray(participantsJson)
            ? participantsJson
            : JSON.parse(participantsJson);
        instance.setParticipants(participants);
        console.log(`Загружено ${participants.length} участников на карту`);
    } catch (error) {
        console.error('Ошибка загрузки участников на карту:', error);
    }
};

window.centerMapOnUserLocation = (containerId) => {
    const instance = resolveInstance(containerId);
    if (!instance) {
        return;
    }

    if (!navigator.geolocation) {
        console.error('Геолокация не поддерживается в этом браузере');
        return;
    }

    navigator.geolocation.getCurrentPosition(
        (position) => {
            instance.setUserLocation({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            });
            instance.centerOn(position.coords.latitude, position.coords.longitude, 5);
            console.log('Карта центрирована на местоположении пользователя');
        },
        (error) => {
            console.error('Ошибка получения геолокации:', error);
        }
    );
};

window.focusOnParticipant = (latitude, longitude, name, containerId) => {
    const instance = resolveInstance(containerId);
    if (!instance) {
        return;
    }
    instance.focusParticipant(latitude, longitude, name);
    console.log(`Фокус на участнике: ${name}`);
};

window.disposeCommunityMap = (containerId) => {
    const targetId = containerId || 'map';
    const instance = mapInstances.get(targetId);
    if (instance) {
        instance.dispose();
        mapInstances.delete(targetId);
    }
};

window.CommunityMapUtils = {
    initializeCommunityMap: window.initializeCommunityMap,
    loadParticipantsOnMap: window.loadParticipantsOnMap,
    centerMapOnUserLocation: window.centerMapOnUserLocation,
    focusOnParticipant: window.focusOnParticipant,
    disposeCommunityMap: window.disposeCommunityMap,
    // SVG-маркеры экспортированы как data: URL — их можно использовать в
    // пользовательских инфоокнах или экспортных предпросмотрах.
    markers: {
        user: () => createMarkerDataUrl(createUserLocationMarker()),
        focus: (name) => createMarkerDataUrl(createFocusMarker(name)),
        participant: (name) => createMarkerDataUrl(createCustomMarker(name))
    }
};

function resolveInstance(containerId) {
    const targetId = containerId || 'map';
    const instance = mapInstances.get(targetId);
    if (!instance) {
        console.error(`Карта #${targetId} не инициализирована`);
    }
    return instance;
}

function numberOrDefault(value, fallback) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function createMapInstance(container, initialState) {
    const canvas = document.createElement('canvas');
    canvas.className = 'community-map-canvas';
    canvas.setAttribute('role', 'img');
    canvas.setAttribute('aria-label', 'Карта участников сообщества');
    container.innerHTML = '';
    container.appendChild(canvas);

    const tooltip = document.createElement('div');
    tooltip.className = 'community-map-tooltip';
    tooltip.style.display = 'none';
    container.appendChild(tooltip);

    const controls = createZoomControls(container);

    const projection = resolveProjection(initialState.projection);
    const centralMeridian = wrapLng(initialState.centralMeridian);

    const state = {
        canvas,
        ctx: canvas.getContext('2d'),
        container,
        tooltip,
        controls,
        participants: [],
        userLocation: null,
        focused: null,
        // Проекция и полигоны суши в её координатах
        projection,
        centralMeridian,
        land: buildProjectedLand(projection, centralMeridian),
        // Камера: центр в координатах проекции и приближение
        zoom: clampZoom(initialState.zoom),
        centerX: 0,
        centerY: 0,
        // Внутренние размеры
        width: 0,
        height: 0,
        devicePixelRatio: window.devicePixelRatio || 1,
        // Состояние перетаскивания
        dragging: false,
        dragStart: null,
        // Кеш hover-маркера
        hoveredParticipantIndex: -1,
        // Подписки на события
        listeners: []
    };
    setCenter(state, initialState.centerLat, initialState.centerLng);

    const resize = () => {
        const rect = container.getBoundingClientRect();
        const width = Math.max(rect.width, 1);
        const height = Math.max(rect.height, 1);
        state.width = width;
        state.height = height;
        state.devicePixelRatio = window.devicePixelRatio || 1;
        canvas.width = Math.round(width * state.devicePixelRatio);
        canvas.height = Math.round(height * state.devicePixelRatio);
        canvas.style.width = `${width}px`;
        canvas.style.height = `${height}px`;
        state.ctx.setTransform(state.devicePixelRatio, 0, 0, state.devicePixelRatio, 0, 0);
        draw();
    };

    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(container);

    const draw = () => drawScene(state);

    const onWheel = (event) => {
        event.preventDefault();
        const direction = event.deltaY > 0 ? -1 : 1;
        const factor = direction > 0 ? 1.25 : 1 / 1.25;
        const rect = canvas.getBoundingClientRect();
        const pointer = {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };
        zoomAt(state, factor, pointer);
        draw();
    };

    const onPointerDown = (event) => {
        if (event.button !== 0) {
            return;
        }
        const view = getView(state);
        state.dragging = true;
        state.dragStart = {
            x: event.clientX,
            y: event.clientY,
            centerX: view.centerX,
            centerY: view.centerY
        };
        canvas.setPointerCapture?.(event.pointerId);
        canvas.style.cursor = 'grabbing';
    };

    const onPointerMove = (event) => {
        if (state.dragging && state.dragStart) {
            const scale = viewScale(state);
            state.centerX = state.dragStart.centerX - (event.clientX - state.dragStart.x) / scale;
            state.centerY = state.dragStart.centerY + (event.clientY - state.dragStart.y) / scale;
            commitView(state);
            draw();
            return;
        }

        updateHover(state, event);
    };

    const endDrag = (event) => {
        if (state.dragging) {
            state.dragging = false;
            state.dragStart = null;
            canvas.releasePointerCapture?.(event.pointerId);
            canvas.style.cursor = 'grab';
        }
    };

    const onClick = (event) => {
        const rect = canvas.getBoundingClientRect();
        const pointer = {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };
        const index = findParticipantAt(state, pointer);
        if (index >= 0) {
            const participant = state.participants[index];
            if (dotNetHelper && dotNetHelper.invokeMethodAsync) {
                dotNetHelper.invokeMethodAsync(
                    'OnParticipantMarkerClick',
                    String(participant.Id ?? index)
                );
            }
        }
    };

    canvas.addEventListener('wheel', onWheel, { passive: false });
    canvas.addEventListener('pointerdown', onPointerDown);
    canvas.addEventListener('pointermove', onPointerMove);
    canvas.addEventListener('pointerup', endDrag);
    canvas.addEventListener('pointercancel', endDrag);
    canvas.addEventListener('pointerleave', () => hideTooltip(state));
    canvas.addEventListener('click', onClick);

    canvas.style.cursor = 'grab';

    controls.zoomIn.addEventListener('click', () => {
        zoomAt(state, 1.25, { x: state.width / 2, y: state.height / 2 });
        draw();
    });
    controls.zoomOut.addEventListener('click', () => {
        zoomAt(state, 1 / 1.25, { x: state.width / 2, y: state.height / 2 });
        draw();
    });
    controls.reset.addEventListener('click', () => {
        // Возврат к виду, с которым карта была инициализирована.
        state.zoom = clampZoom(initialState.zoom);
        setCenter(state, initialState.centerLat, initialState.centerLng);
        draw();
    });

    state.listeners.push({ target: window, type: 'resize', handler: resize });
    window.addEventListener('resize', resize);

    resize();

    return {
        draw,
        projection: projection.name,
        setParticipants(list) {
            state.participants = Array.isArray(list) ? list.slice() : [];
            draw();
        },
        setUserLocation(location) {
            state.userLocation = location;
            draw();
        },
        centerOn(lat, lng, zoom) {
            setCenter(state, lat, lng);
            if (Number.isFinite(zoom)) {
                state.zoom = clampZoom(zoom);
            }
            draw();
        },
        focusParticipant(lat, lng, name) {
            state.focused = { lat, lng, name };
            this.centerOn(lat, lng, Math.max(state.zoom, 4));
            window.clearTimeout(state.focusTimer);
            state.focusTimer = window.setTimeout(() => {
                state.focused = null;
                draw();
            }, 4000);
        },
        dispose() {
            resizeObserver.disconnect();
            state.listeners.forEach(({ target, type, handler }) => {
                target.removeEventListener(type, handler);
            });
            container.innerHTML = '';
        }
    };
}

function createZoomControls(container) {
    const wrapper = document.createElement('div');
    wrapper.className = 'community-map-controls';

    const zoomIn = document.createElement('button');
    zoomIn.type = 'button';
    zoomIn.className = 'community-map-control-btn';
    zoomIn.setAttribute('aria-label', 'Приблизить');
    zoomIn.textContent = '+';

    const zoomOut = document.createElement('button');
    zoomOut.type = 'button';
    zoomOut.className = 'community-map-control-btn';
    zoomOut.setAttribute('aria-label', 'Отдалить');
    zoomOut.textContent = '−';

    const reset = document.createElement('button');
    reset.type = 'button';
    reset.className = 'community-map-control-btn';
    reset.setAttribute('aria-label', 'Сбросить вид');
    reset.textContent = '⌂';

    wrapper.appendChild(zoomIn);
    wrapper.appendChild(zoomOut);
    wrapper.appendChild(reset);
    container.appendChild(wrapper);

    return { zoomIn, zoomOut, reset };
}

function clampZoom(value) {
    const fallback = Number.isFinite(value) ? value : 2;
    return Math.min(20, Math.max(1, fallback));
}

function clampLat(value) {
    if (!Number.isFinite(value)) {
        return 0;
    }
    return Math.max(-90, Math.min(90, value));
}

function wrapLng(value) {
    if (!Number.isFinite(value)) {
        return 0;
    }
    let result = value;
    while (result > 180) {
        result -= 360;
    }
    while (result < -180) {
        result += 360;
    }
    return result;
}

// Сворачивает значение в отрезок [−period/2, period/2].
function wrapPeriodic(value, period) {
    if (!Number.isFinite(value)) {
        return 0;
    }
    return value - period * Math.round(value / period);
}

// --- Проекции -------------------------------------------------------------
// Прямое преобразование принимает долготу относительно центрального меридиана
// и широту в градусах и возвращает [x, y] в единицах проекции (y — на север).

function createProjection(name, forward, inverse, wrapsHorizontally) {
    const [xMax] = forward(180, 0);
    const [, yMax] = forward(0, 90);
    // Контур карты: меридиан +180° с юга на север и меридиан −180° обратно.
    // Полюса у Equal Earth — отрезки, они замыкают контур.
    const outline = [];
    for (let lat = -90; lat <= 90; lat += DENSIFY_STEP) {
        outline.push(forward(180, lat));
    }
    for (let lat = 90; lat >= -90; lat -= DENSIFY_STEP) {
        outline.push(forward(-180, lat));
    }
    return { name, forward, inverse, wrapsHorizontally, xMax, yMax, period: 2 * xMax, outline };
}

function resolveProjection(name) {
    const key = String(name ?? '').replace(/[\s_-]/g, '').toLowerCase();
    if (key && !PROJECTIONS[key]) {
        console.warn(`Неизвестная проекция карты «${name}», используется Equal Earth`);
    }
    return PROJECTIONS[key] || PROJECTIONS[DEFAULT_PROJECTION];
}

function equalEarthForward(lng, lat) {
    const lambda = lng * DEG;
    const theta = Math.asin(EE_M * Math.sin(lat * DEG));
    const t2 = theta * theta;
    const t6 = t2 * t2 * t2;
    const x = lambda * Math.cos(theta)
        / (EE_M * (EE_A1 + 3 * EE_A2 * t2 + t6 * (7 * EE_A3 + 9 * EE_A4 * t2)));
    const y = theta * (EE_A1 + EE_A2 * t2 + t6 * (EE_A3 + EE_A4 * t2));
    return [x, y];
}

// Обратное преобразование: параметрическую широту θ находим из y методом
// Ньютона, дальше долгота и широта выражаются явно.
function equalEarthInverse(x, y) {
    let theta = y;
    for (let i = 0; i < 12; i += 1) {
        const t2 = theta * theta;
        const t6 = t2 * t2 * t2;
        const delta = (theta * (EE_A1 + EE_A2 * t2 + t6 * (EE_A3 + EE_A4 * t2)) - y)
            / (EE_A1 + 3 * EE_A2 * t2 + t6 * (7 * EE_A3 + 9 * EE_A4 * t2));
        theta -= delta;
        if (Math.abs(delta) < 1e-12) {
            break;
        }
    }
    const t2 = theta * theta;
    const t6 = t2 * t2 * t2;
    const lambda = EE_M * x * (EE_A1 + 3 * EE_A2 * t2 + t6 * (7 * EE_A3 + 9 * EE_A4 * t2))
        / Math.cos(theta);
    const lat = Math.asin(Math.max(-1, Math.min(1, Math.sin(theta) / EE_M)));
    return [lambda / DEG, lat / DEG];
}

function equirectangularForward(lng, lat) {
    return [lng * DEG, lat * DEG];
}

function equirectangularInverse(x, y) {
    return [x / DEG, y / DEG];
}

// --- Камера ---------------------------------------------------------------

// Пикселей на единицу проекции. При zoom = 1 карта мира целиком вписана в окно.
function viewScale(state) {
    const { projection } = state;
    const fit = Math.min(
        state.width / (2 * projection.xMax),
        state.height / (2 * projection.yMax)
    );
    return fit * FIT_PADDING * state.zoom;
}

// Видимая область. Камера не даёт увести карту за край: если по оси карта
// меньше окна, она центрируется, иначе край карты не отрывается от края окна.
// Склеивающаяся по горизонтали проекция вместо этого сворачивается по периоду.
function getView(state) {
    const { projection } = state;
    const scale = viewScale(state);
    const halfWidth = state.width / 2 / scale;
    const halfHeight = state.height / 2 / scale;
    return {
        scale,
        centerX: projection.wrapsHorizontally
            ? wrapPeriodic(state.centerX, projection.period)
            : clampAxis(state.centerX, projection.xMax, halfWidth),
        centerY: clampAxis(state.centerY, projection.yMax, halfHeight)
    };
}

function clampAxis(center, extent, halfView) {
    if (!Number.isFinite(center)) {
        return 0;
    }
    const limit = Math.max(0, extent - halfView);
    return Math.max(-limit, Math.min(limit, center));
}

// После действий пользователя запоминаем уже ограниченный центр, чтобы
// перетаскивание за край не копило смещение, которое потом нужно «отматывать».
function commitView(state) {
    const view = getView(state);
    state.centerX = view.centerX;
    state.centerY = view.centerY;
}

function setCenter(state, lat, lng) {
    const [x, y] = projectPoint(state, lat, lng);
    state.centerX = x;
    state.centerY = y;
}

function zoomAt(state, factor, pointer) {
    // Точка карты под указателем остаётся на месте.
    const view = getView(state);
    const anchorX = view.centerX + (pointer.x - state.width / 2) / view.scale;
    const anchorY = view.centerY - (pointer.y - state.height / 2) / view.scale;
    state.zoom = clampZoom(state.zoom * factor);
    const scale = viewScale(state);
    state.centerX = anchorX - (pointer.x - state.width / 2) / scale;
    state.centerY = anchorY + (pointer.y - state.height / 2) / scale;
    commitView(state);
}

// Координаты точки в плоскости проекции относительно центрального меридиана.
function projectPoint(state, lat, lng) {
    return state.projection.forward(wrapLng(lng - state.centralMeridian), clampLat(lat));
}

function toCanvas(state, view, x, y) {
    return {
        x: state.width / 2 + (x - view.centerX) * view.scale,
        y: state.height / 2 - (y - view.centerY) * view.scale
    };
}

function projectToCanvas(state, lat, lng) {
    const view = getView(state);
    let [x, y] = projectPoint(state, lat, lng);
    if (state.projection.wrapsHorizontally) {
        // Из бесконечной ленты копий мира берём ближайшую к центру окна.
        x = view.centerX + wrapPeriodic(x - view.centerX, state.projection.period);
    }
    return toCanvas(state, view, x, y);
}

// Все экранные позиции точки: у склеивающейся проекции точка повторяется
// в каждой видимой копии мира.
function projectToCanvasCopies(state, view, lat, lng) {
    const [x, y] = projectPoint(state, lat, lng);
    return worldCopyOffsets(state, view).map((offset) => toCanvas(state, view, x + offset, y));
}

function worldCopyOffsets(state, view) {
    const { projection } = state;
    const halfWidth = state.width / 2 / view.scale;
    if (!projection.wrapsHorizontally || !Number.isFinite(halfWidth)) {
        return [0];
    }
    const first = Math.ceil((view.centerX - halfWidth - projection.xMax) / projection.period);
    const last = Math.floor((view.centerX + halfWidth + projection.xMax) / projection.period);
    const offsets = [];
    for (let n = first; n <= last; n += 1) {
        offsets.push(n * projection.period);
    }
    return offsets;
}

// Возвращает null, если точка экрана лежит вне карты.
function canvasToLatLng(state, x, y) {
    const { projection } = state;
    const view = getView(state);
    let px = view.centerX + (x - state.width / 2) / view.scale;
    const py = view.centerY - (y - state.height / 2) / view.scale;
    if (projection.wrapsHorizontally) {
        px = wrapPeriodic(px, projection.period);
    }
    if (Math.abs(py) > projection.yMax) {
        return null;
    }
    const [relativeLng, lat] = projection.inverse(px, py);
    if (!Number.isFinite(relativeLng) || Math.abs(relativeLng) > 180 + 1e-9) {
        return null;
    }
    return { lat, lng: wrapLng(relativeLng + state.centralMeridian) };
}

// --- Суша -----------------------------------------------------------------

// Полигоны суши в координатах проекции. Считаются один раз на проекцию и
// центральный меридиан, а при отрисовке кадра их остаётся сдвинуть и
// отмасштабировать. Рёбра полигонов — отрезки в координатах долгота/широта
// (как в GeoJSON); фигуры, пересекающие линию перемены даты, разрезаны по ней.
function buildProjectedLand(projection, centralMeridian) {
    // Долгота относительно центрального меридиана лежит в (−360°, 360°).
    // При фиксированной широте x линеен по долготе, поэтому копии со сдвигом
    // ±360° вместе с обрезкой по контуру карты дают разрез по линии перемены
    // даты. Склеивающейся проекции копии не нужны: их дают копии мира.
    const shifts = projection.wrapsHorizontally ? [0] : [-360, 0, 360];
    const rings = [];
    WORLD_LAND_PATHS.forEach((shape) => {
        const points = densifyRing(shape, DENSIFY_STEP);
        const relative = points.map(([lng]) => lng - centralMeridian);
        const min = Math.min(...relative);
        const max = Math.max(...relative);
        for (const shift of shifts) {
            if (max + shift <= -180 || min + shift >= 180) {
                continue;
            }
            rings.push(points.map(([, lat, seam], index) => {
                const [x, y] = projection.forward(relative[index] + shift, lat);
                return [x, y, seam];
            }));
        }
    });
    return rings;
}

// Дробит рёбра на отрезки не длиннее step градусов и помечает вершины на
// линии перемены даты: рёбра вдоль неё — разрез данных, а не берег.
function densifyRing(ring, step) {
    const points = [];
    for (let i = 0; i < ring.length - 1; i += 1) {
        const [lng0, lat0] = ring[i];
        const [lng1, lat1] = ring[i + 1];
        const count = Math.max(1, Math.ceil(Math.max(Math.abs(lng1 - lng0), Math.abs(lat1 - lat0)) / step));
        for (let j = 0; j < count; j += 1) {
            const lng = lng0 + (lng1 - lng0) * j / count;
            points.push([lng, lat0 + (lat1 - lat0) * j / count, Math.abs(lng) === 180]);
        }
    }
    const [lng, lat] = ring[ring.length - 1];
    points.push([lng, lat, Math.abs(lng) === 180]);
    return points;
}

// --- Отрисовка ------------------------------------------------------------

function drawScene(state) {
    const { ctx, width, height, projection } = state;
    const view = getView(state);
    ctx.clearRect(0, 0, width, height);

    // Пространство вокруг карты чуть темнее океана.
    ctx.fillStyle = '#0b1118';
    ctx.fillRect(0, 0, width, height);
    drawOcean(state, view);

    for (const offset of worldCopyOffsets(state, view)) {
        ctx.save();
        if (!projection.wrapsHorizontally) {
            // Куски полигонов за линией перемены даты отсекает контур карты.
            tracePath(ctx, projection.outline.map(([x, y]) => toCanvas(state, view, x + offset, y)));
            ctx.clip();
        }
        drawLand(state, view, offset);
        drawGraticule(state, view, offset);
        ctx.restore();
    }

    if (!projection.wrapsHorizontally) {
        ctx.save();
        tracePath(ctx, projection.outline.map(([x, y]) => toCanvas(state, view, x, y)));
        ctx.closePath();
        ctx.strokeStyle = 'rgba(148, 163, 184, 0.32)';
        ctx.lineWidth = 1;
        ctx.stroke();
        ctx.restore();
    }

    drawParticipants(state, view);
    drawUserLocation(state, view);
    drawFocusedParticipant(state, view);
}

function drawOcean(state, view) {
    const { ctx, width, projection } = state;
    ctx.save();
    ctx.fillStyle = '#101a26';
    if (projection.wrapsHorizontally) {
        // Одна полоса на всю ширину: соседние копии мира не дают шва.
        const top = toCanvas(state, view, 0, projection.yMax).y;
        const bottom = toCanvas(state, view, 0, -projection.yMax).y;
        ctx.fillRect(0, top, width, bottom - top);
    } else {
        tracePath(ctx, projection.outline.map(([x, y]) => toCanvas(state, view, x, y)));
        ctx.fill();
    }
    ctx.restore();
}

function drawLand(state, view, offset) {
    const { ctx } = state;
    ctx.save();
    ctx.fillStyle = '#1d2b3a';
    ctx.strokeStyle = 'rgba(148, 163, 184, 0.42)';
    ctx.lineWidth = 1;

    state.land.forEach((ring) => {
        const points = ring.map(([x, y]) => toCanvas(state, view, x + offset, y));
        tracePath(ctx, points);
        ctx.fill();

        // Берег обводим без рёбер вдоль линии перемены даты.
        ctx.beginPath();
        points.forEach((point, index) => {
            const onSeam = index > 0 && ring[index][2] && ring[index - 1][2];
            if (index === 0 || onSeam) {
                ctx.moveTo(point.x, point.y);
            } else {
                ctx.lineTo(point.x, point.y);
            }
        });
        ctx.stroke();
    });

    ctx.restore();
}

function drawGraticule(state, view, offset) {
    const { ctx, projection } = state;
    const step = state.zoom >= 6 ? 10 : state.zoom >= 3 ? 20 : 30;
    const toPoint = (relativeLng, lat) => {
        const [x, y] = projection.forward(relativeLng, lat);
        return toCanvas(state, view, x + offset, y);
    };
    const regular = 'rgba(148, 163, 184, 0.18)';
    // Экватор и нулевой меридиан выделены чуть ярче.
    const accent = 'rgba(148, 163, 184, 0.32)';

    ctx.save();
    ctx.lineWidth = 1;

    // Параллели в обеих проекциях — горизонтальные отрезки.
    for (let lat = 0; lat < 90; lat += step) {
        for (const parallel of lat === 0 ? [0] : [lat, -lat]) {
            ctx.strokeStyle = parallel === 0 ? accent : regular;
            tracePath(ctx, [toPoint(-180, parallel), toPoint(180, parallel)]);
            ctx.stroke();
        }
    }

    // Меридианы идут по настоящим долготам, а рисуются относительно центрального.
    for (let lng = -180; lng < 180; lng += step) {
        const relativeLng = wrapLng(lng - state.centralMeridian);
        if (!projection.wrapsHorizontally && Math.abs(relativeLng) === 180) {
            continue; // совпадает с контуром карты
        }
        const points = [];
        for (let lat = -90; lat <= 90; lat += DENSIFY_STEP) {
            points.push(toPoint(relativeLng, lat));
        }
        ctx.strokeStyle = lng === 0 ? accent : regular;
        tracePath(ctx, points);
        ctx.stroke();
    }

    ctx.restore();
}

function tracePath(ctx, points) {
    ctx.beginPath();
    points.forEach((point, index) => {
        if (index === 0) {
            ctx.moveTo(point.x, point.y);
        } else {
            ctx.lineTo(point.x, point.y);
        }
    });
}

function isOnScreen(state, point, margin) {
    return point.x >= -margin && point.x <= state.width + margin
        && point.y >= -margin && point.y <= state.height + margin;
}

function drawParticipants(state, view) {
    const { ctx } = state;
    state.participants.forEach((participant, index) => {
        if (!isFinitePair(participant.Latitude, participant.Longitude)) {
            return;
        }
        const radius = state.hoveredParticipantIndex === index ? 11 : 9;
        const initial = String(participant.Name || '?').charAt(0).toUpperCase();
        const positions = projectToCanvasCopies(state, view, participant.Latitude, participant.Longitude);
        positions.filter((point) => isOnScreen(state, point, 40)).forEach(({ x, y }) => {
            ctx.save();
            ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
            ctx.shadowBlur = 4;
            ctx.beginPath();
            ctx.arc(x, y, radius, 0, Math.PI * 2);
            ctx.fillStyle = '#24dce7';
            ctx.fill();
            ctx.lineWidth = 2;
            ctx.strokeStyle = '#0e1013';
            ctx.stroke();
            ctx.restore();

            ctx.save();
            ctx.fillStyle = '#0e1013';
            ctx.font = 'bold 11px Arial';
            ctx.textAlign = 'center';
            ctx.textBaseline = 'middle';
            ctx.fillText(initial, x, y + 0.5);
            ctx.restore();
        });
    });
}

function drawUserLocation(state, view) {
    if (!state.userLocation) {
        return;
    }
    const { ctx } = state;
    const positions = projectToCanvasCopies(state, view, state.userLocation.latitude, state.userLocation.longitude);
    positions.forEach(({ x, y }) => {
        ctx.save();
        ctx.beginPath();
        ctx.arc(x, y, 8, 0, Math.PI * 2);
        ctx.fillStyle = '#68f2a0';
        ctx.fill();
        ctx.lineWidth = 2;
        ctx.strokeStyle = '#0e1013';
        ctx.stroke();
        ctx.beginPath();
        ctx.arc(x, y, 3, 0, Math.PI * 2);
        ctx.fillStyle = '#0e1013';
        ctx.fill();
        ctx.restore();
    });
}

function drawFocusedParticipant(state, view) {
    if (!state.focused) {
        return;
    }
    const { ctx } = state;
    const positions = projectToCanvasCopies(state, view, state.focused.lat, state.focused.lng);
    positions.forEach(({ x, y }) => {
        ctx.save();
        ctx.beginPath();
        ctx.arc(x, y, 14, 0, Math.PI * 2);
        ctx.strokeStyle = '#ffcf5a';
        ctx.lineWidth = 3;
        ctx.stroke();
        ctx.restore();
    });
}

function updateHover(state, event) {
    const rect = state.canvas.getBoundingClientRect();
    const pointer = {
        x: event.clientX - rect.left,
        y: event.clientY - rect.top
    };
    const index = findParticipantAt(state, pointer);
    if (index !== state.hoveredParticipantIndex) {
        state.hoveredParticipantIndex = index;
        drawScene(state);
    }
    if (index >= 0) {
        showTooltip(state, state.participants[index], pointer);
        state.canvas.style.cursor = 'pointer';
    } else {
        hideTooltip(state);
        state.canvas.style.cursor = state.dragging ? 'grabbing' : 'grab';
    }
}

function findParticipantAt(state, pointer) {
    const view = getView(state);
    for (let i = state.participants.length - 1; i >= 0; i -= 1) {
        const participant = state.participants[i];
        if (!isFinitePair(participant.Latitude, participant.Longitude)) {
            continue;
        }
        const positions = projectToCanvasCopies(state, view, participant.Latitude, participant.Longitude);
        const hit = positions.some(({ x, y }) => {
            const dx = pointer.x - x;
            const dy = pointer.y - y;
            return dx * dx + dy * dy <= 13 * 13;
        });
        if (hit) {
            return i;
        }
    }
    return -1;
}

function showTooltip(state, participant, pointer) {
    const { tooltip, container } = state;
    tooltip.innerHTML = renderTooltipContent(participant);
    tooltip.style.display = 'block';
    const containerRect = container.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();
    let left = pointer.x + 14;
    let top = pointer.y + 14;
    if (left + tooltipRect.width > containerRect.width) {
        left = pointer.x - tooltipRect.width - 14;
    }
    if (top + tooltipRect.height > containerRect.height) {
        top = pointer.y - tooltipRect.height - 14;
    }
    tooltip.style.left = `${Math.max(8, left)}px`;
    tooltip.style.top = `${Math.max(8, top)}px`;
}

function hideTooltip(state) {
    state.tooltip.style.display = 'none';
}

function renderTooltipContent(participant) {
    const rows = [];
    const addRow = (label, value) => {
        if (value !== null && value !== undefined && value !== '') {
            rows.push(`
                <p style="display: grid; grid-template-columns: 122px 1fr; gap: 10px; margin: 7px 0; color: #b8c2d0; line-height: 1.45;">
                    <strong style="color: #f4f7fb;">${label}</strong>
                    <span>${escapeHtml(value)}</span>
                </p>
            `);
        }
    };

    if (participant.Email) {
        addRow('📧 Email:', participant.Email);
    }
    if (participant.Address) {
        addRow('📍 Адрес:', participant.Address);
    }
    const locationParts = [];
    if (participant.City) {
        locationParts.push(participant.City);
    }
    if (participant.Country) {
        locationParts.push(participant.Country);
    }
    if (locationParts.length > 0) {
        addRow('🌍 Местоположение:', locationParts.join(', '));
    } else if (participant.Location) {
        addRow('🌍 Местоположение:', participant.Location);
    }
    if (isFinitePair(participant.Latitude, participant.Longitude)) {
        addRow('🗺️ Координаты:', `${participant.Latitude.toFixed(4)}, ${participant.Longitude.toFixed(4)}`);
    }
    addRow('📅 Регистрация:', formatMapDate(participant.Timestamp || participant.RegisteredAt));
    if (participant.Skills) {
        addRow('🛠 Навыки:', participant.Skills);
    }
    if (participant.LifeGoals) {
        addRow('🎯 Цели:', participant.LifeGoals);
    }
    if (participant.Message) {
        addRow('💬 Сообщение:', participant.Message);
    }

    return `
        <div class="zgl-info-window" style="background: #171a1f; border: 1px solid rgba(148, 163, 184, 0.28); border-radius: 8px; box-shadow: 0 18px 50px rgba(0, 0, 0, 0.38); color: #f4f7fb; font-family: Arial, sans-serif; max-width: 320px; padding: 12px;">
            <h4 style="margin: 0 0 10px 0; color: #24dce7; font-size: 16px;">${escapeHtml(participant.Name || 'Участник')}</h4>
            ${rows.join('')}
        </div>
    `;
}

function isFinitePair(a, b) {
    return Number.isFinite(a) && Number.isFinite(b);
}

function escapeHtml(value) {
    const text = value === null || value === undefined ? '' : String(value);
    const entities = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    };
    return text.replace(/[&<>"']/g, (character) => entities[character]);
}

function formatMapDate(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString();
}

// SVG-«маркеры» из 3D-версии. Используются в data: URL для пользовательских
// фокусных подсветок и для всплывающих иконок участника.
function createMarkerDataUrl(svg) {
    return `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`;
}

function createCustomMarker(name) {
    const initial = escapeHtml(String(name || '?').charAt(0).toUpperCase());
    return `
        <svg width="40" height="40" viewBox="0 0 40 40" xmlns="http://www.w3.org/2000/svg">
            <circle cx="20" cy="20" r="18" fill="#24dce7" stroke="#171a1f" stroke-width="2"/>
            <text x="20" y="26" text-anchor="middle" fill="#0e1013" font-family="Arial" font-size="14" font-weight="bold">${initial}</text>
        </svg>
    `;
}

function createUserLocationMarker() {
    return `
        <svg width="30" height="30" viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
            <circle cx="15" cy="15" r="12" fill="#68f2a0" stroke="#171a1f" stroke-width="2"/>
            <circle cx="15" cy="15" r="5" fill="#0e1013"/>
            <circle cx="15" cy="15" r="2.25" fill="#68f2a0"/>
        </svg>
    `;
}

function createFocusMarker(name) {
    const initial = escapeHtml(String(name || '?').charAt(0).toUpperCase());
    return `
        <svg width="36" height="44" viewBox="0 0 36 44" xmlns="http://www.w3.org/2000/svg">
            <path d="M18 42C14 35.5 6 29.5 6 18C6 11.4 11.4 6 18 6s12 5.4 12 12c0 11.5-8 17.5-12 24Z" fill="#ffcf5a" stroke="#171a1f" stroke-width="2"/>
            <circle cx="18" cy="18" r="7" fill="#171a1f"/>
            <text x="18" y="22.5" text-anchor="middle" fill="#ffcf5a" font-family="Arial" font-size="10" font-weight="bold">${initial}</text>
        </svg>
    `;
}

// Несколько упрощённых полигонов основных континентов и крупных островов.
// Этого достаточно, чтобы карта читалась как «карта мира», и при этом не
// требуется тянуть тяжёлый geojson. Все координаты — пары [долгота, широта].
function buildWorldLandPaths() {
    return [
        // Африка
        [
            [-17, 14], [-15, 21], [-6, 35], [10, 37], [11, 33], [25, 32], [34, 31],
            [37, 22], [43, 12], [51, 11], [43, 0], [40, -9], [40, -16], [35, -22],
            [32, -28], [22, -34], [18, -34], [14, -22], [13, -10], [9, -2], [9, 4],
            [3, 5], [-4, 4], [-9, 6], [-13, 12], [-17, 14]
        ],
        // Европа (упрощённо: Иберия → Скандинавия)
        [
            [-9, 36], [-9, 43], [-5, 48], [-1, 50], [4, 51], [8, 54], [9, 58],
            [10, 64], [16, 69], [27, 71], [30, 65], [27, 60], [22, 56], [18, 49],
            [22, 46], [27, 45], [32, 44], [28, 41], [22, 39], [14, 38], [9, 41],
            [4, 44], [0, 41], [-3, 36], [-9, 36]
        ],
        // Азия (включая Аравийский полуостров, Индийский субконтинент, Юго-Восточную Азию)
        [
            [30, 65], [40, 65], [50, 70], [70, 75], [90, 76], [110, 75], [130, 72],
            [140, 73], [150, 70], [160, 65], [175, 65], [177, 60], [170, 60],
            [160, 58], [142, 53], [135, 45], [129, 43], [128, 38], [125, 32],
            [120, 22], [110, 21], [108, 18], [109, 11], [104, 1], [97, -3],
            [95, 5], [99, 13], [95, 16], [92, 21], [90, 22], [88, 21], [82, 8],
            [77, 8], [73, 16], [69, 22], [63, 25], [57, 20], [55, 25], [50, 25],
            [49, 16], [44, 12], [40, 16], [35, 26], [34, 31], [37, 38], [42, 41],
            [50, 41], [56, 39], [62, 40], [62, 50], [56, 55], [50, 60], [40, 63],
            [30, 65]
        ],
        // Северная Америка
        [
            [-167, 65], [-160, 70], [-150, 71], [-140, 70], [-130, 70], [-115, 73],
            [-100, 75], [-90, 76], [-80, 76], [-72, 78], [-66, 75], [-60, 70],
            [-55, 60], [-65, 52], [-70, 46], [-77, 43], [-82, 35], [-80, 30],
            [-83, 26], [-90, 27], [-95, 28], [-97, 26], [-105, 22], [-117, 32],
            [-124, 40], [-124, 48], [-130, 55], [-140, 58], [-150, 60], [-160, 60],
            [-167, 65]
        ],
        // Центральная Америка
        [
            [-90, 16], [-85, 15], [-82, 10], [-79, 9], [-77, 8], [-83, 8], [-90, 12], [-90, 16]
        ],
        // Южная Америка
        [
            [-81, 12], [-71, 12], [-62, 9], [-52, 5], [-50, 0], [-45, -5], [-42, -10],
            [-38, -12], [-39, -18], [-41, -23], [-48, -28], [-55, -34], [-57, -38],
            [-64, -41], [-66, -45], [-69, -51], [-72, -54], [-73, -47], [-74, -41],
            [-72, -34], [-71, -22], [-69, -15], [-72, -10], [-78, -6], [-81, 0], [-81, 12]
        ],
        // Австралия
        [
            [113, -22], [114, -32], [122, -34], [130, -32], [137, -35], [141, -38],
            [148, -38], [153, -28], [146, -19], [141, -12], [135, -12], [128, -14],
            [122, -17], [115, -19], [113, -22]
        ],
        // Гренландия
        [
            [-50, 60], [-45, 65], [-32, 70], [-20, 75], [-20, 82], [-35, 83], [-50, 80],
            [-55, 73], [-55, 65], [-50, 60]
        ],
        // Великобритания
        [
            [-5, 50], [-6, 55], [-4, 58], [-2, 58], [0, 53], [-2, 51], [-5, 50]
        ],
        // Мадагаскар
        [
            [43, -12], [49, -16], [50, -22], [47, -25], [43, -20], [43, -12]
        ],
        // Япония (упрощённо)
        [
            [130, 31], [131, 34], [135, 35], [139, 36], [142, 40], [144, 43], [142, 45],
            [138, 42], [135, 38], [132, 34], [130, 31]
        ],
        // Новая Зеландия (Северный + Южный острова объединены)
        [
            [172, -34], [175, -36], [178, -38], [177, -41], [174, -41], [172, -44],
            [168, -46], [166, -45], [170, -42], [171, -39], [172, -34]
        ],
        // Антарктида (упрощённая полоса, чтобы не загромождать карту)
        [
            [-180, -65], [180, -65], [180, -85], [-180, -85], [-180, -65]
        ]
    ];
}
