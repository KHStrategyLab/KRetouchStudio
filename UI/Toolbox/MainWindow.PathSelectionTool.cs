using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace KRetouchStudio;

public partial class MainWindow
{
    private string _pathSelectionMode = "pathselect";
    private bool _isPathSelectionDragging;
    private System.Windows.Point _pathSelectionDragStartImagePoint;
    private readonly Dictionary<ManualAnchorPoint, System.Windows.Point> _pathSelectionStartPoints = [];

    public Visibility PathSelectionToolOptionsVisibility => string.Equals(ActiveToolId, "pathselect", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string PathSelectionMode
    {
        get => _pathSelectionMode;
        private set
        {
            if (string.Equals(_pathSelectionMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _pathSelectionMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PathSelectionStatusText));
        }
    }

    public string PathSelectionStatusText => PathAnchorPoints.Count == 0
        ? "No path"
        : string.Equals(PathSelectionMode, "directselect", StringComparison.OrdinalIgnoreCase)
            ? $"Direct  {PathAnchorPoints.Count} pts"
            : $"Path  {PathAnchorPoints.Count} pts";

    private void PathSelectionModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        PathSelectionMode = mode;
        UpdatePathSelectionModeSelection();
        UpdatePathToolStatus();
    }

    private void UpdatePathSelectionModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetPathSelectionModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, PathSelectionMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetPathSelectionModeButtons()
    {
        yield return PathSelectModeButton;
        yield return DirectSelectModeButton;
    }

    private bool CanUsePathSelectionTool()
    {
        return string.Equals(ActiveToolId, "pathselect", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool() &&
               PathAnchorPoints.Count > 0;
    }

    private bool CanUsePathAnchorEditing()
    {
        return CanUsePathTool() ||
               (CanUsePathSelectionTool() && string.Equals(PathSelectionMode, "directselect", StringComparison.OrdinalIgnoreCase));
    }

    private bool CanShowPathOverlay()
    {
        return CanUseSinglePreviewTool() &&
               PathAnchorPoints.Count > 0 &&
               (string.Equals(ActiveToolId, "path", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ActiveToolId, "pathselect", StringComparison.OrdinalIgnoreCase));
    }

    private bool TryStartPathSelectionMove(System.Windows.Point previewPoint)
    {
        if (!CanUsePathSelectionTool() ||
            !string.Equals(PathSelectionMode, "pathselect", StringComparison.OrdinalIgnoreCase) ||
            PathToolGeometry is null ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return false;
        }

        Rect bounds = PathToolGeometry.Bounds;
        bounds.Inflate(12, 12);
        if (!bounds.Contains(previewPoint))
        {
            return false;
        }

        _isPathSelectionDragging = true;
        _pathSelectionDragStartImagePoint = imagePoint;
        _pathSelectionStartPoints.Clear();
        foreach (ManualAnchorPoint point in PathAnchorPoints)
        {
            _pathSelectionStartPoints[point] = new System.Windows.Point(point.OriginalX, point.OriginalY);
        }

        PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
        Mouse.Capture(PreviewSurface);
        return true;
    }

    private void ContinuePathSelectionMove(System.Windows.Point previewPoint)
    {
        if (!_isPathSelectionDragging ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            SelectedPhoto is null)
        {
            return;
        }

        double dx = imagePoint.X - _pathSelectionDragStartImagePoint.X;
        double dy = imagePoint.Y - _pathSelectionDragStartImagePoint.Y;
        double maxX = Math.Max(0, SelectedPhoto.BaseImage.PixelWidth - 1);
        double maxY = Math.Max(0, SelectedPhoto.BaseImage.PixelHeight - 1);

        foreach ((ManualAnchorPoint point, System.Windows.Point start) in _pathSelectionStartPoints)
        {
            point.MoveOriginal(
                Math.Clamp(start.X + dx, 0, maxX),
                Math.Clamp(start.Y + dy, 0, maxY));
        }

        UpdatePathAnchorPointPositions();
        RebuildPathToolGeometry();
    }

    private void StopPathSelectionMove()
    {
        if (!_isPathSelectionDragging)
        {
            return;
        }

        _isPathSelectionDragging = false;
        _pathSelectionStartPoints.Clear();
        Mouse.Capture(null);
        PreviewSurface.Cursor = string.Equals(PathSelectionMode, "pathselect", StringComparison.OrdinalIgnoreCase)
            ? System.Windows.Input.Cursors.SizeAll
            : System.Windows.Input.Cursors.Cross;
    }
}
