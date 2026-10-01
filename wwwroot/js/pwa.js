// wwwroot/js/pwa.js
// Функции PWA для PwaService. Модуль подключается через import() из интерактивного
// рендера, поэтому при пререндеринге он не загружается и не вызывается.

const UPDATE_CONFIRM_MESSAGE = 'Доступно обновление приложения. Обновить сейчас?';
const MANIFEST = '_content/ZealousMindedPeopleGeo/manifest.json';
const TOUCH_ICON = '_content/ZealousMindedPeopleGeo/icons/icon-192x192.png';
const THEME_COLOR = '#0e1013';

// Браузер присылает beforeinstallprompt один раз; сохраняем событие, чтобы показать промпт по кнопке.
let deferredInstallPrompt = null;
let serviceWorkerListenersAttached = false;

window.addEventListener('beforeinstallprompt', (event) => {
    event.preventDefault();
    deferredInstallPrompt = event;
});

window.addEventListener('appinstalled', () => {
    deferredInstallPrompt = null;
});

// Хост мог перехватить событие раньше, чем загрузился модуль, и положить его в window.beforeinstallprompt.
function getInstallPrompt() {
    return deferredInstallPrompt ?? window.beforeinstallprompt ?? null;
}

function clearInstallPrompt() {
    deferredInstallPrompt = null;
    delete window.beforeinstallprompt;
}

// Подключает манифест библиотеки, если у приложения нет своего. Браузер принимает манифест,
// добавленный скриптом, и предлагает установить сайт без правок в разметке приложения.
export function ensureManifest(doc = document) {
    if (doc.querySelector('link[rel="manifest"]')) {
        return false;
    }

    const manifest = doc.createElement('link');
    manifest.rel = 'manifest';
    manifest.href = new URL(MANIFEST, doc.baseURI).href;
    doc.head.appendChild(manifest);

    if (!doc.querySelector('meta[name="theme-color"]')) {
        const themeColor = doc.createElement('meta');
        themeColor.name = 'theme-color';
        themeColor.content = THEME_COLOR;
        doc.head.appendChild(themeColor);
    }

    if (!doc.querySelector('link[rel="apple-touch-icon"]')) {
        const touchIcon = doc.createElement('link');
        touchIcon.rel = 'apple-touch-icon';
        touchIcon.href = new URL(TOUCH_ICON, doc.baseURI).href;
        doc.head.appendChild(touchIcon);
    }

    return true;
}

// Воркер библиотеки лежит в /_content/..., а обслуживать должен все страницы приложения.
// Браузер разрешает такую область, только если сервер отдаёт sw.js с заголовком
// Service-Worker-Allowed. Проверяем заранее: без заголовка офлайн-кэш просто выключен,
// и в консоли не появляется ошибка регистрации.
async function isScopeAllowed(scriptUrl, scope) {
    const script = new URL(scriptUrl, document.baseURI);
    const scriptFolder = script.pathname.slice(0, script.pathname.lastIndexOf('/') + 1);
    if (scope.startsWith(scriptFolder)) {
        return true;
    }

    try {
        const response = await fetch(script.href, { method: 'HEAD', cache: 'no-store' });
        const allowed = response.headers.get('Service-Worker-Allowed');
        return response.ok && allowed !== null && scope.startsWith(new URL(allowed, script).pathname);
    } catch {
        return false;
    }
}

export async function registerServiceWorker(scriptUrl) {
    ensureManifest();

    if (!('serviceWorker' in navigator)) {
        return false;
    }

    // Blazor вызывает модуль после загрузки страницы, когда событие load уже прошло.
    if (document.readyState !== 'complete') {
        await new Promise((resolve) => window.addEventListener('load', resolve, { once: true }));
    }

    // Область воркера — корень приложения с учётом <base href>.
    const scope = new URL('.', document.baseURI).pathname;
    if (!(await isScopeAllowed(scriptUrl, scope))) {
        console.info('[ZealousMindedPeopleGeo] Офлайн-кэш выключен: сервер не отдаёт sw.js с заголовком '
            + 'Service-Worker-Allowed (см. README, раздел «PWA и сервис-воркер»).');
        return false;
    }

    try {
        // updateViaCache: 'none' — проверка новой версии sw.js всегда идёт мимо HTTP-кэша.
        const registration = await navigator.serviceWorker.register(scriptUrl, {
            scope,
            updateViaCache: 'none'
        });
        console.log('Service Worker registered successfully:', registration.scope);

        // Проверяем обновления
        registration.addEventListener('updatefound', () => {
            const newWorker = registration.installing;
            if (newWorker) {
                newWorker.addEventListener('statechange', () => {
                    if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                        // Новый контент доступен, предлагаем обновление
                        if (confirm(UPDATE_CONFIRM_MESSAGE)) {
                            window.location.reload();
                        }
                    }
                });
            }
        });

        // Слушаем сообщения от сервис-воркера
        if (!serviceWorkerListenersAttached) {
            serviceWorkerListenersAttached = true;
            navigator.serviceWorker.addEventListener('message', (event) => {
                if (event.data && event.data.type === 'SW_UPDATE_READY') {
                    if (confirm(UPDATE_CONFIRM_MESSAGE)) {
                        window.location.reload();
                    }
                }
            });
        }

        // register() для того же sw.js новую версию не ищет, а Chromium откладывает
        // проверку после навигации, пока воркер занят. Проверяем явно при каждом запуске.
        registration.update().catch((error) => {
            console.warn('Service Worker update check failed:', error);
        });

        return true;
    } catch (error) {
        console.error('Service Worker registration failed:', error);
        return false;
    }
}

export function isPwaSupported() {
    return 'serviceWorker' in navigator && 'PushManager' in window;
}

export function isRunningAsPwa() {
    return window.matchMedia('(display-mode: standalone)').matches ||
        window.navigator.standalone === true;
}

export function getInstallInfo() {
    const isInstalled = isRunningAsPwa();
    return {
        canInstall: getInstallPrompt() !== null && !isInstalled,
        isInstalled: isInstalled,
        isSupported: isPwaSupported()
    };
}

export async function showInstallPrompt() {
    const promptEvent = getInstallPrompt();
    if (!promptEvent) {
        return false;
    }

    // Событие можно показать только один раз, повторный prompt() бросает исключение.
    clearInstallPrompt();
    await promptEvent.prompt();
    const choiceResult = await promptEvent.userChoice;
    return choiceResult.outcome === 'accepted';
}

export async function clearCache() {
    if (!('caches' in window)) {
        return;
    }

    // Удаляем те же кэши, что и обработчик CLEAR_CACHE в sw.js, но дожидаемся окончания,
    // чтобы сразу после очистки PwaService прочитал актуальный размер.
    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map((cacheName) => caches.delete(cacheName)));
}

export async function updateServiceWorker() {
    if (!('serviceWorker' in navigator)) {
        return;
    }

    const registration = await navigator.serviceWorker.getRegistration();
    if (registration) {
        await registration.update();
    }
}

export async function getCacheInfo() {
    const emptyInfo = { totalSize: 0, cacheCount: 0, caches: [] };
    if (!('caches' in window)) {
        return emptyInfo;
    }

    try {
        const cacheNames = await caches.keys();
        let totalSize = 0;
        const cacheDetails = [];

        for (const name of cacheNames) {
            const cache = await caches.open(name);
            const keys = await cache.keys();
            let cacheSize = 0;

            for (const request of keys) {
                try {
                    const response = await cache.match(request);
                    if (response) {
                        const blob = await response.blob();
                        cacheSize += blob.size;
                    }
                } catch (e) {
                    // Игнорируем ошибки при подсчете размера
                }
            }

            cacheDetails.push({
                name: name,
                size: cacheSize,
                itemCount: keys.length
            });

            totalSize += cacheSize;
        }

        return {
            totalSize: totalSize,
            cacheCount: cacheNames.length,
            caches: cacheDetails
        };
    } catch (error) {
        return emptyInfo;
    }
}

export async function sendNotification(title, options) {
    if (!('Notification' in window) || !('serviceWorker' in navigator)) {
        return false;
    }

    let permission = Notification.permission;
    if (permission === 'default') {
        permission = await Notification.requestPermission();
    }

    if (permission !== 'granted') {
        return false;
    }

    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration) {
        return false;
    }

    await registration.showNotification(title, options);
    return true;
}
