namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Результат операции
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Результат операции репозитория
/// </summary>
public class RepositoryResult : OperationResult
{
    public Guid RecordId { get; set; }
}

/// <summary>
/// Результат геокодирования
/// </summary>
public class GeocodingResult
{
    /// <summary>
    /// Широта
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Долгота
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Форматированный адрес
    /// </summary>
    public string FormattedAddress { get; set; } = string.Empty;

    /// <summary>
    /// Успешность операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Сообщение об ошибке
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Результат валидации данных в библиотеке ZealousMindedPeopleGeo
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();

    public static ValidationResult Success() => new ValidationResult { IsValid = true };
    public static ValidationResult Failure(IEnumerable<ValidationError> errors) => new ValidationResult { IsValid = false, Errors = errors.ToList() };
    public static ValidationResult Failure(string propertyName, string errorMessage) =>
        new ValidationResult { IsValid = false, Errors = new List<ValidationError> { new ValidationError(propertyName, errorMessage) } };
}

/// <summary>
/// Ошибка валидации
/// </summary>
public class ValidationError
{
    public string PropertyName { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    public ValidationError() { }

    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}