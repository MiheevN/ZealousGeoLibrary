using ZealousMindedPeopleGeo.Models;

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
    public void DataSet_PointsAreValidAndCategorized(string key)
    {
        var points = DemoDataSets.FindByKey(key)!.Create();

        Assert.NotEmpty(points);
        Assert.Equal(points.Count, points.Select(p => p.Id).Distinct().Count());
        Assert.All(points, point =>
        {
            Assert.Null(point.Validate());
            Assert.False(string.IsNullOrWhiteSpace(point.Title));
            Assert.False(string.IsNullOrWhiteSpace(point.Category));
            Assert.True(point.Properties.ContainsKey(ParticipantPointProperties.Country), point.Title);
            // Демо-данные — места, а не люди: никаких выдуманных адресов почты.
            Assert.False(point.Properties.ContainsKey(ParticipantPointProperties.Email), point.Title);
        });
    }

    [Theory]
    [InlineData("russian-cities")]
    [InlineData("tech-hubs")]
    public void DataSet_FitsAutomaticCategoryColors(string key)
    {
        // В этих наборах не больше трёх категорий: каждая получает свой цвет, серых нет.
        var palette = GeoPointPalette.For(DemoDataSets.FindByKey(key)!.Create());

        Assert.InRange(palette.Categories.Count, 2, GeoPointPalette.CategoryColors.Count);
        Assert.DoesNotContain(palette.Categories, category => category.IsOther);
    }

    [Fact]
    public void Create_ReturnsIndependentCopiesWithStableIds()
    {
        var set = DemoDataSets.All[0];

        var first = set.Create();
        first[0].Title = "Changed";
        first[0].Properties.Clear();
        first.Clear();
        var second = set.Create();

        Assert.NotEmpty(second);
        Assert.NotEqual("Changed", second[0].Title);
        Assert.NotEmpty(second[0].Properties);
        Assert.Equal(second.Select(p => p.Id), set.Create().Select(p => p.Id));
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
