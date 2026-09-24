using System.Collections.ObjectModel;
using System.ComponentModel;
using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Maui.Tests;

public class YandexMapViewTests
{
    private sealed record Place(string Name, double Lat, double Lon);

    private static DataTemplate PinTemplate() => new(() =>
    {
        var pin = new MapPin();
        pin.SetBinding(MapPin.LabelProperty, nameof(Place.Name));
        pin.SetBinding(MapPin.LocationProperty, new Binding(".", converter: new PlaceToPoint()));
        return pin;
    });

    private sealed class PlaceToPoint : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            value is Place p ? new GeoPoint(p.Lat, p.Lon) : default(GeoPoint);

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }

    [Fact]
    public void ItemsSource_creates_bound_pins()
    {
        var places = new ObservableCollection<Place> { new("A", 1, 2) };
        var map = new YandexMapView { ItemTemplate = PinTemplate(), ItemsSource = places };

        var pin = Assert.Single(map.Pins);
        Assert.Equal("A", pin.Label);
        Assert.Equal(new GeoPoint(1, 2), pin.Location);
        Assert.Same(places[0], pin.BindingContext);
    }

    [Fact]
    public void ItemsSource_changes_are_mirrored_and_manual_pins_are_kept()
    {
        var places = new ObservableCollection<Place> { new("A", 1, 2), new("B", 3, 4) };
        var map = new YandexMapView { ItemTemplate = PinTemplate(), ItemsSource = places };
        var manual = new MapPin { Label = "manual" };
        map.Pins.Add(manual);

        places.RemoveAt(0);
        places.Add(new Place("C", 5, 6));

        Assert.Equal(["B", "manual", "C"], map.Pins.Select(p => p.Label));

        map.ItemsSource = null;

        Assert.Equal([manual], map.Pins);
    }

    [Fact]
    public void ItemTemplateSelector_wins_over_ItemTemplate()
    {
        var map = new YandexMapView
        {
            ItemTemplate = new DataTemplate(() => new MapPin { Label = "template" }),
            ItemTemplateSelector = new Selector(),
            ItemsSource = new[] { new Place("A", 0, 0) },
        };

        Assert.Equal("selector", Assert.Single(map.Pins).Label);
    }

    private sealed class Selector : DataTemplateSelector
    {
        protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
            new(() => new MapPin { Label = "selector" });
    }

    [Fact]
    public void MoveTo_updates_properties_while_camera_mapping_is_suppressed()
    {
        var map = new YandexMapView();
        var suppressedDuringChange = new List<bool>();
        ((INotifyPropertyChanged)map).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(YandexMapView.Center) or nameof(YandexMapView.Zoom))
                suppressedDuringChange.Add(map.IsCameraMappingSuppressed);
        };

        map.MoveTo(new GeoPoint(59.93, 30.31), zoom: 14);

        Assert.Equal(new GeoPoint(59.93, 30.31), map.Center);
        Assert.Equal(14, map.Zoom);
        Assert.Equal([true, true], suppressedDuringChange);
        Assert.False(map.IsCameraMappingSuppressed);
    }

    [Fact]
    public void Platform_camera_change_updates_properties_and_raises_event()
    {
        var map = new YandexMapView();
        CameraChangedEventArgs? raised = null;
        map.CameraChanged += (_, e) => raised = e;
        var position = new MapCameraPosition(new GeoPoint(1, 2), 7, 30, 10);

        map.OnPlatformCameraChanged(position, CameraChangeReason.Gestures, finished: true);

        Assert.Equal(position, map.CameraPosition);
        Assert.NotNull(raised);
        Assert.Equal(CameraChangeReason.Gestures, raised.Reason);
        Assert.True(raised.IsFinished);
    }

    [Fact]
    public void Zoom_is_clamped()
    {
        var map = new YandexMapView { Zoom = 40 };
        Assert.Equal(YandexMapView.MaxZoom, map.Zoom);
    }

    [Fact]
    public void Pin_click_raises_pin_and_map_events()
    {
        var map = new YandexMapView();
        var pin = new MapPin { Label = "a" };
        var order = new List<string>();
        pin.MarkerClicked += (_, _) => order.Add("pin");
        map.PinClicked += (_, e) =>
        {
            order.Add("map");
            e.Handled = false;
        };

        var handled = map.OnPlatformPinClicked(pin);

        Assert.Equal(["pin", "map"], order);
        Assert.False(handled);
    }

    [Fact]
    public void Unhandled_cluster_click_zooms_in()
    {
        var map = new YandexMapView { Zoom = 10, ClusterMinZoom = 15 };
        var location = new GeoPoint(10, 20);

        map.OnPlatformClusterClicked([new MapPin(), new MapPin()], location);

        Assert.Equal(location, map.Center);
        Assert.Equal(14, map.Zoom);
    }

    [Fact]
    public void Handled_cluster_click_keeps_camera()
    {
        var map = new YandexMapView { Zoom = 10 };
        map.ClusterClicked += (_, e) => e.Handled = true;

        map.OnPlatformClusterClicked([new MapPin()], new GeoPoint(10, 20));

        Assert.Equal(YandexMapView.DefaultCenter, map.Center);
        Assert.Equal(10, map.Zoom);
    }

    [Theory]
    [InlineData(10, 15, 14)]
    [InlineData(13, 15, 15)]
    [InlineData(16, 15, 18)]
    [InlineData(20, 15, 21)]
    public void Cluster_zoom(double current, int minZoom, double expected) =>
        Assert.Equal(expected, YandexMapView.GetClusterZoom(current, minZoom));
}
