using Foundation;

namespace Yandex.MapKit.Maui;

// Stored in YMKMapObject.userData to map native objects back to the MAUI models.
internal sealed class PinTag(MapPin pin) : NSObject
{
    public MapPin Pin { get; } = pin;
}

internal sealed class ElementTag(MapElement element) : NSObject
{
    public MapElement Element { get; } = element;
}
