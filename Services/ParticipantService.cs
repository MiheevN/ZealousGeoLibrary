using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Реализация сервиса для управления участниками
/// </summary>
public class ParticipantService : IParticipantService
{
    private readonly IGoogleSheetsService _sheetsService;
    private readonly IGoogleMapsService _mapsService;
    private readonly ZealousMindedPeopleGeoOptions _options;
    private readonly ILogger<ParticipantService> _logger;

    public ParticipantService(
        IGoogleSheetsService sheetsService,
        IGoogleMapsService mapsService,
        IOptions<ZealousMindedPeopleGeoOptions> options,
        ILogger<ParticipantService> logger)
    {
        _sheetsService = sheetsService;
        _mapsService = mapsService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RegistrationResult> RegisterParticipantAsync(ParticipantRegistrationModel model)
    {
        try
        {
            // Валидация модели
            var validationResult = await ValidateRegistrationAsync(model);
            if (!validationResult.IsValid)
            {
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors)
                };
            }

            // Геокодирование адреса
            double latitude = 0;
            double longitude = 0;

            if (!string.IsNullOrWhiteSpace(model.Address))
            {
                var geocodingResult = await _mapsService.GeocodeAddressAsync(model.Address);

                if (!geocodingResult.Success)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        ErrorMessage = $"Не удалось найти координаты для адреса '{model.Address}': {geocodingResult.ErrorMessage}"
                    };
                }

                latitude = geocodingResult.Latitude;
                longitude = geocodingResult.Longitude;
            }
            else
            {
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = "Необходимо указать адрес для геокодирования"
                };
            }

            // Создаем участника
            var participant = new Participant
            {
                Name = model.Name,
                Address = model.Address,
                Latitude = latitude,
                Longitude = longitude,
                SocialMedia = model.SocialMedia,
                Message = model.Message
            };

            // Добавляем в Google Sheet
            var sheetResult = await _sheetsService.AddParticipantAsync(participant);

            if (!sheetResult.Success)
            {
                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка сохранения в таблицу: {sheetResult.ErrorMessage}"
                };
            }

            _logger.LogInformation("Участник {Name} успешно зарегистрирован", participant.Name);

            return new RegistrationResult
            {
                Success = true,
                Participant = participant,
                SheetRowNumber = sheetResult.SheetRowNumber
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка регистрации участника {Name}", model.Name);

            return new RegistrationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        try
        {
            return await _sheetsService.GetParticipantsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка получения списка участников");
            return Enumerable.Empty<Participant>();
        }
    }

    public async Task<ValidationResult> ValidateRegistrationAsync(ParticipantRegistrationModel model)
    {
        var result = new ValidationResult { IsValid = true };

        // Проверяем обязательные поля
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            result.IsValid = false;
            result.Errors.Add("Имя обязательно для заполнения");
        }

        if (string.IsNullOrWhiteSpace(model.Address))
        {
            result.IsValid = false;
            result.Errors.Add("Адрес обязателен для заполнения");
        }

        // Проверяем длину текстовых полей
        if (!string.IsNullOrWhiteSpace(model.SocialMedia) && model.SocialMedia.Length > 200)
        {
            result.IsValid = false;
            result.Errors.Add("Информация о социальных сетях слишком длинная");
        }

        if (!string.IsNullOrWhiteSpace(model.Message) && model.Message.Length > 500)
        {
            result.IsValid = false;
            result.Errors.Add("Сообщение слишком длинное");
        }

        return result;
    }

    public async Task<bool> InitializeStorageAsync()
    {
        try
        {
            await _sheetsService.CreateSheetIfNotExistsAsync();

            var sheetsConnection = await _sheetsService.CheckConnectionAsync();
            var mapsConnection = await _mapsService.CheckConnectionAsync();

            if (!sheetsConnection)
            {
                _logger.LogError("Не удалось подключиться к Google Sheets API");
                return false;
            }

            if (!mapsConnection)
            {
                _logger.LogWarning("Не удалось подключиться к Google Maps API");
            }

            _logger.LogInformation("Хранилище данных инициализировано успешно");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка инициализации хранилища данных");
            return false;
        }
    }
}