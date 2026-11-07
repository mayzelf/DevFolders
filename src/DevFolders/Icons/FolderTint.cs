using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DevFolders.Icons;

internal static class FolderTint
{
    private const int SampleSize = 32;
    private const int HueBins = 24;

    public static Color? From(Badge badge) => badge switch
    {
        GlyphBadge glyph => glyph.Background,
        _ => DominantColor(badge.Artwork),
    };

    /// <summary>
    /// Finds the most prominent color of an artwork. Opaque pixels are grouped by hue and weighted by
    /// how vivid they are; artwork without a clear color (black, white, gray) yields its average gray.
    /// </summary>
    private static Color? DominantColor(BadgeArtwork artwork)
    {
        var pixels = Sample(artwork);
        var weights = new double[HueBins];
        var sums = new (double R, double G, double B)[HueBins];
        double totalWeight = 0, totalLightness = 0;
        int opaque = 0;

        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] < 128)
            {
                continue;
            }

            byte r = pixels[i + 2], g = pixels[i + 1], b = pixels[i];
            var (hue, saturation, lightness) = ColorMath.ToHsl(r, g, b);
            opaque++;
            totalLightness += lightness;

            double weight = saturation * (1 - Math.Abs(lightness - 0.5) * 2);
            int bin = (int)(hue / 360 * HueBins) % HueBins;
            weights[bin] += weight;
            sums[bin] = (sums[bin].R + r * weight, sums[bin].G + g * weight, sums[bin].B + b * weight);
            totalWeight += weight;
        }

        if (opaque == 0)
        {
            return null;
        }

        if (totalWeight < opaque * 0.1)
        {
            byte gray = (byte)Math.Round(totalLightness / opaque * 255);
            return Color.FromRgb(gray, gray, gray);
        }

        int best = Array.IndexOf(weights, weights.Max());
        var (sumR, sumG, sumB) = sums[best];
        return Color.FromRgb((byte)(sumR / weights[best]), (byte)(sumG / weights[best]), (byte)(sumB / weights[best]));
    }

    private static byte[] Sample(BadgeArtwork artwork)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(artwork.ForSize(SampleSize), new Rect(0, 0, SampleSize, SampleSize));
        }

        var bitmap = new RenderTargetBitmap(SampleSize, SampleSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[SampleSize * SampleSize * 4];
        converted.CopyPixels(pixels, SampleSize * 4, 0);
        return pixels;
    }
}
