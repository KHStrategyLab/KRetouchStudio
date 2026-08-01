using System.IO;
using System.Threading;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace KRetouchStudio;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "Global\\KRetouchStudio.SingleInstance";
    private const string SingleInstancePipeName = "KRetouchStudio.SingleInstance.OpenFiles";
    private Mutex? _singleInstanceMutex;
    private CancellationTokenSource? _singleInstancePipeCancellation;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Mutex singleInstanceMutex = new(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            TryForwardStartupArguments(e.Args);
            singleInstanceMutex.Dispose();
            Shutdown(0);
            return;
        }

        _singleInstanceMutex = singleInstanceMutex;
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        string[] startupImagePaths = e.Args
            .Where(File.Exists)
            .Where(IsSupportedImageFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        MainWindow window = new(startupImagePaths);
        MainWindow = window;
        window.Show();
        StartSingleInstancePipeServer(window);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstancePipeCancellation?.Cancel();
        _singleInstancePipeCancellation?.Dispose();
        _singleInstancePipeCancellation = null;
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;
        base.OnExit(e);
    }

    private static bool IsSupportedImageFile(string path)
    {
        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".tif" or ".tiff" or ".bmp" or ".gif" or ".raw";
    }

    private void StartSingleInstancePipeServer(MainWindow window)
    {
        _singleInstancePipeCancellation = new CancellationTokenSource();
        _ = Task.Run(() => RunSingleInstancePipeServerAsync(
            window,
            _singleInstancePipeCancellation.Token));
    }

    private static async Task RunSingleInstancePipeServerAsync(
        MainWindow window,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using NamedPipeServerStream server = new(
                    SingleInstancePipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cancellationToken);

                using StreamReader reader = new(
                    server,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: true,
                    bufferSize: 1024,
                    leaveOpen: true);
                string? payload = await reader.ReadLineAsync(cancellationToken);
                string[] paths = string.IsNullOrWhiteSpace(payload)
                    ? []
                    : JsonSerializer.Deserialize<string[]>(payload) ?? [];
                string[] supportedPaths = paths
                    .Where(File.Exists)
                    .Where(IsSupportedImageFile)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                await window.Dispatcher.InvokeAsync(
                    () => window.OpenPhotosFromExternalRequest(supportedPaths));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex) when (
                ex is IOException or
                JsonException or
                InvalidOperationException)
            {
            }
        }
    }

    private static void TryForwardStartupArguments(IReadOnlyList<string> startupArguments)
    {
        string payload = JsonSerializer.Serialize(startupArguments);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                using NamedPipeClientStream client = new(
                    ".",
                    SingleInstancePipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous);
                client.Connect(timeout: 250);
                using StreamWriter writer = new(
                    client,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    bufferSize: 1024,
                    leaveOpen: false);
                writer.WriteLine(payload);
                writer.Flush();
                return;
            }
            catch (Exception ex) when (
                ex is IOException or
                TimeoutException or
                UnauthorizedAccessException or
                InvalidOperationException)
            {
                Thread.Sleep(100);
            }
        }
    }

}
