using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services
{
    /// <summary>
    /// Интерфейс сервиса для управления участниками
    /// </summary>
    public interface IParticipantService
    {
        /// <summary>
        /// Регистрирует нового участника
        /// </summary>
        /// <param name="model">Модель регистрации</param>
        /// <returns>Результат регистрации</returns>
        Task<RegistrationResult> RegisterParticipantAsync(Models.ParticipantRegistrationModel model);

        /// <summary>
        /// Получает всех участников
        /// </summary>
        /// <returns>Список участников</returns>
        Task<IEnumerable<Participant>> GetAllParticipantsAsync();

        /// <summary>
        /// Валидирует модель регистрации
        /// </summary>
        /// <param name="model">Модель для валидации</param>
        /// <returns>Результат валидации</returns>
        Task<ValidationResult> ValidateRegistrationAsync(Models.ParticipantRegistrationModel model);

        /// <summary>
        /// Инициализирует хранилище данных (Google Sheet)
        /// </summary>
        /// <returns>Результат инициализации</returns>
        Task<bool> InitializeStorageAsync();
    }

    /// <summary>
    /// Результат валидации
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

}