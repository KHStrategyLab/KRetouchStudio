using System.IO;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    private static readonly string MediaPipeOutputRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "MediaPipeOutput");

    private string _mediaPipeStatusText = "MediaPipe: ready";
    private bool _isMediaPipeConnectionRunning;

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
        if (_isMediaPipeConnectionRunning)
        {
            return;
        }

        if (SelectedPhoto is null || string.IsNullOrWhiteSpace(SelectedPhoto.Path))
        {
            MediaPipeStatusText = "MediaPipe: load photo first";
            return;
        }

        _isMediaPipeConnectionRunning = true;
        MediaPipeStatusText = "MediaPipe: running...";

        string outputDirectory = Path.Combine(MediaPipeOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff"));
        try
        {
            MediaPipeConnectionRunRequest request = new(
                _appConfig.MediaPipe.HelperRuntime,
                Path.Combine(AppContext.BaseDirectory, "Tools", "MediaPipe", "mediapipe_helper.py"),
                Path.Combine(AppContext.BaseDirectory, "Assets", "AiModels", "MediaPipe"),
                SelectedPhoto.Path,
                outputDirectory);

            MediaPipeConnectionRunResult result = await MediaPipeConnectionService.RunAsync(
                request,
                CancellationToken.None);

            MediaPipeStatusText = result.SummaryText;
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = "MediaPipe: failed | " + ex.Message;
        }
        finally
        {
            _isMediaPipeConnectionRunning = false;
        }
    }
}
