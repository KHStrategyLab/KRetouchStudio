using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public sealed class PhotoItem : INotifyPropertyChanged
{
    private ImageSource _image;
    private ImageSource _thumbnail;
    private double _multiPreviewOffsetX;
    private double _multiPreviewOffsetY;
    private double _multiPreviewZoomPercent = 100;
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
            OnPropertyChanged();
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
