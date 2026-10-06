using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Именованный набор точек (<see cref="GeoPoint"/>): данные одного глобуса, одной карты
/// или одного контекста. Участников сообщества контейнер хранит как точки, методы для
/// них — в <see cref="ParticipantGeoDataExtensions"/>.
/// </summary>
public interface IGeoDataContainer
{
    /// <summary>
    /// Уникальный идентификатор контейнера
    /// </summary>
    string ContainerId { get; }

    /// <summary>
    /// Количество точек в контейнере
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Добавляет точку. Точку с уже занятым <see cref="GeoPoint.Id"/> или с неверными
    /// координатами контейнер не добавляет и возвращает ошибку.
    /// </summary>
    /// <param name="point">Точка</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> AddPointAsync(GeoPoint point, CancellationToken ct = default);

    /// <summary>
    /// Добавляет несколько точек. Пропускает <c>null</c>, точки с неверными данными и
    /// точки, чей идентификатор уже есть в контейнере или повторяется в наборе.
    /// </summary>
    /// <param name="points">Точки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>
    /// Результат: <see cref="GeoDataOperationResult.ProcessedCount"/> — сколько добавлено,
    /// <see cref="GeoDataOperationResult.SkippedCount"/> — сколько пропущено.
    /// </returns>
    ValueTask<GeoDataOperationResult> AddPointsAsync(IEnumerable<GeoPoint> points, CancellationToken ct = default);

    /// <summary>
    /// Все точки контейнера. Возвращаются копии: их изменение не меняет данные контейнера.
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Точки контейнера</returns>
    ValueTask<IReadOnlyList<GeoPoint>> GetPointsAsync(CancellationToken ct = default);

    /// <summary>
    /// Точка по идентификатору или <c>null</c>
    /// </summary>
    /// <param name="id">Идентификатор точки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Копия точки или <c>null</c></returns>
    ValueTask<GeoPoint?> GetPointAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Заменяет точку с тем же <see cref="GeoPoint.Id"/>
    /// </summary>
    /// <param name="point">Новые данные точки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> UpdatePointAsync(GeoPoint point, CancellationToken ct = default);

    /// <summary>
    /// Удаляет точку
    /// </summary>
    /// <param name="id">Идентификатор точки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> RemovePointAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Удаляет все точки контейнера
    /// </summary>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> ClearAsync(CancellationToken ct = default);
}

/// <summary>
/// Результат операции с гео-данными
/// </summary>
public class GeoDataOperationResult
{
    /// <summary>
    /// Успешность операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение об ошибке (если есть)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Количество обработанных записей
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Количество пропущенных записей при массовом добавлении: <c>null</c>, неверные
    /// данные, повторы идентификаторов
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Идентификатор точки (для операций с одной точкой)
    /// </summary>
    public string? PointId { get; set; }

    /// <summary>
    /// Идентификатор записи как GUID — для участников и точек, чей идентификатор
    /// является GUID
    /// </summary>
    public Guid? RecordId { get; set; }

    /// <summary>
    /// Создает успешный результат
    /// </summary>
    public static GeoDataOperationResult Ok(int processedCount = 1, Guid? recordId = null) => new()
    {
        Success = true,
        ProcessedCount = processedCount,
        RecordId = recordId,
        PointId = recordId?.ToString()
    };

    /// <summary>
    /// Создает успешный результат операции с одной точкой
    /// </summary>
    public static GeoDataOperationResult OkPoint(string pointId) => new()
    {
        Success = true,
        ProcessedCount = 1,
        PointId = pointId,
        RecordId = Guid.TryParse(pointId, out var id) ? id : null
    };

    /// <summary>
    /// Создает результат с ошибкой
    /// </summary>
    public static GeoDataOperationResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
