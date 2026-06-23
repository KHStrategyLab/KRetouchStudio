using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const int WarpTensionBlurRadius = 6;
    private const byte WarpTensionBackgroundLockThreshold = 8;

    private WarpTensionMap? _liquifyTensionMap;
    private string? _liquifyTensionPhotoPath;

    private async Task<bool> EnsureLiquifyTensionMapAsync(PhotoItem photo, int width, int height)
    {
        if (_liquifyTensionMap is not null &&
            _liquifyTensionMap.Width == width &&
            _liquifyTensionMap.Height == height &&
            string.Equals(_liquifyTensionPhotoPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string? alphaPath = await GetOrCreatePersonAlphaPathAsync(photo);
        if (string.IsNullOrWhiteSpace(alphaPath) || !File.Exists(alphaPath))
        {
            _liquifyTensionMap = null;
            _liquifyTensionPhotoPath = null;
            return false;
        }

        _liquifyTensionMap = BuildWarpTensionMap(alphaPath, width, height);
        _liquifyTensionPhotoPath = photo.Path;
        return true;
    }

    private void ClearLiquifyTensionCache()
    {
        _liquifyTensionMap = null;
        _liquifyTensionPhotoPath = null;
    }

    private double GetLiquifyTensionWeight(int x, int y, int width, int height)
    {
        if (_liquifyTensionMap is null ||
            _liquifyTensionMap.Width != width ||
            _liquifyTensionMap.Height != height)
        {
            return 1.0;
        }

        int clampedX = Math.Clamp(x, 0, width - 1);
        int clampedY = Math.Clamp(y, 0, height - 1);
        return _liquifyTensionMap.Weights[(clampedY * width) + clampedX] / 255.0;
    }

    private WarpTensionMap BuildWarpTensionMap(string alphaPath, int width, int height)
    {
        byte[] compactAlpha = GetOrCreateRefinedPersonAlphaMask(alphaPath, width, height);
        byte[] softAlpha = BoxBlurGray8(compactAlpha, width, height, WarpTensionBlurRadius);
        byte[] weights = new byte[softAlpha.Length];
        for (int i = 0; i < softAlpha.Length; i++)
        {
            int alphaValue = softAlpha[i];
            if (alphaValue <= WarpTensionBackgroundLockThreshold)
            {
                weights[i] = 0;
                continue;
            }

            double normalized = alphaValue / 255.0;
            weights[i] = (byte)Math.Clamp((int)Math.Round(SmoothStep01(normalized) * 255.0), 0, 255);
        }

        return new WarpTensionMap(width, height, weights);
    }

    private static byte[] BoxBlurGray8(byte[] source, int width, int height, int radius)
    {
        if (radius <= 0 || width <= 0 || height <= 0)
        {
            return (byte[])source.Clone();
        }

        byte[] horizontal = new byte[source.Length];
        byte[] result = new byte[source.Length];
        int windowSize = (radius * 2) + 1;

        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * width;
            int sum = 0;
            for (int x = -radius; x <= radius; x++)
            {
                int sampleX = Math.Clamp(x, 0, width - 1);
                sum += source[rowOffset + sampleX];
            }

            for (int x = 0; x < width; x++)
            {
                horizontal[rowOffset + x] = (byte)((sum + (windowSize / 2)) / windowSize);

                int removeX = Math.Clamp(x - radius, 0, width - 1);
                int addX = Math.Clamp(x + radius + 1, 0, width - 1);
                sum += source[rowOffset + addX] - source[rowOffset + removeX];
            }
        }

        for (int x = 0; x < width; x++)
        {
            int sum = 0;
            for (int y = -radius; y <= radius; y++)
            {
                int sampleY = Math.Clamp(y, 0, height - 1);
                sum += horizontal[(sampleY * width) + x];
            }

            for (int y = 0; y < height; y++)
            {
                result[(y * width) + x] = (byte)((sum + (windowSize / 2)) / windowSize);

                int removeY = Math.Clamp(y - radius, 0, height - 1);
                int addY = Math.Clamp(y + radius + 1, 0, height - 1);
                sum += horizontal[(addY * width) + x] - horizontal[(removeY * width) + x];
            }
        }

        return result;
    }

    private sealed record WarpTensionMap(int Width, int Height, byte[] Weights);
}
