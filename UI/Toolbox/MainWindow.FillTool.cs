using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;
using MediaSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const int FillFloodTolerance = 60;
    private string _fillToolMode = "bucket";
    private double _fillToolOpacity = 100;
    private string _fillToolStatusText = "Bucket ready";
    private bool _isFillGradientDragging;
    private System.Windows.Point _fillGradientStartPoint;

    public Visibility FillToolOptionsVisibility => string.Equals(ActiveToolId, "fill", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string FillToolMode
    {
        get => _fillToolMode;
        private set
        {
            if (string.Equals(_fillToolMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _fillToolMode = value;
            OnPropertyChanged();
        }
    }

    public double FillToolOpacity
    {
        get => _fillToolOpacity;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_fillToolOpacity - clamped) < 0.01)
            {
                return;
            }

            _fillToolOpacity = clamped;
            OnPropertyChanged();
        }
    }

    public string FillToolStatusText
    {
        get => _fillToolStatusText;
        private set
        {
            _fillToolStatusText = value;
            OnPropertyChanged();
        }
    }

    private void FillModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        FillToolMode = mode;
        FillToolStatusText = string.Equals(mode, "gradient", StringComparison.OrdinalIgnoreCase)
            ? "Gradient ready"
            : "Bucket ready";
        UpdateFillModeSelection();
    }

    private void UpdateFillModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetFillModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, FillToolMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetFillModeButtons()
    {
        yield return BucketFillModeButton;
        yield return GradientFillModeButton;
    }

    private bool CanUseFillPreview()
    {
        return string.Equals(ActiveToolId, "fill", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartFillToolAtPreviewPoint(System.Windows.Point previewPoint)
    {
        if (string.Equals(FillToolMode, "gradient", StringComparison.OrdinalIgnoreCase))
        {
            if (!CanUseFillPreview() || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
            {
                return;
            }

            _isFillGradientDragging = true;
            _fillGradientStartPoint = imagePoint;
            FillToolStatusText = $"Gradient start  X:{imagePoint.X:0} Y:{imagePoint.Y:0}";
            System.Windows.Input.Mouse.Capture(PreviewSurface);
            return;
        }

        ApplyFillToolAtPreviewPoint(previewPoint);
    }

    private void ContinueFillGradient(System.Windows.Point previewPoint)
    {
        if (!_isFillGradientDragging || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        FillToolStatusText = $"Gradient drag  X:{imagePoint.X:0} Y:{imagePoint.Y:0}";
    }

    private void StopFillGradient()
    {
        if (!_isFillGradientDragging)
        {
            return;
        }

        _isFillGradientDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        if (!TryPreviewPointToImagePoint(System.Windows.Input.Mouse.GetPosition(PreviewSurface), out System.Windows.Point endPoint))
        {
            FillToolStatusText = "Gradient cancelled";
            return;
        }

        ApplyGradientFill(_fillGradientStartPoint, endPoint);
    }

    private void ApplyFillToolAtPreviewPoint(System.Windows.Point previewPoint)
    {
        if (!CanUseFillPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        if (FillToolOpacity <= 0)
        {
            FillToolStatusText = "Opacity 0%";
            return;
        }

        if (string.Equals(FillToolMode, "gradient", StringComparison.OrdinalIgnoreCase))
        {
            FillToolStatusText = $"Gradient ready  X:{pixelX} Y:{pixelY}";
            return;
        }

        BitmapSource currentSource = GetCurrentDisplayBitmapSource(photo);
        WriteableBitmap workingBitmap = new(CloneBitmapSource(currentSource));
        int width = workingBitmap.PixelWidth;
        int height = workingBitmap.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            FillToolStatusText = "No image";
            return;
        }

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        workingBitmap.CopyPixels(pixels, stride, 0);

        MediaColor fillColor = GetFillToolColor();
        double opacity = FillToolOpacity / 100.0;
        int affectedPixels;
        string targetLabel;
        if (!TryApplySelectionFill(pixels, width, height, stride, fillColor, opacity, out affectedPixels, out targetLabel))
        {
            affectedPixels = ApplyFloodBucketFill(pixels, width, height, stride, pixelX, pixelY, fillColor, opacity, FillFloodTolerance);
            targetLabel = "Flood";
        }

        if (affectedPixels <= 0)
        {
            FillToolStatusText = "No fill target";
            return;
        }

        workingBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        photo.SetAdjustedImage(workingBitmap);

        string modeLabel = string.Equals(FillToolMode, "gradient", StringComparison.OrdinalIgnoreCase)
            ? "Gradient"
            : "Bucket";
        FillToolStatusText = $"{modeLabel} {targetLabel}  {affectedPixels:N0} px  {FillToolOpacity:0}%";
        PushEditorHistorySnapshot("Fill", FillToolStatusText);
    }

    private void ApplyGradientFill(System.Windows.Point startPoint, System.Windows.Point endPoint)
    {
        if (!CanUseFillPreview() || SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        double dx = endPoint.X - startPoint.X;
        double dy = endPoint.Y - startPoint.Y;
        double lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared < 4)
        {
            FillToolStatusText = "Gradient too short";
            return;
        }

        BitmapSource currentSource = GetCurrentDisplayBitmapSource(photo);
        WriteableBitmap workingBitmap = new(CloneBitmapSource(currentSource));
        int width = workingBitmap.PixelWidth;
        int height = workingBitmap.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            FillToolStatusText = "No image";
            return;
        }

        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        workingBitmap.CopyPixels(pixels, stride, 0);

        MediaColor fillColor = GetFillToolColor();
        double opacity = FillToolOpacity / 100.0;
        int affectedPixels = 0;
        for (int y = 0; y < height; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < width; x++)
            {
                double px = x + 0.5 - startPoint.X;
                double py = y + 0.5 - startPoint.Y;
                double t = Math.Clamp(((px * dx) + (py * dy)) / lengthSquared, 0.0, 1.0);
                double pixelOpacity = t * opacity;
                if (pixelOpacity <= 0.001)
                {
                    continue;
                }

                BlendPixel(pixels, rowOffset + x * 4, fillColor, pixelOpacity);
                affectedPixels++;
            }
        }

        if (affectedPixels <= 0)
        {
            FillToolStatusText = "No gradient target";
            return;
        }

        workingBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        photo.SetAdjustedImage(workingBitmap);
        FillToolStatusText = $"Gradient Full  {affectedPixels:N0} px  {FillToolOpacity:0}%";
        PushEditorHistorySnapshot("Fill", FillToolStatusText);
    }

    private MediaColor GetFillToolColor()
    {
        if (BrushColorPreview is MediaSolidColorBrush brush)
        {
            return brush.Color;
        }

        return MediaColors.White;
    }

    private bool TryApplySelectionFill(
        byte[] pixels,
        int width,
        int height,
        int stride,
        MediaColor fillColor,
        double opacity,
        out int affectedPixels,
        out string targetLabel)
    {
        if (TryApplyMagicSelectionFill(pixels, width, height, stride, fillColor, opacity, out affectedPixels))
        {
            targetLabel = "Magic";
            return true;
        }

        if (TryApplyLassoSelectionFill(pixels, width, height, stride, fillColor, opacity, out affectedPixels))
        {
            targetLabel = "Lasso";
            return true;
        }

        if (TryApplyRectangleSelectionFill(pixels, width, height, stride, fillColor, opacity, out affectedPixels))
        {
            targetLabel = RectangleShapeMode switch
            {
                "ellipse" => "Ellipse",
                "polygon" => "Polygon",
                _ => "Rect"
            };
            return true;
        }

        targetLabel = string.Empty;
        affectedPixels = 0;
        return false;
    }

    private bool TryApplyMagicSelectionFill(
        byte[] pixels,
        int width,
        int height,
        int stride,
        MediaColor fillColor,
        double opacity,
        out int affectedPixels)
    {
        affectedPixels = 0;
        if (MagicSelectionOverlayImage is not BitmapSource overlaySource)
        {
            return false;
        }

        BitmapSource overlay = overlaySource.Format == PixelFormats.Bgra32
            ? overlaySource
            : new FormatConvertedBitmap(overlaySource, PixelFormats.Bgra32, null, 0);
        if (overlay.PixelWidth <= 0 || overlay.PixelHeight <= 0)
        {
            return false;
        }

        int overlayStride = overlay.PixelWidth * 4;
        byte[] overlayPixels = new byte[overlayStride * overlay.PixelHeight];
        overlay.CopyPixels(overlayPixels, overlayStride, 0);

        for (int y = 0; y < height; y++)
        {
            int overlayY = Math.Clamp((int)Math.Floor(((y + 0.5) * overlay.PixelHeight) / height), 0, overlay.PixelHeight - 1);
            int rowOffset = y * stride;
            int overlayRowOffset = overlayY * overlayStride;

            for (int x = 0; x < width; x++)
            {
                int overlayX = Math.Clamp((int)Math.Floor(((x + 0.5) * overlay.PixelWidth) / width), 0, overlay.PixelWidth - 1);
                int overlayOffset = overlayRowOffset + (overlayX * 4);
                byte overlayAlpha = overlayPixels[overlayOffset + 3];
                if (overlayAlpha <= 16)
                {
                    continue;
                }

                int pixelOffset = rowOffset + (x * 4);
                double pixelOpacity = opacity * (overlayAlpha / 255.0);
                BlendPixel(pixels, pixelOffset, fillColor, pixelOpacity);
                affectedPixels++;
            }
        }

        return affectedPixels > 0;
    }

    private bool TryApplyLassoSelectionFill(
        byte[] pixels,
        int width,
        int height,
        int stride,
        MediaColor fillColor,
        double opacity,
        out int affectedPixels)
    {
        affectedPixels = 0;
        if (!LassoToolClosed || _lassoToolPoints.Count < 3)
        {
            return false;
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (System.Windows.Point point in _lassoToolPoints)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
        }

        int left = Math.Clamp((int)Math.Floor(minX), 0, width - 1);
        int top = Math.Clamp((int)Math.Floor(minY), 0, height - 1);
        int right = Math.Clamp((int)Math.Ceiling(maxX), 0, width - 1);
        int bottom = Math.Clamp((int)Math.Ceiling(maxY), 0, height - 1);
        if (right < left || bottom < top)
        {
            return false;
        }

        for (int y = top; y <= bottom; y++)
        {
            int rowOffset = y * stride;
            double sampleY = y + 0.5;
            for (int x = left; x <= right; x++)
            {
                if (!IsPointInsidePolygon(x + 0.5, sampleY, _lassoToolPoints))
                {
                    continue;
                }

                int pixelOffset = rowOffset + (x * 4);
                BlendPixel(pixels, pixelOffset, fillColor, opacity);
                affectedPixels++;
            }
        }

        return affectedPixels > 0;
    }

    private bool TryApplyRectangleSelectionFill(
        byte[] pixels,
        int width,
        int height,
        int stride,
        MediaColor fillColor,
        double opacity,
        out int affectedPixels)
    {
        affectedPixels = 0;
        if (RectangleSelectionVisibility != Visibility.Visible ||
            RectangleSelectionImageWidth <= 0 ||
            RectangleSelectionImageHeight <= 0)
        {
            return false;
        }

        int left = Math.Clamp((int)Math.Floor(RectangleSelectionImageX), 0, width - 1);
        int top = Math.Clamp((int)Math.Floor(RectangleSelectionImageY), 0, height - 1);
        int right = Math.Clamp((int)Math.Ceiling(RectangleSelectionImageX + RectangleSelectionImageWidth) - 1, 0, width - 1);
        int bottom = Math.Clamp((int)Math.Ceiling(RectangleSelectionImageY + RectangleSelectionImageHeight) - 1, 0, height - 1);
        if (right < left || bottom < top)
        {
            return false;
        }

        if (string.Equals(RectangleShapeMode, "rectangle", StringComparison.OrdinalIgnoreCase))
        {
            for (int y = top; y <= bottom; y++)
            {
                int rowOffset = y * stride;
                for (int x = left; x <= right; x++)
                {
                    int pixelOffset = rowOffset + (x * 4);
                    BlendPixel(pixels, pixelOffset, fillColor, opacity);
                    affectedPixels++;
                }
            }

            return affectedPixels > 0;
        }

        if (string.Equals(RectangleShapeMode, "ellipse", StringComparison.OrdinalIgnoreCase))
        {
            double centerX = RectangleSelectionImageX + (RectangleSelectionImageWidth * 0.5);
            double centerY = RectangleSelectionImageY + (RectangleSelectionImageHeight * 0.5);
            double radiusX = Math.Max(0.5, RectangleSelectionImageWidth * 0.5);
            double radiusY = Math.Max(0.5, RectangleSelectionImageHeight * 0.5);

            for (int y = top; y <= bottom; y++)
            {
                int rowOffset = y * stride;
                for (int x = left; x <= right; x++)
                {
                    double dx = ((x + 0.5) - centerX) / radiusX;
                    double dy = ((y + 0.5) - centerY) / radiusY;
                    if ((dx * dx) + (dy * dy) > 1.0)
                    {
                        continue;
                    }

                    int pixelOffset = rowOffset + (x * 4);
                    BlendPixel(pixels, pixelOffset, fillColor, opacity);
                    affectedPixels++;
                }
            }

            return affectedPixels > 0;
        }

        IReadOnlyList<System.Windows.Point> polygonPoints = GetRectangleSelectionImagePolygonPoints();
        if (polygonPoints.Count < 3)
        {
            return false;
        }

        for (int y = top; y <= bottom; y++)
        {
            int rowOffset = y * stride;
            double sampleY = y + 0.5;
            for (int x = left; x <= right; x++)
            {
                if (!IsPointInsidePolygon(x + 0.5, sampleY, polygonPoints))
                {
                    continue;
                }

                int pixelOffset = rowOffset + (x * 4);
                BlendPixel(pixels, pixelOffset, fillColor, opacity);
                affectedPixels++;
            }
        }

        return affectedPixels > 0;
    }

    private int ApplyFloodBucketFill(
        byte[] pixels,
        int width,
        int height,
        int stride,
        int seedX,
        int seedY,
        MediaColor fillColor,
        double opacity,
        int tolerance)
    {
        byte[] sourcePixels = (byte[])pixels.Clone();
        int seedOffset = (seedY * stride) + (seedX * 4);
        int seedB = sourcePixels[seedOffset];
        int seedG = sourcePixels[seedOffset + 1];
        int seedR = sourcePixels[seedOffset + 2];

        bool[] visited = new bool[width * height];
        Queue<int> queue = new();
        int seedIndex = (seedY * width) + seedX;
        queue.Enqueue(seedIndex);
        visited[seedIndex] = true;

        int affectedPixels = 0;
        while (queue.Count > 0)
        {
            int index = queue.Dequeue();
            int x = index % width;
            int y = index / width;
            int pixelOffset = (y * stride) + (x * 4);
            int delta =
                Math.Abs(sourcePixels[pixelOffset + 2] - seedR) +
                Math.Abs(sourcePixels[pixelOffset + 1] - seedG) +
                Math.Abs(sourcePixels[pixelOffset] - seedB);
            if (delta > tolerance)
            {
                continue;
            }

            BlendPixel(pixels, pixelOffset, fillColor, opacity);
            affectedPixels++;

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

        return affectedPixels;
    }

    private void BlendPixel(byte[] pixels, int pixelOffset, MediaColor fillColor, double opacity)
    {
        double clampedOpacity = Math.Clamp(opacity, 0.0, 1.0);
        if (clampedOpacity <= 0.0)
        {
            return;
        }

        pixels[pixelOffset] = BlendChannel(pixels[pixelOffset], fillColor.B, clampedOpacity);
        pixels[pixelOffset + 1] = BlendChannel(pixels[pixelOffset + 1], fillColor.G, clampedOpacity);
        pixels[pixelOffset + 2] = BlendChannel(pixels[pixelOffset + 2], fillColor.R, clampedOpacity);
    }

    private static byte BlendChannel(byte original, byte target, double opacity)
    {
        return (byte)Math.Clamp((int)Math.Round((original * (1.0 - opacity)) + (target * opacity)), 0, 255);
    }

    private IReadOnlyList<System.Windows.Point> GetRectangleSelectionImagePolygonPoints()
    {
        List<System.Windows.Point> points = [];
        if (RectangleSelectionImageWidth <= 0 || RectangleSelectionImageHeight <= 0)
        {
            return points;
        }

        int sides = Math.Max(3, RectanglePolygonSides);
        double centerX = RectangleSelectionImageX + (RectangleSelectionImageWidth * 0.5);
        double centerY = RectangleSelectionImageY + (RectangleSelectionImageHeight * 0.5);
        double radiusX = RectangleSelectionImageWidth * 0.5;
        double radiusY = RectangleSelectionImageHeight * 0.5;
        double startAngle = -Math.PI / 2.0;

        for (int i = 0; i < sides; i++)
        {
            double angle = startAngle + ((Math.PI * 2.0 * i) / sides);
            points.Add(new System.Windows.Point(
                centerX + (Math.Cos(angle) * radiusX),
                centerY + (Math.Sin(angle) * radiusY)));
        }

        return points;
    }

    private static bool IsPointInsidePolygon(double x, double y, IReadOnlyList<System.Windows.Point> points)
    {
        bool isInside = false;
        int count = points.Count;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            System.Windows.Point pi = points[i];
            System.Windows.Point pj = points[j];
            bool intersects = ((pi.Y > y) != (pj.Y > y)) &&
                              (x < (((pj.X - pi.X) * (y - pi.Y)) / ((pj.Y - pi.Y) + double.Epsilon)) + pi.X);
            if (intersects)
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
}
