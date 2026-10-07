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
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: "  anna@example.com ", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        cut.WaitForAssertion(() => Assert.Contains("Point added successfully", cut.Find("[role=alert]").TextContent));
        Assert.Equal("anna@example.com", Assert.Single(await Container.GetAllParticipantsAsync()).Email);
    }

    [Fact]
    public async Task Submit_WithMalformedEmail_ShowsErrorAndSavesNothing()
    {
        var cut = RenderForm();

        Fill(cut, name: "Anna Petrova", email: "not-an-email", address: "Berlin, Alexanderplatz 1");
        await cut.Find("form").SubmitAsync();

        Assert.Contains(cut.FindAll(".validation-message"), m => m.TextContent.Contains("электронной почты"));
        Assert.Empty(await Container.GetPointsAsync());
        Assert.Equal(0, _geocoder.Calls);
    }

    [Fact]
    public void EmailField_SaysItIsOptionalAndPublic()
    {
        var cut = RenderForm();

        Assert.Contains("optional", cut.Find("label[for=email]").TextContent);
        Assert.Equal("email-hint", cut.Find("#email").GetAttribute("aria-describedby"));
        Assert.Contains("Visible to everyone", cut.Find("#email-hint").TextContent);
    }

    private IGeoDataContainer Container => _containers.GetOrCreateContainer(ContainerId);

    private IRenderedComponent<GeoDataParticipantForm> RenderForm() =>
        Render<GeoDataParticipantForm>(p => p
            .Add(c => c.DataContainerId, ContainerId)
            .Add(c => c.AutoGeocode, false));

    private static void Fill(IRenderedComponent<GeoDataParticipantForm> cut, string name, string email, string address)
    {
        cut.Find("#name").Change(name);
        cut.Find("#email").Change(email);
        cut.Find("#address").Change(address);
    }

    private sealed class FakeGeocoder : IGeocodingService
    {
        public int Calls { get; private set; }

        public ValueTask<GeocodingResult> GeocodeAddressAsync(string address, CancellationToken ct = default)
        {
            Calls++;
            return ValueTask.FromResult(new GeocodingResult
            {
                Success = true,
                Latitude = 52.5219,
                Longitude = 13.4132,
                FormattedAddress = address
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
