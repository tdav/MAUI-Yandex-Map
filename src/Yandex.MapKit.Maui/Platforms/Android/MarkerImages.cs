using Android.Content;
using Android.Graphics;
using Paint = Android.Graphics.Paint;
using Android.Graphics.Drawables;
using Com.Yandex.Runtime.Image;
using MColor = Microsoft.Maui.Graphics.Color;

namespace Yandex.MapKit.Maui;

/// <summary>Creates and caches marker images: the standard pin, cluster badges and icons from <see cref="ImageSource"/>.</summary>
internal sealed class MarkerImages(Context context, IMauiContext mauiContext)
{
    private readonly Dictionary<int, ImageProvider> _defaultPins = [];
    private readonly Dictionary<(int Color, string Text), ImageProvider> _clusters = [];
    private readonly Dictionary<ImageSource, Task<ImageProvider?>> _icons = [];
    private readonly float _density = context.Resources?.DisplayMetrics?.Density ?? 1f;

    public ImageProvider DefaultPin(MColor color)
    {
        var argb = color.ToInt();
        if (_defaultPins.TryGetValue(argb, out var cached))
            return cached;

        var width = Dp(28);
        var height = Dp(40);
        var stroke = Dp(2);
        var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!)!;
        using var canvas = new Canvas(bitmap);

        var radius = width / 2f - stroke;
        var cx = width / 2f;
        var cy = radius + stroke;

        using var head = new Android.Graphics.Path();
        head.AddCircle(cx, cy, radius, Android.Graphics.Path.Direction.Cw!);
        using var tail = new Android.Graphics.Path();
        tail.MoveTo(cx - radius * 0.62f, cy + radius * 0.78f);
        tail.LineTo(cx, height - stroke);
        tail.LineTo(cx + radius * 0.62f, cy + radius * 0.78f);
        tail.Close();
        head.InvokeOp(tail, Android.Graphics.Path.Op.Union!);

        using var fill = new Paint(PaintFlags.AntiAlias) { Color = new Android.Graphics.Color(argb) };
        fill.SetStyle(Paint.Style.Fill);
        canvas.DrawPath(head, fill);

        using var outline = new Paint(PaintFlags.AntiAlias) { Color = Android.Graphics.Color.White, StrokeWidth = stroke };
        outline.SetStyle(Paint.Style.Stroke);
        canvas.DrawPath(head, outline);

        using var dot = new Paint(PaintFlags.AntiAlias) { Color = Android.Graphics.Color.White };
        canvas.DrawCircle(cx, cy, radius * 0.36f, dot);

        var provider = ImageProvider.FromBitmap(bitmap, true, $"ymaui-pin-{argb:X8}")!;
        _defaultPins[argb] = provider;
        return provider;
    }

    public ImageProvider Cluster(MColor color, int count)
    {
        var text = count > 999 ? "999+" : count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var argb = color.ToInt();
        if (_clusters.TryGetValue((argb, text), out var cached))
            return cached;

        using var textPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = Android.Graphics.Color.White,
            TextSize = Dp(14),
            TextAlign = Paint.Align.Center,
        };
        textPaint.SetTypeface(Typeface.DefaultBold);

        var stroke = Dp(2);
        var size = (int)Math.Max(Dp(36), textPaint.MeasureText(text) + Dp(16));
        var bitmap = Bitmap.CreateBitmap(size, size, Bitmap.Config.Argb8888!)!;
        using var canvas = new Canvas(bitmap);
        var center = size / 2f;

        using var fill = new Paint(PaintFlags.AntiAlias) { Color = new Android.Graphics.Color(argb) };
        canvas.DrawCircle(center, center, center - stroke, fill);
        using var outline = new Paint(PaintFlags.AntiAlias) { Color = Android.Graphics.Color.White, StrokeWidth = stroke };
        outline.SetStyle(Paint.Style.Stroke);
        canvas.DrawCircle(center, center, center - stroke, outline);

        var baseline = center - (textPaint.Descent() + textPaint.Ascent()) / 2f;
        canvas.DrawText(text, center, baseline, textPaint);

        var provider = ImageProvider.FromBitmap(bitmap, true, $"ymaui-cluster-{argb:X8}-{text}")!;
        _clusters[(argb, text)] = provider;
        return provider;
    }

    /// <summary>Loads an icon through MAUI's image services (files, resources, URIs, streams, fonts).</summary>
    public Task<ImageProvider?> Icon(ImageSource source)
    {
        if (!_icons.TryGetValue(source, out var task))
        {
            task = LoadIcon(source);
            _icons[source] = task;
        }

        return task;
    }

    private async Task<ImageProvider?> LoadIcon(ImageSource source)
    {
        try
        {
            using var result = await source.GetPlatformImageAsync(mauiContext);
            var bitmap = result?.Value switch
            {
                BitmapDrawable { Bitmap: { } b } => b.Copy(Bitmap.Config.Argb8888!, false),
                Drawable drawable => Rasterize(drawable),
                _ => null,
            };

            return bitmap is null ? null : ImageProvider.FromBitmap(bitmap, true, $"ymaui-icon-{Guid.NewGuid():N}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Yandex.MapKit.Maui] Failed to load pin icon {source}: {ex}");
            return null;
        }
    }

    private static Bitmap? Rasterize(Drawable drawable)
    {
        var width = drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : 1;
        var height = drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : 1;
        var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
        if (bitmap is null)
            return null;

        using var canvas = new Canvas(bitmap);
        drawable.SetBounds(0, 0, width, height);
        drawable.Draw(canvas);
        return bitmap;
    }

    private int Dp(float value) => (int)Math.Round(value * _density);
}
