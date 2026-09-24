using System.ComponentModel;
using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Maui.Tests;

public class GeoPointTests
{
    [Theory]
    [InlineData("55.751244, 37.618423", 55.751244, 37.618423)]
    [InlineData("-33.8688 151.2093", -33.8688, 151.2093)]
    [InlineData("0;0", 0, 0)]
    public void Converts_from_string(string text, double latitude, double longitude)
    {
        var converter = TypeDescriptor.GetConverter(typeof(GeoPoint));

        Assert.IsType<GeoPointTypeConverter>(converter);
        Assert.Equal(new GeoPoint(latitude, longitude), converter.ConvertFromInvariantString(text));
    }

    [Fact]
    public void Rejects_malformed_string() =>
        Assert.Throws<FormatException>(() => new GeoPointTypeConverter().ConvertFromInvariantString("55.7"));

    [Fact]
    public void Formats_with_invariant_culture() =>
        Assert.Equal("55.751244, 37.618423", new GeoPoint(55.751244, 37.618423).ToString());
}
