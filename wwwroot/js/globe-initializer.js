// Инициализатор скриптов глобуса для Blazor
// Загружает все необходимые зависимости и делает функции доступными глобально

(function() {
    'use strict';

    console.log('🚀 Инициализация скриптов глобуса для Blazor');

    // Глобальное состояние инициализации (для синхронизации с Blazor)
    window.globeInitializationState = {
        scriptsLoaded: false,
        initialized: false,
        functionsAvailable: false
    };

    // Состояние загрузки (для обратной совместимости)
    window.GlobeInitializer = {
        isInitialized: false,
        dependenciesLoaded: false,
        initAttempts: 0,
        maxInitAttempts: 3  // Уменьшено количество попыток
    };

    // Функция для динамической загрузки скрипта
    function loadScript(src) {
        return new Promise((resolve, reject) => {
            // Проверяем, не загружен ли уже скрипт
            const existingScript = document.querySelector(`script[src*="${src}"]`);
            if (existingScript) {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = () => reject(new Error(`Failed to load script: ${src}`));
            document.head.appendChild(script);
        });
    }

    // Быстрая инициализация всех зависимостей
    async function initializeGlobeScripts() {
        try {
            console.log('🚀 Быстрая загрузка зависимостей глобуса');

            // Загружаем скрипты параллельно для ускорения
            const loadPromises = [
                loadScript('/_content/ZealousMindedPeopleGeo/js/libs/three.module.js'),
                loadScript('/_content/ZealousMindedPeopleGeo/js/libs/OrbitControls.js'),
                loadScript('/_content/ZealousMindedPeopleGeo/js/community-globe-global.js')
            ];

            await Promise.all(loadPromises);
            console.log('✅ Все скрипты загружены параллельно');

            // Проверяем доступность основных функций
            const requiredFunctions = [
                'createGlobe',
                'addTestParticipant',
                'safeAddTestParticipant',
                'removeParticipant',
                'clear',
                'centerOn'
            ];

            for (const funcName of requiredFunctions) {
                if (typeof window[funcName] !== 'function') {
                    throw new Error(`Функция ${funcName} не доступна после загрузки скриптов`);
                }
            }

            window.GlobeInitializer.dependenciesLoaded = true;
            window.GlobeInitializer.isInitialized = true;

            // Обновляем глобальное состояние
            window.globeInitializationState.scriptsLoaded = true;
            window.globeInitializationState.functionsAvailable = true;
            window.globeInitializationState.initialized = true;

            console.log('🎉 Все зависимости глобуса загружены успешно');
            console.log('Доступные функции:', requiredFunctions.filter(name => typeof window[name] === 'function'));

            return true;

        } catch (error) {
            console.error('❌ Ошибка инициализации зависимостей глобуса:', error);
            window.GlobeInitializer.initAttempts++;

            if (window.GlobeInitializer.initAttempts < window.GlobeInitializer.maxInitAttempts) {
                console.log(`Повторная попытка ${window.GlobeInitializer.initAttempts}/${window.GlobeInitializer.maxInitAttempts}`);
                // Повторная попытка через 500мс
                setTimeout(initializeGlobeScripts, 500);
            } else {
                console.error('💥 Превышено максимальное количество попыток инициализации');
            }

            return false;
        }
    }

    // Функция для проверки готовности скриптов
    window.areGlobeScriptsReady = function() {
        return window.GlobeInitializer.isInitialized &&
                typeof window.addTestParticipant === 'function' &&
                typeof window.safeAddTestParticipant === 'function';
    };

    // Функция для проверки готовности экземпляра глобуса
    window.isGlobeInstanceReady = function() {
        return window.globeInstances &&
               window.globeInstances.size > 0 &&
               window.globeInstances.values().next().value &&
               window.globeInstances.values().next().value.state &&
               window.globeInstances.values().next().value.state.isInitialized;
    };

    // Функция для полной проверки готовности
    window.isGlobeReady = function() {
        return areGlobeScriptsReady() && isGlobeInstanceReady();
    };

    // Безопасная проверка поддержки WebGL
    window.checkWebGLSupport = function() {
        try {
            var canvas = document.createElement('canvas');
            return !!(window.WebGLRenderingContext &&
                     (canvas.getContext('webgl') || canvas.getContext('experimental-webgl')));
        } catch (e) {
            return false;
        }
    };

    // Проверка доступности модуля
    window.checkModuleAvailable = async function(modulePath) {
        try {
            await import(modulePath);
            return true;
        } catch (error) {
            return false;
        }
    };

    // Функция для получения детального статуса инициализации
    window.getGlobeStatus = function() {
        const scriptsReady = areGlobeScriptsReady();
        const instanceReady = isGlobeInstanceReady();

        return {
            isInitialized: window.GlobeInitializer.isInitialized,
            dependenciesLoaded: window.GlobeInitializer.dependenciesLoaded,
            initAttempts: window.GlobeInitializer.initAttempts,
            scriptsReady: scriptsReady,
            instanceReady: instanceReady,
            fullyReady: scriptsReady && instanceReady,
            availableFunctions: {
                createGlobe: typeof window.createGlobe === 'function',
                addTestParticipant: typeof window.addTestParticipant === 'function',
                safeAddTestParticipant: typeof window.safeAddTestParticipant === 'function',
                removeParticipant: typeof window.removeParticipant === 'function',
                clear: typeof window.clear === 'function',
                centerOn: typeof window.centerOn === 'function'
            },
            globeInstances: window.globeInstances ? window.globeInstances.size : 0,
            globeState: instanceReady ? window.globeInstances.values().next().value.state : null
        };
    };

    // Безопасная функция добавления участника
    window.safeAddTestParticipant = function(participant) {
        try {
            // Проверяем только готовность скриптов, не экземпляра глобуса
            if (!areGlobeScriptsReady()) {
                console.error('Скрипты глобуса не готовы. Вызовите initializeGlobeScripts() сначала');
                return false;
            }

            // Если экземпляр глобуса не создан, создаем его автоматически
            if (!isGlobeInstanceReady()) {
                console.log('Экземпляр глобуса не найден, создаем автоматически...');
                if (typeof createGlobe === 'function') {
                    const success = createGlobe('globe-container', {
                        width: 800,
                        height: 600,
                        backgroundColor: '#000011',
                        autoRotate: true,
                        enableMouseControls: true
                    });

                    if (!success) {
                        console.error('Не удалось создать экземпляр глобуса');
                        return false;
                    }

                    console.log('Экземпляр глобуса создан автоматически');
                } else {
                    console.error('Функция createGlobe не доступна');
                    return false;
                }
            }

            // Теперь добавляем участника
            return window.addTestParticipant(participant);
        } catch (error) {
            console.error('Ошибка в safeAddTestParticipant:', error);
            return false;
        }
    };

    // Функция для ручной инициализации (вызывается из Blazor)
    window.initializeGlobeScripts = async function() {
        if (window.GlobeInitializer.isInitialized) {
            console.log('✅ Скрипты уже инициализированы');
            return true;
        }

        console.log('🚀 Ручная инициализация скриптов глобуса из Blazor');
        return await initializeGlobeScripts();
    };

    // Автоматическая инициализация при загрузке страницы
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeGlobeScripts);
    } else {
        initializeGlobeScripts();
    }

    console.log('✅ Инициализатор скриптов глобуса загружен');

})();