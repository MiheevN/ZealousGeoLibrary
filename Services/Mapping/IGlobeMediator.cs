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
    Task<GlobeOperationResult> AddParticipantAsync(Participant participant);

    /// <summary>
    /// Добавляет нескольких участников на глобус
    /// </summary>
    Task<GlobeOperationResult> AddParticipantsAsync(IEnumerable<Participant> participants);

    /// <summary>
    /// Удаляет участника с глобуса
    /// </summary>
    Task<GlobeOperationResult> RemoveParticipantAsync(Guid participantId);

    /// <summary>
    /// Центрирует глобус на координатах
    /// </summary>
    Task<GlobeOperationResult> CenterOnAsync(double latitude, double longitude, double zoom = 2.0);

    /// <summary>
    /// Устанавливает уровень детализации
    /// </summary>
    Task<GlobeOperationResult> SetLevelOfDetailAsync(int lod);

    /// <summary>
    /// Управляет автоповоротом глобуса
    /// </summary>
    Task<GlobeOperationResult> SetAutoRotationAsync(bool enabled, double speed = 0.5);

    /// <summary>
    /// Получает текущее состояние глобуса
    /// </summary>
    Task<GlobeState> GetStateAsync();

    /// <summary>
    /// Очищает все объекты с глобуса
    /// </summary>
    Task<GlobeOperationResult> ClearAsync();

    /// <summary>
    /// Освобождает ресурсы глобуса
    /// </summary>
    Task<GlobeOperationResult> DisposeAsync();

    /// <summary>
    /// Проверяет доступность сервиса
    /// </summary>
    Task<bool> IsAvailableAsync();
}