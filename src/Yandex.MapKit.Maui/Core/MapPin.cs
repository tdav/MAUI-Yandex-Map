namespace Yandex.MapKit.Maui;

/// <summary>A marker (placemark) on a <see cref="YandexMapView"/>.</summary>
/// <remarks>
/// Without <see cref="Icon"/> the pin is drawn as a standard marker filled with <see cref="Color"/>.
/// MapKit supports raster icons only: SVG sources are rasterized by MAUI's image pipeline first.
/// </remarks>
public class MapPin : BindableObject
{
    public static readonly Color DefaultColor = Color.FromArgb("#FFE53935");

    public static readonly BindableProperty LocationProperty =
        BindableProperty.Create(nameof(Location), typeof(GeoPoint), typeof(MapPin), default(GeoPoint));

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(MapPin));

    public static readonly BindableProperty IconProperty =
        BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(MapPin));

    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(MapPin), DefaultColor);

    public static readonly BindableProperty AnchorProperty =
        BindableProperty.Create(nameof(Anchor), typeof(Point), typeof(MapPin), new Point(0.5, 1.0));

    public static readonly BindableProperty IconScaleProperty =
        BindableProperty.Create(nameof(IconScale), typeof(double), typeof(MapPin), 1.0);

    public static readonly BindableProperty ZIndexProperty =
        BindableProperty.Create(nameof(ZIndex), typeof(double), typeof(MapPin), 0.0);

    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(MapPin), true);

    public GeoPoint Location
    {
        get => (GeoPoint)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    /// <summary>Text shown under the marker.</summary>
    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Custom raster icon. <see langword="null"/> draws the standard marker.</summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Fill color of the standard marker (ignored when <see cref="Icon"/> is set).</summary>
    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>Point of the icon placed at <see cref="Location"/>, in unit coordinates (0.5, 1 — bottom center).</summary>
    public Point Anchor
    {
        get => (Point)GetValue(AnchorProperty);
        set => SetValue(AnchorProperty, value);
    }

    public double IconScale
    {
        get => (double)GetValue(IconScaleProperty);
        set => SetValue(IconScaleProperty, value);
    }

    public double ZIndex
    {
        get => (double)GetValue(ZIndexProperty);
        set => SetValue(ZIndexProperty, value);
    }

    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    /// <summary>Raised when the user taps the marker.</summary>
    public event EventHandler<PinClickedEventArgs>? MarkerClicked;

    internal void SendMarkerClicked(PinClickedEventArgs args) => MarkerClicked?.Invoke(this, args);
}
