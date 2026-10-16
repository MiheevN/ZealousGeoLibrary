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

    private static List<ValidationResult> EmailErrors(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Where(r => r.MemberNames.Contains(nameof(Participant.Email))).ToList();
    }
}
