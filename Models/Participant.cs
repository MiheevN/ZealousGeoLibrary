using System.ComponentModel.DataAnnotations;
using ZealousMindedPeopleGeo.Resources;
using ZealousMindedPeopleGeo.Validation;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Модель участника сообщества для 3D глобуса
/// </summary>
public class Participant
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationNameRequired))]
    [StringLength(100, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationNameTooLong))]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationAddressRequired))]
    [StringLength(200, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationAddressTooLong))]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Email участника, необязательный: пустая строка — не указан. Указанный email
    /// виден всем в подсказке и карточке точки на карте.
    /// </summary>
    [OptionalEmailAddress(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationEmailInvalid))]
    [StringLength(254, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationEmailTooLong))]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationLocationRequired))]
    [StringLength(200, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationLocationTooLong))]
    public string Location { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationLatitudeRange))]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationLongitudeRange))]
    public double? Longitude { get; set; }

    [StringLength(100, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationCityTooLong))]
    public string? City { get; set; }

    [StringLength(100, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationCountryTooLong))]
    public string? Country { get; set; }

    [StringLength(200, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationSocialMediaTooLong))]
    public string? SocialMedia { get; set; }

    [StringLength(500, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationMessageTooLong))]
    public string? Message { get; set; }

    public string? LifeGoals { get; set; }

    public string? Skills { get; set; }

    public SocialContacts? SocialContacts { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Модель социальных контактов участника
/// </summary>
public class SocialContacts
{
    [Url(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationUrlInvalid))]
    public string? Discord { get; set; }

    [Url(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationUrlInvalid))]
    public string? Telegram { get; set; }

    [Url(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationUrlInvalid))]
    public string? Vk { get; set; }

    [Url(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationUrlInvalid))]
    public string? Website { get; set; }
}

/// <summary>
/// Модель для регистрации участника
/// </summary>
public class ParticipantRegistrationModel
{
    [Required(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationNameRequired))]
    [StringLength(100, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationNameTooLong))]
    public string Name { get; set; } = string.Empty;

    /// <summary>Необязательный email: пустая строка — не указан.</summary>
    [OptionalEmailAddress(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationEmailInvalid))]
    [StringLength(254, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationEmailTooLong))]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationAddressRequired))]
    [StringLength(200, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationAddressTooLong))]
    public string Address { get; set; } = string.Empty;

    [StringLength(200, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationSocialMediaTooLong))]
    public string? SocialMedia { get; set; }

    [StringLength(500, ErrorMessageResourceType = typeof(Ui), ErrorMessageResourceName = nameof(Ui.ValidationMessageTooLong))]
    public string? Message { get; set; }
}