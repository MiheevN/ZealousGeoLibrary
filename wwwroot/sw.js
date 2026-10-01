// Service Worker для Zealous Minded People Geography

// Версия кэшей = версия NuGet-пакета (<Version> в ZealousMindedPeopleGeo.csproj).
// Поднимайте её вместе с пакетом: изменённый sw.js браузер ставит как новый воркер,
// а activate удаляет кэши zealous-geo-* всех прочих версий.
// Совпадение с пакетом проверяет experiments/service-worker-cache.test.mjs.
const SW_VERSION = '1.1.0';
const CACHE_PREFIX = 'zealous-geo-';
const STATIC_CACHE_NAME = `${CACHE_PREFIX}static-v${SW_VERSION}`;
const DYNAMIC_CACHE_NAME = `${CACHE_PREFIX}dynamic-v${SW_VERSION}`;
const CURRENT_CACHES = [STATIC_CACHE_NAME, DYNAMIC_CACHE_NAME];

const LIBRARY_PATH = '/_content/ZealousMindedPeopleGeo/';

// Ресурсы для кэширования при установке (оффлайн-режим)
const STATIC_ASSETS = [
    '/',
    '/_content/ZealousMindedPeopleGeo/css/zealous-ui.css',
    '/_content/ZealousMindedPeopleGeo/css/community-map.css',
    '/_content/ZealousMindedPeopleGeo/css/community-globe.css',
    '/_content/ZealousMindedPeopleGeo/js/community-map.js',
    '/_content/ZealousMindedPeopleGeo/js/community-globe.js',
    '/_content/ZealousMindedPeopleGeo/js/label-scale.js',
    '/_content/ZealousMindedPeopleGeo/js/libs/three.module.js',
    '/_content/ZealousMindedPeopleGeo/js/libs/three.core.js',
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
    console.log('[SW] Installing Service Worker', SW_VERSION);

    event.waitUntil(
        precacheStaticAssets()
            .then(() => {
                console.log('[SW] Static assets cached');
                return self.skipWaiting();
            })
    );
});

// Каждый ресурс кэшируется отдельно: один недоступный файл не должен срывать установку
// и skipWaiting, иначе новая версия ждёт, пока пользователь закроет все вкладки.
async function precacheStaticAssets() {
    const cache = await caches.open(STATIC_CACHE_NAME);
    const results = await Promise.allSettled(
        // cache: 'reload' — берём файлы с сервера, а не из HTTP-кэша браузера
        STATIC_ASSETS.map((url) => cache.add(new Request(url, { cache: 'reload' })))
    );

    results.forEach((result, index) => {
        if (result.status === 'rejected') {
            console.warn('[SW] Error caching static asset:', STATIC_ASSETS[index], result.reason);
        }
    });
}

// Активация сервис-воркера
self.addEventListener('activate', (event) => {
    console.log('[SW] Activating Service Worker', SW_VERSION);

    event.waitUntil(
        deleteOutdatedCaches()
            .then(() => {
                console.log('[SW] Service Worker activated');
                return self.clients.claim();
            })
    );
});

// Удаляем кэши zealous-geo-* прошлых версий; кэши самого сайта не трогаем
async function deleteOutdatedCaches() {
    const cacheNames = await caches.keys();

    await Promise.all(
        cacheNames
            .filter((cacheName) => cacheName.startsWith(CACHE_PREFIX) && !CURRENT_CACHES.includes(cacheName))
            .map((cacheName) => {
                console.log('[SW] Deleting old cache:', cacheName);
                return caches.delete(cacheName);
            })
    );
}

function isLibraryAsset(request) {
    return new URL(request.url).pathname.startsWith(LIBRARY_PATH);
}

// Стратегии кэширования:
// - API и страницы — Network First с fallback на кэш;
// - JS, CSS и данные — Network First: обновлённый код приходит сразу, кэш нужен только без сети;
// - картинки и шрифты библиотеки — Cache First, их обновляет смена SW_VERSION.
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);

    // Обрабатываем только GET-запросы к нашему домену
    if (request.method !== 'GET' || url.origin !== location.origin) {
        return;
    }

    // Соединение Blazor Server и SSE не трогаем: long polling держит запрос открытым,
    // а пока fetch-событие не завершено, новая версия не активируется даже после skipWaiting.
    if (url.pathname.startsWith('/_blazor') ||
        request.headers.get('Accept') === 'text/event-stream') {
        return;
    }

    // Стратегия для API запросов (Network First)
    if (API_CACHE_PATTERNS.some(pattern => pattern.test(request.url))) {
        event.respondWith(networkFirstStrategy(request, DYNAMIC_CACHE_NAME));
        return;
    }

    // Стратегия для HTML страниц (Network First с fallback)
    if (request.mode === 'navigate') {
        event.respondWith(networkFirstWithFallbackStrategy(request));
        return;
    }

    // Тяжёлые и редко меняющиеся ресурсы библиотеки (текстуры, иконки) — Cache First
    if (isLibraryAsset(request) &&
        (request.destination === 'image' || request.destination === 'font')) {
        event.respondWith(cacheFirstStrategy(request));
        return;
    }

    // Скрипты, стили и прочие файлы библиотеки (Network First)
    if (isLibraryAsset(request) ||
        request.destination === 'script' ||
        request.destination === 'style') {
        event.respondWith(networkFirstStrategy(request, STATIC_CACHE_NAME));
        return;
    }

    // Для остальных запросов используем Network First
    event.respondWith(networkFirstStrategy(request, DYNAMIC_CACHE_NAME));
});

// Стратегия Network First
async function networkFirstStrategy(request, cacheName) {
    try {
        // Файлы библиотеки отдаются без версии в URL, и HTTP-кэш браузера может эвристически
        // считать старую копию свежей. no-cache заставляет перепроверить её у сервера (304 дёшев).
        const networkResponse = await fetch(request, isLibraryAsset(request) ? { cache: 'no-cache' } : undefined);

        // Если запрос успешен, кэшируем ответ
        if (networkResponse && networkResponse.status === 200) {
            const cache = await caches.open(cacheName);
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
                title: 'Посмотреть'
            },
            {
                action: 'close',
                title: 'Закрыть'
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
        event.ports[0].postMessage({ version: SW_VERSION });
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
            deleteOutdatedCaches()
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