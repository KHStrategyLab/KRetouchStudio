using KRetouchStudio.Pipeline;
using KRetouchStudio.Tabs;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private bool _isWrinkleCommitRunning;
    private bool _hasPendingWrinkleCommitRequest;
    private WrinkleAdjustmentEventArgs? _pendingWrinkleCommitArgs;

    private async void WrinkleRetouchTab_WrinkleAdjustmentPreviewChanged(object? sender, WrinkleAdjustmentEventArgs e)
    {
        try
        {
            if (SelectedPhoto is not PhotoItem photo)
            {
                return;
            }

            SetCommittedWrinkleSectionState(HasEffectiveWrinkleAdjustment(e.Snapshot) ? e.Snapshot : null);
            PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Wrinkle);
            await RenderAndPublishPhotoEditPipelineAsync(
                photo,
                RetouchStageId.Wrinkle,
                RetouchRenderQuality.Preview,
                "Wrinkle");
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = $"Wrinkle preview failed: {ex.Message}";
        }
    }

    private async void WrinkleRetouchTab_WrinkleAdjustmentCommitted(object? sender, WrinkleAdjustmentEventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        SetCommittedWrinkleSectionState(HasEffectiveWrinkleAdjustment(e.Snapshot) ? e.Snapshot : null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Wrinkle);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Wrinkle,
            RetouchRenderQuality.FullResolution,
            "Wrinkle");
        if (applied)
        {
            PushOrReplaceWrinkleHistory(photo, e.OperationId, e.Value);
            UpdateWrinkleHistoryResetState();
        }
    }

    private async void WrinkleRetouchTab_WrinkleResetRequested(object? sender, EventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        SetCommittedWrinkleSectionState(null);
        WrinkleRetouchTab.RestoreSnapshot(null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Wrinkle);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Wrinkle,
            RetouchRenderQuality.FullResolution,
            "Wrinkle Reset");
        if (applied)
        {
            PushEditorHistorySnapshot(WrinkleHistoryTitle, WrinkleResetHistoryDetail);
            UpdateWrinkleHistoryResetState();
        }
    }

    private async Task ApplyWrinkleDragPreviewAsync(WrinkleAdjustmentEventArgs args)
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
                _committedBlemishSectionState,
                args.Snapshot,
                _committedMakeupSectionState,
                _committedHairSectionState))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Wrinkle: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Wrinkle");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Wrinkle: face landmarks unavailable";
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
            _committedBlemishSectionState,
            args.Snapshot,
            _committedMakeupSectionState,
            _committedHairSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Wrinkle: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplyWrinkleCommittedAsync(WrinkleAdjustmentEventArgs args)
    {
        if (_isWrinkleCommitRunning)
        {
            _pendingWrinkleCommitArgs = args;
            _hasPendingWrinkleCommitRequest = true;
            return;
        }

        _isWrinkleCommitRunning = true;
        try
        {
            WrinkleAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingWrinkleCommitRequest = false;
                _pendingWrinkleCommitArgs = null;
                await ApplyWrinkleCommittedCoreAsync(currentArgs);
                if (_hasPendingWrinkleCommitRequest && _pendingWrinkleCommitArgs is not null)
                {
                    currentArgs = _pendingWrinkleCommitArgs;
                }
            }
            while (_hasPendingWrinkleCommitRequest);
        }
        finally
        {
            _isWrinkleCommitRunning = false;
        }
    }

    private async Task ApplyWrinkleCommittedCoreAsync(WrinkleAdjustmentEventArgs args)
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
            MediaPipeStatusText = "Wrinkle: original-size image only";
            return;
        }

        WrinkleAdjustmentSnapshot? requestedWrinkleState = HasEffectiveWrinkleAdjustment(args.Snapshot)
            ? args.Snapshot
            : null;
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                _committedBlemishSectionState,
                requestedWrinkleState,
                _committedMakeupSectionState,
                _committedHairSectionState))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            SetCommittedWrinkleSectionState(null);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            PushOrReplaceWrinkleHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            UpdateWrinkleHistoryResetState();
            MediaPipeStatusText = "Wrinkle: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Wrinkle");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Wrinkle: face landmarks unavailable";
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
            _committedBlemishSectionState,
            requestedWrinkleState,
            _committedMakeupSectionState,
            _committedHairSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        SetCommittedWrinkleSectionState(requestedWrinkleState);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceWrinkleHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        UpdateWrinkleHistoryResetState();
        MediaPipeStatusText = $"Wrinkle: applied {args.OperationId} {args.Value:0}";
    }

    private bool IsCurrentHistoryWrinkleEffect()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, WrinkleHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(WrinkleHistoryDetail + " ", StringComparison.Ordinal);
    }

    private void PushOrReplaceWrinkleHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = $"{WrinkleHistoryDetail} {operationId} {Math.Clamp(Math.Round(value), 0, 100):0}";
        if (IsCurrentHistoryWrinkleEffect())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, WrinkleHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(WrinkleHistoryTitle, detail);
    }

    private async Task TryResetWrinkleSectionAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || _committedWrinkleSectionState is null)
        {
            UpdateWrinkleHistoryResetState();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource resetResult = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        if ((_committedSkinSectionState is not null && HasEffectiveSkinAdjustment(_committedSkinSectionState)) ||
            (_committedBlemishSectionState is not null && HasEffectiveBlemishAdjustment(_committedBlemishSectionState)) ||
            (_committedMakeupSectionState is not null && HasEffectiveMakeupAdjustment(_committedMakeupSectionState)) ||
            (_committedHairSectionState is not null && HasEffectiveHairAdjustment(_committedHairSectionState)))
        {
            List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Wrinkle Reset");
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }

            if (landmarks.Count == 0)
            {
                MediaPipeStatusText = "Wrinkle reset: face landmarks unavailable";
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
                _committedBlemishSectionState,
                null,
                _committedMakeupSectionState,
                _committedHairSectionState,
                () => renderVersion != _connectedRetouchRenderVersion));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }
        }

        targetPhoto.SetAdjustedImage(resetResult);
        SetCommittedWrinkleSectionState(null);
        WrinkleRetouchTab.ResetAfterHistoryReset();
        ClearWrinkleRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(WrinkleHistoryTitle, WrinkleResetHistoryDetail);
        UpdateWrinkleHistoryResetState();
        MediaPipeStatusText = "Wrinkle: reset";
    }

    private void UpdateWrinkleHistoryResetState()
    {
        WrinkleRetouchTab.CanResetWrinkleTab = _committedWrinkleSectionState is not null;
    }

    private void ClearWrinkleRetouchSession()
    {
        _pendingWrinkleCommitArgs = null;
        _hasPendingWrinkleCommitRequest = false;
        Interlocked.Increment(ref _connectedRetouchRenderVersion);
    }

    private static bool HasEffectiveWrinkleAdjustment(WrinkleAdjustmentSnapshot snapshot)
    {
        return snapshot.Forehead > 0.001 ||
               snapshot.Frown > 0.001 ||
               snapshot.LeftCrowsFeet > 0.001 ||
               snapshot.RightCrowsFeet > 0.001 ||
               snapshot.LeftUnderEye > 0.001 ||
               snapshot.RightUnderEye > 0.001 ||
               snapshot.LeftBunny > 0.001 ||
               snapshot.RightBunny > 0.001 ||
               snapshot.LeftSmileFold > 0.001 ||
               snapshot.RightSmileFold > 0.001 ||
               snapshot.LipLines > 0.001 ||
               snapshot.LeftMarionette > 0.001 ||
               snapshot.RightMarionette > 0.001 ||
               snapshot.ChinCrease > 0.001 ||
               snapshot.Neck > 0.001;
    }

    private static BitmapSource RenderWrinkleAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        WrinkleAdjustmentSnapshot snapshot,
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

        WrinkleStrengthMasks wrinkleMasks = BuildWrinkleStrengthMasks(pointMap, width, height, skinMask, snapshot, faceWidth);
        int fineRadius = Math.Clamp((int)Math.Round(faceWidth * 0.003), 1, 10);
        int broadRadius = Math.Clamp((int)Math.Round(faceWidth * 0.010), fineRadius + 1, 36);
        int facialRadius = Math.Clamp((int)Math.Round(faceWidth * 0.040), broadRadius + 1, 72);
        int foldRadius = Math.Clamp((int)Math.Round(faceWidth * 0.045), facialRadius + 1, 84);
        int neckRadius = Math.Clamp((int)Math.Round(faceWidth * 0.034), broadRadius + 1, 72);
        byte[] fineBlur = BoxBlurBgra32(sourcePixels, width, height, stride, fineRadius);
        byte[] broadBlur = BoxBlurBgra32(sourcePixels, width, height, stride, broadRadius);
        byte[] facialBlur = HasFineWrinkleAdjustment(snapshot)
            ? BoxBlurBgra32(sourcePixels, width, height, stride, facialRadius)
            : broadBlur;
        byte[] foldBlur = HasFoldWrinkleAdjustment(snapshot)
            ? BoxBlurBgra32(sourcePixels, width, height, stride, foldRadius)
            : broadBlur;
        byte[] neckBlur = snapshot.Neck > 0.001
            ? BoxBlurBgra32(sourcePixels, width, height, stride, neckRadius)
            : broadBlur;
        if (shouldCancel())
        {
            return CloneBitmapSource(bgraSource);
        }

        byte[] outputPixels = (byte[])sourcePixels.Clone();
        for (int y = 0; y < height; y++)
        {
            if ((y & 31) == 0 && shouldCancel())
            {
                return CloneBitmapSource(bgraSource);
            }

            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int maskIndex = (y * width) + x;
                double regionAmount = wrinkleMasks.Combined[maskIndex] / 255.0;
                if (regionAmount <= 0.001)
                {
                    continue;
                }

                int index = row + (x * 4);
                double originalB = sourcePixels[index];
                double originalG = sourcePixels[index + 1];
                double originalR = sourcePixels[index + 2];
                double fineB = fineBlur[index];
                double fineG = fineBlur[index + 1];
                double fineR = fineBlur[index + 2];
                double broadB = broadBlur[index];
                double broadG = broadBlur[index + 1];
                double broadR = broadBlur[index + 2];
                double originalLuma = GetSkinLuma(originalB, originalG, originalR);
                double fineLuma = GetSkinLuma(fineB, fineG, fineR);
                double broadLuma = GetSkinLuma(broadB, broadG, broadR);
                double darkLine = Math.Max(0.0, Math.Max(fineLuma, broadLuma) - originalLuma);
                double lineGate = WrinkleSmoothStep((darkLine - 0.6) / 12.0);
                double correctionAmount = regionAmount * (0.06 + (lineGate * 0.68));
                double neckAmount = wrinkleMasks.Neck[maskIndex] / 255.0;
                double targetB = (fineB * 0.78) + (broadB * 0.22);
                double targetG = (fineG * 0.78) + (broadG * 0.22);
                double targetR = (fineR * 0.78) + (broadR * 0.22);
                double facialAmount = wrinkleMasks.FacialFine[maskIndex] / 255.0;
                if (facialAmount > 0.001)
                {
                    double facialB = facialBlur[index];
                    double facialG = facialBlur[index + 1];
                    double facialR = facialBlur[index + 2];
                    double facialLuma = GetSkinLuma(facialB, facialG, facialR);
                    double facialDarkLine = Math.Max(0.0, Math.Max(fineLuma, facialLuma) - originalLuma);
                    double facialLineGate = WrinkleSmoothStep((facialDarkLine - 0.10) / 7.0);
                    double facialCorrection = facialAmount * (0.70 + (facialLineGate * 0.28));
                    if (facialCorrection > correctionAmount)
                    {
                        correctionAmount = facialCorrection;
                        targetB = (fineB * 0.12) + (facialB * 0.88);
                        targetG = (fineG * 0.12) + (facialG * 0.88);
                        targetR = (fineR * 0.12) + (facialR * 0.88);
                    }
                }

                double foldAmount = wrinkleMasks.FacialFold[maskIndex] / 255.0;
                if (foldAmount > 0.001)
                {
                    double foldB = foldBlur[index];
                    double foldG = foldBlur[index + 1];
                    double foldR = foldBlur[index + 2];
                    double foldLuma = GetSkinLuma(foldB, foldG, foldR);
                    double foldDarkLine = Math.Max(0.0, Math.Max(fineLuma, foldLuma) - originalLuma);
                    double foldLineGate = WrinkleSmoothStep((foldDarkLine - 0.10) / 9.0);
                    double foldCorrection = foldAmount * (0.60 + (foldLineGate * 0.38));
                    if (foldCorrection > correctionAmount)
                    {
                        correctionAmount = foldCorrection;
                        targetB = (fineB * 0.14) + (foldB * 0.86);
                        targetG = (fineG * 0.14) + (foldG * 0.86);
                        targetR = (fineR * 0.14) + (foldR * 0.86);
                    }
                }

                if (neckAmount > 0.001)
                {
                    double neckB = neckBlur[index];
                    double neckG = neckBlur[index + 1];
                    double neckR = neckBlur[index + 2];
                    double neckLuma = GetSkinLuma(neckB, neckG, neckR);
                    double neckDarkLine = Math.Max(0.0, Math.Max(fineLuma, neckLuma) - originalLuma);
                    double neckLineGate = WrinkleSmoothStep((neckDarkLine - 0.15) / 8.0);
                    double neckSkinGate = GetNeckSkinGate(originalB, originalG, originalR);
                    double neckCorrection = neckAmount * neckSkinGate * (0.48 + (neckLineGate * 0.46));
                    correctionAmount = Math.Max(correctionAmount, neckCorrection);
                    targetB = (fineB * 0.24) + (neckB * 0.76);
                    targetG = (fineG * 0.24) + (neckG * 0.76);
                    targetR = (fineR * 0.24) + (neckR * 0.76);
                }

                outputPixels[index] = BlendSkinChannel(originalB, targetB, correctionAmount);
                outputPixels[index + 1] = BlendSkinChannel(originalG, targetG, correctionAmount);
                outputPixels[index + 2] = BlendSkinChannel(originalR, targetR, correctionAmount);
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

    private static WrinkleStrengthMasks BuildWrinkleStrengthMasks(
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        byte[] skinMask,
        WrinkleAdjustmentSnapshot snapshot,
        double faceWidth)
    {
        byte[] result = new byte[width * height];
        byte[] neckMask = new byte[width * height];
        byte[] facialFineMask = new byte[width * height];
        byte[] facialFoldMask = new byte[width * height];
        List<Point> oval = FaceShapeHeadTiltFaceOvalIndices
            .Where(pointMap.ContainsKey)
            .Select(index => pointMap[index])
            .ToList();
        if (oval.Count < 12)
        {
            return new WrinkleStrengthMasks(result, neckMask, facialFineMask, facialFoldMask);
        }

        double minX = oval.Min(point => point.X);
        double maxX = oval.Max(point => point.X);
        double minY = oval.Min(point => point.Y);
        double maxY = oval.Max(point => point.Y);
        double faceHeight = Math.Max(1.0, maxY - minY);
        double centerX = (minX + maxX) * 0.5;

        AddWrinkleEllipse(facialFineMask, skinMask, width, height, centerX, minY + (faceHeight * 0.19), faceWidth * 0.31, faceHeight * 0.115, snapshot.Forehead / 100.0);
        AddWrinkleEllipse(facialFineMask, skinMask, width, height, centerX, minY + (faceHeight * 0.31), faceWidth * 0.075, faceHeight * 0.105, snapshot.Frown / 100.0);

        AddEyeWrinkleRegions(facialFineMask, skinMask, pointMap, width, height, faceWidth, faceHeight, snapshot);
        AddNoseWrinkleRegions(facialFineMask, skinMask, pointMap, width, height, faceWidth, faceHeight, snapshot);
        AddMouthWrinkleRegions(facialFineMask, facialFoldMask, skinMask, pointMap, width, height, faceWidth, faceHeight, snapshot);

        Point lowerLip = pointMap.TryGetValue(17, out Point lowerLipPoint)
            ? lowerLipPoint
            : new Point(centerX, minY + (faceHeight * 0.68));
        Point chin = pointMap.TryGetValue(152, out Point chinPoint)
            ? chinPoint
            : new Point(centerX, maxY);
        Point chinCreaseCenter = new(
            (lowerLip.X + chin.X) * 0.5,
            lowerLip.Y + ((chin.Y - lowerLip.Y) * 0.42));
        AddWrinkleEllipse(facialFineMask, skinMask, width, height, chinCreaseCenter.X, chinCreaseCenter.Y, faceWidth * 0.16, faceHeight * 0.042, snapshot.ChinCrease / 100.0);

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Math.Max(facialFineMask[i], facialFoldMask[i]);
        }

        double neckStrength = snapshot.Neck / 100.0;
        AddWrinkleEllipse(neckMask, null, width, height, centerX, maxY + (faceHeight * 0.075), faceWidth * 0.18, faceHeight * 0.105, neckStrength);
        AddWrinkleEllipse(neckMask, null, width, height, centerX, maxY + (faceHeight * 0.245), faceWidth * 0.245, faceHeight * 0.17, neckStrength);
        for (int i = 0; i < result.Length; i++)
        {
            if (neckMask[i] > result[i])
            {
                result[i] = neckMask[i];
            }
        }

        return new WrinkleStrengthMasks(result, neckMask, facialFineMask, facialFoldMask);
    }

    private static void AddEyeWrinkleRegions(
        byte[] result,
        byte[] skinMask,
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        double faceWidth,
        double faceHeight,
        WrinkleAdjustmentSnapshot snapshot)
    {
        AddSingleEyeWrinkleRegions(result, skinMask, pointMap, width, height, true, faceWidth, faceHeight, snapshot.LeftCrowsFeet, snapshot.LeftUnderEye);
        AddSingleEyeWrinkleRegions(result, skinMask, pointMap, width, height, false, faceWidth, faceHeight, snapshot.RightCrowsFeet, snapshot.RightUnderEye);
    }

    private static void AddSingleEyeWrinkleRegions(
        byte[] result,
        byte[] skinMask,
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        bool isLeft,
        double faceWidth,
        double faceHeight,
        double crowsFeet,
        double underEye)
    {
        int outerIndex = isLeft ? 33 : 263;
        int innerIndex = isLeft ? 133 : 362;
        int topIndex = isLeft ? 159 : 386;
        int bottomIndex = isLeft ? 145 : 374;
        if (!pointMap.TryGetValue(outerIndex, out Point outer) ||
            !pointMap.TryGetValue(innerIndex, out Point inner) ||
            !pointMap.TryGetValue(topIndex, out Point top) ||
            !pointMap.TryGetValue(bottomIndex, out Point bottom))
        {
            return;
        }

        Point center = new((outer.X + inner.X + top.X + bottom.X) * 0.25, (outer.Y + inner.Y + top.Y + bottom.Y) * 0.25);
        double outward = isLeft ? -1.0 : 1.0;
        AddWrinkleEllipse(
            result,
            skinMask,
            width,
            height,
            outer.X + (outward * faceWidth * 0.052),
            outer.Y,
            faceWidth * 0.105,
            faceHeight * 0.073,
            crowsFeet / 100.0);
        AddWrinkleEllipse(
            result,
            skinMask,
            width,
            height,
            center.X,
            center.Y + (faceHeight * 0.047),
            faceWidth * 0.115,
            faceHeight * 0.052,
            underEye / 100.0);
    }

    private static void AddNoseWrinkleRegions(
        byte[] result,
        byte[] skinMask,
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        double faceWidth,
        double faceHeight,
        WrinkleAdjustmentSnapshot snapshot)
    {
        if (!pointMap.TryGetValue(1, out Point noseCenter))
        {
            return;
        }

        if (pointMap.TryGetValue(98, out Point leftNose))
        {
            AddWrinkleEllipse(result, skinMask, width, height, (leftNose.X + noseCenter.X) * 0.5, noseCenter.Y - (faceHeight * 0.045), faceWidth * 0.062, faceHeight * 0.075, snapshot.LeftBunny / 100.0);
        }

        if (pointMap.TryGetValue(327, out Point rightNose))
        {
            AddWrinkleEllipse(result, skinMask, width, height, (rightNose.X + noseCenter.X) * 0.5, noseCenter.Y - (faceHeight * 0.045), faceWidth * 0.062, faceHeight * 0.075, snapshot.RightBunny / 100.0);
        }
    }

    private static void AddMouthWrinkleRegions(
        byte[] fineResult,
        byte[] foldResult,
        byte[] skinMask,
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        double faceWidth,
        double faceHeight,
        WrinkleAdjustmentSnapshot snapshot)
    {
        AddSingleMouthSideWrinkleRegions(foldResult, skinMask, pointMap, width, height, true, faceWidth, faceHeight, snapshot.LeftSmileFold, snapshot.LeftMarionette);
        AddSingleMouthSideWrinkleRegions(foldResult, skinMask, pointMap, width, height, false, faceWidth, faceHeight, snapshot.RightSmileFold, snapshot.RightMarionette);

        if (pointMap.TryGetValue(13, out Point upperLip))
        {
            AddWrinkleEllipse(fineResult, skinMask, width, height, upperLip.X, upperLip.Y - (faceHeight * 0.026), faceWidth * 0.14, faceHeight * 0.052, snapshot.LipLines / 100.0);
        }
    }

    private static void AddSingleMouthSideWrinkleRegions(
        byte[] result,
        byte[] skinMask,
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        bool isLeft,
        double faceWidth,
        double faceHeight,
        double smileFold,
        double marionette)
    {
        int noseIndex = isLeft ? 98 : 327;
        int cornerIndex = isLeft ? 61 : 291;
        if (!pointMap.TryGetValue(noseIndex, out Point nose) || !pointMap.TryGetValue(cornerIndex, out Point corner))
        {
            return;
        }

        AddWrinkleCapsule(result, skinMask, width, height, nose, corner, faceWidth * 0.047, smileFold / 100.0);
        double outward = isLeft ? -1.0 : 1.0;
        Point marionetteEnd = new(corner.X + (outward * faceWidth * 0.055), corner.Y + (faceHeight * 0.135));
        AddWrinkleCapsule(result, skinMask, width, height, corner, marionetteEnd, faceWidth * 0.045, marionette / 100.0);
    }

    private static void AddWrinkleEllipse(
        byte[] result,
        byte[]? clipMask,
        int width,
        int height,
        double centerX,
        double centerY,
        double radiusX,
        double radiusY,
        double strength)
    {
        if (strength <= 0.001)
        {
            return;
        }

        radiusX = Math.Max(1.0, radiusX);
        radiusY = Math.Max(1.0, radiusY);
        int minX = Math.Clamp((int)Math.Floor(centerX - radiusX), 0, width - 1);
        int maxX = Math.Clamp((int)Math.Ceiling(centerX + radiusX), 0, width - 1);
        int minY = Math.Clamp((int)Math.Floor(centerY - radiusY), 0, height - 1);
        int maxY = Math.Clamp((int)Math.Ceiling(centerY + radiusY), 0, height - 1);
        for (int y = minY; y <= maxY; y++)
        {
            double normalizedY = (y - centerY) / radiusY;
            int row = y * width;
            for (int x = minX; x <= maxX; x++)
            {
                double normalizedX = (x - centerX) / radiusX;
                double distance = Math.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
                if (distance >= 1.0)
                {
                    continue;
                }

                double feather = 1.0 - WrinkleSmoothStep((distance - 0.68) / 0.32);
                SetWrinkleMaskValue(result, clipMask, row + x, strength * feather);
            }
        }
    }

    private static void AddWrinkleCapsule(
        byte[] result,
        byte[]? clipMask,
        int width,
        int height,
        Point start,
        Point end,
        double radius,
        double strength)
    {
        if (strength <= 0.001)
        {
            return;
        }

        radius = Math.Max(1.0, radius);
        int minX = Math.Clamp((int)Math.Floor(Math.Min(start.X, end.X) - radius), 0, width - 1);
        int maxX = Math.Clamp((int)Math.Ceiling(Math.Max(start.X, end.X) + radius), 0, width - 1);
        int minY = Math.Clamp((int)Math.Floor(Math.Min(start.Y, end.Y) - radius), 0, height - 1);
        int maxY = Math.Clamp((int)Math.Ceiling(Math.Max(start.Y, end.Y) + radius), 0, height - 1);
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = Math.Max(1.0, (dx * dx) + (dy * dy));
        for (int y = minY; y <= maxY; y++)
        {
            int row = y * width;
            for (int x = minX; x <= maxX; x++)
            {
                double projection = Math.Clamp((((x - start.X) * dx) + ((y - start.Y) * dy)) / lengthSquared, 0.0, 1.0);
                double nearestX = start.X + (dx * projection);
                double nearestY = start.Y + (dy * projection);
                double distance = Math.Sqrt(((x - nearestX) * (x - nearestX)) + ((y - nearestY) * (y - nearestY))) / radius;
                if (distance >= 1.0)
                {
                    continue;
                }

                double feather = 1.0 - WrinkleSmoothStep((distance - 0.64) / 0.36);
                SetWrinkleMaskValue(result, clipMask, row + x, strength * feather);
            }
        }
    }

    private static void SetWrinkleMaskValue(byte[] result, byte[]? clipMask, int index, double amount)
    {
        if (clipMask is not null)
        {
            amount *= clipMask[index] / 255.0;
        }

        byte value = (byte)Math.Clamp((int)Math.Round(amount * 255.0), 0, 255);
        if (value > result[index])
        {
            result[index] = value;
        }
    }

    private static double WrinkleSmoothStep(double value)
    {
        double clamped = Math.Clamp(value, 0.0, 1.0);
        return clamped * clamped * (3.0 - (2.0 * clamped));
    }

    private static double GetNeckSkinGate(double blue, double green, double red)
    {
        double luma = GetSkinLuma(blue, green, red);
        double maxChannel = Math.Max(red, Math.Max(green, blue));
        double minChannel = Math.Min(red, Math.Min(green, blue));
        double saturation = maxChannel <= 1.0 ? 0.0 : (maxChannel - minChannel) / maxChannel;
        double lumaGate = Math.Clamp((luma - 24.0) / 42.0, 0.12, 1.0);
        double saturationGate = saturation <= 0.72
            ? 1.0
            : Math.Clamp(1.0 - ((saturation - 0.72) * 2.4), 0.15, 1.0);
        return lumaGate * saturationGate;
    }

    private static bool HasFineWrinkleAdjustment(WrinkleAdjustmentSnapshot snapshot)
    {
        return snapshot.Forehead > 0.001 ||
               snapshot.Frown > 0.001 ||
               snapshot.LeftCrowsFeet > 0.001 ||
               snapshot.RightCrowsFeet > 0.001 ||
               snapshot.LeftUnderEye > 0.001 ||
               snapshot.RightUnderEye > 0.001 ||
               snapshot.LeftBunny > 0.001 ||
               snapshot.RightBunny > 0.001 ||
               snapshot.LipLines > 0.001 ||
               snapshot.ChinCrease > 0.001;
    }

    private static bool HasFoldWrinkleAdjustment(WrinkleAdjustmentSnapshot snapshot)
    {
        return snapshot.LeftSmileFold > 0.001 ||
               snapshot.RightSmileFold > 0.001 ||
               snapshot.LeftMarionette > 0.001 ||
               snapshot.RightMarionette > 0.001;
    }

    private sealed record WrinkleStrengthMasks(
        byte[] Combined,
        byte[] Neck,
        byte[] FacialFine,
        byte[] FacialFold);
}
