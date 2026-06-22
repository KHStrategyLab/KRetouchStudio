using System.Windows;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public static class DoubleChinWorkAreaBuilder
{
    public static Int32Rect Build(BitmapSource source)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int areaSize = Math.Max(1, (int)Math.Round(Math.Min(width, height) * 0.52));
        int x = Math.Clamp((width - areaSize) / 2, 0, Math.Max(0, width - areaSize));
        int y = Math.Clamp((int)Math.Round(height * 0.38), 0, Math.Max(0, height - areaSize));
        return new Int32Rect(x, y, areaSize, areaSize);
    }
}
