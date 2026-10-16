using Bunit;
using ZealousMindedPeopleGeo.Components;

namespace ZealousMindedPeopleGeo.Tests.Components;

/// <summary>
/// Аккордеон настроек глобуса переключает сам компонент: приложению не нужен Bootstrap JS.
/// </summary>
public class CommunityGlobeSettingsTests : BunitContext
{
    [Fact]
    public void Accordion_OpensOneSectionAtATime()
    {
        var cut = Render<CommunityGlobeSettings>();

        Assert.Equal(new[] { "sizeSettings" }, OpenSections(cut));
        Assert.Equal("true", cut.Find("button[aria-controls=sizeSettings]").GetAttribute("aria-expanded"));

        cut.Find("button[aria-controls=lightSettings]").Click();

        Assert.Equal(new[] { "lightSettings" }, OpenSections(cut));
        Assert.Contains("collapsed", cut.Find("button[aria-controls=sizeSettings]").ClassList);
        Assert.Equal("false", cut.Find("button[aria-controls=sizeSettings]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Accordion_ClosesOpenSectionOnSecondClick()
    {
        var cut = Render<CommunityGlobeSettings>();

        cut.Find("button[aria-controls=sizeSettings]").Click();

        Assert.Empty(OpenSections(cut));
    }

    [Fact]
    public void Accordion_DoesNotDependOnBootstrapJs()
    {
        var cut = Render<CommunityGlobeSettings>();

        Assert.Empty(cut.FindAll("[data-bs-toggle], [data-bs-target], [data-bs-parent]"));
    }

    [Theory]
    [InlineData("en", "Globe settings", "Width: 800 px", "Apply")]
    [InlineData("ru", "Настройки глобуса", "Ширина: 800 px", "Применить")]
    public void Labels_FollowUiLanguage(string language, string title, string width, string apply)
    {
        using var culture = new CultureScope(language);
        var cut = Render<CommunityGlobeSettings>();

        Assert.Equal(title, cut.Find("h5").TextContent);
        Assert.Contains(cut.FindAll("label"), label => label.TextContent == width);
        Assert.Contains(apply, cut.Find("button.btn-warning").TextContent);
    }

    private static string[] OpenSections(IRenderedComponent<CommunityGlobeSettings> cut) =>
        cut.FindAll(".accordion-collapse.show").Select(section => section.Id!).ToArray();
}
