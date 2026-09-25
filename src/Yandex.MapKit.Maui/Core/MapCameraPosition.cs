namespace Yandex.MapKit.Maui;

/// <summary>Camera state of a <see cref="YandexMapView"/>.</summary>
/// <param name="Target">Point in the center of the map.</param>
/// <param name="Zoom">Zoom level (0 — whole world, ~21 — building level).</param>
/// <param name="Azimuth">Rotation in degrees, clockwise from north.</param>
/// <param name="Tilt">Tilt in degrees (0 — looking straight down).</param>
public readonly record struct MapCameraPosition(GeoPoint Target, double Zoom, double Azimuth = 0, double Tilt = 0);

/// <summary>What caused the camera to move.</summary>
public enum CameraChangeReason
{
    /// <summary>The user moved the map with a gesture.</summary>
    Gestures,

    /// <summary>The application moved the camera (property change or <see cref="YandexMapView.MoveTo"/>).</summary>
    Application,
}
