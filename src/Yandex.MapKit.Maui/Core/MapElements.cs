using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Yandex.MapKit.Maui;

/// <summary>Base class for vector shapes drawn on a <see cref="YandexMapView"/>.</summary>
public abstract class MapElement : BindableObject
{
    public static readonly BindableProperty StrokeColorProperty =
        BindableProperty.Create(nameof(StrokeColor), typeof(Color), typeof(MapElement), Color.FromArgb("#FF1E88E5"));

    public static readonly BindableProperty StrokeWidthProperty =
        BindableProperty.Create(nameof(StrokeWidth), typeof(double), typeof(MapElement), 3.0);

    public static readonly BindableProperty ZIndexProperty =
        BindableProperty.Create(nameof(ZIndex), typeof(double), typeof(MapElement), 0.0);

    public static readonly BindableProperty IsVisibleProperty =
        BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(MapElement), true);

    public Color StrokeColor
    {
        get => (Color)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    /// <summary>Line width in device-independent units.</summary>
    public double StrokeWidth
    {
        get => (double)GetValue(StrokeWidthProperty);
        set => SetValue(StrokeWidthProperty, value);
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

    /// <summary>Raised when the user taps the shape.</summary>
    public event EventHandler<MapElementClickedEventArgs>? Clicked;

    internal void SendClicked(MapElementClickedEventArgs args) => Clicked?.Invoke(this, args);
}

/// <summary>Base class for shapes defined by a list of points.</summary>
public abstract class GeopathElement : MapElement
{
    protected GeopathElement()
    {
        var geopath = new ObservableCollection<GeoPoint>();
        geopath.CollectionChanged += OnGeopathChanged;
        Geopath = geopath;
    }

    /// <summary>Vertices of the shape. Changes to the collection update the map.</summary>
    public IList<GeoPoint> Geopath { get; }

    private void OnGeopathChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnPropertyChanged(nameof(Geopath));
}

/// <summary>A polyline through <see cref="GeopathElement.Geopath"/>.</summary>
public sealed class MapPolyline : GeopathElement;

/// <summary>A closed polygon. The outline is <see cref="GeopathElement.Geopath"/>; it is closed automatically.</summary>
public sealed class MapPolygon : GeopathElement
{
    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(MapPolygon), Color.FromArgb("#401E88E5"));

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }
}

/// <summary>A circle with a radius in meters.</summary>
public sealed class MapCircle : MapElement
{
    public static readonly BindableProperty CenterProperty =
        BindableProperty.Create(nameof(Center), typeof(GeoPoint), typeof(MapCircle), default(GeoPoint));

    public static readonly BindableProperty RadiusProperty =
        BindableProperty.Create(nameof(Radius), typeof(double), typeof(MapCircle), 100.0);

    public static readonly BindableProperty FillColorProperty =
        BindableProperty.Create(nameof(FillColor), typeof(Color), typeof(MapCircle), Color.FromArgb("#401E88E5"));

    public GeoPoint Center
    {
        get => (GeoPoint)GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    /// <summary>Radius in meters.</summary>
    public double Radius
    {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public Color FillColor
    {
        get => (Color)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }
}
