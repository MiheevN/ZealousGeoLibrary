using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services
{
    /// <summary>
    /// Реализация сервиса для работы с Google Sheets API
    /// </summary>
    public class GoogleSheetsService : IGoogleSheetsService
    {
        private readonly ZealousMindedPeopleGeoOptions _options;
        private SheetsService? _sheetsService;
        private readonly ILogger<GoogleSheetsService> _logger;

        public GoogleSheetsService(
            IOptions<ZealousMindedPeopleGeoOptions> options,
            ILogger<GoogleSheetsService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        private async Task InitializeSheetsServiceAsync()
        {
            if (_sheetsService != null)
            {
                await Task.CompletedTask;
                return;
            }
    
            try
            {
                if (string.IsNullOrEmpty(_options.GoogleServiceAccountKey))
                {
                    throw new InvalidOperationException("Google Service Account Key не настроен");
                }
    
                GoogleCredential credential;
    
                // Используем сервис аккаунт ключ
                var credentialParameters = GoogleCredential.FromJson(_options.GoogleServiceAccountKey);
                credential = credentialParameters.CreateScoped(new[]
                {
                    "https://www.googleapis.com/auth/spreadsheets"
                });
    
                _sheetsService = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "ZealousMindedPeopleGeo"
                });
    
                _logger.LogInformation("Google Sheets сервис инициализирован успешно");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка инициализации Google Sheets сервиса");
                throw;
            }
        }

        public async Task<RegistrationResult> AddParticipantAsync(Participant participant)
        {
            try
            {
                await InitializeSheetsServiceAsync();

                if (_sheetsService == null)
                    throw new InvalidOperationException("Sheets service не инициализирован");

                var values = new List<IList<object>>
                {
                    new List<object>
                    {
                        participant.Timestamp.ToString("O"),
                        participant.Name,
                        participant.Address,
                        participant.Latitude,
                        participant.Longitude,
                        participant.City ?? "",
                        participant.Country ?? "",
                        participant.SocialMedia ?? "",
                        participant.Message ?? ""
                    }
                };

                var valueRange = new ValueRange { Values = values };

                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _options.GoogleSheetId, "Sheet1!A:I");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

                var response = await appendRequest.ExecuteAsync();

                _logger.LogInformation("Участник {Name} успешно добавлен в Google Sheet", participant.Name);

                return new RegistrationResult
                {
                    Success = true,
                    Participant = participant,
                    SheetRowNumber = response.Updates?.UpdatedRows
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления участника {Name} в Google Sheet", participant.Name);

                return new RegistrationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<IEnumerable<Participant>> GetParticipantsAsync()
        {
            try
            {
                await InitializeSheetsServiceAsync();

                if (_sheetsService == null)
                    throw new InvalidOperationException("Sheets service не инициализирован");

                var request = _sheetsService.Spreadsheets.Values.Get(_options.GoogleSheetId, "Sheet1!A2:I");
                var response = await request.ExecuteAsync();

                var participants = new List<Participant>();

                if (response.Values != null)
                {
                    foreach (var row in response.Values)
                    {
                        if (row.Count >= 5) // Минимум timestamp, name, address, lat, lng
                        {
                            try
                            {
                                var participant = new Participant
                                {
                                    Timestamp = DateTime.TryParse(row[0]?.ToString(), out var timestamp) ? timestamp : DateTime.UtcNow,
                                    Name = row[1]?.ToString() ?? "",
                                    Address = row[2]?.ToString() ?? "",
                                    Latitude = double.TryParse(row[3]?.ToString(), out var lat) ? lat : 0,
                                    Longitude = double.TryParse(row[4]?.ToString(), out var lng) ? lng : 0,
                                    City = row.Count > 5 ? row[5]?.ToString() : null,
                                    Country = row.Count > 6 ? row[6]?.ToString() : null,
                                    SocialMedia = row.Count > 7 ? row[7]?.ToString() : null,
                                    Message = row.Count > 8 ? row[8]?.ToString() : null
                                };

                                participants.Add(participant);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Ошибка обработки строки данных участника");
                            }
                        }
                    }
                }

                _logger.LogInformation("Загружено {Count} участников из Google Sheet", participants.Count);

                return participants;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения участников из Google Sheet");

                return Enumerable.Empty<Participant>();
            }
        }

        public async Task<string> CreateSheetIfNotExistsAsync()
        {
            try
            {
                await InitializeSheetsServiceAsync();

                // Проверяем существует ли таблица
                try
                {
                    if (_sheetsService == null)
                        throw new InvalidOperationException("Sheets service не инициализирован");

                    var getRequest = _sheetsService.Spreadsheets.Get(_options.GoogleSheetId);
                    await getRequest.ExecuteAsync();

                    // Таблица существует, проверяем заголовки
                    await EnsureHeadersExistAsync();

                    return _options.GoogleSheetId;
                }
                catch (Google.GoogleApiException ex) when (ex.Error.Code == 404)
                {
                    // Таблица не существует, создаем новую
                    return await CreateNewSheetAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания/проверки Google Sheet");
                throw;
            }
        }

        private async Task<string> CreateNewSheetAsync()
        {
            if (_sheetsService == null)
                throw new InvalidOperationException("Sheets service не инициализирован");

            var spreadsheet = new Spreadsheet
            {
                Properties = new SpreadsheetProperties
                {
                    Title = "Like-Minded People Geography"
                }
            };

            var createRequest = _sheetsService.Spreadsheets.Create(spreadsheet);
            var response = await createRequest.ExecuteAsync();

            var newSheetId = response.SpreadsheetId;

            _logger.LogInformation("Создана новая таблица с ID: {SheetId}", newSheetId);

            // Добавляем заголовки
            await EnsureHeadersExistAsync(newSheetId);

            return newSheetId!;
        }

        private async Task EnsureHeadersExistAsync(string? sheetId = null)
        {
            if (_sheetsService == null)
                throw new InvalidOperationException("Sheets service не инициализирован");

            var targetSheetId = sheetId ?? _options.GoogleSheetId;

            var headers = new List<IList<object>>
            {
                new List<object>
                {
                    "Timestamp", "Name", "Address", "Latitude", "Longitude",
                    "City", "Country", "SocialMedia", "Message"
                }
            };

            var valueRange = new ValueRange { Values = headers };

            var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, targetSheetId, "Sheet1!A1:I1");
            updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            await updateRequest.ExecuteAsync();

            _logger.LogInformation("Заголовки таблицы обновлены");
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                await InitializeSheetsServiceAsync();

                if (_sheetsService == null)
                    return false;

                var request = _sheetsService.Spreadsheets.Get(_options.GoogleSheetId);
                await request.ExecuteAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подключения к Google Sheets API");
                return false;
            }
        }
    }
}