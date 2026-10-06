using ZealousMindedPeopleGeo.Models;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// Цвета маркеров по категориям: одинаковые для карты и глобуса.
/// </summary>
public class GeoPointPaletteTests
{
    [Fact]
    public void WithoutCategories_AllMarkersUseDefaultColorAndNoLegend()
    {
        var points = new[] { Point("a"), Point("b") };

        var palette = GeoPointPalette.For(points);

        Assert.False(palette.HasCategories);
        Assert.Empty(palette.Categories);
        Assert.All(points, p => Assert.Equal(GeoPointPalette.DefaultColor, palette.ColorFor(p)));
    }

    [Fact]
    public void Categories_GetAutomaticColorsInOrderOfAppearance_ThenOther()
    {
        var points = new[]
        {
            Point("1", "europe"), Point("2", "asia"), Point("3", "europe"),
            Point("4", "africa"), Point("5", "oceania"), Point("6", "americas"), Point("7")
        };

        var palette = GeoPointPalette.For(points);

        Assert.Equal(
            new[]
            {
                new GeoPointCategory("europe", GeoPointPalette.CategoryColors[0], 2, false),
                new GeoPointCategory("asia", GeoPointPalette.CategoryColors[1], 1, false),
                new GeoPointCategory("africa", GeoPointPalette.CategoryColors[2], 1, false),
                new GeoPointCategory("oceania", GeoPointPalette.OtherColor, 1, true),
                new GeoPointCategory("americas", GeoPointPalette.OtherColor, 1, true),
                new GeoPointCategory("", GeoPointPalette.OtherColor, 1, true)
            },
            palette.Categories);
        Assert.Equal(GeoPointPalette.CategoryColors[1], palette.ColorFor(points[1]));
        Assert.Equal(GeoPointPalette.OtherColor, palette.ColorFor(points[6]));
    }

    [Fact]
    public void ColorFollowsCategory_NotItsPositionInFilteredView()
    {
        var all = new[] { Point("1", "europe"), Point("2", "asia") };
        var palette = GeoPointPalette.For(all);

        // Фильтр легенды скрывает Европу: Азия сохраняет свой цвет, потому что палитра
        // строится по всем точкам.
        Assert.Equal(GeoPointPalette.CategoryColors[1], palette.ColorFor(all[1]));
    }

    [Fact]
    public void ExplicitColors_WinOverCategoryAndCustomCategoryColors_SkipAutomaticSlot()
    {
        var custom = new Dictionary<string, string> { ["vip"] = "#ffcf5a" };
        var red = Point("r", "europe");
        red.Color = "#ff0000";
        var points = new[] { Point("1", "vip"), Point("2", "europe"), red };

        var palette = GeoPointPalette.For(points, custom);

        Assert.Equal("#ffcf5a", palette.ColorFor(points[0]));
        Assert.Equal(GeoPointPalette.CategoryColors[0], palette.ColorFor(points[1])); // первый свободный слот
        Assert.Equal("#ff0000", palette.ColorFor(red));
    }

    [Fact]
    public void CategoryNames_AreTrimmedAndCaseSensitive()
    {
        var points = new[] { Point("1", " office "), Point("2", "office"), Point("3", "Office"), Point("4", "  ") };

        var palette = GeoPointPalette.For(points);

        Assert.Equal(new[] { "office", "Office", "" }, palette.Categories.Select(c => c.Name));
        Assert.Equal(2, palette.Categories[0].Count);
    }

    [Fact]
    public void UnknownPoint_GetsOtherColor()
    {
        var palette = GeoPointPalette.For(new[] { Point("1", "europe") });

        Assert.Equal(GeoPointPalette.OtherColor, palette.ColorFor(Point("x", "mars")));
    }

    private static GeoPoint Point(string id, string? category = null) => new()
    {
        Id = id,
        Latitude = 1,
        Longitude = 1,
        Title = id,
        Category = category
    };
}
