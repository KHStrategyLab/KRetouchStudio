using System;
using System.Windows;
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
        if (!CanUseSinglePreviewTool())
        {
            return;
        }

        PreviewZoomPercent = 100;
        CenterSinglePreviewImage();
    }

    private void ApplyPreviewActualSize()
    {
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
}
