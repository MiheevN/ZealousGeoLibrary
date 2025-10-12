// wwwroot/js/community-globe.js
//import * as THREE from './libs/three.module.js';
//import { OrbitControls } from './libs/OrbitControls.js';

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
            participantPointSize: 0.5,
            participantPointColor: '#ffff00',
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
            ambientLightIntensity: 3,
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

            if (this.callbacks.onGlobeReady) {
                this.callbacks.onGlobeReady(this.state);
            }

            this.animate();
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
        if (typeof THREE === 'undefined') {
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

        // Получаем контейнер с повторными попытками
        this.container = this.getContainer();
        if (!this.container) {
            throw new Error(`Container with id '${this.containerId}' not found after multiple attempts`);
        }

        this.container.innerHTML = '';
        this.container.appendChild(this.renderer.domElement);

        this.earthGroup = new THREE.Group();
        this.scene.add(this.earthGroup);

        this.raycaster = new THREE.Raycaster();
        this.raycaster.params.Points.threshold = 0.1;

        this.clock = new THREE.Clock();
    }

    createEarth() {
        const earthGeometry = new THREE.SphereGeometry(1, 64, 64);
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

        const earthMaterial = new THREE.MeshPhongMaterial({
            map: loadTexture(this.options.earthTextureUrl),
            normalMap: loadTexture(this.options.normalTextureUrl),
            specularMap: loadTexture(this.options.specularTextureUrl),
            shininess: 0.1
        });

        const earth = new THREE.Mesh(earthGeometry, earthMaterial);
        this.earthGroup.add(earth);
        this.earthRotation = 0;
    }

    createAtmosphere() {
        if (!this.options.enableAtmosphereGlow) return;

        const atmosphereGeometry = new THREE.SphereGeometry(1.05, 64, 64);
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

        const cloudsGeometry = new THREE.SphereGeometry(1.01, 64, 64);
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
        if (!this.state.isInitialized) return;

        this.clearParticipants();
        if (!participants || participants.length === 0) return;

        const geometry = new THREE.BufferGeometry();
        const positions = [];
        const colors = [];
        const sizes = [];

        participants.forEach((participant, index) => {
            const position = this.latLngToVector3(participant.latitude, participant.longitude, 1.001);
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
                    gl_PointSize = size * (300.0 / -mvPosition.z);
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
        this.scene.add(points);
        this.participantPoints.push(points);
        this.state.participantCount = participants.length;

        console.log(`Added ${participants.length} participants to globe`);
    }

    /**
     * Очищает всех участников с глобуса
     * Удаляет точки участников и очищает метаданные
     */
    clearParticipants() {
        this.participantPoints.forEach(points => {
            this.scene.remove(points);
            points.geometry.dispose();
            if (points.material instanceof THREE.Material) points.material.dispose();
        });
        this.participantPoints = [];
        this.pointMetadata.clear();
        this.state.participantCount = 0;
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

    animate() {
        if (!this.state.isInitialized) return;

        this.animationId = requestAnimationFrame(() => this.animate());
        const deltaTime = this.clock.getDelta();

        if (this.earthGroup) {
            this.earthRotation += deltaTime * 0.1;
            this.earthGroup.rotation.y = this.earthRotation;
        }

        if (this.clouds) {
            this.cloudRotation += deltaTime * this.options.cloudsSpeed;
            this.clouds.rotation.y = this.cloudRotation;
        }

        if (this.controls) this.controls.update();
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
        if (!this.state.isInitialized) return;
        const position = this.latLngToVector3(latitude, longitude, zoom);
        this.animateCameraTo(position, 1000);
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
        this.state.isAutoRotating = enabled;
        if (this.controls) {
            this.controls.autoRotate = enabled;
            this.controls.autoRotateSpeed = speed;
        }
    }

    setLevelOfDetail(lod) {
        this.state.currentLod = lod;
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

    getState() {
        return { ...this.state };
    }

    clear() {
        this.clearParticipants();
        this.state.participantCount = 0;
        this.state.countryCount = 0;
    }

    dispose() {
        if (this.animationId) cancelAnimationFrame(this.animationId);
        if (this.controls) this.controls.dispose();

        this.scene.traverse(object => {
            if (object.geometry) object.geometry.dispose();
            if (object.material) {
                if (Array.isArray(object.material)) {
                    object.material.forEach(material => material.dispose());
                } else {
                    object.material.dispose();
                }
            }
        });

        this.renderer.dispose();
        this.pointMetadata.clear();
        this.state.isInitialized = false;
    }

    updateParticipantPosition(participantId, latitude, longitude) {
        const index = Array.from(this.pointMetadata.keys()).findIndex(key => this.pointMetadata.get(key).id === participantId);
        if (index === -1) return;

        const participant = this.pointMetadata.get(`participant_${index}`);
        participant.latitude = latitude;
        participant.longitude = longitude;

        const position = this.latLngToVector3(latitude, longitude, 1.001);
        const geometry = this.participantPoints[0].geometry;
        const positions = geometry.attributes.position.array;
        positions[index * 3] = position.x;
        positions[index * 3 + 1] = position.y;
        positions[index * 3 + 2] = position.z;
        geometry.attributes.position.needsUpdate = true;
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
     * @param {number} participantId - ID участника для удаления
     * @returns {boolean} true если участник найден и удален
     */
    removeParticipantById(participantId) {
        // Находим и удаляем участника с указанным ID
        const participants = Array.from(this.pointMetadata.values());
        const filteredParticipants = participants.filter(p => p.id !== participantId);

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
        if (!this.state.isInitialized) return;

        // Проверяем, существует ли уже участник с таким ID
        const existingIndex = Array.from(this.pointMetadata.values()).findIndex(p => p.id === participant.id);
        if (existingIndex !== -1) {
            console.log(`Участник с ID ${participant.id} уже существует`);
            return false;
        }

        // Добавляем нового участника
        const participants = Array.from(this.pointMetadata.values());
        participants.push(participant);

        this.addParticipants(participants);
        console.log(`Добавлен новый участник: ${participant.name} (${participant.latitude}, ${participant.longitude})`);
        return true;
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
 * @param {Array} participants - Массив объектов участников
 * @returns {boolean} true если участники успешно добавлены
 */
export function addParticipants(participants) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.addParticipants(participants);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error adding participants:', error);
        return false;
    }
}

export function updateParticipantPosition(participantId, latitude, longitude) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.updateParticipantPosition(participantId, latitude, longitude);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error updating participant position:', error);
        return false;
    }
}

/**
 * Удаляет участника по ID с глобуса
 * @param {number} participantId - ID участника для удаления
 * @returns {boolean} true если участник успешно удален
 */
export function removeParticipant(participantId) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            return globe.removeParticipantById(participantId);
        }
        return false;
    } catch (error) {
        console.error('Error removing participant:', error);
        return false;
    }
}

/**
 * Центрирует камеру глобуса на указанных координатах
 * @param {number} latitude - Широта для центрирования
 * @param {number} longitude - Долгота для центрирования
 * @param {number} zoom - Уровень масштабирования (по умолчанию 2.0)
 * @returns {boolean} true если центрирование выполнено успешно
 */
export function centerOn(latitude, longitude, zoom) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.centerOn(latitude, longitude, zoom);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error centering globe:', error);
        return false;
    }
}

export function setLevelOfDetail(lod) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.setLevelOfDetail(lod);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting LOD:', error);
        return false;
    }
}

export function setAutoRotation(enabled, speed) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.setAutoRotation(enabled, speed);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting auto rotation:', error);
        return false;
    }
}

export function setSunLightIntensity(intensity) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.setSunLightIntensity(intensity);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting sun light intensity:', error);
        return false;
    }
}

export function setSunLightColor(colorHex) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.setSunLightColor(colorHex);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting sun light color:', error);
        return false;
    }
}

export function setAmbientLightIntensity(intensity) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.setAmbientLightIntensity(intensity);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error setting ambient light intensity:', error);
        return false;
    }
}

export async function loadCountriesData() {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            const response = await fetch('/_content/ZealousMindedPeopleGeo/data/countries.geojson');
            const data = await response.json();
            globe.loadCountries(data);
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error loading countries data:', error);
        return false;
    }
}

/**
 * Очищает всех участников с глобуса
 * @returns {boolean} true если очистка выполнена успешно
 */
export function clear() {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            globe.clear();
            return true;
        }
        return false;
    } catch (error) {
        console.error('Error clearing globe:', error);
        return false;
    }
}

export function getState() {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            return globe.getState();
        }
        return null;
    } catch (error) {
        console.error('Error getting globe state:', error);
        return null;
    }
}

export function dispose() {
    try {
        for (const [containerId, globe] of globeInstances) {
            globe.dispose();
        }
        globeInstances.clear();
        return true;
    } catch (error) {
        console.error('Error disposing globe:', error);
        return false;
    }
}

/**
 * Добавляет одного участника на глобус с проверкой уникальности
 * @param {Object} participant - Объект участника с полями id, name, latitude, longitude
 * @returns {boolean} true если участник успешно добавлен
 */
export function addTestParticipant(participant) {
    try {
        const globe = globeInstances.values().next().value;
        if (globe) {
            return globe.addTestParticipant(participant);
        }
        return false;
    } catch (error) {
        console.error('Error adding test participant:', error);
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