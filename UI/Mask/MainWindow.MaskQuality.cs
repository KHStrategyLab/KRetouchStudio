using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const byte PersonMaskForegroundThreshold = 96;
    private const int PersonMaskCloseRadius = 2;
    private const int PersonMaskFeatherRadius = 3;

    private RefinedPersonAlphaMask? _refinedPersonAlphaMask;

    private byte[] GetOrCreateRefinedPersonAlphaMask(string alphaPath, int width, int height)
    {
        if (_refinedPersonAlphaMask is not null &&
            _refinedPersonAlphaMask.Width == width &&
            _refinedPersonAlphaMask.Height == height &&
            string.Equals(_refinedPersonAlphaMask.AlphaPath, alphaPath, StringComparison.OrdinalIgnoreCase))
        {
            return _refinedPersonAlphaMask.Pixels;
        }

        byte[] rawAlpha = LoadPersonAlphaGray8Pixels(alphaPath, width, height);
        byte[] refinedAlpha = string.Equals(_personAlphaEngine, PersonAlphaEngineBiRefNet, StringComparison.OrdinalIgnoreCase)
            ? (byte[])rawAlpha.Clone()
            : RefinePersonAlphaMask(rawAlpha, width, height);
        _refinedPersonAlphaMask = new RefinedPersonAlphaMask(alphaPath, width, height, refinedAlpha);
        return refinedAlpha;
    }

    private void ClearRefinedPersonAlphaCache()
    {
        _refinedPersonAlphaMask = null;
    }

    private static byte[] LoadPersonAlphaGray8Pixels(string alphaPath, int width, int height)
    {
        BitmapSource alphaSource = EnsureAlphaMaskSize(LoadBitmapSourceFromFile(alphaPath), width, height);
        alphaSource = EnsureBitmapFormat(alphaSource, PixelFormats.Gray8);

        int stride = CalculateStride(width, PixelFormats.Gray8);
        byte[] source = new byte[stride * height];
        alphaSource.CopyPixels(source, stride, 0);

        if (stride == width)
        {
            return source;
        }

        byte[] compact = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(source, y * stride, compact, y * width, width);
        }

        return compact;
    }

    private static byte[] RefinePersonAlphaMask(byte[] rawAlpha, int width, int height)
    {
        if (rawAlpha.Length == 0 || width <= 0 || height <= 0)
        {
            return (byte[])rawAlpha.Clone();
        }

        bool[] supportMask = BuildBinaryMask(rawAlpha, PersonMaskForegroundThreshold);
        int pixelCount = width * height;
        int minComponentArea = Math.Clamp(pixelCount / 4000, 32, 1200);
        int maxHoleArea = Math.Clamp(pixelCount / 1200, 64, 2500);

        RemoveSmallForegroundComponents(supportMask, width, height, minComponentArea);
        supportMask = DilateBinaryMask(supportMask, width, height, PersonMaskCloseRadius);
        supportMask = ErodeBinaryMask(supportMask, width, height, PersonMaskCloseRadius);
        FillSmallBackgroundHoles(supportMask, width, height, maxHoleArea);
        RemoveSmallForegroundComponents(supportMask, width, height, minComponentArea);

        bool[] innerMask = ErodeBinaryMask(supportMask, width, height, PersonMaskCloseRadius);
        byte[] supportAlpha = new byte[pixelCount];
        for (int i = 0; i < supportAlpha.Length; i++)
        {
            supportAlpha[i] = supportMask[i] ? byte.MaxValue : byte.MinValue;
        }

        byte[] softSupportAlpha = BoxBlurGray8(supportAlpha, width, height, PersonMaskFeatherRadius);
        byte[] refinedAlpha = new byte[pixelCount];
        for (int i = 0; i < refinedAlpha.Length; i++)
        {
            int alpha = Math.Max(rawAlpha[i], softSupportAlpha[i]);
            if (innerMask[i])
            {
                alpha = Math.Max(alpha, 245);
            }
            else if (!supportMask[i] && rawAlpha[i] <= 8)
            {
                alpha = Math.Min(alpha, 128);
            }

            refinedAlpha[i] = (byte)Math.Clamp(alpha, 0, 255);
        }

        return refinedAlpha;
    }

    private static bool[] BuildBinaryMask(byte[] alpha, byte threshold)
    {
        bool[] mask = new bool[alpha.Length];
        for (int i = 0; i < alpha.Length; i++)
        {
            mask[i] = alpha[i] >= threshold;
        }

        return mask;
    }

    private static bool[] DilateBinaryMask(bool[] source, int width, int height, int radius)
    {
        if (radius <= 0)
        {
            return (bool[])source.Clone();
        }

        bool[] result = new bool[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool hasForeground = false;
                for (int dy = -radius; dy <= radius && !hasForeground; dy++)
                {
                    int sampleY = y + dy;
                    if (sampleY < 0 || sampleY >= height)
                    {
                        continue;
                    }

                    int rowOffset = sampleY * width;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int sampleX = x + dx;
                        if (sampleX >= 0 && sampleX < width && source[rowOffset + sampleX])
                        {
                            hasForeground = true;
                            break;
                        }
                    }
                }

                result[(y * width) + x] = hasForeground;
            }
        }

        return result;
    }

    private static bool[] ErodeBinaryMask(bool[] source, int width, int height, int radius)
    {
        if (radius <= 0)
        {
            return (bool[])source.Clone();
        }

        bool[] result = new bool[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool allForeground = true;
                for (int dy = -radius; dy <= radius && allForeground; dy++)
                {
                    int sampleY = y + dy;
                    if (sampleY < 0 || sampleY >= height)
                    {
                        allForeground = false;
                        break;
                    }

                    int rowOffset = sampleY * width;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int sampleX = x + dx;
                        if (sampleX < 0 || sampleX >= width || !source[rowOffset + sampleX])
                        {
                            allForeground = false;
                            break;
                        }
                    }
                }

                result[(y * width) + x] = allForeground;
            }
        }

        return result;
    }

    private static void RemoveSmallForegroundComponents(bool[] mask, int width, int height, int minArea)
    {
        bool[] visited = new bool[mask.Length];
        int[] queue = new int[mask.Length];
        int[] component = new int[mask.Length];

        for (int start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || visited[start])
            {
                continue;
            }

            int count = CollectComponent(mask, visited, queue, component, width, height, start, foreground: true, out _);
            if (count >= minArea)
            {
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                mask[component[i]] = false;
            }
        }
    }

    private static void FillSmallBackgroundHoles(bool[] mask, int width, int height, int maxArea)
    {
        bool[] visited = new bool[mask.Length];
        int[] queue = new int[mask.Length];
        int[] component = new int[mask.Length];

        for (int start = 0; start < mask.Length; start++)
        {
            if (mask[start] || visited[start])
            {
                continue;
            }

            int count = CollectComponent(mask, visited, queue, component, width, height, start, foreground: false, out bool touchesBorder);
            if (touchesBorder || count > maxArea)
            {
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                mask[component[i]] = true;
            }
        }
    }

    private static int CollectComponent(
        bool[] mask,
        bool[] visited,
        int[] queue,
        int[] component,
        int width,
        int height,
        int start,
        bool foreground,
        out bool touchesBorder)
    {
        int head = 0;
        int tail = 0;
        int count = 0;
        touchesBorder = false;

        visited[start] = true;
        queue[tail++] = start;

        while (head < tail)
        {
            int index = queue[head++];
            component[count++] = index;

            int x = index % width;
            int y = index / width;
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                touchesBorder = true;
            }

            TryQueueComponentNeighbor(mask, visited, queue, ref tail, index - 1, x > 0, foreground);
            TryQueueComponentNeighbor(mask, visited, queue, ref tail, index + 1, x < width - 1, foreground);
            TryQueueComponentNeighbor(mask, visited, queue, ref tail, index - width, y > 0, foreground);
            TryQueueComponentNeighbor(mask, visited, queue, ref tail, index + width, y < height - 1, foreground);
        }

        return count;
    }

    private static void TryQueueComponentNeighbor(
        bool[] mask,
        bool[] visited,
        int[] queue,
        ref int tail,
        int index,
        bool inBounds,
        bool foreground)
    {
        if (!inBounds || visited[index] || mask[index] != foreground)
        {
            return;
        }

        visited[index] = true;
        queue[tail++] = index;
    }

    private sealed record RefinedPersonAlphaMask(string AlphaPath, int Width, int Height, byte[] Pixels);
}
