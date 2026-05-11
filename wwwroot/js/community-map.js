// Статичная 2D-карта сообщества на чистом Canvas.
// Использует равноугольную проекцию (equirectangular) и не зависит
// от внешних картографических API. Поддерживает примитивное приближение/отдаление
// колесом мыши или кнопками и перемещение по карте перетаскиванием.

const mapInstances = new Map();
const containerStates = new WeakMap();
let dotNetHelper = null;

const WORLD_LAND_PATHS = buildWorldLandPaths();

window.setDotNetHelper = (helper) => {
    dotNetHelper = helper;
};

window.initializeCommunityMap = (apiKey, centerLat, centerLng, zoom, containerId) => {
    // apiKey оставлен в сигнатуре для обратной совместимости и игнорируется:
    // карта работает полностью локально, без обращений к Google Maps.
    const targetId = containerId || 'map';
    const container = document.getElementById(targetId);
    if (!container) {
        console.error(`Элемент карты #${targetId} не найден`);
        return;
    }

    const instance = createMapInstance(container, {
        centerLat: numberOrDefault(centerLat, 20),
        centerLng: numberOrDefault(centerLng, 0),
        zoom: numberOrDefault(zoom, 2)
    });

    mapInstances.set(targetId, instance);
    instance.draw();
    console.log(`Карта сообщества инициализирована (${targetId})`);
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

    const state = {
        canvas,
        ctx: canvas.getContext('2d'),
        container,
        tooltip,
        controls,
        participants: [],
        userLocation: null,
        focused: null,
        // Текущее состояние камеры
        zoom: clampZoom(initialState.zoom),
        centerLat: clampLat(initialState.centerLat),
        centerLng: wrapLng(initialState.centerLng),
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
        state.dragging = true;
        state.dragStart = {
            x: event.clientX,
            y: event.clientY,
            centerLat: state.centerLat,
            centerLng: state.centerLng
        };
        canvas.setPointerCapture?.(event.pointerId);
        canvas.style.cursor = 'grabbing';
    };

    const onPointerMove = (event) => {
        if (state.dragging && state.dragStart) {
            const dx = event.clientX - state.dragStart.x;
            const dy = event.clientY - state.dragStart.y;
            const { scaleX, scaleY } = projectionScale(state);
            state.centerLng = wrapLng(state.dragStart.centerLng - dx / scaleX);
            state.centerLat = clampLat(state.dragStart.centerLat + dy / scaleY);
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
        state.zoom = 2;
        state.centerLat = 20;
        state.centerLng = 0;
        draw();
    });

    state.listeners.push({ target: window, type: 'resize', handler: resize });
    window.addEventListener('resize', resize);

    resize();

    return {
        draw,
        setParticipants(list) {
            state.participants = Array.isArray(list) ? list.slice() : [];
            draw();
        },
        setUserLocation(location) {
            state.userLocation = location;
            draw();
        },
        centerOn(lat, lng, zoom) {
            state.centerLat = clampLat(lat);
            state.centerLng = wrapLng(lng);
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
    return Math.max(-85, Math.min(85, value));
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

function projectionScale(state) {
    // Базовая проекция охватывает 360° по долготе на ширину канвы при zoom = 1.
    const baseScaleX = state.width / 360;
    const baseScaleY = state.height / 180;
    const scaleX = baseScaleX * state.zoom;
    const scaleY = baseScaleY * state.zoom;
    return { scaleX, scaleY };
}

function projectToCanvas(state, lat, lng) {
    const { scaleX, scaleY } = projectionScale(state);
    const dx = wrapLng(lng - state.centerLng);
    const x = state.width / 2 + dx * scaleX;
    const y = state.height / 2 - (lat - state.centerLat) * scaleY;
    return { x, y };
}

// При отрисовке полигонов важно сохранять «несвёрнутые» смещения относительно
// предыдущей точки, иначе фигуры, пересекающие линию перемены даты, рисуются
// как одна сплошная горизонтальная полоса.
function projectPolygonPoint(state, lat, lng, previousDx) {
    const { scaleX, scaleY } = projectionScale(state);
    let dx = lng - state.centerLng;
    if (previousDx === null) {
        // Первая точка — нормализуем относительно центра карты.
        while (dx > 180) {
            dx -= 360;
        }
        while (dx < -180) {
            dx += 360;
        }
    } else {
        // Последующие точки — минимизируем расстояние от предыдущей вершины.
        while (dx - previousDx > 180) {
            dx -= 360;
        }
        while (dx - previousDx < -180) {
            dx += 360;
        }
    }
    const x = state.width / 2 + dx * scaleX;
    const y = state.height / 2 - (lat - state.centerLat) * scaleY;
    return { x, y, dx };
}

function canvasToLatLng(state, x, y) {
    const { scaleX, scaleY } = projectionScale(state);
    const lng = wrapLng(state.centerLng + (x - state.width / 2) / scaleX);
    const lat = clampLat(state.centerLat - (y - state.height / 2) / scaleY);
    return { lat, lng };
}

function zoomAt(state, factor, pointer) {
    const before = canvasToLatLng(state, pointer.x, pointer.y);
    state.zoom = clampZoom(state.zoom * factor);
    const after = canvasToLatLng(state, pointer.x, pointer.y);
    state.centerLat = clampLat(state.centerLat + (before.lat - after.lat));
    state.centerLng = wrapLng(state.centerLng + (before.lng - after.lng));
}

function drawScene(state) {
    const { ctx, width, height } = state;
    ctx.clearRect(0, 0, width, height);

    drawOcean(ctx, width, height);
    drawLand(state);
    drawGraticule(state);
    drawParticipants(state);
    drawUserLocation(state);
    drawFocusedParticipant(state);
}

function drawOcean(ctx, width, height) {
    ctx.fillStyle = '#101a26';
    ctx.fillRect(0, 0, width, height);
}

function drawLand(state) {
    const { ctx } = state;
    ctx.save();
    ctx.fillStyle = '#1d2b3a';
    ctx.strokeStyle = 'rgba(148, 163, 184, 0.42)';
    ctx.lineWidth = 1;

    WORLD_LAND_PATHS.forEach((shape) => {
        // Чтобы избежать «провисания» полигона через всю карту при пересечении
        // линии смены даты, отрисуем фигуру дважды со смещением ±360°.
        for (const offset of [-360, 0, 360]) {
            ctx.beginPath();
            let previousDx = null;
            for (const [lng, lat] of shape) {
                const result = projectPolygonPoint(state, lat, lng + offset, previousDx);
                if (previousDx === null) {
                    ctx.moveTo(result.x, result.y);
                } else {
                    ctx.lineTo(result.x, result.y);
                }
                previousDx = result.dx;
            }
            ctx.closePath();
            ctx.fill();
            ctx.stroke();
        }
    });

    ctx.restore();
}

function drawGraticule(state) {
    const { ctx, width, height } = state;
    ctx.save();
    ctx.strokeStyle = 'rgba(148, 163, 184, 0.18)';
    ctx.lineWidth = 1;

    const latStep = state.zoom >= 6 ? 10 : state.zoom >= 3 ? 20 : 30;
    const lngStep = state.zoom >= 6 ? 10 : state.zoom >= 3 ? 20 : 30;

    for (let lat = -90; lat <= 90; lat += latStep) {
        const { y } = projectToCanvas(state, lat, state.centerLng);
        if (y < -10 || y > height + 10) {
            continue;
        }
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
    }

    for (let lng = -180; lng <= 180; lng += lngStep) {
        const { x } = projectToCanvas(state, state.centerLat, lng);
        if (x < -10 || x > width + 10) {
            continue;
        }
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, height);
        ctx.stroke();
    }

    // Экватор и нулевой меридиан выделены чуть ярче.
    ctx.strokeStyle = 'rgba(148, 163, 184, 0.32)';
    const equator = projectToCanvas(state, 0, state.centerLng);
    if (equator.y >= 0 && equator.y <= height) {
        ctx.beginPath();
        ctx.moveTo(0, equator.y);
        ctx.lineTo(width, equator.y);
        ctx.stroke();
    }
    const prime = projectToCanvas(state, state.centerLat, 0);
    if (prime.x >= 0 && prime.x <= width) {
        ctx.beginPath();
        ctx.moveTo(prime.x, 0);
        ctx.lineTo(prime.x, height);
        ctx.stroke();
    }
    ctx.restore();
}

function drawParticipants(state) {
    const { ctx } = state;
    state.participants.forEach((participant, index) => {
        if (!isFinitePair(participant.Latitude, participant.Longitude)) {
            return;
        }
        const { x, y } = projectToCanvas(state, participant.Latitude, participant.Longitude);
        if (x < -40 || x > state.width + 40 || y < -40 || y > state.height + 40) {
            return;
        }
        const radius = state.hoveredParticipantIndex === index ? 11 : 9;
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

        const initial = String(participant.Name || '?').charAt(0).toUpperCase();
        ctx.save();
        ctx.fillStyle = '#0e1013';
        ctx.font = 'bold 11px Arial';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(initial, x, y + 0.5);
        ctx.restore();
    });
}

function drawUserLocation(state) {
    if (!state.userLocation) {
        return;
    }
    const { ctx } = state;
    const { x, y } = projectToCanvas(state, state.userLocation.latitude, state.userLocation.longitude);
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
}

function drawFocusedParticipant(state) {
    if (!state.focused) {
        return;
    }
    const { ctx } = state;
    const { x, y } = projectToCanvas(state, state.focused.lat, state.focused.lng);
    ctx.save();
    ctx.beginPath();
    ctx.arc(x, y, 14, 0, Math.PI * 2);
    ctx.strokeStyle = '#ffcf5a';
    ctx.lineWidth = 3;
    ctx.stroke();
    ctx.restore();
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
    for (let i = state.participants.length - 1; i >= 0; i -= 1) {
        const participant = state.participants[i];
        if (!isFinitePair(participant.Latitude, participant.Longitude)) {
            continue;
        }
        const { x, y } = projectToCanvas(state, participant.Latitude, participant.Longitude);
        const dx = pointer.x - x;
        const dy = pointer.y - y;
        if (dx * dx + dy * dy <= 13 * 13) {
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
