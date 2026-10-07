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
// Цвет маркера, если у точки нет своего (совпадает с GeoPointPalette.DefaultColor).
const DEFAULT_MARKER_COLOR = '#24dce7';
// Тот же шаблон, что GeoPointPalette.IsCssColor: цвет из данных попадает и в canvas, и в style подсказки.
const CSS_COLOR_PATTERN = /^(#[0-9a-fA-F]{3,4}|#[0-9a-fA-F]{6}|#[0-9a-fA-F]{8}|[a-zA-Z]{3,30}|(rgb|rgba|hsl|hsla)\([0-9.,%\s/deg]+\))$/;
const MIN_ZOOM = 1;
const MAX_ZOOM = 20;
// Шаг приближения колесом и кнопками.
const ZOOM_STEP = 1.25;

// Размеры маркеров в CSS-пикселях.
const POINT_RADIUS = 9;
const POINT_HOVER_RADIUS = 11;
// Попасть по маркеру можно чуть за его краем.
const HIT_SLOP = 4;
// Значок группы растёт с числом точек от CLUSTER_MIN_RADIUS до CLUSTER_MAX_RADIUS.
const CLUSTER_MIN_RADIUS = 11;
const CLUSTER_MAX_RADIUS = 21;
// Наименьший просвет между краями соседних значков: ближе — и они сливаются в группу.
const CLUSTER_GAP = 6;
// По умолчанию в группу сливаются только маркеры, которые иначе соприкоснулись бы.
const DEFAULT_CLUSTER_RADIUS = 2 * POINT_RADIUS + CLUSTER_GAP;
const MAX_CLUSTER_RADIUS = 200;
// Слитая группа может налезть на соседа, поэтому слияние повторяется, пока
// есть что сливать. Обычно хватает двух-трёх проходов.
const MAX_CLUSTER_PASSES = 12;
// Сколько заголовков показывает подсказка группы.
const CLUSTER_TOOLTIP_TITLES = 8;
// Раскрытая «веером» группа: до SPIDER_CIRCLE_MAX точек — по кругу, больше — по спирали.
const SPIDER_CIRCLE_MAX = 8;
const SPIDER_FOOT_SPACING = 26;
const SPIDER_MIN_LEG = 30;
const SPIDER_SPIRAL_START = 24;
// Подписи свойств участника в подсказке (ключи ParticipantPointProperties).
const PROPERTY_LABELS = {
    email: '📧 Email:',
    address: '📍 Адрес:',
    location: '🌍 Местоположение:',
    socialMedia: '🔗 Соцсети:',
    skills: '🛠 Навыки:',
    lifeGoals: '🎯 Цели:',
    discord: 'Discord:',
    telegram: 'Telegram:',
    vk: 'VK:',
    website: 'Сайт:',
    registeredAt: '📅 Регистрация:'
};
// Город и страна выводятся одной строкой «Местоположение».
const LOCATION_KEYS = ['city', 'country'];

window.setDotNetHelper = (helper) => {
    dotNetHelper = helper;
};

// options: { projection: 'equalEarth' | 'equirectangular', centralMeridian: градусы,
// clustering: группировать близкие маркеры (по умолчанию true), clusterRadius: пиксели }.
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
        centralMeridian: numberOrDefault(options?.centralMeridian, 0),
        clustering: normalizeClusterOptions(options),
        // Обработчик кликов этой карты. Общий setDotNetHelper остаётся для прежнего кода,
        // но с ним клик по любой карте страницы уходил последней инициализированной.
        dotNetHelper: options?.dotNetHelper ?? null
    });

    mapInstances.set(targetId, instance);
    instance.draw();
    console.log(`Карта сообщества инициализирована (${targetId}, ${instance.projection})`);
};

// points: массив точек { id, latitude, longitude, title, description, category, color,
// icon, url, properties } или JSON-строка с ним.
window.loadPointsOnMap = (pointsJson, containerId) => {
    const instance = resolveInstance(containerId);
    if (!instance) {
        return;
    }

    try {
        const points = Array.isArray(pointsJson) ? pointsJson : JSON.parse(pointsJson);
        instance.setPoints(points.map(normalizePoint).filter(Boolean));
    } catch (error) {
        console.error('Ошибка загрузки точек на карту:', error);
    }
};

// Прежний формат: участники ({ Id, Name, Latitude, Longitude, Email, … }).
window.loadParticipantsOnMap = (participantsJson, containerId) => {
    const instance = resolveInstance(containerId);
    if (!instance) {
        return;
    }

    try {
        const participants = Array.isArray(participantsJson)
            ? participantsJson
            : JSON.parse(participantsJson);
        instance.setPoints(participants.map(participantToPoint).filter(Boolean));
    } catch (error) {
        console.error('Ошибка загрузки участников на карту:', error);
    }
};

// options: { clustering: true | false, clusterRadius: пиксели }. Поля, которых
// нет, остаются прежними.
window.setCommunityMapClustering = (options, containerId) => {
    resolveInstance(containerId)?.setClustering(options);
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
    setCommunityMapClustering: window.setCommunityMapClustering,
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
    canvas.setAttribute('aria-label', 'Карта с точками');
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
        points: [],
        // Растёт при каждой смене точек: по нему узнаём, что группы устарели.
        pointsVersion: 0,
        clustering: initialState.clustering,
        // Группы маркеров для текущего масштаба и точка, раскрытая веером.
        groupCache: null,
        spider: null,
        // Маркеры, нарисованные в последнем кадре, сверху вниз — для попадания мышью.
        hitTargets: [],
        dotNetHelper: initialState.dotNetHelper,
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
        // Карту сдвинули: отпускание кнопки — не клик по маркеру.
        dragMoved: false,
        // Ключ маркера под указателем
        hoveredKey: null,
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
        const factor = direction > 0 ? ZOOM_STEP : 1 / ZOOM_STEP;
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
        state.dragMoved = false;
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
            if (Math.hypot(event.clientX - state.dragStart.x, event.clientY - state.dragStart.y) > 3) {
                state.dragMoved = true;
            }
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
        if (state.dragMoved) {
            state.dragMoved = false;
            return;
        }
        const rect = canvas.getBoundingClientRect();
        const pointer = {
            x: event.clientX - rect.left,
            y: event.clientY - rect.top
        };
        const target = findTargetAt(state, pointer);
        if (!target) {
            // Клик мимо маркеров сворачивает раскрытую группу.
            if (state.spider) {
                state.spider = null;
                draw();
            }
            return;
        }
        if (target.type === 'cluster') {
            expandCluster(state, target.group);
            state.hoveredKey = null;
            hideTooltip(state);
            draw();
            return;
        }
        notifyPointClick(state, state.points[target.pointIndex]);
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
        zoomAt(state, ZOOM_STEP, { x: state.width / 2, y: state.height / 2 });
        draw();
    });
    controls.zoomOut.addEventListener('click', () => {
        zoomAt(state, 1 / ZOOM_STEP, { x: state.width / 2, y: state.height / 2 });
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
        setPoints(list) {
            state.points = Array.isArray(list) ? list.slice() : [];
            state.pointsVersion += 1;
            state.spider = null;
            state.hoveredKey = null;
            hideTooltip(state);
            draw();
        },
        setClustering(options) {
            state.clustering = normalizeClusterOptions(options, state.clustering);
            state.spider = null;
            hideTooltip(state);
            draw();
        },
        setParticipants(list) {
            this.setPoints((Array.isArray(list) ? list : []).map(participantToPoint).filter(Boolean));
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
    return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, fallback));
}

// Настройки группировки из options; поля, которых нет, берутся из current.
function normalizeClusterOptions(options, current) {
    const base = current ?? { enabled: true, radius: DEFAULT_CLUSTER_RADIUS };
    const enabled = typeof options?.clustering === 'boolean' ? options.clustering : base.enabled;
    const radius = Number(options?.clusterRadius);
    return {
        enabled,
        radius: Number.isFinite(radius) && options?.clusterRadius !== null
            ? Math.min(MAX_CLUSTER_RADIUS, Math.max(0, radius))
            : base.radius
    };
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
    return projectedToCanvasCopies(state, view, x, y);
}

function projectedToCanvasCopies(state, view, x, y) {
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

// --- Группировка маркеров -----------------------------------------------
// Маркеры, которые на экране налезли бы друг на друга, сливаются в группу со
// счётчиком. Группы считаются в пикселях текущего масштаба, но от сдвига карты
// не зависят: при перетаскивании они не пересчитываются и не «прыгают».

// Группы для текущего масштаба: { key, count, members, x, y }, где members —
// индексы state.points, а x, y — центр группы в координатах проекции.
function getMarkerGroups(state, view) {
    const { clustering } = state;
    const focus = state.focused ? `${state.focused.lat},${state.focused.lng}` : '';
    const cacheKey = `${state.pointsVersion}|${view.scale}|${clustering.enabled}|${clustering.radius}|${focus}`;
    if (state.groupCache?.key === cacheKey) {
        return state.groupCache.groups;
    }

    const projected = state.points.map((point) => projectPoint(state, point.latitude, point.longitude));
    const single = (index) => ({
        key: String(index),
        count: 1,
        members: [index],
        x: projected[index][0],
        y: projected[index][1]
    });

    let groups;
    if (!clustering.enabled) {
        groups = state.points.map((_, index) => single(index));
    } else {
        // Точку, на которой сфокусирована карта, не прячем в группу.
        const free = [];
        const pinned = [];
        state.points.forEach((point, index) => (isFocusedPoint(state, point) ? pinned : free).push(index));
        const period = state.projection.wrapsHorizontally ? state.projection.period * view.scale : 0;
        const clusters = clusterMarkers(
            free.map((index) => ({ x: projected[index][0] * view.scale, y: projected[index][1] * view.scale })),
            { radius: clustering.radius, period });
        groups = clusters.map((cluster) => {
            const members = cluster.members.map((index) => free[index]);
            if (cluster.count === 1) {
                return single(members[0]);
            }
            return {
                key: members.join(','),
                count: cluster.count,
                members,
                x: cluster.x / view.scale,
                y: cluster.y / view.scale
            };
        }).concat(pinned.map(single));
    }

    state.groupCache = { key: cacheKey, groups };
    return groups;
}

function isFocusedPoint(state, point) {
    return Boolean(state.focused)
        && Math.abs(point.latitude - state.focused.lat) < 1e-9
        && Math.abs(point.longitude - state.focused.lng) < 1e-9;
}

// Ключ маркера для подсветки: точка — по индексу, группа — по составу.
function markerKey(group) {
    return group.count === 1 ? `p:${group.members[0]}` : `c:${group.key}`;
}

// Радиус значка группы из count точек; одиночная точка — обычный маркер.
function clusterBadgeRadius(count) {
    if (count <= 1) {
        return POINT_RADIUS;
    }
    return Math.min(CLUSTER_MAX_RADIUS, CLUSTER_MIN_RADIUS + 2.5 * Math.log2(count));
}

// items — маркеры { x, y } в пикселях; options.radius — расстояние между
// центрами, ближе которого маркеры сливаются, options.period — период по x
// у склеивающейся проекции (0 — нет). Две группы сливаются, если их центры
// ближе radius или их значки не помещаются рядом с просветом CLUSTER_GAP.
// Возвращает группы { x, y, count, members } с индексами items по возрастанию.
function clusterMarkers(items, options = {}) {
    const radius = Math.max(0, Number(options.radius) || 0);
    const period = options.period > 0 ? options.period : 0;
    const separation = (a, b) =>
        Math.max(radius, clusterBadgeRadius(a.count) + clusterBadgeRadius(b.count) + CLUSTER_GAP);
    // Ячейка сетки не меньше наибольшего порога: соседей ищем в соседних ячейках.
    const cellSize = Math.max(radius, 2 * CLUSTER_MAX_RADIUS + CLUSTER_GAP);

    let groups = items.map((item, index) => ({
        x: period ? wrapPeriodic(item.x, period) : item.x,
        y: item.y,
        count: 1,
        members: [index]
    }));
    for (let pass = 0; pass < MAX_CLUSTER_PASSES; pass += 1) {
        const merged = mergeNearbyGroups(groups, separation, cellSize, period);
        if (merged.length === groups.length) {
            break;
        }
        groups = merged;
    }
    groups.forEach((group) => group.members.sort((a, b) => a - b));
    return groups;
}

// Один жадный проход: каждая ещё не занятая группа (крупные — первыми)
// забирает близких соседей, а её центр становится средним по всем точкам.
function mergeNearbyGroups(groups, separation, cellSize, period) {
    const grid = buildClusterGrid(groups, cellSize, period);
    const order = groups.map((_, index) => index)
        .sort((a, b) => groups[b].count - groups[a].count || a - b);
    const taken = new Uint8Array(groups.length);
    const result = [];

    for (const index of order) {
        if (taken[index]) {
            continue;
        }
        taken[index] = 1;
        const seed = groups[index];
        let count = seed.count;
        let shiftX = 0;
        let shiftY = 0;
        let members = null;

        grid.forEachNear(seed, (otherIndex) => {
            if (taken[otherIndex]) {
                return;
            }
            const other = groups[otherIndex];
            const dx = period ? wrapPeriodic(other.x - seed.x, period) : other.x - seed.x;
            const dy = other.y - seed.y;
            const limit = separation(seed, other);
            if (dx * dx + dy * dy >= limit * limit) {
                return;
            }
            taken[otherIndex] = 1;
            shiftX += dx * other.count;
            shiftY += dy * other.count;
            count += other.count;
            members ??= seed.members.slice();
            other.members.forEach((member) => members.push(member));
        });

        if (!members) {
            result.push(seed);
            continue;
        }
        const x = seed.x + shiftX / count;
        result.push({ x: period ? wrapPeriodic(x, period) : x, y: seed.y + shiftY / count, count, members });
    }
    return result;
}

// Сетка с ячейками cellSize: соседи группы — в её ячейке и восьми вокруг.
// У склеивающейся проекции столбцы замкнуты в кольцо.
function buildClusterGrid(groups, cellSize, period) {
    const columns = period ? Math.max(1, Math.floor(period / cellSize)) : 0;
    const cellWidth = period ? period / columns : cellSize;
    const columnOf = (x) => (period
        ? Math.min(columns - 1, Math.max(0, Math.floor((x + period / 2) / cellWidth)))
        : Math.floor(x / cellWidth));
    const rowOf = (y) => Math.floor(y / cellSize);
    const cells = new Map();
    groups.forEach((group, index) => {
        const key = `${columnOf(group.x)}:${rowOf(group.y)}`;
        const cell = cells.get(key);
        if (cell) {
            cell.push(index);
        } else {
            cells.set(key, [index]);
        }
    });

    return {
        forEachNear(group, visit) {
            const column = columnOf(group.x);
            const row = rowOf(group.y);
            const nearColumns = new Set([column - 1, column, column + 1]
                .map((value) => (period ? ((value % columns) + columns) % columns : value)));
            nearColumns.forEach((nearColumn) => {
                for (let nearRow = row - 1; nearRow <= row + 1; nearRow += 1) {
                    cells.get(`${nearColumn}:${nearRow}`)?.forEach(visit);
                }
            });
        }
    };
}

// Доли цветов в группе в порядке первого появления: [{ color, count }].
function clusterColorShares(members, points) {
    const shares = new Map();
    members.forEach((index) => {
        const color = markerColor(points[index]);
        shares.set(color, (shares.get(color) ?? 0) + 1);
    });
    return Array.from(shares, ([color, count]) => ({ color, count }));
}

function formatClusterCount(count) {
    if (count < 1000) {
        return String(count);
    }
    if (count < 10000) {
        return `${(Math.floor(count / 100) / 10).toString()}k`;
    }
    return `${Math.floor(count / 1000)}k`;
}

// Смещения точек раскрытой группы от её центра, в пикселях. Соседние точки
// отстоят друг от друга не меньше чем на SPIDER_FOOT_SPACING.
function spiderOffsets(count) {
    const offsets = [];
    if (count <= SPIDER_CIRCLE_MAX) {
        // Хорда между соседями на круге не короче SPIDER_FOOT_SPACING.
        const leg = Math.max(SPIDER_MIN_LEG, SPIDER_FOOT_SPACING / (2 * Math.sin(Math.PI / Math.max(count, 2))));
        for (let i = 0; i < count; i += 1) {
            const angle = -Math.PI / 2 + (2 * Math.PI * i) / count;
            offsets.push({ x: leg * Math.cos(angle), y: leg * Math.sin(angle) });
        }
        return offsets;
    }
    // Спираль Архимеда r = r0 + bθ: витки отстоят друг от друга на тот же шаг,
    // что и соседние точки на витке.
    const turnStep = SPIDER_FOOT_SPACING / (2 * Math.PI);
    let angle = 0;
    for (let i = 0; i < count; i += 1) {
        const leg = SPIDER_SPIRAL_START + turnStep * angle;
        offsets.push({ x: leg * Math.cos(angle), y: leg * Math.sin(angle) });
        angle += SPIDER_FOOT_SPACING / leg;
    }
    return offsets;
}

// Что сделает клик по группе. Если точки разойдутся при приближении — карта
// приблизится шагами колеса ровно настолько, чтобы группа распалась, и встанет
// по центру группы ({ mode: 'zoom', zoom, centerX, centerY }). Если не разойдутся
// и на наибольшем приближении (одинаковые или почти одинаковые координаты) —
// группа раскроется веером ({ mode: 'spider' }). Результат хранится в группе:
// группы пересчитываются при любой смене масштаба.
function clusterExpansion(state, group) {
    if (!group.expansion) {
        group.expansion = computeClusterExpansion(state, group);
    }
    return group.expansion;
}

function computeClusterExpansion(state, group) {
    const { projection } = state;
    // Координаты точек группы без разрыва на линии перемены даты.
    const members = group.members.map((index) => {
        const point = state.points[index];
        const [x, y] = projectPoint(state, point.latitude, point.longitude);
        return {
            x: projection.wrapsHorizontally ? group.x + wrapPeriodic(x - group.x, projection.period) : x,
            y
        };
    });
    const xs = members.map(({ x }) => x);
    const ys = members.map(({ y }) => y);
    const centerX = (Math.min(...xs) + Math.max(...xs)) / 2;
    const centerY = (Math.min(...ys) + Math.max(...ys)) / 2;

    // Масштаб пропорционален приближению. Ступени — как у колеса, последняя —
    // наибольшее приближение.
    const scalePerZoom = getView(state).scale / state.zoom;
    const zooms = [];
    for (let zoom = state.zoom * ZOOM_STEP; zoom < MAX_ZOOM; zoom *= ZOOM_STEP) {
        zooms.push(zoom);
    }
    zooms.push(MAX_ZOOM);
    const splits = (zoom) => {
        const scale = scalePerZoom * zoom;
        const parts = clusterMarkers(
            members.map(({ x, y }) => ({ x: x * scale, y: y * scale })),
            { radius: state.clustering.radius });
        return parts.length > 1;
    };

    if (state.zoom >= MAX_ZOOM || !splits(MAX_ZOOM)) {
        return { mode: 'spider' };
    }
    // Чем ближе, тем дальше точки друг от друга: ищем первую ступень делением пополам.
    let low = 0;
    let high = zooms.length - 1;
    while (low < high) {
        const middle = Math.floor((low + high) / 2);
        if (splits(zooms[middle])) {
            high = middle;
        } else {
            low = middle + 1;
        }
    }
    return { mode: 'zoom', zoom: zooms[low], centerX, centerY };
}

function expandCluster(state, group) {
    const expansion = clusterExpansion(state, group);
    if (expansion.mode === 'spider') {
        state.spider = { key: group.key };
        return;
    }
    state.spider = null;
    state.zoom = expansion.zoom;
    state.centerX = expansion.centerX;
    state.centerY = expansion.centerY;
    commitView(state);
}

function renderClusterTooltip(group, points, mode) {
    const shown = group.members.slice(0, CLUSTER_TOOLTIP_TITLES);
    const rows = shown.map((index) => {
        const point = points[index];
        return `
            <li style="display: flex; align-items: center; gap: 8px; margin: 5px 0; color: #b8c2d0; line-height: 1.35;">
                <span style="flex: none; width: 10px; height: 10px; border-radius: 50%; background: ${markerColor(point)};"></span>
                <span>${escapeHtml(point.title || 'Точка')}</span>
            </li>
        `;
    });
    const rest = group.count - shown.length;
    const hint = mode === 'spider' ? 'Нажмите, чтобы раскрыть' : 'Нажмите, чтобы приблизить';

    return `
        <div class="zgl-info-window" style="background: #171a1f; border: 1px solid rgba(148, 163, 184, 0.28); border-radius: 8px; box-shadow: 0 18px 50px rgba(0, 0, 0, 0.38); color: #f4f7fb; font-family: Arial, sans-serif; max-width: 320px; padding: 12px;">
            <h4 style="margin: 0 0 8px 0; color: #24dce7; font-size: 16px;">${group.count} ${pluralizePoints(group.count)}</h4>
            <ul style="list-style: none; margin: 0; padding: 0;">${rows.join('')}</ul>
            ${rest > 0 ? `<p style="margin: 6px 0 0 0; color: #b8c2d0;">и ещё ${rest}</p>` : ''}
            <p style="margin: 10px 0 0 0; color: #8a94a6; font-size: 12px;">${hint}</p>
        </div>
    `;
}

function pluralizePoints(count) {
    const tens = count % 100;
    const units = count % 10;
    if (tens >= 11 && tens <= 14) {
        return 'точек';
    }
    if (units === 1) {
        return 'точка';
    }
    if (units >= 2 && units <= 4) {
        return 'точки';
    }
    return 'точек';
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

    drawMarkers(state, view);
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

// Рисует точки и группы точек и запоминает, где они легли, для попадания мышью.
function drawMarkers(state, view) {
    const { ctx } = state;
    const groups = getMarkerGroups(state, view);
    const targets = [];
    let spiderGroup = null;

    groups.forEach((group) => {
        if (state.spider && group.key === state.spider.key) {
            spiderGroup = group;
            return;
        }
        const single = group.count === 1;
        const key = markerKey(group);
        const hovered = state.hoveredKey === key;
        const radius = single
            ? (hovered ? POINT_HOVER_RADIUS : POINT_RADIUS)
            : clusterBadgeRadius(group.count) + (hovered ? 2 : 0);
        projectedToCanvasCopies(state, view, group.x, group.y)
            .filter((position) => isOnScreen(state, position, 40))
            .forEach(({ x, y }) => {
                if (single) {
                    drawPointMarker(ctx, x, y, radius, state.points[group.members[0]]);
                    targets.push({ type: 'point', key, pointIndex: group.members[0], x, y, radius: radius + HIT_SLOP });
                } else {
                    drawClusterMarker(ctx, x, y, radius, group, state.points);
                    targets.push({ type: 'cluster', key, group, x, y, radius: radius + HIT_SLOP });
                }
            });
    });

    // Раскрытую группу рисуем поверх остальных. Если такой группы больше нет
    // (сменился масштаб или данные), веер сворачивается сам.
    if (state.spider && !spiderGroup) {
        state.spider = null;
    }
    if (spiderGroup) {
        drawSpider(state, view, spiderGroup, targets);
    }

    // Последний нарисованный — сверху: проверять попадание с конца.
    state.hitTargets = targets.reverse();
}

function drawPointMarker(ctx, x, y, radius, point) {
    ctx.save();
    ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
    ctx.shadowBlur = 4;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fillStyle = markerColor(point);
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
    ctx.fillText(markerLabel(point), x, y + 0.5);
    ctx.restore();
}

// Значок группы: число точек на тёмном круге, а кольцо вокруг поделено между
// цветами точек пропорционально их числу.
function drawClusterMarker(ctx, x, y, radius, group, points) {
    ctx.save();
    ctx.shadowColor = 'rgba(0, 0, 0, 0.45)';
    ctx.shadowBlur = 4;
    ctx.beginPath();
    ctx.arc(x, y, radius, 0, Math.PI * 2);
    ctx.fillStyle = '#171a1f';
    ctx.fill();
    ctx.lineWidth = 2;
    ctx.strokeStyle = '#0e1013';
    ctx.stroke();
    ctx.restore();

    const shares = clusterColorShares(group.members, points);
    // Между долями — тёмный зазор: соседние цвета различимы и без различения цвета.
    const gap = shares.length > 1 ? 0.12 : 0;
    let angle = -Math.PI / 2;
    ctx.save();
    ctx.lineWidth = 4;
    ctx.lineCap = 'butt';
    shares.forEach(({ color, count }) => {
        const sweep = (2 * Math.PI * count) / group.count;
        ctx.beginPath();
        ctx.arc(x, y, radius - 3, angle + gap / 2, angle + Math.max(sweep - gap / 2, gap / 2 + 0.01));
        ctx.strokeStyle = color;
        ctx.stroke();
        angle += sweep;
    });
    ctx.restore();

    ctx.save();
    ctx.fillStyle = '#f4f7fb';
    ctx.font = `bold ${group.count >= 100 ? 10 : 11}px Arial`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(formatClusterCount(group.count), x, y + 0.5);
    ctx.restore();
}

// Точки раскрытой группы — веером вокруг её значка, с тонкими «ножками» к центру.
function drawSpider(state, view, group, targets) {
    const { ctx } = state;
    const offsets = spiderOffsets(group.count);
    projectedToCanvasCopies(state, view, group.x, group.y)
        .filter((position) => isOnScreen(state, position, 40 + spiderReach(offsets)))
        .forEach((center) => {
            ctx.save();
            ctx.strokeStyle = 'rgba(244, 247, 251, 0.55)';
            ctx.lineWidth = 1.5;
            offsets.forEach((offset) => {
                ctx.beginPath();
                ctx.moveTo(center.x, center.y);
                ctx.lineTo(center.x + offset.x, center.y + offset.y);
                ctx.stroke();
            });
            ctx.beginPath();
            ctx.arc(center.x, center.y, 4, 0, Math.PI * 2);
            ctx.fillStyle = '#f4f7fb';
            ctx.fill();
            ctx.restore();

            group.members.forEach((pointIndex, index) => {
                const key = `p:${pointIndex}`;
                const radius = state.hoveredKey === key ? POINT_HOVER_RADIUS : POINT_RADIUS;
                const x = center.x + offsets[index].x;
                const y = center.y + offsets[index].y;
                drawPointMarker(ctx, x, y, radius, state.points[pointIndex]);
                targets.push({ type: 'point', key, pointIndex, x, y, radius: radius + HIT_SLOP });
            });
        });
}

function spiderReach(offsets) {
    return offsets.reduce((max, { x, y }) => Math.max(max, Math.hypot(x, y)), 0);
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
    const target = findTargetAt(state, pointer);
    const key = target ? target.key : null;
    if (key !== state.hoveredKey) {
        state.hoveredKey = key;
        drawScene(state);
    }
    if (target) {
        const html = target.type === 'cluster'
            ? renderClusterTooltip(target.group, state.points, clusterExpansion(state, target.group).mode)
            : renderPointTooltip(state.points[target.pointIndex]);
        showTooltip(state, html, pointer);
        state.canvas.style.cursor = 'pointer';
    } else {
        hideTooltip(state);
        state.canvas.style.cursor = state.dragging ? 'grabbing' : 'grab';
    }
}

// Маркер под указателем по раскладке последнего кадра или null.
function findTargetAt(state, pointer) {
    return state.hitTargets.find(({ x, y, radius }) => {
        const dx = pointer.x - x;
        const dy = pointer.y - y;
        return dx * dx + dy * dy <= radius * radius;
    }) ?? null;
}

// Своя карта знает свой компонент (OnPointMarkerClick). Без него — прежний общий
// обработчик из setDotNetHelper с прежним именем метода.
function notifyPointClick(state, point) {
    if (state.dotNetHelper?.invokeMethodAsync) {
        state.dotNetHelper.invokeMethodAsync('OnPointMarkerClick', String(point.id));
    } else if (dotNetHelper?.invokeMethodAsync) {
        dotNetHelper.invokeMethodAsync('OnParticipantMarkerClick', String(point.id));
    }
}

function showTooltip(state, html, pointer) {
    const { tooltip, container } = state;
    tooltip.innerHTML = html;
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

function renderPointTooltip(point) {
    const rows = [];
    const addRow = (label, value) => {
        if (value !== null && value !== undefined && value !== '') {
            rows.push(`
                <p style="display: grid; grid-template-columns: 122px 1fr; gap: 10px; margin: 7px 0; color: #b8c2d0; line-height: 1.45;">
                    <strong style="color: #f4f7fb;">${escapeHtml(label)}</strong>
                    <span>${escapeHtml(value)}</span>
                </p>
            `);
        }
    };

    const properties = point.properties || {};
    if (point.category) {
        addRow('🏷 Категория:', point.category);
    }
    if (point.description) {
        addRow('💬 Описание:', point.description);
    }

    const location = LOCATION_KEYS.map((key) => properties[key]).filter(Boolean).join(', ');
    Object.keys(properties).forEach((key) => {
        if (LOCATION_KEYS.includes(key)) {
            return;
        }
        const value = key === 'registeredAt' ? formatMapDate(properties[key]) : properties[key];
        if (key === 'location' && location) {
            return; // вместо «location» — город и страна
        }
        addRow(PROPERTY_LABELS[key] || `${key}:`, value);
    });
    if (location) {
        addRow(PROPERTY_LABELS.location, location);
    }
    addRow('🗺️ Координаты:', `${point.latitude.toFixed(4)}, ${point.longitude.toFixed(4)}`);

    return `
        <div class="zgl-info-window" style="background: #171a1f; border: 1px solid rgba(148, 163, 184, 0.28); border-radius: 8px; box-shadow: 0 18px 50px rgba(0, 0, 0, 0.38); color: #f4f7fb; font-family: Arial, sans-serif; max-width: 320px; padding: 12px;">
            <h4 style="margin: 0 0 10px 0; color: #24dce7; font-size: 16px;">${escapeHtml(point.title || 'Точка')}</h4>
            ${rows.join('')}
        </div>
    `;
}

// Точка в едином виде; null — если координат нет.
function normalizePoint(raw) {
    if (!raw) {
        return null;
    }
    const latitude = Number(raw.latitude ?? raw.Latitude);
    const longitude = Number(raw.longitude ?? raw.Longitude);
    if (!isFinitePair(latitude, longitude)) {
        return null;
    }
    const properties = raw.properties ?? raw.Properties;
    return {
        id: String(raw.id ?? raw.Id ?? `${latitude},${longitude}`),
        latitude,
        longitude,
        title: String(raw.title ?? raw.Title ?? ''),
        description: raw.description ?? raw.Description ?? null,
        category: raw.category ?? raw.Category ?? null,
        color: raw.color ?? raw.Color ?? null,
        icon: raw.icon ?? raw.Icon ?? null,
        url: raw.url ?? raw.Url ?? null,
        properties: properties && typeof properties === 'object' ? { ...properties } : {}
    };
}

// Участник в прежнем формате (поля в PascalCase или camelCase) → точка, как
// ParticipantGeoPointExtensions.ToGeoPoint на сервере. Без координат — null.
function participantToPoint(participant) {
    if (!participant) {
        return null;
    }
    const get = (name) => participant[name] ?? participant[name.charAt(0).toLowerCase() + name.slice(1)];
    const contacts = get('SocialContacts') || {};
    const contact = (name) => contacts[name] ?? contacts[name.charAt(0).toLowerCase() + name.slice(1)];
    const properties = {};
    const set = (key, value) => {
        if (value !== null && value !== undefined && value !== '') {
            properties[key] = String(value);
        }
    };
    set('address', get('Address'));
    set('email', get('Email'));
    set('location', get('Location'));
    set('city', get('City'));
    set('country', get('Country'));
    set('socialMedia', get('SocialMedia'));
    set('lifeGoals', get('LifeGoals'));
    set('skills', get('Skills'));
    set('discord', contact('Discord'));
    set('telegram', contact('Telegram'));
    set('vk', contact('Vk'));
    set('website', contact('Website'));
    set('registeredAt', get('Timestamp') ?? get('RegisteredAt'));

    return normalizePoint({
        id: get('Id'),
        latitude: get('Latitude'),
        longitude: get('Longitude'),
        title: get('Name') ?? '',
        description: get('Message') ?? null,
        properties
    });
}

function markerColor(point) {
    const color = typeof point?.color === 'string' ? point.color.trim() : '';
    return CSS_COLOR_PATTERN.test(color) ? color : DEFAULT_MARKER_COLOR;
}

// Подпись внутри маркера: короткая иконка (символ, эмодзи) или первая буква заголовка.
function markerLabel(point) {
    const icon = typeof point.icon === 'string' ? point.icon.trim() : '';
    if (icon && graphemeCount(icon) <= 2) {
        return icon;
    }
    const first = Array.from(String(point.title || '').trim())[0];
    return first ? first.toUpperCase() : '?';
}

function graphemeCount(text) {
    if (typeof Intl !== 'undefined' && typeof Intl.Segmenter === 'function') {
        return Array.from(new Intl.Segmenter(undefined, { granularity: 'grapheme' }).segment(text)).length;
    }
    return Array.from(text).length;
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
