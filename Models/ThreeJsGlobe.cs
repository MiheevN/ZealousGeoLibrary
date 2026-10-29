using System.Text.Json.Serialization;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Модели данных для работы с 3D глобусом на базе Three.js
/// </summary>

/// <summary>
/// Параметры инициализации глобуса
/// </summary>
public class GlobeOptions
{
    /// <summary>
    /// Ширина контейнера в пикселях
    /// </summary>
    public int Width { get; set; } = 800;

    /// <summary>
    /// Высота контейнера в пикселях
    /// </summary>
    public int Height { get; set; } = 600;

    /// <summary>
    /// Цвет фона глобуса
    /// </summary>
    public string BackgroundColor { get; set; } = "#000011";

    /// <summary>
    /// Цвет свечения атмосферы по краю шара
    /// </summary>
    public string AtmosphereColor { get; set; } = "#4fb4ff";

    /// <summary>
    /// Яркость свечения атмосферы (0-1)
    /// </summary>
    public double AtmosphereOpacity { get; set; } = 0.7;

    /// <summary>
    /// Размер точек участников
    /// </summary>
    public double ParticipantPointSize { get; set; } = 0.06;

    /// <summary>
    /// Цвет 3D меток участников
    /// </summary>
    public string ParticipantPointColor { get; set; } = "#24dce7";

    /// <summary>
    /// Сливать близкие на экране маркеры в группу со счётчиком, как на 2D-карте. Группы
    /// пересобираются при приближении, клик по группе приближает к ней.
    /// </summary>
    public bool ClusterPoints { get; set; } = true;

    /// <summary>
    /// Расстояние между маркерами на экране в пикселях (0–200), ближе которого они сливаются.
    /// </summary>
    public int ClusterRadius { get; set; } = 40;

    /// <summary>
    /// Надписи глобуса для экранного диктора: подпись холста, подсказка про клавиши, название
    /// точки без имени и формы «N точек». Без значения — по-английски.
    /// <see cref="Components.CommunityGlobeViewer"/> передаёт перевод на язык пользователя.
    /// </summary>
    public Dictionary<string, string>? Texts { get; set; }

    /// <summary>
    /// Язык (например, "ru"), по правилам которого выбирается форма «1 точка» / «5 точек».
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Отступ острого конца метки от поверхности глобуса
    /// </summary>
    public double ParticipantPointOffset { get; set; } = 0.02;

    /// <summary>
    /// Прозрачность 3D метки участника (0-1)
    /// </summary>
    public double ParticipantMarkerOpacity { get; set; } = 0.72;

    /// <summary>
    /// Интервал между волнами при наведении на метку, мс
    /// </summary>
    public int ParticipantMarkerRippleIntervalMs { get; set; } = 2000;

    /// <summary>
    /// Длительность одной волны при наведении на метку, мс
    /// </summary>
    public int ParticipantMarkerRippleDurationMs { get; set; } = 500;

    /// <summary>
    /// Цвет грани и выделения метки участника
    /// </summary>
    public string HighlightedPointColor { get; set; } = "#e0fcff";

    /// <summary>
    /// Включить автоповорот глобуса
    /// </summary>
    public bool AutoRotate { get; set; } = true;

    /// <summary>
    /// Скорость автоповорота
    /// </summary>
    public double AutoRotateSpeed { get; set; } = 0.1;

    /// <summary>
    /// Включить управление мышью
    /// </summary>
    public bool EnableMouseControls { get; set; } = true;

    /// <summary>
    /// Включить зум колесиком мыши
    /// </summary>
    public bool EnableZoom { get; set; } = true;

    /// <summary>
    /// Минимальный зум
    /// </summary>
    public double MinZoom { get; set; } = 1.03;

    /// <summary>
    /// Максимальный зум
    /// </summary>
    public double MaxZoom { get; set; } = 4.0;

    /// <summary>
    /// Уровень детализации (0-3)
    /// </summary>
    public int LevelOfDetail { get; set; } = 2;

    /// <summary>
    /// Путь к своей карте дня (цвет Земли, sRGB). Пусто — встроенная 8K или 4K по <see cref="TextureQuality"/>.
    /// </summary>
    public string? EarthTextureUrl { get; set; }

    /// <summary>
    /// Путь к карте нормалей (рельеф). Пусто — встроенная 4K.
    /// </summary>
    public string? NormalTextureUrl { get; set; }

    /// <summary>
    /// Путь к карте шероховатости: светлое — матовое (суша), тёмное — блестящее (океан).
    /// Пусто — встроенная 4K.
    /// </summary>
    public string? RoughnessTextureUrl { get; set; }

    /// <summary>
    /// Не используется: браузеры не открывали прежнюю TIFF-карту. Задайте <see cref="RoughnessTextureUrl"/>.
    /// </summary>
    [Obsolete("Use RoughnessTextureUrl: light is rough (land), dark is glossy (ocean).")]
    public string? SpecularTextureUrl { get; set; }

    /// <summary>
    /// Путь к маске облаков: белое — облако, чёрное — чистое небо. Пусто — встроенная 4K.
    /// </summary>
    public string? CloudsTextureUrl { get; set; }

    /// <summary>
    /// Качество встроенных текстур: <c>auto</c> — 8K на компьютере, 4K на телефоне и слабых
    /// видеокартах; <c>high</c> — всегда 8K; <c>standard</c> — всегда 4K.
    /// </summary>
    public string TextureQuality { get; set; } = "auto";

    /// <summary>
    /// Плотность облаков (0-1)
    /// </summary>
    public double CloudsOpacity { get; set; } = 0.85;

    /// <summary>
    /// Скорость движения облаков
    /// </summary>
    public double CloudsSpeed { get; set; } = 0.01;

    /// <summary>
    /// Включить эффект свечения атмосферы
    /// </summary>
    public bool EnableAtmosphereGlow { get; set; } = true;

    /// <summary>
    /// Звёзды на фоне
    /// </summary>
    public bool EnableStars { get; set; } = true;

    /// <summary>
    /// День и ночь: <see cref="GlobeDayNightMode.Off"/> — освещена видимая сторона;
    /// <see cref="GlobeDayNightMode.RealTime"/> — солнце там, где оно сейчас, ночная сторона
    /// тёмная, с огнями городов.
    /// </summary>
    public GlobeDayNightMode DayNightMode { get; set; } = GlobeDayNightMode.Off;

    /// <summary>
    /// Момент для <see cref="GlobeDayNightMode.RealTime"/>. Пусто — текущее время: граница
    /// дня и ночи сдвигается сама.
    /// </summary>
    public DateTimeOffset? DayNightTime { get; set; }

    /// <summary>
    /// Путь к своей карте ночных огней. Пусто — встроенная.
    /// </summary>
    public string? NightTextureUrl { get; set; }

    /// <summary>
    /// Яркость огней городов на ночной стороне
    /// </summary>
    public double NightLightsIntensity { get; set; } = 1.6;

    /// <summary>
    /// Цвет точек стран
    /// </summary>
    public string CountryPointColor { get; set; } = "#ffffff";

    /// <summary>
    /// Размер точек стран
    /// </summary>
    public double CountryPointSize { get; set; } = 0.1;

    /// <summary>
    /// Цвет линий стран
    /// </summary>
    public string CountryLineColor { get; set; } = "#444444";

    /// <summary>
    /// Ширина линий стран
    /// </summary>
    public double CountryLineWidth { get; set; } = 0.5;

    /// <summary>
    /// Направлять ли солнечный свет со стороны текущей камеры
    /// </summary>
    public bool SunLightFollowCamera { get; set; } = true;

    /// <summary>
    /// Условная дистанция направленного солнечного света от центра глобуса
    /// </summary>
    public double SunLightDistance { get; set; } = 6.0;

    /// <summary>
    /// На сколько градусов солнце смещено от камеры вверх-влево (0 — светит из-за камеры).
    /// Свет сбоку даёт шару объём.
    /// </summary>
    public double SunLightOffsetDegrees { get; set; } = 30;

    /// <summary>
    /// Интенсивность основного солнечного света
    /// </summary>
    public double SunLightIntensity { get; set; } = 2.6;

    /// <summary>
    /// Цвет основного солнечного света
    /// </summary>
    public string SunLightColor { get; set; } = "#ffffff";

    /// <summary>
    /// Интенсивность мягкого общего освещения
    /// </summary>
    public double AmbientLightIntensity { get; set; } = 0.3;

    /// <summary>
    /// Цвет мягкого общего освещения
    /// </summary>
    public string AmbientLightColor { get; set; } = "#9db7d1";

    /// <summary>
    /// Интенсивность небесного заполнения для ясного дневного освещения
    /// </summary>
    public double HemisphereLightIntensity { get; set; } = 0.25;

    /// <summary>
    /// Цвет верхнего небесного заполнения
    /// </summary>
    public string HemisphereSkyColor { get; set; } = "#d8f1ff";

    /// <summary>
    /// Цвет нижнего заполняющего света
    /// </summary>
    public string HemisphereGroundColor { get; set; } = "#253042";

    /// <summary>
    /// Интенсивность дополнительного атмосферного свечения
    /// </summary>
    public double AtmosphereLightIntensity { get; set; } = 0;

    /// <summary>
    /// Цвет дополнительного атмосферного свечения
    /// </summary>
    public string AtmosphereLightColor { get; set; } = "#8fdcff";
}

/// <summary>
/// Как освещён 3D-глобус.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GlobeDayNightMode>))]
public enum GlobeDayNightMode
{
    /// <summary>Солнце светит со стороны зрителя: видимая сторона всегда днём.</summary>
    Off,

    /// <summary>Солнце стоит там, где оно в заданный момент; на ночной стороне — огни городов.</summary>
    RealTime
}

/// <summary>
/// Почему 3D-глобус не запустился.
/// </summary>
public enum GlobeInitializationError
{
    /// <summary>Глобус запустился.</summary>
    None,

    /// <summary>Браузер не поддерживает WebGL или он выключен (нет аппаратного ускорения).</summary>
    WebGlUnavailable,

    /// <summary>Скрипты глобуса (Three.js) не загрузились.</summary>
    ScriptsUnavailable,

    /// <summary>На странице нет элемента, в котором рисуется глобус.</summary>
    ContainerNotFound,

    /// <summary>Прочие ошибки; подробности в <see cref="GlobeInitializationResult.ErrorMessage"/>.</summary>
    Unknown
}

/// <summary>
/// Результат инициализации глобуса
/// </summary>
public class GlobeInitializationResult
{
    /// <summary>
    /// Успешность инициализации
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Техническое описание ошибки для журнала (на английском). Пользователю показывайте
    /// текст по <see cref="Error"/>.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Причина неудачи; <see cref="GlobeInitializationError.None"/> при успехе.
    /// </summary>
    public GlobeInitializationError Error { get; set; }

    /// <summary>
    /// Идентификатор глобуса
    /// </summary>
    public string? GlobeId { get; set; }

    /// <summary>
    /// Версия Three.js
    /// </summary>
    public string? ThreeJsVersion { get; set; }

    /// <summary>
    /// Поддерживаемые расширения WebGL
    /// </summary>
    public string[]? SupportedExtensions { get; set; }
}

/// <summary>
/// Результат операции с глобусом
/// </summary>
public class GlobeOperationResult
{
    /// <summary>
    /// Успешность операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Количество обработанных объектов
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Время выполнения операции в мс
    /// </summary>
    public long ExecutionTimeMs { get; set; }
}

/// <summary>
/// Состояние глобуса
/// </summary>
public class GlobeState
{
    /// <summary>
    /// Идентификатор глобуса
    /// </summary>
    public string? GlobeId { get; set; }

    /// <summary>
    /// Инициализирован ли глобус
    /// </summary>
    public bool IsInitialized { get; set; }

    /// <summary>
    /// Количество участников на глобусе
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Количество стран на глобусе
    /// </summary>
    public int CountryCount { get; set; }

    /// <summary>
    /// Текущая камера
    /// </summary>
    public CameraState? Camera { get; set; }

    /// <summary>
    /// Текущие настройки глобуса
    /// </summary>
    public GlobeOptions? Options { get; set; }

    /// <summary>
    /// Включен ли автоповорот
    /// </summary>
    public bool IsAutoRotating { get; set; }

    /// <summary>
    /// Текущий уровень детализации
    /// </summary>
    public int CurrentLevelOfDetail { get; set; }

    /// <summary>
    /// Использование памяти в байтах
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Количество отрисованных кадров в секунду
    /// </summary>
    public double FramesPerSecond { get; set; }
}

/// <summary>
/// Состояние камеры
/// </summary>
public class CameraState
{
    /// <summary>
    /// Позиция камеры (x, y, z)
    /// </summary>
    public double[] Position { get; set; } = new double[3];

    /// <summary>
    /// Направление взгляда камеры (x, y, z)
    /// </summary>
    public double[] Target { get; set; } = new double[3];

    /// <summary>
    /// Угол поворота камеры
    /// </summary>
    public double Rotation { get; set; }

    /// <summary>
    /// Уровень масштабирования
    /// </summary>
    public double Zoom { get; set; }

    /// <summary>
    /// Широта центра вида
    /// </summary>
    public double CenterLatitude { get; set; }

    /// <summary>
    /// Долгота центра вида
    /// </summary>
    public double CenterLongitude { get; set; }
}
