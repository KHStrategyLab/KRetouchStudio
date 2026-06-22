using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility FrameToolOptionsVisibility => string.Equals(ActiveToolId, "frame", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility RectangleFrameSelectionVisibility =>
        FrameSelectionVisibility == Visibility.Visible &&
        string.Equals(_frameSelectionShape, "rectangle", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility EllipseFrameSelectionVisibility =>
        FrameSelectionVisibility == Visibility.Visible &&
        string.Equals(_frameSelectionShape, "ellipse", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public double FrameSelectionLeft
    {
        get => _frameSelectionLeft;
        private set
        {
            _frameSelectionLeft = value;
            OnPropertyChanged();
        }
    }

    public double FrameSelectionTop
    {
        get => _frameSelectionTop;
        private set
        {
            _frameSelectionTop = value;
            OnPropertyChanged();
        }
    }

    public double FrameSelectionWidth
    {
        get => _frameSelectionWidth;
        private set
        {
            _frameSelectionWidth = value;
            OnPropertyChanged();
        }
    }

    public double FrameSelectionHeight
    {
        get => _frameSelectionHeight;
        private set
        {
            _frameSelectionHeight = value;
            OnPropertyChanged();
        }
    }

    public Visibility FrameSelectionVisibility
    {
        get => _frameSelectionVisibility;
        private set
        {
            _frameSelectionVisibility = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleFrameSelectionVisibility));
            OnPropertyChanged(nameof(EllipseFrameSelectionVisibility));
        }
    }

    private void FrameShapeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string shape || string.IsNullOrWhiteSpace(shape))
        {
            return;
        }

        _frameSelectionShape = shape;
        UpdateFrameShapeSelection();
        OnPropertyChanged(nameof(RectangleFrameSelectionVisibility));
        OnPropertyChanged(nameof(EllipseFrameSelectionVisibility));
    }

    private void UpdateFrameShapeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetFrameShapeButtons())
        {
            bool isActive = button.Tag is string shape &&
                            string.Equals(shape, _frameSelectionShape, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetFrameShapeButtons()
    {
        yield return RectangleFrameShapeButton;
        yield return EllipseFrameShapeButton;
    }

    private bool CanStartFrameSelection()
    {
        return string.Equals(ActiveToolId, "frame", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartFrameSelection(System.Windows.Point startPoint)
    {
        _isFrameSelectionDragging = true;
        _frameSelectionStartPoint = ClampPointToPreviewImage(startPoint);
        FrameSelectionLeft = _frameSelectionStartPoint.X;
        FrameSelectionTop = _frameSelectionStartPoint.Y;
        FrameSelectionWidth = 0;
        FrameSelectionHeight = 0;
        FrameSelectionVisibility = Visibility.Visible;
        Mouse.Capture(PreviewSurface);
    }

    private void UpdateFrameSelection(System.Windows.Point currentPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(currentPoint);
        double left = Math.Min(_frameSelectionStartPoint.X, clampedPoint.X);
        double top = Math.Min(_frameSelectionStartPoint.Y, clampedPoint.Y);
        double right = Math.Max(_frameSelectionStartPoint.X, clampedPoint.X);
        double bottom = Math.Max(_frameSelectionStartPoint.Y, clampedPoint.Y);

        FrameSelectionLeft = left;
        FrameSelectionTop = top;
        FrameSelectionWidth = Math.Max(0, right - left);
        FrameSelectionHeight = Math.Max(0, bottom - top);
    }

    private void StartFrameSelectionMove(System.Windows.Point startPoint)
    {
        _isFrameSelectionMoving = true;
        _frameSelectionMoveStartPoint = startPoint;
        _frameSelectionMoveStartLeft = FrameSelectionLeft;
        _frameSelectionMoveStartTop = FrameSelectionTop;
        PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
        Mouse.Capture(PreviewSurface);
    }

    private void MoveFrameSelection(System.Windows.Point currentPoint)
    {
        Vector delta = currentPoint - _frameSelectionMoveStartPoint;
        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        double maxLeft = imageRight - FrameSelectionWidth;
        double maxTop = imageBottom - FrameSelectionHeight;

        FrameSelectionLeft = Math.Clamp(_frameSelectionMoveStartLeft + delta.X, imageLeft, Math.Max(imageLeft, maxLeft));
        FrameSelectionTop = Math.Clamp(_frameSelectionMoveStartTop + delta.Y, imageTop, Math.Max(imageTop, maxTop));
    }

    private void StopFrameSelectionMove()
    {
        if (!_isFrameSelectionMoving)
        {
            return;
        }

        _isFrameSelectionMoving = false;
        Mouse.Capture(null);
        UpdateFrameSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void StopFrameSelection()
    {
        if (!_isFrameSelectionDragging)
        {
            return;
        }

        _isFrameSelectionDragging = false;
        Mouse.Capture(null);
        UpdateFrameSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void UpdateFrameSelectionHoverCursor(System.Windows.Point point)
    {
        if (!string.Equals(ActiveToolId, "frame", StringComparison.OrdinalIgnoreCase) ||
            _isFrameSelectionDragging ||
            _isFrameSelectionMoving ||
            SelectedPreviewPhotos.Count != 1)
        {
            PreviewSurface.Cursor = null;
            return;
        }

        PreviewSurface.Cursor = IsPointNearFrameSelectionOutline(point)
            ? System.Windows.Input.Cursors.SizeAll
            : System.Windows.Input.Cursors.Cross;
    }

    private bool IsPointNearFrameSelectionOutline(System.Windows.Point point)
    {
        if (FrameSelectionVisibility != Visibility.Visible || FrameSelectionWidth < 4 || FrameSelectionHeight < 4)
        {
            return false;
        }

        const double hitTolerance = 8;
        double left = FrameSelectionLeft;
        double top = FrameSelectionTop;
        double right = left + FrameSelectionWidth;
        double bottom = top + FrameSelectionHeight;
        bool insideExpandedBounds = point.X >= left - hitTolerance &&
                                    point.X <= right + hitTolerance &&
                                    point.Y >= top - hitTolerance &&
                                    point.Y <= bottom + hitTolerance;
        if (!insideExpandedBounds)
        {
            return false;
        }

        double edgeDistance = Math.Min(
            Math.Min(Math.Abs(point.X - left), Math.Abs(point.X - right)),
            Math.Min(Math.Abs(point.Y - top), Math.Abs(point.Y - bottom)));
        return edgeDistance <= hitTolerance;
    }
}
