using KRetouchStudio.Pipeline;
using System.IO;
using KRetouchStudio.Tabs;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const string BackgroundReplacementHistoryTitle = "Background";
    private const string WhiteBackgroundHistoryDetail = "White background";
    private const string GrayBackgroundHistoryDetail = "Gray background";
    private const string ColorBackgroundHistoryDetail = "Color background";
    private const string ImageBackgroundHistoryDetail = "Image background";
    private const string BackgroundResetHistoryDetail = "Reset";
    private const string PersonAlphaEngineMediaPipe = "MediaPipe";
    private const string PersonAlphaEngineBiRefNet = "BiRefNet";
    private const byte WhiteBackgroundAlphaLowCutoff = 24;
    private const byte WhiteBackgroundAlphaHighCutoff = 248;
    private const byte WhiteBackgroundSampleAlphaMax = 32;
    private const int WhiteBackgroundProbeHalfLength = 15;
    private const int WhiteBackgroundProbeCandidateRadius = 4;
    private const int WhiteBackgroundProbeInnerRadius = 2;
    private const int WhiteBackgroundProbeBoxSize = 150;
    private const int WhiteBackgroundProbeBoxStride = 50;
    private const byte WhiteBackgroundProbeForegroundThreshold = 64;
    private const int WhiteBackgroundInnerFillRadius = 2;
    private const byte WhiteBackgroundInnerFillAlphaMin = 245;
    private const int WhiteBackgroundEdgeBlurRadius = 1;
    private const int BiRefNetInputSharpenStrength = 100;
    private const int BackgroundDragPreviewRetryDelayMs = 35;
    private const double WhiteBackgroundAlphaGammaMinimum = 0.40;
    private static readonly TimeSpan BiRefNetWarmupTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan BiRefNetInputPrepareTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan BiRefNetAlphaTimeout = TimeSpan.FromMinutes(3);

    private static readonly string BiRefNetOutputRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "BiRefNetOutput");

    private static readonly string BackgroundDiagnosticLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "Logs",
        "background.log");

    private static readonly string BackgroundImageLibraryRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "BackgroundImages");

    private static readonly MediaBrush PreviewSurfaceDefaultBrush = CreateFrozenBrush(MediaColor.FromRgb(17, 19, 21), 1.0);

    private MediaBrush _previewSurfaceBackgroundBrush = PreviewSurfaceDefaultBrush;
    private BitmapSource? _backgroundPreviewImageSource;
    private string? _backgroundPreviewPhotoPath;
    private double _backgroundPreviewFrameWidth;
    private double _backgroundPreviewFrameHeight;
    private string? _personAlphaPath;
    private string? _personAlphaPhotoPath;
    private string? _personAlphaEngine;
    private string? _personAlphaRunMode;
    private string? _personAlphaFailurePhotoPath;
    private string? _personAlphaFailureStatus;
    private string? _biRefNetPreparedInputPath;
    private string? _biRefNetPreparedInputPhotoPath;
    private int _biRefNetPreparedInputSize;
    private int _biRefNetPreparedInputSharpen;
    private readonly SemaphoreSlim _biRefNetInputPrepareGate = new(1, 1);
    private readonly SemaphoreSlim _personAlphaCreateGate = new(1, 1);
    private readonly SemaphoreSlim _backgroundResourcePrepareGate = new(1, 1);
    private readonly SemaphoreSlim _backgroundRenderGate = new(1, 1);
    private bool _isBiRefNetWarmupStarted;
    private bool _isBackgroundPreviewRunning;
    private bool _hasPendingBackgroundPreviewRequest;
    private bool _isBackgroundDragPreviewRunning;
    private bool _hasPendingBackgroundDragPreviewRequest;
    private int _backgroundResourcePrepareVersion;
    private int _backgroundPreviewRenderVersion;

    public MediaBrush PreviewSurfaceBackgroundBrush
    {
        get => _previewSurfaceBackgroundBrush;
        private set
        {
            if (ReferenceEquals(_previewSurfaceBackgroundBrush, value))
            {
                return;
            }

            _previewSurfaceBackgroundBrush = value;
            OnPropertyChanged();
        }
    }

    private void LoadBackgroundSettingsFromConfig()
    {
        BackgroundSettings settings = _appConfig.Background ??= new BackgroundSettings();
        ResetBackgroundAdjustmentSliders();
        BackgroundRetouchTab.SetBackgroundImagePaths(
            settings.BackgroundImagePaths.Where(File.Exists),
            settings.SelectedBackgroundImagePath);
    }

    private void ResetBackgroundAdjustmentSliders()
    {
        if (BackgroundRetouchTab is null)
        {
            return;
        }

        BackgroundRetouchTab.BoundaryProbeStrength = 0;
        BackgroundRetouchTab.BoundaryCleanStrength = 0;
        BackgroundRetouchTab.BackgroundOpacity = 100;
        BackgroundRetouchTab.EdgeBlurStrength = 0;
        BackgroundRetouchTab.AlphaShrinkStrength = 0;
        BackgroundRetouchTab.SoftAlphaStrength = 0;
        BackgroundRetouchTab.AlphaGammaStrength = 0;
    }

    private void SaveBackgroundImageSettings(BackgroundSettings settings)
    {
        settings.BackgroundImagePaths = BackgroundRetouchTab.BackgroundImages
            .Select(item => item.Path)
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        settings.SelectedBackgroundImagePath = string.IsNullOrWhiteSpace(BackgroundRetouchTab.SelectedBackgroundImagePath)
            ? null
            : BackgroundRetouchTab.SelectedBackgroundImagePath;
    }

    private async void BackgroundRetouchTab_BackgroundReplacementRequested(object? sender, EventArgs e)
    {
        await ApplyCurrentBackgroundPipelineAsync(
            RetouchRenderQuality.FullResolution,
            captureHistory: true);
    }

    private async void BackgroundRetouchTab_BackgroundReplacementPreviewChanged(object? sender, EventArgs e)
    {
        try
        {
            await ApplyCurrentBackgroundPipelineAsync(
                RetouchRenderQuality.Preview,
                captureHistory: false);
        }
        catch (Exception ex)
        {
            ClearBackgroundPreview();
            MediaPipeStatusText = "Background preview failed: " + ex.Message;
        }
    }

    private async void BackgroundRetouchTab_BackgroundImageImportRequested(object? sender, EventArgs e)
    {
        Microsoft.Win32.OpenFileDialog dialog = new()
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.tif;*.tiff;*.bmp;*.gif|All files|*.*",
            Multiselect = true,
            Title = "Select background image"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        foreach (string fileName in dialog.FileNames)
        {
            string? libraryPath = CopyBackgroundImageToLibrary(fileName);
            if (!string.IsNullOrWhiteSpace(libraryPath))
            {
                BackgroundRetouchTab.AddBackgroundImagePath(libraryPath);
            }
        }

        BackgroundSettings settings = _appConfig.Background ??= new BackgroundSettings();
        SaveBackgroundImageSettings(settings);
        SaveAppConfig();
        await ApplyCurrentBackgroundPipelineAsync(
            RetouchRenderQuality.FullResolution,
            captureHistory: true);
    }

    private async void BackgroundRetouchTab_BackgroundImageSelected(object? sender, BackgroundImageSelectedEventArgs e)
    {
        BackgroundSettings settings = _appConfig.Background ??= new BackgroundSettings();
        SaveBackgroundImageSettings(settings);
        SaveAppConfig();
        await ApplyCurrentBackgroundPipelineAsync(
            RetouchRenderQuality.FullResolution,
            captureHistory: true);
    }

    private async void BackgroundRetouchTab_BackgroundImageRemoved(object? sender, BackgroundImageRemovedEventArgs e)
    {
        BackgroundSettings settings = _appConfig.Background ??= new BackgroundSettings();
        SaveBackgroundImageSettings(settings);
        SaveAppConfig();
        if (SelectedPhoto is PhotoItem && e.WasSelected)
        {
            await ApplyCurrentBackgroundPipelineAsync(
                RetouchRenderQuality.FullResolution,
                captureHistory: true);
        }

        MediaPipeStatusText = "Background: image removed";
    }

    private static string? CopyBackgroundImageToLibrary(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        Directory.CreateDirectory(BackgroundImageLibraryRoot);
        string sourceFullPath = Path.GetFullPath(sourcePath);
        string libraryFullPath = Path.GetFullPath(BackgroundImageLibraryRoot);
        if (sourceFullPath.StartsWith(libraryFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return sourceFullPath;
        }

        string extension = Path.GetExtension(sourcePath);
        string baseName = Path.GetFileNameWithoutExtension(sourcePath);
        string targetPath = Path.Combine(BackgroundImageLibraryRoot, Path.GetFileName(sourcePath));
        int suffix = 1;
        while (File.Exists(targetPath))
        {
            targetPath = Path.Combine(BackgroundImageLibraryRoot, $"{baseName}_{suffix}{extension}");
            suffix++;
        }

        File.Copy(sourcePath, targetPath);
        return targetPath;
    }

    private async void BackgroundRetouchTab_BackgroundTabOpened(object? sender, EventArgs e)
    {
        PhotoItem? targetPhoto = SelectedPhoto;
        if (targetPhoto is null)
        {
            return;
        }

        await PrepareBackgroundReplacementResourcesAsync(targetPhoto, reportStatus: true);
    }

    private void StartBiRefNetWarmup()
    {
        if (_isBiRefNetWarmupStarted)
        {
            return;
        }

        _isBiRefNetWarmupStarted = true;
        _ = WarmUpBiRefNetAsync();
    }

    private async Task WarmUpBiRefNetAsync()
    {
        string outputDirectory = Path.Combine(BiRefNetOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "_warmup");
        BiRefNetMattingWarmUpRequest request = new(
            _appConfig.MediaPipe.HelperRuntime,
            Path.Combine(AppContext.BaseDirectory, "Tools", "BiRefNet", "birefnet_helper.py"),
            outputDirectory,
            "ZhengPeng7/BiRefNet_lite-matting",
            1024,
            BiRefNetInputSharpenStrength,
            "auto");

        try
        {
            MediaPipeStatusText = "BiRefNet: warming...";
            using CancellationTokenSource timeout = new(BiRefNetWarmupTimeout);
            BiRefNetMattingWarmUpResult result = await BiRefNetMattingService.WarmUpAsync(
                request,
                timeout.Token);

            if (!Dispatcher.HasShutdownStarted)
            {
                MediaPipeStatusText = result.SummaryText;
            }
        }
        catch (Exception ex)
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                MediaPipeStatusText = "BiRefNet: warmup failed | " + ex.Message;
            }
        }
    }

    private async void BackgroundRetouchTab_BackgroundReplacementAdjustmentCommitted(object? sender, EventArgs e)
    {
        await ApplyCurrentBackgroundPipelineAsync(
            RetouchRenderQuality.FullResolution,
            captureHistory: true);
    }

    private async void BackgroundRetouchTab_BackgroundResetRequested(object? sender, EventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        BackgroundRetouchTab.RestoreSnapshot(null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Background);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Background,
            RetouchRenderQuality.FullResolution,
            "Background Reset");
        if (applied)
        {
            PushEditorHistorySnapshot(BackgroundReplacementHistoryTitle, BackgroundResetHistoryDetail);
            UpdateBackgroundHistoryResetState();
        }
    }

    private async Task<bool> ApplyCurrentBackgroundPipelineAsync(
        RetouchRenderQuality quality,
        bool captureHistory)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        BackgroundAdjustmentSnapshot state = BackgroundRetouchTab.CaptureSnapshot();
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Background);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Background,
            quality,
            "Background");
        if (applied && captureHistory && quality == RetouchRenderQuality.FullResolution)
        {
            if (!state.IsNeutral &&
                (state.Mode != BackgroundReplacementMode.Image ||
                 !string.IsNullOrWhiteSpace(state.SelectedImagePath)))
            {
                PushOrReplacePipelineHistory(
                    photo,
                    BackgroundReplacementHistoryTitle,
                    CreateBackgroundPipelineHistoryDetail(state));
            }
            else
            {
                PushEditorHistorySnapshot(
                    BackgroundReplacementHistoryTitle,
                    BackgroundResetHistoryDetail);
            }

            UpdateBackgroundHistoryResetState();
        }

        return applied;
    }

    private bool CanUseBackgroundColorPickPreview()
    {
        return BackgroundRetouchTab?.IsPickBackgroundModeActive == true &&
               CanUseSinglePreviewTool();
    }

    private void ApplyBackgroundPickedColorAtPreviewPoint(System.Windows.Point previewPoint)
    {
        if (!TrySampleBackgroundColorAtPreviewPoint(previewPoint, out MediaColor color))
        {
            MediaPipeStatusText = "Background: pick inside image";
            return;
        }

        BackgroundRetouchTab.ApplyPickedBackgroundColor(color);
        MediaPipeStatusText = $"Background: picked #{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private bool TrySampleBackgroundColorAtPreviewPoint(System.Windows.Point previewPoint, out MediaColor color)
    {
        color = default;
        if (SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePixel(previewPoint, out int pixelX, out int pixelY))
        {
            return false;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        int requestedRange = 5;
        int half = requestedRange / 2;
        int left = Math.Clamp(pixelX - half, 0, source.PixelWidth - 1);
        int top = Math.Clamp(pixelY - half, 0, source.PixelHeight - 1);
        int right = Math.Clamp(left + requestedRange - 1, 0, source.PixelWidth - 1);
        int bottom = Math.Clamp(top + requestedRange - 1, 0, source.PixelHeight - 1);
        left = Math.Max(0, Math.Min(left, right));
        top = Math.Max(0, Math.Min(top, bottom));
        int width = Math.Max(1, right - left + 1);
        int height = Math.Max(1, bottom - top + 1);

        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        bgraSource.CopyPixels(new Int32Rect(left, top, width, height), pixels, stride, 0);

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
        color = MediaColor.FromRgb(r, g, b);
        return true;
    }

    private async Task ApplyBackgroundReplacementDragPreviewAsync()
    {
        if (_isBackgroundDragPreviewRunning)
        {
            _hasPendingBackgroundDragPreviewRequest = true;
            return;
        }

        _isBackgroundDragPreviewRunning = true;
        try
        {
            do
            {
                _hasPendingBackgroundDragPreviewRequest = false;
                await ApplyBackgroundReplacementDragPreviewCoreAsync();
            }
            while (_hasPendingBackgroundDragPreviewRequest);
        }
        finally
        {
            _isBackgroundDragPreviewRunning = false;
        }
    }

    private async Task ApplyBackgroundReplacementDragPreviewCoreAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || BackgroundRetouchTab is null)
        {
            ClearBackgroundPreview();
            return;
        }

        int renderVersion = Volatile.Read(ref _backgroundPreviewRenderVersion);
        double boundaryProbeStrength = BackgroundRetouchTab.BoundaryProbeStrength;
        double boundaryCleanStrength = BackgroundRetouchTab.BoundaryCleanStrength;
        double edgeBlurStrength = BackgroundRetouchTab.EdgeBlurStrength;
        double alphaShrinkStrength = BackgroundRetouchTab.AlphaShrinkStrength;
        double softAlphaStrength = BackgroundRetouchTab.SoftAlphaStrength;
        double alphaGammaStrength = BackgroundRetouchTab.AlphaGammaStrength;
        double backgroundOpacity = BackgroundRetouchTab.BackgroundOpacity;
        (string detailPrefix, string statusName, byte fillB, byte fillG, byte fillR, string? imagePath) = GetActiveBackgroundReplacement();

        if (!IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet))
        {
            _ = PrepareBackgroundReplacementResourcesAsync(targetPhoto, reportStatus: true);
            MediaPipeStatusText = "Background: preparing alpha...";
            return;
        }

        string alphaPath = _personAlphaPath!;
        if (!TryGetBackgroundDragPreviewSource(targetPhoto, out BitmapSource proxySource, out double frameWidth, out double frameHeight))
        {
            MediaPipeStatusText = "Background: preparing 1200 preview...";
            return;
        }

        if (!_backgroundRenderGate.Wait(0))
        {
            _hasPendingBackgroundDragPreviewRequest = true;
            await Task.Delay(BackgroundDragPreviewRetryDelayMs);
            return;
        }

        BitmapSource safeProxy = CreateBackgroundRenderThreadSource(proxySource);
        MediaPipeStatusText = $"Background: {statusName} 1200 preview...";

        BitmapSource preview;
        try
        {
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
                renderVersion != _backgroundPreviewRenderVersion)
            {
                return;
            }

            preview = await Task.Run(() => BuildBackgroundReplacementPreview(
                safeProxy,
                alphaPath,
                fillB,
                fillG,
                fillR,
                imagePath,
                backgroundOpacity,
                boundaryProbeStrength,
                boundaryCleanStrength,
                edgeBlurStrength,
                alphaShrinkStrength,
                softAlphaStrength,
                alphaGammaStrength));
        }
        finally
        {
            _backgroundRenderGate.Release();
        }

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _backgroundPreviewRenderVersion)
        {
            return;
        }

        SetBackgroundPreview(targetPhoto, preview, frameWidth, frameHeight);
        MediaPipeStatusText = $"Background: {statusName} 1200 preview";
    }

    private async Task ApplyBackgroundReplacementPreviewAsync()
    {
        Interlocked.Increment(ref _backgroundPreviewRenderVersion);
        if (_isBackgroundPreviewRunning)
        {
            _hasPendingBackgroundPreviewRequest = true;
            return;
        }

        _isBackgroundPreviewRunning = true;
        await _backgroundRenderGate.WaitAsync();
        try
        {
            do
            {
                _hasPendingBackgroundPreviewRequest = false;
                await ApplyBackgroundReplacementPreviewCoreAsync();
            }
            while (_hasPendingBackgroundPreviewRequest);
        }
        finally
        {
            _backgroundRenderGate.Release();
            _isBackgroundPreviewRunning = false;
        }
    }

    private async Task ApplyBackgroundReplacementPreviewCoreAsync()
    {
        PhotoItem? targetPhoto = SelectedPhoto;
        if (targetPhoto is null)
        {
            MediaPipeStatusText = "Background: load photo first";
            AppendBackgroundDiagnosticLog("request blocked | photo=(none)");
            return;
        }

        int renderVersion = Volatile.Read(ref _backgroundPreviewRenderVersion);
        double boundaryProbeStrength = BackgroundRetouchTab?.BoundaryProbeStrength ?? 0;
        double boundaryCleanStrength = BackgroundRetouchTab?.BoundaryCleanStrength ?? 0;
        double edgeBlurStrength = BackgroundRetouchTab?.EdgeBlurStrength ?? 0;
        double alphaShrinkStrength = BackgroundRetouchTab?.AlphaShrinkStrength ?? 0;
        double softAlphaStrength = BackgroundRetouchTab?.SoftAlphaStrength ?? 0;
        double alphaGammaStrength = BackgroundRetouchTab?.AlphaGammaStrength ?? 0;
        double backgroundOpacity = BackgroundRetouchTab?.BackgroundOpacity ?? 100;
        (string detailPrefix, string statusName, byte fillB, byte fillG, byte fillR, string? imagePath) = GetActiveBackgroundReplacement();
        string historyDetail = CreateBackgroundReplacementHistoryDetail(
            detailPrefix,
            backgroundOpacity,
            boundaryProbeStrength,
            boundaryCleanStrength,
            edgeBlurStrength,
            alphaShrinkStrength,
            softAlphaStrength,
            alphaGammaStrength);
        AppendBackgroundDiagnosticLog($"request | mode={statusName} | photo=\"{targetPhoto.Path}\" | alpha={GetBackgroundAlphaState(targetPhoto)}");
        if (IsCurrentBackgroundReplacementAlreadyApplied(historyDetail))
        {
            MediaPipeStatusText = $"Background: {statusName} already applied";
            AppendBackgroundDiagnosticLog($"skip already applied | mode={statusName} | photo=\"{targetPhoto.Path}\"");
            return;
        }

        MediaPipeStatusText = $"Background: {statusName} preview... | {targetPhoto.FileName}";

        try
        {
            string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
            if (alphaPath is null)
            {
                string failureStatus = _personAlphaFailureStatus ?? "not ready";
                MediaPipeStatusText = $"Background: alpha not ready | {targetPhoto.FileName} | {failureStatus}";
                AppendBackgroundDiagnosticLog($"alpha not ready | mode={statusName} | photo=\"{targetPhoto.Path}\" | status=\"{failureStatus}\"");
                return;
            }

            if (!ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                AppendBackgroundDiagnosticLog($"request abandoned | selected photo changed | mode={statusName} | photo=\"{targetPhoto.Path}\"");
                return;
            }

            AppendBackgroundDiagnosticLog($"render start | mode={statusName} | photo=\"{targetPhoto.Path}\" | alpha=\"{alphaPath}\"");
            bool replaceCurrentBackground = IsCurrentHistoryBackgroundReplacement();
            BitmapSource source = GetBackgroundReplacementRenderSource(targetPhoto, replaceCurrentBackground);
            BitmapSource safeSource = CreateBackgroundRenderThreadSource(source);
            BitmapSource preview = await Task.Run(() => BuildBackgroundReplacementPreview(
                safeSource,
                alphaPath,
                fillB,
                fillG,
                fillR,
                imagePath,
                backgroundOpacity,
                boundaryProbeStrength,
                boundaryCleanStrength,
                edgeBlurStrength,
                alphaShrinkStrength,
                softAlphaStrength,
                alphaGammaStrength));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
                renderVersion != _backgroundPreviewRenderVersion)
            {
                AppendBackgroundDiagnosticLog($"render abandoned | newer request exists | mode={statusName} | photo=\"{targetPhoto.Path}\"");
                return;
            }

            targetPhoto.SetAdjustedImage(preview);
            if (replaceCurrentBackground)
            {
                ReplaceCurrentBackgroundReplacementHistorySnapshot(targetPhoto, historyDetail);
            }
            else
            {
                PushEditorHistorySnapshot(BackgroundReplacementHistoryTitle, historyDetail);
            }

            UpdatePreviewLayout();
            string alphaRunMode = string.IsNullOrWhiteSpace(_personAlphaRunMode)
                ? PersonAlphaEngineBiRefNet
                : $"{PersonAlphaEngineBiRefNet} {_personAlphaRunMode}";
            MediaPipeStatusText = $"Background: {statusName} preview | " + alphaRunMode;
            AppendBackgroundDiagnosticLog($"render success | mode={statusName} | photo=\"{targetPhoto.Path}\" | {alphaRunMode}");
        }
        catch (Exception ex)
        {
            ClearBackgroundPreview();
            MediaPipeStatusText = "Background: failed | " + ex.Message;
            AppendBackgroundDiagnosticLog($"render failed | mode={statusName} | photo=\"{targetPhoto.Path}\" | error=\"{ex.Message}\"");
        }
    }

    private async Task<string?> GetOrCreatePersonAlphaPathAsync(PhotoItem targetPhoto)
    {
        if (IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet))
        {
            AppendBackgroundDiagnosticLog($"alpha cache hit | photo=\"{targetPhoto.Path}\" | alpha=\"{_personAlphaPath}\"");
            return _personAlphaPath;
        }

        if (IsCachedPersonAlphaFailure(targetPhoto))
        {
            MediaPipeStatusText = _personAlphaFailureStatus ?? "Background: alpha failed";
            AppendBackgroundDiagnosticLog($"alpha cached failure | photo=\"{targetPhoto.Path}\" | status=\"{_personAlphaFailureStatus}\"");
            return null;
        }

        await _personAlphaCreateGate.WaitAsync();
        try
        {
            if (IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet))
            {
                AppendBackgroundDiagnosticLog($"alpha cache hit after wait | photo=\"{targetPhoto.Path}\" | alpha=\"{_personAlphaPath}\"");
                return _personAlphaPath;
            }

            if (IsCachedPersonAlphaFailure(targetPhoto))
            {
                MediaPipeStatusText = _personAlphaFailureStatus ?? "Background: alpha failed";
                AppendBackgroundDiagnosticLog($"alpha cached failure after wait | photo=\"{targetPhoto.Path}\" | status=\"{_personAlphaFailureStatus}\"");
                return null;
            }

            string? preparedInputPath = IsCachedBiRefNetPreparedInputValid(targetPhoto)
                ? _biRefNetPreparedInputPath
                : await PrepareBiRefNetInputForPhotoAsync(targetPhoto, reportStatus: false);
            if (string.IsNullOrWhiteSpace(preparedInputPath))
            {
                AppendBackgroundDiagnosticLog($"alpha input not ready | photo=\"{targetPhoto.Path}\"");
            }

            string outputDirectory = Path.Combine(BiRefNetOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "_background");
            AppendBackgroundDiagnosticLog($"alpha run start | photo=\"{targetPhoto.Path}\" | preparedInput=\"{preparedInputPath ?? string.Empty}\" | output=\"{outputDirectory}\"");
            BiRefNetMattingRunRequest request = new(
                _appConfig.MediaPipe.HelperRuntime,
                Path.Combine(AppContext.BaseDirectory, "Tools", "BiRefNet", "birefnet_helper.py"),
                targetPhoto.Path,
                preparedInputPath,
                outputDirectory,
                "ZhengPeng7/BiRefNet_lite-matting",
                1024,
                BiRefNetInputSharpenStrength,
                "auto");

            using CancellationTokenSource timeout = new(BiRefNetAlphaTimeout);
            BiRefNetMattingRunResult result = await BiRefNetMattingService.RunAsync(
                request,
                timeout.Token);

            if (!result.Succeeded)
            {
                AppendBackgroundDiagnosticLog($"alpha run failed | photo=\"{targetPhoto.Path}\" | status=\"{result.SummaryText}\"");
                CachePersonAlphaFailure(targetPhoto, result.SummaryText);
                return null;
            }

            CachePersonAlphaArtifact(outputDirectory, targetPhoto.Path, PersonAlphaEngineBiRefNet, result.RunMode);
            AppendBackgroundDiagnosticLog($"alpha run success | photo=\"{targetPhoto.Path}\" | alpha=\"{_personAlphaPath}\" | mode={result.RunMode}");
            return IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet) ? _personAlphaPath : null;
        }
        catch (OperationCanceledException)
        {
            AppendBackgroundDiagnosticLog($"alpha run timeout | photo=\"{targetPhoto.Path}\"");
            CachePersonAlphaFailure(targetPhoto, "BiRefNet: failed | timeout");
            return null;
        }
        finally
        {
            _personAlphaCreateGate.Release();
        }
    }

    private async Task<string?> PrepareBiRefNetInputForPhotoAsync(PhotoItem targetPhoto, bool reportStatus)
    {
        if (IsCachedBiRefNetPreparedInputValid(targetPhoto))
        {
            return _biRefNetPreparedInputPath;
        }

        await _biRefNetInputPrepareGate.WaitAsync();
        try
        {
            if (IsCachedBiRefNetPreparedInputValid(targetPhoto))
            {
                return _biRefNetPreparedInputPath;
            }

            string outputDirectory = Path.Combine(BiRefNetOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "_input");
            BiRefNetInputPrepareRequest request = new(
                _appConfig.MediaPipe.HelperRuntime,
                Path.Combine(AppContext.BaseDirectory, "Tools", "BiRefNet", "birefnet_helper.py"),
                targetPhoto.Path,
                outputDirectory,
                1024,
                BiRefNetInputSharpenStrength);

            if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                MediaPipeStatusText = "BiRefNet: input 1024...";
            }

            using CancellationTokenSource timeout = new(BiRefNetInputPrepareTimeout);
            BiRefNetInputPrepareResult result = await BiRefNetMattingService.PrepareInputAsync(
                request,
                timeout.Token);

            if (!result.Succeeded || string.IsNullOrWhiteSpace(result.InputPath))
            {
                if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
                {
                    MediaPipeStatusText = result.SummaryText;
                }

                return null;
            }

            _biRefNetPreparedInputPath = result.InputPath;
            _biRefNetPreparedInputPhotoPath = targetPhoto.Path;
            _biRefNetPreparedInputSize = request.InferenceSize;
            _biRefNetPreparedInputSharpen = request.InputSharpen;

            if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                MediaPipeStatusText = result.SummaryText;
            }

            return _biRefNetPreparedInputPath;
        }
        catch (Exception ex)
        {
            if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                MediaPipeStatusText = "BiRefNet: input failed | " + ex.Message;
            }

            return null;
        }
        finally
        {
            _biRefNetInputPrepareGate.Release();
        }
    }

    private bool IsCachedPersonAlphaValid(PhotoItem targetPhoto, string requiredEngine)
    {
        return !string.IsNullOrWhiteSpace(_personAlphaPath) &&
               File.Exists(_personAlphaPath) &&
               string.Equals(_personAlphaPhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(_personAlphaEngine, requiredEngine, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsCachedPersonAlphaFailure(PhotoItem targetPhoto)
    {
        return !string.IsNullOrWhiteSpace(_personAlphaFailurePhotoPath) &&
               string.Equals(_personAlphaFailurePhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase);
    }

    private void CachePersonAlphaFailure(PhotoItem targetPhoto, string status)
    {
        _personAlphaFailurePhotoPath = targetPhoto.Path;
        _personAlphaFailureStatus = status;
        MediaPipeStatusText = status;
        AppendBackgroundDiagnosticLog($"alpha failure cached | photo=\"{targetPhoto.Path}\" | status=\"{status}\"");
    }

    private bool IsCachedBiRefNetPreparedInputValid(PhotoItem targetPhoto)
    {
        return !string.IsNullOrWhiteSpace(_biRefNetPreparedInputPath) &&
               File.Exists(_biRefNetPreparedInputPath) &&
               string.Equals(_biRefNetPreparedInputPhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase) &&
               _biRefNetPreparedInputSize == 1024 &&
               _biRefNetPreparedInputSharpen == BiRefNetInputSharpenStrength;
    }

    private async Task PrepareBackgroundReplacementResourcesAsync(PhotoItem targetPhoto, bool reportStatus)
    {
        int prepareVersion = Interlocked.Increment(ref _backgroundResourcePrepareVersion);
        if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
        {
            MediaPipeStatusText = "Background: preparing resources...";
        }

        await _backgroundResourcePrepareGate.WaitAsync();
        try
        {
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
                prepareVersion != _backgroundResourcePrepareVersion)
            {
                return;
            }

            QueuePreviewProxy1200Build(targetPhoto);
            string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
            if (string.IsNullOrWhiteSpace(alphaPath))
            {
                if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
                {
                    MediaPipeStatusText = _personAlphaFailureStatus ?? "Background: alpha not ready";
                }

                return;
            }

            if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
                prepareVersion != _backgroundResourcePrepareVersion)
            {
                return;
            }

            if (TryGetBackgroundDragPreviewSource(targetPhoto, out BitmapSource previewSource, out _, out _))
            {
                _ = GetOrCreateRefinedPersonAlphaMask(alphaPath, previewSource.PixelWidth, previewSource.PixelHeight);
                if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
                {
                    MediaPipeStatusText = "Background: preview resources ready";
                }

                return;
            }

            if (reportStatus && ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                MediaPipeStatusText = "Background: alpha ready | 1200 preview building";
            }
        }
        finally
        {
            _backgroundResourcePrepareGate.Release();
        }
    }

    private void CachePersonAlphaArtifact(string outputDirectory, string photoPath, string engine, string? runMode = null)
    {
        string alphaJsonPath = Path.Combine(outputDirectory, "person_alpha.json");
        string? alphaPath = null;
        if (File.Exists(alphaJsonPath))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(alphaJsonPath));
            if (document.RootElement.TryGetProperty("alpha_path", out JsonElement alphaPathElement))
            {
                alphaPath = alphaPathElement.GetString();
            }
        }

        alphaPath ??= Path.Combine(outputDirectory, "person_alpha.png");
        if (!File.Exists(alphaPath))
        {
            ClearPersonAlphaCache();
            return;
        }

        _personAlphaPath = alphaPath;
        _personAlphaPhotoPath = photoPath;
        _personAlphaEngine = engine;
        _personAlphaRunMode = runMode;
        _personAlphaFailurePhotoPath = null;
        _personAlphaFailureStatus = null;
        ClearRefinedPersonAlphaCache();
        ClearLiquifyTensionCache();
        AppendBackgroundDiagnosticLog($"alpha artifact cached | photo=\"{photoPath}\" | alpha=\"{alphaPath}\" | engine={engine} | mode={runMode ?? string.Empty}");
    }

    private void ClearPersonAlphaCache()
    {
        AppendBackgroundDiagnosticLog("alpha cache cleared");
        _personAlphaPath = null;
        _personAlphaPhotoPath = null;
        _personAlphaEngine = null;
        _personAlphaRunMode = null;
        _personAlphaFailurePhotoPath = null;
        _personAlphaFailureStatus = null;
        Interlocked.Increment(ref _backgroundResourcePrepareVersion);
        ClearRefinedPersonAlphaCache();
        ClearLiquifyTensionCache();
    }

    private string GetBackgroundAlphaState(PhotoItem targetPhoto)
    {
        if (IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet))
        {
            return $"ready:{_personAlphaPath}";
        }

        if (IsCachedPersonAlphaFailure(targetPhoto))
        {
            return $"failed:{_personAlphaFailureStatus}";
        }

        if (!string.IsNullOrWhiteSpace(_personAlphaPath))
        {
            return $"other-photo:{_personAlphaPhotoPath}";
        }

        return "none";
    }

    private static void AppendBackgroundDiagnosticLog(string message)
    {
        try
        {
            string? directory = Path.GetDirectoryName(BackgroundDiagnosticLogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(
                BackgroundDiagnosticLogPath,
                $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostic logging must never block retouching.
        }
    }

    private bool TryGetBackgroundDragPreviewSource(PhotoItem photo, out BitmapSource previewSource, out double frameWidth, out double frameHeight)
    {
        BitmapSource baseSource = GetBackgroundReplacementRenderSource(photo, replaceCurrentBackground: false);
        frameWidth = baseSource.PixelWidth;
        frameHeight = baseSource.PixelHeight;
        if (Math.Max(baseSource.PixelWidth, baseSource.PixelHeight) <= PhotoItem.PreviewProxyLongSide)
        {
            previewSource = baseSource;
            return true;
        }

        if (photo.PreviewProxy1200 is BitmapSource proxy)
        {
            previewSource = proxy;
            return true;
        }

        QueuePreviewProxy1200Build(photo);
        previewSource = null!;
        return false;
    }

    private void SetBackgroundPreview(PhotoItem photo, BitmapSource preview, double frameWidth, double frameHeight)
    {
        _backgroundPreviewImageSource = preview;
        _backgroundPreviewPhotoPath = photo.Path;
        _backgroundPreviewFrameWidth = frameWidth;
        _backgroundPreviewFrameHeight = frameHeight;
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewLayout();
    }

    private void ClearBackgroundPreview()
    {
        if (_backgroundPreviewImageSource is null &&
            _backgroundPreviewPhotoPath is null)
        {
            return;
        }

        _backgroundPreviewImageSource = null;
        _backgroundPreviewPhotoPath = null;
        _backgroundPreviewFrameWidth = 0;
        _backgroundPreviewFrameHeight = 0;
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewLayout();
    }

    private bool TryGetBackgroundPreviewBitmapSource(PhotoItem photo, out BitmapSource preview)
    {
        if (_backgroundPreviewImageSource is not null &&
            string.Equals(_backgroundPreviewPhotoPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            preview = _backgroundPreviewImageSource;
            return true;
        }

        preview = null!;
        return false;
    }

    private bool TryGetBackgroundPreviewFrameSize(PhotoItem photo, out double width, out double height)
    {
        if (_backgroundPreviewImageSource is not null &&
            _backgroundPreviewFrameWidth > 0 &&
            _backgroundPreviewFrameHeight > 0 &&
            string.Equals(_backgroundPreviewPhotoPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            width = _backgroundPreviewFrameWidth;
            height = _backgroundPreviewFrameHeight;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    private BitmapSource BuildBackgroundReplacementPreview(
        BitmapSource source,
        string alphaPath,
        byte solidB,
        byte solidG,
        byte solidR,
        string? imagePath,
        double backgroundOpacity,
        double boundaryProbeStrength,
        double boundaryCleanStrength,
        double edgeBlurStrength,
        double alphaShrinkStrength,
        double softAlphaStrength,
        double alphaGammaStrength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);

        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int sourceStride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[sourceStride * height];
        byte[] alphaPixels = GetOrCreateRefinedPersonAlphaMask(alphaPath, width, height);
        byte[] resultPixels = new byte[sourceStride * height];
        byte[]? backgroundPixels = TryCreateImageBackgroundPixels(
            imagePath,
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            sourceStride);

        bgraSource.CopyPixels(sourcePixels, sourceStride, 0);
        double normalizedBackgroundOpacity = Math.Clamp(backgroundOpacity / 100.0, 0.0, 1.0);
        int backgroundOpacityAlpha = (int)Math.Round(normalizedBackgroundOpacity * 255.0);
        int inverseBackgroundOpacityAlpha = 255 - backgroundOpacityAlpha;
        (byte backgroundB, byte backgroundG, byte backgroundR) = EstimateBackgroundColorBgra32(
            sourcePixels,
            sourceStride,
            alphaPixels,
            width,
            height);
        alphaPixels = ApplyBoundaryProbePreserve(
            sourcePixels,
            sourceStride,
            alphaPixels,
            width,
            height,
            boundaryProbeStrength,
            backgroundB,
            backgroundG,
            backgroundR);
        alphaPixels = ApplyInnerDarkEdgeFill(
            sourcePixels,
            sourceStride,
            alphaPixels,
            width,
            height,
            backgroundB,
            backgroundG,
            backgroundR);
        alphaPixels = ApplyWhiteBackgroundEdgeBlur(
            alphaPixels,
            width,
            height,
            edgeBlurStrength);
        alphaPixels = ApplyAlphaShrink(
            alphaPixels,
            width,
            height,
            alphaShrinkStrength);

        for (int y = 0; y < height; y++)
        {
            int sourceRow = y * sourceStride;
            int alphaRow = y * width;
            for (int x = 0; x < width; x++)
            {
                int sourceIndex = sourceRow + (x * 4);
                int alpha = alphaPixels[alphaRow + x];
                int gammaAlpha = ApplyAlphaGamma(alpha, alphaGammaStrength);
                int outputAlpha = ShapeWhiteBackgroundAlpha(gammaAlpha, softAlphaStrength);
                int inverseAlpha = 255 - outputAlpha;
                ProbeSample localBackground = GetLocalOutsideBackgroundSampleOrDefault(
                    sourcePixels,
                    sourceStride,
                    alphaPixels,
                    width,
                    height,
                    x,
                    y,
                    backgroundB,
                    backgroundG,
                    backgroundR);
                byte replacementB = backgroundPixels is null ? solidB : backgroundPixels[sourceIndex];
                byte replacementG = backgroundPixels is null ? solidG : backgroundPixels[sourceIndex + 1];
                byte replacementR = backgroundPixels is null ? solidR : backgroundPixels[sourceIndex + 2];

                byte fullReplacementB = BlendSolidWithAlphaKeyCleanup(sourcePixels[sourceIndex], localBackground.B, replacementB, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);
                byte fullReplacementG = BlendSolidWithAlphaKeyCleanup(sourcePixels[sourceIndex + 1], localBackground.G, replacementG, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);
                byte fullReplacementR = BlendSolidWithAlphaKeyCleanup(sourcePixels[sourceIndex + 2], localBackground.R, replacementR, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);

                resultPixels[sourceIndex] = BlendReplacementWithOriginal(sourcePixels[sourceIndex], fullReplacementB, backgroundOpacityAlpha, inverseBackgroundOpacityAlpha);
                resultPixels[sourceIndex + 1] = BlendReplacementWithOriginal(sourcePixels[sourceIndex + 1], fullReplacementG, backgroundOpacityAlpha, inverseBackgroundOpacityAlpha);
                resultPixels[sourceIndex + 2] = BlendReplacementWithOriginal(sourcePixels[sourceIndex + 2], fullReplacementR, backgroundOpacityAlpha, inverseBackgroundOpacityAlpha);
                resultPixels[sourceIndex + 3] = 255;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            sourceStride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource CreateBackgroundRenderThreadSource(BitmapSource source)
    {
        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] pixels = new byte[stride * height];
        bgraSource.CopyPixels(pixels, stride, 0);

        BitmapSource clone = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            pixels,
            stride);
        clone.Freeze();
        return clone;
    }

    private static byte[]? TryCreateImageBackgroundPixels(
        string? imagePath,
        int width,
        int height,
        double dpiX,
        double dpiY,
        int stride)
    {
        if (string.IsNullOrWhiteSpace(imagePath) ||
            !File.Exists(imagePath) ||
            width <= 0 ||
            height <= 0)
        {
            return null;
        }

        try
        {
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            double scale = Math.Max(width / (double)image.PixelWidth, height / (double)image.PixelHeight);
            double renderWidth = image.PixelWidth * scale;
            double renderHeight = image.PixelHeight * scale;
            double offsetX = (width - renderWidth) * 0.5;
            double offsetY = (height - renderHeight) * 0.5;

            DrawingVisual visual = new();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawImage(image, new Rect(offsetX, offsetY, renderWidth, renderHeight));
            }

            RenderTargetBitmap rendered = new(
                width,
                height,
                dpiX,
                dpiY,
                PixelFormats.Pbgra32);
            rendered.Render(visual);
            BitmapSource bgraBackground = EnsureBitmapFormat(rendered, PixelFormats.Bgra32);
            byte[] pixels = new byte[stride * height];
            bgraBackground.CopyPixels(pixels, stride, 0);
            return pixels;
        }
        catch
        {
            return null;
        }
    }

    private bool IsCurrentBackgroundReplacementAlreadyApplied(string historyDetail)
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, BackgroundReplacementHistoryTitle, StringComparison.Ordinal) &&
               string.Equals(_editorUndoHistory[^1].Detail, historyDetail, StringComparison.Ordinal);
    }

    private bool IsCurrentHistoryBackgroundReplacement()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, BackgroundReplacementHistoryTitle, StringComparison.Ordinal) &&
               (_editorUndoHistory[^1].Detail.StartsWith(WhiteBackgroundHistoryDetail, StringComparison.Ordinal) ||
                _editorUndoHistory[^1].Detail.StartsWith(GrayBackgroundHistoryDetail, StringComparison.Ordinal) ||
                _editorUndoHistory[^1].Detail.StartsWith(ColorBackgroundHistoryDetail, StringComparison.Ordinal) ||
                _editorUndoHistory[^1].Detail.StartsWith(ImageBackgroundHistoryDetail, StringComparison.Ordinal));
    }

    private static bool IsBackgroundReplacementHistory(EditorHistoryState history)
    {
        return string.Equals(history.Title, BackgroundReplacementHistoryTitle, StringComparison.Ordinal) &&
               (history.Detail.StartsWith(WhiteBackgroundHistoryDetail, StringComparison.Ordinal) ||
                history.Detail.StartsWith(GrayBackgroundHistoryDetail, StringComparison.Ordinal) ||
                history.Detail.StartsWith(ColorBackgroundHistoryDetail, StringComparison.Ordinal) ||
                history.Detail.StartsWith(ImageBackgroundHistoryDetail, StringComparison.Ordinal));
    }

    private static bool IsBackgroundResetHistory(EditorHistoryState history)
    {
        return string.Equals(history.Title, BackgroundReplacementHistoryTitle, StringComparison.Ordinal) &&
               string.Equals(history.Detail, BackgroundResetHistoryDetail, StringComparison.Ordinal);
    }

    private bool HasActiveBackgroundHistory()
    {
        for (int i = _editorUndoHistory.Count - 1; i >= 0; i--)
        {
            EditorHistoryState history = _editorUndoHistory[i];
            if (IsBackgroundResetHistory(history))
            {
                return false;
            }

            if (IsBackgroundReplacementHistory(history))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateBackgroundHistoryResetState()
    {
        if (BackgroundRetouchTab is not null)
        {
            BackgroundRetouchTab.CanResetBackgroundTab = HasActiveBackgroundHistory();
        }
    }

    private async Task TryResetBackgroundHistoryAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || !HasActiveBackgroundHistory())
        {
            UpdateBackgroundHistoryResetState();
            return;
        }

        if (!TryGetSafeTabResetSource(
                targetPhoto,
                IsBackgroundReplacementHistory,
                IsBackgroundResetHistory,
                out BitmapSource resetSource,
                out string blockingHistoryTitle))
        {
            MediaPipeStatusText = string.IsNullOrWhiteSpace(blockingHistoryTitle)
                ? "Background: nothing to reset"
                : $"Background: reset blocked to preserve {blockingHistoryTitle}";
            UpdateBackgroundHistoryResetState();
            return;
        }

        MediaPipeStatusText = "Background: rebuilding other tabs...";
        BitmapSource? rebuiltImage = await RebuildConnectedRetouchSectionsAsync(
            targetPhoto,
            resetSource,
            "Background Reset");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || rebuiltImage is null)
        {
            MediaPipeStatusText = "Background: reset cancelled; other tabs could not be rebuilt";
            return;
        }

        targetPhoto.SetAdjustedImage(rebuiltImage);
        BackgroundRetouchTab.ResetAfterHistoryReset();
        ClearBackgroundPreview();
        SetConnectedRetouchSessionBase(targetPhoto, resetSource);
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(BackgroundReplacementHistoryTitle, BackgroundResetHistoryDetail);
        UpdateBackgroundHistoryResetState();
        MediaPipeStatusText = "Background: reset";
    }

    private BitmapSource GetBackgroundReplacementRenderSource(PhotoItem photo, bool replaceCurrentBackground)
    {
        if (replaceCurrentBackground && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            return previous.AdjustedImage ?? photo.BaseImage;
        }

        return GetCurrentDisplayBitmapSource(photo);
    }

    private void ReplaceCurrentBackgroundReplacementHistorySnapshot(PhotoItem photo, string historyDetail)
    {
        if (_editorUndoHistory.Count == 0)
        {
            PushEditorHistorySnapshot(BackgroundReplacementHistoryTitle, historyDetail);
            return;
        }

        _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, BackgroundReplacementHistoryTitle, historyDetail);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(photo, persistToDisk: false);
    }

    private (string DetailPrefix, string StatusName, byte B, byte G, byte R, string? ImagePath) GetActiveBackgroundReplacement()
    {
        if (BackgroundRetouchTab?.IsGrayBackgroundModeActive == true)
        {
            return (GrayBackgroundHistoryDetail, "gray", 150, 150, 150, null);
        }

        if (BackgroundRetouchTab?.IsColorBackgroundModeActive == true &&
            BackgroundRetouchTab.CustomBackgroundBrush is SolidColorBrush colorBrush)
        {
            MediaColor color = colorBrush.Color;
            string detail = $"{ColorBackgroundHistoryDetail} #{color.R:X2}{color.G:X2}{color.B:X2}";
            return (detail, "color", color.B, color.G, color.R, null);
        }

        if (BackgroundRetouchTab?.IsImageBackgroundModeActive == true &&
            !string.IsNullOrWhiteSpace(BackgroundRetouchTab.SelectedBackgroundImagePath) &&
            File.Exists(BackgroundRetouchTab.SelectedBackgroundImagePath))
        {
            string fileName = Path.GetFileName(BackgroundRetouchTab.SelectedBackgroundImagePath);
            return ($"{ImageBackgroundHistoryDetail} {fileName}", "image", 255, 255, 255, BackgroundRetouchTab.SelectedBackgroundImagePath);
        }

        return (WhiteBackgroundHistoryDetail, "white", 255, 255, 255, null);
    }

    private static string CreateBackgroundReplacementHistoryDetail(
        string detailPrefix,
        double backgroundOpacity,
        double boundaryProbeStrength,
        double boundaryCleanStrength,
        double edgeBlurStrength,
        double alphaShrinkStrength,
        double softAlphaStrength,
        double alphaGammaStrength)
    {
        double opacity = Math.Clamp(Math.Round(backgroundOpacity), 0, 100);
        double edge = Math.Clamp(Math.Round(boundaryProbeStrength), 0, 100);
        double clean = Math.Clamp(Math.Round(boundaryCleanStrength), 0, 100);
        double blur = Math.Clamp(Math.Round(edgeBlurStrength), 0, 100);
        double shrink = Math.Clamp(Math.Round(alphaShrinkStrength), 0, 100);
        double soft = Math.Clamp(Math.Round(softAlphaStrength), 0, 100);
        double gamma = Math.Clamp(Math.Round(alphaGammaStrength), 0, 100);
        return $"{detailPrefix} | Source Original | Opacity {opacity:0} | Edge {edge:0} | Clean {clean:0} | Blur {blur:0} | Shrink {shrink:0} | Soft {soft:0} | Gamma {gamma:0}";
    }

    private static int ApplyAlphaGamma(int alpha, double alphaGammaStrength)
    {
        if (alpha <= 0 || alpha >= 255)
        {
            return alpha;
        }

        double normalizedStrength = Math.Clamp(alphaGammaStrength / 100.0, 0.0, 1.0);
        if (normalizedStrength <= 0.001)
        {
            return alpha;
        }

        double gamma = 1.0 - ((1.0 - WhiteBackgroundAlphaGammaMinimum) * normalizedStrength);
        double normalizedAlpha = Math.Clamp(alpha / 255.0, 0.0, 1.0);
        return (int)Math.Clamp(Math.Round(Math.Pow(normalizedAlpha, gamma) * 255.0), 0, 255);
    }

    private static int ShapeWhiteBackgroundAlpha(int alpha, double softAlphaStrength)
    {
        if (alpha <= 1)
        {
            return 0;
        }

        double normalizedSoftAlpha = Math.Clamp(softAlphaStrength / 100.0, 0.0, 1.0);
        int shapedAlpha = alpha;
        if (alpha >= WhiteBackgroundAlphaHighCutoff)
        {
            shapedAlpha = 255;
        }

        if (normalizedSoftAlpha <= 0.001)
        {
            return shapedAlpha;
        }

        return (int)Math.Clamp(Math.Round(shapedAlpha + ((alpha - shapedAlpha) * normalizedSoftAlpha)), 0, 255);
    }

    private static byte[] ApplyWhiteBackgroundEdgeBlur(
        byte[] alphaPixels,
        int width,
        int height,
        double edgeBlurStrength)
    {
        double normalizedStrength = Math.Clamp(edgeBlurStrength / 100.0, 0.0, 1.0);
        if (normalizedStrength <= 0.001 ||
            alphaPixels.Length == 0 ||
            width <= 0 ||
            height <= 0)
        {
            return alphaPixels;
        }

        bool[] supportMask = BuildBinaryMask(alphaPixels, WhiteBackgroundAlphaLowCutoff);
        bool[] dilatedMask = DilateBinaryMask(supportMask, width, height, WhiteBackgroundEdgeBlurRadius);
        bool[] erodedMask = ErodeBinaryMask(supportMask, width, height, WhiteBackgroundEdgeBlurRadius);
        byte[] blurredAlpha = BoxBlurGray8(alphaPixels, width, height, WhiteBackgroundEdgeBlurRadius);
        byte[] result = (byte[])alphaPixels.Clone();

        for (int i = 0; i < result.Length; i++)
        {
            if (dilatedMask[i] == erodedMask[i])
            {
                continue;
            }

            int original = alphaPixels[i];
            int blurred = blurredAlpha[i];
            result[i] = (byte)Math.Clamp((int)Math.Round(original + ((blurred - original) * normalizedStrength)), 0, 255);
        }

        return result;
    }

    private static byte[] ApplyAlphaShrink(
        byte[] alphaPixels,
        int width,
        int height,
        double shrinkStrength)
    {
        double normalizedStrength = Math.Clamp(shrinkStrength / 100.0, 0.0, 1.0);
        if (normalizedStrength <= 0.001 ||
            alphaPixels.Length == 0 ||
            width <= 0 ||
            height <= 0)
        {
            return alphaPixels;
        }

        double radiusValue = normalizedStrength * 3.0;
        int radius = Math.Max(1, (int)Math.Ceiling(radiusValue));
        bool[] supportMask = BuildBinaryMask(alphaPixels, WhiteBackgroundAlphaLowCutoff);
        bool[] erodedMask = ErodeBinaryMask(supportMask, width, height, radius);
        double blend = Math.Clamp(radiusValue / radius, 0.0, 1.0);
        byte[] result = (byte[])alphaPixels.Clone();

        for (int i = 0; i < result.Length; i++)
        {
            if (erodedMask[i])
            {
                continue;
            }

            int original = alphaPixels[i];
            result[i] = (byte)Math.Clamp((int)Math.Round(original * (1.0 - blend)), 0, 255);
        }

        return result;
    }

    private static byte[] ApplyBoundaryProbePreserve(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height,
        double strength,
        byte backgroundB,
        byte backgroundG,
        byte backgroundR)
    {
        double normalizedStrength = Math.Clamp(strength / 100.0, 0.0, 1.0);
        if (normalizedStrength <= 0.001)
        {
            return alphaPixels;
        }

        bool[] supportMask = BuildBinaryMask(alphaPixels, WhiteBackgroundProbeForegroundThreshold);
        bool[] candidateMask = DilateBinaryMask(supportMask, width, height, WhiteBackgroundProbeCandidateRadius);
        bool[] innerMask = ErodeBinaryMask(supportMask, width, height, WhiteBackgroundProbeInnerRadius);
        float[] localScores = BuildBoundaryProbeLocalScores(
            sourcePixels,
            stride,
            alphaPixels,
            candidateMask,
            innerMask,
            width,
            height,
            backgroundB,
            backgroundG,
            backgroundR);
        float[] boxScores = BuildBoundaryProbeBoxScores(
            alphaPixels,
            candidateMask,
            innerMask,
            localScores,
            width,
            height);
        byte[] result = (byte[])alphaPixels.Clone();

        for (int y = 1; y < height - 1; y++)
        {
            int rowOffset = y * width;
            for (int x = 1; x < width - 1; x++)
            {
                int index = rowOffset + x;
                int alpha = alphaPixels[index];
                if (!candidateMask[index] || innerMask[index] || alpha >= 250)
                {
                    continue;
                }

                double localScore = localScores[index];
                double boxScore = boxScores[index];
                double score = localScore * (0.35 + (boxScore * 0.65));
                if (boxScore >= 0.45 && localScore >= 0.20)
                {
                    score = Math.Max(score, localScore * 0.85);
                }

                if (score <= 0.001)
                {
                    continue;
                }

                int boost = (int)Math.Round((255 - alpha) * normalizedStrength * score);
                result[index] = (byte)Math.Clamp(alpha + boost, alpha, 255);
            }
        }

        return result;
    }

    private static float[] BuildBoundaryProbeLocalScores(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        bool[] candidateMask,
        bool[] innerMask,
        int width,
        int height,
        byte backgroundB,
        byte backgroundG,
        byte backgroundR)
    {
        float[] localScores = new float[alphaPixels.Length];
        for (int y = 1; y < height - 1; y++)
        {
            int rowOffset = y * width;
            for (int x = 1; x < width - 1; x++)
            {
                int index = rowOffset + x;
                if (!candidateMask[index] || innerMask[index] || alphaPixels[index] >= 250)
                {
                    continue;
                }

                double score = CalculateBoundaryProbePreserveScore(
                    sourcePixels,
                    stride,
                    alphaPixels,
                    width,
                    height,
                    x,
                    y,
                    backgroundB,
                    backgroundG,
                    backgroundR);
                localScores[index] = (float)score;
            }
        }

        return localScores;
    }

    private static float[] BuildBoundaryProbeBoxScores(
        byte[] alphaPixels,
        bool[] candidateMask,
        bool[] innerMask,
        float[] localScores,
        int width,
        int height)
    {
        float[] scoreSums = new float[alphaPixels.Length];
        float[] weightSums = new float[alphaPixels.Length];
        int boxHalf = Math.Max(1, WhiteBackgroundProbeBoxSize / 2);
        int bucketColumns = Math.Max(1, ((width - 1) / WhiteBackgroundProbeBoxStride) + 1);
        int bucketRows = Math.Max(1, ((height - 1) / WhiteBackgroundProbeBoxStride) + 1);
        bool[] queuedBuckets = new bool[bucketColumns * bucketRows];

        for (int y = 1; y < height - 1; y++)
        {
            int rowOffset = y * width;
            for (int x = 1; x < width - 1; x++)
            {
                int index = rowOffset + x;
                if (!candidateMask[index] || innerMask[index] || alphaPixels[index] >= 250)
                {
                    continue;
                }

                int bucketX = Math.Clamp((x + (WhiteBackgroundProbeBoxStride / 2)) / WhiteBackgroundProbeBoxStride, 0, bucketColumns - 1);
                int bucketY = Math.Clamp((y + (WhiteBackgroundProbeBoxStride / 2)) / WhiteBackgroundProbeBoxStride, 0, bucketRows - 1);
                queuedBuckets[(bucketY * bucketColumns) + bucketX] = true;
            }
        }

        for (int bucketY = 0; bucketY < bucketRows; bucketY++)
        {
            for (int bucketX = 0; bucketX < bucketColumns; bucketX++)
            {
                if (!queuedBuckets[(bucketY * bucketColumns) + bucketX])
                {
                    continue;
                }

                int centerX = Math.Clamp(bucketX * WhiteBackgroundProbeBoxStride, 0, width - 1);
                int centerY = Math.Clamp(bucketY * WhiteBackgroundProbeBoxStride, 0, height - 1);
                int left = Math.Max(1, centerX - boxHalf);
                int right = Math.Min(width - 2, centerX + boxHalf);
                int top = Math.Max(1, centerY - boxHalf);
                int bottom = Math.Min(height - 2, centerY + boxHalf);
                double boxScore = CalculateBoundaryProbeBoxScore(
                    candidateMask,
                    innerMask,
                    localScores,
                    width,
                    left,
                    top,
                    right,
                    bottom);
                if (boxScore <= 0.001)
                {
                    continue;
                }

                AccumulateBoundaryProbeBoxScore(
                    alphaPixels,
                    candidateMask,
                    innerMask,
                    width,
                    left,
                    top,
                    right,
                    bottom,
                    centerX,
                    centerY,
                    boxHalf,
                    (float)boxScore,
                    scoreSums,
                    weightSums);
            }
        }

        float[] boxScores = new float[alphaPixels.Length];
        for (int i = 0; i < boxScores.Length; i++)
        {
            if (weightSums[i] > 0.0001f)
            {
                boxScores[i] = Math.Clamp(scoreSums[i] / weightSums[i], 0.0f, 1.0f);
            }
        }

        return boxScores;
    }

    private static double CalculateBoundaryProbeBoxScore(
        bool[] candidateMask,
        bool[] innerMask,
        float[] localScores,
        int width,
        int left,
        int top,
        int right,
        int bottom)
    {
        int candidateCount = 0;
        int subjectCount = 0;
        int strongCount = 0;
        int connectedCount = 0;
        int minX = right;
        int maxX = left;
        int minY = bottom;
        int maxY = top;
        double scoreSum = 0.0;

        for (int y = top; y <= bottom; y++)
        {
            int rowOffset = y * width;
            for (int x = left; x <= right; x++)
            {
                int index = rowOffset + x;
                if (!candidateMask[index] || innerMask[index])
                {
                    continue;
                }

                candidateCount++;
                double localScore = localScores[index];
                if (localScore < 0.15)
                {
                    continue;
                }

                subjectCount++;
                scoreSum += localScore;
                if (localScore >= 0.55)
                {
                    strongCount++;
                }

                if (HasNearbyBoundarySubjectScore(localScores, width, x, y, left, top, right, bottom))
                {
                    connectedCount++;
                }

                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
            }
        }

        if (candidateCount == 0 || subjectCount < 6)
        {
            return 0.0;
        }

        int span = Math.Max(maxX - minX, maxY - minY);
        if (subjectCount < 10 && span < 18)
        {
            return 0.0;
        }

        double averageSubjectScore = scoreSum / subjectCount;
        double subjectRatio = subjectCount / (double)candidateCount;
        double densityScore = Math.Clamp(subjectCount / 90.0, 0.0, 1.0);
        double ratioScore = Math.Clamp((subjectRatio - 0.06) / 0.24, 0.0, 1.0);
        double continuityScore = connectedCount / (double)subjectCount;
        double spanScore = Math.Clamp((span - 18.0) / 92.0, 0.0, 1.0);
        double strongScore = Math.Clamp(strongCount / 24.0, 0.0, 1.0);
        double structureScore = Math.Clamp(
            (continuityScore * 0.34) +
            (spanScore * 0.28) +
            (Math.Max(densityScore, ratioScore) * 0.24) +
            (strongScore * 0.14),
            0.0,
            1.0);

        return Math.Clamp(averageSubjectScore * (0.45 + (structureScore * 0.75)), 0.0, 1.0);
    }

    private static bool HasNearbyBoundarySubjectScore(
        float[] localScores,
        int width,
        int x,
        int y,
        int left,
        int top,
        int right,
        int bottom)
    {
        const float NeighborScoreThreshold = 0.15f;
        for (int dy = -2; dy <= 2; dy++)
        {
            int sampleY = y + dy;
            if (sampleY < top || sampleY > bottom)
            {
                continue;
            }

            int rowOffset = sampleY * width;
            for (int dx = -2; dx <= 2; dx++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int sampleX = x + dx;
                if (sampleX < left || sampleX > right)
                {
                    continue;
                }

                if (localScores[rowOffset + sampleX] >= NeighborScoreThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void AccumulateBoundaryProbeBoxScore(
        byte[] alphaPixels,
        bool[] candidateMask,
        bool[] innerMask,
        int width,
        int left,
        int top,
        int right,
        int bottom,
        int centerX,
        int centerY,
        int boxHalf,
        float boxScore,
        float[] scoreSums,
        float[] weightSums)
    {
        double inverseBoxHalf = 1.0 / Math.Max(1, boxHalf);
        for (int y = top; y <= bottom; y++)
        {
            int rowOffset = y * width;
            for (int x = left; x <= right; x++)
            {
                int index = rowOffset + x;
                if (!candidateMask[index] || innerMask[index] || alphaPixels[index] >= 250)
                {
                    continue;
                }

                double normalizedDistance = Math.Max(Math.Abs(x - centerX), Math.Abs(y - centerY)) * inverseBoxHalf;
                double weight = Math.Clamp(1.0 - normalizedDistance, 0.05, 1.0);
                scoreSums[index] += (float)(boxScore * weight);
                weightSums[index] += (float)weight;
            }
        }
    }

    private static double CalculateBoundaryProbePreserveScore(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height,
        int x,
        int y,
        byte backgroundB,
        byte backgroundG,
        byte backgroundR)
    {
        int index = (y * width) + x;
        int sourceIndex = (y * stride) + (x * 4);
        byte b = sourcePixels[sourceIndex];
        byte g = sourcePixels[sourceIndex + 1];
        byte r = sourcePixels[sourceIndex + 2];

        double backgroundDistance = ColorDistance(b, g, r, backgroundB, backgroundG, backgroundR);
        double backgroundLuminance = Luminance(backgroundB, backgroundG, backgroundR);
        double currentLuminance = Luminance(b, g, r);
        double darkScore = Math.Clamp((backgroundLuminance - currentLuminance - 10.0) / 54.0, 0.0, 1.0);
        double colorScore = Math.Clamp((backgroundDistance - 18.0) / 72.0, 0.0, 1.0);
        double score = Math.Max(darkScore, colorScore);

        int gradientX = alphaPixels[index + 1] - alphaPixels[index - 1];
        int gradientY = alphaPixels[index + width] - alphaPixels[index - width];
        double gradientLength = Math.Sqrt((gradientX * gradientX) + (gradientY * gradientY));
        if (gradientLength >= 1.0)
        {
            double normalX = gradientX / gradientLength;
            double normalY = gradientY / gradientLength;
            if (TrySampleProbeAverage(sourcePixels, stride, width, height, x, y, normalX, normalY, out ProbeSample insideSample) &&
                TrySampleProbeAverage(sourcePixels, stride, width, height, x, y, -normalX, -normalY, out ProbeSample outsideSample))
            {
                double currentToInside = ColorDistance(b, g, r, insideSample.B, insideSample.G, insideSample.R);
                double currentToOutside = ColorDistance(b, g, r, outsideSample.B, outsideSample.G, outsideSample.R);
                double insideOutsideDistance = ColorDistance(insideSample.B, insideSample.G, insideSample.R, outsideSample.B, outsideSample.G, outsideSample.R);
                if (insideOutsideDistance > 18.0 && currentToInside + 8.0 < currentToOutside)
                {
                    score = Math.Max(score, 0.72);
                }
            }
        }

        if (backgroundDistance < 12.0 && darkScore < 0.08)
        {
            score = 0.0;
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    private static byte[] ApplyInnerDarkEdgeFill(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height,
        byte backgroundB,
        byte backgroundG,
        byte backgroundR)
    {
        bool[] supportMask = BuildBinaryMask(alphaPixels, WhiteBackgroundProbeForegroundThreshold);
        bool[] innerMask = ErodeBinaryMask(supportMask, width, height, WhiteBackgroundInnerFillRadius);
        bool[] fillBand = DilateBinaryMask(innerMask, width, height, WhiteBackgroundInnerFillRadius + 1);
        byte[] result = (byte[])alphaPixels.Clone();

        for (int y = 1; y < height - 1; y++)
        {
            int rowOffset = y * width;
            for (int x = 1; x < width - 1; x++)
            {
                int index = rowOffset + x;
                if (!fillBand[index] ||
                    alphaPixels[index] >= WhiteBackgroundInnerFillAlphaMin ||
                    !HasNearbyInnerSupport(innerMask, width, height, x, y))
                {
                    continue;
                }

                int sourceIndex = (y * stride) + (x * 4);
                byte b = sourcePixels[sourceIndex];
                byte g = sourcePixels[sourceIndex + 1];
                byte r = sourcePixels[sourceIndex + 2];
                if (!IsLikelyDarkSubjectPixel(b, g, r, backgroundB, backgroundG, backgroundR))
                {
                    continue;
                }

                result[index] = WhiteBackgroundInnerFillAlphaMin;
            }
        }

        return result;
    }

    private static bool HasNearbyInnerSupport(bool[] innerMask, int width, int height, int x, int y)
    {
        for (int dy = -WhiteBackgroundInnerFillRadius; dy <= WhiteBackgroundInnerFillRadius; dy++)
        {
            int sampleY = y + dy;
            if (sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            int rowOffset = sampleY * width;
            for (int dx = -WhiteBackgroundInnerFillRadius; dx <= WhiteBackgroundInnerFillRadius; dx++)
            {
                int sampleX = x + dx;
                if (sampleX >= 0 && sampleX < width && innerMask[rowOffset + sampleX])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsLikelyDarkSubjectPixel(byte b, byte g, byte r, byte backgroundB, byte backgroundG, byte backgroundR)
    {
        double backgroundLuminance = Luminance(backgroundB, backgroundG, backgroundR);
        double currentLuminance = Luminance(b, g, r);
        double backgroundDistance = ColorDistance(b, g, r, backgroundB, backgroundG, backgroundR);
        return backgroundLuminance - currentLuminance > 28.0 || backgroundDistance > 58.0;
    }

    private static bool TrySampleProbeAverage(
        byte[] sourcePixels,
        int stride,
        int width,
        int height,
        int x,
        int y,
        double normalX,
        double normalY,
        out ProbeSample sample)
    {
        long sumB = 0;
        long sumG = 0;
        long sumR = 0;
        int count = 0;

        for (int distance = 3; distance <= WhiteBackgroundProbeHalfLength; distance += 2)
        {
            int sampleX = (int)Math.Round(x + (normalX * distance));
            int sampleY = (int)Math.Round(y + (normalY * distance));
            if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            int sourceIndex = (sampleY * stride) + (sampleX * 4);
            sumB += sourcePixels[sourceIndex];
            sumG += sourcePixels[sourceIndex + 1];
            sumR += sourcePixels[sourceIndex + 2];
            count++;
        }

        if (count == 0)
        {
            sample = default;
            return false;
        }

        sample = new ProbeSample(
            (byte)Math.Clamp((int)Math.Round(sumB / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumG / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumR / (double)count), 0, 255));
        return true;
    }

    private static ProbeSample GetLocalOutsideBackgroundSampleOrDefault(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height,
        int x,
        int y,
        byte fallbackB,
        byte fallbackG,
        byte fallbackR)
    {
        if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
        {
            return new ProbeSample(fallbackB, fallbackG, fallbackR);
        }

        int index = (y * width) + x;
        int gradientX = alphaPixels[index + 1] - alphaPixels[index - 1];
        int gradientY = alphaPixels[index + width] - alphaPixels[index - width];
        double gradientLength = Math.Sqrt((gradientX * gradientX) + (gradientY * gradientY));
        if (gradientLength < 1.0)
        {
            return new ProbeSample(fallbackB, fallbackG, fallbackR);
        }

        double outsideX = -gradientX / gradientLength;
        double outsideY = -gradientY / gradientLength;
        return TrySampleOutsideBackgroundAverage(
            sourcePixels,
            stride,
            alphaPixels,
            width,
            height,
            x,
            y,
            outsideX,
            outsideY,
            out ProbeSample localBackground)
            ? localBackground
            : new ProbeSample(fallbackB, fallbackG, fallbackR);
    }

    private static bool TrySampleOutsideBackgroundAverage(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height,
        int x,
        int y,
        double normalX,
        double normalY,
        out ProbeSample sample)
    {
        long sumB = 0;
        long sumG = 0;
        long sumR = 0;
        int count = 0;

        for (int distance = 3; distance <= WhiteBackgroundProbeHalfLength; distance += 2)
        {
            int sampleX = (int)Math.Round(x + (normalX * distance));
            int sampleY = (int)Math.Round(y + (normalY * distance));
            if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height)
            {
                continue;
            }

            int alpha = alphaPixels[(sampleY * width) + sampleX];
            if (alpha > WhiteBackgroundSampleAlphaMax)
            {
                continue;
            }

            int sourceIndex = (sampleY * stride) + (sampleX * 4);
            sumB += sourcePixels[sourceIndex];
            sumG += sourcePixels[sourceIndex + 1];
            sumR += sourcePixels[sourceIndex + 2];
            count++;
        }

        if (count < 2)
        {
            sample = default;
            return false;
        }

        sample = new ProbeSample(
            (byte)Math.Clamp((int)Math.Round(sumB / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumG / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumR / (double)count), 0, 255));
        return true;
    }

    private static double ColorDistance(byte b1, byte g1, byte r1, byte b2, byte g2, byte r2)
    {
        double db = b1 - b2;
        double dg = g1 - g2;
        double dr = r1 - r2;
        return Math.Sqrt((db * db) + (dg * dg) + (dr * dr));
    }

    private static double Luminance(byte b, byte g, byte r)
    {
        return (0.0722 * b) + (0.7152 * g) + (0.2126 * r);
    }

    private static byte BlendSolidWithAlphaKeyCleanup(byte channel, byte backgroundChannel, byte solidChannel, int sourceAlpha, int outputAlpha, int inverseOutputAlpha, double cleanStrength)
    {
        if (outputAlpha <= 0)
        {
            return solidChannel;
        }

        if (outputAlpha >= 255)
        {
            return channel;
        }

        byte cleanedChannel = RemoveBackgroundContamination(channel, backgroundChannel, sourceAlpha, cleanStrength);
        return BlendOverSolid(cleanedChannel, solidChannel, outputAlpha, inverseOutputAlpha);
    }

    private readonly record struct ProbeSample(byte B, byte G, byte R);

    private static byte RemoveBackgroundContamination(byte channel, byte backgroundChannel, int alpha, double cleanStrength)
    {
        if (alpha >= WhiteBackgroundAlphaHighCutoff)
        {
            return channel;
        }

        double normalizedAlpha = Math.Clamp(alpha / 255.0, 0.10, 1.0);
        double foreground = (channel - (backgroundChannel * (1.0 - normalizedAlpha))) / normalizedAlpha;
        foreground = Math.Clamp(foreground, 0.0, 255.0);

        double baseCleanupStrength = Math.Clamp((WhiteBackgroundAlphaHighCutoff - alpha) / 160.0, 0.0, 1.0);
        double userCleanupStrength = Math.Clamp(cleanStrength / 100.0, 0.0, 1.0);
        double cleanupStrength = Math.Clamp(baseCleanupStrength * (0.55 + (userCleanupStrength * 1.45)), 0.0, 1.0);
        double cleaned = channel + ((foreground - channel) * cleanupStrength);
        return (byte)Math.Clamp((int)Math.Round(cleaned), 0, 255);
    }

    private static byte BlendOverSolid(byte channel, byte solidChannel, int alpha, int inverseAlpha)
    {
        return (byte)(((channel * alpha) + (solidChannel * inverseAlpha) + 127) / 255);
    }

    private static byte BlendReplacementWithOriginal(byte originalChannel, byte replacementChannel, int opacityAlpha, int inverseOpacityAlpha)
    {
        if (opacityAlpha <= 0)
        {
            return originalChannel;
        }

        if (opacityAlpha >= 255)
        {
            return replacementChannel;
        }

        return (byte)(((replacementChannel * opacityAlpha) + (originalChannel * inverseOpacityAlpha) + 127) / 255);
    }

    private static (byte B, byte G, byte R) EstimateBackgroundColorBgra32(
        byte[] sourcePixels,
        int stride,
        byte[] alphaPixels,
        int width,
        int height)
    {
        long sumB = 0;
        long sumG = 0;
        long sumR = 0;
        int count = 0;
        int borderBand = Math.Clamp(Math.Min(width, height) / 24, 12, 48);
        int step = Math.Max(1, Math.Min(width, height) / 360);

        for (int y = 0; y < height; y += step)
        {
            bool inYBorder = y < borderBand || y >= height - borderBand;
            int sourceRow = y * stride;
            int alphaRow = y * width;
            for (int x = 0; x < width; x += step)
            {
                if (!inYBorder && x >= borderBand && x < width - borderBand)
                {
                    continue;
                }

                if (alphaPixels[alphaRow + x] > WhiteBackgroundSampleAlphaMax)
                {
                    continue;
                }

                int sourceIndex = sourceRow + (x * 4);
                sumB += sourcePixels[sourceIndex];
                sumG += sourcePixels[sourceIndex + 1];
                sumR += sourcePixels[sourceIndex + 2];
                count++;
            }
        }

        if (count < 32)
        {
            AddCornerBackgroundSamples(sourcePixels, stride, width, height, ref sumB, ref sumG, ref sumR, ref count);
        }

        if (count == 0)
        {
            return (255, 255, 255);
        }

        return (
            (byte)Math.Clamp((int)Math.Round(sumB / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumG / (double)count), 0, 255),
            (byte)Math.Clamp((int)Math.Round(sumR / (double)count), 0, 255));
    }

    private static void AddCornerBackgroundSamples(
        byte[] sourcePixels,
        int stride,
        int width,
        int height,
        ref long sumB,
        ref long sumG,
        ref long sumR,
        ref int count)
    {
        int sampleWidth = Math.Min(width, Math.Clamp(width / 12, 8, 80));
        int sampleHeight = Math.Min(height, Math.Clamp(height / 12, 8, 80));
        AddCornerBackgroundSample(sourcePixels, stride, 0, 0, sampleWidth, sampleHeight, ref sumB, ref sumG, ref sumR, ref count);
        AddCornerBackgroundSample(sourcePixels, stride, width - sampleWidth, 0, sampleWidth, sampleHeight, ref sumB, ref sumG, ref sumR, ref count);
        AddCornerBackgroundSample(sourcePixels, stride, 0, height - sampleHeight, sampleWidth, sampleHeight, ref sumB, ref sumG, ref sumR, ref count);
        AddCornerBackgroundSample(sourcePixels, stride, width - sampleWidth, height - sampleHeight, sampleWidth, sampleHeight, ref sumB, ref sumG, ref sumR, ref count);
    }

    private static void AddCornerBackgroundSample(
        byte[] sourcePixels,
        int stride,
        int left,
        int top,
        int width,
        int height,
        ref long sumB,
        ref long sumG,
        ref long sumR,
        ref int count)
    {
        int right = Math.Max(left, left + width);
        int bottom = Math.Max(top, top + height);
        for (int y = top; y < bottom; y += 4)
        {
            int sourceRow = y * stride;
            for (int x = left; x < right; x += 4)
            {
                int sourceIndex = sourceRow + (x * 4);
                sumB += sourcePixels[sourceIndex];
                sumG += sourcePixels[sourceIndex + 1];
                sumR += sourcePixels[sourceIndex + 2];
                count++;
            }
        }
    }

    private static BitmapSource LoadBitmapSourceFromFile(string path)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource EnsureAlphaMaskSize(BitmapSource source, int width, int height)
    {
        BitmapSource graySource = EnsureBitmapFormat(source, PixelFormats.Gray8);
        if (graySource.PixelWidth == width && graySource.PixelHeight == height)
        {
            return graySource;
        }

        TransformedBitmap scaled = new(
            graySource,
            new ScaleTransform(
                width / (double)Math.Max(1, graySource.PixelWidth),
                height / (double)Math.Max(1, graySource.PixelHeight)));
        scaled.Freeze();
        return EnsureBitmapFormat(scaled, PixelFormats.Gray8);
    }

    private static BitmapSource EnsureBitmapFormat(BitmapSource source, PixelFormat format)
    {
        if (source.Format == format)
        {
            return source;
        }

        FormatConvertedBitmap converted = new(source, format, null, 0);
        converted.Freeze();
        return converted;
    }

    private static int CalculateStride(int pixelWidth, PixelFormat format)
    {
        return ((pixelWidth * format.BitsPerPixel) + 7) / 8;
    }
}
