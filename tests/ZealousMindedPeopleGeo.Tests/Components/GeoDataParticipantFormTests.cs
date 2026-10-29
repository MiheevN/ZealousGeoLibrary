using System.Reflection;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ZealousMindedPeopleGeo.Components;
using ZealousMindedPeopleGeo.Models;
using ZealousMindedPeopleGeo.Services.GeoDataContainer;
using ZealousMindedPeopleGeo.Services.Geocoding;
using ZealousMindedPeopleGeo.Services.Mapping;

namespace ZealousMindedPeopleGeo.Tests.Components;

/// <summary>
/// GeoDataParticipantForm: что пользователь обязан заполнить и что попадает в контейнер.
/// </summary>
public class GeoDataParticipantFormTests : BunitContext
{
    private const string ContainerId = "form-tests";

    private readonly GeoDataContainerManager _containers =
        new(NullLogger<GeoDataContainerManager>.Instance, NullLoggerFactory.Instance);
    private readonly FakeGeocoder _geocoder = new();

    public GeoDataParticipantFormTests()
    {
        Services.AddLogging();
        Services.AddSingleton<IGeoDataContainerManager>(_containers);
        Services.AddSingleton<IGeocodingService>(_geocoder);
        Services.AddSingleton(new GlobeStateService());
        // Без GlobeId форма к глобусу не обращается.
        Services.AddSingleton(DispatchProxy.Create<IGlobeMediator, UnusedService>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Submit_WithoutEmail_SavesPointWithoutEmail(string email)
    {
        using var english = new CultureScope("en");
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: email, address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("Point added successfully", cut.Find("[role=alert]").TextContent));
        Assert.Empty(cut.FindAll(".validation-message"));
        var saved = Assert.Single(await Container.GetAllParticipantsAsync());
        Assert.Equal("Anna Petrova", saved.Name);
        Assert.Equal(string.Empty, saved.Email);
        var point = Assert.Single(await Container.GetPointsAsync());
        Assert.False(point.Properties.ContainsKey(ParticipantPointProperties.Email));
    }

    [Fact]
    public async Task Submit_WithEmail_SavesItTrimmed()
    {
        using var english = new CultureScope("en");
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: "  anna@example.com ", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("Point added successfully", cut.Find("[role=alert]").TextContent));
        Assert.Equal("anna@example.com", Assert.Single(await Container.GetAllParticipantsAsync()).Email);
    }

    [Fact]
    public async Task Submit_WithMalformedEmail_ShowsErrorAndSavesNothing()
    {
        using var english = new CultureScope("en");
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: "not-an-email", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        Assert.Contains(cut.FindAll(".validation-message"), m => m.TextContent.Contains("Email address is not valid"));
        Assert.Empty(await Container.GetPointsAsync());
        Assert.Equal(0, _geocoder.Calls);
    }

    [Fact]
    public void EmailField_SaysItIsOptionalAndPublic()
    {
        using var english = new CultureScope("en");
        var cut = RenderForm();

        Assert.Contains("optional", cut.Find("label[for=email]").TextContent);
        Assert.Equal("email-hint", cut.Find("#email").GetAttribute("aria-describedby"));
        Assert.Contains("Visible to everyone", cut.Find("#email-hint").TextContent);
    }

    [Fact]
    public async Task OnSubmitting_Rejection_ShowsReasonKeepsFieldsAndSavesNothing()
    {
        using var english = new CultureScope("en");
        Participant? checkedParticipant = null;
        var cut = RenderForm(participant =>
        {
            checkedParticipant = participant;
            return Task.FromResult<string?>("Too many points, try again in an hour");
        });

        Fill(cut, name: "Anna Petrova", email: "", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal("Too many points, try again in an hour", cut.Find("[role=alert]").TextContent.Trim()));
        Assert.Empty(await Container.GetPointsAsync());
        Assert.Equal("Anna Petrova", cut.Find("#name").GetAttribute("value"));
        Assert.NotNull(checkedParticipant);
        Assert.Equal("Anna Petrova", checkedParticipant.Name);
        Assert.Equal(52.5219, checkedParticipant.Latitude);
        Assert.Equal(13.4132, checkedParticipant.Longitude);
    }

    [Fact]
    public async Task OnSubmitting_Approval_SavesPoint()
    {
        using var english = new CultureScope("en");
        var calls = 0;
        var cut = RenderForm(_ =>
        {
            calls++;
            return Task.FromResult<string?>(null);
        });

        Fill(cut, name: "Anna Petrova", email: "", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("Point added successfully", cut.Find("[role=alert]").TextContent));
        Assert.Equal(1, calls);
        Assert.Single(await Container.GetPointsAsync());
    }

    [Fact]
    public async Task RussianUi_ShowsRussianLabelsAndMessages()
    {
        using var russian = new CultureScope("ru");
        var cut = RenderForm();

        Assert.Equal("Добавить точку", cut.Find("h3").TextContent);
        Assert.Equal("Email (необязательно)", cut.Find("label[for=email]").TextContent);
        Assert.Equal("Добавить", cut.Find("button[type=submit]").TextContent.Trim());

        Fill(cut, name: "", email: "not-an-email", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        var errors = cut.FindAll(".validation-message").Select(m => m.TextContent).ToList();
        Assert.Contains("Укажите имя", errors);
        Assert.Contains("Некорректный адрес электронной почты", errors);
    }

    [Fact]
    public void TitleAndButtonParameters_OverrideTranslations()
    {
        using var russian = new CultureScope("ru");
        var cut = Render<GeoDataParticipantForm>(p => p
            .Add(c => c.DataContainerId, ContainerId)
            .Add(c => c.AutoGeocode, false)
            .Add(c => c.Title, "Новый офис")
            .Add(c => c.SubmitButtonText, "Сохранить офис"));

        Assert.Equal("Новый офис", cut.Find("h3").TextContent);
        Assert.Equal("Сохранить офис", cut.Find("button[type=submit]").TextContent.Trim());
    }

    [Theory]
    [InlineData("ru", GeocodingError.NotFound, "Адрес не найден. Проверьте написание или добавьте город и страну.")]
    [InlineData("en", GeocodingError.NotFound, "Could not find this address. Check the spelling or add the city and country.")]
    [InlineData("ru", GeocodingError.Unavailable, "Поиск адресов на этом сайте недоступен. Обратитесь к администратору сайта.")]
    [InlineData("en", GeocodingError.Failed, "Could not look up the address. Please try again.")]
    public async Task Submit_AddressNotLookedUp_ExplainsWhyWithoutTechnicalDetails(string language, GeocodingError failure, string message)
    {
        using var culture = new CultureScope(language);
        _geocoder.Failure = failure;
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: "", address: "Nowhere 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Equal(message, cut.Find("[role=alert]").TextContent.Trim()));
        Assert.DoesNotContain("technical detail", cut.Markup);
        Assert.Empty(await Container.GetPointsAsync());
    }

    [Fact]
    public void EnteredAddress_IsLookedUpBeforeSubmit_WithDataAttribution()
    {
        using var english = new CultureScope("en");
        _geocoder.Attribution = "Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright";
        var cut = Render<GeoDataParticipantForm>(p => p.Add(c => c.DataContainerId, ContainerId));

        cut.Find("#address").Change("Berlin, Alexanderplatz 1");

        cut.WaitForAssertion(() => Assert.Contains("Berlin, Alexanderplatz 1", cut.Find(".location-preview").TextContent));
        Assert.Equal(_geocoder.Attribution, cut.Find(".location-attribution").TextContent);
        Assert.Equal(1, _geocoder.Calls);
    }

    [Fact]
    public void EnteredAddress_NotFound_SaysSoUnderTheField()
    {
        using var russian = new CultureScope("ru");
        _geocoder.Failure = GeocodingError.NotFound;
        var cut = Render<GeoDataParticipantForm>(p => p.Add(c => c.DataContainerId, ContainerId));

        cut.Find("#address").Change("Nowhere 1");

        cut.WaitForAssertion(() => Assert.Equal(
            "Адрес не найден. Проверьте написание или добавьте город и страну.",
            cut.Find(".geodata-hint-warning").TextContent));
        Assert.Empty(cut.FindAll(".location-preview"));
    }

    [Fact]
    public void AutoGeocodeOff_DoesNotLookUpWhileTyping()
    {
        var cut = RenderForm();

        cut.Find("#address").Change("Berlin, Alexanderplatz 1");

        Assert.Equal(0, _geocoder.Calls);
        Assert.Empty(cut.FindAll(".location-preview"));
    }

    private IGeoDataContainer Container => _containers.GetOrCreateContainer(ContainerId);

    private IRenderedComponent<GeoDataParticipantForm> RenderForm(Func<Participant, Task<string?>>? onSubmitting = null) =>
        Render<GeoDataParticipantForm>(p => p
            .Add(c => c.DataContainerId, ContainerId)
            .Add(c => c.AutoGeocode, false)
            .Add(c => c.OnSubmitting, onSubmitting));

    private static void Fill(IRenderedComponent<GeoDataParticipantForm> cut, string name, string email, string address)
    {
        cut.Find("#name").Change(name);
        cut.Find("#email").Change(email);
        cut.Find("#address").Change(address);
    }

    private sealed class FakeGeocoder : IGeocodingService
    {
        public int Calls { get; private set; }

        /// <summary>Причина, по которой адрес «не найдётся»; None — найдётся.</summary>
        public GeocodingError Failure { get; set; }

        public string? Attribution { get; set; }

        public ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default)
        {
            Calls++;
            if (Failure != GeocodingError.None)
            {
                return ValueTask.FromResult(new GeocodingResult { Success = false, Error = Failure, ErrorMessage = "technical detail" });
            }

            return ValueTask.FromResult(new GeocodingResult
            {
                Success = true,
                Latitude = 52.5219,
                Longitude = 13.4132,
                FormattedAddress = address,
                Attribution = Attribution
            });
        }

        public ValueTask<GeocodingResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public bool ValidateCoordinates(double latitude, double longitude) => true;

        public ValueTask<bool> IsAvailableAsync(CancellationToken ct = default) => ValueTask.FromResult(true);
    }

    /// <summary>Сервис, к которому тест не ожидает обращений.</summary>
    public class UnusedService : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call: {targetMethod?.Name}");
    }
}
