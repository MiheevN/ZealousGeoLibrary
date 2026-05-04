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

        this.options = {
            ...options,
            width: 800,
            height: 600,
            backgroundColor: '#000011',
            atmosphereColor: '#00aaff',
            atmosphereOpacity: 0.2,
            participantPointSize: 0.2,
            participantPointColor: '#ffff00',
            participantPointOffset: 0.02, // Расстояние точек от поверхности глобуса
            highlightedPointColor: '#ff6600',
            autoRotate: true,
            autoRotateSpeed: 0.1,
            enableMouseControls: true,
            enableZoom: true,
            minZoom: 0.5,
            maxZoom: 4.0,
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

        this.state = {
            isInitialized: false,
            isAutoRotating: this.options.autoRotate,
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
        this.participantLabels = [];
        this.labelTargetPixelHeight = DEFAULT_LABEL_PIXEL_HEIGHT;
        this.countryPolygons = [];
        this.raycaster = null;
        this.mouse = { x: 0, y: 0 };
        this.animationId = null;
        this.clock = null;
        this.pointMetadata = new Map();
        this.callbacks = { // Инициализация callbacks
            onGlobeReady: null,
            onError: null,
            onParticipantClick: null
        };

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
        this.camera = new THREE.PerspectiveCamera(75, aspect, 0.1, 1000);
        this.camera.position.set(
            this.state.cameraPosition.x,
            this.state.cameraPosition.y,
            this.state.cameraPosition.z
        );

        this.renderer = new THREE.WebGLRenderer({ antialias: true });
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
        this.controls.minDistance = this.options.minZoom;
        this.controls.maxDistance = this.options.maxZoom;
        this.controls.autoRotate = this.options.autoRotate;
        this.controls.autoRotateSpeed = this.options.autoRotateSpeed;
    }

    setupEventListeners() {
        this.renderer.domElement.addEventListener('click', (event) => this.onMouseClick(event));
        this.renderer.domElement.addEventListener('mousemove', (event) => this.onMouseMove(event));
        window.addEventListener('resize', () => this.onWindowResize());
    }

    onMouseClick(event) {
        const rect = this.renderer.domElement.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

        this.raycaster.setFromCamera(this.mouse, this.camera);

        if (this.participantPoints.length > 0) {
            const intersects = this.raycaster.intersectObjects(this.participantPoints);
            if (intersects.length > 0) {
                const pointIndex = intersects[0].index;
                const metadata = this.pointMetadata.get(`participant_${pointIndex}`);
                if (metadata && this.callbacks.onParticipantClick) {
                    this.callbacks.onParticipantClick(metadata);
                }
            }
        }
    }

    onMouseMove(event) {
        const rect = this.renderer.domElement.getBoundingClientRect();
        this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
        this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
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
            const geometry = new THREE.BufferGeometry();
            const positions = [];
            const colors = [];
            const sizes = [];

            participants.forEach((participant, index) => {
                // Валидация координат
                if (typeof participant.latitude !== 'number' || typeof participant.longitude !== 'number' ||
                    isNaN(participant.latitude) || isNaN(participant.longitude)) {
                    console.warn(`⚠️ Пропускаем участника ${participant.name}: некорректные координаты (${participant.latitude}, ${participant.longitude})`);
                    return;
                }

                const radius = 1 + this.options.participantPointOffset;
                const position = this.latLngToVector3(participant.latitude, participant.longitude, radius);
                positions.push(position.x, position.y, position.z);
                const color = new THREE.Color(this.options.participantPointColor);
                colors.push(color.r, color.g, color.b);
                sizes.push(this.options.participantPointSize);
                this.pointMetadata.set(`participant_${index}`, participant);
            });

            geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
            geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
            geometry.setAttribute('size', new THREE.Float32BufferAttribute(sizes, 1));

            const material = new THREE.ShaderMaterial({
                uniforms: { pointTexture: { value: this.createCircleTexture() } },
                vertexShader: `
                    attribute float size;
                    attribute vec3 color;
                    varying vec3 vColor;
                    void main() {
                        vColor = color;
                        vec4 mvPosition = modelViewMatrix * vec4(position, 1.0);
                        gl_PointSize = size * (150.0 / -mvPosition.z);
                        gl_Position = projectionMatrix * mvPosition;
                    }
                `,
                fragmentShader: `
                    uniform sampler2D pointTexture;
                    varying vec3 vColor;
                    void main() {
                        gl_FragColor = vec4(vColor, 1.0);
                        gl_FragColor = gl_FragColor * texture2D(pointTexture, gl_PointCoord);
                    }
                `,
                transparent: true
            });

            const points = new THREE.Points(geometry, material);
            this.earthGroup.add(points); // Добавляем в earthGroup чтобы точки вращались с глобусом
            this.participantPoints.push(points);

            // Создаем текстовые метки
            this.createParticipantLabels(participants);
            
            console.log(`🎯 Создано ${participants.length} точек участников`);
            console.log('Позиции точек:', positions.slice(0, 9)); // Первые 3 точки
            console.log('Размеры точек:', sizes.slice(0, 3));
            this.state.participantCount = participants.length;

            console.log(`✅ Добавлено ${participants.length} участников на глобус`);
        console.log(`📊 Общее количество объектов в earthGroup: ${this.earthGroup.children.length}`);

            // Принудительно обновляем рендер для немедленного отображения точек
            if (this.renderer && this.scene && this.camera) {
                console.log(`🔄 Принудительный рендер сцены для контейнера ${this.containerId}`);
                this.renderer.render(this.scene, this.camera);
            }

            console.log(`📈 Финальное состояние для контейнера ${this.containerId}:`);
            console.log(`   - Точек участников: ${this.state.participantCount}`);
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
        this.participantPoints.forEach(points => {
            this.earthGroup.remove(points); // Удаляем из earthGroup
            points.geometry.dispose();
            if (points.material instanceof THREE.Material) points.material.dispose();
        });
        this.participantLabels.forEach(label => {
            this.earthGroup.remove(label);
        });
        this.participantPoints = [];
        this.participantLabels = [];
        this.pointMetadata.clear();
        this.state.participantCount = 0;
        console.log('🧹 Очищены все точки участников');
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

    createParticipantLabels(participants) {
        console.log(`🏷️ Создание меток для ${participants.length} участников`);

        participants.forEach((participant, index) => {
            // Проверка валидности данных
            if (!participant.name || participant.name.trim() === '') {
                console.warn(`⚠️ Пропускаем метку для участника без имени (index ${index})`);
                return;
            }

            try {
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                const fontSize = 24;
                const scale = 2;

                ctx.font = `${fontSize}px Arial`;
                const textWidth = ctx.measureText(participant.name).width;
                canvas.width = (textWidth + 20) * scale;
                canvas.height = (fontSize + 10) * scale;
                ctx.scale(scale, scale);

                // Полупрозрачный фон для лучшей читаемости
                ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
                ctx.fillRect(0, 0, textWidth + 20, fontSize + 10);

                // Белая рамка
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.8)';
                ctx.lineWidth = 1;
                ctx.strokeRect(0, 0, textWidth + 20, fontSize + 10);

                // Текст
                ctx.fillStyle = 'white';
                ctx.font = `${fontSize}px Arial`;
                ctx.fillText(participant.name, 10, fontSize + 2);

                const texture = new THREE.CanvasTexture(canvas);
                const material = new THREE.SpriteMaterial({ map: texture });
                const sprite = new THREE.Sprite(material);

                const radius = 1 + this.options.participantPointOffset + 0.03;
                const position = this.latLngToVector3(participant.latitude, participant.longitude, radius);
                sprite.position.set(position.x, position.y, position.z);
                sprite.userData.labelAspectRatio = canvas.width / canvas.height;
                sprite.userData.targetPixelHeight = this.labelTargetPixelHeight;
                this.updateParticipantLabelScale(sprite);

                this.earthGroup.add(sprite);
                this.participantLabels.push(sprite);

                console.log(`🏷️ Создана метка для ${participant.name} на позиции (${position.x.toFixed(3)}, ${position.y.toFixed(3)}, ${position.z.toFixed(3)})`);
            } catch (error) {
                console.error(`❌ Ошибка создания метки для участника ${participant.name}:`, error);
            }
        });

        console.log(`✅ Создано ${this.participantLabels.length} меток участников`);
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

    updateParticipantLabelScales() {
        this.participantLabels.forEach(label => this.updateParticipantLabelScale(label));
    }

    animate() {
        if (!this.state.isInitialized) return;

        this.animationId = requestAnimationFrame(() => this.animate());
        const deltaTime = this.clock.getDelta();

        if (this.earthGroup && this.state.isAutoRotating) {
            this.earthRotation += deltaTime * 0.1;
            this.earthGroup.rotation.y = this.earthRotation;
        }

        if (this.clouds) {
            this.cloudRotation += deltaTime * this.options.cloudsSpeed;
            this.clouds.rotation.y = this.cloudRotation;
        }

        if (this.controls) this.controls.update();
        this.updateParticipantLabelScales();
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
            const position = this.latLngToVector3(latitude, longitude, zoom);
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
        const startTime = Date.now();

        const animate = () => {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeInOutCubic = t => t < 0.5 ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
            const easedProgress = easeInOutCubic(progress);

            this.camera.position.x = startPosition.x + (targetPosition.x - startPosition.x) * easedProgress;
            this.camera.position.y = startPosition.y + (targetPosition.y - startPosition.y) * easedProgress;
            this.camera.position.z = startPosition.z + (targetPosition.z - startPosition.z) * easedProgress;
            this.camera.lookAt(0, 0, 0);

            if (progress < 1) requestAnimationFrame(animate);
        };

        animate();
    }

    setAutoRotation(enabled, speed) {
        try {
            this.state.isAutoRotating = enabled;
            if (this.controls) {
                this.controls.autoRotate = enabled;
                this.controls.autoRotateSpeed = speed;
            }
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

    updateSettings(settings) {
        try {
            this.options.participantPointSize = settings.participantPointSize;
            this.options.participantPointOffset = settings.participantPointOffset;
            this.options.participantPointColor = settings.participantPointColor;
            this.options.highlightedPointColor = settings.highlightedPointColor;
            this.options.autoRotateSpeed = settings.autoRotateSpeed;
            this.options.cloudsOpacity = settings.cloudsOpacity;
            this.options.cloudsSpeed = settings.cloudsSpeed;
            this.options.atmosphereOpacity = settings.atmosphereOpacity;
            
            this.setAutoRotation(settings.autoRotate, settings.autoRotateSpeed);
            this.setSunLightIntensity(settings.sunLightIntensity);
            this.setSunLightColor(settings.sunLightColor);
            this.setAmbientLightIntensity(settings.ambientLightIntensity);
            
            this.toggleAtmosphere(settings.enableAtmosphereGlow);
            this.toggleClouds(settings.enableClouds);
            
            if (this.renderer) {
                this.renderer.setSize(settings.width, settings.height);
            }
            if (this.camera) {
                this.camera.aspect = settings.width / settings.height;
                this.camera.updateProjectionMatrix();
            }
            if (this.controls) {
                this.controls.minDistance = settings.minZoom;
                this.controls.maxDistance = settings.maxZoom;
                this.controls.enableZoom = settings.enableZoom;
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
        const index = Array.from(this.pointMetadata.keys()).findIndex(key => 
            this.pointMetadata.get(key).id.toString() === participantId.toString());
        if (index === -1) return false;

        const participant = this.pointMetadata.get(`participant_${index}`);
        participant.latitude = latitude;
        participant.longitude = longitude;

        const radius = 1 + this.options.participantPointOffset;
        const position = this.latLngToVector3(latitude, longitude, radius);
        const geometry = this.participantPoints[0].geometry;
        const positions = geometry.attributes.position.array;
        positions[index * 3] = position.x;
        positions[index * 3 + 1] = position.y;
        positions[index * 3 + 2] = position.z;
        geometry.attributes.position.needsUpdate = true;
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
