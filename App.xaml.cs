using System.IO;
using System.Threading;
using System.Windows;

namespace KRetouchStudio;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "Global\\KRetouchStudio.SingleInstance";
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Mutex singleInstanceMutex = new(initiallyOwned: true, SingleInstanceMutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
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
    }

    protected override void OnExit(ExitEventArgs e)
    {
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

}
