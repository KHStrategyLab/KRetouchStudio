using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility LiquifyToolOptionsVisibility => string.Equals(ActiveToolId, "liquify", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double LiquifySize
    {
        get => _liquifySize;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 4, 512);
            if (Math.Abs(_liquifySize - clamped) < 0.01)
            {
                return;
            }

            _liquifySize = clamped;
            LiquifyCircleSize = clamped;
            OnPropertyChanged();
        }
    }

    public double LiquifySoftness
    {
        get => _liquifySoftness;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_liquifySoftness - clamped) < 0.01)
            {
                return;
            }

            _liquifySoftness = clamped;
            OnPropertyChanged();
        }
    }

    public double LiquifyStrength
    {
        get => _liquifyStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 1, 100);
            if (Math.Abs(_liquifyStrength - clamped) < 0.01)
            {
                return;
            }

            _liquifyStrength = clamped;
            OnPropertyChanged();
        }
    }

    public bool ShowLiquifyCircle
    {
        get => _showLiquifyCircle;
        set
        {
            if (_showLiquifyCircle == value)
            {
                return;
            }

            _showLiquifyCircle = value;
            OnPropertyChanged();
            UpdateLiquifyCircleVisibility();
        }
    }

    public double LiquifyCircleLeft
    {
        get => _liquifyCircleLeft;
        private set
        {
            _liquifyCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double LiquifyCircleTop
    {
        get => _liquifyCircleTop;
        private set
        {
            _liquifyCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double LiquifyCircleSize
    {
        get => _liquifyCircleSize;
        private set
        {
            _liquifyCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility LiquifyCircleVisibility
    {
        get => _liquifyCircleVisibility;
        private set
        {
            _liquifyCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    public string LiquifyStatusText
    {
        get => _liquifyStatusText;
        private set
        {
            _liquifyStatusText = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseLiquifyPreview()
    {
        return string.Equals(ActiveToolId, "liquify", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void UpdateLiquifyCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, LiquifySize);
        LiquifyCircleSize = size;
        LiquifyCircleLeft = center.X - (size * 0.5);
        LiquifyCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateLiquifyCircleVisibility();
    }

    private void UpdateLiquifyCircleVisibility()
    {
        LiquifyCircleVisibility = CanUseLiquifyPreview() && ShowLiquifyCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void StartLiquifyStroke(System.Windows.Point previewPoint)
    {
        if (!EnsureLiquifyWorkingBitmap() ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        _isLiquifyDragging = true;
        _liquifyLastImagePoint = new System.Windows.Point(pixelX, pixelY);
        LiquifyStatusText = $"Warping  X:{pixelX} Y:{pixelY}";
        UpdateLiquifyCircle(previewPoint);
        Mouse.Capture(PreviewSurface);
    }

    private void ContinueLiquifyStroke(System.Windows.Point previewPoint)
    {
        if (!_isLiquifyDragging ||
            _liquifyWorkingBitmap is null ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        System.Windows.Point currentImagePoint = new(pixelX, pixelY);
        ApplyLiquifyStrokeSegment(_liquifyLastImagePoint, currentImagePoint);
        _liquifyLastImagePoint = currentImagePoint;
        LiquifyStatusText = $"Warping  X:{pixelX} Y:{pixelY}";
        UpdateLiquifyCircle(previewPoint);
    }

    private void StopLiquifyStroke()
    {
        if (!_isLiquifyDragging)
        {
            return;
        }

        _isLiquifyDragging = false;
        Mouse.Capture(null);
        LiquifyStatusText = _liquifyWorkingBitmap is null ? "Ready" : "Liquify applied";
        if (CanUseLiquifyPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        }

        if (_liquifyWorkingBitmap is not null)
        {
            PushEditorHistorySnapshot("Liquify", LiquifyStatusText);
        }
    }

    private bool EnsureLiquifyWorkingBitmap()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        if (ReferenceEquals(_liquifySessionPhoto, photo) && _liquifyWorkingBitmap is not null)
        {
            return true;
        }

        BitmapSource currentSource = GetCurrentDisplayBitmapSource(photo);
        _liquifySessionBaseImage = CloneBitmapSource(currentSource);
        _liquifyWorkingBitmap = new WriteableBitmap(_liquifySessionBaseImage);
        _liquifySessionPhoto = photo;
        photo.SetAdjustedImage(_liquifyWorkingBitmap);
        LiquifyStatusText = "Liquify ready";
        return true;
    }

    private void ResetLiquifyPreview()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        if (!ReferenceEquals(_liquifySessionPhoto, photo) || _liquifySessionBaseImage is null)
        {
            LiquifyStatusText = "No liquify session";
            return;
        }

        _liquifyWorkingBitmap = new WriteableBitmap(_liquifySessionBaseImage);
        photo.SetAdjustedImage(_liquifyWorkingBitmap);
        LiquifyStatusText = "Liquify reset";
        PushEditorHistorySnapshot("Liquify", LiquifyStatusText);
    }

    private void ClearLiquifySession(bool restoreImage)
    {
        Mouse.Capture(null);
        _isLiquifyDragging = false;
        if (restoreImage && _liquifySessionPhoto is not null && _liquifySessionBaseImage is not null)
        {
            _liquifySessionPhoto.SetAdjustedImage(_liquifySessionBaseImage);
        }

        _liquifySessionPhoto = null;
        _liquifySessionBaseImage = null;
        _liquifyWorkingBitmap = null;
        LiquifyStatusText = "Ready";
        LiquifyCircleVisibility = Visibility.Collapsed;
    }

    private static BitmapSource GetCurrentDisplayBitmapSource(PhotoItem photo)
    {
        return photo.Image as BitmapSource ?? photo.BaseImage;
    }

    private static BitmapSource CloneBitmapSource(BitmapSource source)
    {
        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        WriteableBitmap clone = new(bgraSource);
        clone.Freeze();
        return clone;
    }

    private void ApplyLiquifyStrokeSegment(System.Windows.Point fromImagePoint, System.Windows.Point toImagePoint)
    {
        if (_liquifyWorkingBitmap is null)
        {
            return;
        }

        double dx = toImagePoint.X - fromImagePoint.X;
        double dy = toImagePoint.Y - fromImagePoint.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance < 0.01)
        {
            return;
        }

        double radius = Math.Max(2.0, LiquifySize * 0.5);
        double stepSpacing = Math.Max(1.0, radius * 0.22);
        int steps = Math.Max(1, (int)Math.Ceiling(distance / stepSpacing));

        for (int i = 1; i <= steps; i++)
        {
            double previousT = (i - 1) / (double)steps;
            double currentT = i / (double)steps;
            double previousX = fromImagePoint.X + (dx * previousT);
            double previousY = fromImagePoint.Y + (dy * previousT);
            double currentX = fromImagePoint.X + (dx * currentT);
            double currentY = fromImagePoint.Y + (dy * currentT);
            ApplyLiquifyWarpDab(_liquifyWorkingBitmap, currentX, currentY, currentX - previousX, currentY - previousY);
        }
    }

    private void ApplyLiquifyWarpDab(WriteableBitmap target, double centerX, double centerY, double dragX, double dragY)
    {
        double strengthScale = LiquifyStrength / 100.0;
        double appliedDx = dragX * strengthScale;
        double appliedDy = dragY * strengthScale;
        if (Math.Abs(appliedDx) < 0.01 && Math.Abs(appliedDy) < 0.01)
        {
            return;
        }

        double radius = Math.Max(2.0, LiquifySize * 0.5);
        double softness = Math.Clamp(LiquifySoftness / 100.0, 0.0, 1.0);
        double innerRadius = radius * (1.0 - softness);
        double margin = Math.Max(Math.Abs(appliedDx), Math.Abs(appliedDy)) + 2.0;

        int left = Math.Max(0, (int)Math.Floor(centerX - radius - margin));
        int top = Math.Max(0, (int)Math.Floor(centerY - radius - margin));
        int right = Math.Min(target.PixelWidth - 1, (int)Math.Ceiling(centerX + radius + margin));
        int bottom = Math.Min(target.PixelHeight - 1, (int)Math.Ceiling(centerY + radius + margin));
        if (right < left || bottom < top)
        {
            return;
        }

        Int32Rect roi = new(left, top, right - left + 1, bottom - top + 1);
        int stride = roi.Width * 4;
        byte[] sourcePixels = new byte[stride * roi.Height];
        target.CopyPixels(roi, sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double localCenterX = centerX - left;
        double localCenterY = centerY - top;

        for (int y = 0; y < roi.Height; y++)
        {
            for (int x = 0; x < roi.Width; x++)
            {
                double dx = x - localCenterX;
                double dy = y - localCenterY;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance > radius)
                {
                    continue;
                }

                double weight;
                if (distance <= innerRadius || radius <= innerRadius + 0.001)
                {
                    weight = 1.0;
                }
                else
                {
                    double edgeT = (distance - innerRadius) / (radius - innerRadius);
                    weight = 1.0 - SmoothStep01(edgeT);
                }

                double sampleX = x - (appliedDx * weight);
                double sampleY = y - (appliedDy * weight);
                SampleBilinearBgra32(sourcePixels, roi.Width, roi.Height, stride, sampleX, sampleY, out byte b, out byte g, out byte r, out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        target.WritePixels(roi, resultPixels, stride, 0);
    }

    private static void SampleBilinearBgra32(byte[] pixels, int width, int height, int stride, double x, double y, out byte b, out byte g, out byte r, out byte a)
    {
        double clampedX = Math.Clamp(x, 0, Math.Max(0, width - 1));
        double clampedY = Math.Clamp(y, 0, Math.Max(0, height - 1));

        int x0 = (int)Math.Floor(clampedX);
        int y0 = (int)Math.Floor(clampedY);
        int x1 = Math.Min(width - 1, x0 + 1);
        int y1 = Math.Min(height - 1, y0 + 1);
        double fx = clampedX - x0;
        double fy = clampedY - y0;

        int o00 = (y0 * stride) + (x0 * 4);
        int o10 = (y0 * stride) + (x1 * 4);
        int o01 = (y1 * stride) + (x0 * 4);
        int o11 = (y1 * stride) + (x1 * 4);

        double w00 = (1.0 - fx) * (1.0 - fy);
        double w10 = fx * (1.0 - fy);
        double w01 = (1.0 - fx) * fy;
        double w11 = fx * fy;

        b = (byte)Math.Clamp((int)Math.Round(
            (pixels[o00] * w00) +
            (pixels[o10] * w10) +
            (pixels[o01] * w01) +
            (pixels[o11] * w11)), 0, 255);
        g = (byte)Math.Clamp((int)Math.Round(
            (pixels[o00 + 1] * w00) +
            (pixels[o10 + 1] * w10) +
            (pixels[o01 + 1] * w01) +
            (pixels[o11 + 1] * w11)), 0, 255);
        r = (byte)Math.Clamp((int)Math.Round(
            (pixels[o00 + 2] * w00) +
            (pixels[o10 + 2] * w10) +
            (pixels[o01 + 2] * w01) +
            (pixels[o11 + 2] * w11)), 0, 255);
        a = (byte)Math.Clamp((int)Math.Round(
            (pixels[o00 + 3] * w00) +
            (pixels[o10 + 3] * w10) +
            (pixels[o01 + 3] * w01) +
            (pixels[o11 + 3] * w11)), 0, 255);
    }

    private static double SmoothStep01(double t)
    {
        double clamped = Math.Clamp(t, 0.0, 1.0);
        return clamped * clamped * (3.0 - (2.0 * clamped));
    }
}
