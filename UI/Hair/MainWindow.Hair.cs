using KRetouchStudio.Pipeline;
using KRetouchStudio.Tabs;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace KRetouchStudio;

public partial class MainWindow
{
    private bool _isHairCommitRunning;
    private bool _hasPendingHairCommitRequest;
    private HairAdjustmentEventArgs? _pendingHairCommitArgs;

    private async void HairRetouchTab_HairAdjustmentPreviewChanged(object? sender, HairAdjustmentEventArgs e)
    {
        try
        {
            if (SelectedPhoto is not PhotoItem photo)
            {
                return;
            }

            SetCommittedHairSectionState(HasEffectiveHairAdjustment(e.Snapshot) ? e.Snapshot : null);
            PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Hair);
            await RenderAndPublishPhotoEditPipelineAsync(
                photo,
                RetouchStageId.Hair,
                RetouchRenderQuality.Preview,
                "Hair");
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = $"Hair preview failed: {ex.Message}";
        }
    }

    private async void HairRetouchTab_HairAdjustmentCommitted(object? sender, HairAdjustmentEventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        SetCommittedHairSectionState(HasEffectiveHairAdjustment(e.Snapshot) ? e.Snapshot : null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Hair);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Hair,
            RetouchRenderQuality.FullResolution,
            "Hair");
        if (applied)
        {
            PushOrReplaceHairHistory(photo, e.OperationId, e.Value);
            UpdateHairHistoryResetState();
        }
    }

    private async void HairRetouchTab_HairResetRequested(object? sender, EventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        SetCommittedHairSectionState(null);
        HairRetouchTab.RestoreSnapshot(null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.Hair);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.Hair,
            RetouchRenderQuality.FullResolution,
            "Hair Reset");
        if (applied)
        {
            PushEditorHistorySnapshot(HairHistoryTitle, HairResetHistoryDetail);
            UpdateHairHistoryResetState();
        }
    }

    private async Task ApplyHairDragPreviewAsync(HairAdjustmentEventArgs args)
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
                _committedMakeupSectionState,
                args.Snapshot))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Hair: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Hair");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Hair: face landmarks unavailable";
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
            _committedMakeupSectionState,
            args.Snapshot,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Hair: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplyHairCommittedAsync(HairAdjustmentEventArgs args)
    {
        if (_isHairCommitRunning)
        {
            _pendingHairCommitArgs = args;
            _hasPendingHairCommitRequest = true;
            return;
        }

        _isHairCommitRunning = true;
        try
        {
            HairAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingHairCommitRequest = false;
                _pendingHairCommitArgs = null;
                await ApplyHairCommittedCoreAsync(currentArgs);
                if (_hasPendingHairCommitRequest && _pendingHairCommitArgs is not null)
                {
                    currentArgs = _pendingHairCommitArgs;
                }
            }
            while (_hasPendingHairCommitRequest);
        }
        finally
        {
            _isHairCommitRunning = false;
        }
    }

    private async Task ApplyHairCommittedCoreAsync(HairAdjustmentEventArgs args)
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
            MediaPipeStatusText = "Hair: original-size image only";
            return;
        }

        HairAdjustmentSnapshot? requestedHairState = HasEffectiveHairAdjustment(args.Snapshot)
            ? args.Snapshot
            : null;
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                _committedBlemishSectionState,
                _committedWrinkleSectionState,
                _committedMakeupSectionState,
                requestedHairState))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            SetCommittedHairSectionState(null);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            PushOrReplaceHairHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            UpdateHairHistoryResetState();
            MediaPipeStatusText = "Hair: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Hair");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Hair: face landmarks unavailable";
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
            _committedMakeupSectionState,
            requestedHairState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        SetCommittedHairSectionState(requestedHairState);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceHairHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        UpdateHairHistoryResetState();
        MediaPipeStatusText = $"Hair: applied {args.OperationId} {args.Value:0}";
    }

    private bool IsCurrentHistoryHairEffect()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, HairHistoryTitle, StringComparison.Ordinal);
    }

    private void PushOrReplaceHairHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = string.IsNullOrWhiteSpace(operationId)
            ? HairHistoryDetail
            : $"{operationId} {value:0}";
        if (IsCurrentHistoryHairEffect())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, HairHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(HairHistoryTitle, detail);
    }

    private async Task TryResetHairSectionAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || _committedHairSectionState is null)
        {
            UpdateHairHistoryResetState();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource resetResult = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        if ((_committedSkinSectionState is not null && HasEffectiveSkinAdjustment(_committedSkinSectionState)) ||
            (_committedBlemishSectionState is not null && HasEffectiveBlemishAdjustment(_committedBlemishSectionState)) ||
            (_committedWrinkleSectionState is not null && HasEffectiveWrinkleAdjustment(_committedWrinkleSectionState)) ||
            (_committedMakeupSectionState is not null && HasEffectiveMakeupAdjustment(_committedMakeupSectionState)))
        {
            List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Hair Reset");
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }

            if (landmarks.Count == 0)
            {
                MediaPipeStatusText = "Hair reset: face landmarks unavailable";
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
                _committedMakeupSectionState,
                null,
                () => renderVersion != _connectedRetouchRenderVersion));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }
        }

        targetPhoto.SetAdjustedImage(resetResult);
        SetCommittedHairSectionState(null);
        HairRetouchTab.ResetAfterHistoryReset();
        ClearHairRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(HairHistoryTitle, HairResetHistoryDetail);
        UpdateHairHistoryResetState();
        MediaPipeStatusText = "Hair: reset";
    }

    private void UpdateHairHistoryResetState()
    {
        HairRetouchTab.CanResetHairTab = _committedHairSectionState is not null;
    }

    private void ClearHairRetouchSession()
    {
        _pendingHairCommitArgs = null;
        _hasPendingHairCommitRequest = false;
        Interlocked.Increment(ref _connectedRetouchRenderVersion);
    }

    private static bool HasEffectiveHairAdjustment(HairAdjustmentSnapshot snapshot)
    {
        return HasCenteredEffect(snapshot.HairlineHeight) ||
               HasCenteredEffect(snapshot.TempleBalance) ||
               HasCenteredEffect(snapshot.BabyHairProtect) ||
               HasCenteredEffect(snapshot.TopVolume) ||
               HasCenteredEffect(snapshot.SideVolume) ||
               HasCenteredEffect(snapshot.CrownLift) ||
               snapshot.StrayHair > 0.001 ||
               snapshot.Frizz > 0.001 ||
               snapshot.EdgeCleanup > 0.001 ||
               snapshot.Shine > 0.001 ||
               snapshot.Depth > 0.001 ||
               snapshot.ScalpCover > 0.001 ||
               snapshot.Tint > 0.001 ||
               HasCenteredEffect(snapshot.WarmCool) ||
               snapshot.Darken > 0.001 ||
               snapshot.ColorStrength > 0.001;
    }

    private static BitmapSource RenderHairAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        HairAdjustmentSnapshot snapshot,
        Func<bool> shouldCancel)
    {
        BitmapSource geometryResult = ApplyHairGeometryAdjustments(source, pointMap, snapshot, shouldCancel);
        if (shouldCancel())
        {
            return geometryResult;
        }

        return ApplyHairPixelAdjustments(geometryResult, pointMap, snapshot, shouldCancel);
    }

    private static BitmapSource ApplyHairGeometryAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        HairAdjustmentSnapshot snapshot,
        Func<bool> shouldCancel)
    {
        double hairline = (snapshot.HairlineHeight - 50.0) / 50.0;
        double temple = (snapshot.TempleBalance - 50.0) / 50.0;
        double top = (snapshot.TopVolume - 50.0) / 50.0;
        double side = (snapshot.SideVolume - 50.0) / 50.0;
        double crown = (snapshot.CrownLift - 50.0) / 50.0;
        if (Math.Abs(hairline) <= 0.001 &&
            Math.Abs(temple) <= 0.001 &&
            Math.Abs(top) <= 0.001 &&
            Math.Abs(side) <= 0.001 &&
            Math.Abs(crown) <= 0.001)
        {
            return source.IsFrozen ? source : CloneBitmapSource(source);
        }

        if (!TryGetHairGeometry(pointMap, source.PixelWidth, source.PixelHeight, out HairGeometry geometry))
        {
            return source.IsFrozen ? source : CloneBitmapSource(source);
        }

        double w = geometry.FaceWidth;
        double h = geometry.FaceHeight;
        double centerX = geometry.CenterX;
        double topY = geometry.FaceTop;
        double hairlineDy = hairline * h * 0.075;
        double topDy = -top * h * 0.095;
        double crownDy = -crown * h * 0.080;
        double sideDx = side * w * 0.090;
        double templeDx = temple * w * 0.050;
        List<FaceShapeControlPoint> controls =
        [
            new(centerX - (w * 0.32), topY - (h * 0.015), 0, hairlineDy * 0.72),
            new(centerX, topY - (h * 0.035), 0, hairlineDy),
            new(centerX + (w * 0.32), topY - (h * 0.015), 0, hairlineDy * 0.72),
            new(centerX - (w * 0.22), topY - (h * 0.34), 0, topDy * 0.82),
            new(centerX + (w * 0.22), topY - (h * 0.34), 0, topDy * 0.82),
            new(centerX, topY - (h * 0.55), 0, topDy + crownDy),
            new(geometry.FaceLeft - (w * 0.12), topY - (h * 0.02), -sideDx + templeDx, topDy * 0.35),
            new(geometry.FaceRight + (w * 0.12), topY - (h * 0.02), sideDx - templeDx, topDy * 0.35),
            new(geometry.FaceLeft - (w * 0.16), topY + (h * 0.20), -sideDx * 0.72 + templeDx, 0),
            new(geometry.FaceRight + (w * 0.16), topY + (h * 0.20), sideDx * 0.72 - templeDx, 0),
            new(centerX - (w * 0.28), topY + (h * 0.28), 0, 0),
            new(centerX + (w * 0.28), topY + (h * 0.28), 0, 0),
            new(centerX, topY + (h * 0.34), 0, 0)
        ];

        Rect bounds = new(
            Math.Max(0, centerX - (w * 0.95)),
            Math.Max(0, topY - (h * 0.78)),
            Math.Min(source.PixelWidth, centerX + (w * 0.95)) - Math.Max(0, centerX - (w * 0.95)),
            Math.Min(source.PixelHeight, topY + (h * 0.45)) - Math.Max(0, topY - (h * 0.78)));
        return BuildFaceShapeControlWarpPreview(
            EnsureBitmapFormat(source, PixelFormats.Bgra32),
            controls,
            Math.Max(18.0, w * 0.18),
            CreateFaceDetailWeightProfile(bounds, 1.0, 0.48),
            shouldCancel);
    }

    private static BitmapSource ApplyHairPixelAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        HairAdjustmentSnapshot snapshot,
        Func<bool> shouldCancel)
    {
        bool hasPixelEffect = snapshot.StrayHair > 0.001 ||
                              snapshot.Frizz > 0.001 ||
                              snapshot.EdgeCleanup > 0.001 ||
                              HasCenteredEffect(snapshot.BabyHairProtect) ||
                              snapshot.Shine > 0.001 ||
                              snapshot.Depth > 0.001 ||
                              snapshot.ScalpCover > 0.001 ||
                              snapshot.Tint > 0.001 ||
                              HasCenteredEffect(snapshot.WarmCool) ||
                              snapshot.Darken > 0.001 ||
                              snapshot.ColorStrength > 0.001;
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        if (!hasPixelEffect ||
            !TryGetHairGeometry(pointMap, bgraSource.PixelWidth, bgraSource.PixelHeight, out HairGeometry geometry))
        {
            return bgraSource.IsFrozen ? bgraSource : CloneBitmapSource(bgraSource);
        }

        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] hairMask = BuildRoughHairMask(sourcePixels, width, height, stride, geometry);
        int blurRadius = Math.Clamp((int)Math.Round(geometry.FaceWidth * 0.016), 2, 48);
        byte[] smoothPixels = BoxBlurBgra32(sourcePixels, width, height, stride, blurRadius);
        byte[] outputPixels = (byte[])sourcePixels.Clone();

        double cleanup = Math.Clamp((snapshot.StrayHair * 0.34 + snapshot.Frizz * 0.46 + snapshot.EdgeCleanup * 0.52) / 100.0, 0.0, 1.0);
        double shine = snapshot.Shine / 100.0;
        double depth = snapshot.Depth / 100.0;
        double scalpCover = snapshot.ScalpCover / 100.0;
        double tint = snapshot.Tint / 100.0;
        double warmCool = (snapshot.WarmCool - 50.0) / 50.0;
        double darken = snapshot.Darken / 100.0;
        double colorStrength = snapshot.ColorStrength / 100.0;
        double protect = Math.Clamp(snapshot.BabyHairProtect / 100.0, 0.0, 1.0);
        double babyHairDelta = (snapshot.BabyHairProtect - 50.0) / 50.0;

        for (int y = 0; y < height; y++)
        {
            if ((y & 31) == 0 && shouldCancel())
            {
                return CloneBitmapSource(bgraSource);
            }

            int row = y * stride;
            double hairlineDistance = Math.Abs(y - geometry.FaceTop) / Math.Max(1.0, geometry.FaceHeight * 0.10);
            double hairlineProtection = 1.0 - (Math.Exp(-(hairlineDistance * hairlineDistance)) * protect * 0.78);
            for (int x = 0; x < width; x++)
            {
                int maskIndex = (y * width) + x;
                double mask = hairMask[maskIndex] / 255.0;
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
                double protectedMask = mask * hairlineProtection;

                double cleanupAmount = protectedMask * cleanup;
                b += (smoothPixels[index] - b) * cleanupAmount;
                g += (smoothPixels[index + 1] - g) * cleanupAmount;
                r += (smoothPixels[index + 2] - r) * cleanupAmount;

                double babyHairAmount = babyHairDelta * mask * Math.Exp(-(hairlineDistance * hairlineDistance));
                if (babyHairAmount >= 0.0)
                {
                    b += (originalB - smoothPixels[index]) * babyHairAmount * 0.50;
                    g += (originalG - smoothPixels[index + 1]) * babyHairAmount * 0.50;
                    r += (originalR - smoothPixels[index + 2]) * babyHairAmount * 0.50;
                }
                else
                {
                    double softenAmount = -babyHairAmount * 0.34;
                    b += (smoothPixels[index] - b) * softenAmount;
                    g += (smoothPixels[index + 1] - g) * softenAmount;
                    r += (smoothPixels[index + 2] - r) * softenAmount;
                }

                double luma = GetSkinLuma(b, g, r);
                double shineLift = shine * protectedMask * (16.0 + (18.0 * Math.Clamp((luma - 40.0) / 180.0, 0.0, 1.0)));
                b += shineLift;
                g += shineLift;
                r += shineLift;

                double depthScale = 1.0 - (depth * protectedMask * 0.34);
                b *= depthScale;
                g *= depthScale;
                r *= depthScale;

                double scalpZone = Math.Exp(-Math.Pow((x - geometry.CenterX) / Math.Max(1.0, geometry.FaceWidth * 0.22), 2.0)) *
                                   Math.Exp(-Math.Pow((y - (geometry.FaceTop - geometry.FaceHeight * 0.24)) / Math.Max(1.0, geometry.FaceHeight * 0.24), 2.0));
                double scalpAmount = scalpCover * protectedMask * scalpZone * Math.Clamp((luma - 70.0) / 150.0, 0.0, 1.0) * 0.72;
                b += (42.0 - b) * scalpAmount;
                g += (52.0 - g) * scalpAmount;
                r += (66.0 - r) * scalpAmount;

                double tintAmount = tint * protectedMask * 0.48;
                b += (52.0 - b) * tintAmount;
                g += (68.0 - g) * tintAmount;
                r += (96.0 - r) * tintAmount;

                b -= warmCool * protectedMask * 18.0;
                r += warmCool * protectedMask * 18.0;

                double darkenScale = 1.0 - (darken * protectedMask * 0.48);
                b *= darkenScale;
                g *= darkenScale;
                r *= darkenScale;

                if (colorStrength > 0.001)
                {
                    double adjustedLuma = GetSkinLuma(b, g, r);
                    double saturationScale = 1.0 + (colorStrength * protectedMask * 0.90);
                    b = adjustedLuma + ((b - adjustedLuma) * saturationScale);
                    g = adjustedLuma + ((g - adjustedLuma) * saturationScale);
                    r = adjustedLuma + ((r - adjustedLuma) * saturationScale);
                }

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

    private static bool TryGetHairGeometry(
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        out HairGeometry geometry)
    {
        List<Point> oval = FaceShapeHeadTiltFaceOvalIndices
            .Where(pointMap.ContainsKey)
            .Select(index => pointMap[index])
            .ToList();
        if (oval.Count < 12)
        {
            geometry = default;
            return false;
        }

        double left = Math.Clamp(oval.Min(point => point.X), 0, width - 1);
        double right = Math.Clamp(oval.Max(point => point.X), 0, width - 1);
        double top = Math.Clamp(oval.Min(point => point.Y), 0, height - 1);
        double bottom = Math.Clamp(oval.Max(point => point.Y), 0, height - 1);
        geometry = new HairGeometry(left, right, top, bottom, (left + right) * 0.5, Math.Max(1.0, right - left), Math.Max(1.0, bottom - top), oval);
        return geometry.FaceWidth >= 20 && geometry.FaceHeight >= 20;
    }

    private static byte[] BuildRoughHairMask(
        byte[] pixels,
        int width,
        int height,
        int stride,
        HairGeometry geometry)
    {
        byte[] region = new byte[width * height];
        double centerY = geometry.FaceTop - (geometry.FaceHeight * 0.10);
        AddWrinkleEllipse(
            region,
            null,
            width,
            height,
            geometry.CenterX,
            centerY,
            geometry.FaceWidth * 0.82,
            geometry.FaceHeight * 0.66,
            1.0);
        AddWrinkleCapsule(
            region,
            null,
            width,
            height,
            new Point(geometry.FaceLeft - geometry.FaceWidth * 0.18, geometry.FaceTop - geometry.FaceHeight * 0.10),
            new Point(geometry.FaceLeft - geometry.FaceWidth * 0.12, geometry.FaceTop + geometry.FaceHeight * 0.48),
            geometry.FaceWidth * 0.22,
            1.0);
        AddWrinkleCapsule(
            region,
            null,
            width,
            height,
            new Point(geometry.FaceRight + geometry.FaceWidth * 0.18, geometry.FaceTop - geometry.FaceHeight * 0.10),
            new Point(geometry.FaceRight + geometry.FaceWidth * 0.12, geometry.FaceTop + geometry.FaceHeight * 0.48),
            geometry.FaceWidth * 0.22,
            1.0);

        byte[] faceMask = new byte[width * height];
        FillSkinPolygon(faceMask, width, height, geometry.FaceOval);
        faceMask = BoxBlurGray8(faceMask, width, height, Math.Clamp((int)Math.Round(geometry.FaceWidth * 0.018), 2, 36));
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int maskIndex = (y * width) + x;
                int index = row + (x * 4);
                double outsideFace = 1.0 - (faceMask[maskIndex] / 255.0);
                double luma = GetSkinLuma(pixels[index], pixels[index + 1], pixels[index + 2]);
                double chroma = Math.Max(pixels[index], Math.Max(pixels[index + 1], pixels[index + 2])) -
                                Math.Min(pixels[index], Math.Min(pixels[index + 1], pixels[index + 2]));
                double backgroundGuard = luma > 242 && chroma < 14 ? 0.04 : luma > 226 && chroma < 10 ? 0.30 : 1.0;
                region[maskIndex] = (byte)Math.Clamp(Math.Round(region[maskIndex] * outsideFace * backgroundGuard), 0, 255);
            }
        }

        return BoxBlurGray8(region, width, height, Math.Clamp((int)Math.Round(geometry.FaceWidth * 0.010), 1, 24));
    }

    private readonly record struct HairGeometry(
        double FaceLeft,
        double FaceRight,
        double FaceTop,
        double FaceBottom,
        double CenterX,
        double FaceWidth,
        double FaceHeight,
        IReadOnlyList<Point> FaceOval);
}
