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
    /// Цвет атмосферы
    /// </summary>
    public string AtmosphereColor { get; set; } = "#00aaff";

    /// <summary>
    /// Прозрачность атмосферы (0-1)
    /// </summary>
    public double AtmosphereOpacity { get; set; } = 0.3;

    /// <summary>
    /// Размер точек участников
    /// </summary>
    public double ParticipantPointSize { get; set; } = 0.06;

    /// <summary>
    /// Цвет 3D меток участников
    /// </summary>
    public string ParticipantPointColor { get; set; } = "#24dce7";

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
    public double AutoRotateSpeed { get; set; } = 0.5;

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
    /// Путь к текстуре Земли
    /// </summary>
    public string? EarthTextureUrl { get; set; }

    /// <summary>
    /// Путь к текстуре нормалей
    /// </summary>
    public string? NormalTextureUrl { get; set; }

    /// <summary>
    /// Путь к текстуре specular карты
    /// </summary>
    public string? SpecularTextureUrl { get; set; }

    /// <summary>
    /// Путь к текстуре облаков
    /// </summary>
    public string? CloudsTextureUrl { get; set; }

    /// <summary>
    /// Прозрачность облаков (0-1)
    /// </summary>
    public double CloudsOpacity { get; set; } = 0.4;

    /// <summary>
    /// Скорость движения облаков
    /// </summary>
    public double CloudsSpeed { get; set; } = 0.2;

    /// <summary>
    /// Включить эффект свечения атмосферы
    /// </summary>
    public bool EnableAtmosphereGlow { get; set; } = true;

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
    /// Интенсивность основного солнечного света
    /// </summary>
    public double SunLightIntensity { get; set; } = 2.8;

    /// <summary>
    /// Цвет основного солнечного света
    /// </summary>
    public string SunLightColor { get; set; } = "#ffffff";

    /// <summary>
    /// Интенсивность мягкого общего освещения
    /// </summary>
    public double AmbientLightIntensity { get; set; } = 1.2;

    /// <summary>
    /// Цвет мягкого общего освещения
    /// </summary>
    public string AmbientLightColor { get; set; } = "#9db7d1";

    /// <summary>
    /// Интенсивность небесного заполнения для ясного дневного освещения
    /// </summary>
    public double HemisphereLightIntensity { get; set; } = 0.8;

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
    public double AtmosphereLightIntensity { get; set; } = 0.7;

    /// <summary>
    /// Цвет дополнительного атмосферного свечения
    /// </summary>
    public string AtmosphereLightColor { get; set; } = "#8fdcff";
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
    /// Сообщение об ошибке
    /// </summary>
    public string? ErrorMessage { get; set; }

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
