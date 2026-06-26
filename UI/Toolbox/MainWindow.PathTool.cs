using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility PathToolOptionsVisibility => string.Equals(ActiveToolId, "path", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double PathToolFeather
    {
        get => _pathToolFeather;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 500);
            if (Math.Abs(_pathToolFeather - clamped) < 0.01)
            {
                return;
            }

            _pathToolFeather = clamped;
            OnPropertyChanged();
        }
    }

    public bool PathToolClosed
    {
        get => _pathToolClosed;
        set
        {
            bool next = value && PathAnchorPoints.Count >= 3;
            if (_pathToolClosed == next)
            {
                return;
            }

            _pathToolClosed = next;
            OnPropertyChanged();
            RebuildPathToolGeometry();
        }
    }

    public Geometry? PathToolGeometry
    {
        get => _pathToolGeometry;
        private set
        {
            _pathToolGeometry = value;
            OnPropertyChanged();
        }
    }

    public Visibility PathToolVisibility
    {
        get => _pathToolVisibility;
        private set
        {
            _pathToolVisibility = value;
            OnPropertyChanged();
        }
    }

    public string PathToolStatusText
    {
        get => _pathToolStatusText;
        private set
        {
            _pathToolStatusText = value;
            OnPropertyChanged();
        }
    }

    private void PathToolClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearPathTool();
    }

    private bool CanUsePathTool()
    {
        return string.Equals(ActiveToolId, "path", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void AddPathAnchorAtPreviewPoint(System.Windows.Point previewPoint)
    {
        if (!CanUsePathTool() || !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        if (PathToolClosed)
        {
            PathToolClosed = false;
        }

        int pointIndex = PathAnchorPoints.Count + 1;
        PathAnchorPoints.Add(new ManualAnchorPoint(
            $"path_{pointIndex}",
            $"P{pointIndex}",
            "#F08A5D",
            pixelX,
            pixelY,
            18,
            10));
        UpdatePathAnchorPointPositions();
        RebuildPathToolGeometry();
    }

    private void PathAnchor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanUsePathAnchorEditing() ||
            sender is not FrameworkElement element ||
            element.DataContext is not ManualAnchorPoint point)
        {
            return;
        }

        _draggingPathAnchorPoint = point;
        Mouse.Capture(element);
        e.Handled = true;
    }

    private void PathAnchor_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!CanUsePathAnchorEditing() || _draggingPathAnchorPoint is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        MovePathAnchorToPreviewPoint(_draggingPathAnchorPoint, e.GetPosition(PreviewSurface));
        e.Handled = true;
    }

    private void PathAnchor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggingPathAnchorPoint is null)
        {
            return;
        }

        MovePathAnchorToPreviewPoint(_draggingPathAnchorPoint, e.GetPosition(PreviewSurface));
        _draggingPathAnchorPoint = null;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void UpdatePathAnchorPointPositions()
    {
        if (SelectedPhoto is null || PathAnchorPoints.Count == 0)
        {
            return;
        }

        double imageWidth = SelectedPhoto.BaseImage.PixelWidth;
        double imageHeight = SelectedPhoto.BaseImage.PixelHeight;
        if (!TryGetCurrentPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            return;
        }

        foreach (ManualAnchorPoint point in PathAnchorPoints)
        {
            double pointOffset = point.VisualSize * 0.5;
            point.DisplayLeft = offsetX + point.OriginalX * scale - pointOffset;
            point.DisplayTop = offsetY + point.OriginalY * scale - pointOffset;
        }
    }

    private void MovePathAnchorToPreviewPoint(ManualAnchorPoint point, System.Windows.Point previewPoint)
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        double imageWidth = SelectedPhoto.BaseImage.PixelWidth;
        double imageHeight = SelectedPhoto.BaseImage.PixelHeight;
        if (!TryGetCurrentPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            return;
        }

        double originalX = Math.Clamp((previewPoint.X - offsetX) / scale, 0, Math.Max(0, imageWidth - 1));
        double originalY = Math.Clamp((previewPoint.Y - offsetY) / scale, 0, Math.Max(0, imageHeight - 1));
        point.MoveOriginal(originalX, originalY);
        UpdatePathAnchorPointPositions();
        RebuildPathToolGeometry();
    }

    private void RebuildPathToolGeometry()
    {
        if (PathAnchorPoints.Count == 0)
        {
            PathToolGeometry = null;
            PathToolVisibility = Visibility.Collapsed;
            UpdatePathToolStatus();
            return;
        }

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            ManualAnchorPoint firstPoint = PathAnchorPoints[0];
            System.Windows.Point startPoint = new(
                firstPoint.DisplayLeft + firstPoint.VisualSize * 0.5,
                firstPoint.DisplayTop + firstPoint.VisualSize * 0.5);

            context.BeginFigure(startPoint, false, PathToolClosed && PathAnchorPoints.Count >= 3);
            for (int i = 1; i < PathAnchorPoints.Count; i++)
            {
                ManualAnchorPoint point = PathAnchorPoints[i];
                System.Windows.Point nextPoint = new(
                    point.DisplayLeft + point.VisualSize * 0.5,
                    point.DisplayTop + point.VisualSize * 0.5);
                context.LineTo(nextPoint, true, false);
            }
        }

        geometry.Freeze();
        PathToolGeometry = geometry;
        PathToolVisibility = CanShowPathOverlay() ? Visibility.Visible : Visibility.Collapsed;
        UpdatePathToolStatus();
    }

    private void UpdatePathToolStatus()
    {
        string mode = PathToolClosed && PathAnchorPoints.Count >= 3 ? "Closed" : "Open";
        PathToolStatusText = PathAnchorPoints.Count == 0
            ? "No path"
            : $"Pts {PathAnchorPoints.Count} / {mode}";
        PathToolVisibility = CanShowPathOverlay() && PathAnchorPoints.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ClearPathTool()
    {
        PathAnchorPoints.Clear();
        _draggingPathAnchorPoint = null;
        if (_pathToolClosed)
        {
            _pathToolClosed = false;
            OnPropertyChanged(nameof(PathToolClosed));
        }

        PathToolGeometry = null;
        PathToolVisibility = Visibility.Collapsed;
        PathToolStatusText = "No path";
        Mouse.Capture(null);
    }
}
