using System.Windows;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public static class NoseWorkAreaBuilder
{
    public static Int32Rect BuildNoseWorkAreaFallback(BitmapSource source)
    {
        return BuildFallback(source);
    }

    public static Int32Rect BuildFallback(BitmapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int boxWidth = Math.Max(1, (int)Math.Round(width * 0.14));
        int boxHeight = Math.Max(1, (int)Math.Round(height * 0.18));
        int x = Math.Clamp((int)Math.Round(width * 0.50 - boxWidth * 0.50), 0, Math.Max(0, width - boxWidth));
        int y = Math.Clamp((int)Math.Round(height * 0.39), 0, Math.Max(0, height - boxHeight));
        return new Int32Rect(x, y, boxWidth, boxHeight);
    }

}
