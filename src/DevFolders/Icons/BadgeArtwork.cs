using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevFolders.Shell;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace DevFolders.Icons;

/// <summary>
/// Artwork shown inside a folder badge. Holds either raster frames (ICO, PNG, EXE icons)
/// or a vector drawing (SVG), and hands out the best source for a requested pixel size.
/// </summary>
public sealed class BadgeArtwork
{
    private static readonly int[] ExecutableIconSizes = [16, 32, 48, 256];

    private readonly IReadOnlyList<BitmapSource> _frames;
    private readonly DrawingImage? _vector;

    private BadgeArtwork(IReadOnlyList<BitmapSource> frames)
    {
        _frames = frames;
        var largest = frames.MaxBy(f => f.PixelWidth)!;
        AspectRatio = (double)largest.PixelWidth / largest.PixelHeight;
        LargestSize = Math.Max(largest.PixelWidth, largest.PixelHeight);
    }

    private BadgeArtwork(DrawingImage vector)
    {
        _frames = [];
        _vector = vector;
        var bounds = vector.Drawing.Bounds;
        AspectRatio = bounds.Height > 0 ? bounds.Width / bounds.Height : 1;
        LargestSize = int.MaxValue;
    }

    public double AspectRatio { get; }

    public int LargestSize { get; }

    public bool IsUsableAsBadge => LargestSize >= 16 && AspectRatio is >= 0.8 and <= 1.25;

    public static BadgeArtwork? TryLoad(string path)
    {
        try
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".svg" => FromSvg(path),
                ".exe" or ".dll" => FromExecutable(path),
                _ => FromImage(path),
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    public ImageSource ForSize(int pixels)
    {
        if (_vector != null)
        {
            return _vector;
        }

        return _frames.Where(f => f.PixelWidth >= pixels).MinBy(f => f.PixelWidth)
            ?? _frames.MaxBy(f => f.PixelWidth)!;
    }

    private static BadgeArtwork? FromImage(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frames = decoder.Frames.Select(Frozen).ToList();
        return frames.Count > 0 ? new BadgeArtwork(frames) : null;
    }

    private static BadgeArtwork? FromExecutable(string path)
    {
        var frames = ExecutableIconSizes
            .Select(size => ShellIcons.Extract(path, 0, size))
            .OfType<BitmapSource>()
            .ToList();
        return frames.Count > 0 ? new BadgeArtwork(frames) : null;
    }

    private static BadgeArtwork? FromSvg(string path)
    {
        var settings = new WpfDrawingSettings { IncludeRuntime = false, TextAsGeometry = true };
        var drawing = new FileSvgReader(settings).Read(path);
        if (drawing == null || drawing.Bounds.IsEmpty)
        {
            return null;
        }

        var image = new DrawingImage(drawing);
        image.Freeze();
        return new BadgeArtwork(image);
    }

    private static BitmapSource Frozen(BitmapSource bitmap)
    {
        if (!bitmap.IsFrozen && bitmap.CanFreeze)
        {
            bitmap.Freeze();
        }

        return bitmap;
    }
}
