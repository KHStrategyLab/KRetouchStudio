using System;
using System.Collections.Generic;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility HealingToolOptionsVisibility => string.Equals(ActiveToolId, "healing", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string HealingMode
    {
        get => _healingMode;
        private set
        {
            if (string.Equals(_healingMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _healingMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealingModeHintText));
            SaveToolboxDefaults();
        }
    }

    public string HealingModeHintText => HealingMode switch
    {
        "patch" => "Alt+Click source, drag target",
        "spot" => "Auto spot healing",
        _ => "Alt+Click source, drag target"
    };

    public double HealingSize
    {
        get => _healingSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_healingSize - clamped) < 0.01)
            {
                return;
            }

            _healingSize = clamped;
            HealingCircleSize = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double HealingSoftness
    {
        get => _healingSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_healingSoftness - clamped) < 0.01)
            {
                return;
            }

            _healingSoftness = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double HealingStrength
    {
        get => _healingStrength;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_healingStrength - clamped) < 0.01)
            {
                return;
            }

            _healingStrength = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public bool ShowHealingCircle
    {
        get => _showHealingCircle;
        set
        {
            if (_showHealingCircle == value)
            {
                return;
            }

            _showHealingCircle = value;
            OnPropertyChanged();
            UpdateHealingCircleVisibility();
            SaveToolboxDefaults();
        }
    }

    public double HealingCircleLeft
    {
        get => _healingCircleLeft;
        private set
        {
            _healingCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double HealingCircleTop
    {
        get => _healingCircleTop;
        private set
        {
            _healingCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double HealingCircleSize
    {
        get => _healingCircleSize;
        private set
        {
            _healingCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility HealingCircleVisibility
    {
        get => _healingCircleVisibility;
        private set
        {
            _healingCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private void HealingModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        HealingMode = mode;
        UpdateHealingModeSelection();
    }

    private void HealingResetButton_Click(object sender, RoutedEventArgs e)
    {
        HealingMode = "healing";
        HealingSize = 80;
        HealingSoftness = 50;
        HealingStrength = 50;
        ShowHealingCircle = true;
        UpdateHealingModeSelection();
        SaveToolboxDefaults();
    }

    private void UpdateHealingModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetHealingModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, HealingMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetHealingModeButtons()
    {
        yield return HealingBrushModeButton;
        yield return PatchHealingModeButton;
        yield return SpotHealingModeButton;
    }

    private bool CanUseHealingPreview()
    {
        return string.Equals(ActiveToolId, "healing", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartHealingStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (!CanUseHealingPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        bool isSpot = string.Equals(HealingMode, "spot", StringComparison.OrdinalIgnoreCase);
        bool setSource = !isSpot && ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt || !_hasHealingSource);
        if (setSource)
        {
            _hasHealingSource = true;
            _healingSourceImagePoint = imagePoint;
            _healingSourceBitmap = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isHealingDragging = true;
        _healingStrokeStartSourcePoint = _healingSourceImagePoint;
        _healingStrokeStartTargetPoint = imagePoint;
        _healingLastImagePoint = imagePoint;
        if (!isSpot && _healingSourceBitmap is not null)
        {
            BeginSourceCopyStroke(target);
        }

        ApplyHealingDab(target, imagePoint, pressure);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueHealingStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (!_isHealingDragging ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyHealingStrokeSegment(target, _healingLastImagePoint, imagePoint, pressure);
        _healingLastImagePoint = imagePoint;
    }

    private void StopHealingStroke()
    {
        if (!_isHealingDragging)
        {
            return;
        }

        _isHealingDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        EndSourceCopyStroke();
        PushEditorHistorySnapshot("Healing", $"{HealingMode} {HealingSize:0}px / {HealingStrength:0}%");
    }

    private void ApplyHealingDab(System.Windows.Media.Imaging.WriteableBitmap target, System.Windows.Point imagePoint, double pressure)
    {
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        double opacity = ApplyToolPressureToOpacity(Math.Clamp(HealingStrength / 100.0, 0.0, 1.0), pressure);
        if (string.Equals(HealingMode, "spot", StringComparison.OrdinalIgnoreCase) || _healingSourceBitmap is null)
        {
            ApplyBlurSharpDab(target, imagePoint, size, HealingSoftness, size * 0.18, opacity * 100.0, false);
            return;
        }

        System.Windows.Point sourcePoint = new(
            _healingStrokeStartSourcePoint.X + (imagePoint.X - _healingStrokeStartTargetPoint.X),
            _healingStrokeStartSourcePoint.Y + (imagePoint.Y - _healingStrokeStartTargetPoint.Y));
        ApplySourceCopyDab(target, _healingSourceBitmap, imagePoint, sourcePoint, size, HealingSoftness, opacity);
    }

    private void UpdateHealingCircle(System.Windows.Point previewPoint, double pressure)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        HealingCircleSize = size;
        HealingCircleLeft = center.X - (size * 0.5);
        HealingCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateHealingCircleVisibility();
    }

    private void UpdateHealingCircleVisibility()
    {
        HealingCircleVisibility = CanUseHealingPreview() && ShowHealingCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyHealingStrokeSegment(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point fromImagePoint,
        System.Windows.Point toImagePoint,
        double pressure)
    {
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        ForEachToolStrokePoint(fromImagePoint, toImagePoint, size, point =>
        {
            ApplyHealingDab(target, point, pressure);
        });
    }
}
