using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Validation;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// Демонстрационные наборы питают витрину и примеры, поэтому должны быть корректными данными.
/// </summary>
public class DemoDataSetsTests
{
    public static TheoryData<string> DataSetKeys => new(DemoDataSets.All.Select(set => set.Key));

    [Fact]
    public void All_HaveUniqueKeysAndDescriptions()
    {
        Assert.NotEmpty(DemoDataSets.All);
        Assert.Equal(DemoDataSets.All.Count, DemoDataSets.All.Select(s => s.Key).Distinct().Count());
        Assert.All(DemoDataSets.All, set =>
        {
            Assert.False(string.IsNullOrWhiteSpace(set.Title));
            Assert.False(string.IsNullOrWhiteSpace(set.Description));
        });
    }

    [Theory]
    [MemberData(nameof(DataSetKeys))]
    public void DataSet_ParticipantsPassValidation(string key)
    {
        using var localization = new LocalizationService(NullLogger<LocalizationService>.Instance);
        var validator = new ParticipantValidator(localization);

        var participants = DemoDataSets.FindByKey(key)!.Create();

        Assert.NotEmpty(participants);
        Assert.Equal(participants.Count, participants.Select(p => p.Id).Distinct().Count());
        Assert.All(participants, participant =>
        {
            var result = validator.Validate(participant);
            Assert.True(result.IsValid, $"{participant.Name}: {string.Join("; ", result.Errors)}");
        });
    }

    [Fact]
    public void Create_ReturnsIndependentCopies()
    {
        var set = DemoDataSets.All[0];

        var first = set.Create();
        first[0].Name = "Changed";
        first.Clear();
        var second = set.Create();

        Assert.NotEmpty(second);
        Assert.NotEqual("Changed", second[0].Name);
    }

    [Theory]
    [InlineData("tech-hubs")]
    [InlineData("TECH-HUBS")]
    public void FindByKey_IgnoresCase(string key)
    {
        Assert.Equal("tech-hubs", DemoDataSets.FindByKey(key)?.Key);
    }

    [Fact]
    public void FindByKey_UnknownKey_ReturnsNull()
    {
        Assert.Null(DemoDataSets.FindByKey("no-such-set"));
    }
}
