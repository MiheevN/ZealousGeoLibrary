using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping;

/// <summary>
/// Интерфейс сервиса для работы с 3D глобусом на базе Three.js
/// </summary>
public interface IThreeJsGlobeService
{
    /// <summary>
    /// Инициализирует 3D глобус
    /// </summary>
    /// <param name="containerId">ID HTML контейнера</param>
    /// <param name="options">Параметры глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат инициализации</returns>
    ValueTask<Models.GlobeInitializationResult> InitializeGlobeAsync(string containerId, Models.GlobeOptions options, CancellationToken ct = default);

    /// <summary>
    /// Добавляет участников на глобус
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="participants">Список участников</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат добавления</returns>
    ValueTask<Models.GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Models.Participant> participants, CancellationToken ct = default);

    /// <summary>
    /// Обновляет позицию участника на глобусе
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="participantId">ID участника</param>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат обновления</returns>
    ValueTask<Models.GlobeOperationResult> UpdateParticipantPositionAsync(string containerId, Guid participantId, double latitude, double longitude, CancellationToken ct = default);

    /// <summary>
    /// Удаляет участника с глобуса
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="participantId">ID участника</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат удаления</returns>
    ValueTask<Models.GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId, CancellationToken ct = default);

    /// <summary>
    /// Центрирует глобус на указанных координатах
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="latitude">Широта</param>
    /// <param name="longitude">Долгота</param>
    /// <param name="zoom">Уровень масштабирования</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат центрирования</returns>
    ValueTask<Models.GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает уровень детализации глобуса
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="lod">Уровень детализации (0-3)</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат установки</returns>
    ValueTask<Models.GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod, CancellationToken ct = default);

    /// <summary>
    /// Включает/выключает режим автоповорота
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="enabled">Включить автоповорот</param>
    /// <param name="speed">Скорость поворота</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат операции</returns>
    ValueTask<Models.GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5, CancellationToken ct = default);

    /// <summary>
    /// Загружает данные стран для отображения на глобусе
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат загрузки</returns>
    ValueTask<Models.GlobeOperationResult> LoadCountriesDataAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Очищает глобус от всех объектов
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат очистки</returns>
    ValueTask<Models.GlobeOperationResult> ClearAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Получает текущее состояние глобуса
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Состояние глобуса</returns>
    ValueTask<Models.GlobeState> GetStateAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Уничтожает глобус и освобождает ресурсы
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат уничтожения</returns>
    ValueTask<Models.GlobeOperationResult> DisposeAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Проверяет доступность сервиса
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат проверки</returns>
    ValueTask<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Проверяет доступность конкретного глобуса
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат проверки</returns>
    ValueTask<bool> IsGlobeAvailableAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Устанавливает callback для уведомления о готовности глобуса
    /// </summary>
    /// <param name="containerId">ID контейнера глобуса</param>
    /// <param name="callback">Callback функция</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Результат установки</returns>
    ValueTask<Models.GlobeOperationResult> SetReadyCallbackAsync(string containerId, Func<Models.GlobeState, Task> callback, CancellationToken ct = default);
}