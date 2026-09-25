using ObjCRuntime;

namespace YandexMapsMobile;

[Native]
public enum YMKAnimationType : ulong
{
    Smooth,
    Linear,
}

[Native]
public enum YMKCameraUpdateReason : ulong
{
    Gestures,
    Application,
}

[Native]
public enum YMKTextStylePlacement : ulong
{
    Center,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}
