using System;
using System.Windows;
using System.Windows.Media;

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
            SaveToolboxDefaults();
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
            SaveToolboxDefaults();
        }
    }

    public double StampOpacity
    {
        get => _stampOpacity;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_stampOpacity - clamped) < 0.01)
            {
                return;
            }

            _stampOpacity = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
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
            SaveToolboxDefaults();
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

    public Geometry? StampSourceMarkerGeometry
    {
        get => _stampSourceMarkerGeometry;
        private set
        {
            _stampSourceMarkerGeometry = value;
            OnPropertyChanged();
        }
    }

    public Visibility StampSourceMarkerVisibility
    {
        get => _stampSourceMarkerVisibility;
        private set
        {
            _stampSourceMarkerVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseStampPreview()
    {
        return string.Equals(ActiveToolId, "stamp", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartStampStroke(System.Windows.Point previewPoint, double pressure)
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
            UpdateStampSourceMarkerVisibility();
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
        _stampLastImagePoint = imagePoint;
        BeginSourceCopyStroke(target);
        double size = ApplyToolPressureToSize(StampSize, pressure);
        double opacity = ApplyToolPressureToOpacity(StampOpacity / 100.0, pressure);
        ApplySourceCopyDab(target, _stampSourceBitmap, imagePoint, _stampStrokeStartSourcePoint, size, StampSoftness, opacity);
        UpdateStampSourceMarker(GetCurrentStampSourceImagePoint(imagePoint));
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueStampStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (!_isStampDragging ||
            _stampSourceBitmap is null ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyStampStrokeSegment(target, _stampLastImagePoint, imagePoint, pressure);
        _stampLastImagePoint = imagePoint;
        UpdateStampSourceMarker(GetCurrentStampSourceImagePoint(imagePoint));
    }

    private void StopStampStroke()
    {
        if (!_isStampDragging)
        {
            return;
        }

        _isStampDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        EndSourceCopyStroke();
        UpdateStampSourceMarkerVisibility();
        PushEditorHistorySnapshot("Stamp", $"{StampSize:0}px");
    }

    private void UpdateStampCircle(System.Windows.Point previewPoint, double pressure)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = ApplyToolPressureToSize(StampSize, pressure);
        StampCircleSize = size;
        StampCircleLeft = center.X - (size * 0.5);
        StampCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateStampCircleVisibility();
        if (_isStampDragging && TryPreviewPointToImagePoint(center, out System.Windows.Point imagePoint))
        {
            UpdateStampSourceMarker(GetCurrentStampSourceImagePoint(imagePoint));
        }
        else
        {
            UpdateStampSourceMarkerVisibility();
        }
    }

    private void UpdateStampCircleVisibility()
    {
        StampCircleVisibility = CanUseStampPreview() && ShowStampCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private bool CanShowStampSourceMarker()
    {
        return CanUseStampPreview() &&
               _hasStampSource &&
               _isStampDragging &&
               _stampSourceBitmap is not null;
    }

    private System.Windows.Point GetCurrentStampSourceImagePoint(System.Windows.Point targetImagePoint)
    {
        if (!_isStampDragging)
        {
            return _stampSourceImagePoint;
        }

        return new System.Windows.Point(
            _stampStrokeStartSourcePoint.X + (targetImagePoint.X - _stampStrokeStartTargetPoint.X),
            _stampStrokeStartSourcePoint.Y + (targetImagePoint.Y - _stampStrokeStartTargetPoint.Y));
    }

    private void UpdateStampSourceMarkerVisibility()
    {
        if (!CanShowStampSourceMarker())
        {
            StampSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        UpdateStampSourceMarker(GetCurrentStampSourceImagePoint(_stampLastImagePoint));
    }

    private void UpdateStampSourceMarker(System.Windows.Point sourceImagePoint)
    {
        if (!CanShowStampSourceMarker() || SelectedPhoto is not PhotoItem photo)
        {
            StampSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        sourceImagePoint = new System.Windows.Point(
            Math.Clamp(sourceImagePoint.X, 0, Math.Max(0, source.PixelWidth - 1)),
            Math.Clamp(sourceImagePoint.Y, 0, Math.Max(0, source.PixelHeight - 1)));
        if (!TryGetCurrentPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out double scale))
        {
            StampSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        System.Windows.Point previewPoint = ToPreviewPoint(sourceImagePoint, offsetX, offsetY, scale);
        const double radius = 7.0;
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new System.Windows.Point(previewPoint.X - radius, previewPoint.Y), false, false);
            context.LineTo(new System.Windows.Point(previewPoint.X + radius, previewPoint.Y), true, false);
            context.BeginFigure(new System.Windows.Point(previewPoint.X, previewPoint.Y - radius), false, false);
            context.LineTo(new System.Windows.Point(previewPoint.X, previewPoint.Y + radius), true, false);
        }

        geometry.Freeze();
        StampSourceMarkerGeometry = geometry;
        StampSourceMarkerVisibility = Visibility.Visible;
    }

    private void ApplyStampStrokeSegment(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point fromImagePoint,
        System.Windows.Point toImagePoint,
        double pressure)
    {
        if (_stampSourceBitmap is null)
        {
            return;
        }

        double size = ApplyToolPressureToSize(StampSize, pressure);
        double opacity = ApplyToolPressureToOpacity(StampOpacity / 100.0, pressure);
        ForEachToolStrokePoint(fromImagePoint, toImagePoint, size, point =>
        {
            System.Windows.Point sourcePoint = new(
                _stampStrokeStartSourcePoint.X + (point.X - _stampStrokeStartTargetPoint.X),
                _stampStrokeStartSourcePoint.Y + (point.Y - _stampStrokeStartTargetPoint.Y));
            ApplySourceCopyDab(target, _stampSourceBitmap, point, sourcePoint, size, StampSoftness, opacity);
        });
    }
}
