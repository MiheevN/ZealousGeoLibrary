// Глобальные переменные для карты
let map = null;
let markers = [];
let userLocationMarker = null;
let dotNetHelper = null;

window.setDotNetHelper = (helper) => {
    dotNetHelper = helper;
};

window.initializeCommunityMap = (apiKey, centerLat, centerLng, zoom) => {
    // Загружаем Google Maps API если он не загружен
    if (typeof google === 'undefined') {
        loadGoogleMapsApi(apiKey).then(() => {
            initializeMap(centerLat, centerLng, zoom, dotNetHelper);
        });
    } else {
        initializeMap(centerLat, centerLng, zoom, dotNetHelper);
    }
};

function loadGoogleMapsApi(apiKey) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places`;
        script.onload = resolve;
        script.onerror = reject;
        document.head.appendChild(script);
    });
}

function initializeMap(centerLat, centerLng, zoom, dotNetHelper) {
    const mapOptions = {
        center: { lat: centerLat, lng: centerLng },
        zoom: zoom,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
        mapTypeControl: true,
        streetViewControl: true,
        fullscreenControl: true
    };

    const mapElement = document.getElementById('map');
    if (!mapElement) {
        console.error('Элемент карты не найден');
        return;
    }

    map = new google.maps.Map(mapElement, mapOptions);

    // Добавляем обработчик клика на карту
    map.addListener('click', (event) => {
        if (window.dotNetHelper && window.dotNetHelper.invokeMethodAsync) {
            window.dotNetHelper.invokeMethodAsync('OnMapClick', event.latLng.lat(), event.latLng.lng());
        }
    });

    console.log('Карта сообщества инициализирована');
}

window.loadParticipantsOnMap = (participantsJson) => {
    if (!map) {
        console.error('Карта не инициализирована');
        return;
    }

    try {
        const participants = JSON.parse(participantsJson);

        // Удаляем существующие маркеры
        clearAllMarkers();

        // Добавляем маркеры для каждого участника
        participants.forEach((participant, index) => {
            addParticipantMarker(participant, index + 1);
        });

        console.log(`Загружено ${participants.length} участников на карту`);
    } catch (error) {
        console.error('Ошибка загрузки участников на карту:', error);
    }
};

function addParticipantMarker(participant, index) {
    if (!participant.Latitude || !participant.Longitude) {
        return;
    }

    const markerPosition = {
        lat: participant.Latitude,
        lng: participant.Longitude
    };

    const marker = new google.maps.Marker({
        position: markerPosition,
        map: map,
        title: participant.Name,
        animation: google.maps.Animation.DROP,
        icon: {
            url: createMarkerDataUrl(createCustomMarker(participant.Name)),
            scaledSize: new google.maps.Size(40, 40),
            anchor: new google.maps.Point(20, 40)
        }
    });

    // Создаем информационное окно
    const infoWindow = new google.maps.InfoWindow({
        content: createInfoWindowContent(participant)
    });

    // Добавляем обработчик клика на маркер
    marker.addListener('click', () => {
        infoWindow.open(map, marker);

        // Вызываем метод в Blazor компоненте
        if (dotNetHelper && dotNetHelper.invokeMethodAsync) {
            dotNetHelper.invokeMethodAsync('OnParticipantMarkerClick', participant.Id || index);
        }
    });

    markers.push(marker);

    // Автоматически показываем информационное окно для первого маркера
    if (index === 1) {
        setTimeout(() => {
            infoWindow.open(map, marker);
        }, 1000);
    }
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

    return text.replace(/[&<>"']/g, character => entities[character]);
}

function formatMapDate(value) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? '—' : date.toLocaleDateString();
}

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

function createInfoWindowContent(participant) {
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

    // Отображаем город и страну если они есть
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

    if (participant.Latitude && participant.Longitude) {
        addRow('🗺️ Координаты:', `${participant.Latitude.toFixed(6)}, ${participant.Longitude.toFixed(6)}`);
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

    // Добавляем социальные сети если есть
    const socialLinks = [];
    if (participant.SocialContacts?.Discord) {
        socialLinks.push(`Discord: ${participant.SocialContacts.Discord}`);
    }
    if (participant.SocialContacts?.Telegram) {
        socialLinks.push(`Telegram: ${participant.SocialContacts.Telegram}`);
    }
    if (participant.SocialContacts?.Vk) {
        socialLinks.push(`VK: ${participant.SocialContacts.Vk}`);
    }
    if (participant.SocialContacts?.Website) {
        socialLinks.push(`Website: ${participant.SocialContacts.Website}`);
    }

    // Также поддерживаем старое поле SocialMedia для обратной совместимости
    if (participant.SocialMedia) {
        socialLinks.push(participant.SocialMedia);
    }

    if (socialLinks.length > 0) {
        rows.push(`
            <p style="display: grid; grid-template-columns: 122px 1fr; gap: 10px; margin: 7px 0; color: #b8c2d0; line-height: 1.45;">
                <strong style="color: #f4f7fb;">🌐 Соцсети:</strong>
                <span>${socialLinks.map(escapeHtml).join('<br>')}</span>
            </p>
        `);
    }

    return `
        <div class="zgl-info-window" style="background: #171a1f; border: 1px solid rgba(148, 163, 184, 0.28); border-radius: 8px; box-shadow: 0 18px 50px rgba(0, 0, 0, 0.38); color: #f4f7fb; font-family: Arial, sans-serif; max-width: 320px; padding: 12px;">
            <h4 style="margin: 0 0 10px 0; color: #24dce7; font-size: 16px;">${escapeHtml(participant.Name || 'Участник')}</h4>
            ${rows.join('')}
        </div>
    `;
}

function clearAllMarkers() {
    markers.forEach(marker => {
        marker.setMap(null);
    });
    markers = [];
}

window.centerMapOnUserLocation = () => {
    if (!map) {
        console.error('Карта не инициализирована');
        return;
    }

    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const userLocation = {
                    lat: position.coords.latitude,
                    lng: position.coords.longitude
                };

                map.setCenter(userLocation);
                map.setZoom(12);

                // Добавляем маркер текущего местоположения
                if (userLocationMarker) {
                    userLocationMarker.setMap(null);
                }

                userLocationMarker = new google.maps.Marker({
                    position: userLocation,
                    map: map,
                    title: 'Ваше местоположение',
                    icon: {
                        url: createMarkerDataUrl(createUserLocationMarker()),
                        scaledSize: new google.maps.Size(30, 30),
                        anchor: new google.maps.Point(15, 15)
                    }
                });

                console.log('Карта центрирована на вашем местоположении');
            },
            (error) => {
                console.error('Ошибка получения геолокации:', error);
                alert('Не удалось получить ваше местоположение. Проверьте настройки браузера.');
            }
        );
    } else {
        console.error('Геолокация не поддерживается в этом браузере');
        alert('Геолокация не поддерживается в вашем браузере');
    }
};

window.focusOnParticipant = (latitude, longitude, name) => {
    if (!map) {
        console.error('Карта не инициализирована');
        return;
    }

    const position = { lat: latitude, lng: longitude };

    map.setCenter(position);
    map.setZoom(15);

    // Создаем временный маркер для фокуса
    new google.maps.Marker({
        position: position,
        map: map,
        title: name,
        animation: google.maps.Animation.BOUNCE,
        icon: {
            url: createMarkerDataUrl(createFocusMarker(name)),
            scaledSize: new google.maps.Size(36, 44),
            anchor: new google.maps.Point(18, 42)
        }
    });

    console.log(`Фокус на участнике: ${name}`);
};

// Экспортируем функции для использования в других модулях
window.CommunityMapUtils = {
    initializeCommunityMap,
    loadParticipantsOnMap,
    centerMapOnUserLocation,
    focusOnParticipant,
    clearAllMarkers
};
