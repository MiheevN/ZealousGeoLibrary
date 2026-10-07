using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Validation;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.Validation;

/// <summary>
/// Правила валидации участника: что пропускается, что отклоняется и по какому полю.
/// </summary>
public sealed class ParticipantValidatorTests : IDisposable
{
    private readonly LocalizationService _localization = new(NullLogger<LocalizationService>.Instance);
    private readonly ParticipantValidator _validator;

    public ParticipantValidatorTests()
    {
        _validator = new ParticipantValidator(_localization);
    }

    [Fact]
    public void ValidParticipant_Passes()
    {
        var result = _validator.Validate(CreateParticipant("Alice Smith"));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("Анна-Мария")]
    [InlineData("Алёна Фёдорова")]
    [InlineData("José García")]
    [InlineData("François D'Arc")]
    [InlineData("Brasília")]
    public void Name_AcceptsLettersOfAnyAlphabet(string name)
    {
        AssertValid(CreateParticipant(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("R2D2")]
    [InlineData("Alice <script>")]
    public void Name_RejectsEmptyShortAndNonLetters(string name)
    {
        AssertInvalid(CreateParticipant(name), nameof(Participant.Name));
    }

    [Fact]
    public void Name_RejectsMoreThan100Characters()
    {
        AssertInvalid(CreateParticipant(new string('a', 101)), nameof(Participant.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Email_IsOptional(string email)
    {
        var participant = CreateParticipant();
        participant.Email = email;

        AssertValid(participant);
        Assert.True(new ParticipantRegistrationValidator(_localization)
            .Validate(new ParticipantRegistrationModel { Name = "Alice Smith", Email = email, Address = "Tverskaya street 1, Moscow" })
            .IsValid);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("two@at@example.com")]
    public void Email_WhenGiven_MustLookLikeAddress(string email)
    {
        var participant = CreateParticipant();
        participant.Email = email;

        AssertInvalid(participant, nameof(Participant.Email));
        var registration = new ParticipantRegistrationValidator(_localization)
            .Validate(new ParticipantRegistrationModel { Name = "Alice Smith", Email = email, Address = "Tverskaya street 1, Moscow" });
        Assert.Contains(registration.Errors, e => e.PropertyName == nameof(ParticipantRegistrationModel.Email));
    }

    [Fact]
    public void Email_WhenGiven_RejectsMoreThan254Characters()
    {
        var participant = CreateParticipant();
        participant.Email = new string('a', 250) + "@x.io";

        AssertInvalid(participant, nameof(Participant.Email));
    }

    // Форма проверяет модель атрибутами (DataAnnotationsValidator), а не FluentValidation.
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
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(0, 0)]
    public void Coordinates_AcceptBoundaries(double latitude, double longitude)
    {
        AssertValid(CreateParticipant(latitude: latitude, longitude: longitude));
    }

    [Theory]
    [InlineData(90.0001, 0, nameof(Participant.Latitude))]
    [InlineData(-91, 0, nameof(Participant.Latitude))]
    [InlineData(0, 180.0001, nameof(Participant.Longitude))]
    [InlineData(0, -181, nameof(Participant.Longitude))]
    public void Coordinates_RejectOutOfRange(double latitude, double longitude, string property)
    {
        AssertInvalid(CreateParticipant(latitude: latitude, longitude: longitude), property);
    }

    [Fact]
    public void SocialContacts_AcceptHttpLinks()
    {
        var participant = CreateParticipant();
        participant.SocialContacts = new SocialContacts { Telegram = "https://t.me/example", Website = "http://example.com" };

        AssertValid(participant);
    }

    [Theory]
    [InlineData("@example")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public void SocialContacts_RejectNonHttpLinks(string link)
    {
        var participant = CreateParticipant();
        participant.SocialContacts = new SocialContacts { Vk = link };

        AssertInvalid(participant, nameof(Participant.SocialContacts));
    }

    [Fact]
    public void Location_InRussia_MustHaveCoordinatesInRussia()
    {
        var moscow = CreateParticipant(latitude: 55.75, longitude: 37.62);
        moscow.Location = "Москва, Россия";
        var wrong = CreateParticipant(latitude: 40.71, longitude: -74.0);
        wrong.Location = "Москва, Россия";

        AssertValid(moscow);
        Assert.False(_validator.Validate(wrong).IsValid);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-3700)]
    public void RegisteredAt_RejectsFutureAndVeryOldDates(int daysFromNow)
    {
        var participant = CreateParticipant();
        participant.RegisteredAt = DateTime.UtcNow.AddDays(daysFromNow);

        AssertInvalid(participant, nameof(Participant.RegisteredAt));
    }

    [Theory]
    [InlineData(45, 90, true)]
    [InlineData(-90, -180, true)]
    [InlineData(91, 0, false)]
    [InlineData(0, 181, false)]
    public void CoordinateValidator_ChecksRanges(double latitude, double longitude, bool expected)
    {
        Assert.Equal(expected, new CoordinateValidator().Validate((latitude, longitude)).IsValid);
    }

    [Theory]
    [InlineData("Tverskaya street 1, Moscow", true)]
    [InlineData("город Казань, улица Баумана 5", true)]
    [InlineData("Somewhere far away", false)]
    public void RegistrationValidator_RequiresLocationKeywordsInAddress(string address, bool expected)
    {
        var model = new ParticipantRegistrationModel
        {
            Name = "Alice Smith",
            Email = "alice@example.com",
            Address = address
        };

        var result = new ParticipantRegistrationValidator(_localization).Validate(model);

        Assert.Equal(expected, result.IsValid);
    }

    public void Dispose()
    {
        _localization.Dispose();
    }

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> EmailErrors(object model)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model, new System.ComponentModel.DataAnnotations.ValidationContext(model), results, validateAllProperties: true);
        return results.Where(r => r.MemberNames.Contains(nameof(Participant.Email))).ToList();
    }

    private void AssertValid(Participant participant)
    {
        var result = _validator.Validate(participant);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    private void AssertInvalid(Participant participant, string property)
    {
        var result = _validator.Validate(participant);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == property);
    }
}
