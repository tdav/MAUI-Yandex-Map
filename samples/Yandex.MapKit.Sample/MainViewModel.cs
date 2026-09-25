using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Sample;

public sealed record Place(string Name, GeoPoint Location);

public sealed class MainViewModel : INotifyPropertyChanged
{
    private GeoPoint _center = new(55.751244, 37.618423);
    private double _zoom = 13;
    private bool _isClustering;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Shown through <see cref="YandexMapView.ItemsSource"/> + <see cref="YandexMapView.ItemTemplate"/>.</summary>
    public ObservableCollection<Place> Places { get; } =
    [
        new("Красная площадь", new GeoPoint(55.753930, 37.620795)),
        new("Большой театр", new GeoPoint(55.760186, 37.618711)),
        new("Парк Горького", new GeoPoint(55.731482, 37.603527)),
        new("Москва-Сити", new GeoPoint(55.749511, 37.537083)),
        new("ВДНХ", new GeoPoint(55.826296, 37.637650)),
    ];

    public GeoPoint Center
    {
        get => _center;
        set
        {
            if (Set(ref _center, value))
                OnPropertyChanged(nameof(Status));
        }
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (Set(ref _zoom, value))
                OnPropertyChanged(nameof(Status));
        }
    }

    public bool IsClustering
    {
        get => _isClustering;
        set => Set(ref _isClustering, value);
    }

    public string Status => $"{Center}  ·  zoom {Zoom:0.0}";

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
