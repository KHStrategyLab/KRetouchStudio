using KRetouchStudio.Tabs;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private bool _isSkinCommitRunning;
    private bool _hasPendingSkinCommitRequest;
    private SkinAdjustmentEventArgs? _pendingSkinCommitArgs;

    private async void SkinRetouchTab_SkinAdjustmentPreviewChanged(object? sender, SkinAdjustmentEventArgs e)
    {
        try
        {
            await ApplySkinDragPreviewAsync(e);
        }
        catch (Exception ex)
        {
            MediaPipeStatusText = $"Skin preview failed: {ex.Message}";
        }
    }

    private async void SkinRetouchTab_SkinAdjustmentCommitted(object? sender, SkinAdjustmentEventArgs e)
    {
        await ApplySkinCommittedAsync(e);
    }

    private async void SkinRetouchTab_SkinResetRequested(object? sender, EventArgs e)
    {
        await TryResetSkinSectionAsync();
    }

    private async Task ApplySkinDragPreviewAsync(SkinAdjustmentEventArgs args)
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);
        if (!HasEffectiveConnectedRetouchAdjustment(args.Snapshot, _committedBlemishSectionState))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Skin: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Skin");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Skin: face landmarks unavailable";
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
            args.Snapshot,
            _committedBlemishSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Skin: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplySkinCommittedAsync(SkinAdjustmentEventArgs args)
    {
        if (_isSkinCommitRunning)
        {
            _pendingSkinCommitArgs = args;
            _hasPendingSkinCommitRequest = true;
            return;
        }

        _isSkinCommitRunning = true;
        try
        {
            SkinAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingSkinCommitRequest = false;
                _pendingSkinCommitArgs = null;
                await ApplySkinCommittedCoreAsync(currentArgs);
                if (_hasPendingSkinCommitRequest && _pendingSkinCommitArgs is not null)
                {
                    currentArgs = _pendingSkinCommitArgs;
                }
            }
            while (_hasPendingSkinCommitRequest);
        }
        finally
        {
            _isSkinCommitRunning = false;
        }
    }

    private async Task ApplySkinCommittedCoreAsync(SkinAdjustmentEventArgs args)
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
            MediaPipeStatusText = "Skin: original-size image only";
            return;
        }

        SkinAdjustmentSnapshot? requestedSkinState = HasEffectiveSkinAdjustment(args.Snapshot)
            ? args.Snapshot
            : null;
        if (!HasEffectiveConnectedRetouchAdjustment(requestedSkinState, _committedBlemishSectionState))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            SetCommittedSkinSectionState(null);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            PushOrReplaceSkinHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            UpdateSkinHistoryResetState();
            MediaPipeStatusText = "Skin: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Skin");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Skin: face landmarks unavailable";
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
            requestedSkinState,
            _committedBlemishSectionState,
            () => renderVersion != _connectedRetouchRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        SetCommittedSkinSectionState(requestedSkinState);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceSkinHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        UpdateSkinHistoryResetState();
        MediaPipeStatusText = $"Skin: applied {args.OperationId} {args.Value:0}";
    }

    private bool IsCurrentHistorySkinEffect()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, SkinHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(SkinHistoryDetail + " ", StringComparison.Ordinal);
    }

    private void PushOrReplaceSkinHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = $"{SkinHistoryDetail} {operationId} {Math.Clamp(Math.Round(value), 0, 100):0}";
        if (IsCurrentHistorySkinEffect())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, SkinHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(SkinHistoryTitle, detail);
    }

    private async Task TryResetSkinSectionAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || _committedSkinSectionState is null)
        {
            UpdateSkinHistoryResetState();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        BitmapSource baseSource = GetConnectedRetouchRenderSource(targetPhoto);
        BitmapSource resetResult = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        if (_committedBlemishSectionState is not null && HasEffectiveBlemishAdjustment(_committedBlemishSectionState))
        {
            List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Skin Reset");
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }

            if (landmarks.Count == 0)
            {
                MediaPipeStatusText = "Skin reset: face landmarks unavailable";
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
                null,
                _committedBlemishSectionState,
                () => renderVersion != _connectedRetouchRenderVersion));
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) || renderVersion != _connectedRetouchRenderVersion)
            {
                return;
            }
        }

        targetPhoto.SetAdjustedImage(resetResult);
        SetCommittedSkinSectionState(null);
        SkinRetouchTab.ResetAfterHistoryReset();
        ClearSkinRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(SkinHistoryTitle, SkinResetHistoryDetail);
        UpdateSkinHistoryResetState();
        MediaPipeStatusText = "Skin: reset";
    }

    private void UpdateSkinHistoryResetState()
    {
        SkinRetouchTab.CanResetSkinTab = _committedSkinSectionState is not null;
    }

    private void ClearSkinRetouchSession()
    {
        _pendingSkinCommitArgs = null;
        _hasPendingSkinCommitRequest = false;
        Interlocked.Increment(ref _connectedRetouchRenderVersion);
    }

    private static bool HasEffectiveSkinAdjustment(SkinAdjustmentSnapshot snapshot)
    {
        return snapshot.EvenTone > 0.001 ||
               snapshot.ToneLift > 0.001 ||
               snapshot.ColorCast > 0.001 ||
               snapshot.Softness > 0.001 ||
               snapshot.TextureProtect > 0.001 ||
               snapshot.DetailReturn > 0.001 ||
               snapshot.PoreReduce > 0.001 ||
               snapshot.FineTexture > 0.001 ||
               snapshot.PoreEdgeProtect > 0.001 ||
               snapshot.RedReduce > 0.001 ||
               snapshot.ToneBlend > 0.001 ||
               snapshot.NaturalColor > 0.001 ||
               snapshot.ShineReduce > 0.001 ||
               snapshot.HighlightProtect > 0.001 ||
               snapshot.ShineTextureReturn > 0.001;
    }

    private static BitmapSource RenderSkinAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        SkinAdjustmentSnapshot snapshot,
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

        int fineRadius = Math.Clamp((int)Math.Round(faceWidth * 0.005), 1, 18);
        int broadRadius = Math.Clamp((int)Math.Round(faceWidth * 0.018), fineRadius + 1, 64);
        byte[] fineBlur = BoxBlurBgra32(sourcePixels, width, height, stride, fineRadius);
        byte[] broadBlur = BoxBlurBgra32(sourcePixels, width, height, stride, broadRadius);
        if (shouldCancel())
        {
            return CloneBitmapSource(bgraSource);
        }

        byte[] outputPixels = (byte[])sourcePixels.Clone();
        double evenTone = snapshot.EvenTone / 100.0;
        double toneLift = snapshot.ToneLift / 100.0;
        double colorCast = snapshot.ColorCast / 100.0;
        double softness = snapshot.Softness / 100.0;
        double textureProtect = snapshot.TextureProtect / 100.0;
        double detailReturn = snapshot.DetailReturn / 100.0;
        double poreReduce = snapshot.PoreReduce / 100.0;
        double fineTexture = snapshot.FineTexture / 100.0;
        double edgeProtect = snapshot.PoreEdgeProtect / 100.0;
        double redReduce = snapshot.RedReduce / 100.0;
        double toneBlend = snapshot.ToneBlend / 100.0;
        double naturalColor = snapshot.NaturalColor / 100.0;
        double shineReduce = snapshot.ShineReduce / 100.0;
        double highlightProtect = snapshot.HighlightProtect / 100.0;
        double shineTextureReturn = snapshot.ShineTextureReturn / 100.0;

        for (int y = 0; y < height; y++)
        {
            if ((y & 31) == 0 && shouldCancel())
            {
                return CloneBitmapSource(bgraSource);
            }

            int previousRow = Math.Max(0, y - 1) * stride;
            int nextRow = Math.Min(height - 1, y + 1) * stride;
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
                double broadB = broadBlur[index];
                double broadG = broadBlur[index + 1];
                double broadR = broadBlur[index + 2];
                double fineB = fineBlur[index];
                double fineG = fineBlur[index + 1];
                double fineR = fineBlur[index + 2];

                double originalLuma = GetSkinLuma(originalB, originalG, originalR);
                double maxChannel = Math.Max(originalR, Math.Max(originalG, originalB));
                double minChannel = Math.Min(originalR, Math.Min(originalG, originalB));
                double saturation = maxChannel <= 1 ? 0 : (maxChannel - minChannel) / maxChannel;
                double skinGate = Math.Clamp((originalLuma - 24.0) / 38.0, 0.12, 1.0);
                if (saturation > 0.72)
                {
                    skinGate *= Math.Clamp(1.0 - ((saturation - 0.72) * 2.2), 0.18, 1.0);
                }

                mask *= skinGate;
                double localEdge = GetSkinEdgeStrength(sourcePixels, width, stride, x, previousRow, row, nextRow);
                double edgeGate = 1.0 - (edgeProtect * Math.Clamp(localEdge / 44.0, 0.0, 0.92));

                double evenAmount = evenTone * 0.34;
                b += (broadB - b) * evenAmount;
                g += (broadG - g) * evenAmount;
                r += (broadR - r) * evenAmount;

                double lift = toneLift * 18.0 * (1.0 - (originalLuma / 255.0 * 0.35));
                b += lift;
                g += lift;
                r += lift;

                if (colorCast > 0.001)
                {
                    double luma = GetSkinLuma(b, g, r);
                    double neutralAmount = colorCast * 0.28;
                    b += (luma - b) * neutralAmount;
                    g += (luma - g) * neutralAmount;
                    r += (luma - r) * neutralAmount;
                }

                double softnessAmount = softness * 0.54 * (1.0 - (textureProtect * 0.82)) * edgeGate;
                double softB = (fineB * 0.78) + (broadB * 0.22);
                double softG = (fineG * 0.78) + (broadG * 0.22);
                double softR = (fineR * 0.78) + (broadR * 0.22);
                b += (softB - b) * softnessAmount;
                g += (softG - g) * softnessAmount;
                r += (softR - r) * softnessAmount;

                double poreAmount = poreReduce * 0.46 * edgeGate;
                b += (fineB - b) * poreAmount;
                g += (fineG - g) * poreAmount;
                r += (fineR - r) * poreAmount;

                double detailAmount = (detailReturn * 0.34) + (fineTexture * 0.28);
                b += (originalB - fineB) * detailAmount;
                g += (originalG - fineG) * detailAmount;
                r += (originalR - fineR) * detailAmount;

                double redExcess = Math.Max(0.0, r - ((g * 0.68) + (b * 0.32)) - 3.0);
                double rednessGate = Math.Clamp(redExcess / 42.0, 0.0, 1.0);
                if (redReduce > 0.001 && rednessGate > 0.001)
                {
                    double reduction = redExcess * redReduce * 0.62;
                    r -= reduction;
                    g += reduction * 0.12;
                    b += reduction * 0.05;
                }

                double redBlendAmount = toneBlend * rednessGate * 0.30;
                b += (broadB - b) * redBlendAmount;
                g += (broadG - g) * redBlendAmount;
                r += (broadR - r) * redBlendAmount;

                if (naturalColor > 0.001 && (redReduce > 0.001 || toneBlend > 0.001))
                {
                    double currentLuma = GetSkinLuma(b, g, r);
                    double restoreAmount = naturalColor * 0.62;
                    b += ((currentLuma + (originalB - originalLuma)) - b) * restoreAmount;
                    g += ((currentLuma + (originalG - originalLuma)) - g) * restoreAmount;
                    r += ((currentLuma + (originalR - originalLuma)) - r) * restoreAmount;
                }

                if (shineReduce > 0.001)
                {
                    double currentLuma = GetSkinLuma(b, g, r);
                    double broadLuma = GetSkinLuma(broadB, broadG, broadR);
                    double highlightExcess = Math.Max(0.0, currentLuma - broadLuma - 5.0);
                    double naturalHighlight = Math.Clamp((broadLuma - 155.0) / 70.0, 0.0, 1.0);
                    double protect = 1.0 - (highlightProtect * naturalHighlight * 0.82);
                    double reduction = highlightExcess * shineReduce * 0.78 * protect;
                    b -= reduction;
                    g -= reduction;
                    r -= reduction;
                }

                double shineDetailAmount = shineTextureReturn * 0.26;
                b += (originalB - fineB) * shineDetailAmount;
                g += (originalG - fineG) * shineDetailAmount;
                r += (originalR - fineR) * shineDetailAmount;

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

    private static bool TryBuildSkinMask(
        IReadOnlyDictionary<int, Point> pointMap,
        int width,
        int height,
        out byte[] mask,
        out double faceWidth)
    {
        mask = new byte[width * height];
        faceWidth = 0;
        List<Point> facePolygon = [];
        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            if (pointMap.TryGetValue(index, out Point point))
            {
                facePolygon.Add(point);
            }
        }

        if (facePolygon.Count < 12)
        {
            return false;
        }

        double minX = facePolygon.Min(point => point.X);
        double maxX = facePolygon.Max(point => point.X);
        double minY = facePolygon.Min(point => point.Y);
        double maxY = facePolygon.Max(point => point.Y);
        faceWidth = Math.Max(1.0, maxX - minX);
        FillSkinPolygon(mask, width, height, facePolygon);

        int featherRadius = Math.Clamp((int)Math.Round(faceWidth * 0.018), 2, 48);
        byte[] feathered = BoxBlurGray8(mask, width, height, featherRadius);
        byte[] protection = Enumerable.Repeat((byte)255, mask.Length).ToArray();

        ApplySkinFeatureProtection(protection, width, height, pointMap, [33, 133, 159, 145, 160, 144, 158, 153], 0.35, 0.80);
        ApplySkinFeatureProtection(protection, width, height, pointMap, [362, 263, 386, 374, 385, 380, 387, 373], 0.35, 0.80);
        ApplySkinFeatureProtection(protection, width, height, pointMap, [70, 63, 105, 66, 107, 55, 65], 0.28, 0.55);
        ApplySkinFeatureProtection(protection, width, height, pointMap, [336, 296, 334, 293, 300, 285, 295], 0.28, 0.55);
        ApplySkinFeatureProtection(
            protection,
            width,
            height,
            pointMap,
            [61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, 308, 415, 310, 311, 312, 13, 82, 81, 80, 78],
            0.18,
            0.38);

        if (pointMap.TryGetValue(98, out Point leftNostril))
        {
            ApplySkinEllipseProtection(protection, width, height, leftNostril.X, leftNostril.Y, faceWidth * 0.025, faceWidth * 0.018);
        }

        if (pointMap.TryGetValue(327, out Point rightNostril))
        {
            ApplySkinEllipseProtection(protection, width, height, rightNostril.X, rightNostril.Y, faceWidth * 0.025, faceWidth * 0.018);
        }

        int minPixelY = Math.Clamp((int)Math.Floor(minY), 0, height - 1);
        int maxPixelY = Math.Clamp((int)Math.Ceiling(maxY), 0, height - 1);
        int minPixelX = Math.Clamp((int)Math.Floor(minX), 0, width - 1);
        int maxPixelX = Math.Clamp((int)Math.Ceiling(maxX), 0, width - 1);
        for (int y = minPixelY; y <= maxPixelY; y++)
        {
            int row = y * width;
            for (int x = minPixelX; x <= maxPixelX; x++)
            {
                int index = row + x;
                if (mask[index] == 0)
                {
                    feathered[index] = 0;
                    continue;
                }

                feathered[index] = (byte)((feathered[index] * protection[index] + 127) / 255);
            }
        }

        mask = feathered;
        return true;
    }

    private static void FillSkinPolygon(byte[] mask, int width, int height, IReadOnlyList<Point> polygon)
    {
        int minY = Math.Clamp((int)Math.Floor(polygon.Min(point => point.Y)), 0, height - 1);
        int maxY = Math.Clamp((int)Math.Ceiling(polygon.Max(point => point.Y)), 0, height - 1);
        List<double> intersections = new(polygon.Count);
        for (int y = minY; y <= maxY; y++)
        {
            double scanY = y + 0.5;
            intersections.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                Point a = polygon[i];
                Point b = polygon[(i + 1) % polygon.Count];
                if ((a.Y <= scanY && b.Y > scanY) || (b.Y <= scanY && a.Y > scanY))
                {
                    double ratio = (scanY - a.Y) / (b.Y - a.Y);
                    intersections.Add(a.X + ((b.X - a.X) * ratio));
                }
            }

            intersections.Sort();
            int row = y * width;
            for (int i = 0; i + 1 < intersections.Count; i += 2)
            {
                int startX = Math.Clamp((int)Math.Ceiling(intersections[i]), 0, width - 1);
                int endX = Math.Clamp((int)Math.Floor(intersections[i + 1]), 0, width - 1);
                for (int x = startX; x <= endX; x++)
                {
                    mask[row + x] = 255;
                }
            }
        }
    }

    private static void ApplySkinFeatureProtection(
        byte[] protection,
        int width,
        int height,
        IReadOnlyDictionary<int, Point> pointMap,
        IReadOnlyList<int> indices,
        double paddingXRatio,
        double paddingYRatio)
    {
        List<Point> points = [];
        foreach (int index in indices)
        {
            if (pointMap.TryGetValue(index, out Point point))
            {
                points.Add(point);
            }
        }

        if (points.Count < 2)
        {
            return;
        }

        double minX = points.Min(point => point.X);
        double maxX = points.Max(point => point.X);
        double minY = points.Min(point => point.Y);
        double maxY = points.Max(point => point.Y);
        double featureWidth = Math.Max(2.0, maxX - minX);
        double featureHeight = Math.Max(2.0, maxY - minY);
        double radiusX = (featureWidth * 0.5) * (1.0 + (paddingXRatio * 2.0));
        double radiusY = Math.Max(featureHeight * 0.5, featureWidth * 0.09) * (1.0 + (paddingYRatio * 2.0));
        ApplySkinEllipseProtection(
            protection,
            width,
            height,
            (minX + maxX) * 0.5,
            (minY + maxY) * 0.5,
            radiusX,
            radiusY);
    }

    private static void ApplySkinEllipseProtection(
        byte[] protection,
        int width,
        int height,
        double centerX,
        double centerY,
        double radiusX,
        double radiusY)
    {
        radiusX = Math.Max(1.0, radiusX);
        radiusY = Math.Max(1.0, radiusY);
        int minX = Math.Clamp((int)Math.Floor(centerX - (radiusX * 1.35)), 0, width - 1);
        int maxX = Math.Clamp((int)Math.Ceiling(centerX + (radiusX * 1.35)), 0, width - 1);
        int minY = Math.Clamp((int)Math.Floor(centerY - (radiusY * 1.35)), 0, height - 1);
        int maxY = Math.Clamp((int)Math.Ceiling(centerY + (radiusY * 1.35)), 0, height - 1);
        for (int y = minY; y <= maxY; y++)
        {
            double normalizedY = (y - centerY) / radiusY;
            int row = y * width;
            for (int x = minX; x <= maxX; x++)
            {
                double normalizedX = (x - centerX) / radiusX;
                double distance = Math.Sqrt((normalizedX * normalizedX) + (normalizedY * normalizedY));
                if (distance >= 1.35)
                {
                    continue;
                }

                double factor = distance <= 1.0 ? 0.0 : (distance - 1.0) / 0.35;
                int index = row + x;
                protection[index] = (byte)Math.Min(protection[index], Math.Clamp((int)Math.Round(factor * 255.0), 0, 255));
            }
        }
    }

    private static byte[] BoxBlurBgra32(byte[] source, int width, int height, int stride, int radius)
    {
        if (radius <= 0)
        {
            return (byte[])source.Clone();
        }

        byte[] horizontal = new byte[source.Length];
        byte[] output = new byte[source.Length];
        int window = (radius * 2) + 1;
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int channel = 0; channel < 3; channel++)
            {
                int sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = Math.Clamp(offset, 0, width - 1);
                    sum += source[row + (sampleX * 4) + channel];
                }

                for (int x = 0; x < width; x++)
                {
                    horizontal[row + (x * 4) + channel] = (byte)((sum + (window / 2)) / window);
                    int removeX = Math.Clamp(x - radius, 0, width - 1);
                    int addX = Math.Clamp(x + radius + 1, 0, width - 1);
                    sum += source[row + (addX * 4) + channel] - source[row + (removeX * 4) + channel];
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            int columnOffset = x * 4;
            for (int channel = 0; channel < 3; channel++)
            {
                int sum = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = Math.Clamp(offset, 0, height - 1);
                    sum += horizontal[(sampleY * stride) + columnOffset + channel];
                }

                for (int y = 0; y < height; y++)
                {
                    int index = (y * stride) + columnOffset + channel;
                    output[index] = (byte)((sum + (window / 2)) / window);
                    int removeY = Math.Clamp(y - radius, 0, height - 1);
                    int addY = Math.Clamp(y + radius + 1, 0, height - 1);
                    sum += horizontal[(addY * stride) + columnOffset + channel] -
                           horizontal[(removeY * stride) + columnOffset + channel];
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            for (int x = 0; x < width; x++)
            {
                int alphaIndex = row + (x * 4) + 3;
                output[alphaIndex] = source[alphaIndex];
            }
        }

        return output;
    }

    private static double GetSkinEdgeStrength(
        byte[] pixels,
        int width,
        int stride,
        int x,
        int previousRow,
        int row,
        int nextRow)
    {
        int leftX = Math.Max(0, x - 1) * 4;
        int rightX = Math.Min(width - 1, x + 1) * 4;
        int centerX = x * 4;
        double left = GetSkinLuma(pixels[row + leftX], pixels[row + leftX + 1], pixels[row + leftX + 2]);
        double right = GetSkinLuma(pixels[row + rightX], pixels[row + rightX + 1], pixels[row + rightX + 2]);
        double top = GetSkinLuma(pixels[previousRow + centerX], pixels[previousRow + centerX + 1], pixels[previousRow + centerX + 2]);
        double bottom = GetSkinLuma(pixels[nextRow + centerX], pixels[nextRow + centerX + 1], pixels[nextRow + centerX + 2]);
        return (Math.Abs(right - left) + Math.Abs(bottom - top)) * 0.5;
    }

    private static double GetSkinLuma(double blue, double green, double red)
    {
        return (blue * 0.114) + (green * 0.587) + (red * 0.299);
    }

    private static byte BlendSkinChannel(double original, double adjusted, double amount)
    {
        double blended = original + ((Math.Clamp(adjusted, 0.0, 255.0) - original) * Math.Clamp(amount, 0.0, 1.0));
        return (byte)Math.Clamp((int)Math.Round(blended), 0, 255);
    }
}
