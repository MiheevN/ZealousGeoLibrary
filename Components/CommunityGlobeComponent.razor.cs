using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ZealousMindedPeopleGeo.Services.Mapping;
using ZealousMindedPeopleGeo.Services.Repositories;
using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Components;

public partial class CommunityGlobeComponent
{
    /// <summary>
    /// Уникальный идентификатор контейнера для глобуса
    /// </summary>
    [Parameter] public string ContainerId { get; set; } = $"globe-{Guid.NewGuid():N}";

    /// <summary>
    /// Ширина контейнера глобуса в пикселях
    /// </summary>
    [Parameter] public int Width { get; set; } = 800;

    /// <summary>
    /// Высота контейнера глобуса в пикселях
    /// </summary>
    [Parameter] public int Height { get; set; } = 600;

    /// <summary>
    /// Показывать панель управления
    /// </summary>
    [Parameter] public bool ShowControls { get; set; } = true;

    /// <summary>
    /// Показывать информацию об участниках при клике
    /// </summary>
    [Parameter] public bool ShowParticipantInfo { get; set; } = true;

    /// <summary>
    /// Параметры глобуса
    /// </summary>
    [Parameter] public GlobeOptions? Options { get; set; }

    /// <summary>
    /// Событие клика по участнику
    /// </summary>
    [Parameter] public EventCallback<Participant> OnParticipantClick { get; set; }

    /// <summary>
    /// Событие инициализации глобуса
    /// </summary>
    [Parameter] public EventCallback<GlobeInitializationResult> OnGlobeInitialized { get; set; }

    /// <summary>
    /// Событие изменения состояния глобуса
    /// </summary>
    [Parameter] public EventCallback<GlobeState> OnStateChanged { get; set; }

    /// <summary>
    /// Показывать панель управления участниками
    /// </summary>
    [Parameter] public bool ShowParticipantManagement { get; set; } = true;

    /// <summary>
    /// Событие добавления участника
    /// </summary>
    [Parameter] public EventCallback<Participant> OnParticipantAdded { get; set; }

    /// <summary>
    /// Событие удаления участника
    /// </summary>
    [Parameter] public EventCallback<int> OnParticipantRemoved { get; set; }

    [Inject] private IGlobeMediator GlobeMediator { get; set; } = default!;
    [Inject] private IParticipantRepository ParticipantRepository { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private bool _isInitializing = false;
    private bool _isInitialized = false;
    private string? _errorMessage;
    private Participant? _selectedParticipant;
    private int _participantCount;
    private int _countryCount;
    private int _currentLod = 2;
    private bool _isAutoRotating = true;
    private GlobeState? _currentState;
    private System.Timers.Timer? _updateTimer;

    // Переменные для управления участниками
    private Participant _newParticipant = new();
    private int _participantIdToRemove;
    private bool _isAddingParticipant = false;
    private bool _isRemovingParticipant = false;
    private string? _operationMessage;
    private string? _operationError;
    private bool _isParticipantPanelCollapsed = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Инициализируем скрипты глобуса
            await InitializeGlobeScriptsAsync();

            await InitializeGlobeAsync();
            await LoadParticipantsAsync();

            // Настраиваем таймер для периодического обновления данных
            _updateTimer = new System.Timers.Timer(30000); // Каждые 30 секунд
            _updateTimer.Elapsed += async (s, e) =>
            {
                try
                {
                    // Используем InvokeAsync для корректного обновления состояния в Blazor Server
                    await InvokeAsync(async () =>
                    {
                        await LoadParticipantsAsync();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка в таймере обновления участников: {ex.Message}");
                }
            };
            _updateTimer.Start();
        }
    }

    private async Task InitializeGlobeAsync()
    {
        _isInitializing = true;
        _errorMessage = null;
        StateHasChanged();

        const int maxRetries = 3;
        const int retryDelay = 200; // миллисекунды

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Добавляем дополнительную задержку между попытками
                if (attempt > 1)
                {
                    await Task.Delay(retryDelay * attempt);
                }

                var globeOptions = Options ?? new GlobeOptions
                {
                    Width = Width,
                    Height = Height,
                    AutoRotate = true,
                    EnableMouseControls = true,
                    LevelOfDetail = _currentLod
                };

                var result = await GlobeMediator.InitializeGlobeAsync(ContainerId, globeOptions);

                if (result.Success)
                {
                    _isInitialized = true;
                    await OnGlobeInitialized.InvokeAsync(result);
                    await UpdateGlobeStateAsync();
                    return; // Успешная инициализация, выходим из цикла
                }
                else
                {
                    _errorMessage = result.ErrorMessage ?? "Неизвестная ошибка инициализации";
                    if (attempt == maxRetries)
                    {
                        break; // Последняя попытка неудачна
                    }
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Ошибка инициализации (попытка {attempt}/{maxRetries}): {ex.Message}";
                if (attempt == maxRetries)
                {
                    break; // Последняя попытка неудачна
                }
            }
        }

        _isInitializing = false;
        InvokeAsync(StateHasChanged);
    }

    private async Task LoadParticipantsAsync()
    {
        if (!_isInitialized) return;

        try
        {
            var participants = await ParticipantRepository.GetAllParticipantsAsync();

            var result = await GlobeMediator.AddParticipantsAsync(participants);

            if (result.Success)
            {
                _participantCount = participants.Count();
                await UpdateGlobeStateAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Ошибка загрузки участников: {ex.Message}";
            StateHasChanged();
        }
    }

    private async Task InitializeGlobeScriptsAsync()
    {
        try
        {
            Console.WriteLine("🔍 Начало инициализации скриптов глобуса");

            // Шаг 1: Проверяем, не инициализированы ли уже скрипты
            try
            {
                var initState = await JSRuntime.InvokeAsync<object>("eval", "window.globeInitializationState");
                var stateDict = initState as System.Collections.Generic.Dictionary<string, object>;

                if (stateDict != null &&
                    stateDict.TryGetValue("functionsAvailable", out var functionsAvailable) &&
                    (bool)functionsAvailable)
                {
                    Console.WriteLine("✅ Скрипты глобуса уже инициализированы");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ℹ️ Скрипты еще не загружены: {ex.Message}");
            }

            // Шаг 2: Если скрипты не загружены, загружаем их динамически
            Console.WriteLine("📥 Динамическая загрузка скриптов глобуса...");

            try
            {

                await JSRuntime.InvokeVoidAsync("import", "_content/ZealousMindedPeopleGeo/js/community-globe.js");

                Console.WriteLine("✅ Community Globe загружен");

                // Даем время на полную инициализацию
                await Task.Delay(1000);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка динамической загрузки скриптов: {ex.Message}");
                throw;
            }

            // Шаг 3: Ждем полной инициализации скриптов (максимум 10 секунд)
            var maxWaitTime = 10000;
            var waitStep = 200;

            for (int i = 0; i < maxWaitTime / waitStep; i++)
            {
                await Task.Delay(waitStep);

                try
                {
                    // Проверяем глобальное состояние инициализации
                    var initState = await JSRuntime.InvokeAsync<object>("eval", "window.globeInitializationState");
                    var stateDict = initState as System.Collections.Generic.Dictionary<string, object>;

                    if (stateDict != null)
                    {
                        // Проверяем что скрипты загружены
                        if (stateDict.TryGetValue("scriptsLoaded", out var scriptsLoaded) && (bool)scriptsLoaded)
                        {
                            Console.WriteLine($"✅ Скрипты загружены на попытке {i + 1}");

                            // Проверяем что функции доступны
                            if (stateDict.TryGetValue("functionsAvailable", out var functionsAvailable) && (bool)functionsAvailable)
                            {
                                Console.WriteLine($"🎉 Все функции доступны на попытке {i + 1}");
                                return; // Всё готово
                            }
                            else
                            {
                                Console.WriteLine($"⏳ Попытка {i + 1}: функции еще не доступны");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⏳ Попытка {i + 1}: скрипты еще не загружены");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⏳ Попытка {i + 1}: состояние инициализации недоступно");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⏳ Попытка {i + 1}: ошибка проверки состояния - {ex.Message}");
                }
            }

            Console.WriteLine("⚠️ Инициализация скриптов не завершена в таймаут, но продолжаем");
            // Не выбрасываем исключение, чтобы компонент мог работать
        }
        catch (Exception ex)
        {
            _errorMessage = $"Ошибка инициализации скриптов: {ex.Message}";
            Console.WriteLine($"💥 Критическая ошибка инициализации скриптов: {ex.Message}");
            StateHasChanged();
        }
    }

    private async Task RetryInitializationAsync()
    {
        if (_isInitializing || _isInitialized) return;

        await InitializeGlobeAsync();
    }

    private async Task ToggleRotationAsync()
    {
        if (!_isInitialized) return;

        var newRotationState = !_isAutoRotating;
        var speed = newRotationState ? 0.5 : 0.0;
        var result = await GlobeMediator.SetAutoRotationAsync(newRotationState, speed);

        if (result.Success)
        {
            _isAutoRotating = newRotationState;
            await UpdateGlobeStateAsync();
        }
    }

    private async Task OnLodChanged(ChangeEventArgs e)
    {
        if (!_isInitialized || !int.TryParse(e.Value?.ToString(), out var lod)) return;

        _currentLod = lod;
        var result = await GlobeMediator.SetLevelOfDetailAsync(lod);

        if (result.Success)
        {
            await UpdateGlobeStateAsync();
        }
    }

    private async Task RefreshDataAsync()
    {
        await LoadParticipantsAsync();
    }

    private async Task CenterOnUserAsync()
    {
        if (!_isInitialized) return;

        // Центрируем на Москву по умолчанию
        var result = await GlobeMediator.CenterOnAsync(55.7558, 37.6176);

        if (result.Success)
        {
            await UpdateGlobeStateAsync();
        }
    }

    private void CloseParticipantInfo()
    {
        _selectedParticipant = null;
        StateHasChanged();
    }

    private void ToggleParticipantPanel()
    {
        _isParticipantPanelCollapsed = !_isParticipantPanelCollapsed;
        StateHasChanged();
    }

    private void ClearParticipantForm()
    {
        _newParticipant = new Participant();
        StateHasChanged();
    }

    /// <summary>
    /// Проверяет доступность JavaScript функций глобуса
    /// </summary>
    private async Task<bool> IsJavaScriptAvailableAsync(string functionName = "safeAddTestParticipant")
    {
        try
        {
            return await JSRuntime.InvokeAsync<bool>("eval", $"typeof window.{functionName} === 'function'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ JavaScript недоступен: {ex.Message}");
            return false;
        }
    }

    private async Task CenterOnFormCoordinates()
    {
        if (!_isInitialized || !_newParticipant.Latitude.HasValue || !_newParticipant.Longitude.HasValue) return;

        var result = await GlobeMediator.CenterOnAsync(_newParticipant.Latitude.Value, _newParticipant.Longitude.Value);

        if (result.Success)
        {
            ShowOperationMessage("✅ Центрировано на координатах формы", false);
        }
        else
        {
            ShowOperationMessage($"❌ Ошибка центрирования: {result.ErrorMessage}", true);
        }
    }

    private async Task AddParticipantAsync()
    {
        if (!_isInitialized || _isAddingParticipant) return;

        _isAddingParticipant = true;
        _operationMessage = null;
        _operationError = null;
        StateHasChanged();

        try
        {
            // Валидация данных
            if (string.IsNullOrWhiteSpace(_newParticipant.Name))
            {
                ShowOperationMessage("❌ Введите имя участника", true);
                return;
            }

            if (!_newParticipant.Latitude.HasValue || !_newParticipant.Longitude.HasValue)
            {
                ShowOperationMessage("❌ Введите координаты", true);
                return;
            }

            if (_newParticipant.Latitude < -90 || _newParticipant.Latitude > 90)
            {
                ShowOperationMessage("❌ Широта должна быть от -90 до 90", true);
                return;
            }

            if (_newParticipant.Longitude < -180 || _newParticipant.Longitude > 180)
            {
                ShowOperationMessage("❌ Долгота должна быть от -180 до 180", true);
                return;
            }

            // Создаем участника для JavaScript
            var jsParticipant = new
            {
                id = DateTime.Now.Ticks,
                name = _newParticipant.Name,
                latitude = _newParticipant.Latitude.Value,
                longitude = _newParticipant.Longitude.Value,
                location = $"{_newParticipant.Name} ({_newParticipant.Latitude.Value:F4}, {_newParticipant.Longitude.Value:F4})"
            };

            // Создаем участника для репозитория
            var participant = new Participant
            {
                Id = Guid.NewGuid(),
                Name = _newParticipant.Name,
                Address = $"{_newParticipant.Latitude.Value:F4}, {_newParticipant.Longitude.Value:F4}",
                Email = "user@example.com",
                Location = _newParticipant.Name,
                City = _newParticipant.City ?? _newParticipant.Name,
                Country = _newParticipant.Country ?? "Россия",
                Latitude = _newParticipant.Latitude.Value,
                Longitude = _newParticipant.Longitude.Value,
                Message = _newParticipant.Message ?? $"Добавлен через форму: {_newParticipant.Name}",
                RegisteredAt = DateTime.UtcNow,
                Timestamp = DateTime.UtcNow
            };

            // Добавляем в репозиторий
            var addResult = await ParticipantRepository.AddParticipantAsync(participant);
            bool result = addResult.Success;
            
            Console.WriteLine($"Результат добавления участника: {result}");

            if (result)
            {
                ShowOperationMessage($"✅ Участник '{_newParticipant.Name}' добавлен!", false);
                ClearParticipantForm();
                await LoadParticipantsAsync(); // Обновляем глобус через сервис
                await OnParticipantAdded.InvokeAsync(participant);
            }
            else
            {
                ShowOperationMessage($"❌ Не удалось добавить участника '{_newParticipant.Name}'", true);
            }
        }
        catch (Exception ex)
        {
            ShowOperationMessage($"❌ Ошибка: {ex.Message}", true);
        }
        finally
        {
            _isAddingParticipant = false;
            StateHasChanged();
        }
    }

    private async Task AddQuickParticipantAsync(string name, double latitude, double longitude)
    {
        if (!_isInitialized || _isAddingParticipant) return;

        _isAddingParticipant = true;
        _operationMessage = null;
        _operationError = null;
        StateHasChanged();

        try
        {
            var jsParticipant = new
            {
                id = DateTime.Now.Ticks,
                name = name,
                latitude = latitude,
                longitude = longitude,
                location = $"{name} ({latitude:F4}, {longitude:F4})"
            };

            Console.WriteLine($"🔍 Перед вызовом safeAddTestParticipant (быстрое добавление): {jsParticipant.name} ({jsParticipant.latitude}, {jsParticipant.longitude})");

            // Проверяем доступность функции перед вызовом
            bool isFunctionAvailable = false;
            try
            {
                isFunctionAvailable = await JSRuntime.InvokeAsync<bool>("eval", $"window.globeModule && typeof window.globeModule.safeAddTestParticipant === 'function'");
                Console.WriteLine($"Функция safeAddTestParticipant доступна: {isFunctionAvailable}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ JavaScript недоступен в тестовом окружении: {ex.Message}");
                isFunctionAvailable = false;
            }

            bool result = false;
            if (isFunctionAvailable)
            {
                // JavaScript доступен - используем модульную функцию
                result = await JSRuntime.InvokeAsync<bool>("eval", $"window.globeModule.safeAddTestParticipant({System.Text.Json.JsonSerializer.Serialize(jsParticipant)})");
                Console.WriteLine($"Результат быстрого добавления участника через JS: {result}");
            }
            else
            {
                // JavaScript недоступен (тестовое окружение) - используем репозиторий напрямую
                Console.WriteLine("🔄 JS недоступен, добавляем участника через репозиторий");
                var participant = new Participant
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Address = $"{latitude:F4}, {longitude:F4}", // Обязательное поле
                    Email = "user@example.com", // Обязательное поле
                    Location = name, // Обязательное поле
                    City = name,
                    Country = "Россия",
                    Latitude = latitude,
                    Longitude = longitude,
                    Message = $"Быстро добавлен: {name}",
                    RegisteredAt = DateTime.UtcNow,
                    Timestamp = DateTime.UtcNow
                };

                var addResult = await ParticipantRepository.AddParticipantAsync(participant);
                result = addResult.Success;
                Console.WriteLine($"Результат добавления участника через репозиторий: {result}");
            }

            if (result)
            {
                ShowOperationMessage($"✅ Участник '{name}' добавлен!", false);
                await LoadParticipantsAsync();
            }
            else
            {
                ShowOperationMessage($"❌ Не удалось добавить участника '{name}'", true);
            }
        }
        catch (Exception ex)
        {
            ShowOperationMessage($"❌ Ошибка: {ex.Message}", true);
        }
        finally
        {
            _isAddingParticipant = false;
            StateHasChanged();
        }
    }

    private async Task AddRandomParticipantAsync()
    {
        var randomCities = new[]
        {
            new { Name = "Токио", Lat = 35.6762, Lng = 139.6503 },
            new { Name = "Лондон", Lat = 51.5074, Lng = -0.1278 },
            new { Name = "Нью-Йорк", Lat = 40.7128, Lng = -74.0060 },
            new { Name = "Париж", Lat = 48.8566, Lng = 2.3522 },
            new { Name = "Сидней", Lat = -33.8688, Lng = 151.2093 },
            new { Name = "Рио-де-Жанейро", Lat = -22.9068, Lng = -43.1729 },
            new { Name = "Кейптаун", Lat = -33.9249, Lng = 18.4241 },
            new { Name = "Мумбаи", Lat = 19.0760, Lng = 72.8777 }
        };

        var random = new Random();
        var city = randomCities[random.Next(randomCities.Length)];

        await AddQuickParticipantAsync(city.Name, city.Lat, city.Lng);
    }

    private async Task RemoveParticipantAsync()
    {
        if (!_isInitialized || _isRemovingParticipant || _participantIdToRemove == 0) return;

        _isRemovingParticipant = true;
        _operationMessage = null;
        _operationError = null;
        StateHasChanged();

        try
        {
            // Проверяем доступность функции перед вызовом
            bool isFunctionAvailable = false;
            try
            {
                isFunctionAvailable = await JSRuntime.InvokeAsync<bool>("eval", $"typeof window.removeParticipant === 'function'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ JavaScript недоступен в тестовом окружении: {ex.Message}");
                isFunctionAvailable = false;
            }

            bool result = false;
            if (isFunctionAvailable)
            {
                // JavaScript доступен - используем его
                result = await JSRuntime.InvokeAsync<bool>("removeParticipant", _participantIdToRemove);
                Console.WriteLine($"Результат удаления участника через JS: {result}");
            }
            else
            {
                // JavaScript недоступен (тестовое окружение) - используем репозиторий напрямую
                Console.WriteLine("🔄 JS недоступен, удаляем участника через репозиторий");
                // Для удаления по ID нам нужно найти участника по ID в репозитории
                var allParticipants = await ParticipantRepository.GetAllParticipantsAsync();
                var participantToRemove = allParticipants.FirstOrDefault(p => p.Id.GetHashCode() == _participantIdToRemove);

                if (participantToRemove != null)
                {
                    var removeResult = await ParticipantRepository.DeleteParticipantAsync(participantToRemove.Id);
                    result = removeResult.Success;
                    Console.WriteLine($"Результат удаления участника через репозиторий: {result}");
                }
                else
                {
                    Console.WriteLine($"Участник с ID {_participantIdToRemove} не найден в репозитории");
                    result = false;
                }
            }

            if (result)
            {
                ShowOperationMessage($"✅ Участник с ID {_participantIdToRemove} удален", false);
                _participantIdToRemove = 0;
                await LoadParticipantsAsync();
                await OnParticipantRemoved.InvokeAsync(_participantIdToRemove);
            }
            else
            {
                ShowOperationMessage($"❌ Не удалось удалить участника с ID {_participantIdToRemove}", true);
            }
        }
        catch (Exception ex)
        {
            ShowOperationMessage($"❌ Ошибка: {ex.Message}", true);
        }
        finally
        {
            _isRemovingParticipant = false;
            StateHasChanged();
        }
    }

    private void ShowOperationMessage(string message, bool isError)
    {
        _operationMessage = message;
        _operationError = isError ? message : null;

        // Автоматически скрываем сообщение через 3 секунды
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            _operationMessage = null;
            _operationError = null;
            InvokeAsync(StateHasChanged);
        });
    }

    private async Task UpdateGlobeStateAsync()
    {
        if (!_isInitialized) return;

        try
        {
            _currentState = await GlobeMediator.GetStateAsync();

            if (_currentState != null)
            {
                _participantCount = _currentState.ParticipantCount;
                _countryCount = _currentState.CountryCount;
                _isAutoRotating = _currentState.IsAutoRotating;
                _currentLod = _currentState.CurrentLevelOfDetail;

                await OnStateChanged.InvokeAsync(_currentState);
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Ошибка обновления состояния: {ex.Message}";
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _updateTimer?.Stop();
        _updateTimer?.Dispose();

        if (_isInitialized)
        {
            await GlobeMediator.DisposeAsync();
        }
    }

    void IDisposable.Dispose()
    {
        _ = DisposeAsync();
    }
}