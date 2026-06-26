using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    private string _magicToolMode = "wand";

    public Visibility MagicToolOptionsVisibility => string.Equals(ActiveToolId, "magic", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string MagicToolMode
    {
        get => _magicToolMode;
        private set
        {
            if (string.Equals(_magicToolMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _magicToolMode = value;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double MagicTolerance
    {
        get => _magicTolerance;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 765);
            if (Math.Abs(_magicTolerance - clamped) < 0.01)
            {
                return;
            }

            _magicTolerance = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double MagicSampleRange
    {
        get => _magicSampleRange;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 1, 101);
            if (((int)clamped % 2) == 0)
            {
                clamped = Math.Min(101, clamped + 1);
            }

            if (Math.Abs(_magicSampleRange - clamped) < 0.01)
            {
                return;
            }

            _magicSampleRange = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public string MagicSelectionInfoText
    {
        get => _magicSelectionInfoText;
        private set
        {
            _magicSelectionInfoText = value;
            OnPropertyChanged();
        }
    }

    public ImageSource? MagicSelectionOverlayImage
    {
        get => _magicSelectionOverlayImage;
        private set
        {
            _magicSelectionOverlayImage = value;
            OnPropertyChanged();
        }
    }

    public Visibility MagicSelectionVisibility
    {
        get => _magicSelectionVisibility;
        private set
        {
            _magicSelectionVisibility = value;
            OnPropertyChanged();
        }
    }

    private void MagicModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        MagicToolMode = mode;
        UpdateMagicModeSelection();
    }

    private void UpdateMagicModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetMagicModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, MagicToolMode, StringComparison.OrdinalIgnoreCase);
            button.Background = isActive
                ? (System.Windows.Media.Brush)FindResource("PanelSelectedBg")
                : (System.Windows.Media.Brush)FindResource("SurfacePrimary");
            button.BorderBrush = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("MenuBorder");
            button.Foreground = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("TextMain");
        }
    }

    private IEnumerable<System.Windows.Controls.Button> GetMagicModeButtons()
    {
        yield return MagicWandModeButton;
        yield return QuickSelectModeButton;
    }

    private void MagicClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearMagicSelection();
    }

    private bool CanUseMagicPreview()
    {
        return string.Equals(ActiveToolId, "magic", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void ApplyMagicSelectAtPreviewPoint(System.Windows.Point previewPoint, bool addToSelection)
    {
        if (SelectedPhoto is not PhotoItem photo ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            ClearMagicSelection();
            return;
        }

        BitmapSource displaySource = photo.Image as BitmapSource ?? photo.BaseImage;

        double relativeX = (previewPoint.X - PreviewImageLeft) / PreviewImageWidth;
        double relativeY = (previewPoint.Y - PreviewImageTop) / PreviewImageHeight;
        if (relativeX < 0 || relativeX > 1 || relativeY < 0 || relativeY > 1)
        {
            return;
        }

        BitmapSource source = displaySource.Format == PixelFormats.Bgra32
            ? displaySource
            : new FormatConvertedBitmap(displaySource, PixelFormats.Bgra32, null, 0);

        double proxyScale = Math.Min(1.0, 1024.0 / Math.Max(source.PixelWidth, source.PixelHeight));
        BitmapSource proxySource = source;
        if (proxyScale < 0.999)
        {
            proxySource = new TransformedBitmap(source, new ScaleTransform(proxyScale, proxyScale));
        }

        proxySource = proxySource.Format == PixelFormats.Bgra32
            ? proxySource
            : new FormatConvertedBitmap(proxySource, PixelFormats.Bgra32, null, 0);

        int width = proxySource.PixelWidth;
        int height = proxySource.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            ClearMagicSelection();
            return;
        }

        int seedX = Math.Clamp((int)Math.Floor(relativeX * width), 0, width - 1);
        int seedY = Math.Clamp((int)Math.Floor(relativeY * height), 0, height - 1);

        int requestedRange = Math.Max(1, (int)Math.Round(MagicSampleRange));
        if ((requestedRange & 1) == 0)
        {
            requestedRange += 1;
        }

        int proxyRange = Math.Max(1, (int)Math.Round(requestedRange * proxyScale));
        if (((proxyRange & 1) == 0))
        {
            proxyRange += 1;
        }

        if (string.Equals(MagicToolMode, "quickselect", StringComparison.OrdinalIgnoreCase))
        {
            proxyRange = Math.Min(101, proxyRange + 4);
        }

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        proxySource.CopyPixels(pixels, stride, 0);

        int half = proxyRange / 2;
        int sampleLeft = Math.Clamp(seedX - half, 0, width - 1);
        int sampleTop = Math.Clamp(seedY - half, 0, height - 1);
        int sampleRight = Math.Clamp(sampleLeft + proxyRange - 1, 0, width - 1);
        int sampleBottom = Math.Clamp(sampleTop + proxyRange - 1, 0, height - 1);
        sampleLeft = Math.Min(sampleLeft, sampleRight);
        sampleTop = Math.Min(sampleTop, sampleBottom);

        long sumB = 0;
        long sumG = 0;
        long sumR = 0;
        int sampleCount = 0;
        for (int y = sampleTop; y <= sampleBottom; y++)
        {
            int rowOffset = y * stride;
            for (int x = sampleLeft; x <= sampleRight; x++)
            {
                int offset = rowOffset + (x * 4);
                sumB += pixels[offset];
                sumG += pixels[offset + 1];
                sumR += pixels[offset + 2];
                sampleCount++;
            }
        }

        if (sampleCount <= 0)
        {
            ClearMagicSelection();
            return;
        }

        int seedR = (int)Math.Round(sumR / (double)sampleCount);
        int seedG = (int)Math.Round(sumG / (double)sampleCount);
        int seedB = (int)Math.Round(sumB / (double)sampleCount);
        int tolerance = (int)Math.Round(MagicTolerance);
        if (string.Equals(MagicToolMode, "quickselect", StringComparison.OrdinalIgnoreCase))
        {
            tolerance = Math.Min(765, tolerance + 40);
        }

        bool[] visited = new bool[width * height];
        bool[] selected = new bool[width * height];
        Queue<int> queue = new();
        int seedIndex = (seedY * width) + seedX;
        queue.Enqueue(seedIndex);
        visited[seedIndex] = true;

        int selectedCount = 0;
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int pixelOffset = index * 4;
            int delta =
                Math.Abs(pixels[pixelOffset + 2] - seedR) +
                Math.Abs(pixels[pixelOffset + 1] - seedG) +
                Math.Abs(pixels[pixelOffset] - seedB);
            if (delta > tolerance)
            {
                continue;
            }

            selected[index] = true;
            selectedCount++;

            int x = index % width;
            int y = index / width;

            if (x > 0)
            {
                int neighbor = index - 1;
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (x < width - 1)
            {
                int neighbor = index + 1;
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (y > 0)
            {
                int neighbor = index - width;
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }

            if (y < height - 1)
            {
                int neighbor = index + width;
                if (!visited[neighbor])
                {
                    visited[neighbor] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (selectedCount <= 0)
        {
            if (!addToSelection || !HasReusableMagicSelection(width, height))
            {
                ClearMagicSelection();
            }

            return;
        }

        int finalSelectedCount = selectedCount;
        if (addToSelection && HasReusableMagicSelection(width, height) && _magicSelectionMask is not null)
        {
            for (int index = 0; index < selected.Length; index++)
            {
                if (_magicSelectionMask[index] && !selected[index])
                {
                    selected[index] = true;
                    finalSelectedCount++;
                }
            }
        }

        byte[] overlayPixels = new byte[pixels.Length];
        for (int index = 0; index < selected.Length; index++)
        {
            if (!selected[index])
            {
                continue;
            }

            int x = index % width;
            int y = index / width;
            bool isEdge = x == 0 || x == width - 1 || y == 0 || y == height - 1;
            if (!isEdge)
            {
                isEdge =
                    !selected[index - 1] ||
                    !selected[index + 1] ||
                    !selected[index - width] ||
                    !selected[index + width];
            }

            int pixelOffset = index * 4;
            if (isEdge)
            {
                overlayPixels[pixelOffset] = 90;
                overlayPixels[pixelOffset + 1] = 211;
                overlayPixels[pixelOffset + 2] = 241;
                overlayPixels[pixelOffset + 3] = 210;
            }
            else
            {
                overlayPixels[pixelOffset] = 90;
                overlayPixels[pixelOffset + 1] = 170;
                overlayPixels[pixelOffset + 2] = 255;
                overlayPixels[pixelOffset + 3] = 72;
            }
        }

        WriteableBitmap overlayBitmap = new(width, height, 96, 96, PixelFormats.Bgra32, null);
        overlayBitmap.WritePixels(new Int32Rect(0, 0, width, height), overlayPixels, stride, 0);
        overlayBitmap.Freeze();

        MagicSelectionOverlayImage = overlayBitmap;
        _magicSelectionMask = selected;
        _magicSelectionMaskWidth = width;
        _magicSelectionMaskHeight = height;
        _magicSelectionCount = finalSelectedCount;
        string modeLabel = string.Equals(MagicToolMode, "quickselect", StringComparison.OrdinalIgnoreCase)
            ? "Quick"
            : "Wand";
        string addLabel = addToSelection && finalSelectedCount > selectedCount
            ? $"  Added {selectedCount:N0} px"
            : string.Empty;
        MagicSelectionInfoText = $"{modeLabel}  Selected {finalSelectedCount:N0} px{addLabel}  Seed #{seedR:X2}{seedG:X2}{seedB:X2}  Tol {tolerance}  Proxy {width}x{height}";
        UpdateMagicSelectionVisibility();
    }

    private bool HasReusableMagicSelection(int width, int height)
    {
        return _magicSelectionMask is not null &&
               _magicSelectionMaskWidth == width &&
               _magicSelectionMaskHeight == height &&
               _magicSelectionCount > 0;
    }

    private void ClearMagicSelection()
    {
        MagicSelectionOverlayImage = null;
        _magicSelectionMask = null;
        _magicSelectionMaskWidth = 0;
        _magicSelectionMaskHeight = 0;
        _magicSelectionCount = 0;
        MagicSelectionInfoText = "No selection";
        UpdateMagicSelectionVisibility();
    }

    private void UpdateMagicSelectionVisibility()
    {
        MagicSelectionVisibility = CanUseMagicPreview() && MagicSelectionOverlayImage is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
