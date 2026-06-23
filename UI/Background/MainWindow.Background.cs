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
    private const string WhiteBackgroundHistoryTitle = "Background";
    private const string WhiteBackgroundHistoryDetail = "White background";
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

    private static readonly string BiRefNetOutputRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KRetouchStudio",
        "BiRefNetOutput");

    private static readonly MediaBrush PreviewSurfaceDefaultBrush = CreateFrozenBrush(MediaColor.FromRgb(17, 19, 21), 1.0);

    private MediaBrush _previewSurfaceBackgroundBrush = PreviewSurfaceDefaultBrush;
    private BitmapSource? _backgroundPreviewImageSource;
    private string? _backgroundPreviewPhotoPath;
    private string? _personAlphaPath;
    private string? _personAlphaPhotoPath;
    private string? _personAlphaEngine;
    private string? _personAlphaRunMode;
    private bool _isBackgroundPreviewRunning;
    private bool _hasPendingBackgroundPreviewRequest;

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

    private async void BackgroundRetouchTab_WhiteBackgroundRequested(object? sender, EventArgs e)
    {
        await ApplyWhiteBackgroundPreviewAsync();
    }

    private async void BackgroundRetouchTab_WhiteBackgroundAdjustmentCommitted(object? sender, EventArgs e)
    {
        if (!IsCurrentHistoryWhiteBackground())
        {
            return;
        }

        await ApplyWhiteBackgroundPreviewAsync();
    }

    private async Task ApplyWhiteBackgroundPreviewAsync()
    {
        if (_isBackgroundPreviewRunning)
        {
            _hasPendingBackgroundPreviewRequest = true;
            return;
        }

        _isBackgroundPreviewRunning = true;
        try
        {
            do
            {
                _hasPendingBackgroundPreviewRequest = false;
                await ApplyWhiteBackgroundPreviewCoreAsync();
            }
            while (_hasPendingBackgroundPreviewRequest);
        }
        finally
        {
            _isBackgroundPreviewRunning = false;
        }
    }

    private async Task ApplyWhiteBackgroundPreviewCoreAsync()
    {
        PhotoItem? targetPhoto = SelectedPhoto;
        if (targetPhoto is null)
        {
            MediaPipeStatusText = "Background: load photo first";
            return;
        }

        double boundaryProbeStrength = BackgroundRetouchTab?.BoundaryProbeStrength ?? 0;
        double boundaryCleanStrength = BackgroundRetouchTab?.BoundaryCleanStrength ?? 0;
        string historyDetail = CreateWhiteBackgroundHistoryDetail(boundaryProbeStrength, boundaryCleanStrength);
        if (IsCurrentWhiteBackgroundAlreadyApplied(historyDetail))
        {
            MediaPipeStatusText = "Background: white already applied";
            return;
        }

        MediaPipeStatusText = "Background: white preview...";

        try
        {
            string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
            if (alphaPath is null)
            {
                MediaPipeStatusText = "Background: person alpha not ready";
                return;
            }

            if (!ReferenceEquals(SelectedPhoto, targetPhoto))
            {
                return;
            }

            bool replaceCurrentWhiteBackground = IsCurrentHistoryWhiteBackground();
            BitmapSource source = GetWhiteBackgroundRenderSource(targetPhoto, replaceCurrentWhiteBackground);
            BitmapSource preview = BuildWhiteBackgroundPreview(source, alphaPath, boundaryProbeStrength, boundaryCleanStrength);
            targetPhoto.SetAdjustedImage(preview);
            if (replaceCurrentWhiteBackground)
            {
                ReplaceCurrentWhiteBackgroundHistorySnapshot(targetPhoto, historyDetail);
            }
            else
            {
                PushEditorHistorySnapshot(WhiteBackgroundHistoryTitle, historyDetail);
            }

            UpdatePreviewImageFrame();
            string alphaRunMode = string.IsNullOrWhiteSpace(_personAlphaRunMode)
                ? PersonAlphaEngineBiRefNet
                : $"{PersonAlphaEngineBiRefNet} {_personAlphaRunMode}";
            MediaPipeStatusText = "Background: white preview | " + alphaRunMode;
        }
        catch (Exception ex)
        {
            ClearBackgroundPreview();
            MediaPipeStatusText = "Background: failed | " + ex.Message;
        }
    }

    private async Task<string?> GetOrCreatePersonAlphaPathAsync(PhotoItem targetPhoto)
    {
        if (IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet))
        {
            return _personAlphaPath;
        }

        string outputDirectory = Path.Combine(BiRefNetOutputRoot, DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "_background");
        BiRefNetMattingRunRequest request = new(
            _appConfig.MediaPipe.HelperRuntime,
            Path.Combine(AppContext.BaseDirectory, "Tools", "BiRefNet", "birefnet_helper.py"),
            targetPhoto.Path,
            outputDirectory,
            "ZhengPeng7/BiRefNet_lite-matting",
            1024,
            "auto");

        BiRefNetMattingRunResult result = await BiRefNetMattingService.RunAsync(
            request,
            CancellationToken.None);

        if (!result.Succeeded)
        {
            MediaPipeStatusText = result.SummaryText;
            return null;
        }

        CachePersonAlphaArtifact(outputDirectory, targetPhoto.Path, PersonAlphaEngineBiRefNet, result.RunMode);
        return IsCachedPersonAlphaValid(targetPhoto, PersonAlphaEngineBiRefNet) ? _personAlphaPath : null;
    }

    private bool IsCachedPersonAlphaValid(PhotoItem targetPhoto, string requiredEngine)
    {
        return !string.IsNullOrWhiteSpace(_personAlphaPath) &&
               File.Exists(_personAlphaPath) &&
               string.Equals(_personAlphaPhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(_personAlphaEngine, requiredEngine, StringComparison.OrdinalIgnoreCase);
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
        ClearRefinedPersonAlphaCache();
        ClearLiquifyTensionCache();
    }

    private void ClearPersonAlphaCache()
    {
        _personAlphaPath = null;
        _personAlphaPhotoPath = null;
        _personAlphaEngine = null;
        _personAlphaRunMode = null;
        ClearRefinedPersonAlphaCache();
        ClearLiquifyTensionCache();
    }

    private void SetBackgroundPreview(PhotoItem photo, BitmapSource preview)
    {
        _backgroundPreviewImageSource = preview;
        _backgroundPreviewPhotoPath = photo.Path;
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewImageFrame();
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
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewImageFrame();
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

    private BitmapSource BuildWhiteBackgroundPreview(BitmapSource source, string alphaPath, double boundaryProbeStrength, double boundaryCleanStrength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);

        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int sourceStride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[sourceStride * height];
        byte[] alphaPixels = GetOrCreateRefinedPersonAlphaMask(alphaPath, width, height);
        byte[] resultPixels = new byte[sourceStride * height];

        bgraSource.CopyPixels(sourcePixels, sourceStride, 0);
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

        for (int y = 0; y < height; y++)
        {
            int sourceRow = y * sourceStride;
            int alphaRow = y * width;
            for (int x = 0; x < width; x++)
            {
                int sourceIndex = sourceRow + (x * 4);
                int alpha = alphaPixels[alphaRow + x];
                int outputAlpha = ShapeWhiteBackgroundAlpha(alpha);
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

                resultPixels[sourceIndex] = BlendWhiteWithAlphaKeyCleanup(sourcePixels[sourceIndex], localBackground.B, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);
                resultPixels[sourceIndex + 1] = BlendWhiteWithAlphaKeyCleanup(sourcePixels[sourceIndex + 1], localBackground.G, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);
                resultPixels[sourceIndex + 2] = BlendWhiteWithAlphaKeyCleanup(sourcePixels[sourceIndex + 2], localBackground.R, alpha, outputAlpha, inverseAlpha, boundaryCleanStrength);
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

    private bool IsCurrentWhiteBackgroundAlreadyApplied(string historyDetail)
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, WhiteBackgroundHistoryTitle, StringComparison.Ordinal) &&
               string.Equals(_editorUndoHistory[^1].Detail, historyDetail, StringComparison.Ordinal);
    }

    private bool IsCurrentHistoryWhiteBackground()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, WhiteBackgroundHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(WhiteBackgroundHistoryDetail, StringComparison.Ordinal);
    }

    private BitmapSource GetWhiteBackgroundRenderSource(PhotoItem photo, bool replaceCurrentWhiteBackground)
    {
        return photo.BaseImage;
    }

    private void ReplaceCurrentWhiteBackgroundHistorySnapshot(PhotoItem photo, string historyDetail)
    {
        if (_editorUndoHistory.Count == 0)
        {
            PushEditorHistorySnapshot(WhiteBackgroundHistoryTitle, historyDetail);
            return;
        }

        _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, WhiteBackgroundHistoryTitle, historyDetail);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(photo, persistToDisk: false);
    }

    private static string CreateWhiteBackgroundHistoryDetail(double boundaryProbeStrength, double boundaryCleanStrength)
    {
        double edge = Math.Clamp(Math.Round(boundaryProbeStrength), 0, 100);
        double clean = Math.Clamp(Math.Round(boundaryCleanStrength), 0, 100);
        return $"{WhiteBackgroundHistoryDetail} | Source Original | Edge {edge:0} | Clean {clean:0}";
    }

    private static int ShapeWhiteBackgroundAlpha(int alpha)
    {
        if (alpha <= 1)
        {
            return 0;
        }

        if (alpha >= WhiteBackgroundAlphaHighCutoff)
        {
            return 255;
        }

        return alpha;
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

    private static byte BlendWhiteWithAlphaKeyCleanup(byte channel, byte backgroundChannel, int sourceAlpha, int outputAlpha, int inverseOutputAlpha, double cleanStrength)
    {
        if (outputAlpha <= 0)
        {
            return 255;
        }

        if (outputAlpha >= 255)
        {
            return channel;
        }

        byte cleanedChannel = RemoveBackgroundContamination(channel, backgroundChannel, sourceAlpha, cleanStrength);
        return BlendOverWhite(cleanedChannel, outputAlpha, inverseOutputAlpha);
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

    private static byte BlendOverWhite(byte channel, int alpha, int inverseAlpha)
    {
        return (byte)(((channel * alpha) + (255 * inverseAlpha) + 127) / 255);
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
