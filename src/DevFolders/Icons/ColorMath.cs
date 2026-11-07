namespace DevFolders.Icons;

internal static class ColorMath
{
    public static (double Hue, double Saturation, double Lightness) ToHsl(byte red, byte green, byte blue)
    {
        double r = red / 255.0, g = green / 255.0, b = blue / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double lightness = (max + min) / 2;
        double delta = max - min;

        if (delta == 0)
        {
            return (0, 0, lightness);
        }

        double saturation = lightness > 0.5 ? delta / (2 - max - min) : delta / (max + min);
        double hue = max == r ? (g - b) / delta + (g < b ? 6 : 0)
            : max == g ? (b - r) / delta + 2
            : (r - g) / delta + 4;

        return (hue * 60, saturation, lightness);
    }

    public static (byte Red, byte Green, byte Blue) FromHsl(double hue, double saturation, double lightness)
    {
        if (saturation == 0)
        {
            byte gray = ToByte(lightness);
            return (gray, gray, gray);
        }

        double q = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        double p = 2 * lightness - q;
        double h = hue / 360;

        return (ToByte(Channel(p, q, h + 1.0 / 3)), ToByte(Channel(p, q, h)), ToByte(Channel(p, q, h - 1.0 / 3)));
    }

    private static double Channel(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
