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
        return CanUseSinglePreviewTool() &&
               CanUseTemporaryZoomGesture();
    }

    private bool CanUseTemporaryZoomGesture()
    {
        return _isSpacePressed &&
               (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
    }

    private void StartZoomSelection(System.Windows.Point startPoint)
    {
        _isTemporaryZoomSelectionDragging = CanUseSelectTemporaryZoomPreview();
        _zoomSelectionMultiPhoto = null;
        _zoomSelectionMultiTile = null;
        StartZoomSelectionCore(startPoint);
    }

    private void StartMultiPreviewZoomSelection(PhotoItem photo, FrameworkElement tile, System.Windows.Point startPoint)
    {
        _isTemporaryZoomSelectionDragging = true;
        _zoomSelectionMultiPhoto = photo;
        _zoomSelectionMultiTile = tile;
        StartZoomSelectionCore(startPoint);
    }

    private void StartZoomSelectionCore(System.Windows.Point startPoint)
    {
        _isZoomSelectionDragging = true;
        _zoomSelectionStartPoint = ClampPointToZoomTarget(startPoint);
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
        System.Windows.Point clampedPoint = ClampPointToZoomTarget(currentPoint);
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
            if (_zoomSelectionMultiPhoto is null)
            {
                ApplyZoomClick();
            }

            ClearZoomSelectionTarget();
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
        if (_zoomSelectionMultiPhoto is not null && _zoomSelectionMultiTile is not null)
        {
            ApplyMultiPreviewZoomSelection(_zoomSelectionMultiPhoto, _zoomSelectionMultiTile, selectionLeft, selectionTop, selectionWidth, selectionHeight);
            ClearZoomSelectionTarget();
            return;
        }

        if ((!CanUseZoomPreview() && !_isTemporaryZoomSelectionDragging) || SelectedPhoto is null)
        {
            ClearZoomSelectionTarget();
            return;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        double sourceWidth = source.PixelWidth;
        double sourceHeight = source.PixelHeight;
        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;
        if (sourceWidth <= 0 || sourceHeight <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
        {
            ClearZoomSelectionTarget();
            return;
        }

        if (!TryGetCurrentPreviewImageTransform(sourceWidth, sourceHeight, out double offsetX, out double offsetY, out double currentScale) ||
            currentScale <= 0)
        {
            ClearZoomSelectionTarget();
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
            ClearZoomSelectionTarget();
            return;
        }

        double fitScale = Math.Min(surfaceWidth / sourceWidth, surfaceHeight / sourceHeight);
        if (fitScale <= 0)
        {
            ClearZoomSelectionTarget();
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
        ClearZoomSelectionTarget();
    }

    private void ApplyMultiPreviewZoomSelection(PhotoItem photo, FrameworkElement tile, double selectionLeft, double selectionTop, double selectionWidth, double selectionHeight)
    {
        if (selectionWidth <= 1 || selectionHeight <= 1)
        {
            return;
        }

        BitmapSource source = photo.Image as BitmapSource ?? photo.BaseImage;
        double sourceWidth = source.PixelWidth;
        double sourceHeight = source.PixelHeight;
        double tileWidth = tile.ActualWidth;
        double tileHeight = tile.ActualHeight;
        if (sourceWidth <= 0 || sourceHeight <= 0 || tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        Rect tileRect = GetPreviewTileSurfaceRect(tile);
        Rect selectionRect = new(selectionLeft, selectionTop, selectionWidth, selectionHeight);
        selectionRect.Intersect(tileRect);
        if (selectionRect.Width < 2 || selectionRect.Height < 2)
        {
            return;
        }

        double baseScale = Math.Min(tileWidth / sourceWidth, tileHeight / sourceHeight);
        double currentScale = baseScale * photo.MultiPreviewZoomScale;
        if (baseScale <= 0 || currentScale <= 0)
        {
            return;
        }

        double selectionTileLeft = selectionRect.Left - tileRect.Left;
        double selectionTileTop = selectionRect.Top - tileRect.Top;
        double currentDisplayedWidth = sourceWidth * currentScale;
        double currentDisplayedHeight = sourceHeight * currentScale;
        double currentImageLeft = (tileWidth - currentDisplayedWidth) * 0.5 + photo.MultiPreviewOffsetX;
        double currentImageTop = (tileHeight - currentDisplayedHeight) * 0.5 + photo.MultiPreviewOffsetY;

        double imageLeft = Math.Clamp((selectionTileLeft - currentImageLeft) / currentScale, 0, sourceWidth);
        double imageTop = Math.Clamp((selectionTileTop - currentImageTop) / currentScale, 0, sourceHeight);
        double imageRight = Math.Clamp((selectionTileLeft + selectionRect.Width - currentImageLeft) / currentScale, 0, sourceWidth);
        double imageBottom = Math.Clamp((selectionTileTop + selectionRect.Height - currentImageTop) / currentScale, 0, sourceHeight);
        double imageSelectionWidth = Math.Max(1, imageRight - imageLeft);
        double imageSelectionHeight = Math.Max(1, imageBottom - imageTop);
        if (imageSelectionWidth < 2 || imageSelectionHeight < 2)
        {
            return;
        }

        double targetScale = Math.Min(tileWidth / imageSelectionWidth, tileHeight / imageSelectionHeight);
        double targetZoom = Math.Clamp(Math.Round((targetScale / baseScale * 100.0) / 5.0) * 5.0, 100.0, PhotoItem.MultiPreviewMaxZoomPercent);
        double centerImageX = imageLeft + imageSelectionWidth * 0.5;
        double centerImageY = imageTop + imageSelectionHeight * 0.5;

        photo.MultiPreviewZoomPercent = targetZoom;
        double newScale = baseScale * photo.MultiPreviewZoomScale;
        double targetOffsetX = ((sourceWidth * 0.5) - centerImageX) * newScale;
        double targetOffsetY = ((sourceHeight * 0.5) - centerImageY) * newScale;
        UpdatePreviewTilePan(photo, tile, targetOffsetX, targetOffsetY);
        ZoomToolStatusText = $"Tile zoom {photo.MultiPreviewZoomPercent:0}%";
    }

    private System.Windows.Point ClampPointToZoomTarget(System.Windows.Point point)
    {
        if (_zoomSelectionMultiTile is not null)
        {
            Rect tileRect = GetPreviewTileSurfaceRect(_zoomSelectionMultiTile);
            return new System.Windows.Point(
                Math.Clamp(point.X, tileRect.Left, tileRect.Right),
                Math.Clamp(point.Y, tileRect.Top, tileRect.Bottom));
        }

        return ClampPointToPreviewImage(point);
    }

    private Rect GetPreviewTileSurfaceRect(FrameworkElement tile)
    {
        System.Windows.Point topLeft = tile.TranslatePoint(new System.Windows.Point(0, 0), PreviewSurface);
        return new Rect(topLeft.X, topLeft.Y, Math.Max(0, tile.ActualWidth), Math.Max(0, tile.ActualHeight));
    }

    private void ClearZoomSelectionTarget()
    {
        _isTemporaryZoomSelectionDragging = false;
        _zoomSelectionMultiPhoto = null;
        _zoomSelectionMultiTile = null;
    }
}
