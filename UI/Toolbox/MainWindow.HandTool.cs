using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility HandToolOptionsVisibility => string.Equals(ActiveToolId, "hand", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    private bool CanUseHandPreview()
    {
        return string.Equals(ActiveToolId, "hand", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private bool CanUseTemporaryHandPreview()
    {
        return CanUseSinglePreviewTool() &&
               _isSpacePressed &&
               (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control;
    }

    private void PreviewFitInButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreviewFitIn();
    }

    private void PreviewActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPreviewActualSize();
    }

    private void ApplyPreviewFitIn()
    {
        if (SelectedPreviewPhotos.Count > 1)
        {
            ApplyMultiPreviewFitIn();
            return;
        }

        if (!CanUseSinglePreviewTool())
        {
            return;
        }

        PreviewZoomPercent = 100;
        CenterSinglePreviewImage();
    }

    private void ApplyPreviewActualSize()
    {
        if (SelectedPreviewPhotos.Count > 1)
        {
            ApplyMultiPreviewActualSize();
            return;
        }

        if (!CanUseSinglePreviewTool() || SelectedPhoto is null)
        {
            return;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        double surfaceWidth = PreviewSurface.ActualWidth;
        double surfaceHeight = PreviewSurface.ActualHeight;
        if (source.PixelWidth <= 0 || source.PixelHeight <= 0 || surfaceWidth <= 0 || surfaceHeight <= 0)
        {
            return;
        }

        double fitScale = Math.Min(surfaceWidth / source.PixelWidth, surfaceHeight / source.PixelHeight);
        if (fitScale <= 0)
        {
            return;
        }

        PreviewZoomPercent = Math.Round((100.0 / fitScale) / 5.0) * 5.0;
        CenterSinglePreviewImage();
    }

    private void CenterSinglePreviewImage()
    {
        if (SelectedPhoto is null)
        {
            return;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        if (TryGetPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out _))
        {
            UpdateSinglePreviewPan(offsetX, offsetY);
        }
    }

    private void ApplyMultiPreviewFitIn()
    {
        if (SelectedPreviewPhotos.Count <= 1)
        {
            return;
        }

        MultiPreviewItemsControl?.UpdateLayout();
        foreach (PhotoItem photo in SelectedPreviewPhotos)
        {
            photo.MultiPreviewZoomPercent = 100;
            photo.MultiPreviewOffsetX = 0;
            photo.MultiPreviewOffsetY = 0;
            photo.UseOriginalForMultiPreview = false;
        }

        ReapplyMultiPreviewTilePanClamps();
    }

    private void ApplyMultiPreviewActualSize()
    {
        if (SelectedPreviewPhotos.Count <= 1 || MultiPreviewItemsControl is null)
        {
            return;
        }

        MultiPreviewItemsControl.UpdateLayout();
        foreach (PhotoItem photo in SelectedPreviewPhotos)
        {
            if (!TryGetMultiPreviewTile(photo, out FrameworkElement? tile) ||
                tile is null ||
                tile.ActualWidth <= 0 ||
                tile.ActualHeight <= 0 ||
                photo.BaseImage.PixelWidth <= 0 ||
                photo.BaseImage.PixelHeight <= 0)
            {
                continue;
            }

            double fitScale = Math.Min(
                tile.ActualWidth / photo.BaseImage.PixelWidth,
                tile.ActualHeight / photo.BaseImage.PixelHeight);
            if (fitScale <= 0)
            {
                continue;
            }

            photo.MultiPreviewZoomPercent = 100.0 / fitScale;
            UpdatePreviewTilePan(photo, tile, 0, 0);
        }
    }
}
