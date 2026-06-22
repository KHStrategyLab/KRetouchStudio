using System.Windows;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public static class LocalWorkbenchMaskPreviewBuilder
{
    public static LocalWorkbenchMaskSet BuildDoubleChinPlaceholderMasks(int width, int height)
    {
        return new LocalWorkbenchMaskSet(
            CreateEllipseOverlay(width, height, 0.50, 0.62, 0.58, 0.30, 0, 210, 190, 72),
            CreateBandOverlay(width, height, 0.18, 0.25, 245, 210, 70, 90),
            CreateBandOverlay(width, height, 0.76, 0.96, 255, 80, 80, 72),
            CreateEllipseOverlay(width, height, 0.50, 0.62, 0.48, 0.22, 80, 235, 170, 92));
    }

    public static LocalWorkbenchMaskSet BuildNosePlaceholderMasks(int width, int height)
    {
        return new LocalWorkbenchMaskSet(
            CreateEllipseOverlay(width, height, 0.50, 0.50, 0.30, 0.38, 255, 105, 105, 78),
            CreateBandOverlay(width, height, 0.00, 0.24, 245, 210, 70, 86),
            CreateBandOverlay(width, height, 0.80, 1.00, 255, 80, 80, 72),
            CreateRectOverlay(width, height, 0.50, 0.50, 0.88, 0.90, 80, 235, 170, 76));
    }

    private static BitmapSource CreateEllipseOverlay(
        int width,
        int height,
        double centerX,
        double centerY,
        double radiusX,
        double radiusY,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            double normalizedY = height <= 1 ? 0 : (double)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                double normalizedX = width <= 1 ? 0 : (double)x / (width - 1);
                double dx = (normalizedX - centerX) / Math.Max(0.0001, radiusX);
                double dy = (normalizedY - centerY) / Math.Max(0.0001, radiusY);
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > 1)
                {
                    continue;
                }

                double feather = 1 - SmoothStep(0.82, 1, distance);
                WritePixel(pixels, width, x, y, red, green, blue, (byte)Math.Clamp(alpha * feather, 0, 255));
            }
        }

        return CreateOverlay(width, height, pixels);
    }

    private static BitmapSource CreateBandOverlay(
        int width,
        int height,
        double startY,
        double endY,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            double normalizedY = height <= 1 ? 0 : (double)y / (height - 1);
            double verticalWeight = SmoothStep(startY, startY + 0.04, normalizedY) * (1 - SmoothStep(endY - 0.04, endY, normalizedY));
            if (verticalWeight <= 0)
            {
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                double normalizedX = width <= 1 ? 0 : (double)x / (width - 1);
                double horizontalWeight = SmoothStep(0.08, 0.22, normalizedX) * (1 - SmoothStep(0.78, 0.92, normalizedX));
                double weight = verticalWeight * horizontalWeight;
                WritePixel(pixels, width, x, y, red, green, blue, (byte)Math.Clamp(alpha * weight, 0, 255));
            }
        }

        return CreateOverlay(width, height, pixels);
    }

    private static BitmapSource CreateRectOverlay(
        int width,
        int height,
        double centerX,
        double centerY,
        double sizeX,
        double sizeY,
        byte red,
        byte green,
        byte blue,
        byte alpha)
    {
        byte[] pixels = new byte[width * height * 4];
        double halfX = Math.Max(0.0001, sizeX * 0.5);
        double halfY = Math.Max(0.0001, sizeY * 0.5);
        for (int y = 0; y < height; y++)
        {
            double normalizedY = height <= 1 ? 0 : (double)y / (height - 1);
            double dy = Math.Abs(normalizedY - centerY);
            if (dy > halfY)
            {
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                double normalizedX = width <= 1 ? 0 : (double)x / (width - 1);
                double dx = Math.Abs(normalizedX - centerX);
                if (dx > halfX)
                {
                    continue;
                }

                double edgeX = halfX <= 0 ? 1 : dx / halfX;
                double edgeY = halfY <= 0 ? 1 : dy / halfY;
                double edge = Math.Max(edgeX, edgeY);
                double feather = 1 - SmoothStep(0.88, 1, edge);
                WritePixel(pixels, width, x, y, red, green, blue, (byte)Math.Clamp(alpha * feather, 0, 255));
            }
        }

        return CreateOverlay(width, height, pixels);
    }

    private static BitmapSource CreateOverlay(int width, int height, byte[] pixels)
    {
        BitmapSource overlay = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, width * 4);
        overlay.Freeze();
        return overlay;
    }

    private static void WritePixel(byte[] pixels, int width, int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        int index = (y * width + x) * 4;
        pixels[index] = blue;
        pixels[index + 1] = green;
        pixels[index + 2] = red;
        pixels[index + 3] = alpha;
    }

    private static double SmoothStep(double edge0, double edge1, double value)
    {
        if (Math.Abs(edge1 - edge0) < 0.000001)
        {
            return value >= edge1 ? 1 : 0;
        }

        double t = Math.Clamp((value - edge0) / (edge1 - edge0), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
