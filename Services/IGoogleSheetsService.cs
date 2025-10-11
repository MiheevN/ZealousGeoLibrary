using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services
{
    /// <summary>
    /// Интерфейс сервиса для работы с Google Sheets API
    /// </summary>
    public interface IGoogleSheetsService
    {
        /// <summary>
        /// Добавляет участника в Google Sheet
        /// </summary>
        /// <param name="participant">Данные участника</param>
        /// <returns>Результат операции</returns>
        Task<RegistrationResult> AddParticipantAsync(Participant participant);

        /// <summary>
        /// Получает всех участников из Google Sheet
        /// </summary>
        /// <returns>Список участников</returns>
        Task<IEnumerable<Participant>> GetParticipantsAsync();

        /// <summary>
        /// Создает Google Sheet если он не существует
        /// </summary>
        /// <returns>ID созданного листа</returns>
        Task<string> CreateSheetIfNotExistsAsync();

        /// <summary>
        /// Проверяет доступность Google Sheets API
        /// </summary>
        /// <returns>Результат проверки</returns>
        Task<bool> CheckConnectionAsync();
    }
}