using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer;

/// <summary>
/// Интерфейс менеджера именованных контейнеров гео-данных.
/// Обеспечивает централизованное управление несколькими контейнерами точек
/// (<see cref="GeoPoint"/>). Загрузка участников сообщества —
/// <see cref="ParticipantGeoDataExtensions.LoadDataAsync"/>.
/// </summary>
public interface IGeoDataContainerManager
{
    /// <summary>
    /// Создает или получает существующий контейнер по идентификатору
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <returns>Контейнер гео-данных</returns>
    IGeoDataContainer GetOrCreateContainer(string containerId);

    /// <summary>
    /// Получает контейнер по идентификатору
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <returns>Контейнер или null если не найден</returns>
    IGeoDataContainer? GetContainer(string containerId);

    /// <summary>
    /// Проверяет существование контейнера
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <returns>true если контейнер существует</returns>
    bool ContainerExists(string containerId);

    /// <summary>
    /// Удаляет контейнер
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <returns>true если контейнер был удален</returns>
    bool RemoveContainer(string containerId);

    /// <summary>
    /// Получает список всех идентификаторов контейнеров
    /// </summary>
    /// <returns>Коллекция идентификаторов</returns>
    IEnumerable<string> GetContainerIds();

    /// <summary>
    /// Заменяет данные контейнера точками: очищает его и добавляет точки
    /// (как <see cref="IGeoDataContainer.AddPointsAsync"/>)
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="points">Точки для загрузки</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> LoadPointsAsync(string containerId, IEnumerable<GeoPoint> points, CancellationToken ct = default);

    /// <summary>
    /// Загружает данные в контейнер из JSON файла
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="jsonFilePath">Путь к JSON файлу</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> LoadFromJsonFileAsync(string containerId, string jsonFilePath, CancellationToken ct = default);

    /// <summary>
    /// Загружает данные в контейнер из JSON строки: массива точек
    /// (<c>{ "id", "latitude", "longitude", "title", "properties": {…} }</c>)
    /// или массива участников в прежнем формате (<c>{ "name", "email", … }</c>).
    /// Форматы можно смешивать: элемент с полем <c>title</c> или <c>properties</c>
    /// читается как точка, остальные — как участники.
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="jsonContent">JSON строка с данными</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> LoadFromJsonAsync(string containerId, string jsonContent, CancellationToken ct = default);

    /// <summary>
    /// Сохраняет данные контейнера в JSON файл
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="jsonFilePath">Путь к JSON файлу</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Результат операции</returns>
    ValueTask<GeoDataOperationResult> SaveToJsonFileAsync(string containerId, string jsonFilePath, CancellationToken ct = default);

    /// <summary>
    /// Экспортирует данные контейнера в JSON строку — массив точек
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>JSON строка с данными</returns>
    ValueTask<string> ExportToJsonAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Событие, вызываемое при изменении данных в контейнере
    /// </summary>
    event Action<string, GeoDataChangeType>? OnDataChanged;
}

/// <summary>
/// Тип изменения данных в контейнере
/// </summary>
public enum GeoDataChangeType
{
    /// <summary>
    /// Добавление точки
    /// </summary>
    Added,

    /// <summary>
    /// Обновление точки
    /// </summary>
    Updated,

    /// <summary>
    /// Удаление точки
    /// </summary>
    Removed,

    /// <summary>
    /// Очистка контейнера
    /// </summary>
    Cleared,

    /// <summary>
    /// Массовая загрузка данных
    /// </summary>
    BulkLoaded
}
