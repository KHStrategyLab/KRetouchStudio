using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    private string _zoomToolStatusText = "Click or drag to zoom";

    public Visibility ZoomToolOptionsVisibility => string.Equals(ActiveToolId, "zoom", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string ZoomToolStatusText
    {
        get => _zoomToolStatusText;
        private set
        {
            _zoomToolStatusText = value;
            OnPropertyChanged();
        }
    }

    public double ZoomSelectionLeft
    {
        get => _zoomSelectionLeft;
        private set
        {
            _zoomSelectionLeft = value;
            OnPropertyChanged();
        }
    }

    public double ZoomSelectionTop
    {
        get => _zoomSelectionTop;
        private set
        {
            _zoomSelectionTop = value;
            OnPropertyChanged();
        }
    }

    public double ZoomSelectionWidth
    {
        get => _zoomSelectionWidth;
        private set
        {
            _zoomSelectionWidth = value;
            OnPropertyChanged();
        }
    }

    public double ZoomSelectionHeight
    {
        get => _zoomSelectionHeight;
        private set
        {
            _zoomSelectionHeight = value;
            OnPropertyChanged();
        }
    }

    public Visibility ZoomSelectionVisibility
    {
        get => _zoomSelectionVisibility;
        private set
        {
            _zoomSelectionVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseZoomPreview()
    {
        return string.Equals(ActiveToolId, "zoom", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private bool CanUseSelectTemporaryZoomPreview()
    {
        return string.Equals(ActiveToolId, "select", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool() &&
               _isSpacePressed &&
               (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
    }

    private void StartZoomSelection(System.Windows.Point startPoint)
    {
        _isTemporaryZoomSelectionDragging = CanUseSelectTemporaryZoomPreview();
        _isZoomSelectionDragging = true;
        _zoomSelectionStartPoint = ClampPointToPreviewImage(startPoint);
        ZoomSelectionLeft = _zoomSelectionStartPoint.X;
        ZoomSelectionTop = _zoomSelectionStartPoint.Y;
        ZoomSelectionWidth = 0;
        ZoomSelectionHeight = 0;
        ZoomSelectionVisibility = Visibility.Visible;
        ZoomToolStatusText = "Drag zoom box";
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        Mouse.Capture(PreviewSurface);
    }

    private void UpdateZoomSelection(System.Windows.Point currentPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(currentPoint);
        double left = Math.Min(_zoomSelectionStartPoint.X, clampedPoint.X);
        double top = Math.Min(_zoomSelectionStartPoint.Y, clampedPoint.Y);
        double right = Math.Max(_zoomSelectionStartPoint.X, clampedPoint.X);
        double bottom = Math.Max(_zoomSelectionStartPoint.Y, clampedPoint.Y);

        ZoomSelectionLeft = left;
        ZoomSelectionTop = top;
        ZoomSelectionWidth = Math.Max(0, right - left);
        ZoomSelectionHeight = Math.Max(0, bottom - top);
        ZoomToolStatusText = $"{ZoomSelectionWidth:0} x {ZoomSelectionHeight:0}";
    }

    private void StopZoomSelection()
    {
        if (!_isZoomSelectionDragging)
        {
            return;
        }

        bool isClickZoom = ZoomSelectionWidth < 4 && ZoomSelectionHeight < 4;
        double selectionLeft = ZoomSelectionLeft;
        double selectionTop = ZoomSelectionTop;
        double selectionWidth = ZoomSelectionWidth;
        double selectionHeight = ZoomSelectionHeight;

        _isZoomSelectionDragging = false;
        ZoomSelectionVisibility = Visibility.Collapsed;
        Mouse.Capture(null);

        if (isClickZoom)
        {
            ApplyZoomClick();
            _isTemporaryZoomSelectionDragging = false;
            return;
        }

        ApplyZoomSelection(selectionLeft, selectionTop, selectionWidth, selectionHeight);
    }

    private void ApplyZoomClick()
    {
        bool isZoomOut = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        double step = isZoomOut ? -10 : 10;
        PreviewZoomPercent = Math.Round((PreviewZoomPercent + step) / 5) * 5;
        ZoomToolStatusText = isZoomOut ? $"Zoom out {PreviewZoomPercent:0}%" : $"Zoom in {PreviewZoomPercent:0}%";
    }

    private void ApplyZoomSelection(double selectionLeft, double selectionTop, double selectionWidth, double selectionHeight)
    {
        if ((!CanUseZoomPreview() && !_isTemporaryZoomSelectionDragging) || SelectedPhoto is null)
        {
            _isTemporaryZoomSelectionDragging = false;
            return;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        double sourceWidth = source.PixelWidth;
        double sourceHeight = source.PixelHeight;
        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;
        if (sourceWidth <= 0 || sourceHeight <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
        {
            _isTemporaryZoomSelectionDragging = false;
            return;
        }

        if (!TryGetPreviewImageTransform(sourceWidth, sourceHeight, out double offsetX, out double offsetY, out double currentScale) ||
            currentScale <= 0)
        {
            _isTemporaryZoomSelectionDragging = false;
            return;
        }

        double imageLeft = Math.Clamp((selectionLeft - offsetX) / currentScale, 0, sourceWidth);
        double imageTop = Math.Clamp((selectionTop - offsetY) / currentScale, 0, sourceHeight);
        double imageRight = Math.Clamp((selectionLeft + selectionWidth - offsetX) / currentScale, 0, sourceWidth);
        double imageBottom = Math.Clamp((selectionTop + selectionHeight - offsetY) / currentScale, 0, sourceHeight);
        double imageSelectionWidth = Math.Max(1, imageRight - imageLeft);
        double imageSelectionHeight = Math.Max(1, imageBottom - imageTop);
        if (imageSelectionWidth < 2 || imageSelectionHeight < 2)
        {
            ApplyZoomClick();
            return;
        }

        double fitScale = Math.Min(surfaceWidth / sourceWidth, surfaceHeight / sourceHeight);
        if (fitScale <= 0)
        {
            _isTemporaryZoomSelectionDragging = false;
            return;
        }

        double targetScale = Math.Min(surfaceWidth / imageSelectionWidth, surfaceHeight / imageSelectionHeight);
        double targetZoom = Math.Clamp(Math.Round((targetScale / fitScale * 100.0) / 5.0) * 5.0, 25.0, 300.0);
        double centerImageX = imageLeft + imageSelectionWidth * 0.5;
        double centerImageY = imageTop + imageSelectionHeight * 0.5;

        PreviewZoomPercent = targetZoom;

        if (TryGetPreviewImageTransform(sourceWidth, sourceHeight, out _, out _, out double newScale) &&
            newScale > 0)
        {
            double nextLeft = surfaceWidth * 0.5 - centerImageX * newScale;
            double nextTop = surfaceHeight * 0.5 - centerImageY * newScale;
            UpdateSinglePreviewPan(nextLeft, nextTop);
        }

        ZoomToolStatusText = $"Selection zoom {PreviewZoomPercent:0}%";
        _isTemporaryZoomSelectionDragging = false;
    }
}
