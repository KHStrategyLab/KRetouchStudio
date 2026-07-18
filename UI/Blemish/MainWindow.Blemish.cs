using KRetouchStudio.Tabs;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private bool _isBlemishCommitRunning;
    private bool _hasPendingBlemishCommitRequest;
    private BlemishAdjustmentEventArgs? _pendingBlemishCommitArgs;

    private async void BlemishRetouchTab_BlemishAdjustmentPreviewChanged(object? sender, BlemishAdjustmentEventArgs e)
    {
        try
        {
            await ApplyBlemishDragPreviewAsync(e);
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = $"Blemish preview failed: {ex.Message}";
        }
    }

    private async void BlemishRetouchTab_BlemishAdjustmentCommitted(object? sender, BlemishAdjustmentEventArgs e)
    {
        await ApplyBlemishCommittedAsync(e);
    }

    private async void BlemishRetouchTab_BlemishResetRequested(object? sender, EventArgs e)
    {
        await TryResetBlemishSectionAsync();
    }

    private async Task ApplyBlemishDragPreviewAsync(BlemishAdjustmentEventArgs args)
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                args.Snapshot,
                _committedWrinkleSectionState,
                _committedMakeupSectionState))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Blemish: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Blemish");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Blemish: face landmarks unavailable";
            return;
        }

        IReadOnlyDictionary<int, Point> pointMap = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            proxySource.PixelWidth,
            proxySource.PixelHeight);
        Dictionary<int, Point> safePointMap = pointMap.ToDictionary(pair => pair.Key, pair => pair.Value);
        BitmapSource safeProxy = CloneBitmapSource(proxySource);
        BitmapSource preview = await Task.Run(() => RenderConnectedRetouchSections(
            safeProxy,
            safePointMap,
            _committedSkinSectionState,
            args.Snapshot,
            _committedWrinkleSectionState,
            _committedMakeupSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Blemish: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplyBlemishCommittedAsync(BlemishAdjustmentEventArgs args)
    {
        if (_isBlemishCommitRunning)
        {
            _pendingBlemishCommitArgs = args;
            _hasPendingBlemishCommitRequest = true;
            return;
        }

        _isBlemishCommitRunning = true;
        try
        {
            BlemishAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingBlemishCommitRequest = false;
                _pendingBlemishCommitArgs = null;
                await ApplyBlemishCommittedCoreAsync(currentArgs);
                if (_hasPendingBlemishCommitRequest && _pendingBlemishCommitArgs is not null)
                {
                    currentArgs = _pendingBlemishCommitArgs;
                }
            }
            while (_hasPendingBlemishCommitRequest);
        }
        finally
        {
            _isBlemishCommitRunning = false;
        }
    }

    private async Task ApplyBlemishCommittedCoreAsync(BlemishAdjustmentEventArgs args)
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Blemish: original-size image only";
            return;
        }

        BlemishAdjustmentSnapshot? requestedBlemishState = HasEffectiveBlemishAdjustment(args.Snapshot)
            ? args.Snapshot
            : null;
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                requestedBlemishState,
                _committedWrinkleSectionState,
                _committedMakeupSectionState))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            SetCommittedBlemishSectionState(null);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            PushOrReplaceBlemishHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            UpdateBlemishHistoryResetState();
            MediaPipeStatusText = "Blemish: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Blemish");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Blemish: face landmarks unavailable";
            return;
        }

        IReadOnlyDictionary<int, Point> pointMap = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        Dictionary<int, Point> safePointMap = pointMap.ToDictionary(pair => pair.Key, pair => pair.Value);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        BitmapSource result = await Task.Run(() => RenderConnectedRetouchSections(
            safeBase,
            safePointMap,
            _committedSkinSectionState,
            requestedBlemishState,
            _committedWrinkleSectionState,
            _committedMakeupSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        SetCommittedBlemishSectionState(requestedBlemishState);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceBlemishHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        UpdateBlemishHistoryResetState();
        MediaPipeStatusText = $"Blemish: applied {args.OperationId} {args.Value:0}";
    }

    private bool IsCurrentHistoryBlemishEffect()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, BlemishHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(BlemishHistoryDetail + " ", StringComparison.Ordinal);
    }

    private void PushOrReplaceBlemishHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = $"{BlemishHistoryDetail} {operationId} {Math.Clamp(Math.Round(value), 0, 100):0}";
        if (IsCurrentHistoryBlemishEffect())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, BlemishHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(BlemishHistoryTitle, detail);
    }

    private async Task TryResetBlemishSectionAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || _committedBlemishSectionState is null)
        {
            UpdateBlemishHistoryResetState();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource resetResult = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        if ((_committedSkinSectionState is not null && HasEffectiveSkinAdjustment(_committedSkinSectionState)) ||
            (_committedWrinkleSectionState is not null && HasEffectiveWrinkleAdjustment(_committedWrinkleSectionState)) ||
            (_committedMakeupSectionState is not null && HasEffectiveMakeupAdjustment(_committedMakeupSectionState)))
        {
            List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Blemish Reset");
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }

            if (landmarks.Count == 0)
            {
                MediaPipeStatusText = "Blemish reset: face landmarks unavailable";
                return;
            }

            IReadOnlyDictionary<int, Point> pointMap = GetOrCreateFaceShapePointMap(
                targetPhoto,
                landmarks,
                baseSource.PixelWidth,
                baseSource.PixelHeight);
            Dictionary<int, Point> safePointMap = pointMap.ToDictionary(pair => pair.Key, pair => pair.Value);
            BitmapSource safeBase = CloneBitmapSource(baseSource);
            resetResult = await Task.Run(() => RenderConnectedRetouchSections(
                safeBase,
                safePointMap,
                _committedSkinSectionState,
                null,
                _committedWrinkleSectionState,
                _committedMakeupSectionState,
                () => renderVersion != _connectedRetouchRenderVersion));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }
        }

        targetPhoto.SetAdjustedImage(resetResult);
        SetCommittedBlemishSectionState(null);
        BlemishRetouchTab.ResetAfterHistoryReset();
        ClearBlemishRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(BlemishHistoryTitle, BlemishResetHistoryDetail);
        UpdateBlemishHistoryResetState();
        MediaPipeStatusText = "Blemish: reset";
    }

    private void UpdateBlemishHistoryResetState()
    {
        BlemishRetouchTab.CanResetBlemishTab = _committedBlemishSectionState is not null;
    }

    private void ClearBlemishRetouchSession()
    {
        _pendingBlemishCommitArgs = null;
        _hasPendingBlemishCommitRequest = false;
        Interlocked.Increment(ref _connectedRetouchRenderVersion);
    }

    private static bool HasEffectiveBlemishAdjustment(BlemishAdjustmentSnapshot snapshot)
    {
        return snapshot.AcneReduce > 0.001 ||
               snapshot.AcneRedness > 0.001 ||
               snapshot.AcneBump > 0.001 ||
               snapshot.SpotRemove > 0.001 ||
               snapshot.SpotBlend > 0.001 ||
               snapshot.SpotTextureMatch > 0.001 ||
               snapshot.MoleReduce > 0.001 ||
               snapshot.MoleProtect > 0.001 ||
               snapshot.MoleEdgeBlend > 0.001 ||
               snapshot.FreckleFade > 0.001 ||
               snapshot.FreckleDensity > 0.001 ||
               snapshot.FreckleProtect > 0.001 ||
               snapshot.ScarSoften > 0.001 ||
               snapshot.ScarToneBlend > 0.001 ||
               snapshot.ScarTextureMatch > 0.001;
    }

    private static BitmapSource RenderBlemishAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        BlemishAdjustmentSnapshot snapshot,
        Func<bool> shouldCancel)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);

        if (!TryBuildSkinMask(pointMap, width, height, out byte[] skinMask, out double faceWidth) || shouldCancel())
        {
            return CloneBitmapSource(bgraSource);
        }

        int fineRadius = Math.Clamp((int)Math.Round(faceWidth * 0.0035), 1, 12);
        int broadRadius = Math.Clamp((int)Math.Round(faceWidth * 0.012), fineRadius + 1, 42);
        byte[] fineBlur = BoxBlurBgra32(sourcePixels, width, height, stride, fineRadius);
        byte[] broadBlur = BoxBlurBgra32(sourcePixels, width, height, stride, broadRadius);
        if (shouldCancel())
        {
            return CloneBitmapSource(bgraSource);
        }

        byte[] outputPixels = (byte[])sourcePixels.Clone();
        double acneReduce = snapshot.AcneReduce / 100.0;
        double acneRedness = snapshot.AcneRedness / 100.0;
        double acneBump = snapshot.AcneBump / 100.0;
        double spotRemove = snapshot.SpotRemove / 100.0;
        double spotBlend = snapshot.SpotBlend / 100.0;
        double spotTextureMatch = snapshot.SpotTextureMatch / 100.0;
        double moleReduce = snapshot.MoleReduce / 100.0;
        double moleProtect = snapshot.MoleProtect / 100.0;
        double moleEdgeBlend = snapshot.MoleEdgeBlend / 100.0;
        double freckleFade = snapshot.FreckleFade / 100.0;
        double freckleDensity = snapshot.FreckleDensity / 100.0;
        double freckleProtect = snapshot.FreckleProtect / 100.0;
        double scarSoften = snapshot.ScarSoften / 100.0;
        double scarToneBlend = snapshot.ScarToneBlend / 100.0;
        double scarTextureMatch = snapshot.ScarTextureMatch / 100.0;

        for (int y = 0; y < height; y++)
        {
            if ((y & 31) == 0 && shouldCancel())
            {
                return CloneBitmapSource(bgraSource);
            }

            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                double mask = skinMask[(y * width) + x] / 255.0;
                if (mask <= 0.001)
                {
                    continue;
                }

                int index = row + (x * 4);
                double originalB = sourcePixels[index];
                double originalG = sourcePixels[index + 1];
                double originalR = sourcePixels[index + 2];
                double b = originalB;
                double g = originalG;
                double r = originalR;
                double fineB = fineBlur[index];
                double fineG = fineBlur[index + 1];
                double fineR = fineBlur[index + 2];
                double broadB = broadBlur[index];
                double broadG = broadBlur[index + 1];
                double broadR = broadBlur[index + 2];
                double originalLuma = GetSkinLuma(originalB, originalG, originalR);
                double fineLuma = GetSkinLuma(fineB, fineG, fineR);
                double broadLuma = GetSkinLuma(broadB, broadG, broadR);
                double localDetail = Math.Abs(originalLuma - fineLuma);
                double darkDifference = Math.Max(0.0, broadLuma - originalLuma);
                double redExcess = Math.Max(0.0, originalR - ((originalG * 0.68) + (originalB * 0.32)) - 4.0);
                double redGate = BlemishSmoothStep((redExcess - 5.0) / 34.0);
                double detailGate = BlemishSmoothStep((localDetail - 4.0) / 28.0);
                double darkGate = BlemishSmoothStep((darkDifference - 5.0) / 38.0);

                double acneGate = Math.Max(redGate, detailGate * 0.70);
                double acneAmount = acneReduce * acneGate * 0.58;
                b += (((fineB * 0.68) + (broadB * 0.32)) - b) * acneAmount;
                g += (((fineG * 0.68) + (broadG * 0.32)) - g) * acneAmount;
                r += (((fineR * 0.68) + (broadR * 0.32)) - r) * acneAmount;

                if (acneRedness > 0.001 && redGate > 0.001)
                {
                    double reduction = redExcess * acneRedness * redGate * 0.72;
                    r -= reduction;
                    g += reduction * 0.10;
                    b += reduction * 0.04;
                }

                double bumpAmount = acneBump * detailGate * 0.48;
                b += (fineB - b) * bumpAmount;
                g += (fineG - g) * bumpAmount;
                r += (fineR - r) * bumpAmount;

                double spotAmount = spotRemove * darkGate * (0.48 + (spotBlend * 0.24));
                double spotTargetB = fineB + ((broadB - fineB) * spotBlend);
                double spotTargetG = fineG + ((broadG - fineG) * spotBlend);
                double spotTargetR = fineR + ((broadR - fineR) * spotBlend);
                b += (spotTargetB - b) * spotAmount;
                g += (spotTargetG - g) * spotAmount;
                r += (spotTargetR - r) * spotAmount;
                double spotDetailReturn = spotTextureMatch * spotRemove * darkGate * 0.24;
                b += (originalB - fineB) * spotDetailReturn;
                g += (originalG - fineG) * spotDetailReturn;
                r += (originalR - fineR) * spotDetailReturn;

                double moleGate = BlemishSmoothStep((darkDifference - 18.0) / 55.0) *
                                  BlemishSmoothStep((178.0 - originalLuma) / 85.0);
                double molePreserve = 1.0 - (moleProtect * 0.92);
                double moleAmount = moleReduce * moleGate * molePreserve * (0.48 + (moleEdgeBlend * 0.28));
                b += (broadB - b) * moleAmount;
                g += (broadG - g) * moleAmount;
                r += (broadR - r) * moleAmount;

                double freckleThreshold = 17.0 - (freckleDensity * 10.0);
                double freckleGate = BlemishSmoothStep((darkDifference - freckleThreshold) / 30.0) *
                                     (1.0 - (moleGate * 0.55));
                double freckleAmount = freckleFade * freckleGate * (1.0 - (freckleProtect * 0.82)) * 0.52;
                b += (fineB - b) * freckleAmount;
                g += (fineG - g) * freckleAmount;
                r += (fineR - r) * freckleAmount;

                double scarGate = BlemishSmoothStep((localDetail - 6.0) / 34.0);
                double scarSoftenAmount = scarSoften * scarGate * 0.42;
                b += (fineB - b) * scarSoftenAmount;
                g += (fineG - g) * scarSoftenAmount;
                r += (fineR - r) * scarSoftenAmount;
                double scarBlendAmount = scarToneBlend * scarGate * 0.34;
                b += (broadB - b) * scarBlendAmount;
                g += (broadG - g) * scarBlendAmount;
                r += (broadR - r) * scarBlendAmount;
                double scarDetailReturn = scarTextureMatch * (scarSoften + scarToneBlend) * 0.16 * scarGate;
                b += (originalB - fineB) * scarDetailReturn;
                g += (originalG - fineG) * scarDetailReturn;
                r += (originalR - fineR) * scarDetailReturn;

                outputPixels[index] = BlendSkinChannel(originalB, b, mask);
                outputPixels[index + 1] = BlendSkinChannel(originalG, g, mask);
                outputPixels[index + 2] = BlendSkinChannel(originalR, r, mask);
            }
        }

        BitmapSource output = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            outputPixels,
            stride);
        output.Freeze();
        return output;
    }

    private static double BlemishSmoothStep(double value)
    {
        double clamped = Math.Clamp(value, 0.0, 1.0);
        return clamped * clamped * (3.0 - (2.0 * clamped));
    }
}
