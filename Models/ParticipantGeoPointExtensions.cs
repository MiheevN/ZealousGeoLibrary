using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ZealousMindedPeopleGeo.Models;

/// <summary>
/// Ключи <see cref="GeoPoint.Properties"/>, в которых точка хранит поля участника.
/// </summary>
public static class ParticipantPointProperties
{
    public const string Address = "address";
    public const string Email = "email";
    public const string Location = "location";
    public const string City = "city";
    public const string Country = "country";
    public const string SocialMedia = "socialMedia";
    public const string LifeGoals = "lifeGoals";
    public const string Skills = "skills";
    public const string Discord = "discord";
    public const string Telegram = "telegram";
    public const string Vk = "vk";
    public const string Website = "website";
    public const string RegisteredAt = "registeredAt";
}

/// <summary>
/// Преобразование участника сообщества в точку и обратно без потери полей:
/// имя становится заголовком, сообщение — описанием, остальное — свойствами точки.
/// </summary>
public static class ParticipantGeoPointExtensions
{
    /// <summary>
    /// Точка участника. У участника должны быть координаты.
    /// </summary>
    /// <exception cref="ArgumentException">У участника нет широты или долготы.</exception>
    public static GeoPoint ToGeoPoint(this Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (participant.Latitude is not double latitude || participant.Longitude is not double longitude)
        {
            throw new ArgumentException(
                $"Participant '{participant.Name}' ({participant.Id}) has no coordinates", nameof(participant));
        }

        var properties = new Dictionary<string, string>();

        // Обязательные строки участника пустые по умолчанию: пустое значение не храним.
        // Необязательные храним, если они заданы, даже пустыми, чтобы отличать "" от null.
        SetIfNotEmpty(properties, ParticipantPointProperties.Address, participant.Address);
        SetIfNotEmpty(properties, ParticipantPointProperties.Email, participant.Email);
        SetIfNotEmpty(properties, ParticipantPointProperties.Location, participant.Location);
        SetIfNotNull(properties, ParticipantPointProperties.City, participant.City);
        SetIfNotNull(properties, ParticipantPointProperties.Country, participant.Country);
        SetIfNotNull(properties, ParticipantPointProperties.SocialMedia, participant.SocialMedia);
        SetIfNotNull(properties, ParticipantPointProperties.LifeGoals, participant.LifeGoals);
        SetIfNotNull(properties, ParticipantPointProperties.Skills, participant.Skills);
        SetIfNotNull(properties, ParticipantPointProperties.Discord, participant.SocialContacts?.Discord);
        SetIfNotNull(properties, ParticipantPointProperties.Telegram, participant.SocialContacts?.Telegram);
        SetIfNotNull(properties, ParticipantPointProperties.Vk, participant.SocialContacts?.Vk);
        SetIfNotNull(properties, ParticipantPointProperties.Website, participant.SocialContacts?.Website);
        properties[ParticipantPointProperties.RegisteredAt] =
            participant.RegisteredAt.ToString("O", CultureInfo.InvariantCulture);

        return new GeoPoint
        {
            Id = participant.Id.ToString(),
            Latitude = latitude,
            Longitude = longitude,
            Title = participant.Name,
            Description = participant.Message,
            Properties = properties
        };
    }

    /// <summary>
    /// Участник из точки. Подходит для любой точки: чего нет в свойствах, остаётся
    /// значением по умолчанию.
    /// </summary>
    public static Participant ToParticipant(this GeoPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        var properties = point.Properties ?? new Dictionary<string, string>();
        string? Get(string key) => properties.TryGetValue(key, out var value) ? value : null;

        var discord = Get(ParticipantPointProperties.Discord);
        var telegram = Get(ParticipantPointProperties.Telegram);
        var vk = Get(ParticipantPointProperties.Vk);
        var website = Get(ParticipantPointProperties.Website);
        var hasSocialContacts = discord is not null || telegram is not null || vk is not null || website is not null;

        return new Participant
        {
            Id = ParticipantIdFor(point.Id),
            Name = point.Title,
            Message = point.Description,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
            Address = Get(ParticipantPointProperties.Address) ?? string.Empty,
            Email = Get(ParticipantPointProperties.Email) ?? string.Empty,
            Location = Get(ParticipantPointProperties.Location) ?? string.Empty,
            City = Get(ParticipantPointProperties.City),
            Country = Get(ParticipantPointProperties.Country),
            SocialMedia = Get(ParticipantPointProperties.SocialMedia),
            LifeGoals = Get(ParticipantPointProperties.LifeGoals),
            Skills = Get(ParticipantPointProperties.Skills),
            SocialContacts = hasSocialContacts
                ? new SocialContacts { Discord = discord, Telegram = telegram, Vk = vk, Website = website }
                : null,
            RegisteredAt = DateTime.TryParse(
                Get(ParticipantPointProperties.RegisteredAt),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var registeredAt)
                ? registeredAt
                : default
        };
    }

    /// <summary>
    /// Идентификатор участника для точки. Если <paramref name="pointId"/> — GUID, это он
    /// сам, иначе GUID, вычисленный из строки: одна и та же точка всегда даёт один и тот же.
    /// </summary>
    public static Guid ParticipantIdFor(string? pointId)
    {
        if (Guid.TryParse(pointId, out var id))
        {
            return id;
        }

        // GUID версии 3 (RFC 4122): MD5 от строки с проставленными битами версии и варианта.
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(pointId ?? string.Empty));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash, bigEndian: true);
    }

    private static void SetIfNotEmpty(Dictionary<string, string> properties, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            properties[key] = value;
        }
    }

    private static void SetIfNotNull(Dictionary<string, string> properties, string key, string? value)
    {
        if (value is not null)
        {
            properties[key] = value;
        }
    }
}
