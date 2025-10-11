using System.ComponentModel.DataAnnotations;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Модель участника сообщества для 3D глобуса
/// </summary>
public class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required(ErrorMessage = "Имя обязательно для заполнения")]
    [StringLength(100, ErrorMessage = "Имя не может превышать 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес обязателен для заполнения")]
    [StringLength(200, ErrorMessage = "Адрес не может превышать 200 символов")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен для заполнения")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    [StringLength(254, ErrorMessage = "Email не может превышать 254 символа")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Местоположение обязательно для заполнения")]
    [StringLength(200, ErrorMessage = "Местоположение не может превышать 200 символов")]
    public string Location { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Широта должна быть в диапазоне от -90 до 90")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Долгота должна быть в диапазоне от -180 до 180")]
    public double? Longitude { get; set; }

    [StringLength(100, ErrorMessage = "Город не может превышать 100 символов")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "Страна не может превышать 100 символов")]
    public string? Country { get; set; }

    [StringLength(200, ErrorMessage = "Социальные сети не могут превышать 200 символов")]
    public string? SocialMedia { get; set; }

    [StringLength(500, ErrorMessage = "Сообщение не может превышать 500 символов")]
    public string? Message { get; set; }

    public string? LifeGoals { get; set; }

    public string? Skills { get; set; }

    public SocialContacts? SocialContacts { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Модель социальных контактов участника
/// </summary>
public class SocialContacts
{
    [Url(ErrorMessage = "Некорректный URL Discord")]
    public string? Discord { get; set; }

    [Url(ErrorMessage = "Некорректный URL Telegram")]
    public string? Telegram { get; set; }

    [Url(ErrorMessage = "Некорректный URL VK")]
    public string? Vk { get; set; }

    [Url(ErrorMessage = "Некорректный URL сайта")]
    public string? Website { get; set; }
}

/// <summary>
/// Модель для регистрации участника
/// </summary>
public class ParticipantRegistrationModel
{
    [Required(ErrorMessage = "Имя обязательно для заполнения")]
    [StringLength(100, ErrorMessage = "Имя не может превышать 100 символов")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email обязателен для заполнения")]
    [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты")]
    [StringLength(254, ErrorMessage = "Email не может превышать 254 символа")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес обязателен для заполнения")]
    [StringLength(200, ErrorMessage = "Адрес не может превышать 200 символов")]
    public string Address { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Социальные сети не могут превышать 200 символов")]
    public string? SocialMedia { get; set; }

    [StringLength(500, ErrorMessage = "Сообщение не может превышать 500 символов")]
    public string? Message { get; set; }
}