using System.ComponentModel.DataAnnotations;
using ZealousMindedPeopleGeo.Models;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.Validation;

/// <summary>
/// Атрибуты моделей участника: по ним форма проверяет ввод (DataAnnotationsValidator).
/// </summary>
public sealed class ParticipantAttributesTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("anna@example.com", true)]
    [InlineData("not-an-email", false)]
    public void EmailAttributes_AllowEmptyAndRejectMalformed(string email, bool expected)
    {
        var participant = CreateParticipant();
        participant.Email = email;
        var registration = new ParticipantRegistrationModel { Name = "Alice Smith", Email = email, Address = "Tverskaya street 1" };

        Assert.Equal(expected, EmailErrors(participant).Count == 0);
        Assert.Equal(expected, EmailErrors(registration).Count == 0);
    }

    [Theory]
    [InlineData("en", "Name is required", "Name must be at most 100 characters", "Latitude must be between -90 and 90", "Website: link is not valid")]
    [InlineData("ru", "Укажите имя", "Имя — не длиннее 100 символов", "Широта — от -90 до 90", "Website: некорректная ссылка")]
    public void ErrorMessages_FollowUiLanguage(string language, string required, string tooLong, string range, string url)
    {
        using var culture = new CultureScope(language);
        var participant = CreateParticipant();
        participant.Latitude = 95;

        participant.Name = "";
        Assert.Contains(required, Errors(participant));
        participant.Name = new string('a', 101);
        Assert.Contains(tooLong, Errors(participant));
        Assert.Contains(range, Errors(participant));
        Assert.Contains(url, Errors(new SocialContacts { Website = "not a link" }));
    }

    private static List<string?> Errors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage).ToList();
    }

    private static List<ValidationResult> EmailErrors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Where(r => r.MemberNames.Contains(nameof(Participant.Email))).ToList();
    }
}
