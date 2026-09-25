using Yandex.MapKit.Maui;

namespace Yandex.MapKit.Maui.Tests;

internal sealed class FakeAdapter<T> : IMapObjectAdapter<T>
{
    public List<string> Log { get; } = [];
    public List<T> Items { get; } = [];

    public void Add(T item)
    {
        Items.Add(item);
        Log.Add($"add {Name(item)}");
    }

    public void Update(T item, string? propertyName) => Log.Add($"update {Name(item)} {propertyName}");

    public void Remove(T item)
    {
        Items.Remove(item);
        Log.Add($"remove {Name(item)}");
    }

    public void Clear()
    {
        Items.Clear();
        Log.Add("clear");
    }

    private static string? Name(T item) => item is MapPin pin ? pin.Label : item?.GetType().Name;
}
