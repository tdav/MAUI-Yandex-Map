namespace Yandex.MapKit.Maui;

public sealed class MapClickedEventArgs(GeoPoint location) : EventArgs
{
    public GeoPoint Location { get; } = location;
}

public sealed class PinClickedEventArgs(MapPin pin) : EventArgs
{
    public MapPin Pin { get; } = pin;

    /// <summary>Set to <see langword="true"/> to stop the tap from reaching the map (no <see cref="YandexMapView.MapClicked"/>).</summary>
    public bool Handled { get; set; } = true;
}

public sealed class MapElementClickedEventArgs(MapElement element, GeoPoint location) : EventArgs
{
    public MapElement Element { get; } = element;
    public GeoPoint Location { get; } = location;
    public bool Handled { get; set; } = true;
}

public sealed class ClusterClickedEventArgs(IReadOnlyList<MapPin> pins, GeoPoint location) : EventArgs
{
    public IReadOnlyList<MapPin> Pins { get; } = pins;
    public GeoPoint Location { get; } = location;

    /// <summary>
    /// Set to <see langword="true"/> to suppress the default behavior
    /// (zooming the camera in on the cluster).
    /// </summary>
    public bool Handled { get; set; }
}

public sealed class CameraChangedEventArgs(MapCameraPosition position, CameraChangeReason reason, bool isFinished) : EventArgs
{
    public MapCameraPosition Position { get; } = position;
    public CameraChangeReason Reason { get; } = reason;

    /// <summary><see langword="true"/> when the movement has finished (the camera is idle).</summary>
    public bool IsFinished { get; } = isFinished;
}
