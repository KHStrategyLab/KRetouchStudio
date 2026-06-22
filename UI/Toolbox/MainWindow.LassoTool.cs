using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace KRetouchStudio;

public partial class MainWindow
{
    private readonly List<System.Windows.Point> _lassoToolPoints = [];
    private bool _isLassoToolDragging;
    private Geometry? _lassoToolGeometry;
    private Visibility _lassoToolVisibility = Visibility.Collapsed;
    private string _lassoToolStatusText = "No lasso";
    private double _lassoToolFeather;
    private bool _lassoToolClosed = true;

    public Visibility LassoToolOptionsVisibility => string.Equals(ActiveToolId, "lasso", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double LassoToolFeather
    {
        get => _lassoToolFeather;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 500);
            if (Math.Abs(_lassoToolFeather - clamped) < 0.01)
            {
                return;
            }

            _lassoToolFeather = clamped;
            OnPropertyChanged();
        }
    }

    public bool LassoToolClosed
    {
        get => _lassoToolClosed;
        set
        {
            if (_lassoToolClosed == value)
            {
                return;
            }

            _lassoToolClosed = value;
            OnPropertyChanged();
            RebuildLassoToolGeometry();
        }
    }

    public Geometry? LassoToolGeometry
    {
        get => _lassoToolGeometry;
        private set
        {
            _lassoToolGeometry = value;
            OnPropertyChanged();
        }
    }

    public Visibility LassoToolVisibility
    {
        get => _lassoToolVisibility;
        private set
        {
            _lassoToolVisibility = value;
            OnPropertyChanged();
        }
    }

    public string LassoToolStatusText
    {
        get => _lassoToolStatusText;
        private set
        {
            _lassoToolStatusText = value;
            OnPropertyChanged();
        }
    }

    private void LassoToolClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearLassoTool();
    }

    private bool CanUseLassoTool()
    {
        return string.Equals(ActiveToolId, "lasso", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartLassoTool(System.Windows.Point previewPoint)
    {
        if (!CanUseLassoTool() || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        _lassoToolPoints.Clear();
        _lassoToolPoints.Add(imagePoint);
        _isLassoToolDragging = true;
        LassoToolVisibility = Visibility.Visible;
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        Mouse.Capture(PreviewSurface);
        RebuildLassoToolGeometry();
    }

    private void ContinueLassoTool(System.Windows.Point previewPoint)
    {
        if (!_isLassoToolDragging || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        if (_lassoToolPoints.Count == 0)
        {
            _lassoToolPoints.Add(imagePoint);
        }
        else
        {
            System.Windows.Point last = _lassoToolPoints[^1];
            double dx = imagePoint.X - last.X;
            double dy = imagePoint.Y - last.Y;
            if ((dx * dx) + (dy * dy) >= 4.0)
            {
                _lassoToolPoints.Add(imagePoint);
            }
        }

        RebuildLassoToolGeometry();
    }

    private void StopLassoTool()
    {
        if (!_isLassoToolDragging)
        {
            return;
        }

        _isLassoToolDragging = false;
        Mouse.Capture(null);
        RebuildLassoToolGeometry();
    }

    private void RebuildLassoToolGeometry()
    {
        if (_lassoToolPoints.Count < 2 || SelectedPhoto is null)
        {
            LassoToolGeometry = null;
            LassoToolVisibility = Visibility.Collapsed;
            LassoToolStatusText = _lassoToolPoints.Count == 0 ? "No lasso" : $"Pts {_lassoToolPoints.Count}";
            return;
        }

        double imageWidth = SelectedPhoto.BaseImage.PixelWidth;
        double imageHeight = SelectedPhoto.BaseImage.PixelHeight;
        if (!TryGetPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            return;
        }

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            System.Windows.Point firstPoint = ToPreviewPoint(_lassoToolPoints[0], offsetX, offsetY, scale);
            context.BeginFigure(firstPoint, false, LassoToolClosed && !_isLassoToolDragging);
            for (int i = 1; i < _lassoToolPoints.Count; i++)
            {
                context.LineTo(ToPreviewPoint(_lassoToolPoints[i], offsetX, offsetY, scale), true, false);
            }
        }

        geometry.Freeze();
        LassoToolGeometry = geometry;
        LassoToolVisibility = CanUseLassoTool() ? Visibility.Visible : Visibility.Collapsed;
        LassoToolStatusText = $"Pts {_lassoToolPoints.Count} / {(LassoToolClosed ? "Closed" : "Open")}";
    }

    private static System.Windows.Point ToPreviewPoint(System.Windows.Point imagePoint, double offsetX, double offsetY, double scale)
    {
        return new(offsetX + imagePoint.X * scale, offsetY + imagePoint.Y * scale);
    }

    private void ClearLassoTool()
    {
        _lassoToolPoints.Clear();
        _isLassoToolDragging = false;
        LassoToolGeometry = null;
        LassoToolVisibility = Visibility.Collapsed;
        LassoToolStatusText = "No lasso";
        Mouse.Capture(null);
    }
}
