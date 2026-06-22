using System;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility HistoryBrushToolOptionsVisibility => string.Equals(ActiveToolId, "historybrush", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double HistoryBrushSize
    {
        get => _historyBrushSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_historyBrushSize - clamped) < 0.01)
            {
                return;
            }

            _historyBrushSize = clamped;
            HistoryBrushCircleSize = clamped;
            OnPropertyChanged();
        }
    }

    public double HistoryBrushSoftness
    {
        get => _historyBrushSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_historyBrushSoftness - clamped) < 0.01)
            {
                return;
            }

            _historyBrushSoftness = clamped;
            OnPropertyChanged();
        }
    }

    public double HistoryBrushStrength
    {
        get => _historyBrushStrength;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_historyBrushStrength - clamped) < 0.01)
            {
                return;
            }

            _historyBrushStrength = clamped;
            OnPropertyChanged();
        }
    }

    public bool ShowHistoryBrushCircle
    {
        get => _showHistoryBrushCircle;
        set
        {
            if (_showHistoryBrushCircle == value)
            {
                return;
            }

            _showHistoryBrushCircle = value;
            OnPropertyChanged();
            UpdateHistoryBrushCircleVisibility();
        }
    }

    public double HistoryBrushCircleLeft
    {
        get => _historyBrushCircleLeft;
        private set
        {
            _historyBrushCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double HistoryBrushCircleTop
    {
        get => _historyBrushCircleTop;
        private set
        {
            _historyBrushCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double HistoryBrushCircleSize
    {
        get => _historyBrushCircleSize;
        private set
        {
            _historyBrushCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility HistoryBrushCircleVisibility
    {
        get => _historyBrushCircleVisibility;
        private set
        {
            _historyBrushCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseHistoryBrushPreview()
    {
        return string.Equals(ActiveToolId, "historybrush", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartHistoryBrushStroke(System.Windows.Point previewPoint)
    {
        if (!CanUseHistoryBrushPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isHistoryBrushDragging = true;
        ApplyRestoreDab(target, photo.BaseImage, imagePoint, HistoryBrushSize, HistoryBrushSoftness, HistoryBrushStrength / 100.0);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueHistoryBrushStroke(System.Windows.Point previewPoint)
    {
        if (!_isHistoryBrushDragging ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyRestoreDab(target, photo.BaseImage, imagePoint, HistoryBrushSize, HistoryBrushSoftness, HistoryBrushStrength / 100.0);
    }

    private void StopHistoryBrushStroke()
    {
        if (!_isHistoryBrushDragging)
        {
            return;
        }

        _isHistoryBrushDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        PushEditorHistorySnapshot("History Brush", $"{HistoryBrushSize:0}px / {HistoryBrushStrength:0}%");
    }

    private void UpdateHistoryBrushCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, HistoryBrushSize);
        HistoryBrushCircleSize = size;
        HistoryBrushCircleLeft = center.X - (size * 0.5);
        HistoryBrushCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateHistoryBrushCircleVisibility();
    }

    private void UpdateHistoryBrushCircleVisibility()
    {
        HistoryBrushCircleVisibility = CanUseHistoryBrushPreview() && ShowHistoryBrushCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
