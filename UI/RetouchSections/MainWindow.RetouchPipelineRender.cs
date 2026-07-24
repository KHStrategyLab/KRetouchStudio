using KRetouchStudio.Pipeline;
using KRetouchStudio.Tabs;
using System.IO;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private async Task<BitmapSource?> RenderPhotoEditPipelineAsync(
        PhotoItem photo,
        RetouchStageId changedStage,
        RetouchRenderQuality quality,
        string statusPrefix)
    {
        PhotoEditState state = GetOrCreatePhotoEditState(photo);
        PhotoEditStateSnapshot snapshot = state.CaptureSnapshot();
        FaceShapeHeadPosePreviewMode headPosePreviewMode = FaceShapeHeadPosePreviewMode.None;
        if (quality == RetouchRenderQuality.Preview &&
            changedStage == RetouchStageId.FaceShape &&
            FaceShapeRetouchTab is not null)
        {
            if (FaceShapeRetouchTab.IsFaceTiltFaceShapeModeSelected)
            {
                headPosePreviewMode = FaceShapeHeadPosePreviewMode.FaceTilt;
            }
            else if (FaceShapeRetouchTab.IsFaceTurnFaceShapeModeSelected)
            {
                headPosePreviewMode = FaceShapeHeadPosePreviewMode.FaceTurn;
            }
            else if (FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected)
            {
                headPosePreviewMode = FaceShapeHeadPosePreviewMode.HeadTilt;
            }
        }

        FaceShapeAdjustmentSnapshot? faceShapeState = snapshot.FaceShapeState;
        if (faceShapeState is null &&
            headPosePreviewMode != FaceShapeHeadPosePreviewMode.None)
        {
            faceShapeState = FaceShapeAdjustmentSnapshot.Neutral;
        }

        PhotoRetouchPipelineSession session = GetOrCreateRetouchPipelineSession(photo);
        RetouchStageId firstDirtyStage = RetouchStageCatalog.GetFirstDirtyStage(changedStage);
        RetouchRenderTicket ticket = session.BeginRender(quality, firstDirtyStage);

        try
        {
            RetouchStageCacheEntry workingBase = EnsureWorkingBasePipelineCache(
                photo,
                snapshot,
                session,
                quality);
            RetouchStageCacheEntry upstream = workingBase;
            if (session.Cache.TryGetNearestValidUpstream(
                    firstDirtyStage,
                    quality,
                    out RetouchStageCacheEntry? cachedUpstream) &&
                cachedUpstream is not null)
            {
                upstream = cachedUpstream;
            }

            BitmapSource current = upstream.Bitmap;
            long inputRevision = upstream.OutputRevision;
            RetouchStageId firstRenderStage = (RetouchStageId)((int)upstream.StageId + 1);

            List<MediaPipeLandmarkPoint>? landmarks = null;
            IReadOnlyDictionary<int, Point>? pointMap = null;
            FaceShapePointArray pointArray = default;
            string? personAlphaPath = null;
            byte[]? personAlphaPixels = null;

            async Task EnsureLandmarksAsync()
            {
                if (pointMap is not null)
                {
                    return;
                }

                landmarks = await GetOrCreateFaceShapeLandmarksAsync(photo, statusPrefix);
                if (!session.IsCurrent(ticket) || landmarks.Count == 0)
                {
                    throw new OperationCanceledException(ticket.CancellationToken);
                }

                pointMap = GetOrCreateFaceShapePointMap(
                        photo,
                        landmarks,
                        current.PixelWidth,
                        current.PixelHeight)
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                pointArray = GetOrCreateFaceShapePointArray(
                    photo,
                    landmarks,
                    current.PixelWidth,
                    current.PixelHeight);
                session.PublishAnalysisRevision();
            }

            async Task EnsurePersonAlphaAsync()
            {
                if (personAlphaPixels is not null)
                {
                    return;
                }

                personAlphaPath ??= await GetOrCreatePersonAlphaPathAsync(photo);
                if (!session.IsCurrent(ticket) || string.IsNullOrWhiteSpace(personAlphaPath))
                {
                    throw new OperationCanceledException(ticket.CancellationToken);
                }

                personAlphaPixels = GetOrCreateRefinedPersonAlphaMask(
                    personAlphaPath,
                    current.PixelWidth,
                    current.PixelHeight);
            }

            foreach (RetouchStageId stage in RetouchStageCatalog.EnumerateRasterRange(
                         firstRenderStage,
                         RetouchStageId.Background))
            {
                ticket.CancellationToken.ThrowIfCancellationRequested();
                if (!session.IsCurrent(ticket))
                {
                    return null;
                }

                BitmapSource output = current;
                switch (stage)
                {
                    case RetouchStageId.Tone when
                        snapshot.ToneState is { HasEffectiveAdjustment: true }:
                        output = await RenderTonePipelineStageAsync(
                            current,
                            snapshot.ToneState,
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.FaceShape when faceShapeState is not null:
                        await EnsureLandmarksAsync();
                        bool requiresFaceShapePersonAlpha =
                            faceShapeState.UpperAlign > 0.001 ||
                            (quality == RetouchRenderQuality.FullResolution &&
                             (Math.Abs(faceShapeState.FaceTurn - 50.0) > 0.001 ||
                              Math.Abs(faceShapeState.HeadTilt - 50.0) > 0.001));
                        if (requiresFaceShapePersonAlpha)
                        {
                            await EnsurePersonAlphaAsync();
                        }

                        BitmapSource safeFaceShapeSource = CloneBitmapSource(current);
                        IReadOnlyDictionary<int, Point> safeFaceShapePoints = pointMap!;
                        FaceShapePointArray safePointArray = pointArray;
                        byte[]? safeAlpha = personAlphaPixels;
                        output = await Task.Run(() => RenderFaceShapePipelineStage(
                            safeFaceShapeSource,
                            safeFaceShapePoints,
                            safePointArray,
                            safeAlpha,
                            faceShapeState,
                            headPosePreviewMode,
                            quality == RetouchRenderQuality.FullResolution,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.FaceDetail when snapshot.FaceDetailState is not null:
                        await EnsureLandmarksAsync();
                        BitmapSource safeFaceDetailSource = CloneBitmapSource(current);
                        output = await Task.Run(() => BuildFaceDetailRetouchPreview(
                            safeFaceDetailSource,
                            pointMap!,
                            snapshot.FaceDetailState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Skin when snapshot.SkinState is not null:
                        await EnsureLandmarksAsync();
                        output = await Task.Run(() => RenderSkinAdjustments(
                            CloneBitmapSource(current),
                            pointMap!,
                            snapshot.SkinState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Blemish when snapshot.BlemishState is not null:
                        await EnsureLandmarksAsync();
                        output = await Task.Run(() => RenderBlemishAdjustments(
                            CloneBitmapSource(current),
                            pointMap!,
                            snapshot.BlemishState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Wrinkle when snapshot.WrinkleState is not null:
                        await EnsureLandmarksAsync();
                        output = await Task.Run(() => RenderWrinkleAdjustments(
                            CloneBitmapSource(current),
                            pointMap!,
                            snapshot.WrinkleState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Makeup when snapshot.MakeupState is not null:
                        await EnsureLandmarksAsync();
                        output = await Task.Run(() => RenderMakeupAdjustments(
                            CloneBitmapSource(current),
                            pointMap!,
                            snapshot.MakeupState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Hair when snapshot.HairState is not null:
                        await EnsureLandmarksAsync();
                        output = await Task.Run(() => RenderHairAdjustments(
                            CloneBitmapSource(current),
                            pointMap!,
                            snapshot.HairState,
                            () => !session.IsCurrent(ticket)),
                            ticket.CancellationToken);
                        break;

                    case RetouchStageId.Background when
                        snapshot.BackgroundState is { HasEffectiveAdjustment: true }:
                        string? backgroundAlphaPath = await GetOrCreatePipelinePersonAlphaPathAsync(
                            photo,
                            current,
                            snapshot.BaseRevision,
                            session,
                            ticket);
                        if (string.IsNullOrWhiteSpace(backgroundAlphaPath))
                        {
                            throw new InvalidOperationException("The transformed subject mask is unavailable.");
                        }

                        output = await RenderBackgroundPipelineStageAsync(
                            current,
                            backgroundAlphaPath,
                            snapshot.BackgroundState,
                            ticket);
                        break;
                }

                if (!session.IsCurrent(ticket))
                {
                    return null;
                }

                inputRevision = session.Cache.Publish(
                    stage,
                    quality,
                    inputRevision,
                    session.GetStageStateRevision(stage),
                    session.GeometryRevision,
                    session.AnalysisRevision,
                    output);
                current = output;
            }

            if (quality == RetouchRenderQuality.FullResolution)
            {
                session.Cache.TrimQuality(
                    RetouchRenderQuality.FullResolution,
                    maximumEntries: 6,
                    CreateFullResolutionPipelineCacheCheckpoints(snapshot));
            }

            return session.IsCurrent(ticket) ? current : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(SelectedPhoto, photo))
            {
                MediaPipeStatusText = $"{statusPrefix}: {ex.Message}";
            }

            return null;
        }
    }

    private RetouchStageCacheEntry EnsureWorkingBasePipelineCache(
        PhotoItem photo,
        PhotoEditStateSnapshot snapshot,
        PhotoRetouchPipelineSession session,
        RetouchRenderQuality quality)
    {
        if (session.Cache.TryGet(
                RetouchStageId.WorkingBase,
                quality,
                out RetouchStageCacheEntry? cachedBase) &&
            cachedBase is not null &&
            cachedBase.StateRevision == snapshot.BaseRevision)
        {
            return cachedBase;
        }

        BitmapSource source = quality == RetouchRenderQuality.Preview
            ? GetOrCreateFaceShapeHeadPoseDragProxy(photo, photo.BaseImage)
            : CloneBitmapSource(photo.BaseImage);
        session.Cache.Publish(
            RetouchStageId.WorkingBase,
            quality,
            0,
            snapshot.BaseRevision,
            session.GeometryRevision,
            session.AnalysisRevision,
            source);
        session.Cache.TryGet(
            RetouchStageId.WorkingBase,
            quality,
            out RetouchStageCacheEntry? publishedBase);
        return publishedBase!;
    }

    private static Task<BitmapSource> RenderTonePipelineStageAsync(
        BitmapSource source,
        ToneAdjustmentSnapshot state,
        CancellationToken cancellationToken)
    {
        ToneCurveEditorState curveState = new();
        curveState.RestoreSnapshot(state.Curve);
        byte[] allLut = curveState.BuildCurveLookupTable(CurveChannel.All);
        byte[] redLut = curveState.BuildCurveLookupTable(CurveChannel.Red);
        byte[] greenLut = curveState.BuildCurveLookupTable(CurveChannel.Green);
        byte[] blueLut = curveState.BuildCurveLookupTable(CurveChannel.Blue);
        BitmapSource safeSource = CloneBitmapSource(source);
        return Task.Run(() => ApplyToneCurveToBitmap(
            safeSource,
            allLut,
            redLut,
            greenLut,
            blueLut,
            state.Curve.Strength / 100.0,
            state.Exposure,
            state.Contrast,
            state.Saturation,
            state.WhiteBalance,
            state.Sharpness,
            cancellationToken),
            cancellationToken);
    }

    private async Task<BitmapSource> RenderBackgroundPipelineStageAsync(
        BitmapSource source,
        string alphaPath,
        BackgroundAdjustmentSnapshot state,
        RetouchRenderTicket ticket)
    {
        (byte fillB, byte fillG, byte fillR, string? imagePath) = GetBackgroundPipelineFill(state);
        BitmapSource safeSource = CreateBackgroundRenderThreadSource(source);
        await _backgroundRenderGate.WaitAsync(ticket.CancellationToken);
        try
        {
            return await Task.Run(() => BuildBackgroundReplacementPreview(
                safeSource,
                alphaPath,
                fillB,
                fillG,
                fillR,
                imagePath,
                state.BackgroundOpacity,
                state.BoundaryProbeStrength,
                state.BoundaryCleanStrength,
                state.EdgeBlurStrength,
                state.AlphaShrinkStrength,
                state.SoftAlphaStrength,
                state.AlphaGammaStrength,
                PersonAlphaEngineBiRefNet),
                ticket.CancellationToken);
        }
        finally
        {
            _backgroundRenderGate.Release();
        }
    }

    private static (byte B, byte G, byte R, string? ImagePath) GetBackgroundPipelineFill(
        BackgroundAdjustmentSnapshot state)
    {
        return state.Mode switch
        {
            BackgroundReplacementMode.Gray => (150, 150, 150, null),
            BackgroundReplacementMode.SolidColor =>
                ((byte)state.SolidColorArgb,
                 (byte)(state.SolidColorArgb >> 8),
                 (byte)(state.SolidColorArgb >> 16),
                 null),
            BackgroundReplacementMode.Image when
                !string.IsNullOrWhiteSpace(state.SelectedImagePath) &&
                File.Exists(state.SelectedImagePath) =>
                (255, 255, 255, state.SelectedImagePath),
            BackgroundReplacementMode.Image =>
                throw new FileNotFoundException("The selected background image is unavailable.", state.SelectedImagePath),
            _ => (255, 255, 255, null)
        };
    }

    private static IReadOnlySet<RetouchStageId> CreateFullResolutionPipelineCacheCheckpoints(
        PhotoEditStateSnapshot state)
    {
        HashSet<RetouchStageId> checkpoints =
        [
            RetouchStageId.WorkingBase,
            RetouchStageId.FaceShape,
            RetouchStageId.FaceDetail,
            RetouchStageId.Background
        ];
        RetouchStageId[] activeStages =
        [
            state.HairState is not null ? RetouchStageId.Hair : RetouchStageId.WorkingBase,
            state.MakeupState is not null ? RetouchStageId.Makeup : RetouchStageId.WorkingBase,
            state.WrinkleState is not null ? RetouchStageId.Wrinkle : RetouchStageId.WorkingBase,
            state.BlemishState is not null ? RetouchStageId.Blemish : RetouchStageId.WorkingBase,
            state.SkinState is not null ? RetouchStageId.Skin : RetouchStageId.WorkingBase,
            state.ToneState is not null ? RetouchStageId.Tone : RetouchStageId.WorkingBase
        ];
        foreach (RetouchStageId stage in activeStages
                     .Where(stage => stage != RetouchStageId.WorkingBase)
                     .Take(2))
        {
            checkpoints.Add(stage);
        }

        return checkpoints;
    }
}
