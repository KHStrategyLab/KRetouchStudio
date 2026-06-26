using System;
using System.Collections.Generic;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility BlurSharpToolOptionsVisibility => string.Equals(ActiveToolId, "blursharp", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string BlurSharpMode
    {
        get => _blurSharpMode;
        private set
        {
            _blurSharpMode = value;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BlurSharpSize
    {
        get => _blurSharpSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_blurSharpSize - clamped) < 0.01)
            {
                return;
            }

            _blurSharpSize = clamped;
            BlurSharpCircleSize = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BlurSharpSoftness
    {
        get => _blurSharpSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_blurSharpSoftness - clamped) < 0.01)
            {
                return;
            }

            _blurSharpSoftness = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BlurSharpStrength
    {
        get => _blurSharpStrength;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_blurSharpStrength - clamped) < 0.01)
            {
                return;
            }

            _blurSharpStrength = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BlurSharpRadius
    {
        get => _blurSharpRadius;
        set
        {
            double clamped = Math.Clamp(value, 0.1, 100);
            if (Math.Abs(_blurSharpRadius - clamped) < 0.01)
            {
                return;
            }

            _blurSharpRadius = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public bool ShowBlurSharpCircle
    {
        get => _showBlurSharpCircle;
        set
        {
            if (_showBlurSharpCircle == value)
            {
                return;
            }

            _showBlurSharpCircle = value;
            OnPropertyChanged();
            UpdateBlurSharpCircleVisibility();
            SaveToolboxDefaults();
        }
    }

    public double BlurSharpCircleLeft
    {
        get => _blurSharpCircleLeft;
        private set
        {
            _blurSharpCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double BlurSharpCircleTop
    {
        get => _blurSharpCircleTop;
        private set
        {
            _blurSharpCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double BlurSharpCircleSize
    {
        get => _blurSharpCircleSize;
        private set
        {
            _blurSharpCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility BlurSharpCircleVisibility
    {
        get => _blurSharpCircleVisibility;
        private set
        {
            _blurSharpCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private void BlurSharpModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        BlurSharpMode = mode;
        UpdateBlurSharpModeSelection();
    }

    private void UpdateBlurSharpModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetBlurSharpModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, BlurSharpMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetBlurSharpModeButtons()
    {
        yield return BlurModeButton;
        yield return SharpenModeButton;
    }

    private bool CanUseBlurSharpPreview()
    {
        return string.Equals(ActiveToolId, "blursharp", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartBlurSharpStroke(System.Windows.Point previewPoint)
    {
        if (!CanUseBlurSharpPreview() ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isBlurSharpDragging = true;
        ApplyBlurSharpDab(target, imagePoint, BlurSharpSize, BlurSharpSoftness, BlurSharpRadius, BlurSharpStrength, IsSharpenMode);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueBlurSharpStroke(System.Windows.Point previewPoint)
    {
        if (!_isBlurSharpDragging ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyBlurSharpDab(target, imagePoint, BlurSharpSize, BlurSharpSoftness, BlurSharpRadius, BlurSharpStrength, IsSharpenMode);
    }

    private void StopBlurSharpStroke()
    {
        if (!_isBlurSharpDragging)
        {
            return;
        }

        _isBlurSharpDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        PushEditorHistorySnapshot(IsSharpenMode ? "Sharpen" : "Blur", $"{BlurSharpSize:0}px / {BlurSharpStrength:0}%");
    }

    private void UpdateBlurSharpCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, BlurSharpSize);
        BlurSharpCircleSize = size;
        BlurSharpCircleLeft = center.X - (size * 0.5);
        BlurSharpCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateBlurSharpCircleVisibility();
    }

    private void UpdateBlurSharpCircleVisibility()
    {
        BlurSharpCircleVisibility = CanUseBlurSharpPreview() && ShowBlurSharpCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool IsSharpenMode => string.Equals(BlurSharpMode, "sharpen", StringComparison.OrdinalIgnoreCase);
}
