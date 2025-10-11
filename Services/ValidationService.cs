using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Validation;

namespace ZealousMindedPeopleGeo.Services;

/// <summary>
/// Сервис для валидации данных с использованием FluentValidation
/// </summary>
public class ValidationService : IValidationService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationService> _logger;
    private readonly Dictionary<Type, IValidator> _validators = new();

    public ValidationService(
        IServiceProvider serviceProvider,
        ILogger<ValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        // Регистрируем встроенные валидаторы
        RegisterValidator<Participant, ParticipantValidator>();
        RegisterValidator<ParticipantRegistrationModel, ParticipantRegistrationValidator>();
        RegisterValidator<GlobeOptions, GlobeOptionsValidator>();
        RegisterValidator<GeocodingRequest, GeocodingRequestValidator>();
        RegisterValidator<(double, double), CoordinateValidator>();

        _logger.LogInformation("ValidationService initialized with {ValidatorCount} validators", _validators.Count);
    }

    /// <summary>
    /// Зарегистрировать валидатор для типа
    /// </summary>
    public void RegisterValidator<T, TValidator>()
        where TValidator : IValidator<T>
    {
        try
        {
            var validator = _serviceProvider.GetService(typeof(TValidator)) as IValidator<T>;
            if (validator != null)
            {
                _validators[typeof(T)] = validator;
                _logger.LogDebug("Registered validator {ValidatorType} for type {Type}", typeof(TValidator).Name, typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register validator {ValidatorType} for type {Type}", typeof(TValidator).Name, typeof(T).Name);
        }
    }

    /// <summary>
    /// Валидировать объект
    /// </summary>
    public async Task<Models.ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        try
        {
            if (instance == null)
            {
                return Models.ValidationResult.Failure("", "Объект не может быть null");
            }

            if (_validators.TryGetValue(typeof(T), out var validator))
            {
                if (validator is IValidator<T> typedValidator)
                {
                    var result = await typedValidator.ValidateAsync(instance, cancellationToken);
                    var validationResult = ConvertFluentValidationResult(result);
                    LogValidationResult(typeof(T).Name, validationResult);
                    return validationResult;
                }
            }

            _logger.LogWarning("No validator found for type {Type}", typeof(T).Name);
            return Models.ValidationResult.Failure("", $"Валидатор для типа {typeof(T).Name} не найден");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating instance of type {Type}", typeof(T).Name);
            return Models.ValidationResult.Failure("", $"Ошибка валидации: {ex.Message}");
        }
    }

    /// <summary>
    /// Валидировать объект и выбросить исключение при ошибках
    /// </summary>
    public async Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Ошибки валидации для {typeof(T).Name}: {errors}");
        }
    }

    /// <summary>
    /// Валидировать географические координаты
    /// </summary>
    public async Task<Models.ValidationResult> ValidateCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var coordinates = (latitude, longitude);
        return await ValidateAsync(coordinates, cancellationToken);
    }

    /// <summary>
    /// Валидировать адрес для геокодирования
    /// </summary>
    public async Task<Models.ValidationResult> ValidateGeocodingRequestAsync(string address, string? language = null, string? countryCode = null, CancellationToken cancellationToken = default)
    {
        var request = new GeocodingRequest
        {
            Address = address,
            Language = language,
            CountryCode = countryCode
        };

        return await ValidateAsync(request, cancellationToken);
    }

    /// <summary>
    /// Проверить, что объект валиден
    /// </summary>
    public async Task<bool> IsValidAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken);
        return result.IsValid;
    }

    /// <summary>
    /// Получить первую ошибку валидации
    /// </summary>
    public async Task<string?> GetFirstErrorAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken);
        return result.Errors.FirstOrDefault()?.ErrorMessage;
    }

    /// <summary>
    /// Получить все ошибки валидации в виде словаря
    /// </summary>
    public async Task<Dictionary<string, List<string>>> GetErrorsByPropertyAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken);
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToList());
    }

    /// <summary>
    /// Получить все ошибки валидации в виде списка
    /// </summary>
    public async Task<List<string>> GetAllErrorsAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var result = await ValidateAsync(instance, cancellationToken);
        return result.Errors.Select(e => e.ErrorMessage).ToList();
    }

    private void LogValidationResult(string typeName, Models.ValidationResult result)
    {
        if (result.IsValid)
        {
            _logger.LogDebug("Validation successful for {Type}", typeName);
        }
        else
        {
            _logger.LogWarning("Validation failed for {Type} with {ErrorCount} errors: {Errors}",
                typeName, result.Errors.Count, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    private Models.ValidationResult ConvertFluentValidationResult(FluentValidation.Results.ValidationResult fluentResult)
    {
        if (fluentResult.IsValid)
        {
            return Models.ValidationResult.Success();
        }

        var errors = fluentResult.Errors.Select(e => new Models.ValidationError(e.PropertyName, e.ErrorMessage));
        return Models.ValidationResult.Failure(errors);
    }

    public void Dispose()
    {
        _validators.Clear();
    }
}

/// <summary>
/// Интерфейс для сервиса валидации
/// </summary>
public interface IValidationService
{
    void RegisterValidator<T, TValidator>() where TValidator : IValidator<T>;
    Task<Models.ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<Models.ValidationResult> ValidateCoordinatesAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<Models.ValidationResult> ValidateGeocodingRequestAsync(string address, string? language = null, string? countryCode = null, CancellationToken cancellationToken = default);
    Task<bool> IsValidAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<string?> GetFirstErrorAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<Dictionary<string, List<string>>> GetErrorsByPropertyAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<List<string>> GetAllErrorsAsync<T>(T instance, CancellationToken cancellationToken = default);
}

/// <summary>
/// Расширения для удобной работы с валидацией
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Валидировать участника и вернуть результат
    /// </summary>
    public static async Task<Models.ValidationResult> ValidateParticipantAsync(
        this IValidationService validationService,
        Participant participant,
        CancellationToken cancellationToken = default)
    {
        return await validationService.ValidateAsync(participant, cancellationToken);
    }

    /// <summary>
    /// Валидировать модель регистрации участника и вернуть результат
    /// </summary>
    public static async Task<Models.ValidationResult> ValidateParticipantRegistrationAsync(
        this IValidationService validationService,
        ParticipantRegistrationModel model,
        CancellationToken cancellationToken = default)
    {
        return await validationService.ValidateAsync(model, cancellationToken);
    }

    /// <summary>
    /// Валидировать настройки глобуса и вернуть результат
    /// </summary>
    public static async Task<Models.ValidationResult> ValidateGlobeOptionsAsync(
        this IValidationService validationService,
        GlobeOptions options,
        CancellationToken cancellationToken = default)
    {
        return await validationService.ValidateAsync(options, cancellationToken);
    }

    /// <summary>
    /// Проверить, что координаты валидны
    /// </summary>
    public static async Task<bool> AreCoordinatesValidAsync(
        this IValidationService validationService,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        var result = await validationService.ValidateCoordinatesAsync(latitude, longitude, cancellationToken);
        return result.IsValid;
    }

    /// <summary>
    /// Проверить, что адрес валиден для геокодирования
    /// </summary>
    public static async Task<bool> IsAddressValidForGeocodingAsync(
        this IValidationService validationService,
        string address,
        CancellationToken cancellationToken = default)
    {
        var result = await validationService.ValidateGeocodingRequestAsync(address, cancellationToken: cancellationToken);
        return result.IsValid;
    }
}