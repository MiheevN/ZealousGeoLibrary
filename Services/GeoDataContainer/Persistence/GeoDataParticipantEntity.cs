using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Services.GeoDataContainer.Persistence;

/// <summary>
/// Сущность участника для хранения в базе данных.
/// Отделяет доменную модель <see cref="Participant"/> от схемы хранения и
/// добавляет привязку к именованному контейнеру (<see cref="ContainerId"/>),
/// что позволяет хранить данные нескольких глобусов в одной таблице.
/// </summary>
public class GeoDataParticipantEntity
{
    /// <summary>
    /// Идентификатор участника
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор контейнера (глобуса), которому принадлежит участник
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Имя участника</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Адрес участника</summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>Электронная почта участника</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Местоположение участника</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Широта</summary>
    public double? Latitude { get; set; }

    /// <summary>Долгота</summary>
    public double? Longitude { get; set; }

    /// <summary>Город</summary>
    public string? City { get; set; }

    /// <summary>Страна</summary>
    public string? Country { get; set; }

    /// <summary>Социальные сети (текстовое поле)</summary>
    public string? SocialMedia { get; set; }

    /// <summary>Сообщение участника</summary>
    public string? Message { get; set; }

    /// <summary>Жизненные цели</summary>
    public string? LifeGoals { get; set; }

    /// <summary>Навыки</summary>
    public string? Skills { get; set; }

    /// <summary>Discord</summary>
    public string? Discord { get; set; }

    /// <summary>Telegram</summary>
    public string? Telegram { get; set; }

    /// <summary>VK</summary>
    public string? Vk { get; set; }

    /// <summary>Веб-сайт</summary>
    public string? Website { get; set; }

    /// <summary>Дата регистрации</summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Создает сущность хранения из доменной модели участника
    /// </summary>
    /// <param name="containerId">Идентификатор контейнера (глобуса)</param>
    /// <param name="participant">Доменная модель участника</param>
    /// <returns>Сущность для сохранения в БД</returns>
    public static GeoDataParticipantEntity FromParticipant(string containerId, Participant participant)
    {
        return new GeoDataParticipantEntity
        {
            Id = participant.Id,
            ContainerId = containerId,
            Name = participant.Name,
            Address = participant.Address,
            Email = participant.Email,
            Location = participant.Location,
            Latitude = participant.Latitude,
            Longitude = participant.Longitude,
            City = participant.City,
            Country = participant.Country,
            SocialMedia = participant.SocialMedia,
            Message = participant.Message,
            LifeGoals = participant.LifeGoals,
            Skills = participant.Skills,
            Discord = participant.SocialContacts?.Discord,
            Telegram = participant.SocialContacts?.Telegram,
            Vk = participant.SocialContacts?.Vk,
            Website = participant.SocialContacts?.Website,
            RegisteredAt = participant.RegisteredAt
        };
    }

    /// <summary>
    /// Обновляет поля существующей сущности данными доменной модели
    /// (идентификатор и контейнер не изменяются)
    /// </summary>
    /// <param name="participant">Доменная модель участника</param>
    public void UpdateFrom(Participant participant)
    {
        Name = participant.Name;
        Address = participant.Address;
        Email = participant.Email;
        Location = participant.Location;
        Latitude = participant.Latitude;
        Longitude = participant.Longitude;
        City = participant.City;
        Country = participant.Country;
        SocialMedia = participant.SocialMedia;
        Message = participant.Message;
        LifeGoals = participant.LifeGoals;
        Skills = participant.Skills;
        Discord = participant.SocialContacts?.Discord;
        Telegram = participant.SocialContacts?.Telegram;
        Vk = participant.SocialContacts?.Vk;
        Website = participant.SocialContacts?.Website;
        RegisteredAt = participant.RegisteredAt;
    }

    /// <summary>
    /// Преобразует сущность хранения обратно в доменную модель участника
    /// </summary>
    /// <returns>Доменная модель участника</returns>
    public Participant ToParticipant()
    {
        var hasSocialContacts = Discord is not null || Telegram is not null || Vk is not null || Website is not null;

        return new Participant
        {
            Id = Id,
            Name = Name,
            Address = Address,
            Email = Email,
            Location = Location,
            Latitude = Latitude,
            Longitude = Longitude,
            City = City,
            Country = Country,
            SocialMedia = SocialMedia,
            Message = Message,
            LifeGoals = LifeGoals,
            Skills = Skills,
            SocialContacts = hasSocialContacts
                ? new SocialContacts
                {
                    Discord = Discord,
                    Telegram = Telegram,
                    Vk = Vk,
                    Website = Website
                }
                : null,
            RegisteredAt = RegisteredAt
        };
    }
}
