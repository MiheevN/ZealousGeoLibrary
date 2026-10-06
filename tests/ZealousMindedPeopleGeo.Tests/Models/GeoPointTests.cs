using ZealousMindedPeopleGeo.Models;
using static ZealousMindedPeopleGeo.Tests.TestData;

namespace ZealousMindedPeopleGeo.Tests.Models;

/// <summary>
/// Модель точки и её связь с участником сообщества.
/// </summary>
public class GeoPointTests
{
    [Fact]
    public void NewPoint_HasGuidIdAndEmptyProperties()
    {
        var point = new GeoPoint();

        Assert.True(Guid.TryParse(point.Id, out _));
        Assert.NotEqual(point.Id, new GeoPoint().Id);
        Assert.Empty(point.Properties);
        Assert.Null(point.Validate());
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Validate_AcceptsBoundaryCoordinates(double latitude, double longitude)
    {
        Assert.Null(new GeoPoint { Latitude = latitude, Longitude = longitude }.Validate());
    }

    [Fact]
    public void Validate_ChecksLengthsOfEveryLimitedField()
    {
        Assert.Contains("Color", new GeoPoint { Color = new string('x', GeoPoint.MaxColorLength + 1) }.Validate());
        Assert.Contains("Icon", new GeoPoint { Icon = new string('x', GeoPoint.MaxIconLength + 1) }.Validate());
        Assert.Contains("Url", new GeoPoint { Url = new string('x', GeoPoint.MaxUrlLength + 1) }.Validate());
    }

    [Fact]
    public void Clone_CopiesPropertiesDictionary()
    {
        var point = new GeoPoint { Title = "A", Properties = { ["k"] = "v" } };

        var copy = point.Clone();
        copy.Properties["k"] = "changed";
        copy.Title = "B";

        Assert.Equal("v", point.Properties["k"]);
        Assert.Equal("A", point.Title);
        Assert.Equal(point.Id, copy.Id);
    }

    [Fact]
    public void ToGeoPoint_WithoutCoordinates_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateParticipant(latitude: null).ToGeoPoint());
        Assert.Throws<ArgumentException>(() => CreateParticipant(longitude: null).ToGeoPoint());
    }

    [Fact]
    public void ToGeoPoint_SkipsEmptyRequiredStringsAndKeepsSetOptionalOnes()
    {
        var participant = CreateParticipant("Bob");
        participant.Address = "";
        participant.City = "";
        participant.Country = null;

        var point = participant.ToGeoPoint();

        Assert.False(point.Properties.ContainsKey(ParticipantPointProperties.Address));
        Assert.Equal("", point.Properties[ParticipantPointProperties.City]);
        Assert.False(point.Properties.ContainsKey(ParticipantPointProperties.Country));
        Assert.True(point.Properties.ContainsKey(ParticipantPointProperties.RegisteredAt));
    }

    [Fact]
    public void ToParticipant_FromArbitraryPoint_UsesDefaults()
    {
        var participant = new GeoPoint { Id = "sensor-17", Latitude = 1, Longitude = 2, Title = "Sensor" }.ToParticipant();

        Assert.Equal("Sensor", participant.Name);
        Assert.Equal(string.Empty, participant.Email);
        Assert.Null(participant.SocialContacts);
        Assert.Equal(default, participant.RegisteredAt);
    }

    [Fact]
    public void ParticipantIdFor_KeepsGuidsAndDerivesStableVersion3Guids()
    {
        var guid = Guid.NewGuid();

        Assert.Equal(guid, ParticipantGeoPointExtensions.ParticipantIdFor(guid.ToString()));
        Assert.Equal(guid, ParticipantGeoPointExtensions.ParticipantIdFor(guid.ToString("N").ToUpperInvariant()));

        var derived = ParticipantGeoPointExtensions.ParticipantIdFor("sensor-17");
        Assert.Equal(derived, ParticipantGeoPointExtensions.ParticipantIdFor("sensor-17"));
        Assert.NotEqual(derived, ParticipantGeoPointExtensions.ParticipantIdFor("sensor-18"));
        Assert.Equal(3, derived.Version);
    }
}
