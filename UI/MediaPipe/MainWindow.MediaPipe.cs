using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace KRetouchStudio;

public partial class MainWindow
{
    private static readonly string MediaPipeOutputRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "MediaPipeOutput");

    private static readonly MediaBrush MediaPipeFaceBoxStroke = CreateFrozenBrush(MediaColor.FromRgb(80, 210, 255), 0.95);
    private static readonly MediaBrush MediaPipeFeatureStroke = CreateFrozenBrush(MediaColor.FromRgb(255, 218, 96), 0.9);
    private static readonly MediaBrush MediaPipePointStroke = CreateFrozenBrush(MediaColor.FromRgb(255, 248, 184), 0.95);
    private static readonly MediaBrush MediaPipePointFill = CreateFrozenBrush(MediaColor.FromRgb(255, 190, 72), 0.85);
    private static readonly MediaBrush MediaPipeAllPointDebugStroke = CreateFrozenBrush(MediaColor.FromRgb(18, 20, 24), 0.85);
    private static readonly MediaBrush MediaPipeAllPointDebugFill = CreateFrozenBrush(MediaColor.FromRgb(255, 88, 196), 0.82);

    private string _mediaPipeStatusText = "MediaPipe: ready";
    private bool _isMediaPipeConnectionRunning;
    private bool _showMediaPipeAllPointDebugLayer;
    private string? _mediaPipeOverlayPhotoPath;
    private List<MediaPipeFaceBox> _mediaPipeFaceBoxes = [];
    private List<MediaPipeFeaturePath> _mediaPipeFeaturePaths = [];
    private List<MediaPipeLandmarkPoint> _mediaPipeAllLandmarkPoints = [];

    public ObservableCollection<PreviewDebugRectOverlay> MediaPipeFaceBoxOverlays { get; } = new();

    public ObservableCollection<PreviewDebugPolylineOverlay> MediaPipeFeaturePathOverlays { get; } = new();

    public ObservableCollection<PreviewDebugPointOverlay> MediaPipeFeaturePointOverlays { get; } = new();

    public ObservableCollection<PreviewDebugPointOverlay> MediaPipeAllPointDebugOverlays { get; } = new();

    public Visibility MediaPipePreviewOverlayVisibility =>
        SelectedPreviewPhotos.Count == 1 &&
        (MediaPipeFaceBoxOverlays.Count > 0 ||
         MediaPipeFeaturePathOverlays.Count > 0 ||
         MediaPipeFeaturePointOverlays.Count > 0)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility MediaPipeAllPointDebugOverlayVisibility =>
        SelectedPreviewPhotos.Count == 1 &&
        _showMediaPipeAllPointDebugLayer &&
        MediaPipeAllPointDebugOverlays.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string MediaPipeAllPointDebugButtonText =>
        _showMediaPipeAllPointDebugLayer ? "MP Points On" : "MP Points";

    public string MediaPipeStatusText
    {
        get => _mediaPipeStatusText;
        private set
        {
            if (string.Equals(_mediaPipeStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _mediaPipeStatusText = value;
            OnPropertyChanged();
        }
    }

    private async void MediaPipeTestButton_Click(object sender, RoutedEventArgs e)
    {
        await RunMediaPipePreviewAsync();
    }

    private async void MediaPipeAllPointDebugButton_Click(object sender, RoutedEventArgs e)
    {
        _showMediaPipeAllPointDebugLayer = !_showMediaPipeAllPointDebugLayer;
        OnPropertyChanged(nameof(MediaPipeAllPointDebugButtonText));

        if (_showMediaPipeAllPointDebugLayer && _mediaPipeAllLandmarkPoints.Count == 0)
        {
            await RunMediaPipePreviewAsync();
            return;
        }

        UpdateMediaPipePreviewOverlay();
    }

    private async Task RunMediaPipePreviewAsync()
    {
        if (_isMediaPipeConnectionRunning)
        {
            return;
        }

        PhotoItem? targetPhoto = SelectedPhoto;
        if (targetPhoto is null || string.IsNullOrWhiteSpace(targetPhoto.Path))
        {
            MediaPipeStatusText = "MediaPipe: load photo first";
            return;
        }

        _isMediaPipeConnectionRunning = true;
        MediaPipeStatusText = "MediaPipe: running...";

        string targetPhotoPath = targetPhoto.Path;
        string outputDirectory = Path.Combine(MediaPipeOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff"));
        try
        {
            MediaPipeConnectionRunRequest request = new(
                _appConfig.MediaPipe.HelperRuntime,
                Path.Combine(AppContext.BaseDirectory, "Tools", "MediaPipe", "mediapipe_helper.py"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "AiModels", "MediaPipe"),
                targetPhotoPath,
                outputDirectory);

            MediaPipeConnectionRunResult result = await MediaPipeConnectionService.RunAsync(
                request,
                CancellationToken.None);

            MediaPipeStatusText = result.SummaryText;
            if (!ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                return;
            }

            if (result.Succeeded)
            {
                LoadMediaPipePreviewOverlay(outputDirectory, targetPhotoPath);
            }
            else
            {
                ClearMediaPipePreviewOverlay();
            }
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = "MediaPipe: failed | " + ex.Message;
            ClearMediaPipePreviewOverlay();
        }
        finally
        {
            _isMediaPipeConnectionRunning = false;
        }
    }

    private static MediaBrush CreateFrozenBrush(MediaColor color, double opacity)
    {
        SolidColorBrush brush = new(color) { Opacity = opacity };
        brush.Freeze();
        return brush;
    }

    private void LoadMediaPipePreviewOverlay(string outputDirectory, string photoPath)
    {
        _mediaPipeOverlayPhotoPath = photoPath;
        CachePersonAlphaArtifact(outputDirectory, photoPath, PersonAlphaEngineMediaPipe);
        _mediaPipeFaceBoxes = ReadMediaPipeFaceBoxes(Path.Combine(outputDirectory, "face_box.json"));
        _mediaPipeFeaturePaths = ReadMediaPipeFeaturePaths(Path.Combine(outputDirectory, "face_pose.json"));
        _mediaPipeAllLandmarkPoints = ReadMediaPipeAllLandmarkPoints(Path.Combine(outputDirectory, "face_pose.json"));
        UpdateMediaPipePreviewOverlay();
    }

    private void ClearMediaPipePreviewOverlay()
    {
        _mediaPipeOverlayPhotoPath = null;
        _mediaPipeFaceBoxes.Clear();
        _mediaPipeFeaturePaths.Clear();
        _mediaPipeAllLandmarkPoints.Clear();
        ClearMediaPipePreviewOverlayItems();
    }

    private void ClearMediaPipePreviewOverlayItems()
    {
        MediaPipeFaceBoxOverlays.Clear();
        MediaPipeFeaturePathOverlays.Clear();
        MediaPipeFeaturePointOverlays.Clear();
        MediaPipeAllPointDebugOverlays.Clear();
        OnPropertyChanged(nameof(MediaPipePreviewOverlayVisibility));
        OnPropertyChanged(nameof(MediaPipeAllPointDebugOverlayVisibility));
    }

    private void UpdateMediaPipePreviewOverlay()
    {
        MediaPipeFaceBoxOverlays.Clear();
        MediaPipeFeaturePathOverlays.Clear();
        MediaPipeFeaturePointOverlays.Clear();
        MediaPipeAllPointDebugOverlays.Clear();

        if (SelectedPhoto is null ||
            SelectedPreviewPhotos.Count != 1 ||
            string.IsNullOrWhiteSpace(_mediaPipeOverlayPhotoPath) ||
            !string.Equals(SelectedPhoto.Path, _mediaPipeOverlayPhotoPath, StringComparison.OrdinalIgnoreCase) ||
            (_mediaPipeFaceBoxes.Count == 0 && _mediaPipeFeaturePaths.Count == 0 && _mediaPipeAllLandmarkPoints.Count == 0))
        {
            OnPropertyChanged(nameof(MediaPipePreviewOverlayVisibility));
            OnPropertyChanged(nameof(MediaPipeAllPointDebugOverlayVisibility));
            return;
        }

        BitmapSource source = GetSinglePreviewBitmapSource(SelectedPhoto);
        if (source.PixelWidth <= 0 ||
            source.PixelHeight <= 0 ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            OnPropertyChanged(nameof(MediaPipePreviewOverlayVisibility));
            OnPropertyChanged(nameof(MediaPipeAllPointDebugOverlayVisibility));
            return;
        }

        double scaleX = PreviewImageWidth / source.PixelWidth;
        double scaleY = PreviewImageHeight / source.PixelHeight;

        foreach (MediaPipeFaceBox box in _mediaPipeFaceBoxes)
        {
            MediaPipeFaceBoxOverlays.Add(new PreviewDebugRectOverlay(
                PreviewImageLeft + box.X * scaleX,
                PreviewImageTop + box.Y * scaleY,
                box.Width * scaleX,
                box.Height * scaleY,
                MediaPipeFaceBoxStroke,
                1.6));
        }

        foreach (MediaPipeFeaturePath path in _mediaPipeFeaturePaths)
        {
            if (path.Points.Count < 2)
            {
                continue;
            }

            PointCollection displayPoints = [];
            foreach (MediaPipeLandmarkPoint point in path.Points)
            {
                displayPoints.Add(ToMediaPipePreviewPoint(point, source.PixelWidth, source.PixelHeight, scaleX, scaleY));
            }

            if (path.Closed && displayPoints.Count > 2)
            {
                displayPoints.Add(displayPoints[0]);
            }

            MediaPipeFeaturePathOverlays.Add(new PreviewDebugPolylineOverlay(
                displayPoints,
                MediaPipeFeatureStroke,
                1.25));

            foreach (System.Windows.Point point in displayPoints.Take(path.Closed ? Math.Max(0, displayPoints.Count - 1) : displayPoints.Count))
            {
                const double pointSize = 3.4;
                MediaPipeFeaturePointOverlays.Add(new PreviewDebugPointOverlay(
                    point.X - pointSize * 0.5,
                    point.Y - pointSize * 0.5,
                    pointSize,
                    MediaPipePointStroke,
                    MediaPipePointFill,
                    0.8));
            }
        }

        if (_showMediaPipeAllPointDebugLayer)
        {
            foreach (MediaPipeLandmarkPoint point in _mediaPipeAllLandmarkPoints)
            {
                System.Windows.Point displayPoint = ToMediaPipePreviewPoint(point, source.PixelWidth, source.PixelHeight, scaleX, scaleY);
                const double pointSize = 2.8;
                MediaPipeAllPointDebugOverlays.Add(new PreviewDebugPointOverlay(
                    displayPoint.X - pointSize * 0.5,
                    displayPoint.Y - pointSize * 0.5,
                    pointSize,
                    MediaPipeAllPointDebugStroke,
                    MediaPipeAllPointDebugFill,
                    0.65));
            }
        }

        OnPropertyChanged(nameof(MediaPipePreviewOverlayVisibility));
        OnPropertyChanged(nameof(MediaPipeAllPointDebugOverlayVisibility));
    }

    private System.Windows.Point ToMediaPipePreviewPoint(
        MediaPipeLandmarkPoint point,
        int imageWidth,
        int imageHeight,
        double scaleX,
        double scaleY)
    {
        return new System.Windows.Point(
            PreviewImageLeft + point.X * imageWidth * scaleX,
            PreviewImageTop + point.Y * imageHeight * scaleY);
    }

    private static List<MediaPipeFaceBox> ReadMediaPipeFaceBoxes(string path)
    {
        List<MediaPipeFaceBox> boxes = [];
        if (!File.Exists(path))
        {
            return boxes;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("faces", out JsonElement faces) ||
            faces.ValueKind != JsonValueKind.Array)
        {
            return boxes;
        }

        foreach (JsonElement face in faces.EnumerateArray())
        {
            if (!face.TryGetProperty("box", out JsonElement box) ||
                !TryGetDouble(box, "x", out double x) ||
                !TryGetDouble(box, "y", out double y) ||
                !TryGetDouble(box, "width", out double width) ||
                !TryGetDouble(box, "height", out double height))
            {
                continue;
            }

            boxes.Add(new MediaPipeFaceBox(x, y, width, height));
        }

        return boxes;
    }

    private static List<MediaPipeFeaturePath> ReadMediaPipeFeaturePaths(string path)
    {
        List<MediaPipeFeaturePath> paths = [];
        if (!File.Exists(path))
        {
            return paths;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("faces", out JsonElement faces) ||
            faces.ValueKind != JsonValueKind.Array ||
            faces.GetArrayLength() == 0)
        {
            return paths;
        }

        JsonElement firstFace = faces[0];
        if (!firstFace.TryGetProperty("overlay_landmarks", out JsonElement overlay) ||
            !overlay.TryGetProperty("groups", out JsonElement groups) ||
            groups.ValueKind != JsonValueKind.Array)
        {
            return paths;
        }

        foreach (JsonElement group in groups.EnumerateArray())
        {
            string name = group.TryGetProperty("name", out JsonElement nameElement)
                ? nameElement.GetString() ?? string.Empty
                : string.Empty;
            bool closed = group.TryGetProperty("closed", out JsonElement closedElement) &&
                          closedElement.ValueKind == JsonValueKind.True;
            if (!group.TryGetProperty("points", out JsonElement points) ||
                points.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            List<MediaPipeLandmarkPoint> pathPoints = [];
            foreach (JsonElement point in points.EnumerateArray())
            {
                if (!TryGetDouble(point, "x", out double x) ||
                    !TryGetDouble(point, "y", out double y) ||
                    !TryGetDouble(point, "z", out double z))
                {
                    continue;
                }

                int index = point.TryGetProperty("index", out JsonElement indexElement) &&
                            indexElement.TryGetInt32(out int parsedIndex)
                    ? parsedIndex
                    : -1;
                pathPoints.Add(new MediaPipeLandmarkPoint(index, x, y, z));
            }

            if (pathPoints.Count > 0)
            {
                paths.Add(new MediaPipeFeaturePath(name, closed, pathPoints));
            }
        }

        return paths;
    }

    private static List<MediaPipeLandmarkPoint> ReadMediaPipeAllLandmarkPoints(string path)
    {
        List<MediaPipeLandmarkPoint> points = [];
        if (!File.Exists(path))
        {
            return points;
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("faces", out JsonElement faces) ||
            faces.ValueKind != JsonValueKind.Array ||
            faces.GetArrayLength() == 0)
        {
            return points;
        }

        JsonElement firstFace = faces[0];
        if (!firstFace.TryGetProperty("all_landmarks", out JsonElement landmarks) ||
            landmarks.ValueKind != JsonValueKind.Array)
        {
            return points;
        }

        foreach (JsonElement point in landmarks.EnumerateArray())
        {
            if (!TryGetDouble(point, "x", out double x) ||
                !TryGetDouble(point, "y", out double y) ||
                !TryGetDouble(point, "z", out double z))
            {
                continue;
            }

            int index = point.TryGetProperty("index", out JsonElement indexElement) &&
                        indexElement.TryGetInt32(out int parsedIndex)
                ? parsedIndex
                : -1;
            points.Add(new MediaPipeLandmarkPoint(index, x, y, z));
        }

        return points;
    }

    private static bool TryGetDouble(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out JsonElement property) &&
               property.TryGetDouble(out value);
    }

    private sealed record MediaPipeFaceBox(double X, double Y, double Width, double Height);

    private sealed record MediaPipeFeaturePath(string Name, bool Closed, List<MediaPipeLandmarkPoint> Points);

    private sealed record MediaPipeLandmarkPoint(int Index, double X, double Y, double Z);
}
