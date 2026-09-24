using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;
using MColor = Microsoft.Maui.Graphics.Color;

namespace Yandex.MapKit.Maui;

/// <summary>Creates and caches marker images: the standard pin, cluster badges and icons from <see cref="ImageSource"/>.</summary>
internal sealed class MarkerImages(IMauiContext mauiContext)
{
    private readonly Dictionary<int, UIImage> _defaultPins = [];
    private readonly Dictionary<(int Color, string Text), UIImage> _clusters = [];
    private readonly Dictionary<ImageSource, Task<UIImage?>> _icons = [];

    public UIImage DefaultPin(MColor color)
    {
        var argb = color.ToInt();
        if (_defaultPins.TryGetValue(argb, out var cached))
            return cached;

        const float width = 28, height = 40, stroke = 2;
        var radius = width / 2 - stroke;
        var center = new CGPoint(width / 2, radius + stroke);
        var tip = new CGPoint(width / 2, height - stroke);

        // Teardrop: the long arc of the head joined by tangents to the tip.
        var distance = tip.Y - center.Y;
        var alpha = (nfloat)Math.Acos(radius / distance);
        var path = new UIBezierPath();
        path.MoveTo(tip);
        path.AddArc(center, radius, (nfloat)(Math.PI / 2) + alpha, (nfloat)(Math.PI / 2) - alpha, true);
        path.ClosePath();
        path.LineWidth = stroke;
        path.LineJoinStyle = CGLineJoin.Round;

        var image = new UIGraphicsImageRenderer(new CGSize(width, height)).CreateImage(_ =>
        {
            color.ToPlatform().SetFill();
            path.Fill();
            UIColor.White.SetStroke();
            path.Stroke();
            UIColor.White.SetFill();
            UIBezierPath.FromOval(new CGRect(center.X - radius * 0.36f, center.Y - radius * 0.36f, radius * 0.72f, radius * 0.72f)).Fill();
        });

        _defaultPins[argb] = image;
        return image;
    }

    public UIImage Cluster(MColor color, nuint count)
    {
        var text = count > 999 ? "999+" : count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var argb = color.ToInt();
        if (_clusters.TryGetValue((argb, text), out var cached))
            return cached;

        var attributes = new UIStringAttributes
        {
            Font = UIFont.BoldSystemFontOfSize(14),
            ForegroundColor = UIColor.White,
        };
        var label = new NSString(text);
        var textSize = label.GetSizeUsingAttributes(attributes);
        const float stroke = 2;
        var size = (nfloat)Math.Max(36, textSize.Width + 16);

        var image = new UIGraphicsImageRenderer(new CGSize(size, size)).CreateImage(_ =>
        {
            var circle = UIBezierPath.FromOval(new CGRect(stroke, stroke, size - 2 * stroke, size - 2 * stroke));
            color.ToPlatform().SetFill();
            circle.Fill();
            UIColor.White.SetStroke();
            circle.LineWidth = stroke;
            circle.Stroke();
            label.DrawString(new CGPoint((size - textSize.Width) / 2, (size - textSize.Height) / 2), attributes);
        });

        _clusters[(argb, text)] = image;
        return image;
    }

    /// <summary>Loads an icon through MAUI's image services (files, resources, URIs, streams, fonts).</summary>
    public Task<UIImage?> Icon(ImageSource source)
    {
        if (!_icons.TryGetValue(source, out var task))
        {
            task = LoadIcon(source);
            _icons[source] = task;
        }

        return task;
    }

    private async Task<UIImage?> LoadIcon(ImageSource source)
    {
        try
        {
            // The result is not disposed: the image stays cached for the lifetime of the handler.
            var result = await source.GetPlatformImageAsync(mauiContext);
            return result?.Value;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Yandex.MapKit.Maui] Failed to load pin icon {source}: {ex}");
            return null;
        }
    }
}
