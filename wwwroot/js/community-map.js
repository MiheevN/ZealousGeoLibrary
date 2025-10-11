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
            url: `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(createCustomMarker(participant.Name))}`,
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

function createCustomMarker(name) {
    const initial = name.charAt(0).toUpperCase();
    return `
        <svg width="40" height="40" viewBox="0 0 40 40" xmlns="http://www.w3.org/2000/svg">
            <circle cx="20" cy="20" r="18" fill="#007bff" stroke="#ffffff" stroke-width="2"/>
            <text x="20" y="26" text-anchor="middle" fill="white" font-family="Arial" font-size="14" font-weight="bold">${initial}</text>
        </svg>
    `;
}

function createInfoWindowContent(participant) {
    let content = `
        <div style="max-width: 250px; font-family: Arial, sans-serif;">
            <h4 style="margin: 0 0 10px 0; color: #007bff;">${participant.Name}</h4>
            <p style="margin: 5px 0; color: #666;"><strong>📍 Местоположение:</strong> ${participant.Location}</p>
            <p style="margin: 5px 0; color: #666;"><strong>📅 Регистрация:</strong> ${new Date(participant.Timestamp).toLocaleDateString()}</p>
    `;

    if (participant.Skills) {
        content += `<p style="margin: 5px 0; color: #666;"><strong>🛠 Навыки:</strong> ${participant.Skills}</p>`;
    }

    if (participant.LifeGoals) {
        content += `<p style="margin: 5px 0; color: #666;"><strong>🎯 Цели:</strong> ${participant.LifeGoals}</p>`;
    }

    if (participant.Message) {
        content += `<p style="margin: 5px 0; color: #666;"><strong>💬 Сообщение:</strong> ${participant.Message}</p>`;
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

    if (socialLinks.length > 0) {
        content += `<p style="margin: 5px 0; color: #666;"><strong>🌐 Социальные сети:</strong></p>`;
        content += `<p style="margin: 5px 0; padding-left: 10px; color: #666;">${socialLinks.join(', ')}</p>`;
    }

    content += `</div>`;

    return content;
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
                        url: 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(`
                            <svg width="30" height="30" viewBox="0 0 30 30" xmlns="http://www.w3.org/2000/svg">
                                <circle cx="15" cy="15" r="12" fill="#28a745" stroke="#ffffff" stroke-width="2"/>
                                <circle cx="15" cy="15" r="6" fill="#ffffff"/>
                            </svg>
                        `),
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
        animation: google.maps.Animation.BOUNCE
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