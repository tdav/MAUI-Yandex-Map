using System.ComponentModel;
using System.Globalization;

namespace Yandex.MapKit.Maui;

/// <summary>Converts <c>"55.751, 37.618"</c> (latitude, longitude; invariant culture) to <see cref="GeoPoint"/> for XAML.</summary>
public sealed class GeoPointTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
            return base.ConvertFrom(context, culture, value);

        var parts = text.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2
            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return new GeoPoint(latitude, longitude);
        }

        throw new FormatException($"Cannot convert \"{text}\" to {nameof(GeoPoint)}. Expected \"latitude, longitude\".");
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        value is GeoPoint point && destinationType == typeof(string)
            ? point.ToString()
            : base.ConvertTo(context, culture, value, destinationType);
}
