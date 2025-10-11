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
    public double ParticipantPointSize { get; set; } = 0.5;

    /// <summary>
    /// Цвет точек участников
    /// </summary>
    public string ParticipantPointColor { get; set; } = "#ffff00";

    /// <summary>
    /// Цвет выделенной точки участника
    /// </summary>
    public string HighlightedPointColor { get; set; } = "#ff6600";

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
    public double MinZoom { get; set; } = 0.5;

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

/// <summary>
/// Данные точки на глобусе
/// </summary>
public class GlobePointData
{
    /// <summary>
    /// Идентификатор точки
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Широта
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Долгота
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Размер точки
    /// </summary>
    public double Size { get; set; } = 1.0;

    /// <summary>
    /// Цвет точки (hex)
    /// </summary>
    public string Color { get; set; } = "#ffffff";

    /// <summary>
    /// Прозрачность точки (0-1)
    /// </summary>
    public double Opacity { get; set; } = 1.0;

    /// <summary>
    /// Название точки (для тултипа)
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Дополнительные данные точки
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Анимация точки
    /// </summary>
    public PointAnimation? Animation { get; set; }
}

/// <summary>
/// Анимация точки
/// </summary>
public class PointAnimation
{
    /// <summary>
    /// Тип анимации
    /// </summary>
    public string Type { get; set; } = "pulse"; // pulse, bounce, glow

    /// <summary>
    /// Длительность анимации в секундах
    /// </summary>
    public double Duration { get; set; } = 2.0;

    /// <summary>
    /// Амплитуда анимации
    /// </summary>
    public double Amplitude { get; set; } = 1.0;

    /// <summary>
    /// Скорость анимации
    /// </summary>
    public double Speed { get; set; } = 1.0;

    /// <summary>
    /// Зациклена ли анимация
    /// </summary>
    public bool Loop { get; set; } = true;
}

/// <summary>
/// Данные страны для отображения на глобусе
/// </summary>
public class GlobeCountryData
{
    /// <summary>
    /// Код страны (ISO)
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>
    /// Название страны
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Название страны на русском
    /// </summary>
    public string? NameRu { get; set; }

    /// <summary>
    /// Цвет страны
    /// </summary>
    public string Color { get; set; } = "#ffffff";

    /// <summary>
    /// Прозрачность страны (0-1)
    /// </summary>
    public double Opacity { get; set; } = 0.8;

    /// <summary>
    /// Координаты границы страны
    /// </summary>
    public double[][][] Coordinates { get; set; } = Array.Empty<double[][]>();

    /// <summary>
    /// Центр страны (широта, долгота)
    /// </summary>
    public double[] Center { get; set; } = new double[2];

    /// <summary>
    /// Количество участников в стране
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    /// Дополнительные данные страны
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Событие глобуса
/// </summary>
public class GlobeEvent
{
    /// <summary>
    /// Тип события
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Данные события
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Временная метка события
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Идентификатор связанного объекта
    /// </summary>
    public string? ObjectId { get; set; }
}

/// <summary>
/// Конфигурация производительности глобуса
/// </summary>
public class GlobePerformanceConfig
{
    /// <summary>
    /// Максимальное количество точек участников
    /// </summary>
    public int MaxParticipantPoints { get; set; } = 10000;

    /// <summary>
    /// Максимальное количество полигонов стран
    /// </summary>
    public int MaxCountryPolygons { get; set; } = 200;

    /// <summary>
    /// Уровень детализации геометрии (низкий, средний, высокий)
    /// </summary>
    public string GeometryDetail { get; set; } = "medium";

    /// <summary>
    /// Включить оптимизацию для мобильных устройств
    /// </summary>
    public bool EnableMobileOptimization { get; set; } = true;

    /// <summary>
    /// Максимальное количество кадров в секунду
    /// </summary>
    public int MaxFps { get; set; } = 60;

    /// <summary>
    /// Размер буфера геометрии
    /// </summary>
    public int GeometryBufferSize { get; set; } = 1000000;

    /// <summary>
    /// Включить LOD (Level of Detail)
    /// </summary>
    public bool EnableLod { get; set; } = true;

    /// <summary>
    /// Расстояние для LOD переключения
    /// </summary>
    public double[] LodDistances { get; set; } = { 1.0, 2.0, 4.0 };
}