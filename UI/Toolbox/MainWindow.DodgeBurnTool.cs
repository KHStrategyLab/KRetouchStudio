using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility DodgeBurnToolOptionsVisibility => string.Equals(ActiveToolId, "dodgeburn", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string DodgeBurnMode
    {
        get => _dodgeBurnMode;
        private set
        {
            if (string.Equals(_dodgeBurnMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _dodgeBurnMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DodgeBurnCircleStroke));
            SaveToolboxDefaults();
        }
    }

    public double DodgeBurnSize
    {
        get => _dodgeBurnSize;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 4, 512);
            if (Math.Abs(_dodgeBurnSize - clamped) < 0.01)
            {
                return;
            }

            _dodgeBurnSize = clamped;
            DodgeBurnCircleSize = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double DodgeBurnSoftness
    {
        get => _dodgeBurnSoftness;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_dodgeBurnSoftness - clamped) < 0.01)
            {
                return;
            }

            _dodgeBurnSoftness = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double DodgeBurnStrength
    {
        get => _dodgeBurnStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 1, 100);
            if (Math.Abs(_dodgeBurnStrength - clamped) < 0.01)
            {
                return;
            }

            _dodgeBurnStrength = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public bool ShowDodgeBurnCircle
    {
        get => _showDodgeBurnCircle;
        set
        {
            if (_showDodgeBurnCircle == value)
            {
                return;
            }

            _showDodgeBurnCircle = value;
            OnPropertyChanged();
            UpdateDodgeBurnCircleVisibility();
            SaveToolboxDefaults();
        }
    }

    public double DodgeBurnCircleLeft
    {
        get => _dodgeBurnCircleLeft;
        private set
        {
            _dodgeBurnCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double DodgeBurnCircleTop
    {
        get => _dodgeBurnCircleTop;
        private set
        {
            _dodgeBurnCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double DodgeBurnCircleSize
    {
        get => _dodgeBurnCircleSize;
        private set
        {
            _dodgeBurnCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility DodgeBurnCircleVisibility
    {
        get => _dodgeBurnCircleVisibility;
        private set
        {
            _dodgeBurnCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    public string DodgeBurnCircleStroke => string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
        ? "#76A9FF"
        : "#F7B267";

    public string DodgeBurnStatusText
    {
        get => _dodgeBurnStatusText;
        private set
        {
            _dodgeBurnStatusText = value;
            OnPropertyChanged();
        }
    }

    private void DodgeBurnModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        DodgeBurnMode = mode;
        UpdateDodgeBurnModeSelection();
        DodgeBurnStatusText = string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
            ? "Burn ready"
            : "Dodge ready";
    }

    private void UpdateDodgeBurnModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetDodgeBurnModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, DodgeBurnMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetDodgeBurnModeButtons()
    {
        yield return DodgeModeButton;
        yield return BurnModeButton;
    }

    private void DodgeBurnResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetDodgeBurnPreview();
    }

    private bool CanUseDodgeBurnPreview()
    {
        return string.Equals(ActiveToolId, "dodgeburn", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void UpdateDodgeBurnCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, DodgeBurnSize);
        DodgeBurnCircleSize = size;
        DodgeBurnCircleLeft = center.X - (size * 0.5);
        DodgeBurnCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateDodgeBurnCircleVisibility();
    }

    private void UpdateDodgeBurnCircleVisibility()
    {
        DodgeBurnCircleVisibility = CanUseDodgeBurnPreview() && ShowDodgeBurnCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void StartDodgeBurnStroke(System.Windows.Point previewPoint)
    {
        if (!EnsureDodgeBurnWorkingBitmap() ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        _isDodgeBurnDragging = true;
        _dodgeBurnLastImagePoint = new System.Windows.Point(pixelX, pixelY);
        ApplyDodgeBurnDab(_dodgeBurnWorkingBitmap!, pixelX, pixelY);
        DodgeBurnStatusText = $"{GetDodgeBurnActionLabel()}  X:{pixelX} Y:{pixelY}";
        UpdateDodgeBurnCircle(previewPoint);
        Mouse.Capture(PreviewSurface);
    }

    private void ContinueDodgeBurnStroke(System.Windows.Point previewPoint)
    {
        if (!_isDodgeBurnDragging ||
            _dodgeBurnWorkingBitmap is null ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        System.Windows.Point currentImagePoint = new(pixelX, pixelY);
        ApplyDodgeBurnStrokeSegment(_dodgeBurnLastImagePoint, currentImagePoint);
        _dodgeBurnLastImagePoint = currentImagePoint;
        DodgeBurnStatusText = $"{GetDodgeBurnActionLabel()}  X:{pixelX} Y:{pixelY}";
        UpdateDodgeBurnCircle(previewPoint);
    }

    private void StopDodgeBurnStroke()
    {
        if (!_isDodgeBurnDragging)
        {
            return;
        }

        _isDodgeBurnDragging = false;
        Mouse.Capture(null);
        DodgeBurnStatusText = _dodgeBurnWorkingBitmap is null
            ? "Ready"
            : $"{(string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase) ? "Burn" : "Dodge")} applied";
        if (CanUseDodgeBurnPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        }

        if (_dodgeBurnWorkingBitmap is not null)
        {
            PushEditorHistorySnapshot(
                string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase) ? "Burn" : "Dodge",
                DodgeBurnStatusText);
        }
    }

    private bool EnsureDodgeBurnWorkingBitmap()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        if (ReferenceEquals(_dodgeBurnSessionPhoto, photo) &&
            _dodgeBurnWorkingBitmap is not null &&
            ReferenceEquals(photo.Image, _dodgeBurnWorkingBitmap))
        {
            return true;
        }

        BitmapSource currentSource = GetCurrentDisplayBitmapSource(photo);
        _dodgeBurnSessionBaseImage = CloneBitmapSource(currentSource);
        _dodgeBurnWorkingBitmap = new WriteableBitmap(_dodgeBurnSessionBaseImage);
        _dodgeBurnSessionPhoto = photo;
        photo.SetAdjustedImage(_dodgeBurnWorkingBitmap);
        DodgeBurnStatusText = string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
            ? "Burn ready"
            : "Dodge ready";
        return true;
    }

    private void ResetDodgeBurnPreview()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        if (!ReferenceEquals(_dodgeBurnSessionPhoto, photo) || _dodgeBurnSessionBaseImage is null)
        {
            DodgeBurnStatusText = "No dodge / burn session";
            return;
        }

        _dodgeBurnWorkingBitmap = new WriteableBitmap(_dodgeBurnSessionBaseImage);
        photo.SetAdjustedImage(_dodgeBurnWorkingBitmap);
        DodgeBurnStatusText = string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
            ? "Burn reset"
            : "Dodge reset";
        PushEditorHistorySnapshot(
            string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase) ? "Burn" : "Dodge",
            DodgeBurnStatusText);
    }

    private void ClearDodgeBurnSession(bool restoreImage)
    {
        Mouse.Capture(null);
        _isDodgeBurnDragging = false;
        if (restoreImage && _dodgeBurnSessionPhoto is not null && _dodgeBurnSessionBaseImage is not null)
        {
            _dodgeBurnSessionPhoto.SetAdjustedImage(_dodgeBurnSessionBaseImage);
        }

        _dodgeBurnSessionPhoto = null;
        _dodgeBurnSessionBaseImage = null;
        _dodgeBurnWorkingBitmap = null;
        DodgeBurnStatusText = "Ready";
        DodgeBurnCircleVisibility = Visibility.Collapsed;
    }

    private string GetDodgeBurnActionLabel()
    {
        return string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase)
            ? "Burning"
            : "Dodging";
    }

    private void ApplyDodgeBurnStrokeSegment(System.Windows.Point fromImagePoint, System.Windows.Point toImagePoint)
    {
        if (_dodgeBurnWorkingBitmap is null)
        {
            return;
        }

        double dx = toImagePoint.X - fromImagePoint.X;
        double dy = toImagePoint.Y - fromImagePoint.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));
        if (distance < 0.01)
        {
            ApplyDodgeBurnDab(_dodgeBurnWorkingBitmap, toImagePoint.X, toImagePoint.Y);
            return;
        }

        double radius = Math.Max(2.0, DodgeBurnSize * 0.5);
        double stepSpacing = Math.Max(1.0, radius * 0.18);
        int steps = Math.Max(1, (int)Math.Ceiling(distance / stepSpacing));

        for (int i = 1; i <= steps; i++)
        {
            double currentT = i / (double)steps;
            double currentX = fromImagePoint.X + (dx * currentT);
            double currentY = fromImagePoint.Y + (dy * currentT);
            ApplyDodgeBurnDab(_dodgeBurnWorkingBitmap, currentX, currentY);
        }
    }

    private void ApplyDodgeBurnDab(WriteableBitmap target, double centerX, double centerY)
    {
        double radius = Math.Max(2.0, DodgeBurnSize * 0.5);
        double softness = Math.Clamp(DodgeBurnSoftness / 100.0, 0.0, 1.0);
        double strengthScale = Math.Clamp(DodgeBurnStrength / 100.0, 0.0, 1.0);
        double innerRadius = radius * (1.0 - softness);

        int left = Math.Max(0, (int)Math.Floor(centerX - radius - 1.0));
        int top = Math.Max(0, (int)Math.Floor(centerY - radius - 1.0));
        int right = Math.Min(target.PixelWidth - 1, (int)Math.Ceiling(centerX + radius + 1.0));
        int bottom = Math.Min(target.PixelHeight - 1, (int)Math.Ceiling(centerY + radius + 1.0));
        if (right < left || bottom < top)
        {
            return;
        }

        Int32Rect roi = new(left, top, right - left + 1, bottom - top + 1);
        int stride = roi.Width * 4;
        byte[] pixels = new byte[stride * roi.Height];
        target.CopyPixels(roi, pixels, stride, 0);

        double localCenterX = centerX - left;
        double localCenterY = centerY - top;
        bool isBurn = string.Equals(DodgeBurnMode, "burn", StringComparison.OrdinalIgnoreCase);

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

                double alpha = Math.Clamp(weight * strengthScale, 0.0, 1.0);
                if (alpha <= 0.001)
                {
                    continue;
                }

                int offset = (y * stride) + (x * 4);
                pixels[offset] = ApplyDodgeBurnChannel(pixels[offset], alpha, isBurn);
                pixels[offset + 1] = ApplyDodgeBurnChannel(pixels[offset + 1], alpha, isBurn);
                pixels[offset + 2] = ApplyDodgeBurnChannel(pixels[offset + 2], alpha, isBurn);
            }
        }

        target.WritePixels(roi, pixels, stride, 0);
    }

    private static byte ApplyDodgeBurnChannel(byte value, double alpha, bool isBurn)
    {
        double result = isBurn
            ? value * (1.0 - alpha)
            : value + ((255.0 - value) * alpha);
        return (byte)Math.Clamp((int)Math.Round(result), 0, 255);
    }
}
