using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevFolders.Shell;

namespace DevFolders.Icons;

public enum FolderStyle
{
    Standard,
    DevFolder,
}

/// <summary>
/// Draws folder icons: the Windows folder (or a blue variant with a code mark for dev folders),
/// optionally with a badge in the bottom-right corner.
/// </summary>
public static class FolderIconRenderer
{
    public static readonly int[] IconSizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256];

    private const double DevFolderHueShift = 170;
    private static readonly Color BadgeOutline = Color.FromArgb(0x30, 0, 0, 0);
    private static readonly Geometry CodeMark = Geometry.Parse("M 0.41,0.50 L 0.33,0.60 L 0.41,0.70 M 0.59,0.50 L 0.67,0.60 L 0.59,0.70 M 0.54,0.47 L 0.46,0.73");
    private static readonly ConcurrentDictionary<(FolderStyle, int), BitmapSource> BaseCache = new();

    public static byte[] CreateIcon(FolderStyle style, Badge? badge) =>
        IcoWriter.Write(IconSizes.Select(size => Render(style, badge, size)).ToList());

    public static BitmapSource Render(FolderStyle style, Badge? badge, int size)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

        using (var context = visual.RenderOpen())
        {
            context.DrawImage(FolderBase(style, size), new Rect(0, 0, size, size));

            if (style == FolderStyle.DevFolder && size >= 24)
            {
                DrawCodeMark(context, size);
            }

            if (badge != null)
            {
                DrawBadge(context, badge, size);
            }
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource FolderBase(FolderStyle style, int size) =>
        BaseCache.GetOrAdd((style, size), key =>
        {
            var folder = ShellIcons.Folder(key.Item2) ?? FallbackFolder(key.Item2);
            return key.Item1 == FolderStyle.DevFolder ? ShiftHue(folder, DevFolderHueShift) : folder;
        });

    private static void DrawCodeMark(DrawingContext context, int size)
    {
        var geometry = CodeMark.Clone();
        geometry.Transform = new ScaleTransform(size, size);
        var pen = new Pen(Brushes.White, size * 0.055) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    /// Draws the badge as a white rounded tile anchored to the bottom-right corner. Image badges are
    /// clipped into the tile; glyph badges are tinted white on a colored background. Small icon sizes
    /// get a proportionally larger badge so it stays recognizable in list and details views.
    /// </summary>
    private static void DrawBadge(DrawingContext context, Badge badge, int size)
    {
        double ratio = size <= 16 ? 0.62 : size <= 32 ? 0.52 : 0.46;
        double outer = Math.Round(size * ratio);
        var tile = new Rect(size - outer, size - outer, outer, outer);
        double radius = outer * 0.24;
        double outline = Math.Max(1, size / 96.0);

        var outlineRect = Rect.Inflate(tile, -outline / 2, -outline / 2);
        context.DrawRoundedRectangle(Brushes.White, new Pen(new SolidColorBrush(BadgeOutline), outline), outlineRect, radius, radius);

        double inset = Math.Max(1, Math.Round(outer * 0.08));
        var inner = Rect.Inflate(tile, -inset, -inset);
        double innerRadius = Math.Max(0, radius - inset);
        int pixels = (int)Math.Ceiling(inner.Width);

        switch (badge)
        {
            case ImageBadge image:
                context.PushClip(new RectangleGeometry(inner, innerRadius, innerRadius));
                context.DrawImage(image.Artwork.ForSize(pixels), Fit(inner, image.Artwork.AspectRatio));
                context.Pop();
                break;

            case GlyphBadge glyph:
                context.DrawRoundedRectangle(new SolidColorBrush(glyph.Background), null, inner, innerRadius, innerRadius);
                var glyphArea = Fit(Rect.Inflate(inner, -inner.Width * 0.17, -inner.Height * 0.17), glyph.Artwork.AspectRatio);
                context.PushOpacityMask(new ImageBrush(glyph.Artwork.ForSize(pixels)));
                context.DrawRectangle(Brushes.White, null, glyphArea);
                context.Pop();
                break;
        }
    }

    private static Rect Fit(Rect area, double aspectRatio)
    {
        double width = area.Width;
        double height = area.Height;
        if (aspectRatio > 1)
        {
            height = width / aspectRatio;
        }
        else
        {
            width = height * aspectRatio;
        }

        return new Rect(area.X + (area.Width - width) / 2, area.Y + (area.Height - height) / 2, width, height);
    }

    private static BitmapSource ShiftHue(BitmapSource source, double degrees)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int width = converted.PixelWidth;
        int height = converted.PixelHeight;
        int stride = width * 4;
        var pixels = new byte[stride * height];
        converted.CopyPixels(pixels, stride, 0);

        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0)
            {
                continue;
            }

            var (hue, saturation, lightness) = ColorMath.ToHsl(pixels[i + 2], pixels[i + 1], pixels[i]);
            var (r, g, b) = ColorMath.FromHsl((hue + degrees) % 360, saturation, lightness * 0.88);
            pixels[i + 2] = r;
            pixels[i + 1] = g;
            pixels[i] = b;
        }

        var result = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        result.Freeze();
        return result;
    }

    private static BitmapSource FallbackFolder(int size)
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0xE0, 0xA4, 0x2B)), null,
            Geometry.Parse("M 4,12 A 3,3 0 0 1 7,9 H 22 L 27,14 H 57 A 3,3 0 0 1 60,17 V 52 A 3,3 0 0 1 57,55 H 7 A 3,3 0 0 1 4,52 Z")));
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0xFF, 0xC8, 0x3D)), null,
            Geometry.Parse("M 4,22 A 3,3 0 0 1 7,19 H 57 A 3,3 0 0 1 60,22 V 52 A 3,3 0 0 1 57,55 H 7 A 3,3 0 0 1 4,52 Z")));
        drawing.Transform = new ScaleTransform(size / 64.0, size / 64.0);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawDrawing(drawing);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
