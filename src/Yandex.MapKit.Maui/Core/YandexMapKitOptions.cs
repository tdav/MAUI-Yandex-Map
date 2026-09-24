namespace Yandex.MapKit.Maui;

/// <summary>Process-wide MapKit settings, applied before the SDK is initialized.</summary>
public sealed class YandexMapKitOptions
{
    /// <summary>MapKit Mobile SDK key from the Yandex Developer Dashboard.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Map language, e.g. <c>"ru_RU"</c> or <c>"en_US"</c>. MapKit uses one language per process and it
    /// cannot be changed after initialization. <see langword="null"/> uses the system locale.
    /// </summary>
    public string? Locale { get; set; }

    internal static YandexMapKitOptions Current { get; set; } = new();
}
