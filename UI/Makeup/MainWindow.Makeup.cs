using KRetouchStudio.Tabs;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private bool _isMakeupCommitRunning;
    private bool _hasPendingMakeupCommitRequest;
    private MakeupAdjustmentEventArgs? _pendingMakeupCommitArgs;

    private async void MakeupRetouchTab_MakeupAdjustmentPreviewChanged(object? sender, MakeupAdjustmentEventArgs e)
    {
        try
        {
            await ApplyMakeupDragPreviewAsync(e);
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = $"Makeup preview failed: {ex.Message}";
        }
    }

    private async void MakeupRetouchTab_MakeupAdjustmentCommitted(object? sender, MakeupAdjustmentEventArgs e)
    {
        await ApplyMakeupCommittedAsync(e);
    }

    private async void MakeupRetouchTab_MakeupResetRequested(object? sender, EventArgs e)
    {
        await TryResetMakeupSectionAsync();
    }

    private async Task ApplyMakeupDragPreviewAsync(MakeupAdjustmentEventArgs args)
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
                _committedWrinkleSectionState,
                args.Snapshot,
                _committedHairSectionState))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Makeup: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Makeup");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Makeup: face landmarks unavailable";
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
            _committedWrinkleSectionState,
            args.Snapshot,
            _committedHairSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Makeup: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplyMakeupCommittedAsync(MakeupAdjustmentEventArgs args)
    {
        if (_isMakeupCommitRunning)
        {
            _pendingMakeupCommitArgs = args;
            _hasPendingMakeupCommitRequest = true;
            return;
        }

        _isMakeupCommitRunning = true;
        try
        {
            MakeupAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingMakeupCommitRequest = false;
                _pendingMakeupCommitArgs = null;
                await ApplyMakeupCommittedCoreAsync(currentArgs);
                if (_hasPendingMakeupCommitRequest && _pendingMakeupCommitArgs is not null)
                {
                    currentArgs = _pendingMakeupCommitArgs;
                }
            }
            while (_hasPendingMakeupCommitRequest);
        }
        finally
        {
            _isMakeupCommitRunning = false;
        }
    }

    private async Task ApplyMakeupCommittedCoreAsync(MakeupAdjustmentEventArgs args)
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
            MediaPipeStatusText = "Makeup: original-size image only";
            return;
        }

        MakeupAdjustmentSnapshot? requestedMakeupState = HasEffectiveMakeupAdjustment(args.Snapshot)
            ? args.Snapshot
            : null;
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                _committedBlemishSectionState,
                _committedWrinkleSectionState,
                requestedMakeupState,
                _committedHairSectionState))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            SetCommittedMakeupSectionState(null);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            PushOrReplaceMakeupHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            UpdateMakeupHistoryResetState();
            MediaPipeStatusText = "Makeup: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Makeup");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Makeup: face landmarks unavailable";
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
            _committedWrinkleSectionState,
            requestedMakeupState,
            _committedHairSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        SetCommittedMakeupSectionState(requestedMakeupState);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceMakeupHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        UpdateMakeupHistoryResetState();
        MediaPipeStatusText = $"Makeup: applied {args.OperationId} {args.Value:0}";
    }

    private bool IsCurrentHistoryMakeupEffect()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, MakeupHistoryTitle, StringComparison.Ordinal);
    }

    private void PushOrReplaceMakeupHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = string.IsNullOrWhiteSpace(operationId)
            ? MakeupHistoryDetail
            : $"{operationId} {value:0}";
        if (IsCurrentHistoryMakeupEffect())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, MakeupHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(MakeupHistoryTitle, detail);
    }

    private async Task TryResetMakeupSectionAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || _committedMakeupSectionState is null)
        {
            UpdateMakeupHistoryResetState();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource resetResult = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        if ((_committedSkinSectionState is not null && HasEffectiveSkinAdjustment(_committedSkinSectionState)) ||
            (_committedBlemishSectionState is not null && HasEffectiveBlemishAdjustment(_committedBlemishSectionState)) ||
            (_committedWrinkleSectionState is not null && HasEffectiveWrinkleAdjustment(_committedWrinkleSectionState)) ||
            (_committedHairSectionState is not null && HasEffectiveHairAdjustment(_committedHairSectionState)))
        {
            List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Makeup Reset");
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }

            if (landmarks.Count == 0)
            {
                MediaPipeStatusText = "Makeup reset: face landmarks unavailable";
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
                _committedWrinkleSectionState,
                null,
                _committedHairSectionState,
                () => renderVersion != _connectedRetouchRenderVersion));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }
        }

        targetPhoto.SetAdjustedImage(resetResult);
        SetCommittedMakeupSectionState(null);
        MakeupRetouchTab.ResetAfterHistoryReset();
        ClearMakeupRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(MakeupHistoryTitle, MakeupResetHistoryDetail);
        UpdateMakeupHistoryResetState();
        MediaPipeStatusText = "Makeup: reset";
    }

    private void UpdateMakeupHistoryResetState()
    {
        MakeupRetouchTab.CanResetMakeupTab = _committedMakeupSectionState is not null;
    }

    private void ClearMakeupRetouchSession()
    {
        _pendingMakeupCommitArgs = null;
        _hasPendingMakeupCommitRequest = false;
        Interlocked.Increment(ref _connectedRetouchRenderVersion);
    }

    private static bool HasEffectiveMakeupAdjustment(MakeupAdjustmentSnapshot snapshot)
    {
        return snapshot.BaseCoverage > 0.001 ||
               snapshot.BaseEvenness > 0.001 ||
               snapshot.BaseFinish > 0.001 ||
               snapshot.BrowDensity > 0.001 ||
               snapshot.BrowShape > 0.001 ||
               snapshot.BrowColor > 0.001 ||
               snapshot.EyeShadow > 0.001 ||
               snapshot.EyeLiner > 0.001 ||
               snapshot.EyeLash > 0.001 ||
               snapshot.CheekBlush > 0.001 ||
               snapshot.CheekContour > 0.001 ||
               snapshot.CheekHighlight > 0.001 ||
               snapshot.LipColor > 0.001 ||
               snapshot.LipSaturation > 0.001 ||
               snapshot.LipGloss > 0.001;
    }

    private static BitmapSource RenderMakeupAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        MakeupAdjustmentSnapshot snapshot,
        Func<bool> shouldCancel)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);

        if (!TryBuildSkinMask(pointMap, width, height, out byte[] skinMask, out double faceWidth) ||
            !TryBuildMakeupRegionMasks(pointMap, width, height, faceWidth, out MakeupRegionMasks masks) ||
            shouldCancel())
        {
            return CloneBitmapSource(bgraSource);
        }

        bool hasBase = snapshot.BaseCoverage > 0.001 || snapshot.BaseEvenness > 0.001 || snapshot.BaseFinish > 0.001;
        int baseRadius = Math.Clamp((int)Math.Round(faceWidth * 0.020), 2, 72);
        byte[] baseBlur = hasBase
            ? BoxBlurBgra32(sourcePixels, width, height, stride, baseRadius)
            : sourcePixels;
        byte[] outputPixels = (byte[])sourcePixels.Clone();

        double baseCoverage = snapshot.BaseCoverage / 100.0;
        double baseEvenness = snapshot.BaseEvenness / 100.0;
        double baseFinish = snapshot.BaseFinish / 100.0;
        double browDensity = snapshot.BrowDensity / 100.0;
        double browShape = snapshot.BrowShape / 100.0;
        double browColor = snapshot.BrowColor / 100.0;
        double eyeShadow = snapshot.EyeShadow / 100.0;
        double eyeLiner = snapshot.EyeLiner / 100.0;
        double eyeLash = snapshot.EyeLash / 100.0;
        double cheekBlush = snapshot.CheekBlush / 100.0;
        double cheekContour = snapshot.CheekContour / 100.0;
        double cheekHighlight = snapshot.CheekHighlight / 100.0;
        double lipColor = snapshot.LipColor / 100.0;
        double lipSaturation = snapshot.LipSaturation / 100.0;
        double lipGloss = snapshot.LipGloss / 100.0;

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
                int index = row + (x * 4);
                double originalB = sourcePixels[index];
                double originalG = sourcePixels[index + 1];
                double originalR = sourcePixels[index + 2];
                double b = originalB;
                double g = originalG;
                double r = originalR;

                double baseMask = skinMask[maskIndex] / 255.0;
                if (baseMask > 0.001 && hasBase)
                {
                    double broadB = baseBlur[index];
                    double broadG = baseBlur[index + 1];
                    double broadR = baseBlur[index + 2];
                    double coverageAmount = baseMask * baseCoverage * 0.58;
                    b += (broadB - b) * coverageAmount;
                    g += (broadG - g) * coverageAmount;
                    r += (broadR - r) * coverageAmount;

                    double currentLuma = GetSkinLuma(b, g, r);
                    double broadLuma = GetSkinLuma(broadB, broadG, broadR);
                    double toneShift = (broadLuma - currentLuma) * baseMask * baseEvenness * 0.82;
                    b += toneShift;
                    g += toneShift;
                    r += toneShift;

                    double finishAmount = baseMask * baseFinish * 0.38;
                    b += (((broadB * 0.96) + 5.0) - b) * finishAmount;
                    g += (((broadG * 0.99) + 6.0) - g) * finishAmount;
                    r += (((broadR * 1.03) + 7.0) - r) * finishAmount;
                }

                BlendMakeupTint(ref b, ref g, ref r, 118, 92, 208, cheekBlush * 0.46 * MaskAmount(masks.CheekBlush, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 55, 68, 98, cheekContour * 0.42 * MaskAmount(masks.CheekContour, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 232, 238, 252, cheekHighlight * 0.46 * MaskAmount(masks.CheekHighlight, maskIndex));

                BlendMakeupTint(ref b, ref g, ref r, 28, 34, 42, browDensity * 0.68 * MaskAmount(masks.Brow, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 24, 30, 38, browShape * 0.62 * MaskAmount(masks.BrowShape, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 44, 54, 76, browColor * 0.58 * MaskAmount(masks.Brow, maskIndex));

                BlendMakeupTint(ref b, ref g, ref r, 74, 78, 122, eyeShadow * 0.54 * MaskAmount(masks.EyeShadow, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 18, 22, 30, eyeLiner * 0.88 * MaskAmount(masks.EyeLine, maskIndex));
                BlendMakeupTint(ref b, ref g, ref r, 10, 13, 18, eyeLash * 0.68 * MaskAmount(masks.EyeLine, maskIndex));

                double lipMask = MaskAmount(masks.Lip, maskIndex);
                BlendMakeupTint(ref b, ref g, ref r, 82, 58, 202, lipColor * 0.74 * lipMask);
                if (lipSaturation > 0.001 && lipMask > 0.001)
                {
                    double luma = GetSkinLuma(b, g, r);
                    double saturationScale = 1.0 + (lipSaturation * lipMask * 1.25);
                    b = luma + ((b - luma) * saturationScale);
                    g = luma + ((g - luma) * saturationScale);
                    r = luma + ((r - luma) * saturationScale);
                }

                BlendMakeupTint(ref b, ref g, ref r, 238, 232, 255, lipGloss * 0.72 * MaskAmount(masks.LipGloss, maskIndex));

                outputPixels[index] = BlendSkinChannel(originalB, b, 1.0);
                outputPixels[index + 1] = BlendSkinChannel(originalG, g, 1.0);
                outputPixels[index + 2] = BlendSkinChannel(originalR, r, 1.0);
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

    private static bool TryBuildMakeupRegionMasks(
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        double faceWidth,
        out MakeupRegionMasks masks)
    {
        byte[] brow = new byte[width * height];
        byte[] browShape = new byte[width * height];
        byte[] eyeShadow = new byte[width * height];
        byte[] eyeLine = new byte[width * height];
        byte[] cheekBlush = new byte[width * height];
        byte[] cheekContour = new byte[width * height];
        byte[] cheekHighlight = new byte[width * height];
        byte[] lip = new byte[width * height];
        byte[] lipGloss = new byte[width * height];
        masks = new MakeupRegionMasks(brow, browShape, eyeShadow, eyeLine, cheekBlush, cheekContour, cheekHighlight, lip, lipGloss);

        List<Point> oval = FaceShapeHeadTiltFaceOvalIndices
            .Where(pointMap.ContainsKey)
            .Select(index => pointMap[index])
            .ToList();
        if (oval.Count < 12)
        {
            return false;
        }

        double faceHeight = Math.Max(1.0, oval.Max(point => point.Y) - oval.Min(point => point.Y));
        AddMakeupPolygon(brow, pointMap, [70, 63, 105, 66, 107, 55, 65, 52, 53, 46], width, height, faceWidth * 0.006);
        AddMakeupPolygon(brow, pointMap, [336, 296, 334, 293, 300, 276, 283, 282, 295, 285], width, height, faceWidth * 0.006);
        AddMakeupPath(browShape, pointMap, [70, 63, 105, 66, 107], width, height, faceWidth * 0.020);
        AddMakeupPath(browShape, pointMap, [336, 296, 334, 293, 300], width, height, faceWidth * 0.020);

        AddSingleEyeMakeupMasks(pointMap, true, width, height, faceWidth, faceHeight, eyeShadow, eyeLine);
        AddSingleEyeMakeupMasks(pointMap, false, width, height, faceWidth, faceHeight, eyeShadow, eyeLine);

        AddSingleCheekMakeupMasks(pointMap, true, width, height, faceWidth, faceHeight, cheekBlush, cheekContour, cheekHighlight);
        AddSingleCheekMakeupMasks(pointMap, false, width, height, faceWidth, faceHeight, cheekBlush, cheekContour, cheekHighlight);

        AddMakeupPolygon(
            lip,
            pointMap,
            [61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, 409, 270, 269, 267, 0, 37, 39, 40, 185],
            width,
            height,
            faceWidth * 0.008);
        if (pointMap.TryGetValue(13, out Point upperLip) && pointMap.TryGetValue(14, out Point lowerLip))
        {
            Point center = new((upperLip.X + lowerLip.X) * 0.5, (upperLip.Y + lowerLip.Y) * 0.5);
            AddWrinkleEllipse(lipGloss, lip, width, height, center.X, center.Y - (faceHeight * 0.010), faceWidth * 0.105, faceHeight * 0.018, 1.0);
        }

        return true;
    }

    private static void AddSingleEyeMakeupMasks(
        IReadOnlyDictionary<int, Point> pointMap,
        bool isLeft,
        int width,
        int height,
        double faceWidth,
        double faceHeight,
        byte[] eyeShadow,
        byte[] eyeLine)
    {
        int outerIndex = isLeft ? 33 : 263;
        int innerIndex = isLeft ? 133 : 362;
        int topIndex = isLeft ? 159 : 386;
        if (!pointMap.TryGetValue(outerIndex, out Point outer) ||
            !pointMap.TryGetValue(innerIndex, out Point inner) ||
            !pointMap.TryGetValue(topIndex, out Point top))
        {
            return;
        }

        Point center = new((outer.X + inner.X + top.X) / 3.0, top.Y - (faceHeight * 0.030));
        AddWrinkleEllipse(eyeShadow, null, width, height, center.X, center.Y, faceWidth * 0.125, faceHeight * 0.060, 1.0);
        AddMakeupPath(
            eyeLine,
            pointMap,
            isLeft ? [33, 160, 159, 158, 133] : [362, 385, 386, 387, 263],
            width,
            height,
            faceWidth * 0.010);
    }

    private static void AddSingleCheekMakeupMasks(
        IReadOnlyDictionary<int, Point> pointMap,
        bool isLeft,
        int width,
        int height,
        double faceWidth,
        double faceHeight,
        byte[] cheekBlush,
        byte[] cheekContour,
        byte[] cheekHighlight)
    {
        int cheekIndex = isLeft ? 205 : 425;
        int sideIndex = isLeft ? 234 : 454;
        if (!pointMap.TryGetValue(cheekIndex, out Point cheek) || !pointMap.TryGetValue(sideIndex, out Point side))
        {
            return;
        }

        AddWrinkleEllipse(cheekBlush, null, width, height, cheek.X, cheek.Y, faceWidth * 0.155, faceHeight * 0.095, 1.0);
        AddWrinkleCapsule(cheekContour, null, width, height, side, cheek, faceWidth * 0.060, 1.0);
        AddWrinkleEllipse(cheekHighlight, null, width, height, cheek.X, cheek.Y - (faceHeight * 0.055), faceWidth * 0.125, faceHeight * 0.042, 1.0);
    }

    private static void AddMakeupPolygon(
        byte[] target,
        IReadOnlyDictionary<int, Point> pointMap,
        IReadOnlyList<int> indices,
        int width,
        int height,
        double featherRadius)
    {
        List<Point> polygon = indices.Where(pointMap.ContainsKey).Select(index => pointMap[index]).ToList();
        if (polygon.Count < 3)
        {
            return;
        }

        byte[] localMask = new byte[width * height];
        FillSkinPolygon(localMask, width, height, polygon);
        int radius = Math.Clamp((int)Math.Round(featherRadius), 1, 24);
        byte[] feathered = BoxBlurGray8(localMask, width, height, radius);
        for (int i = 0; i < target.Length; i++)
        {
            if (feathered[i] > target[i])
            {
                target[i] = feathered[i];
            }
        }
    }

    private static void AddMakeupPath(
        byte[] target,
        IReadOnlyDictionary<int, Point> pointMap,
        IReadOnlyList<int> indices,
        int width,
        int height,
        double radius)
    {
        for (int i = 0; i + 1 < indices.Count; i++)
        {
            if (pointMap.TryGetValue(indices[i], out Point start) && pointMap.TryGetValue(indices[i + 1], out Point end))
            {
                AddWrinkleCapsule(target, null, width, height, start, end, radius, 1.0);
            }
        }
    }

    private static double MaskAmount(byte[] mask, int index)
    {
        return mask[index] / 255.0;
    }

    private static void BlendMakeupTint(
        ref double blue,
        ref double green,
        ref double red,
        double targetBlue,
        double targetGreen,
        double targetRed,
        double amount)
    {
        double clamped = Math.Clamp(amount, 0.0, 1.0);
        blue += (targetBlue - blue) * clamped;
        green += (targetGreen - green) * clamped;
        red += (targetRed - red) * clamped;
    }

    private sealed record MakeupRegionMasks(
        byte[] Brow,
        byte[] BrowShape,
        byte[] EyeShadow,
        byte[] EyeLine,
        byte[] CheekBlush,
        byte[] CheekContour,
        byte[] CheekHighlight,
        byte[] Lip,
        byte[] LipGloss);
}
