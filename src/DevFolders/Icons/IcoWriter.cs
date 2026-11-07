using System.IO;
using System.Windows.Media.Imaging;

namespace DevFolders.Icons;

internal static class IcoWriter
{
    /// <summary>
    /// Packs square images into a multi-resolution .ico file, storing every entry as PNG.
    /// </summary>
    public static byte[] Write(IReadOnlyList<BitmapSource> images)
    {
        var encoded = images.Select(EncodePng).ToList();

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)images.Count);

        int offset = 6 + 16 * images.Count;
        for (int i = 0; i < images.Count; i++)
        {
            byte dimension = images[i].PixelWidth >= 256 ? (byte)0 : (byte)images[i].PixelWidth;
            writer.Write(dimension);
            writer.Write(dimension);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(encoded[i].Length);
            writer.Write(offset);
            offset += encoded[i].Length;
        }

        foreach (var png in encoded)
        {
            writer.Write(png);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] EncodePng(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
