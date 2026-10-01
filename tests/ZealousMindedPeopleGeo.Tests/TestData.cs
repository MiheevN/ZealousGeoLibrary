using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Tests;

/// <summary>
/// Общие тестовые данные.
/// </summary>
internal static class TestData
{
    /// <summary>
    /// Создаёт участника, который проходит валидацию: имя только из букв, координаты Москвы.
    /// </summary>
    public static Participant CreateParticipant(
        string name = "Test Participant",
        double? latitude = 55.7558,
        double? longitude = 37.6176,
        Guid? id = null)
    {
        return new Participant
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Email = $"{name.Replace(' ', '.').ToLowerInvariant()}@example.com",
            Address = "Test Address",
            Location = "Test Location",
            Latitude = latitude,
            Longitude = longitude
        };
    }
}
