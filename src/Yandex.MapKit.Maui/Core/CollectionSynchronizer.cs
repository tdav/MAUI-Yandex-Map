using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Yandex.MapKit.Maui;

/// <summary>Applies model changes to native map objects.</summary>
internal interface IMapObjectAdapter<in T>
{
    void Add(T item);

    /// <param name="propertyName">Changed property; <see langword="null"/> means "refresh everything".</param>
    void Update(T item, string? propertyName);

    void Remove(T item);

    /// <summary>Removes every native object created by this adapter.</summary>
    void Clear();
}

/// <summary>
/// Mirrors an <see cref="ObservableCollection{T}"/> of bindable models onto an <see cref="IMapObjectAdapter{T}"/>:
/// collection changes become Add/Remove, item property changes become Update.
/// </summary>
internal sealed class CollectionSynchronizer<T> : IDisposable where T : class, INotifyPropertyChanged
{
    private readonly ObservableCollection<T> _source;
    private readonly List<T> _tracked = [];
    private IMapObjectAdapter<T> _adapter;
    private bool _disposed;

    public CollectionSynchronizer(ObservableCollection<T> source, IMapObjectAdapter<T> adapter)
    {
        _source = source;
        _adapter = adapter;
        _source.CollectionChanged += OnCollectionChanged;
        AttachAll();
    }

    public IReadOnlyList<T> Tracked => _tracked;

    /// <summary>Removes everything from the current adapter and re-adds all items to <paramref name="adapter"/>.</summary>
    public void Reset(IMapObjectAdapter<T> adapter)
    {
        DetachAll();
        _adapter = adapter;
        AttachAll();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _source.CollectionChanged -= OnCollectionChanged;
        foreach (var item in _tracked)
            item.PropertyChanged -= OnItemPropertyChanged;
        _tracked.Clear();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (T item in e.NewItems!)
                    Attach(item);
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (T item in e.OldItems!)
                    Detach(item);
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (T item in e.OldItems!)
                    Detach(item);
                foreach (T item in e.NewItems!)
                    Attach(item);
                break;

            case NotifyCollectionChangedAction.Move:
                break;

            case NotifyCollectionChangedAction.Reset:
                DetachAll();
                AttachAll();
                break;
        }
    }

    private void AttachAll()
    {
        foreach (var item in _source)
            Attach(item);
    }

    private void DetachAll()
    {
        foreach (var item in _tracked)
            item.PropertyChanged -= OnItemPropertyChanged;
        _tracked.Clear();
        _adapter.Clear();
    }

    private void Attach(T item)
    {
        if (_tracked.Contains(item))
            return;

        _tracked.Add(item);
        item.PropertyChanged += OnItemPropertyChanged;
        _adapter.Add(item);
    }

    private void Detach(T item)
    {
        if (!_tracked.Remove(item))
            return;

        item.PropertyChanged -= OnItemPropertyChanged;
        _adapter.Remove(item);
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // BindingContext changes on templated pins do not affect the native object.
        if (sender is T item && e.PropertyName != BindableObject.BindingContextProperty.PropertyName)
            _adapter.Update(item, e.PropertyName);
    }
}
