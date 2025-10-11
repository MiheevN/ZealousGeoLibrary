using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Repositories
{
    /// <summary>
    /// Интерфейс репозитория для работы с данными участников
    /// Позволяет абстрагироваться от конкретного источника данных (Google Sheets, база данных и т.д.)
    /// </summary>
    public interface IParticipantRepository
    {
        /// <summary>
        /// Добавляет нового участника
        /// </summary>
        /// <param name="participant">Данные участника</param>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Результат операции</returns>
        ValueTask<RepositoryResult> AddParticipantAsync(Participant participant, CancellationToken ct = default);

        /// <summary>
        /// Получает всех участников
        /// </summary>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Список участников</returns>
        ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default);

        /// <summary>
        /// Получает участника по ID
        /// </summary>
        /// <param name="id">ID участника</param>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Данные участника или null если не найден</returns>
        ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Обновляет данные участника
        /// </summary>
        /// <param name="participant">Обновленные данные участника</param>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Результат операции</returns>
        ValueTask<RepositoryResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default);

        /// <summary>
        /// Удаляет участника
        /// </summary>
        /// <param name="id">ID участника для удаления</param>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Результат операции</returns>
        ValueTask<RepositoryResult> DeleteParticipantAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Проверяет доступность репозитория
        /// </summary>
        /// <param name="ct">Токен отмены операции</param>
        /// <returns>Результат проверки</returns>
        ValueTask<bool> IsAvailableAsync(CancellationToken ct = default);
    }

    // Используется модель RepositoryResult из ZealousMindedPeopleGeo.Models
}