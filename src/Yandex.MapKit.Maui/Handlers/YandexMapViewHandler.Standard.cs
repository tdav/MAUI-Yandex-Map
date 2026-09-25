#if !ANDROID && !IOS
using Microsoft.Maui.Handlers;

namespace Yandex.MapKit.Maui;

// Platforms without Yandex MapKit (and the platform-neutral net10.0 build).
public partial class YandexMapViewHandler
{
    protected override object CreatePlatformView() =>
        throw new PlatformNotSupportedException("Yandex MapKit is available on Android and iOS only.");

    private partial void MoveCamera(MapCameraPosition position, bool animated) { }
    private partial void UpdateNightMode() { }
    private partial void UpdateShowingUser() { }
    private partial void UpdateGestures() { }
    private partial void UpdateClustering() { }
}
#endif
