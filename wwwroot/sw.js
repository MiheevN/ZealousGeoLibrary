// Service Worker для Zealous Minded People Geography
const CACHE_NAME = 'zealous-geo-v1.0.0';
const STATIC_CACHE_NAME = 'zealous-geo-static-v1.0.0';
const DYNAMIC_CACHE_NAME = 'zealous-geo-dynamic-v1.0.0';

// Ресурсы для кэширования при установке
const STATIC_ASSETS = [
    '/',
    '/_content/ZealousMindedPeopleGeo/css/site.css',
    '/_content/ZealousMindedPeopleGeo/js/community-map.js',
    '/_content/ZealousMindedPeopleGeo/js/community-globe.js',
    '/_content/ZealousMindedPeopleGeo/js/libs/three.module.js',
    '/_content/ZealousMindedPeopleGeo/js/libs/OrbitControls.js',
    '/_content/ZealousMindedPeopleGeo/manifest.json'
];

// API endpoints для кэширования
const API_CACHE_PATTERNS = [
    /\/api\/participants/,
    /\/api\/geocoding/,
    /\/api\/healthcheck/
];

// Установка сервис-воркера
self.addEventListener('install', (event) => {
    console.log('[SW] Installing Service Worker');

    event.waitUntil(
        caches.open(STATIC_CACHE_NAME)
            .then((cache) => {
                console.log('[SW] Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('[SW] Static assets cached successfully');
                return self.skipWaiting();
            })
            .catch((error) => {
                console.error('[SW] Error caching static assets:', error);
            })
    );
});

// Активация сервис-воркера
self.addEventListener('activate', (event) => {
    console.log('[SW] Activating Service Worker');

    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => {
                        // Удаляем старые кэши
                        if (cacheName !== STATIC_CACHE_NAME &&
                            cacheName !== DYNAMIC_CACHE_NAME &&
                            cacheName.startsWith('zealous-geo-')) {
                            console.log('[SW] Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
            .then(() => {
                console.log('[SW] Service Worker activated');
                return self.clients.claim();
            })
    );
});

// Стратегия кэширования: Network First для API, Cache First для статических ресурсов
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Обрабатываем только запросы к нашему домену
    if (url.origin !== location.origin) {
        return;
    }

    // Стратегия для API запросов (Network First)
    if (API_CACHE_PATTERNS.some(pattern => pattern.test(request.url))) {
        event.respondWith(networkFirstStrategy(request));
        return;
    }

    // Стратегия для статических ресурсов (Cache First)
    if (request.destination === 'style' ||
        request.destination === 'script' ||
        request.destination === 'image' ||
        request.url.includes('/_content/ZealousMindedPeopleGeo/')) {
        event.respondWith(cacheFirstStrategy(request));
        return;
    }

    // Стратегия для HTML страниц (Network First с fallback)
    if (request.destination === 'document') {
        event.respondWith(networkFirstWithFallbackStrategy(request));
        return;
    }

    // Для остальных запросов используем Network First
    event.respondWith(networkFirstStrategy(request));
});

// Стратегия Network First
async function networkFirstStrategy(request) {
    try {
        // Пробуем получить данные из сети
        const networkResponse = await fetch(request);

        // Если запрос успешен, кэшируем ответ
        if (networkResponse && networkResponse.status === 200) {
            const cache = await caches.open(DYNAMIC_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.log('[SW] Network failed, trying cache:', request.url);

        // Если сеть недоступна, пробуем кэш
        const cachedResponse = await caches.match(request);

        if (cachedResponse) {
            return cachedResponse;
        }

        // Если нет в кэше, возвращаем оффлайн страницу
        if (request.destination === 'document') {
            return caches.match('/');
        }

        throw error;
    }
}

// Стратегия Cache First
async function cacheFirstStrategy(request) {
    try {
        // Пробуем получить из кэша
        const cachedResponse = await caches.match(request);

        if (cachedResponse) {
            return cachedResponse;
        }

        // Если нет в кэше, загружаем из сети
        const networkResponse = await fetch(request);

        if (networkResponse && networkResponse.status === 200) {
            const cache = await caches.open(STATIC_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.error('[SW] Cache First strategy failed:', error);
        throw error;
    }
}

// Стратегия Network First с fallback на кэш
async function networkFirstWithFallbackStrategy(request) {
    try {
        const networkResponse = await fetch(request);

        if (networkResponse && networkResponse.status === 200) {
            const cache = await caches.open(DYNAMIC_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }

        return networkResponse;
    } catch (error) {
        console.log('[SW] Network failed for document, trying cache');

        const cachedResponse = await caches.match(request);

        if (cachedResponse) {
            return cachedResponse;
        }

        // Возвращаем главную страницу как fallback
        return caches.match('/');
    }
}

// Обработка push уведомлений
self.addEventListener('push', (event) => {
    console.log('[SW] Push event received');

    const options = {
        body: event.data ? event.data.text() : 'Новое событие в сообществе',
        icon: '/_content/ZealousMindedPeopleGeo/icons/icon-192x192.png',
        badge: '/_content/ZealousMindedPeopleGeo/icons/badge-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            dateOfArrival: Date.now(),
            primaryKey: 1
        },
        actions: [
            {
                action: 'explore',
                title: 'Посмотреть',
                icon: '/_content/ZealousMindedPeopleGeo/icons/checkmark.png'
            },
            {
                action: 'close',
                title: 'Закрыть',
                icon: '/_content/ZealousMindedPeopleGeo/icons/xmark.png'
            }
        ],
        requireInteraction: false,
        silent: false
    };

    event.waitUntil(
        self.registration.showNotification('Zealous Minded People Geography', options)
    );
});

// Обработка кликов по уведомлениям
self.addEventListener('notificationclick', (event) => {
    console.log('[SW] Notification click received');

    event.notification.close();

    if (event.action === 'explore') {
        event.waitUntil(
            clients.openWindow('/')
        );
    }
});

// Обработка сообщений из основного потока
self.addEventListener('message', (event) => {
    console.log('[SW] Message received:', event.data);

    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }

    if (event.data && event.data.type === 'GET_VERSION') {
        event.ports[0].postMessage({ version: CACHE_NAME });
    }

    if (event.data && event.data.type === 'CLEAR_CACHE') {
        event.waitUntil(
            caches.keys().then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => caches.delete(cacheName))
                );
            })
        );
    }
});

// Периодическая очистка старого кэша
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'CLEANUP_CACHE') {
        event.waitUntil(
            caches.keys().then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => {
                        if (cacheName !== STATIC_CACHE_NAME && cacheName !== DYNAMIC_CACHE_NAME) {
                            console.log('[SW] Deleting old cache:', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
        );
    }
});

// Обработка ошибок
self.addEventListener('error', (event) => {
    console.error('[SW] Service Worker error:', event.error);
});

self.addEventListener('unhandledrejection', (event) => {
    console.error('[SW] Service Worker unhandled promise rejection:', event.reason);
    event.preventDefault();
});