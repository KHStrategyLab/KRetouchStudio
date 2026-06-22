using System;
using System.Windows;
using System.Windows.Input;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility RulerToolOptionsVisibility => string.Equals(ActiveToolId, "ruler", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double RulerX1
    {
        get => _rulerX1;
        private set
        {
            _rulerX1 = value;
            OnPropertyChanged();
        }
    }

    public double RulerY1
    {
        get => _rulerY1;
        private set
        {
            _rulerY1 = value;
            OnPropertyChanged();
        }
    }

    public double RulerX2
    {
        get => _rulerX2;
        private set
        {
            _rulerX2 = value;
            OnPropertyChanged();
        }
    }

    public double RulerY2
    {
        get => _rulerY2;
        private set
        {
            _rulerY2 = value;
            OnPropertyChanged();
        }
    }

    public double RulerLabelLeft
    {
        get => _rulerLabelLeft;
        private set
        {
            _rulerLabelLeft = value;
            OnPropertyChanged();
        }
    }

    public double RulerLabelTop
    {
        get => _rulerLabelTop;
        private set
        {
            _rulerLabelTop = value;
            OnPropertyChanged();
        }
    }

    public string RulerMeasurementText
    {
        get => _rulerMeasurementText;
        private set
        {
            _rulerMeasurementText = value;
            OnPropertyChanged();
        }
    }

    public Visibility RulerVisibility
    {
        get => _rulerVisibility;
        private set
        {
            _rulerVisibility = value;
            OnPropertyChanged();
        }
    }

    private void RulerClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearRulerMeasurement();
    }

    private bool CanUseRulerPreview()
    {
        return string.Equals(ActiveToolId, "ruler", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartRulerMeasurement(System.Windows.Point startPoint)
    {
        _isRulerDragging = true;
        _rulerStartPoint = ClampPointToPreviewImage(startPoint);
        RulerX1 = _rulerStartPoint.X;
        RulerY1 = _rulerStartPoint.Y;
        RulerX2 = _rulerStartPoint.X;
        RulerY2 = _rulerStartPoint.Y;
        RulerVisibility = Visibility.Visible;
        UpdateRulerReadout(_rulerStartPoint);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        Mouse.Capture(PreviewSurface);
    }

    private void UpdateRulerMeasurement(System.Windows.Point currentPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(currentPoint);
        RulerX2 = clampedPoint.X;
        RulerY2 = clampedPoint.Y;
        UpdateRulerReadout(clampedPoint);
    }

    private void StopRulerMeasurement()
    {
        if (!_isRulerDragging)
        {
            return;
        }

        _isRulerDragging = false;
        Mouse.Capture(null);
        if (CanUseRulerPreview())
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        }
    }

    private void ClearRulerMeasurement()
    {
        _isRulerDragging = false;
        RulerVisibility = Visibility.Collapsed;
        RulerMeasurementText = "0 px / 0 deg";
        Mouse.Capture(null);
    }

    private void UpdateRulerReadout(System.Windows.Point currentPoint)
    {
        double dx = currentPoint.X - _rulerStartPoint.X;
        double dy = currentPoint.Y - _rulerStartPoint.Y;
        double length = Math.Sqrt((dx * dx) + (dy * dy));
        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        RulerMeasurementText = $"{length:0.0} px / {angle:0.0} deg";
        RulerLabelLeft = Math.Min(Math.Max(currentPoint.X + 8, PreviewImageLeft), PreviewImageLeft + Math.Max(0, PreviewImageWidth - 120));
        RulerLabelTop = Math.Min(Math.Max(currentPoint.Y + 8, PreviewImageTop), PreviewImageTop + Math.Max(0, PreviewImageHeight - 24));
    }
}
