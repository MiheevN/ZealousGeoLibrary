using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Mapping;

/// <summary>
/// Интерфейс посредника для работы с 3D глобусом
/// Обеспечивает чистую интеграцию между Blazor и JavaScript
/// </summary>
public interface IGlobeMediator
{
    /// <summary>
    /// Инициализирует глобус с указанными параметрами
    /// </summary>
    Task<GlobeInitializationResult> InitializeGlobeAsync(string containerId, GlobeOptions options);

    /// <summary>
    /// Добавляет участника на глобус
    /// </summary>
    Task<GlobeOperationResult> AddParticipantAsync(string containerId, Participant participant);

    /// <summary>
    /// Добавляет нескольких участников на глобус
    /// </summary>
    Task<GlobeOperationResult> AddParticipantsAsync(string containerId, IEnumerable<Participant> participants);

    /// <summary>
    /// Удаляет участника с глобуса
    /// </summary>
    Task<GlobeOperationResult> RemoveParticipantAsync(string containerId, Guid participantId);

    /// <summary>
    /// Центрирует глобус на координатах
    /// </summary>
    Task<GlobeOperationResult> CenterOnAsync(string containerId, double latitude, double longitude, double zoom = 2.0);

    /// <summary>
    /// Устанавливает уровень детализации
    /// </summary>
    Task<GlobeOperationResult> SetLevelOfDetailAsync(string containerId, int lod);

    /// <summary>
    /// Управляет автоповоротом глобуса
    /// </summary>
    Task<GlobeOperationResult> SetAutoRotationAsync(string containerId, bool enabled, double speed = 0.5);

    /// <summary>
    /// Получает текущее состояние глобуса
    /// </summary>
    Task<GlobeState> GetStateAsync(string containerId);

    /// <summary>
    /// Очищает все объекты с глобуса
    /// </summary>
    Task<GlobeOperationResult> ClearAsync(string containerId);

    /// <summary>
    /// Освобождает ресурсы глобуса
    /// </summary>
    Task<GlobeOperationResult> DisposeAsync(string containerId);

    /// <summary>
    /// Проверяет доступность сервиса
    /// </summary>
    Task<bool> IsAvailableAsync();
}