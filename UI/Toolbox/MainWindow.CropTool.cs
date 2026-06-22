using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPoint = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private static readonly CropPresetOption[] CropPresetOptionList =
    [
        new("free", "Free"),
        new("3x4cm", "3x4 cm", 3.0, 4.0, "cm", 300),
        new("3.5x4.5cm", "3.5x4.5 cm", 3.5, 4.5, "cm", 300),
        new("4x5cm", "4x5 cm", 4.0, 5.0, "cm", 300),
        new("5x7cm", "5x7 cm", 5.0, 7.0, "cm", 300),
        new("saved", "Saved")
    ];

    private static readonly string[] CropUnitOptionList = ["cm", "mm", "px", "inch"];

    public IReadOnlyList<CropPresetOption> CropPresetOptions => CropPresetOptionList;

    public IReadOnlyList<string> CropUnitOptions => CropUnitOptionList;

    public Visibility CropToolOptionsVisibility => string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string SelectedCropPresetKey
    {
        get => NormalizeCropPresetKey(_appConfig.CropPreset.SelectedPresetKey);
        set
        {
            string normalized = NormalizeCropPresetKey(value);
            if (string.Equals(_appConfig.CropPreset.SelectedPresetKey, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _appConfig.CropPreset.SelectedPresetKey = normalized;
            ApplyCropPresetDefinition(normalized);
            SaveAppConfig();
            OnPropertyChanged();
            RaiseCropPresetPropertyChanged();

            if (CanUseSinglePreviewTool())
            {
                EnsureCropSelectionMatchesPreset();
            }
        }
    }

    public double CropPresetWidth
    {
        get => _cropPresetWidth;
        set
        {
            double clamped = Math.Max(0.1, Math.Round(value, 2));
            if (Math.Abs(_cropPresetWidth - clamped) < 0.01)
            {
                return;
            }

            _cropPresetWidth = clamped;
            OnPropertyChanged();
        }
    }

    public double CropPresetHeight
    {
        get => _cropPresetHeight;
        set
        {
            double clamped = Math.Max(0.1, Math.Round(value, 2));
            if (Math.Abs(_cropPresetHeight - clamped) < 0.01)
            {
                return;
            }

            _cropPresetHeight = clamped;
            OnPropertyChanged();
        }
    }

    public string CropPresetUnit
    {
        get => _cropPresetUnit;
        set
        {
            string normalized = NormalizeCropUnit(value);
            if (string.Equals(_cropPresetUnit, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _cropPresetUnit = normalized;
            OnPropertyChanged();
        }
    }

    public int CropPresetDpi
    {
        get => _cropPresetDpi;
        set
        {
            int clamped = Math.Clamp(value, 1, 2400);
            if (_cropPresetDpi == clamped)
            {
                return;
            }

            _cropPresetDpi = clamped;
            OnPropertyChanged();
        }
    }

    public double CropRotationAngle
    {
        get => _cropRotationAngle;
        set
        {
            double normalized = NormalizeCropRotationAngle(value);
            if (Math.Abs(_cropRotationAngle - normalized) < 0.01)
            {
                return;
            }

            _cropRotationAngle = normalized;
            OnPropertyChanged();
            RaiseCropRotationPropertyChanged();
            RaiseRectangleSelectionHandlePropertyChanged();
            if (IsCropImageRotationActive &&
                RectangleSelectionVisibility == Visibility.Visible)
            {
                SyncRectangleSelectionImageRectFromOverlay();
            }
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public bool CropRotateImageEnabled
    {
        get => _cropRotateImageEnabled;
        set
        {
            if (_cropRotateImageEnabled == value)
            {
                return;
            }

            _cropRotateImageEnabled = value;
            OnPropertyChanged();
            RaiseCropRotationPropertyChanged();
            RaiseRectangleSelectionHandlePropertyChanged();
            if (RectangleSelectionVisibility == Visibility.Visible)
            {
                SyncRectangleSelectionImageRectFromOverlay();
            }
            else
            {
                UpdateRectangleSelectionOverlayFromImageRect();
            }
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double SinglePreviewImageRotationAngle =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        CropRotateImageEnabled
            ? CropRotationAngle
            : 0;

    public double RectangleSelectionRotationAngle =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        !CropRotateImageEnabled
            ? CropRotationAngle
            : 0;

    public string CropFaceMetricText => TryBuildCropFaceMetricText(out string text)
        ? text
        : string.Empty;

    public Visibility CropFaceMetricVisibility => string.IsNullOrWhiteSpace(CropFaceMetricText)
        ? Visibility.Collapsed
        : Visibility.Visible;

    private bool CropPresetConstrainsAspectRatio =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(SelectedCropPresetKey, "free", StringComparison.OrdinalIgnoreCase) &&
        CropPresetWidth > 0 &&
        CropPresetHeight > 0;

    private void LoadCropPresetSettingsFromConfig()
    {
        _appConfig.CropPreset ??= new CropPresetSettings();
        _appConfig.CropPreset.SelectedPresetKey = NormalizeCropPresetKey(_appConfig.CropPreset.SelectedPresetKey);

        if (_appConfig.CropPreset.SavedWidth <= 0)
        {
            _appConfig.CropPreset.SavedWidth = 3.5;
        }

        if (_appConfig.CropPreset.SavedHeight <= 0)
        {
            _appConfig.CropPreset.SavedHeight = 4.5;
        }

        if (_appConfig.CropPreset.SavedDpi <= 0)
        {
            _appConfig.CropPreset.SavedDpi = 300;
        }

        _appConfig.CropPreset.SavedUnit = NormalizeCropUnit(_appConfig.CropPreset.SavedUnit);
        ApplyCropPresetDefinition(_appConfig.CropPreset.SelectedPresetKey);
        RaiseCropPresetPropertyChanged();
    }

    private void RaiseCropPresetPropertyChanged()
    {
        OnPropertyChanged(nameof(CropPresetOptions));
        OnPropertyChanged(nameof(CropUnitOptions));
        OnPropertyChanged(nameof(SelectedCropPresetKey));
        OnPropertyChanged(nameof(CropPresetWidth));
        OnPropertyChanged(nameof(CropPresetHeight));
        OnPropertyChanged(nameof(CropPresetUnit));
        OnPropertyChanged(nameof(CropPresetDpi));
    }

    private void CropPresetSaveButton_Click(object sender, RoutedEventArgs e)
    {
        _appConfig.CropPreset.SavedWidth = CropPresetWidth;
        _appConfig.CropPreset.SavedHeight = CropPresetHeight;
        _appConfig.CropPreset.SavedUnit = NormalizeCropUnit(CropPresetUnit);
        _appConfig.CropPreset.SavedDpi = CropPresetDpi;
        _appConfig.CropPreset.SelectedPresetKey = "saved";
        SaveAppConfig();

        ApplyCropPresetDefinition("saved");
        OnPropertyChanged(nameof(SelectedCropPresetKey));
        RaiseCropPresetPropertyChanged();

        if (CanUseSinglePreviewTool())
        {
            EnsureCropSelectionMatchesPreset();
        }
    }

    private static BitmapSource GetCropSourceBitmapSource(PhotoItem photo)
    {
        return photo.BaseImage;
    }

    private bool TryApplyCropSelection()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        BitmapSource source = GetCropSourceBitmapSource(photo);
        BitmapSource? croppedClone = TryBuildCropBitmap(source, out string historyDetail);
        if (croppedClone is null)
        {
            return false;
        }

        ClearRectangleSelection();
        CropRotationAngle = 0;
        photo.SetAdjustedImage(croppedClone);
        ResetSinglePreviewToFitCanvas();
        PushEditorHistorySnapshot("Crop", historyDetail);
        return true;
    }

    private BitmapSource? TryBuildCropBitmap(BitmapSource source, out string historyDetail)
    {
        historyDetail = string.Empty;

        if (HasEffectiveCropRotation)
        {
            if (!TryBuildRotatedCropBitmap(source, out BitmapSource? rotatedCrop))
            {
                return null;
            }

            if (rotatedCrop is null)
            {
                return null;
            }

            string modeLabel = CropRotateImageEnabled
                ? "Image Rotate"
                : "Tool Rotate";
            historyDetail = $"Crop {rotatedCrop.PixelWidth} x {rotatedCrop.PixelHeight} ({modeLabel} {CropRotationAngle:0.#}°)";
            return rotatedCrop;
        }

        if (!TryBuildCropRect(source.PixelWidth, source.PixelHeight, out Int32Rect cropRect))
        {
            return null;
        }

        CroppedBitmap cropped = new(source, cropRect);
        BitmapSource croppedClone = CloneBitmapSource(cropped);
        historyDetail = $"Crop {cropRect.Width} x {cropRect.Height}";
        return croppedClone;
    }

    private bool TryBuildCropRect(int imageWidth, int imageHeight, out Int32Rect cropRect)
    {
        cropRect = default;
        if (imageWidth <= 0 || imageHeight <= 0)
        {
            return false;
        }

        int x = Math.Clamp((int)Math.Round(RectangleSelectionImageX), 0, Math.Max(0, imageWidth - 1));
        int y = Math.Clamp((int)Math.Round(RectangleSelectionImageY), 0, Math.Max(0, imageHeight - 1));
        int right = Math.Clamp((int)Math.Round(RectangleSelectionImageX + RectangleSelectionImageWidth), x + 1, imageWidth);
        int bottom = Math.Clamp((int)Math.Round(RectangleSelectionImageY + RectangleSelectionImageHeight), y + 1, imageHeight);
        int width = right - x;
        int height = bottom - y;
        if (width < 4 || height < 4)
        {
            return false;
        }

        cropRect = new Int32Rect(x, y, width, height);
        return true;
    }

    private void EnsureCropSelectionMatchesPreset()
    {
        if (!string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) ||
            SelectedPhoto is not PhotoItem photo ||
            !TryGetCropPresetPixelSize(out double widthPx, out double heightPx))
        {
            return;
        }

        BitmapSource source = GetCropSourceBitmapSource(photo);
        widthPx = Math.Clamp(widthPx, 4, source.PixelWidth);
        heightPx = Math.Clamp(heightPx, 4, source.PixelHeight);

        double centerX = source.PixelWidth * 0.5;
        double centerY = source.PixelHeight * 0.5;
        if (RectangleSelectionVisibility == Visibility.Visible &&
            RectangleSelectionImageWidth > 0 &&
            RectangleSelectionImageHeight > 0)
        {
            centerX = RectangleSelectionImageX + (RectangleSelectionImageWidth * 0.5);
            centerY = RectangleSelectionImageY + (RectangleSelectionImageHeight * 0.5);
        }

        SetCropSelectionImageRect(
            centerX - (widthPx * 0.5),
            centerY - (heightPx * 0.5),
            widthPx,
            heightPx,
            source);
    }

    private void SetCropSelectionImageRect(
        double imageX,
        double imageY,
        double imageWidth,
        double imageHeight,
        BitmapSource source)
    {
        double clampedWidth = Math.Clamp(imageWidth, 4, source.PixelWidth);
        double clampedHeight = Math.Clamp(imageHeight, 4, source.PixelHeight);
        double maxX = Math.Max(0, source.PixelWidth - clampedWidth);
        double maxY = Math.Max(0, source.PixelHeight - clampedHeight);

        _rectangleSelectionImageX = Math.Clamp(imageX, 0, maxX);
        _rectangleSelectionImageY = Math.Clamp(imageY, 0, maxY);
        _rectangleSelectionImageWidth = clampedWidth;
        _rectangleSelectionImageHeight = clampedHeight;
        OnPropertyChanged(nameof(RectangleSelectionImageX));
        OnPropertyChanged(nameof(RectangleSelectionImageY));
        OnPropertyChanged(nameof(RectangleSelectionImageWidth));
        OnPropertyChanged(nameof(RectangleSelectionImageHeight));
        UpdateRectangleSelectionOverlayFromImageRect();
        RaiseCropTelemetryPropertyChanged();
    }

    private bool TryGetCropPresetPixelSize(out double widthPx, out double heightPx)
    {
        widthPx = 0;
        heightPx = 0;

        if (string.Equals(SelectedCropPresetKey, "free", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        double dpi = Math.Max(1, CropPresetDpi);
        widthPx = ConvertCropValueToPixels(CropPresetWidth, CropPresetUnit, dpi);
        heightPx = ConvertCropValueToPixels(CropPresetHeight, CropPresetUnit, dpi);
        return widthPx >= 4 && heightPx >= 4;
    }

    private static double ConvertCropValueToPixels(double value, string unit, double dpi)
    {
        double safeValue = Math.Max(0, value);
        return NormalizeCropUnit(unit) switch
        {
            "mm" => (safeValue / 25.4) * dpi,
            "inch" => safeValue * dpi,
            "px" => safeValue,
            _ => (safeValue / 2.54) * dpi
        };
    }

    private bool TryGetCropAspectRatio(out double aspectRatio)
    {
        aspectRatio = 0;
        if (!CropPresetConstrainsAspectRatio)
        {
            return false;
        }

        aspectRatio = CropPresetWidth / CropPresetHeight;
        return !double.IsNaN(aspectRatio) &&
               !double.IsInfinity(aspectRatio) &&
               aspectRatio > 0.01;
    }

    private static string NormalizeCropPresetKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "free";
        }

        string normalized = value.Trim().ToLowerInvariant();
        return CropPresetOptionList.Any(option => string.Equals(option.Key, normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : "free";
    }

    private static string NormalizeCropUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "cm";
        }

        string normalized = value.Trim().ToLowerInvariant();
        return CropUnitOptionList.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : "cm";
    }

    private void ApplyCropPresetDefinition(string presetKey)
    {
        CropPresetOption option = CropPresetOptionList.FirstOrDefault(current => string.Equals(current.Key, presetKey, StringComparison.OrdinalIgnoreCase))
            ?? CropPresetOptionList[0];

        if (option.Width.HasValue && option.Height.HasValue && !string.IsNullOrWhiteSpace(option.Unit) && option.Dpi.HasValue)
        {
            _cropPresetWidth = option.Width.Value;
            _cropPresetHeight = option.Height.Value;
            _cropPresetUnit = NormalizeCropUnit(option.Unit);
            _cropPresetDpi = Math.Clamp(option.Dpi.Value, 1, 2400);
        }
        else if (string.Equals(option.Key, "saved", StringComparison.OrdinalIgnoreCase))
        {
            _cropPresetWidth = Math.Max(0.1, _appConfig.CropPreset.SavedWidth);
            _cropPresetHeight = Math.Max(0.1, _appConfig.CropPreset.SavedHeight);
            _cropPresetUnit = NormalizeCropUnit(_appConfig.CropPreset.SavedUnit);
            _cropPresetDpi = Math.Clamp(_appConfig.CropPreset.SavedDpi, 1, 2400);
        }

        OnPropertyChanged(nameof(CropPresetWidth));
        OnPropertyChanged(nameof(CropPresetHeight));
        OnPropertyChanged(nameof(CropPresetUnit));
        OnPropertyChanged(nameof(CropPresetDpi));
    }

    private bool IsCropToolSettingInputFocused()
    {
        return System.Windows.Input.Keyboard.FocusedElement is System.Windows.Controls.TextBox or System.Windows.Controls.ComboBox or System.Windows.Controls.ComboBoxItem;
    }

    private bool TryBuildCropFaceMetricText(out string text)
    {
        text = string.Empty;
        return false;
    }

    private void RaiseCropTelemetryPropertyChanged()
    {
        OnPropertyChanged(nameof(CropFaceMetricText));
        OnPropertyChanged(nameof(CropFaceMetricVisibility));
    }

    private bool HasEffectiveCropRotation =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        Math.Abs(CropRotationAngle) >= 0.01;

    private bool IsCropImageRotationActive =>
        HasEffectiveCropRotation &&
        CropRotateImageEnabled;

    private double NormalizeCropRotationAngle(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        double normalized = value % 360.0;
        if (normalized > 180.0)
        {
            normalized -= 360.0;
        }
        else if (normalized < -180.0)
        {
            normalized += 360.0;
        }

        return Math.Round(normalized, 1);
    }

    private void RaiseCropRotationPropertyChanged()
    {
        OnPropertyChanged(nameof(SinglePreviewImageRotationAngle));
        OnPropertyChanged(nameof(RectangleSelectionRotationAngle));
    }

    private bool TryBuildRotatedCropBitmap(BitmapSource source, out BitmapSource? croppedBitmap)
    {
        croppedBitmap = null;
        if (!TryGetCropSelectionImageMetrics(source, out double selectionLeft, out double selectionTop, out double selectionWidth, out double selectionHeight))
        {
            return false;
        }

        double centerX = selectionLeft + (selectionWidth * 0.5);
        double centerY = selectionTop + (selectionHeight * 0.5);
        double renderAngle = CropRotateImageEnabled
            ? CropRotationAngle
            : -CropRotationAngle;
        WpfPoint rotationPivot = CropRotateImageEnabled
            ? new WpfPoint(source.PixelWidth * 0.5, source.PixelHeight * 0.5)
            : new WpfPoint(centerX, centerY);
        croppedBitmap = RenderTransformedCropBitmap(
            source,
            selectionWidth,
            selectionHeight,
            centerX,
            centerY,
            renderAngle,
            rotationPivot);
        return true;
    }

    private bool TryGetCropSelectionImageMetrics(
        BitmapSource source,
        out double selectionLeft,
        out double selectionTop,
        out double selectionWidth,
        out double selectionHeight)
    {
        selectionLeft = 0;
        selectionTop = 0;
        selectionWidth = 0;
        selectionHeight = 0;

        if (RectangleSelectionVisibility != Visibility.Visible ||
            RectangleSelectionWidth < 4 ||
            RectangleSelectionHeight < 4)
        {
            return false;
        }

        selectionWidth = Math.Clamp(RectangleSelectionImageWidth, 1, source.PixelWidth);
        selectionHeight = Math.Clamp(RectangleSelectionImageHeight, 1, source.PixelHeight);
        if (selectionWidth < 1 || selectionHeight < 1)
        {
            return false;
        }

        double centerX = RectangleSelectionImageX + (RectangleSelectionImageWidth * 0.5);
        double centerY = RectangleSelectionImageY + (RectangleSelectionImageHeight * 0.5);
        double minCenterX = selectionWidth * 0.5;
        double maxCenterX = Math.Max(minCenterX, source.PixelWidth - (selectionWidth * 0.5));
        double minCenterY = selectionHeight * 0.5;
        double maxCenterY = Math.Max(minCenterY, source.PixelHeight - (selectionHeight * 0.5));
        centerX = Math.Clamp(centerX, minCenterX, maxCenterX);
        centerY = Math.Clamp(centerY, minCenterY, maxCenterY);
        selectionLeft = centerX - (selectionWidth * 0.5);
        selectionTop = centerY - (selectionHeight * 0.5);
        return true;
    }

    private BitmapSource RenderTransformedCropBitmap(
        BitmapSource source,
        double selectionWidth,
        double selectionHeight,
        double selectionCenterX,
        double selectionCenterY,
        double rotationAngle,
        WpfPoint rotationPivot)
    {
        int pixelWidth = Math.Max(1, (int)Math.Round(selectionWidth));
        int pixelHeight = Math.Max(1, (int)Math.Round(selectionHeight));
        double dpiX = source.DpiX > 0 ? source.DpiX : 96;
        double dpiY = source.DpiY > 0 ? source.DpiY : 96;
        WpfPoint transformedCenter = Math.Abs(rotationAngle) < 0.01
            ? new WpfPoint(selectionCenterX, selectionCenterY)
            : RotatePoint(new WpfPoint(selectionCenterX, selectionCenterY), rotationPivot, rotationAngle);

        DrawingVisual visual = new();
        using (DrawingContext drawingContext = visual.RenderOpen())
        {
            TransformGroup transforms = new();
            if (Math.Abs(rotationAngle) >= 0.01)
            {
                transforms.Children.Add(new RotateTransform(rotationAngle, rotationPivot.X, rotationPivot.Y));
            }

            transforms.Children.Add(new TranslateTransform(
                (pixelWidth * 0.5) - transformedCenter.X,
                (pixelHeight * 0.5) - transformedCenter.Y));
            // WPF drawing uses 96-DPI device-independent units; crop math above is in source pixels.
            transforms.Children.Add(new ScaleTransform(96.0 / dpiX, 96.0 / dpiY));
            drawingContext.PushTransform(transforms);
            drawingContext.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
            drawingContext.Pop();
        }

        RenderTargetBitmap bitmap = new(
            pixelWidth,
            pixelHeight,
            dpiX,
            dpiY,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static WpfPoint RotatePoint(WpfPoint point, WpfPoint center, double angleDegrees)
    {
        if (Math.Abs(angleDegrees) < 0.01)
        {
            return point;
        }

        double radians = angleDegrees * Math.PI / 180.0;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double dx = point.X - center.X;
        double dy = point.Y - center.Y;
        return new WpfPoint(
            center.X + (dx * cos) - (dy * sin),
            center.Y + (dx * sin) + (dy * cos));
    }

    public sealed record CropPresetOption(
        string Key,
        string Label,
        double? Width = null,
        double? Height = null,
        string? Unit = null,
        int? Dpi = null);
}
