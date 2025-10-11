using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.Repositories
{
    /// <summary>
    /// Реализация репозитория участников с использованием Google Sheets
    /// </summary>
    public class GoogleSheetsParticipantRepository : IParticipantRepository
    {
        private readonly ZealousMindedPeopleGeoOptions _options;
        private readonly ILogger<GoogleSheetsParticipantRepository> _logger;
        private readonly IGoogleSheetsService _googleSheetsService;

        public GoogleSheetsParticipantRepository(
            IOptions<ZealousMindedPeopleGeoOptions> options,
            ILogger<GoogleSheetsParticipantRepository> logger,
            IGoogleSheetsService googleSheetsService)
        {
            _options = options.Value;
            _logger = logger;
            _googleSheetsService = googleSheetsService;
        }

        public async ValueTask<RepositoryResult> AddParticipantAsync(Participant participant, CancellationToken ct = default)
        {
            try
            {
                var sheetResult = await _googleSheetsService.AddParticipantAsync(participant);

                if (sheetResult.Success)
                {
                    return new RepositoryResult
                    {
                        Success = true,
                        RecordId = Guid.NewGuid(), // Используем Guid вместо номера строки
                    };
                }
                else
                {
                    return new RepositoryResult
                    {
                        Success = false,
                        ErrorMessage = sheetResult.ErrorMessage
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления участника в репозиторий");

                return new RepositoryResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async ValueTask<IEnumerable<Participant>> GetAllParticipantsAsync(CancellationToken ct = default)
        {
            try
            {
                return await _googleSheetsService.GetParticipantsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения участников из репозитория");
                return Enumerable.Empty<Participant>();
            }
        }

        public async ValueTask<Participant?> GetParticipantByIdAsync(Guid id, CancellationToken ct = default)
        {
            try
            {
                var participants = await GetAllParticipantsAsync();
                return participants.FirstOrDefault(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения участника по ID {Id}", id);
                return null;
            }
        }

        public async ValueTask<RepositoryResult> UpdateParticipantAsync(Participant participant, CancellationToken ct = default)
        {
            // В текущей реализации Google Sheets API поддерживает только добавление (append)
            // Для обновления можно реализовать дополнительную логику
            // Пока возвращаем ошибку с предложением добавить нового участника

            await Task.CompletedTask;
            return new RepositoryResult
            {
                Success = false,
                ErrorMessage = "Обновление участников в Google Sheets не реализовано. Добавьте участника как нового."
            };
        }

        public async ValueTask<RepositoryResult> DeleteParticipantAsync(Guid id, CancellationToken ct = default)
        {
            // Google Sheets API не поддерживает удаление отдельных строк
            // Можно реализовать через создание новой таблицы без удаляемой строки

            await Task.CompletedTask;
            return new RepositoryResult
            {
                Success = false,
                ErrorMessage = "Удаление участников в Google Sheets не реализовано из-за ограничений API."
            };
        }

        public async ValueTask<bool> IsAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                return await _googleSheetsService.CheckConnectionAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки доступности репозитория");
                return false;
            }
        }
    }
}