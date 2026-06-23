using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public enum PreviewProxy1200State
{
    NotNeeded,
    Missing,
    Building,
    Ready
}

public sealed class PhotoItem : INotifyPropertyChanged
{
    public const int PreviewProxyLongSide = 1200;
    public const double PreviewProxySharpness = 35;

    private ImageSource _image;
    private ImageSource _thumbnail;
    private BitmapSource? _previewProxy1200;
    private bool _isPreviewProxy1200Building;
    private double _multiPreviewOffsetX;
    private double _multiPreviewOffsetY;
    private double _multiPreviewZoomPercent = 100;
    private bool _useOriginalForMultiPreview;
    private bool _isSelected;
    private string _fileName;
    private string _path;

    private PhotoItem(string path, BitmapSource baseImage, BitmapSource thumbnail)
    {
        _path = path;
        _fileName = System.IO.Path.GetFileName(path);
        BaseImage = baseImage;
        _image = baseImage;
        _thumbnail = thumbnail;
        DisplayInfo = $"{baseImage.PixelWidth} x {baseImage.PixelHeight}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Path
    {
        get => _path;
        private set => _path = value;
    }

    public string FileName
    {
        get => _fileName;
        private set => _fileName = value;
    }

    public string DisplayInfo { get; }

    public BitmapSource BaseImage { get; }

    public ImageSource Image
    {
        get => _image;
        private set
        {
            if (ReferenceEquals(_image, value))
            {
                return;
            }

            _image = value;
            _previewProxy1200 = null;
            _isPreviewProxy1200Building = false;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewProxy1200));
            OnPropertyChanged(nameof(PreviewProxy1200State));
            OnPropertyChanged(nameof(MultiPreviewImageSource));
        }
    }

    public ImageSource Thumbnail
    {
        get => _thumbnail;
        private set
        {
            if (ReferenceEquals(_thumbnail, value))
            {
                return;
            }

            _thumbnail = value;
            OnPropertyChanged();
        }
    }

    public BitmapSource? PreviewProxy1200
    {
        get => _previewProxy1200;
        private set
        {
            if (ReferenceEquals(_previewProxy1200, value))
            {
                return;
            }

            _previewProxy1200 = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewProxy1200State));
            OnPropertyChanged(nameof(MultiPreviewImageSource));
        }
    }

    public PreviewProxy1200State PreviewProxy1200State
    {
        get
        {
            if (_isPreviewProxy1200Building)
            {
                return PreviewProxy1200State.Building;
            }

            if (_previewProxy1200 is not null)
            {
                return PreviewProxy1200State.Ready;
            }

            BitmapSource source = Image as BitmapSource ?? BaseImage;
            return Math.Max(source.PixelWidth, source.PixelHeight) <= PreviewProxyLongSide
                ? PreviewProxy1200State.NotNeeded
                : PreviewProxy1200State.Missing;
        }
    }

    public ImageSource MultiPreviewImageSource =>
        _useOriginalForMultiPreview || PreviewProxy1200 is null
            ? Image
            : PreviewProxy1200;

    public double MultiPreviewOffsetX
    {
        get => _multiPreviewOffsetX;
        set
        {
            if (Math.Abs(_multiPreviewOffsetX - value) < 0.01)
            {
                return;
            }

            _multiPreviewOffsetX = value;
            OnPropertyChanged();
        }
    }

    public double MultiPreviewOffsetY
    {
        get => _multiPreviewOffsetY;
        set
        {
            if (Math.Abs(_multiPreviewOffsetY - value) < 0.01)
            {
                return;
            }

            _multiPreviewOffsetY = value;
            OnPropertyChanged();
        }
    }

    public double MultiPreviewZoomPercent
    {
        get => _multiPreviewZoomPercent;
        set
        {
            double clamped = Math.Clamp(value, 100, 300);
            if (Math.Abs(_multiPreviewZoomPercent - clamped) < 0.01)
            {
                return;
            }

            _multiPreviewZoomPercent = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MultiPreviewZoomScale));
        }
    }

    public double MultiPreviewZoomScale => MultiPreviewZoomPercent / 100.0;

    public bool UseOriginalForMultiPreview
    {
        get => _useOriginalForMultiPreview;
        set
        {
            if (_useOriginalForMultiPreview == value)
            {
                return;
            }

            _useOriginalForMultiPreview = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MultiPreviewImageSource));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public static PhotoItem Load(string path)
    {
        return new PhotoItem(path, LoadBitmap(path, null), LoadBitmap(path, 96));
    }

    public void SetAdjustedImage(ImageSource image)
    {
        Image = image;
    }

    public void SetPreviewProxy1200(BitmapSource? proxy)
    {
        bool proxyChanged = !ReferenceEquals(_previewProxy1200, proxy);
        bool stateChanged = _isPreviewProxy1200Building || proxyChanged;
        _previewProxy1200 = proxy;
        _isPreviewProxy1200Building = false;

        if (proxyChanged)
        {
            OnPropertyChanged(nameof(PreviewProxy1200));
            OnPropertyChanged(nameof(MultiPreviewImageSource));
        }

        if (stateChanged)
        {
            OnPropertyChanged(nameof(PreviewProxy1200State));
        }
    }

    public void SetPreviewProxy1200Building(bool isBuilding)
    {
        if (_isPreviewProxy1200Building == isBuilding)
        {
            return;
        }

        _isPreviewProxy1200Building = isBuilding;
        OnPropertyChanged(nameof(PreviewProxy1200State));
    }

    public void ResetAdjustedImage()
    {
        Image = BaseImage;
    }

    public void Rename(string newPath)
    {
        if (string.Equals(_path, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _path = newPath;
        _fileName = System.IO.Path.GetFileName(newPath);
        OnPropertyChanged(nameof(Path));
        OnPropertyChanged(nameof(FileName));
    }

    private static BitmapSource LoadBitmap(string path, int? decodePixelWidth)
    {
        if (ColorManagementSettings.Mode == ColorManagementMode.Disabled)
        {
            return LoadFallbackBitmap(path, decodePixelWidth);
        }

        try
        {
            return LoadColorManagedBitmap(path, decodePixelWidth);
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or UnauthorizedAccessException)
        {
            return LoadFallbackBitmap(path, decodePixelWidth);
        }
    }

    private static BitmapSource LoadColorManagedBitmap(string path, int? decodePixelWidth)
    {
        BitmapDecoder decoder = BitmapDecoder.Create(
            new Uri(path, UriKind.Absolute),
            BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreImageCache,
            BitmapCacheOption.OnLoad);

        BitmapFrame frame = decoder.Frames[0];
        BitmapSource source = frame;
        ColorContext destinationContext = CreateDestinationColorContext();
        ColorContext sourceContext = frame.ColorContexts?.FirstOrDefault() ?? new ColorContext(PixelFormats.Bgra32);
        source = new ColorConvertedBitmap(source, sourceContext, destinationContext, PixelFormats.Pbgra32);

        if (decodePixelWidth is not null && source.PixelWidth > decodePixelWidth.Value)
        {
            double scale = (double)decodePixelWidth.Value / source.PixelWidth;
            source = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        }

        source.Freeze();
        return source;
    }

    private static ColorContext CreateDestinationColorContext()
    {
        if (ColorManagementSettings.Mode == ColorManagementMode.Manual &&
            !string.IsNullOrWhiteSpace(ColorManagementSettings.ManualDisplayProfilePath) &&
            File.Exists(ColorManagementSettings.ManualDisplayProfilePath))
        {
            return new ColorContext(new Uri(ColorManagementSettings.ManualDisplayProfilePath, UriKind.Absolute));
        }

        return new ColorContext(PixelFormats.Bgra32);
    }

    private static BitmapImage LoadFallbackBitmap(string path, int? decodePixelWidth)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        if (decodePixelWidth is not null)
        {
            bitmap.DecodePixelWidth = decodePixelWidth.Value;
        }

        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static BitmapSource? CreatePreviewProxy1200(BitmapSource source)
    {
        if (Math.Max(source.PixelWidth, source.PixelHeight) <= PreviewProxyLongSide)
        {
            return null;
        }

        BitmapSource proxySource = ResizeLongSide(source, PreviewProxyLongSide);
        BitmapSource bgraSource = EnsureBgraBitmapSource(proxySource);
        int stride = bgraSource.PixelWidth * 4;
        byte[] pixels = new byte[stride * bgraSource.PixelHeight];
        bgraSource.CopyPixels(pixels, stride, 0);

        ApplyProxySharpness(pixels, bgraSource.PixelWidth, bgraSource.PixelHeight, stride, PreviewProxySharpness);

        WriteableBitmap proxy = new(
            bgraSource.PixelWidth,
            bgraSource.PixelHeight,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null);
        proxy.WritePixels(new System.Windows.Int32Rect(0, 0, bgraSource.PixelWidth, bgraSource.PixelHeight), pixels, stride, 0);
        proxy.Freeze();
        return proxy;
    }

    private static BitmapSource ResizeLongSide(BitmapSource source, int longSide)
    {
        int sourceLongSide = Math.Max(source.PixelWidth, source.PixelHeight);
        if (sourceLongSide <= 0 || sourceLongSide <= longSide)
        {
            return source;
        }

        double scale = longSide / (double)sourceLongSide;
        TransformedBitmap resized = new(source, new ScaleTransform(scale, scale));
        resized.Freeze();
        return resized;
    }

    private static BitmapSource EnsureBgraBitmapSource(BitmapSource source)
    {
        if (source.Format == PixelFormats.Bgra32)
        {
            return source;
        }

        FormatConvertedBitmap converted = new(source, PixelFormats.Bgra32, null, 0);
        converted.Freeze();
        return converted;
    }

    private static void ApplyProxySharpness(byte[] pixels, int pixelWidth, int pixelHeight, int stride, double sharpness)
    {
        if (pixelWidth < 3 || pixelHeight < 3 || Math.Abs(sharpness) < 0.001)
        {
            return;
        }

        byte[] sourcePixels = (byte[])pixels.Clone();
        double amount = Math.Clamp(sharpness / 100.0, -1.0, 1.0);

        for (int y = 1; y < pixelHeight - 1; y++)
        {
            for (int x = 1; x < pixelWidth - 1; x++)
            {
                int index = (y * stride) + (x * 4);
                for (int channel = 0; channel < 3; channel++)
                {
                    int sum = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        int rowOffset = (y + ky) * stride;
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            sum += sourcePixels[rowOffset + ((x + kx) * 4) + channel];
                        }
                    }

                    double original = sourcePixels[index + channel];
                    double blurred = sum / 9.0;
                    double adjusted = amount > 0
                        ? original + ((original - blurred) * amount * 1.2)
                        : original + ((blurred - original) * -amount);
                    pixels[index + channel] = ClampProxyChannel(adjusted);
                }
            }
        }
    }

    private static byte ClampProxyChannel(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
