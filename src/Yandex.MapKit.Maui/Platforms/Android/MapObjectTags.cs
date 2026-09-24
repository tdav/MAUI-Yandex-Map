namespace Yandex.MapKit.Maui;

// Stored in MapObject.UserData to map native objects back to the MAUI models.
internal sealed class PinTag(MapPin pin) : Java.Lang.Object
{
    public MapPin Pin { get; } = pin;
}

internal sealed class ElementTag(MapElement element) : Java.Lang.Object
{
    public MapElement Element { get; } = element;
}
