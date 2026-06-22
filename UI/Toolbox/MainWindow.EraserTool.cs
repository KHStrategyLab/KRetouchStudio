using System;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility EraserToolOptionsVisibility => string.Equals(ActiveToolId, "eraser", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double EraserSize
    {
        get => _eraserSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_eraserSize - clamped) < 0.01)
            {
                return;
            }

            _eraserSize = clamped;
            EraserCircleSize = clamped;
            OnPropertyChanged();
        }
    }

    public double EraserSoftness
    {
        get => _eraserSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_eraserSoftness - clamped) < 0.01)
            {
                return;
            }

            _eraserSoftness = clamped;
            OnPropertyChanged();
        }
    }

    public bool ShowEraserCircle
    {
        get => _showEraserCircle;
        set
        {
            if (_showEraserCircle == value)
            {
                return;
            }

            _showEraserCircle = value;
            OnPropertyChanged();
            UpdateEraserCircleVisibility();
        }
    }

    public double EraserCircleLeft
    {
        get => _eraserCircleLeft;
        private set
        {
            _eraserCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double EraserCircleTop
    {
        get => _eraserCircleTop;
        private set
        {
            _eraserCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double EraserCircleSize
    {
        get => _eraserCircleSize;
        private set
        {
            _eraserCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility EraserCircleVisibility
    {
        get => _eraserCircleVisibility;
        private set
        {
            _eraserCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseEraserPreview()
    {
        return string.Equals(ActiveToolId, "eraser", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartEraserStroke(System.Windows.Point previewPoint)
    {
        if (!CanUseEraserPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isEraserDragging = true;
        ApplyRestoreDab(target, photo.BaseImage, imagePoint, EraserSize, EraserSoftness, 1.0);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueEraserStroke(System.Windows.Point previewPoint)
    {
        if (!_isEraserDragging ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyRestoreDab(target, photo.BaseImage, imagePoint, EraserSize, EraserSoftness, 1.0);
    }

    private void StopEraserStroke()
    {
        if (!_isEraserDragging)
        {
            return;
        }

        _isEraserDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        PushEditorHistorySnapshot("Eraser", $"{EraserSize:0}px");
    }

    private void UpdateEraserCircle(System.Windows.Point previewPoint)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = Math.Max(1, EraserSize);
        EraserCircleSize = size;
        EraserCircleLeft = center.X - (size * 0.5);
        EraserCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateEraserCircleVisibility();
    }

    private void UpdateEraserCircleVisibility()
    {
        EraserCircleVisibility = CanUseEraserPreview() && ShowEraserCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
