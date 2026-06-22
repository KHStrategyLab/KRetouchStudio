using System;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility StampToolOptionsVisibility => string.Equals(ActiveToolId, "stamp", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double StampSize
    {
        get => _stampSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_stampSize - clamped) < 0.01)
            {
                return;
            }

            _stampSize = clamped;
            StampCircleSize = clamped;
            OnPropertyChanged();
        }
    }

    public double StampSoftness
    {
        get => _stampSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_stampSoftness - clamped) < 0.01)
            {
                return;
            }

            _stampSoftness = clamped;
            OnPropertyChanged();
        }
    }

    public bool ShowStampCircle
    {
        get => _showStampCircle;
        set
        {
            if (_showStampCircle == value)
            {
                return;
            }

            _showStampCircle = value;
            OnPropertyChanged();
            UpdateStampCircleVisibility();
        }
    }

    public string StampSourceText
    {
        get => _stampSourceText;
        private set
        {
            _stampSourceText = value;
            OnPropertyChanged();
        }
    }

    public double StampCircleLeft
    {
        get => _stampCircleLeft;
        private set
        {
            _stampCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double StampCircleTop
    {
        get => _stampCircleTop;
        private set
        {
            _stampCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double StampCircleSize
    {
        get => _stampCircleSize;
        private set
        {
            _stampCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility StampCircleVisibility
    {
        get => _stampCircleVisibility;
        private set
        {
            _stampCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseStampPreview()
    {
        return string.Equals(ActiveToolId, "stamp", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartStampStroke(System.Windows.Point previewPoint)
    {
        if (!CanUseStampPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        bool setSource = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt || !_hasStampSource;
        if (setSource)
        {
            _hasStampSource = true;
            _stampSourceImagePoint = imagePoint;
            _stampSourceBitmap = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
            StampSourceText = $"Source: {imagePoint.X:0}, {imagePoint.Y:0}";
            return;
        }

        if (_stampSourceBitmap is null ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isStampDragging = true;
        _stampStrokeStartSourcePoint = _stampSourceImagePoint;
        _stampStrokeStartTargetPoint = imagePoint;
        ApplySourceCopyDab(target, _stampSourceBitmap, imagePoint, _stampStrokeStartSourcePoint, StampSize, StampSoftness, 1.0);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueStampStroke(System.Windows.Point previewPoint)
    {
        if (!_isStampDragging ||
            _stampSourceBitmap is null ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        System.Windows.Point sourcePoint = new(
            _stampStrokeStartSourcePoint.X + (imagePoint.X - _stampStrokeStartTargetPoint.X),
            _stampStrokeStartSourcePoint.Y + (imagePoint.Y - _stampStrokeStartTargetPoint.Y));
        ApplySourceCopyDab(target, _stampSourceBitmap, imagePoint, sourcePoint, StampSize, StampSoftness, 1.0);
    }

    private void StopStampStroke()
    {
        if (!_isStampDragging)
        {
            return;
        }

        _isStampDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        PushEditorHistorySnapshot("Stamp", $"{StampSize:0}px");
    }

    private void UpdateStampCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, StampSize);
        StampCircleSize = size;
        StampCircleLeft = center.X - (size * 0.5);
        StampCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateStampCircleVisibility();
    }

    private void UpdateStampCircleVisibility()
    {
        StampCircleVisibility = CanUseStampPreview() && ShowStampCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
