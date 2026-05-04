// wwwroot/js/community-globe.js
//import * as THREE from './libs/three.module.js';
//import { OrbitControls } from './libs/OrbitControls.js';
import { DEFAULT_LABEL_PIXEL_HEIGHT, calculateLabelScaleForCamera } from './label-scale.js';

// Глобальные переменные для библиотек
let THREE, OrbitControls;

// Функция для загрузки скрипта
async function loadScript(src) {
    return new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.type = 'module';
        script.onload = resolve;
        script.onerror = () => reject(new Error(`Failed to load script: ${src}`));
        script.src = src;
        document.head.appendChild(script);
    });
}

// Загружаем зависимости асинхронно при инициализации модуля
async function initializeDependencies() {
    try {
        console.log('🔄 Загрузка зависимостей...');

        // Загружаем Three.js
        const threeModule = await import('./libs/three.module.js');
        THREE = threeModule.default || threeModule;
        console.log('✅ Three.js загружен:', THREE.REVISION);

        // Загружаем OrbitControls
        const controlsModule = await import('./libs/OrbitControls.js');
        OrbitControls = controlsModule.OrbitControls;
        console.log('✅ OrbitControls загружен');

        return true;
    } catch (error) {
        console.error('❌ Ошибка загрузки зависимостей:', error);
        return false;
    }
}

// Инициализируем зависимости
let dependenciesLoaded = false;

initializeDependencies().then(success => {
    dependenciesLoaded = success;
    if (success) {
        console.log('🎉 Все зависимости загружены успешно');
    } else {
        console.error('💥 Не удалось загрузить зависимости');
    }
});

// Глобальный реестр экземпляров глобуса
const globeInstances = new Map();

/**
 * Класс для создания и управления интерактивным 3D глобусом сообщества
 * Поддерживает добавление/удаление участников, настройку освещения,
 * управление камерой и визуализацию географических данных
 */
class CommunityGlobe {
    /**
     * Конструктор 3D глобуса
     * @param {string} containerId - ID HTML элемента-контейнера
     * @param {Object} options - Настройки глобуса
     */
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.container = null; // Будет инициализирован позже

        const defaultOptions = {
            width: 800,
            height: 600,
            backgroundColor: '#000011',
            atmosphereColor: '#00aaff',
            atmosphereOpacity: 0.2,
            participantPointSize: 0.06,
            participantPointColor: '#24dce7',
            participantPointOffset: 0.02, // Расстояние точек от поверхности глобуса
            participantMarkerOpacity: 0.72,
            participantMarkerHeight: 0.06,
            participantMarkerTipHeight: 0.16,
            participantMarkerRadius: 0.018,
            participantMarkerFacets: 4,
            participantMarkerLabelGap: 0.038,
            participantMarkerRippleIntervalMs: 2000,
            participantMarkerRippleDurationMs: 500,
            participantMarkerRippleRadius: 0.16,
            participantMarkerRippleWidth: 0.018,
            participantMarkerRippleWaveCount: 2,
            participantMarkerTiltStartDistance: 2.2,
            participantMarkerTiltFullDistance: 1.1,
            participantMarkerMaxTiltDegrees: 42,
            participantMarkerReferenceDistance: 2.6,
            participantMarkerMinScale: 0.35,
            participantMarkerMaxScale: 1.2,
<<<<<<< issue-22-e21d3bae3760
            participantMarkerLabelSideOffset: 0.02,
            participantMarkerLabelUpOffset: 0.055,
            participantMarkerLabelCloseLift: 0.06,
            participantLabelLiftDistance: 1.1,
=======
            participantLabelLoweringDistance: 1.1,
            participantLabelHiddenOpacity: 0.12,
            participantLabelHorizonFade: 0.25,
>>>>>>> master
            preserveDrawingBuffer: false,
            highlightedPointColor: '#e0fcff',
            autoRotate: true,
            autoRotateSpeed: 0.1,
            autoRotateResumeDelay: 3000,
            enableMouseControls: true,
            enableZoom: true,
            minZoom: 1.1,
            maxZoom: 4.0,
            cameraNearPlane: 0.02,
            earthRadius: 1,
            cloudsRadius: 1.01,
            atmosphereRadius: 1.05,
            cameraSurfaceClearance: 0.08,
            cameraZoomInMinSpeed: 0.16,
            cameraZoomInMaxSpeed: 0.9,
            cameraZoomOutSpeed: 1.15,
            cameraZoomSlowdownDistance: 1.1,
            levelOfDetail: 2,
            earthTextureUrl: "/_content/ZealousMindedPeopleGeo/assets/earth/8k_earth_daymap.jpg",
            normalTextureUrl: "/_content/ZealousMindedPeopleGeo/assets/earth/8k_earth_normal_map.tif",
            specularTextureUrl: "/_content/ZealousMindedPeopleGeo/assets/earth/8k_earth_specular_map.tif",
            cloudsTextureUrl: "/_content/ZealousMindedPeopleGeo/assets/earth/8k_earth_clouds.jpg",
            enableClouds: true,
            cloudsOpacity: 0.1,
            cloudsSpeed: 0.01,
            enableAtmosphereGlow: true,
            countryPointColor: '#ffffff',
            countryPointSize: 0.1,
            countryLineColor: '#444444',
            countryLineWidth: 0.5,
            // Настройки освещения
            sunLightIntensity: 3.0,
            sunLightColor: '#ffffff',
            ambientLightIntensity: 4,
            ambientLightColor: '#404040',
            atmosphereLightIntensity: 1,
            atmosphereLightColor: '#00aaff',
        };
        this.options = { ...defaultOptions };
        Object.entries(options || {}).forEach(([key, value]) => {
            if (value !== undefined && value !== null) {
                this.options[key] = value;
            }
        });

        this.state = {
            isInitialized: false,
            isAutoRotating: this.options.autoRotate,
            isUserInteracting: false,
            isPointerOverGlobe: false,
            currentLod: this.options.levelOfDetail,
            participantCount: 0,
            countryCount: 0,
            cameraPosition: { x: 0, y: 0, z: 2.5 },
            cameraTarget: { x: 0, y: 0, z: 0 }
        };

        this.scene = null;
        this.camera = null;
        this.renderer = null;
        this.controls = null;
        this.earthGroup = null;
        this.atmosphere = null;
        this.clouds = null;
        this.participantPoints = [];
        this.participantMarkers = [];
        this.participantMarkerHitTargets = [];
        this.participantLabels = [];
        this.hoveredParticipantMarker = null;
        this.labelTargetPixelHeight = DEFAULT_LABEL_PIXEL_HEIGHT;
        this.countryPolygons = [];
        this.raycaster = null;
        this.mouse = { x: 0, y: 0 };
        this.animationId = null;
        this.clock = null;
        this.pointMetadata = new Map();
        this.autoRotationResumeTimer = null;
        this.callbacks = { // Инициализация callbacks
            onGlobeReady: null,
            onError: null,
            onParticipantClick: null
        };

        this.initializeAutoRotationInteractionState();

        console.log(`🔧 Создание глобуса для контейнера: ${containerId}`);
        this.init();
    }

    /**
     * Асинхронная инициализация глобуса
     * Создает сцену, камеру, освещение, загружает текстуры
     * @returns {Promise<void>}
     */
    async init() {
        try {
            // Ждем загрузки зависимостей
            let attempts = 0;
            while (!dependenciesLoaded && attempts < 50) {
                await new Promise(resolve => setTimeout(resolve, 100));
                attempts++;
            }

            if (!dependenciesLoaded) {
                throw new Error('Не удалось загрузить зависимости Three.js');
            }

            if (!this.isWebGLSupported()) {
                throw new Error('WebGL is not supported in this browser');
            }

            this.setupScene();
            this.createEarth();
            this.createAtmosphere();
            this.createClouds();
            this.setupLighting();
            this.setupControls();
            this.setupEventListeners();

            this.state.isInitialized = true;

            console.log(`🌍 Глобус ${this.containerId} инициализирован и готов к работе`);
            console.log(`📊 Состояние глобуса ${this.containerId}:`, this.state);

            this.animate();
            
            // Вызываем callback после задержки
            setTimeout(() => {
                if (this.callbacks.onGlobeReady) {
                    console.log(`📞 Вызов callback onGlobeReady для ${this.containerId}`);
                    this.callbacks.onGlobeReady(this.state);
                } else {
                    console.log(`⚠️ Callback не установлен для ${this.containerId}`);
                }
            }, 200);
        } catch (error) {
            console.error('Failed to initialize globe:', error);
            if (this.callbacks.onError) {
                this.callbacks.onError(error.message);
            }
            throw error;
        }
    }

    /**
     * Проверяет поддержку WebGL в браузере
     * @returns {boolean} true если WebGL поддерживается
     */
    isWebGLSupported() {
        try {
            const canvas = document.createElement('canvas');
            return !!(window.WebGLRenderingContext &&
                (canvas.getContext('webgl') || canvas.getContext('experimental-webgl')));
        } catch (e) {
            return false;
        }
    }

    getContainer(maxAttempts = 50, delayMs = 100) {
        for (let attempt = 1; attempt <= maxAttempts; attempt++) {
            const container = document.getElementById(this.containerId);
            if (container) {
                console.log(`Container found on attempt ${attempt}`);
                return container;
            }

            if (attempt < maxAttempts) {
                // Ждем перед следующей попыткой
                // Используем промис для асинхронного ожидания
                const startTime = Date.now();
                while (Date.now() - startTime < delayMs) {
                    // Busy wait для синхронного выполнения
                }
            }
        }

        console.error(`Container with id '${this.containerId}' not found after ${maxAttempts} attempts`);
        return null;
    }

    setupScene() {
        console.log('🔧 setupScene: начало для контейнера', this.containerId);
        console.log('🔍 Проверка THREE.js:', typeof THREE);
        if (typeof THREE === 'undefined') {
            console.error('❌ Three.js не загружен. setupScene не может быть выполнен.');
            throw new Error('Three.js не загружен. setupScene не может быть выполнен.');
        }

        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(this.options.backgroundColor);

        const aspect = this.options.width / this.options.height;
        this.camera = new THREE.PerspectiveCamera(75, aspect, this.getPositiveNumber(this.options.cameraNearPlane, 0.02), 1000);
        this.camera.position.set(
            this.state.cameraPosition.x,
            this.state.cameraPosition.y,
            this.state.cameraPosition.z
        );

        this.renderer = new THREE.WebGLRenderer({
            antialias: true,
            preserveDrawingBuffer: Boolean(this.options.preserveDrawingBuffer)
        });
        this.renderer.setSize(this.options.width, this.options.height);
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.shadowMap.enabled = true;
        this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;

        console.log('🔍 Поиск контейнера:', this.containerId);
        this.container = this.getContainer();
        console.log('📦 Контейнер найден:', this.container);
        console.log('📦 Контейнер DOM элемент:', this.container?.tagName, this.container?.id);

        if (!this.container) {
            console.error('❌ Контейнер не найден для ID:', this.containerId);
            throw new Error(`Container with id '${this.containerId}' not found after multiple attempts`);
        }

        console.log('🧹 Селективная очистка старых Three.js canvas элементов');
        console.log('📦 Контейнер перед очисткой:', this.container?.tagName, this.container?.id);
        console.log('📦 Дочерних элементов в контейнере:', this.container?.childNodes?.length || 0);

        // Удаляем только старые canvas элементы Three.js, но сохраняем другие элементы Blazor
        if (this.container && this.container.childNodes) {
            const canvasElements = this.container.querySelectorAll('canvas');
            console.log('🧹 Найдено canvas элементов для удаления:', canvasElements.length);

            canvasElements.forEach((canvas, index) => {
                console.log(`🧹 Удаление canvas элемента ${index}:`, canvas);
                this.container.removeChild(canvas);
            });

            console.log('✅ Старые canvas элементы удалены');
        }

        console.log('📦 Контейнер готов для добавления нового renderer');

        console.log('➕ Добавление renderer в контейнер');
        this.container.appendChild(this.renderer.domElement);
        console.log('✅ Renderer добавлен в контейнер', this.containerId);

        this.earthGroup = new THREE.Group();
        this.scene.add(this.earthGroup);

        this.raycaster = new THREE.Raycaster();
        this.raycaster.params.Points.threshold = 0.1;

        this.clock = new THREE.Clock();
        console.log('🔧 setupScene: завершено для контейнера', this.containerId);
    }

    createEarth() {
        const earthGeometry = new THREE.SphereGeometry(1, 128, 128); // Увеличиваем количество сегментов
        const textureLoader = new THREE.TextureLoader();

        console.log('🌍 Загрузка текстур глобуса:');
        console.log('Основная текстура:', this.options.earthTextureUrl);
        console.log('Карта нормалей:', this.options.normalTextureUrl);
        console.log('Карта спекуляции:', this.options.specularTextureUrl);

        // Загружаем текстуры асинхронно для избежания блокировок
        const loadTexture = (url) => {
            if (!url) return null;
            try {
                console.log('Загрузка текстуры:', url);
                return textureLoader.load(url);
            } catch (error) {
                console.error('Ошибка загрузки текстуры:', url, error);
                return null;
            }
        };

        const earthMaterial = new THREE.MeshStandardMaterial({
            map: loadTexture(this.options.earthTextureUrl),
            normalMap: loadTexture(this.options.normalTextureUrl),
            roughnessMap: loadTexture(this.options.specularTextureUrl),
            roughness: 0.8,
            metalness: 0.1
        });

        const earth = new THREE.Mesh(earthGeometry, earthMaterial);
        this.earthGroup.add(earth);
        this.earthRotation = 0;
    }

    createAtmosphere() {
        if (!this.options.enableAtmosphereGlow) return;

        const atmosphereGeometry = new THREE.SphereGeometry(1.05, 128, 128);
        const atmosphereMaterial = new THREE.MeshPhongMaterial({
            color: this.options.atmosphereColor,
            transparent: true,
            opacity: this.options.atmosphereOpacity,
            side: THREE.BackSide
        });

        this.atmosphere = new THREE.Mesh(atmosphereGeometry, atmosphereMaterial);
        this.earthGroup.add(this.atmosphere);
    }

    createClouds() {
        if (!this.options.enableClouds || !this.options.cloudsTextureUrl) return;

        const cloudsGeometry = new THREE.SphereGeometry(1.01, 128, 128);
        const textureLoader = new THREE.TextureLoader();

        console.log('☁️ Загрузка текстуры облаков:', this.options.cloudsTextureUrl);

        const loadCloudTexture = (url) => {
            if (!url) return null;
            try {
                console.log('Загрузка облаков:', url);
                return textureLoader.load(url);
            } catch (error) {
                console.error('Ошибка загрузки облаков:', url, error);
                return null;
            }
        };

        const cloudsMaterial = new THREE.MeshPhongMaterial({
            map: loadCloudTexture(this.options.cloudsTextureUrl),
            transparent: true,
            opacity: this.options.cloudsOpacity
        });

        this.clouds = new THREE.Mesh(cloudsGeometry, cloudsMaterial);
        this.earthGroup.add(this.clouds);
        this.cloudRotation = 0;
    }

    setupLighting() {
        console.log('💡 Настройка освещения глобуса:');
        console.log('Яркость солнца:', this.options.sunLightIntensity);
        console.log('Цвет солнца:', this.options.sunLightColor);
        console.log('Яркость окружения:', this.options.ambientLightIntensity);
        console.log('Цвет окружения:', this.options.ambientLightColor);

        // Преобразуем цвет из hex в Color
        const sunColor = new THREE.Color(this.options.sunLightColor);
        const ambientColor = new THREE.Color(this.options.ambientLightColor);
        const atmosphereColor = new THREE.Color(this.options.atmosphereLightColor);

        const sunLight = new THREE.DirectionalLight(sunColor, this.options.sunLightIntensity);
        sunLight.position.set(5, 3, 5);
        sunLight.castShadow = true;
        this.scene.add(sunLight);

        const ambientLight = new THREE.AmbientLight(ambientColor, this.options.ambientLightIntensity);
        this.scene.add(ambientLight);

        const atmosphereLight = new THREE.PointLight(atmosphereColor, this.options.atmosphereLightIntensity, 100);
        atmosphereLight.position.set(0, 0, 3);
        this.scene.add(atmosphereLight);
    }

    setupControls() {
        if (!this.options.enableMouseControls) return;

        if (typeof OrbitControls === 'undefined') {
            console.error('OrbitControls не загружен');
            return;
        }

        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.enableZoom = this.options.enableZoom;
        this.controls.enablePan = false;
        this.controls.target.set(0, 0, 0);
        if (this.controls.cursor) {
            this.controls.cursor.set(0, 0, 0);
        }
        if ('minTargetRadius' in this.controls) {
            this.controls.minTargetRadius = 0;
            this.controls.maxTargetRadius = 0;
        }
        this.updateCameraControlLimits();
        this.controls.zoomSpeed = this.calculateCameraZoomSpeed(this.camera.position.length(), false);
        this.controls.autoRotate = this.state.isAutoRotating;
        this.controls.autoRotateSpeed = this.options.autoRotateSpeed;
        this.controls.addEventListener('start', () => this.pauseAutoRotationForInteraction());
        this.controls.addEventListener('end', () => this.scheduleAutoRotationResume());
    }

    setupEventListeners() {
        const globeElement = this.renderer.domElement;
        globeElement.addEventListener('click', (event) => this.onMouseClick(event));
        globeElement.addEventListener('mousemove', (event) => this.onMouseMove(event));
        globeElement.addEventListener('pointerenter', () => this.handleGlobePointerEnter());
        globeElement.addEventListener('pointerleave', () => this.handleGlobePointerLeave());
        globeElement.addEventListener('pointerup', (event) => this.handleGlobePointerRelease(event));
        globeElement.addEventListener('pointercancel', () => this.handleGlobePointerLeave());
        globeElement.addEventListener('wheel', (event) => this.updateCameraZoomSpeedForWheel(event), { capture: true, passive: true });
        window.addEventListener('resize', () => this.onWindowResize());
    }

    onMouseClick(event) {
        this.updateMousePosition(event);

        const marker = this.getIntersectedParticipantMarker();
        const metadata = marker?.userData?.participant;
        if (metadata && this.callbacks.onParticipantClick) {
            this.callbacks.onParticipantClick(metadata);
        }
    }

    onMouseMove(event) {
        this.handleGlobePointerEnter();
        this.updateMousePosition(event);

        const marker = this.getIntersectedParticipantMarker();
        this.setHoveredParticipantMarker(marker);
        if (this.renderer?.domElement) {
            this.renderer.domElement.style.cursor = marker ? 'pointer' : '';
        }
    }

    updateMousePosition(event) {
        const rect = this.renderer.domElement.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    }

    getIntersectedParticipantMarker() {
        if (!this.raycaster || !this.camera || this.participantMarkerHitTargets.length === 0) {
            return null;
        }

        this.raycaster.setFromCamera(this.mouse, this.camera);
        const intersects = this.raycaster.intersectObjects(this.participantMarkerHitTargets, false);
        if (intersects.length === 0) {
            return null;
        }

        return intersects[0].object.userData.participantMarker || null;
    }

    setHoveredParticipantMarker(marker) {
        if (this.hoveredParticipantMarker === marker) {
            return;
        }

        if (this.hoveredParticipantMarker) {
            this.hoveredParticipantMarker.userData.isHovered = false;
            this.resetParticipantMarkerRipples(this.hoveredParticipantMarker);
        }

        this.hoveredParticipantMarker = marker;

        if (marker) {
            marker.userData.isHovered = true;
            const rippleGroup = marker.userData.rippleGroup;
            if (rippleGroup) {
                rippleGroup.visible = true;
                rippleGroup.userData.startedAt = this.getAnimationTimeMs();
            }
        }
    }

    onWindowResize() {
        if (!this.container) {
            this.container = this.getContainer();
        }

        if (this.container) {
            const rect = this.container.getBoundingClientRect();
            this.options.width = rect.width || this.options.width;
            this.options.height = rect.height || this.options.height;

            this.camera.aspect = this.options.width / this.options.height;
            this.camera.updateProjectionMatrix();
            this.renderer.setSize(this.options.width, this.options.height);
        }
    }

    /**
      * Добавляет массив участников на глобус
      * @param {Array} participants - Массив объектов участников с координатами
      */
     addParticipants(participants) {
         console.log(`🎯 addParticipants вызван для контейнера ${this.containerId}:`, participants?.length || 0, 'участников');

         if (!this.state.isInitialized) {
             console.log(`❌ Глобус ${this.containerId} не инициализирован, планирую повтор через 500ms`);
             setTimeout(() => this.addParticipants(participants), 500);
             return false;
         }

         this.clearParticipants();
         if (!participants || participants.length === 0) {
             console.log(`📊 Нет участников для добавления на глобус ${this.containerId}`);
             return true;
         }

         try {
            let markerIndex = 0;

            participants.forEach((participant, index) => {
                // Валидация координат
                if (typeof participant.latitude !== 'number' || typeof participant.longitude !== 'number' ||
                    isNaN(participant.latitude) || isNaN(participant.longitude)) {
                    console.warn(`⚠️ Пропускаем участника ${participant.name}: некорректные координаты (${participant.latitude}, ${participant.longitude})`);
                    return;
                }

                const marker = this.createParticipantMarker(participant, markerIndex);
                this.earthGroup.add(marker);
                this.participantMarkers.push(marker);
                this.participantPoints.push(marker); // Обратная совместимость с отладочными API
                this.pointMetadata.set(`participant_${markerIndex}`, participant);
                markerIndex++;
            });

            this.state.participantCount = markerIndex;
            this.updateParticipantMarkerTransforms();
            this.updateParticipantLabelBillboards();

            console.log(`🎯 Создано ${markerIndex} 3D меток участников`);
            console.log(`✅ Добавлено ${markerIndex} участников на глобус`);
            console.log(`📊 Общее количество объектов в earthGroup: ${this.earthGroup.children.length}`);

            // Принудительно обновляем рендер для немедленного отображения точек
            if (this.renderer && this.scene && this.camera) {
                console.log(`🔄 Принудительный рендер сцены для контейнера ${this.containerId}`);
                this.renderer.render(this.scene, this.camera);
            }

            console.log(`📈 Финальное состояние для контейнера ${this.containerId}:`);
            console.log(`   - Меток участников: ${this.state.participantCount}`);
            console.log(`   - Объектов в сцене: ${this.scene.children.length}`);
            console.log(`   - Объектов в earthGroup: ${this.earthGroup.children.length}`);

            return true;
        } catch (error) {
            console.error('Error adding participants:', error);
            return false;
        }
    }

    /**
     * Очищает всех участников с глобуса
     * Удаляет точки участников и очищает метаданные
     */
    clearParticipants() {
        this.participantMarkers.forEach(marker => {
            this.earthGroup.remove(marker);
            this.disposeObject3D(marker);
        });
        this.participantPoints = [];
        this.participantMarkers = [];
        this.participantMarkerHitTargets = [];
        this.participantLabels = [];
        this.hoveredParticipantMarker = null;
        this.pointMetadata.clear();
        this.state.participantCount = 0;
        console.log('🧹 Очищены все метки участников');
    }

    disposeObject3D(object) {
        object.traverse(child => {
            if (child.geometry) {
                child.geometry.dispose();
            }

            if (child.material) {
                this.disposeMaterial(child.material);
            }
        });
    }

    disposeMaterial(material) {
        if (Array.isArray(material)) {
            material.forEach(item => this.disposeMaterial(item));
            return;
        }

        if (material.map) {
            material.map.dispose();
        }

        material.dispose();
    }

    latLngToVector3(lat, lng, radius = 1) {
        const phi = (90 - lat) * (Math.PI / 180);
        const theta = (lng + 180) * (Math.PI / 180);
        return {
            x: -(radius * Math.sin(phi) * Math.cos(theta)),
            z: (radius * Math.sin(phi) * Math.sin(theta)),
            y: (radius * Math.cos(phi))
        };
    }

    createCircleTexture() {
        const canvas = document.createElement('canvas');
        canvas.width = 64;
        canvas.height = 64;
        const ctx = canvas.getContext('2d');
        const centerX = canvas.width / 2;
        const centerY = canvas.height / 2;
        const radius = canvas.width / 2;

        const gradient = ctx.createRadialGradient(centerX, centerY, 0, centerX, centerY, radius);
        gradient.addColorStop(0, 'rgba(255, 255, 255, 1)');
        gradient.addColorStop(0.2, 'rgba(255, 255, 255, 1)');
        gradient.addColorStop(0.4, 'rgba(255, 255, 255, 0.8)');
        gradient.addColorStop(1, 'rgba(255, 255, 255, 0)');

        ctx.fillStyle = gradient;
        ctx.beginPath();
        ctx.arc(centerX, centerY, radius, 0, 2 * Math.PI);
        ctx.fill();

        return new THREE.CanvasTexture(canvas);
    }

    createParticipantMarker(participant, index) {
        const dimensions = this.getParticipantMarkerDimensions();
        const radius = 1 + this.options.participantPointOffset;
        const position = this.latLngToVector3(participant.latitude, participant.longitude, radius);
        const normal = new THREE.Vector3(position.x, position.y, position.z).normalize();
        const marker = new THREE.Group();
        const visual = new THREE.Group();

        marker.position.set(position.x, position.y, position.z);
        marker.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), normal);
        marker.userData = {
            participant,
            participantIndex: index,
            normal,
            dimensions,
            visual,
            isHovered: false
        };

        marker.add(visual);

        const color = new THREE.Color(participant.markerColor || this.options.participantPointColor);
        const opacity = this.getClampedNumber(this.options.participantMarkerOpacity, 0.72, 0.05, 1);
        const bodyMaterial = new THREE.MeshPhongMaterial({
            color,
            transparent: true,
            opacity,
            shininess: 90,
            depthWrite: false,
            side: THREE.DoubleSide
        });
        const tipMaterial = bodyMaterial.clone();
        const edgeMaterial = new THREE.LineBasicMaterial({
            color: this.options.highlightedPointColor || '#e0fcff',
            transparent: true,
            opacity: Math.min(1, opacity + 0.18),
            depthWrite: false
        });

        const markerFacets = this.getParticipantMarkerFacetCount();
        const tipGeometry = new THREE.ConeGeometry(dimensions.radius, dimensions.tipHeight, markerFacets, 1, false);
        tipGeometry.rotateX(Math.PI);
        tipGeometry.translate(0, dimensions.tipHeight / 2, 0);
        const tip = new THREE.Mesh(tipGeometry, tipMaterial);

        const bodyGeometry = new THREE.CylinderGeometry(
            dimensions.radius * 0.92,
            dimensions.radius,
            dimensions.bodyHeight,
            markerFacets,
            1,
            false
        );
        bodyGeometry.translate(0, dimensions.tipHeight + dimensions.bodyHeight / 2, 0);
        const body = new THREE.Mesh(bodyGeometry, bodyMaterial);

        const bodyEdges = new THREE.LineSegments(new THREE.EdgesGeometry(bodyGeometry), edgeMaterial.clone());
        const tipEdges = new THREE.LineSegments(new THREE.EdgesGeometry(tipGeometry), edgeMaterial);

        [tip, body].forEach(mesh => {
            mesh.userData.participantMarker = marker;
            mesh.userData.participant = participant;
            this.participantMarkerHitTargets.push(mesh);
        });

        visual.add(tip);
        visual.add(body);
        visual.add(bodyEdges);
        visual.add(tipEdges);

        const label = this.createParticipantLabelMesh(participant, dimensions);
        if (label) {
            marker.add(label);
            marker.userData.label = label;
            this.participantLabels.push(label);
        }

        const rippleGroup = this.createParticipantRippleGroup(participant, dimensions);
        marker.userData.rippleGroup = rippleGroup;
        marker.add(rippleGroup);

        return marker;
    }

    createParticipantLabelMesh(participant, dimensions) {
        if (!participant.name || participant.name.trim() === '') {
            console.warn(`⚠️ Пропускаем метку для участника без имени`);
            return null;
        }

        try {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            const fontSize = 24;
            const scale = 2;
            const paddingX = 14;
            const paddingY = 8;

            ctx.font = `600 ${fontSize}px Arial`;
            const textWidth = Math.ceil(ctx.measureText(participant.name).width);
            canvas.width = (textWidth + paddingX * 2) * scale;
            canvas.height = (fontSize + paddingY * 2) * scale;
            ctx.scale(scale, scale);

            ctx.font = `600 ${fontSize}px Arial`;
            ctx.textBaseline = 'middle';
            ctx.shadowColor = 'rgba(0, 0, 0, 0.9)';
            ctx.shadowBlur = 7;
            ctx.lineWidth = 4;
            ctx.strokeStyle = 'rgba(0, 0, 0, 0.78)';
            ctx.strokeText(participant.name, paddingX, canvas.height / (2 * scale));
            ctx.fillStyle = 'rgba(245, 255, 255, 0.98)';
            ctx.fillText(participant.name, paddingX, canvas.height / (2 * scale));

            const texture = new THREE.CanvasTexture(canvas);
            texture.needsUpdate = true;

            const material = new THREE.MeshBasicMaterial({
                map: texture,
                transparent: true,
                depthTest: false,
                depthWrite: false,
                side: THREE.DoubleSide
            });
            const label = new THREE.Mesh(new THREE.PlaneGeometry(1, 1), material);
            const cameraDistance = this.camera?.position?.length?.() ?? this.getPositiveNumber(this.options.participantMarkerReferenceDistance, 2.6);
            const markerScale = this.calculateParticipantMarkerDistanceScale(cameraDistance);
            const initialOffset = this.calculateParticipantLabelOffset(cameraDistance, dimensions, markerScale);
            label.position.set(initialOffset.side, initialOffset.depth, initialOffset.lift);
            label.frustumCulled = false;
            label.renderOrder = 10;
            label.userData.labelAspectRatio = canvas.width / canvas.height;
            label.userData.targetPixelHeight = this.labelTargetPixelHeight;
            this.updateParticipantLabelScale(label);

            return label;
        } catch (error) {
            console.error(`❌ Ошибка создания метки для участника ${participant.name}:`, error);
            return null;
        }
    }

    createParticipantRippleGroup(participant, dimensions) {
        const group = new THREE.Group();
        const timing = this.getParticipantRippleTiming(participant);
        const waveCount = Math.max(1, Math.round(this.getPositiveNumber(
            participant.rippleWaveCount ?? this.options.participantMarkerRippleWaveCount,
            2
        )));
        const color = new THREE.Color(participant.rippleColor || this.options.participantPointColor);

        group.visible = false;
        group.userData = {
            timing,
            startedAt: 0,
            maxOpacity: 0.62
        };

        for (let waveIndex = 0; waveIndex < waveCount; waveIndex++) {
            const innerRadius = Math.max(0.001, dimensions.rippleRadius - dimensions.rippleWidth);
            const geometry = new THREE.RingGeometry(innerRadius, dimensions.rippleRadius, 64);
            geometry.rotateX(-Math.PI / 2);
            geometry.translate(0, 0.003 + waveIndex * 0.0008, 0);

            const material = new THREE.MeshBasicMaterial({
                color,
                transparent: true,
                opacity: 0,
                depthWrite: false,
                side: THREE.DoubleSide
            });
            const wave = new THREE.Mesh(geometry, material);
            wave.visible = false;
            wave.scale.setScalar(0.001);
            wave.userData.phaseOffsetMs = waveIndex * (timing.durationMs / (waveCount + 1));
            group.add(wave);
        }

        return group;
    }

    getParticipantMarkerDimensions() {
        const size = this.getClampedNumber(this.options.participantPointSize, 0.06, 0.04, 2);
        const scale = this.clamp(Math.sqrt(size / 0.2), 0.55, 3);

        return {
            radius: this.getPositiveNumber(this.options.participantMarkerRadius, 0.018) * scale,
            bodyHeight: this.getPositiveNumber(this.options.participantMarkerHeight, 0.06) * scale,
            tipHeight: this.getPositiveNumber(this.options.participantMarkerTipHeight, 0.16) * scale,
            labelGap: this.getPositiveNumber(this.options.participantMarkerLabelGap, 0.038) * scale,
            rippleRadius: this.getPositiveNumber(this.options.participantMarkerRippleRadius, 0.16) * scale,
            rippleWidth: this.getPositiveNumber(this.options.participantMarkerRippleWidth, 0.018) * scale
        };
    }

    getParticipantMarkerFacetCount() {
        return Math.round(this.getClampedNumber(this.options.participantMarkerFacets, 4, 4, 12));
    }

    getParticipantRippleTiming(participant = {}) {
        const intervalMs = this.getPositiveNumber(
            participant.rippleIntervalMs ?? participant.markerRippleIntervalMs ?? this.options.participantMarkerRippleIntervalMs,
            2000
        );
        const durationMs = this.getPositiveNumber(
            participant.rippleDurationMs ?? participant.markerRippleDurationMs ?? this.options.participantMarkerRippleDurationMs,
            500
        );

        return {
            intervalMs,
            durationMs: Math.min(durationMs, intervalMs)
        };
    }

    calculateParticipantMarkerTiltAmount(cameraDistance) {
        const startDistance = this.getPositiveNumber(this.options.participantMarkerTiltStartDistance, 2.2);
        const fullDistance = this.getPositiveNumber(this.options.participantMarkerTiltFullDistance, 1.1);
        const maxTiltRadians = this.getClampedNumber(this.options.participantMarkerMaxTiltDegrees, 42, 0, 80) * Math.PI / 180;

        if (startDistance <= fullDistance || cameraDistance >= startDistance) {
            return 0;
        }

        const progress = this.clamp((startDistance - cameraDistance) / (startDistance - fullDistance), 0, 1);
        return maxTiltRadians * progress;
    }

    getCameraCollisionRadius() {
        const earthRadius = this.getPositiveNumber(this.options.earthRadius, 1);
        const cloudsRadius = this.options.enableClouds
            ? this.getPositiveNumber(this.options.cloudsRadius, 1.01)
            : earthRadius;
        const atmosphereRadius = this.options.enableAtmosphereGlow
            ? this.getPositiveNumber(this.options.atmosphereRadius, 1.05)
            : earthRadius;

        return Math.max(earthRadius, cloudsRadius, atmosphereRadius);
    }

    getCameraDistanceLimits() {
        const configuredMinDistance = this.getPositiveNumber(this.options.minZoom, 1.1);
        const surfaceClearance = this.getPositiveNumber(this.options.cameraSurfaceClearance, 0.08);
        const safeMinDistance = this.getCameraCollisionRadius() + surfaceClearance;
        const minDistance = Math.max(configuredMinDistance, safeMinDistance);
        const configuredMaxDistance = this.getPositiveNumber(this.options.maxZoom, 4);
        const maxDistance = Math.max(configuredMaxDistance, minDistance + 0.1);

        return {
            minDistance,
            maxDistance
        };
    }

    calculateCameraZoomSpeed(cameraDistance, isZoomingIn) {
        if (!isZoomingIn) {
            return this.getPositiveNumber(this.options.cameraZoomOutSpeed, 1.15);
        }

        const { minDistance } = this.getCameraDistanceLimits();
        const minSpeed = this.getPositiveNumber(this.options.cameraZoomInMinSpeed, 0.16);
        const maxSpeed = Math.max(minSpeed, this.getPositiveNumber(this.options.cameraZoomInMaxSpeed, 0.9));
        const slowdownDistance = this.getPositiveNumber(this.options.cameraZoomSlowdownDistance, 1.1);
        const progress = this.clamp((cameraDistance - minDistance) / slowdownDistance, 0, 1);

        return minSpeed + (maxSpeed - minSpeed) * progress;
    }

    updateCameraZoomSpeedForWheel(event) {
        if (!this.controls || !this.camera || !this.options.enableZoom || !event || event.deltaY === 0) {
            return;
        }

        this.controls.zoomSpeed = this.calculateCameraZoomSpeed(this.camera.position.length(), event.deltaY < 0);
    }

    updateCameraControlLimits() {
        if (!this.controls) return;

        const { minDistance, maxDistance } = this.getCameraDistanceLimits();
        this.controls.minDistance = minDistance;
        this.controls.maxDistance = maxDistance;
    }

    getClampedCameraDistance(distance) {
        const { minDistance, maxDistance } = this.getCameraDistanceLimits();
        return this.clamp(this.getPositiveNumber(distance, minDistance), minDistance, maxDistance);
    }

    getSafeCameraPosition(position) {
        const { minDistance, maxDistance } = this.getCameraDistanceLimits();
        const safePosition = new THREE.Vector3(
            this.getFiniteNumber(position?.x, 0),
            this.getFiniteNumber(position?.y, 0),
            this.getFiniteNumber(position?.z, minDistance)
        );

        if (safePosition.lengthSq() < 0.000001) {
            safePosition.set(0, 0, minDistance);
        }

        safePosition.setLength(this.clamp(safePosition.length(), minDistance, maxDistance));
        return safePosition;
    }

    applyCameraDistanceSafety() {
        if (!this.camera) return;

        const safePosition = this.getSafeCameraPosition(this.camera.position);
        if (safePosition.distanceToSquared(this.camera.position) <= 0.0000001) {
            return;
        }

        this.camera.position.copy(safePosition);
        this.camera.lookAt(0, 0, 0);

        if (this.controls) {
            this.controls.target.set(0, 0, 0);
        }
    }

    calculateParticipantMarkerDistanceScale(cameraDistance) {
        const referenceDistance = this.getPositiveNumber(this.options.participantMarkerReferenceDistance, 2.6);
        const minScale = this.getPositiveNumber(this.options.participantMarkerMinScale, 0.35);
        const maxScale = Math.max(minScale, this.getPositiveNumber(this.options.participantMarkerMaxScale, 1.2));

        return this.clamp(cameraDistance / referenceDistance, minScale, maxScale);
    }

    calculateParticipantLabelOffset(cameraDistance, dimensions, markerScale) {
        const safeScale = this.getPositiveNumber(markerScale, 1);
        const radius = this.getPositiveNumber(dimensions?.radius, 0);
        const tipHeight = this.getPositiveNumber(dimensions?.tipHeight, 0);
        const bodyHeight = this.getPositiveNumber(dimensions?.bodyHeight, 0);
        const labelGap = this.getPositiveNumber(dimensions?.labelGap, 0.01);
        const baseDepth = labelGap * safeScale;
        const topAnchor = (tipHeight + bodyHeight + labelGap) * safeScale;
        const { minDistance } = this.getCameraDistanceLimits();
        const liftDistance = this.getPositiveNumber(
            this.options.participantLabelLiftDistance ?? this.options.participantLabelLoweringDistance,
            1.1
        );
        const farProgress = this.clamp((cameraDistance - minDistance) / liftDistance, 0, 1);
        const closeProgress = 1 - farProgress;
        const sideOffset = (radius + this.getPositiveNumber(this.options.participantMarkerLabelSideOffset, 0.02)) * safeScale;
        const upOffset = this.getPositiveNumber(this.options.participantMarkerLabelUpOffset, 0.055) * safeScale;
        const closeLift = this.getPositiveNumber(this.options.participantMarkerLabelCloseLift, 0.06) * safeScale * closeProgress;

        return {
            side: sideOffset,
            depth: baseDepth + (topAnchor - baseDepth) * farProgress,
            lift: upOffset + closeLift
        };
    }

    calculateParticipantLabelAnchorHeight(cameraDistance, dimensions, markerScale) {
        return this.calculateParticipantLabelOffset(cameraDistance, dimensions, markerScale).depth;
    }

    getParticipantLabelTangentDirections(marker) {
        const fallbackSide = new THREE.Vector3(1, 0, 0);
        const fallbackLift = new THREE.Vector3(0, 0, 1);
        if (!this.camera || !marker) {
            return { side: fallbackSide, lift: fallbackLift };
        }

        const markerWorldPosition = marker.getWorldPosition(new THREE.Vector3());
        const projectedPosition = markerWorldPosition.clone().project(this.camera);
        const sideSign = Number.isFinite(projectedPosition.x) && projectedPosition.x >= 0 ? -1 : 1;
        const cameraRightWorld = new THREE.Vector3(1, 0, 0).applyQuaternion(this.camera.quaternion).multiplyScalar(sideSign);
        const inverseMarkerQuaternion = marker.getWorldQuaternion(new THREE.Quaternion()).invert();
        const localSide = cameraRightWorld.applyQuaternion(inverseMarkerQuaternion);
        localSide.y = 0;

        if (localSide.lengthSq() < 0.000001) {
            localSide.copy(fallbackSide).multiplyScalar(sideSign);
        } else {
            localSide.normalize();
        }

        const cameraUpWorld = new THREE.Vector3(0, 1, 0).applyQuaternion(this.camera.quaternion);
        const localLift = cameraUpWorld.applyQuaternion(inverseMarkerQuaternion);
        localLift.y = 0;
        localLift.addScaledVector(localSide, -localLift.dot(localSide));

        if (localLift.lengthSq() < 0.000001) {
            localLift.set(-localSide.z, 0, localSide.x);
        } else {
            localLift.normalize();
        }

        return {
            side: localSide,
            lift: localLift
        };
    }

    getAnimationTimeMs() {
        if (typeof performance !== 'undefined' && typeof performance.now === 'function') {
            return performance.now();
        }

        return Date.now();
    }

    getPositiveNumber(value, fallback) {
        const number = Number(value);
        return Number.isFinite(number) && number > 0 ? number : fallback;
    }

    getFiniteNumber(value, fallback) {
        const number = Number(value);
        return Number.isFinite(number) ? number : fallback;
    }

    getClampedNumber(value, fallback, min, max) {
        return this.clamp(this.getFiniteNumber(value, fallback), min, max);
    }

    clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    updateParticipantLabelScale(label) {
        if (!this.camera || !this.renderer || !label) return;

        const distanceToCamera = label.getWorldPosition(new THREE.Vector3()).distanceTo(this.camera.position);
        const rendererSize = this.renderer.getSize(new THREE.Vector2());
        const scale = calculateLabelScaleForCamera(
            distanceToCamera,
            this.camera.fov,
            rendererSize.height,
            label.userData.labelAspectRatio,
            label.userData.targetPixelHeight
        );

        label.scale.set(scale.width, scale.height, 1);
    }

    calculateParticipantLabelVisibilityOpacity(markerWorldPosition, cameraPosition) {
        const markerX = this.getFiniteNumber(markerWorldPosition?.x, 0);
        const markerY = this.getFiniteNumber(markerWorldPosition?.y, 0);
        const markerZ = this.getFiniteNumber(markerWorldPosition?.z, 0);
        const cameraX = this.getFiniteNumber(cameraPosition?.x, 0);
        const cameraY = this.getFiniteNumber(cameraPosition?.y, 0);
        const cameraZ = this.getFiniteNumber(cameraPosition?.z, 0);
        const markerLength = Math.hypot(markerX, markerY, markerZ);
        const cameraLength = Math.hypot(cameraX, cameraY, cameraZ);
        const visibleOpacity = 1;
        const hiddenOpacity = this.getClampedNumber(this.options.participantLabelHiddenOpacity, 0.12, 0, visibleOpacity);
        const fadeBand = this.getClampedNumber(this.options.participantLabelHorizonFade, 0.25, 0.001, 1);
        const earthRadius = this.getPositiveNumber(this.options.earthRadius, 1);

        if (markerLength <= 0.000001 || cameraLength <= 0.000001) {
            return visibleOpacity;
        }

        const facing = (markerX * cameraX + markerY * cameraY + markerZ * cameraZ) / (markerLength * cameraLength);
        const horizonFacing = this.calculateParticipantLabelHorizonFacing(markerLength, cameraLength, earthRadius);
        const visibility = facing - horizonFacing;
        if (visibility >= fadeBand) {
            return visibleOpacity;
        }
        if (visibility <= -fadeBand) {
            return hiddenOpacity;
        }

        const progress = (visibility + fadeBand) / (fadeBand * 2);
        const easedProgress = progress * progress * (3 - 2 * progress);
        return hiddenOpacity + (visibleOpacity - hiddenOpacity) * easedProgress;
    }

    calculateParticipantLabelHorizonFacing(markerDistance, cameraDistance, earthRadius) {
        const safeMarkerDistance = this.getPositiveNumber(markerDistance, 1);
        const safeCameraDistance = this.getPositiveNumber(cameraDistance, 1);
        const safeEarthRadius = Math.min(
            this.getPositiveNumber(earthRadius, 1),
            safeMarkerDistance - 0.000001,
            safeCameraDistance - 0.000001
        );
        const markerTerm = Math.max(0, safeMarkerDistance * safeMarkerDistance - safeEarthRadius * safeEarthRadius);
        const cameraTerm = Math.max(0, safeCameraDistance * safeCameraDistance - safeEarthRadius * safeEarthRadius);
        const numerator = safeEarthRadius * safeEarthRadius - Math.sqrt(markerTerm * cameraTerm);

        return this.clamp(numerator / (safeMarkerDistance * safeCameraDistance), -1, 1);
    }

    updateParticipantLabelOpacity(label) {
        if (!this.camera || !label?.parent) return;

        const markerWorldPosition = label.parent.getWorldPosition(new THREE.Vector3());
        const opacity = this.calculateParticipantLabelVisibilityOpacity(markerWorldPosition, this.camera.position);
        const materials = Array.isArray(label.material) ? label.material : [label.material];

        materials.forEach(material => {
            if (!material) return;

            material.transparent = true;
            material.opacity = opacity;
        });
    }

    updateParticipantLabelBillboard(label) {
        if (!this.camera || !label?.parent) return;

        const cameraWorldQuaternion = this.camera.getWorldQuaternion(new THREE.Quaternion());
        const parentWorldQuaternion = label.parent.getWorldQuaternion(new THREE.Quaternion());
        label.quaternion.copy(parentWorldQuaternion.invert().multiply(cameraWorldQuaternion));
    }

    updateParticipantLabelBillboards() {
        this.participantLabels.forEach(label => {
            this.updateParticipantLabelBillboard(label);
            this.updateParticipantLabelScale(label);
            this.updateParticipantLabelOpacity(label);
        });
    }

    updateParticipantMarkerTransforms() {
        if (!this.camera || this.participantMarkers.length === 0) return;

        const cameraDistance = this.camera.position.length();
        const tiltAmount = this.calculateParticipantMarkerTiltAmount(cameraDistance);
        const markerScale = this.calculateParticipantMarkerDistanceScale(cameraDistance);
        const localUp = new THREE.Vector3(0, 1, 0);

        this.participantMarkers.forEach(marker => {
            const visual = marker.userData.visual;
            if (!visual) return;

            visual.scale.setScalar(markerScale);
            if (marker.userData.rippleGroup) {
                marker.userData.rippleGroup.scale.setScalar(markerScale);
            }
            if (marker.userData.label && marker.userData.dimensions) {
                const dimensions = marker.userData.dimensions;
                const labelOffset = this.calculateParticipantLabelOffset(cameraDistance, dimensions, markerScale);
                const labelDirections = this.getParticipantLabelTangentDirections(marker);
                const sidePosition = labelDirections.side.clone().multiplyScalar(labelOffset.side);
                const liftPosition = labelDirections.lift.clone().multiplyScalar(labelOffset.lift);
                marker.userData.label.position.set(
                    sidePosition.x + liftPosition.x,
                    labelOffset.depth,
                    sidePosition.z + liftPosition.z
                );
            }

            visual.quaternion.identity();
            if (tiltAmount <= 0) return;

            const markerWorldPosition = marker.getWorldPosition(new THREE.Vector3());
            const normal = markerWorldPosition.clone().normalize();
            const towardCamera = this.camera.position.clone().sub(markerWorldPosition).normalize();
            const tangentTowardCamera = towardCamera.sub(normal.clone().multiplyScalar(towardCamera.dot(normal)));
            if (tangentTowardCamera.lengthSq() < 0.000001) return;

            const awayWorld = tangentTowardCamera.normalize().negate();
            const inverseWorldQuaternion = marker.getWorldQuaternion(new THREE.Quaternion()).invert();
            const awayLocal = awayWorld.applyQuaternion(inverseWorldQuaternion);
            awayLocal.y = 0;

            if (awayLocal.lengthSq() < 0.000001) return;

            awayLocal.normalize();
            const tiltAxis = localUp.clone().cross(awayLocal).normalize();
            visual.quaternion.setFromAxisAngle(tiltAxis, tiltAmount);
        });
    }

    updateParticipantMarkerRipples(now) {
        this.participantMarkers.forEach(marker => {
            const rippleGroup = marker.userData.rippleGroup;
            if (!rippleGroup) return;

            if (!marker.userData.isHovered) {
                this.resetParticipantMarkerRipples(marker);
                return;
            }

            const timing = rippleGroup.userData.timing;
            const cycleElapsed = (now - rippleGroup.userData.startedAt) % timing.intervalMs;

            rippleGroup.children.forEach(wave => {
                const elapsed = cycleElapsed - wave.userData.phaseOffsetMs;
                if (elapsed < 0 || elapsed > timing.durationMs) {
                    wave.visible = false;
                    wave.material.opacity = 0;
                    return;
                }

                const progress = elapsed / timing.durationMs;
                const scale = this.clamp(1 - progress, 0.04, 1);
                wave.visible = true;
                wave.scale.setScalar(scale);
                wave.material.opacity = rippleGroup.userData.maxOpacity * Math.sin(Math.PI * progress);
            });
        });
    }

    resetParticipantMarkerRipples(marker) {
        const rippleGroup = marker?.userData?.rippleGroup;
        if (!rippleGroup) return;

        rippleGroup.visible = false;
        rippleGroup.children.forEach(wave => {
            wave.visible = false;
            wave.scale.setScalar(0.001);
            if (wave.material) {
                wave.material.opacity = 0;
            }
        });
    }

    animate() {
        if (!this.state.isInitialized) return;

        this.animationId = requestAnimationFrame(() => this.animate());
        const deltaTime = this.clock.getDelta();

        if (this.earthGroup && this.state.isAutoRotating) {
            const rotationSpeed = Number.isFinite(Number(this.options.autoRotateSpeed))
                ? Number(this.options.autoRotateSpeed)
                : 0.1;
            this.earthRotation += deltaTime * rotationSpeed;
            this.earthGroup.rotation.y = this.earthRotation;
        }

        if (this.clouds) {
            this.cloudRotation += deltaTime * this.options.cloudsSpeed;
            this.clouds.rotation.y = this.cloudRotation;
        }

        if (this.controls) {
            this.updateCameraControlLimits();
            this.controls.update();
        }
        this.applyCameraDistanceSafety();
        this.updateParticipantMarkerTransforms();
        this.updateParticipantLabelBillboards();
        this.updateParticipantMarkerRipples(this.getAnimationTimeMs());
        this.updateCameraState();
        this.renderer.render(this.scene, this.camera);
    }

    updateCameraState() {
        if (this.camera) {
            this.state.cameraPosition = {
                x: this.camera.position.x,
                y: this.camera.position.y,
                z: this.camera.position.z
            };
        }
    }

    centerOn(latitude, longitude, zoom = 2.0) {
        if (!this.state.isInitialized) return false;
        try {
            const position = this.latLngToVector3(latitude, longitude, this.getClampedCameraDistance(zoom));
            this.animateCameraTo(position, 1000);
            return true;
        } catch (error) {
            console.error('Error centering camera:', error);
            return false;
        }
    }

    animateCameraTo(targetPosition, duration = 1000) {
        if (!this.camera) return;
        const startPosition = { ...this.camera.position };
        const safeTargetPosition = this.getSafeCameraPosition(targetPosition);
        const startTime = Date.now();

        const animate = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeInOutCubic = t => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
            const easedProgress = easeInOutCubic(progress);

            this.camera.position.x = startPosition.x + (safeTargetPosition.x - startPosition.x) * easedProgress;
            this.camera.position.y = startPosition.y + (safeTargetPosition.y - startPosition.y) * easedProgress;
            this.camera.position.z = startPosition.z + (safeTargetPosition.z - startPosition.z) * easedProgress;
            this.applyCameraDistanceSafety();
            this.camera.lookAt(0, 0, 0);

            if (progress < 1) requestAnimationFrame(animate);
        };

        animate();
    }

    getAutoRotationResumeDelay() {
        const delay = Number(this.options.autoRotateResumeDelay);
        return Number.isFinite(delay) && delay >= 0 ? delay : 3000;
    }

    initializeAutoRotationInteractionState() {
        this.clearAutoRotationResumeTimer();
        this.state.isUserInteracting = false;
        this.state.isPointerOverGlobe = false;
    }

    clearAutoRotationResumeTimer() {
        if (this.autoRotationResumeTimer) {
            clearTimeout(this.autoRotationResumeTimer);
            this.autoRotationResumeTimer = null;
        }
    }

    applyAutoRotationState(enabled) {
        const isEnabled = Boolean(enabled);
        this.state.isAutoRotating = isEnabled;

        if (this.controls) {
            this.controls.autoRotate = isEnabled;
            this.controls.autoRotateSpeed = this.options.autoRotateSpeed;
        }
    }

    pauseAutoRotationForInteraction() {
        this.clearAutoRotationResumeTimer();
        this.state.isUserInteracting = true;
        this.state.isPointerOverGlobe = true;

        if (this.options.autoRotate) {
            this.applyAutoRotationState(false);
        }
    }

    handleGlobePointerEnter() {
        this.state.isPointerOverGlobe = true;

        if (this.options.autoRotate && !this.state.isAutoRotating) {
            this.clearAutoRotationResumeTimer();
        }
    }

    handleGlobePointerLeave() {
        this.state.isPointerOverGlobe = false;
        this.setHoveredParticipantMarker(null);

        if (this.renderer?.domElement) {
            this.renderer.domElement.style.cursor = '';
        }

        if (!this.state.isUserInteracting && this.options.autoRotate && !this.state.isAutoRotating) {
            this.scheduleAutoRotationResume();
        }
    }

    handleGlobePointerRelease(event) {
        if (this.isPointerEventInsideGlobe(event)) {
            this.handleGlobePointerEnter();
            return;
        }

        this.handleGlobePointerLeave();
    }

    isPointerEventInsideGlobe(event) {
        const element = this.renderer?.domElement;
        if (!element || typeof element.getBoundingClientRect !== 'function') {
            return true;
        }

        const clientX = Number(event?.clientX);
        const clientY = Number(event?.clientY);
        if (!Number.isFinite(clientX) || !Number.isFinite(clientY)) {
            return true;
        }

        const rect = element.getBoundingClientRect();
        const right = Number.isFinite(rect.right) ? rect.right : rect.left + rect.width;
        const bottom = Number.isFinite(rect.bottom) ? rect.bottom : rect.top + rect.height;

        return clientX >= rect.left && clientX <= right && clientY >= rect.top && clientY <= bottom;
    }

    scheduleAutoRotationResume() {
        this.clearAutoRotationResumeTimer();
        this.state.isUserInteracting = false;

        if (!this.options.autoRotate) {
            this.applyAutoRotationState(false);
            return;
        }

        if (this.state.isPointerOverGlobe) {
            return;
        }

        this.autoRotationResumeTimer = setTimeout(() => {
            this.autoRotationResumeTimer = null;
            if (!this.state.isUserInteracting && !this.state.isPointerOverGlobe && this.options.autoRotate) {
                this.applyAutoRotationState(true);
            }
        }, this.getAutoRotationResumeDelay());
    }

    setAutoRotation(enabled, speed) {
        try {
            this.options.autoRotate = Boolean(enabled);
            const parsedSpeed = Number(speed);
            if (speed !== undefined && speed !== null && Number.isFinite(parsedSpeed)) {
                this.options.autoRotateSpeed = parsedSpeed;
            }
            this.clearAutoRotationResumeTimer();
            this.state.isUserInteracting = false;
            this.applyAutoRotationState(this.options.autoRotate);
            return true;
        } catch (error) {
            console.error('Error setting auto rotation:', error);
            return false;
        }
    }

    setLevelOfDetail(lod) {
        try {
            this.state.currentLod = lod;
            return true;
        } catch (error) {
            console.error('Error setting LOD:', error);
            return false;
        }
    }

    setSunLightIntensity(intensity) {
        if (this.scene) {
            const sunLight = this.scene.children.find(child => child instanceof THREE.DirectionalLight);
            if (sunLight) {
                sunLight.intensity = intensity;
                console.log('Яркость солнца изменена на:', intensity);
            }
        }
    }

    setSunLightColor(colorHex) {
        if (this.scene) {
            const sunLight = this.scene.children.find(child => child instanceof THREE.DirectionalLight);
            if (sunLight) {
                sunLight.color = new THREE.Color(colorHex);
                console.log('Цвет солнца изменен на:', colorHex);
            }
        }
    }

    setAmbientLightIntensity(intensity) {
        if (this.scene) {
            const ambientLight = this.scene.children.find(child => child instanceof THREE.AmbientLight);
            if (ambientLight) {
                ambientLight.intensity = intensity;
                console.log('Яркость окружения изменена на:', intensity);
            }
        }
    }

    setAtmosphereLightIntensity(intensity) {
        if (this.scene) {
            const atmosphereLight = this.scene.children.find(child => child instanceof THREE.PointLight);
            if (atmosphereLight) {
                atmosphereLight.intensity = intensity;
                console.log('Яркость атмосферного света изменена на:', intensity);
            }
        }
    }

    toggleAtmosphere(enabled) {
        if (enabled && !this.atmosphere) {
            this.options.enableAtmosphereGlow = true;
            this.createAtmosphere();
        } else if (!enabled && this.atmosphere) {
            this.earthGroup.remove(this.atmosphere);
            this.atmosphere.geometry.dispose();
            this.atmosphere.material.dispose();
            this.atmosphere = null;
            this.options.enableAtmosphereGlow = false;
        }
    }

    toggleClouds(enabled) {
        if (enabled && !this.clouds) {
            this.options.enableClouds = true;
            this.createClouds();
        } else if (!enabled && this.clouds) {
            this.earthGroup.remove(this.clouds);
            this.clouds.geometry.dispose();
            this.clouds.material.dispose();
            this.clouds = null;
            this.options.enableClouds = false;
        }
    }

    updateOptionFromSettings(settings, key) {
        if (Object.prototype.hasOwnProperty.call(settings, key) && settings[key] !== undefined && settings[key] !== null) {
            this.options[key] = settings[key];
        }
    }

    updateSettings(settings) {
        try {
            [
                'width',
                'height',
                'backgroundColor',
                'atmosphereColor',
                'atmosphereOpacity',
                'participantPointSize',
                'participantPointOffset',
                'participantPointColor',
                'participantMarkerOpacity',
                'participantMarkerHeight',
                'participantMarkerTipHeight',
                'participantMarkerRadius',
                'participantMarkerFacets',
                'participantMarkerLabelGap',
                'participantMarkerRippleIntervalMs',
                'participantMarkerRippleDurationMs',
                'participantMarkerReferenceDistance',
                'participantMarkerMinScale',
                'participantMarkerMaxScale',
<<<<<<< issue-22-e21d3bae3760
                'participantMarkerLabelSideOffset',
                'participantMarkerLabelUpOffset',
                'participantMarkerLabelCloseLift',
                'participantLabelLiftDistance',
=======
                'participantLabelLoweringDistance',
                'participantLabelHiddenOpacity',
                'participantLabelHorizonFade',
>>>>>>> master
                'highlightedPointColor',
                'autoRotateSpeed',
                'cloudsOpacity',
                'cloudsSpeed',
                'minZoom',
                'maxZoom',
                'cameraNearPlane',
                'cameraSurfaceClearance',
                'cameraZoomInMinSpeed',
                'cameraZoomInMaxSpeed',
                'cameraZoomOutSpeed',
                'cameraZoomSlowdownDistance'
            ].forEach(key => this.updateOptionFromSettings(settings, key));
            
            this.setAutoRotation(settings.autoRotate, settings.autoRotateSpeed);
            this.setSunLightIntensity(settings.sunLightIntensity);
            this.setSunLightColor(settings.sunLightColor);
            this.setAmbientLightIntensity(settings.ambientLightIntensity);
            
            this.toggleAtmosphere(settings.enableAtmosphereGlow);
            this.toggleClouds(settings.enableClouds);
            
            if (this.renderer && settings.width && settings.height) {
                this.renderer.setSize(settings.width, settings.height);
            }
            if (this.camera) {
                if (settings.width && settings.height) {
                    this.camera.aspect = settings.width / settings.height;
                }
                this.camera.near = this.getPositiveNumber(this.options.cameraNearPlane, 0.02);
                this.camera.updateProjectionMatrix();
            }
            if (this.controls) {
                this.controls.enableZoom = settings.enableZoom;
                this.updateCameraControlLimits();
                this.applyCameraDistanceSafety();
            }
            if (this.atmosphere) {
                this.atmosphere.material.opacity = settings.atmosphereOpacity;
            }
            if (this.clouds) {
                this.clouds.material.opacity = settings.cloudsOpacity;
            }
            
            const participants = Array.from(this.pointMetadata.values());
            if (participants.length > 0) {
                this.addParticipants(participants);
            }
            
            console.log('✅ Настройки применены');
            return true;
        } catch (error) {
            console.error('Ошибка применения настроек:', error);
            return false;
        }
    }

    getState() {
        return { ...this.state };
    }

    clear() {
        try {
            this.clearParticipants();
            this.state.participantCount = 0;
            this.state.countryCount = 0;
            return true;
        } catch (error) {
            console.error('Error clearing globe:', error);
            return false;
        }
    }

    dispose() {
        console.log('🗑️ CommunityGlobe.dispose вызван для контейнера:', this.containerId);
        console.log('🗑️ Состояние перед dispose:', this.state?.isInitialized);

        try {
            if (this.animationId) {
                console.log('🗑️ Отмена animation frame');
                cancelAnimationFrame(this.animationId);
            }

            this.clearAutoRotationResumeTimer();

            if (this.controls) {
                console.log('🗑️ Освобождение controls');
                this.controls.dispose();
            }

            console.log('🗑️ Обход сцены для освобождения ресурсов');
            let disposedObjects = 0;
            this.scene.traverse(object => {
                console.log('🗑️ Обработка объекта:', object.type, object.constructor.name);
                if (object.geometry) {
                    console.log('  - Освобождение geometry');
                    object.geometry.dispose();
                }
                if (object.material) {
                    if (Array.isArray(object.material)) {
                        console.log('  - Освобождение массива materials:', object.material.length);
                        object.material.forEach(material => material.dispose());
                    } else {
                        console.log('  - Освобождение material');
                        object.material.dispose();
                    }
                }
                disposedObjects++;
            });
            console.log('🗑️ Освобождено объектов:', disposedObjects);

            if (this.renderer) {
                console.log('🗑️ Освобождение renderer');
                this.renderer.dispose();
            }

            console.log('🗑️ Очистка pointMetadata');
            this.pointMetadata.clear();

            this.state.isInitialized = false;
            console.log('🗑️ Состояние установлено в неинициализированное');

        } catch (error) {
            console.error('💥 Критическая ошибка в CommunityGlobe.dispose:', error);
            console.error('💥 Контейнер в момент ошибки:', this.containerId);
            console.error('💥 Renderer в момент ошибки:', this.renderer);
            console.error('💥 Scene в момент ошибки:', this.scene);
        }
    }

    updateParticipantPosition(participantId, latitude, longitude) {
        const entry = Array.from(this.pointMetadata.entries()).find(([, participant]) =>
            participant.id.toString() === participantId.toString());
        if (!entry) return false;

        const [key, participant] = entry;
        participant.latitude = latitude;
        participant.longitude = longitude;

        const marker = this.participantMarkers.find(item =>
            item.userData.participant?.id?.toString() === participantId.toString());
        if (!marker) return false;

        const radius = 1 + this.options.participantPointOffset;
        const position = this.latLngToVector3(latitude, longitude, radius);
        const normal = new THREE.Vector3(position.x, position.y, position.z).normalize();
        marker.position.set(position.x, position.y, position.z);
        marker.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), normal);
        marker.userData.normal = normal;
        this.pointMetadata.set(key, participant);

        return true;
    }

    /**
     * Удаляет участника по ID (старый метод)
     * @param {number} participantId - ID участника для удаления
     * @deprecated Рекомендуется использовать removeParticipantById
     */
    removeParticipant(participantId) {
        const index = Array.from(this.pointMetadata.keys()).findIndex(key => this.pointMetadata.get(key).id === participantId);
        if (index === -1) return;

        this.pointMetadata.delete(`participant_${index}`);
        const participants = Array.from(this.pointMetadata.values());
        this.addParticipants(participants);
    }

    /**
     * Удаляет участника по ID (оптимизированный метод)
     * @param {string} participantId - ID участника для удаления
     * @returns {boolean} true если участник найден и удален
     */
    removeParticipantById(participantId) {
        // Находим и удаляем участника с указанным ID
        const participants = Array.from(this.pointMetadata.values());
        const filteredParticipants = participants.filter(p => p.id.toString() !== participantId.toString());

        if (filteredParticipants.length < participants.length) {
            this.clearParticipants();
            if (filteredParticipants.length > 0) {
                this.addParticipants(filteredParticipants);
            }
            console.log(`Участник с ID ${participantId} удален`);
            return true;
        }

        console.log(`Участник с ID ${participantId} не найден`);
        return false;
    }

    /**
     * Добавляет одного участника на глобус с проверкой уникальности ID
     * @param {Object} participant - Объект участника с полями id, name, latitude, longitude
     * @returns {boolean} true если участник успешно добавлен
     */
    addTestParticipant(participant) {
        if (!this.state.isInitialized) {
            console.log('❌ Глобус не инициализирован');
            return false;
        }

        // Проверяем, существует ли уже участник с таким ID
        const existingIndex = Array.from(this.pointMetadata.values()).findIndex(p => p.id === participant.id);
        if (existingIndex !== -1) {
            console.log(`Участник с ID ${participant.id} уже существует`);
            return false;
        }

        // Добавляем нового участника
        const participants = Array.from(this.pointMetadata.values());
        participants.push(participant);

        const result = this.addParticipants(participants);
        if (result) {
            console.log(`✅ Добавлен новый участник: ${participant.name} (${participant.latitude}, ${participant.longitude})`);
        }
        return result;
    }
}

export async function initializeScripts() {
    console.log('Initializing Community Globe scripts...');
    return true;
}

/**
 * Создает экземпляр 3D глобуса в указанном контейнере
 * @param {string} containerId - ID HTML элемента-контейнера
 * @param {Object} options - Настройки глобуса
 * @returns {boolean} true если глобус успешно создан
 */
export function createGlobe(containerId, options) {
    try {
        console.log('Creating globe for container:', containerId);
        const globe = new CommunityGlobe(containerId, options);
        globeInstances.set(containerId, globe);
        console.log('Globe created successfully');
        return true;
    } catch (error) {
        console.error('Error creating globe:', error);
        return false;
    }
}

/**
 * Добавляет массив участников на глобус
 * @param {string} containerId - ID контейнера глобуса
 * @param {Array} participants - Массив объектов участников
 * @returns {boolean} true если участники успешно добавлены
 */
export function addParticipants(containerId, participants) {
    try {
        console.log('🔄 Добавление участников на глобус', containerId, ':', participants?.length || 0);
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            const result = globe.addParticipants(participants);
            console.log('✅ Результат добавления участников на глобус', containerId, ':', result);
            return result;
        }
        console.log('❌ Глобус', containerId, 'не инициализирован');
        return false;
    } catch (error) {
        console.error('Error adding participants to globe', containerId, ':', error);
        return false;
    }
}

export function updateParticipantPosition(containerId, participantId, latitude, longitude) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.updateParticipantPosition(participantId, latitude, longitude);
        }
        return false;
    } catch (error) {
        console.error('Error updating participant position on globe', containerId, ':', error);
        return false;
    }
}

/**
 * Удаляет участника по ID с глобуса
 * @param {string} containerId - ID контейнера глобуса
 * @param {string} participantId - ID участника для удаления
 * @returns {boolean} true если участник успешно удален
 */
export function removeParticipant(containerId, participantId) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.removeParticipantById(participantId);
        }
        return false;
    } catch (error) {
        console.error('Error removing participant from globe', containerId, ':', error);
        return false;
    }
}

/**
 * Центрирует камеру глобуса на указанных координатах
 * @param {string} containerId - ID контейнера глобуса
 * @param {number} latitude - Широта для центрирования
 * @param {number} longitude - Долгота для центрирования
 * @param {number} zoom - Уровень масштабирования (по умолчанию 2.0)
 * @returns {boolean} true если центрирование выполнено успешно
 */
export function centerOn(containerId, latitude, longitude, zoom) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.centerOn(latitude, longitude, zoom);
        }
        return false;
    } catch (error) {
        console.error('Error centering globe', containerId, ':', error);
        return false;
    }
}

export function setLevelOfDetail(containerId, lod) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.setLevelOfDetail(lod);
        }
        return false;
    } catch (error) {
        console.error('Error setting LOD for globe', containerId, ':', error);
        return false;
    }
}

export function setAutoRotation(containerId, enabled, speed) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.setAutoRotation(enabled, speed);
        }
        return false;
    } catch (error) {
        console.error('Error setting auto rotation for globe', containerId, ':', error);
        return false;
    }
}

export function setSunLightIntensity(containerId, intensity) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            globe.setSunLightIntensity(intensity);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting sun light intensity for globe', containerId, ':', error);
        return false;
    }
}

export function setSunLightColor(containerId, colorHex) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            globe.setSunLightColor(colorHex);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting sun light color for globe', containerId, ':', error);
        return false;
    }
}

export function setAmbientLightIntensity(containerId, intensity) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            globe.setAmbientLightIntensity(intensity);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting ambient light intensity for globe', containerId, ':', error);
        return false;
    }
}

export function updateSettings(containerId, settings) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            return globe.updateSettings(settings);
        }
        return false;
    } catch (error) {
        console.error('Error updating settings for globe', containerId, ':', error);
        return false;
    }
}

export async function loadCountriesData(containerId) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            const response = await fetch('/_content/ZealousMindedPeopleGeo/data/countries.geojson');
            const data = await response.json();
            globe.loadCountries(data);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error loading countries data for globe', containerId, ':', error);
        return false;
    }
}

/**
 * Очищает всех участников с глобуса
 * @param {string} containerId - ID контейнера глобуса
 * @returns {boolean} true если очистка выполнена успешно
 */
export function clear(containerId) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe && globe.state && globe.state.isInitialized) {
            return globe.clear();
        }
        return false;
    } catch (error) {
        console.error('Error clearing globe', containerId, ':', error);
        return false;
    }
}

export function getState(containerId) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            return globe.getState();
        }
        return null;
    } catch (error) {
        console.error('Error getting globe', containerId, 'state:', error);
        return null;
    }
}

export function dispose(containerId) {
    try {
        console.log('🗑️ JavaScript dispose вызван для containerId:', containerId);
        console.log('🗑️ Количество экземпляров глобуса перед dispose:', globeInstances.size);
        console.log('🗑️ Доступные контейнеры:', Array.from(globeInstances.keys()));

        if (containerId) {
            console.log('🗑️ Удаление конкретного глобуса:', containerId);
            const globe = globeInstances.get(containerId);
            if (globe) {
                console.log('🗑️ Найден глобус для удаления:', containerId, 'состояние:', globe.state?.isInitialized);
                globe.dispose();
                globeInstances.delete(containerId);
                console.log('🗑️ Глобус удален успешно:', containerId);
                return true;
            } else {
                console.log('🗑️ Глобус не найден для containerId:', containerId);
            }
            return false;
        } else {
            console.log('🗑️ Удаление всех глобусов (containerId не указан)');
            // Если containerId не указан, очищаем все глобусы (для обратной совместимости)
            for (const [id, globe] of globeInstances) {
                console.log('🗑️ Удаление глобуса:', id, 'состояние:', globe.state?.isInitialized);
                globe.dispose();
            }
            globeInstances.clear();
            console.log('🗑️ Все глобусы удалены');
            return true;
        }
    } catch (error) {
        console.error('💥 Критическая ошибка в dispose для containerId:', containerId, error);
        console.error('💥 Stack trace:', error.stack);
        return false;
    }
}

/**
 * Добавляет одного участника на глобус с проверкой уникальности
 * @param {string} containerId - ID контейнера глобуса
 * @param {Object} participant - Объект участника с полями id, name, latitude, longitude
 * @returns {boolean} true если участник успешно добавлен
 */
export function addTestParticipant(containerId, participant) {
    try {
        const globe = globeInstances.get(containerId);
        if (globe) {
            return globe.addTestParticipant(participant);
        }
        return false;
    } catch (error) {
        console.error('Error adding test participant to globe', containerId, ':', error);
        return false;
    }
}

export function safeAddTestParticipant(participant) {
    try {
        console.log('🔍 Модульная функция safeAddTestParticipant вызвана');
        console.log('Данные участника:', participant);

        // Проверяем доступность глобуса
        if (!dependenciesLoaded) {
            console.error('❌ Зависимости не загружены');
            return false;
        }

        if (globeInstances.size === 0) {
            console.error('❌ Нет созданных экземпляров глобуса');
            return false;
        }

        // Поскольку containerId не передан, используем первый доступный глобус для обратной совместимости
        if (globeInstances.size > 0) {
            const containerId = globeInstances.keys().next().value;
            const globe = globeInstances.get(containerId);
            if (globe && globe.state && globe.state.isInitialized) {
                return globe.addTestParticipant(participant);
            } else {
                console.error('❌ Глобус не инициализирован');
                return false;
            }
        } else {
            console.error('❌ Нет созданных экземпляров глобуса');
            return false;
        }
    } catch (error) {
        console.error('💥 Критическая ошибка в safeAddTestParticipant:', error);
        return false;
    }
}

export function getThreeJsVersion() {
    try {
        if (typeof THREE !== 'undefined' && THREE.REVISION) {
            return THREE.REVISION;
        }
        return 'unknown';
    } catch (error) {
        console.error('Error getting Three.js version:', error);
        return 'error';
    }
}

/**
 * Отладочная функция для проверки состояния глобуса
 * @param {string} containerId - ID контейнера глобуса (опционально)
 */
export function debugGlobeState(containerId) {
    try {
        console.log('🔍 Отладка состояния глобуса:');
        console.log('Зависимости загружены:', dependenciesLoaded);
        console.log('Количество экземпляров глобуса:', globeInstances.size);

        if (containerId) {
            const globe = globeInstances.get(containerId);
            if (globe) {
                console.log(`🔍 Состояние глобуса ${containerId}:`, globe.state);
                console.log(`Количество точек участников в ${containerId}:`, globe.participantPoints.length);
                console.log(`Метаданные участников в ${containerId}:`, globe.pointMetadata.size);

                if (globe.earthGroup) {
                    console.log(`Объекты в earthGroup ${containerId}:`, globe.earthGroup.children.length);
                    globe.earthGroup.children.forEach((child, index) => {
                        console.log(`  ${index}: ${child.type} (${child.constructor.name})`);
                    });
                }
            } else {
                console.log(`❌ Глобус ${containerId} не найден`);
            }
        } else {
            // Если containerId не указан, показываем все глобусы
            for (const [id, globe] of globeInstances) {
                console.log(`Глобус ${id}:`, globe.state);
            }
        }

        return true;
    } catch (error) {
        console.error('Error in debug function:', error);
        return false;
    }
}

export function setGlobeReadyCallbackDirect(containerId, dotNetReference) {
    try {
        console.log(`📞 Установка прямого callback для ${containerId}`);
        const globe = globeInstances.get(containerId);
        if (globe) {
            globe.callbacks.onGlobeReady = async (state) => {
                console.log(`📞 Вызов .NET callback для ${containerId}`, state);
                await dotNetReference.invokeMethodAsync('Invoke', state);
            };
            console.log(`✅ Прямой callback установлен для ${containerId}`);
            return true;
        }
        console.error(`❌ Глобус ${containerId} не найден`);
        return false;
    } catch (error) {
        console.error('💥 Ошибка установки callback:', error);
        return false;
    }
}
