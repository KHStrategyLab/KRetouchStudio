using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility SamplerToolOptionsVisibility => string.Equals(ActiveToolId, "sampler", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public double SampleRange
    {
        get => _sampleRange;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 1, 501);
            if (Math.Abs(_sampleRange - clamped) < 0.01)
            {
                return;
            }

            _sampleRange = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public string SampleResultText
    {
        get => _sampleResultText;
        private set
        {
            _sampleResultText = value;
            OnPropertyChanged();
        }
    }

    public System.Windows.Media.Brush SampleColorPreview
    {
        get => _sampleColorPreview;
        private set
        {
            _sampleColorPreview = value;
            OnPropertyChanged();
        }
    }

    public double SampleRegionLeft
    {
        get => _sampleRegionLeft;
        private set
        {
            _sampleRegionLeft = value;
            OnPropertyChanged();
        }
    }

    public double SampleRegionTop
    {
        get => _sampleRegionTop;
        private set
        {
            _sampleRegionTop = value;
            OnPropertyChanged();
        }
    }

    public double SampleRegionWidth
    {
        get => _sampleRegionWidth;
        private set
        {
            _sampleRegionWidth = value;
            OnPropertyChanged();
        }
    }

    public double SampleRegionHeight
    {
        get => _sampleRegionHeight;
        private set
        {
            _sampleRegionHeight = value;
            OnPropertyChanged();
        }
    }

    public double SampleLabelLeft
    {
        get => _sampleLabelLeft;
        private set
        {
            _sampleLabelLeft = value;
            OnPropertyChanged();
        }
    }

    public double SampleLabelTop
    {
        get => _sampleLabelTop;
        private set
        {
            _sampleLabelTop = value;
            OnPropertyChanged();
        }
    }

    public Visibility SampleRegionVisibility
    {
        get => _sampleRegionVisibility;
        private set
        {
            _sampleRegionVisibility = value;
            OnPropertyChanged();
        }
    }

    private bool CanUseSamplerPreview()
    {
        return string.Equals(ActiveToolId, "sampler", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void SampleColorAtPreviewPoint(System.Windows.Point previewPoint)
    {
        if (SelectedPhoto?.BaseImage is not BitmapSource source ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return;
        }

        int requestedRange = Math.Max(1, (int)Math.Round(SampleRange));
        int half = requestedRange / 2;
        int left = Math.Clamp(pixelX - half, 0, source.PixelWidth - 1);
        int top = Math.Clamp(pixelY - half, 0, source.PixelHeight - 1);
        int right = Math.Clamp(left + requestedRange - 1, 0, source.PixelWidth - 1);
        int bottom = Math.Clamp(top + requestedRange - 1, 0, source.PixelHeight - 1);
        left = Math.Max(0, Math.Min(left, right));
        top = Math.Max(0, Math.Min(top, bottom));
        int width = Math.Max(1, right - left + 1);
        int height = Math.Max(1, bottom - top + 1);

        BitmapSource sampleSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        sampleSource.CopyPixels(new Int32Rect(left, top, width, height), pixels, stride, 0);

        long sumB = 0;
        long sumG = 0;
        long sumR = 0;
        int count = width * height;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            sumB += pixels[i];
            sumG += pixels[i + 1];
            sumR += pixels[i + 2];
        }

        byte r = (byte)Math.Clamp((int)Math.Round(sumR / (double)count), 0, 255);
        byte g = (byte)Math.Clamp((int)Math.Round(sumG / (double)count), 0, 255);
        byte b = (byte)Math.Clamp((int)Math.Round(sumB / (double)count), 0, 255);
        string hex = $"#{r:X2}{g:X2}{b:X2}";
        SampleColorPreview = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        SampleResultText = $"{hex}  RGB({r}, {g}, {b})  X:{pixelX} Y:{pixelY}  {width}x{height}";
        UpdateSampleOverlay(left, top, width, height, previewPoint);
    }

    private void UpdateSampleOverlay(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, System.Windows.Point previewPoint)
    {
        if (SelectedPhoto?.BaseImage is not BitmapSource source || source.PixelWidth <= 0 || source.PixelHeight <= 0)
        {
            SampleRegionVisibility = Visibility.Collapsed;
            return;
        }

        double scaleX = PreviewImageWidth / source.PixelWidth;
        double scaleY = PreviewImageHeight / source.PixelHeight;
        SampleRegionLeft = PreviewImageLeft + sourceLeft * scaleX;
        SampleRegionTop = PreviewImageTop + sourceTop * scaleY;
        SampleRegionWidth = Math.Max(1, sourceWidth * scaleX);
        SampleRegionHeight = Math.Max(1, sourceHeight * scaleY);
        SampleLabelLeft = Math.Min(Math.Max(previewPoint.X + 8, PreviewImageLeft), PreviewImageLeft + Math.Max(0, PreviewImageWidth - 260));
        SampleLabelTop = Math.Min(Math.Max(previewPoint.Y + 8, PreviewImageTop), PreviewImageTop + Math.Max(0, PreviewImageHeight - 24));
        SampleRegionVisibility = Visibility.Visible;
    }
}
