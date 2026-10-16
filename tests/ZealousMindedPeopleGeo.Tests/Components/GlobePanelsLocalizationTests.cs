using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ZealousMindedPeopleGeo.Components;
using ZealousMindedPeopleGeo.Services;
using ZealousMindedPeopleGeo.Services.Mapping;

namespace ZealousMindedPeopleGeo.Tests.Components;

/// <summary>
/// Панели глобуса и PWA-менеджер говорят на языке CurrentUICulture.
/// </summary>
public class GlobePanelsLocalizationTests : BunitContext
{
    public GlobePanelsLocalizationTests()
    {
        Services.AddSingleton(new GlobeStateService());
        Services.AddSingleton(DispatchProxy.Create<IGlobeMediator, GeoDataParticipantFormTests.UnusedService>());
        Services.AddSingleton<IPwaService>(new InstallablePwa());
    }

    [Theory]
    [InlineData("en", "Rotation:", "Low", "Turn rotation on or off", "Current position is not set")]
    [InlineData("ru", "Вращение:", "Низкая", "Включить или выключить вращение", "Текущая позиция не установлена")]
    public void GlobeControls_FollowUiLanguage(string language, string rotation, string low, string rotationButton, string noLocation)
    {
        using var culture = new CultureScope(language);
        var cut = Render<CommunityGlobeControls>();

        Assert.Equal(rotation, cut.Find("small").TextContent);
        Assert.Equal(low, cut.Find("option[value='0']").TextContent);
        Assert.Equal(rotationButton, cut.Find("button[aria-pressed]").GetAttribute("aria-label"));
        Assert.Equal(noLocation, cut.Find("button[title]:not([aria-pressed])").GetAttribute("title"));
    }

    [Theory]
    [InlineData("en", "Install the app", "Later")]
    [InlineData("ru", "Установите приложение", "Позже")]
    public void PwaInstallPrompt_FollowsUiLanguage(string language, string title, string later)
    {
        using var culture = new CultureScope(language);
        var cut = Render<PwaManagerComponent>(p => p.Add(c => c.AutoCheckUpdates, false));

        cut.WaitForAssertion(() => Assert.Equal(title, cut.Find(".install-prompt h4").TextContent));
        Assert.Equal(later, cut.Find(".install-actions .btn-secondary").TextContent.Trim());
    }

    private sealed class InstallablePwa : IPwaService
    {
        public Task InitializeAsync() => Task.CompletedTask;
        public Task<bool> IsPwaSupportedAsync() => Task.FromResult(true);
        public Task<bool> IsRunningAsPwaAsync() => Task.FromResult(false);
        public Task<PwaInstallInfo> GetInstallInfoAsync() => Task.FromResult(new PwaInstallInfo { CanInstall = true, IsSupported = true });
        public Task<bool> ShowInstallPromptAsync() => Task.FromResult(false);
        public Task ClearCacheAsync() => Task.CompletedTask;
        public Task UpdateServiceWorkerAsync() => Task.CompletedTask;
        public Task<PwaCacheInfo> GetCacheInfoAsync() => Task.FromResult(new PwaCacheInfo());
        public Task<bool> SendNotificationAsync(string title, string body, NotificationOptions? options = null) => Task.FromResult(false);
    }
}
