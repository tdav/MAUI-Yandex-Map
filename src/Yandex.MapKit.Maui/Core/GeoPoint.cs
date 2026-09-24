namespace Yandex.MapKit.Maui;

/// <summary>A WGS-84 coordinate.</summary>
public readonly record struct GeoPoint(double Latitude, double Longitude)
{
    public override string ToString() =>
        string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitude:0.######}, {Longitude:0.######}");
}
