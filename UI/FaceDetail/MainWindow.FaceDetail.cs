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
    private const string FaceDetailHistoryTitle = "Face Detail";
    private const string FaceDetailHistoryDetail = "Detail";
    private const string FaceDetailResetHistoryDetail = "Reset";
    private const double FaceDetailNeutralSliderValue = 50.0;
    private PhotoItem? _faceDetailSessionPhoto;
    private string? _faceDetailSessionPath;
    private BitmapSource? _faceDetailSessionBaseImage;
    private bool _isFaceDetailCommitRunning;
    private bool _hasPendingFaceDetailCommitRequest;
    private FaceDetailAdjustmentEventArgs? _pendingFaceDetailCommitArgs;
    private int _faceDetailRenderVersion;

    private async void FaceDetailRetouchTab_FaceDetailAdjustmentPreviewChanged(object? sender, FaceDetailAdjustmentEventArgs e)
    {
        try
        {
            if (SelectedPhoto is not PhotoItem photo)
            {
                return;
            }

            PreparePhotoEditPipelineStageChange(photo, RetouchStageId.FaceDetail);
            await RenderAndPublishPhotoEditPipelineAsync(
                photo,
                RetouchStageId.FaceDetail,
                RetouchRenderQuality.Preview,
                "Face Detail");
        }
        catch (Exception ex)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"Face Detail preview failed: {ex.Message}";
        }
    }

    private async void FaceDetailRetouchTab_FaceDetailAdjustmentCommitted(object? sender, FaceDetailAdjustmentEventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.FaceDetail);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.FaceDetail,
            RetouchRenderQuality.FullResolution,
            "Face Detail");
        if (applied)
        {
            PushOrReplaceFaceDetailHistory(photo, e.OperationId, e.Value);
            UpdateFaceDetailHistoryResetState();
        }
    }

    private async void FaceDetailRetouchTab_FaceDetailResetRequested(object? sender, EventArgs e)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        FaceDetailRetouchTab.RestoreSnapshot(null);
        PreparePhotoEditPipelineStageChange(photo, RetouchStageId.FaceDetail);
        bool applied = await RenderAndPublishPhotoEditPipelineAsync(
            photo,
            RetouchStageId.FaceDetail,
            RetouchRenderQuality.FullResolution,
            "Face Detail Reset");
        if (applied)
        {
            PushEditorHistorySnapshot(FaceDetailHistoryTitle, FaceDetailResetHistoryDetail);
            UpdateFaceDetailHistoryResetState();
        }
    }

    private async Task ApplyFaceDetailDragPreviewAsync(FaceDetailAdjustmentEventArgs args)
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        FaceDetailAdjustmentSnapshot snapshot = args.Snapshot;
        int renderVersion = Interlocked.Increment(ref _faceDetailRenderVersion);
        BitmapSource baseSource = GetFaceDetailRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);

        if (!HasEffectiveFaceDetailAdjustment(snapshot))
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = "Face Detail: preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Detail");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceDetailRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Detail: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            proxySource.PixelWidth,
            proxySource.PixelHeight);
        BitmapSource safeProxy = CloneBitmapSource(proxySource);
        MediaPipeStatusText = $"Face Detail: {PhotoItem.PreviewProxyLongSide:0} preview {args.OperationId} {args.Value:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceDetailRetouchPreview(
                safeProxy,
                landmarkPoints,
                snapshot,
                () => renderVersion != _faceDetailRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceDetailRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"Face Detail: preview {args.OperationId} {args.Value:0}";
    }

    private async Task ApplyFaceDetailCommittedAsync(FaceDetailAdjustmentEventArgs args)
    {
        if (_isFaceDetailCommitRunning)
        {
            _pendingFaceDetailCommitArgs = args;
            _hasPendingFaceDetailCommitRequest = true;
            Interlocked.Increment(ref _faceDetailRenderVersion);
            return;
        }

        _isFaceDetailCommitRunning = true;
        try
        {
            FaceDetailAdjustmentEventArgs currentArgs = args;
            do
            {
                _hasPendingFaceDetailCommitRequest = false;
                _pendingFaceDetailCommitArgs = null;
                await ApplyFaceDetailCommittedCoreAsync(currentArgs);
                if (_pendingFaceDetailCommitArgs is not null)
                {
                    currentArgs = _pendingFaceDetailCommitArgs;
                }
            }
            while (_hasPendingFaceDetailCommitRequest);
        }
        finally
        {
            _isFaceDetailCommitRunning = false;
        }
    }

    private async Task ApplyFaceDetailCommittedCoreAsync(FaceDetailAdjustmentEventArgs args)
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        FaceDetailAdjustmentSnapshot snapshot = args.Snapshot;
        int renderVersion = Interlocked.Increment(ref _faceDetailRenderVersion);
        BitmapSource baseSource = GetFaceDetailRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Detail: original-size image only";
            return;
        }

        if (!HasEffectiveFaceDetailAdjustment(snapshot))
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            ClearFaceShapeHeadPoseDragPreview();
            PushOrReplaceFaceDetailHistory(targetPhoto, args.OperationId, args.Value);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Detail: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Detail");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceDetailRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Detail: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        MediaPipeStatusText = $"Face Detail: applying {args.OperationId} {args.Value:0}...";

        BitmapSource result = await Task.Run(() =>
            BuildFaceDetailRetouchPreview(
                safeBase,
                landmarkPoints,
                snapshot,
                () => renderVersion != _faceDetailRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceDetailRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(result);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceFaceDetailHistory(targetPhoto, args.OperationId, args.Value);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Detail: applied {args.OperationId} {args.Value:0}";
    }

    private BitmapSource GetFaceDetailRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceDetailSessionPhoto, photo) &&
            _faceDetailSessionBaseImage is not null &&
            !string.IsNullOrWhiteSpace(_faceDetailSessionPath) &&
            string.Equals(_faceDetailSessionPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            return _faceDetailSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceDetail() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceDetailSessionPhoto = photo;
        _faceDetailSessionPath = photo.Path;
        _faceDetailSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceDetailSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceDetail()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceDetailHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceDetailHistoryDetail, StringComparison.Ordinal);
    }

    private static bool IsFaceDetailResetHistory(EditorHistoryState snapshot)
    {
        return string.Equals(snapshot.Title, FaceDetailHistoryTitle, StringComparison.Ordinal) &&
               string.Equals(snapshot.Detail, FaceDetailResetHistoryDetail, StringComparison.Ordinal);
    }

    private static bool IsFaceDetailEffectHistory(EditorHistoryState snapshot)
    {
        return string.Equals(snapshot.Title, FaceDetailHistoryTitle, StringComparison.Ordinal) &&
               snapshot.Detail.StartsWith(FaceDetailHistoryDetail, StringComparison.Ordinal);
    }

    private bool HasActiveFaceDetailHistory()
    {
        for (int i = _editorUndoHistory.Count - 1; i >= 0; i--)
        {
            EditorHistoryState snapshot = _editorUndoHistory[i];
            if (IsFaceDetailResetHistory(snapshot))
            {
                return false;
            }

            if (IsFaceDetailEffectHistory(snapshot))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanResetFaceDetailHistory()
    {
        return SelectedPhoto is not null &&
               _editorUndoHistory.Count > 1 &&
               HasActiveFaceDetailHistory();
    }

    private void UpdateFaceDetailHistoryResetState()
    {
        FaceDetailRetouchTab.CanResetFaceDetailHistory = CanResetFaceDetailHistory();
    }

    private bool TryGetFaceDetailResetSource(PhotoItem photo, out BitmapSource source)
    {
        source = photo.BaseImage;
        int firstActiveFaceDetailIndex = -1;
        for (int i = 0; i < _editorUndoHistory.Count; i++)
        {
            EditorHistoryState snapshot = _editorUndoHistory[i];
            if (IsFaceDetailResetHistory(snapshot))
            {
                firstActiveFaceDetailIndex = -1;
                continue;
            }

            if (firstActiveFaceDetailIndex < 0 && IsFaceDetailEffectHistory(snapshot))
            {
                firstActiveFaceDetailIndex = i;
            }
        }

        if (firstActiveFaceDetailIndex < 0)
        {
            return false;
        }

        if (firstActiveFaceDetailIndex == 0)
        {
            source = photo.BaseImage;
            return true;
        }

        EditorHistoryState resetBase = _editorUndoHistory[firstActiveFaceDetailIndex - 1];
        source = resetBase.AdjustedImage ?? photo.BaseImage;
        return true;
    }

    private async Task TryResetFaceDetailHistoryAsync()
    {
        if (!CanResetFaceDetailHistory() || SelectedPhoto is not PhotoItem targetPhoto)
        {
            UpdateFaceDetailHistoryResetState();
            return;
        }

        if (!TryGetSafeTabResetSource(
                targetPhoto,
                IsFaceDetailEffectHistory,
                IsFaceDetailResetHistory,
                out BitmapSource resetSource,
                out string blockingHistoryTitle))
        {
            MediaPipeStatusText = string.IsNullOrWhiteSpace(blockingHistoryTitle)
                ? "Face Detail: nothing to reset"
                : $"Face Detail: reset blocked to preserve {blockingHistoryTitle}";
            UpdateFaceDetailHistoryResetState();
            return;
        }

        MediaPipeStatusText = "Face Detail: rebuilding other tabs...";
        BitmapSource? rebuiltImage = await RebuildConnectedRetouchSectionsAsync(
            targetPhoto,
            resetSource,
            "Face Detail Reset");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) || rebuiltImage is null)
        {
            MediaPipeStatusText = "Face Detail: reset cancelled; other tabs could not be rebuilt";
            return;
        }

        targetPhoto.SetAdjustedImage(rebuiltImage);
        FaceDetailRetouchTab.ResetAfterHistoryReset();
        ClearFaceDetailRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        SetConnectedRetouchSessionBase(targetPhoto, resetSource);
        UpdatePreviewLayout();
        PushEditorHistorySnapshot(FaceDetailHistoryTitle, FaceDetailResetHistoryDetail);
        MediaPipeStatusText = "Face Detail: reset";
    }

    private void PushOrReplaceFaceDetailHistory(PhotoItem photo, string operationId, double value)
    {
        string detail = $"{FaceDetailHistoryDetail} {operationId} {Math.Clamp(Math.Round(value), 0, 100):0}";
        if (IsCurrentHistoryFaceDetail())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceDetailHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceDetailHistoryTitle, detail);
    }

    private void ClearFaceDetailRetouchSession()
    {
        _faceDetailSessionPhoto = null;
        _faceDetailSessionPath = null;
        _faceDetailSessionBaseImage = null;
        _pendingFaceDetailCommitArgs = null;
        _hasPendingFaceDetailCommitRequest = false;
        Interlocked.Increment(ref _faceDetailRenderVersion);
    }

    private static bool HasEffectiveFaceDetailAdjustment(FaceDetailAdjustmentSnapshot s)
    {
        return
            HasCenteredEffect(s.EyeSize) ||
            HasCenteredEffect(s.LeftEyeHeight) ||
            HasCenteredEffect(s.RightEyeHeight) ||
            HasCenteredEffect(s.LeftEyeTilt) ||
            HasCenteredEffect(s.RightEyeTilt) ||
            HasCenteredEffect(s.EyeDistance) ||
            s.LeftDarkCircle > 0.001 ||
            s.RightDarkCircle > 0.001 ||
            s.LeftUnderEye > 0.001 ||
            s.RightUnderEye > 0.001 ||
            HasCenteredEffect(s.LeftBrowThickness) ||
            HasCenteredEffect(s.RightBrowThickness) ||
            HasCenteredEffect(s.BrowDistance) ||
            HasCenteredEffect(s.LeftBrowTilt) ||
            HasCenteredEffect(s.RightBrowTilt) ||
            HasCenteredEffect(s.LeftBrowArch) ||
            HasCenteredEffect(s.RightBrowArch) ||
            HasCenteredEffect(s.LeftBrowPosition) ||
            HasCenteredEffect(s.RightBrowPosition) ||
            HasCenteredEffect(s.LeftBrowTail) ||
            HasCenteredEffect(s.RightBrowTail) ||
            HasCenteredEffect(s.NoseSize) ||
            HasCenteredEffect(s.NoseLength) ||
            HasCenteredEffect(s.NoseBridge) ||
            HasCenteredEffect(s.NoseWidth) ||
            HasCenteredEffect(s.NoseTip) ||
            HasCenteredEffect(s.LeftNostril) ||
            HasCenteredEffect(s.RightNostril) ||
            HasCenteredEffect(s.MouthWidth) ||
            HasCenteredEffect(s.LeftMouthCorner) ||
            HasCenteredEffect(s.RightMouthCorner) ||
            HasCenteredEffect(s.UpperLip) ||
            HasCenteredEffect(s.LowerLip) ||
            HasCenteredEffect(s.NeckSlim) ||
            HasCenteredEffect(s.NeckLength) ||
            s.NeckWrinkle > 0.001 ||
            s.DoubleChin > 0.001 ||
            HasCenteredEffect(s.LeftSideNeck) ||
            HasCenteredEffect(s.RightSideNeck) ||
            HasCenteredEffect(s.LeftTrapezius) ||
            HasCenteredEffect(s.RightTrapezius);
    }

    private static BitmapSource BuildFaceDetailRetouchPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot snapshot,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        BitmapSource current = bgraSource;
        foreach (FaceDetailWarpPlan warpPlan in BuildFaceDetailWarpPlans(
                     landmarks,
                     bgraSource.PixelWidth,
                     bgraSource.PixelHeight,
                     snapshot))
        {
            if (shouldCancel?.Invoke() == true)
            {
                return current;
            }

            BitmapSource currentBgra = EnsureBitmapFormat(current, PixelFormats.Bgra32);
            current = BuildFaceShapeControlWarpPreview(
                currentBgra,
                warpPlan.Controls,
                warpPlan.Sigma,
                CreateFaceDetailWeightProfile(warpPlan.Bounds, warpPlan.RadiusScale, warpPlan.SolidRadius),
                shouldCancel);
        }

        if (shouldCancel?.Invoke() == true)
        {
            return current;
        }

        return ApplyFaceDetailToneAdjustments(current, landmarks, snapshot, shouldCancel);
    }

    private static List<FaceDetailWarpPlan> BuildFaceDetailWarpPlans(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        FaceDetailAdjustmentSnapshot snapshot)
    {
        List<FaceDetailWarpPlan> plans = [];

        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.24,
            sigmaRatio: 0.12,
            radiusScale: 0.46,
            solidRadius: 0.50,
            buildControls: (controls, affectedPoints) => AddSingleEyeControls(
                landmarks,
                isLeft: true,
                snapshot.EyeSize,
                snapshot.LeftEyeHeight,
                snapshot.LeftEyeTilt,
                snapshot.EyeDistance,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.24,
            sigmaRatio: 0.12,
            radiusScale: 0.46,
            solidRadius: 0.50,
            buildControls: (controls, affectedPoints) => AddSingleEyeControls(
                landmarks,
                isLeft: false,
                snapshot.EyeSize,
                snapshot.RightEyeHeight,
                snapshot.RightEyeTilt,
                snapshot.EyeDistance,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.18,
            sigmaRatio: 0.08,
            radiusScale: 0.36,
            solidRadius: 0.45,
            buildControls: (controls, affectedPoints) => AddSingleBrowThicknessControls(
                landmarks,
                isLeft: true,
                snapshot.LeftBrowThickness,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.18,
            sigmaRatio: 0.08,
            radiusScale: 0.36,
            solidRadius: 0.45,
            buildControls: (controls, affectedPoints) => AddSingleBrowThicknessControls(
                landmarks,
                isLeft: false,
                snapshot.RightBrowThickness,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.26,
            sigmaRatio: 0.12,
            radiusScale: 0.48,
            solidRadius: 0.48,
            buildControls: (controls, affectedPoints) => AddSingleBrowControls(
                landmarks,
                isLeft: true,
                snapshot.LeftBrowTilt,
                snapshot.LeftBrowArch,
                snapshot.LeftBrowPosition,
                snapshot.LeftBrowTail,
                snapshot.BrowDistance,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.26,
            sigmaRatio: 0.12,
            radiusScale: 0.48,
            solidRadius: 0.48,
            buildControls: (controls, affectedPoints) => AddSingleBrowControls(
                landmarks,
                isLeft: false,
                snapshot.RightBrowTilt,
                snapshot.RightBrowArch,
                snapshot.RightBrowPosition,
                snapshot.RightBrowTail,
                snapshot.BrowDistance,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.36,
            sigmaRatio: 0.16,
            radiusScale: 0.58,
            solidRadius: 0.57,
            buildControls: (controls, affectedPoints) => AddNoseDetailControls(landmarks, snapshot, controls, affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.30,
            sigmaRatio: 0.14,
            radiusScale: 0.50,
            solidRadius: 0.52,
            buildControls: (controls, affectedPoints) => AddMouthDetailControls(landmarks, snapshot, controls, affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.16,
            sigmaRatio: 0.090,
            radiusScale: 0.54,
            solidRadius: 0.42,
            buildControls: (controls, affectedPoints) => AddSingleMouthCornerControls(
                landmarks,
                isLeft: true,
                snapshot,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.16,
            sigmaRatio: 0.090,
            radiusScale: 0.54,
            solidRadius: 0.42,
            buildControls: (controls, affectedPoints) => AddSingleMouthCornerControls(
                landmarks,
                isLeft: false,
                snapshot,
                controls,
                affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.18,
            sigmaRatio: 0.075,
            radiusScale: 0.34,
            solidRadius: 0.44,
            buildControls: (controls, affectedPoints) => AddUpperLipThicknessControls(landmarks, snapshot, controls, affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.18,
            sigmaRatio: 0.085,
            radiusScale: 0.38,
            solidRadius: 0.46,
            buildControls: (controls, affectedPoints) => AddLowerLipThicknessControls(landmarks, snapshot, controls, affectedPoints));
        AddFaceDetailWarpPlan(
            plans,
            width,
            height,
            marginRatio: 0.50,
            sigmaRatio: 0.20,
            radiusScale: 0.68,
            solidRadius: 0.62,
            buildControls: (controls, affectedPoints) => AddLowerFaceDetailControls(landmarks, snapshot, controls, affectedPoints));

        return plans;
    }

    private static void AddFaceDetailWarpPlan(
        List<FaceDetailWarpPlan> plans,
        int width,
        int height,
        double marginRatio,
        double sigmaRatio,
        double radiusScale,
        double solidRadius,
        Action<List<FaceShapeControlPoint>, List<Point>> buildControls)
    {
        List<FaceShapeControlPoint> controls = [];
        List<Point> affectedPoints = [];
        buildControls(controls, affectedPoints);
        if (controls.Count == 0 || affectedPoints.Count == 0)
        {
            return;
        }

        Rect bounds = BuildFaceDetailBounds(affectedPoints, width, height, marginRatio);
        AddFaceDetailAnchorFrame(controls, bounds, width, height);
        double sigma = Math.Max(6.0, Math.Max(bounds.Width, bounds.Height) * sigmaRatio);
        plans.Add(new FaceDetailWarpPlan(bounds, sigma, controls, radiusScale, solidRadius));
    }

    private static void AddSingleEyeControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double size,
        double height,
        double tilt,
        double distance,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int outerIndex = isLeft ? 33 : 263;
        int innerIndex = isLeft ? 133 : 362;
        int topIndex = isLeft ? 159 : 386;
        int bottomIndex = isLeft ? 145 : 374;
        if (!TryGetLandmark(landmarks, outerIndex, out Point outer) ||
            !TryGetLandmark(landmarks, innerIndex, out Point inner) ||
            !TryGetLandmark(landmarks, topIndex, out Point top) ||
            !TryGetLandmark(landmarks, bottomIndex, out Point bottom))
        {
            return;
        }

        Point center = AveragePoints(outer, inner, top, bottom);
        double eyeBundleWidth = Math.Max(1.0, Math.Abs(inner.X - outer.X));
        double eyeBundleHeight = Math.Max(1.0, Math.Abs(bottom.Y - top.Y));
        double bundleShiftX = CenteredStrengthInDirection(distance, eyeBundleWidth * 0.26, isLeft ? -1 : 1);
        double sizeX = CenteredStrength(size, eyeBundleWidth * 0.13);
        double sizeY = CenteredStrength(size, eyeBundleHeight * 0.32);
        double tiltY = CenteredStrength(tilt, eyeBundleHeight * 0.165);
        double outerTiltY = -tiltY;
        double innerTiltY = tiltY;
        double heightOpenY = CenteredStrength(height, eyeBundleHeight * 0.46);
        double heightWidthX = CenteredStrength(height, eyeBundleWidth * 0.052);
        double upperOpenY = -heightOpenY * 0.80;
        double lowerOpenY = heightOpenY * 0.20;
        int[] upperLidIndices = isLeft ? [157, 158, 159, 160, 161] : [384, 385, 386, 387, 388];
        int[] lowerLidIndices = isLeft ? [144, 145, 153, 154, 155] : [373, 374, 380, 381, 382];
        int[] outerCornerIndices = isLeft ? [33, 7, 163, 246] : [263, 249, 390, 466];
        int[] innerCornerIndices = isLeft ? [133, 155, 173] : [362, 382, 398];

        int[] contourIndices = isLeft
            ? [33, 7, 163, 144, 145, 153, 154, 155, 133, 173, 157, 158, 159, 160, 161, 246]
            : [362, 382, 381, 380, 374, 373, 390, 249, 263, 466, 388, 387, 386, 385, 384, 398];
        for (int i = 0; i < contourIndices.Length; i++)
        {
            int index = contourIndices[i];
            if (index == outerIndex ||
                index == innerIndex ||
                index == topIndex ||
                index == bottomIndex ||
                Array.IndexOf(upperLidIndices, index) >= 0 ||
                Array.IndexOf(lowerLidIndices, index) >= 0 ||
                Array.IndexOf(outerCornerIndices, index) >= 0 ||
                Array.IndexOf(innerCornerIndices, index) >= 0)
            {
                continue;
            }

            if (TryGetLandmark(landmarks, index, out Point contourPoint))
            {
                AddControl(controls, affectedPoints, contourPoint, bundleShiftX, 0);
            }
        }

        double outerDirection = isLeft ? -1.0 : 1.0;
        double innerDirection = isLeft ? 1.0 : -1.0;
        Point innerCornerSupport = new(inner.X + (innerDirection * eyeBundleWidth * 0.18), inner.Y);
        Point outerCornerSupport = new(outer.X + (outerDirection * eyeBundleWidth * 0.24), outer.Y);
        AddControl(controls, affectedPoints, innerCornerSupport, bundleShiftX * 0.95, innerTiltY * 0.70);
        AddControl(controls, affectedPoints, outerCornerSupport, bundleShiftX * 0.88, outerTiltY * 0.70);

        if (Math.Abs(heightOpenY) > 0.01)
        {
            double irisGuardDx = bundleShiftX * 0.55;
            double irisGuardY = eyeBundleHeight * 0.075;
            AddSupportControl(controls, center, irisGuardDx, 0);
            AddSupportControl(controls, new Point(center.X - (eyeBundleWidth * 0.10), center.Y), irisGuardDx, 0);
            AddSupportControl(controls, new Point(center.X + (eyeBundleWidth * 0.10), center.Y), irisGuardDx, 0);
            AddSupportControl(controls, new Point(center.X, center.Y - irisGuardY), irisGuardDx, 0);
            AddSupportControl(controls, new Point(center.X, center.Y + irisGuardY), irisGuardDx, 0);
        }

        for (int i = 0; i < upperLidIndices.Length; i++)
        {
            if (upperLidIndices[i] == topIndex)
            {
                continue;
            }

            if (TryGetLandmark(landmarks, upperLidIndices[i], out Point upperPoint))
            {
                AddControl(controls, affectedPoints, upperPoint, bundleShiftX, upperOpenY * 0.82);
            }
        }

        for (int i = 0; i < lowerLidIndices.Length; i++)
        {
            if (lowerLidIndices[i] == bottomIndex)
            {
                continue;
            }

            if (TryGetLandmark(landmarks, lowerLidIndices[i], out Point lowerPoint))
            {
                AddControl(controls, affectedPoints, lowerPoint, bundleShiftX, lowerOpenY * 0.70);
            }
        }

        for (int i = 0; i < outerCornerIndices.Length; i++)
        {
            int index = outerCornerIndices[i];
            if (index == outerIndex)
            {
                continue;
            }

            if (TryGetLandmark(landmarks, index, out Point outerCornerPoint))
            {
                AddControl(controls, affectedPoints, outerCornerPoint, bundleShiftX, outerTiltY * 0.82);
            }
        }

        for (int i = 0; i < innerCornerIndices.Length; i++)
        {
            int index = innerCornerIndices[i];
            if (index == innerIndex)
            {
                continue;
            }

            if (TryGetLandmark(landmarks, index, out Point innerCornerPoint))
            {
                AddControl(controls, affectedPoints, innerCornerPoint, bundleShiftX, innerTiltY * 0.82);
            }
        }
        AddControl(controls, affectedPoints, outer, bundleShiftX + (isLeft ? -sizeX - heightWidthX : sizeX + heightWidthX), isLeft ? -tiltY : -tiltY);
        AddControl(controls, affectedPoints, inner, bundleShiftX + (isLeft ? sizeX + heightWidthX : -sizeX - heightWidthX), isLeft ? tiltY : tiltY);
        AddControl(controls, affectedPoints, top, bundleShiftX, -sizeY + upperOpenY);
        AddControl(controls, affectedPoints, bottom, bundleShiftX, sizeY + lowerOpenY);
        AddControl(controls, affectedPoints, center, bundleShiftX * 0.55, 0);
    }

    private static void AddSingleBrowControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double tilt,
        double arch,
        double position,
        double tail,
        double distance,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int[] indices = isLeft ? [70, 63, 105, 66, 107] : [336, 296, 334, 293, 300];
        List<Point> points = GetExistingPoints(landmarks, indices);
        if (points.Count < 3)
        {
            return;
        }

        Point center = AveragePoints(points);
        double browWidth = Math.Max(1.0, points.Max(p => p.X) - points.Min(p => p.X));
        double lift = -CenteredStrength(position, browWidth * 0.10);
        double archLift = -CenteredStrength(arch, browWidth * 0.07);
        double distanceDx = CenteredStrengthInDirection(distance, browWidth * 0.13, isLeft ? -1 : 1);
        double tiltY = CenteredStrength(tilt, browWidth * 0.055);
        double tailY = -CenteredStrength(tail, browWidth * 0.075);
        Point innerBrow = isLeft
            ? points.OrderByDescending(p => p.X).First()
            : points.OrderBy(p => p.X).First();
        double innerDirection = isLeft ? 1.0 : -1.0;
        Point innerBrowSupport = new(innerBrow.X + (innerDirection * browWidth * 0.16), innerBrow.Y);
        AddControl(controls, affectedPoints, innerBrowSupport, distanceDx * 0.95, 0);

        for (int i = 0; i < points.Count; i++)
        {
            Point p = points[i];
            double normalizedX = (p.X - center.X) / Math.Max(1.0, browWidth * 0.5);
            double dy = lift + (archLift * (1.0 - Math.Min(1.0, Math.Abs(normalizedX))));
            dy += normalizedX * (isLeft ? tiltY : -tiltY);
            if ((isLeft && p.X < center.X) || (!isLeft && p.X > center.X))
            {
                dy += tailY;
            }

            AddControl(controls, affectedPoints, p, distanceDx, dy);
        }
    }

    private static void AddSingleBrowThicknessControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double thickness,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int[] upperIndices = isLeft ? [70, 63, 105, 66, 107] : [336, 296, 334, 293, 300];
        int[] lowerIndices = isLeft ? [46, 53, 52, 65, 55] : [285, 295, 282, 283, 276];
        List<(Point Upper, Point Lower)> pairs = [];
        List<Point> allPoints = [];
        for (int i = 0; i < Math.Min(upperIndices.Length, lowerIndices.Length); i++)
        {
            if (!TryGetLandmark(landmarks, upperIndices[i], out Point upper) ||
                !TryGetLandmark(landmarks, lowerIndices[i], out Point lower))
            {
                continue;
            }

            pairs.Add((upper, lower));
            allPoints.Add(upper);
            allPoints.Add(lower);
        }

        if (pairs.Count < 2)
        {
            return;
        }

        double browWidth = Math.Max(1.0, allPoints.Max(p => p.X) - allPoints.Min(p => p.X));
        double browHeight = Math.Max(1.0, allPoints.Max(p => p.Y) - allPoints.Min(p => p.Y));
        double spread = CenteredStrength(thickness, Math.Max(browHeight * 0.90, browWidth * 0.030));
        if (Math.Abs(spread) < 0.01)
        {
            return;
        }

        for (int i = 0; i < pairs.Count; i++)
        {
            (Point upper, Point lower) = pairs[i];
            double t = pairs.Count == 1 ? 0.5 : i / (double)(pairs.Count - 1);
            double weight = 0.72 + (0.28 * Math.Sin(t * Math.PI));
            AddControl(controls, affectedPoints, upper, 0, -spread * weight);
            AddControl(controls, affectedPoints, lower, 0, spread * weight);

            Point centerLine = new((upper.X + lower.X) * 0.5, (upper.Y + lower.Y) * 0.5);
            AddSupportControl(controls, centerLine, 0, 0);
            AddSupportControl(controls, centerLine, 0, 0);
        }
    }

    private static void AddNoseDetailControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 4, out Point tip) ||
            !TryGetLandmark(landmarks, 168, out Point bridge) ||
            !TryGetLandmark(landmarks, 98, out Point leftNostril) ||
            !TryGetLandmark(landmarks, 327, out Point rightNostril))
        {
            return;
        }

        Point center = AveragePoints(tip, bridge, leftNostril, rightNostril);
        double noseWidth = Math.Max(1.0, rightNostril.X - leftNostril.X);
        double noseHeight = Math.Max(1.0, tip.Y - bridge.Y);
        double sizeExpand = CenteredStrength(s.NoseSize, noseWidth * 0.20);
        double widthExpand = CenteredStrength(s.NoseWidth, noseWidth * 0.32);
        double bridgePull = CenteredStrength(s.NoseBridge, noseWidth * 0.20);
        double tipLift = -CenteredStrength(s.NoseTip, noseHeight * 0.18);
        double lengthDrop = CenteredStrength(s.NoseLength, noseHeight * 0.22);
        double nostrilExpandL = CenteredStrength(s.LeftNostril, noseWidth * 0.26);
        double nostrilExpandR = CenteredStrength(s.RightNostril, noseWidth * 0.26);

        AddNoseBoxWidthControls(bridge, tip, leftNostril, rightNostril, noseWidth, noseHeight, sizeExpand, controls, affectedPoints);
        AddNoseBoxLengthControls(landmarks, bridge, tip, leftNostril, rightNostril, noseWidth, noseHeight, lengthDrop, controls, affectedPoints);
        AddControl(controls, affectedPoints, leftNostril, -widthExpand - nostrilExpandL, 0);
        AddControl(controls, affectedPoints, rightNostril, widthExpand + nostrilExpandR, 0);
        AddControl(controls, affectedPoints, tip, 0, tipLift);
        AddControl(controls, affectedPoints, bridge, 0, -bridgePull * 0.32);
        AddControl(controls, affectedPoints, new Point(center.X - noseWidth * 0.22, center.Y), bridgePull * 1.05, 0);
        AddControl(controls, affectedPoints, new Point(center.X + noseWidth * 0.22, center.Y), -bridgePull * 1.05, 0);
        AddControl(controls, affectedPoints, new Point(center.X - noseWidth * 0.34, center.Y + noseHeight * 0.18), bridgePull + (widthExpand * 0.52), 0);
        AddControl(controls, affectedPoints, new Point(center.X + noseWidth * 0.34, center.Y + noseHeight * 0.18), -bridgePull - (widthExpand * 0.52), 0);
    }

    private static void AddNoseBoxWidthControls(
        Point bridge,
        Point tip,
        Point leftNostril,
        Point rightNostril,
        double noseWidth,
        double noseHeight,
        double sizeExpand,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (Math.Abs(sizeExpand) < 0.01)
        {
            return;
        }

        double axisTopX = bridge.X;
        double axisBottomX = tip.X;
        double halfBoxWidth = Math.Max(1.0, noseWidth * 0.50);
        (double T, double HalfWeight, double MoveWeight)[] rows =
        [
            (0.03, 0.24, 0.18),
            (0.24, 0.44, 0.42),
            (0.50, 0.70, 0.72),
            (0.78, 1.00, 1.00),
            (0.96, 0.94, 0.92)
        ];

        foreach ((double t, double halfWeight, double moveWeight) in rows)
        {
            double centerX = Lerp(axisTopX, axisBottomX, t);
            double y = bridge.Y + (noseHeight * t);
            double half = halfBoxWidth * halfWeight;
            AddControl(controls, affectedPoints, new Point(centerX - half, y), -sizeExpand * moveWeight, 0);
            AddControl(controls, affectedPoints, new Point(centerX + half, y), sizeExpand * moveWeight, 0);
            AddSupportControl(controls, new Point(centerX, y), 0, 0);
        }

        AddControl(controls, affectedPoints, leftNostril, -sizeExpand, 0);
        AddControl(controls, affectedPoints, rightNostril, sizeExpand, 0);
        AddNoseEyeAnchorControls(bridge, noseWidth, noseHeight, controls);
    }

    private static void AddNoseBoxLengthControls(
        IReadOnlyDictionary<int, Point> landmarks,
        Point bridge,
        Point tip,
        Point leftNostril,
        Point rightNostril,
        double noseWidth,
        double noseHeight,
        double lengthDrop,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (Math.Abs(lengthDrop) < 0.01)
        {
            return;
        }

        AddSupportControl(controls, bridge, 0, 0);
        AddSupportControl(controls, new Point(bridge.X - (noseWidth * 0.28), bridge.Y + (noseHeight * 0.06)), 0, 0);
        AddSupportControl(controls, new Point(bridge.X + (noseWidth * 0.28), bridge.Y + (noseHeight * 0.06)), 0, 0);
        AddNoseEyeAnchorControls(bridge, noseWidth, noseHeight, controls);

        (double T, double WidthWeight, double MoveWeight)[] rows =
        [
            (0.22, 0.36, 0.10),
            (0.46, 0.58, 0.34),
            (0.72, 0.92, 0.70),
            (0.96, 1.00, 1.00)
        ];

        foreach ((double t, double widthWeight, double moveWeight) in rows)
        {
            double centerX = Lerp(bridge.X, tip.X, t);
            double y = bridge.Y + (noseHeight * t);
            double half = noseWidth * 0.50 * widthWeight;
            double dy = lengthDrop * moveWeight;
            AddControl(controls, affectedPoints, new Point(centerX, y), 0, dy);
            AddControl(controls, affectedPoints, new Point(centerX - half, y), 0, dy * 0.82);
            AddControl(controls, affectedPoints, new Point(centerX + half, y), 0, dy * 0.82);
        }

        AddControl(controls, affectedPoints, tip, 0, lengthDrop);
        AddControl(controls, affectedPoints, leftNostril, 0, lengthDrop * 0.72);
        AddControl(controls, affectedPoints, rightNostril, 0, lengthDrop * 0.72);
    }

    private static void AddNoseEyeAnchorControls(
        Point bridge,
        double noseWidth,
        double noseHeight,
        List<FaceShapeControlPoint> controls)
    {
        double topY = bridge.Y - (noseHeight * 0.04);
        double sideY = bridge.Y + (noseHeight * 0.12);
        double innerEyeX = noseWidth * 0.78;
        double outerGuardX = noseWidth * 1.10;
        AddSupportControl(controls, new Point(bridge.X - innerEyeX, topY), 0, 0);
        AddSupportControl(controls, new Point(bridge.X + innerEyeX, topY), 0, 0);
        AddSupportControl(controls, new Point(bridge.X - outerGuardX, sideY), 0, 0);
        AddSupportControl(controls, new Point(bridge.X + outerGuardX, sideY), 0, 0);
    }

    private static void AddMouthDetailControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 61, out Point leftCorner) ||
            !TryGetLandmark(landmarks, 291, out Point rightCorner) ||
            !TryGetLandmark(landmarks, 13, out Point upper) ||
            !TryGetLandmark(landmarks, 14, out Point lower))
        {
            return;
        }

        double mouthWidth = Math.Max(1.0, rightCorner.X - leftCorner.X);
        double mouthHeight = Math.Max(1.0, lower.Y - upper.Y);
        double widthX = CenteredStrength(s.MouthWidth, mouthWidth * 0.16);
        Dictionary<int, (double Dx, double Dy)> mouthDeltas = [];

        AddMouthWidthDeltas(mouthDeltas, -widthX, isLeft: true);
        AddMouthWidthDeltas(mouthDeltas, widthX, isLeft: false);

        AddMouthDeltaControls(landmarks, mouthDeltas, controls, affectedPoints);

        Point center = AveragePoints(leftCorner, rightCorner, upper, lower);
        AddSupportControl(controls, new Point(center.X, center.Y), 0, 0);
        AddSupportControl(controls, new Point(center.X, center.Y - mouthHeight * 1.55), 0, 0);
        AddSupportControl(controls, new Point(center.X, center.Y + mouthHeight * 1.55), 0, 0);
    }

    private static void AddSingleMouthCornerControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 61, out Point leftCorner) ||
            !TryGetLandmark(landmarks, 291, out Point rightCorner) ||
            !TryGetLandmark(landmarks, 13, out Point upper) ||
            !TryGetLandmark(landmarks, 14, out Point lower))
        {
            return;
        }

        double mouthWidth = Math.Max(1.0, rightCorner.X - leftCorner.X);
        double mouthHeight = Math.Max(1.0, lower.Y - upper.Y);
        double cornerPullMax = Math.Max(mouthHeight * 0.95, mouthWidth * 0.105);
        double cornerPull = CenteredStrength(isLeft ? s.LeftMouthCorner : s.RightMouthCorner, cornerPullMax) * 1.30;
        if (Math.Abs(cornerPull) < 0.01)
        {
            return;
        }

        double cornerDx = (isLeft ? -1.0 : 1.0) * cornerPull * 0.58;
        double cornerDy = -cornerPull * 0.78;
        Dictionary<int, (double Dx, double Dy)> cornerDeltas = [];

        AddMouthCornerVectorDeltas(cornerDeltas, cornerDx, cornerDy, isLeft);
        AddMouthDeltaControls(landmarks, cornerDeltas, controls, affectedPoints);
        AddMouthCornerDestinationBoosts(landmarks, isLeft, cornerDx, cornerDy, controls, affectedPoints);
        AddMouthCornerBrushControls(landmarks, isLeft, cornerDx, cornerDy, controls, affectedPoints);
        AddMouthCornerBodyAnchors(landmarks, isLeft, controls, affectedPoints);
    }

    private static void AddUpperLipThicknessControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 61, out Point leftCorner) ||
            !TryGetLandmark(landmarks, 291, out Point rightCorner) ||
            !TryGetLandmark(landmarks, 13, out Point upper) ||
            !TryGetLandmark(landmarks, 14, out Point lower))
        {
            return;
        }

        double mouthWidth = Math.Max(1.0, rightCorner.X - leftCorner.X);
        double mouthHeight = Math.Max(1.0, lower.Y - upper.Y);
        double lipVerticalMax = Math.Max(mouthHeight * 0.72, mouthWidth * 0.055) * 7.5;
        double upperLift = -CenteredStrength(s.UpperLip, lipVerticalMax);
        if (Math.Abs(upperLift) < 0.01)
        {
            return;
        }

        int[] moveIndices = [40, 39, 37, 0, 267, 269, 270];
        int[] lowerLineIndices = [78, 191, 80, 81, 82, 13, 312, 311, 310, 415, 308];
        int[] endAnchorIndices = [185, 409];
        int[] strongCenterIndices = [82, 13, 312, 311];
        AddUpperLipArcControls(landmarks, moveIndices, controls, affectedPoints, maxDy: upperLift);
        AddHardLipAnchorControls(landmarks, lowerLineIndices, controls, affectedPoints, repeatCount: 14);
        AddHardLipAnchorControls(landmarks, endAnchorIndices, controls, affectedPoints, repeatCount: 10);
        AddHardLipAnchorControls(landmarks, strongCenterIndices, controls, affectedPoints, repeatCount: 12);
        AddLipLineSegmentAnchors(landmarks, lowerLineIndices, controls, affectedPoints, repeatCount: 8);
    }

    private static void AddLowerLipThicknessControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 61, out Point leftCorner) ||
            !TryGetLandmark(landmarks, 291, out Point rightCorner) ||
            !TryGetLandmark(landmarks, 13, out Point upper) ||
            !TryGetLandmark(landmarks, 14, out Point lower))
        {
            return;
        }

        double mouthWidth = Math.Max(1.0, rightCorner.X - leftCorner.X);
        double mouthHeight = Math.Max(1.0, lower.Y - upper.Y);
        double lipVerticalMax = Math.Max(mouthHeight * 0.72, mouthWidth * 0.055) * 7.5;
        double lowerDrop = CenteredStrength(s.LowerLip, lipVerticalMax);
        if (Math.Abs(lowerDrop) < 0.01)
        {
            return;
        }

        int[] moveIndices = [146, 91, 181, 84, 17, 314, 405, 321, 375];
        int[] upperLineIndices = [78, 95, 88, 178, 87, 14, 317, 402, 318, 324, 308];
        int[] strongCenterIndices = [87, 14, 317, 402];
        AddLipArcControls(landmarks, moveIndices, controls, affectedPoints, centerX: lower.X, halfWidth: mouthWidth * 0.50, maxDy: lowerDrop);
        AddHardLipAnchorControls(landmarks, upperLineIndices, controls, affectedPoints, repeatCount: 16);
        AddHardLipAnchorControls(landmarks, strongCenterIndices, controls, affectedPoints, repeatCount: 14);
        AddLipLineSegmentAnchors(landmarks, upperLineIndices, controls, affectedPoints, repeatCount: 10);
    }

    private static void AddLipArcControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        double centerX,
        double halfWidth,
        double maxDy)
    {
        if (Math.Abs(maxDy) < 0.01)
        {
            return;
        }

        const double endpointWeight = 0.32;
        const double peakWeight = 0.82;
        List<(Point Point, double Dy)> arcControls = [];
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (!TryGetLandmark(landmarks, index, out Point point))
            {
                continue;
            }

            double t = indices.Count == 1 ? 0.5 : i / (double)(indices.Count - 1);
            double arch = Math.Clamp(1.0 - Math.Pow((t * 2.0) - 1.0, 2.0), 0.0, 1.0);
            double weight = endpointWeight + ((peakWeight - endpointWeight) * arch);
            double dy = maxDy * weight;
            arcControls.Add((point, dy));
            AddControl(controls, affectedPoints, point, 0, dy);
        }

        for (int i = 0; i < arcControls.Count - 1; i++)
        {
            (Point start, double startDy) = arcControls[i];
            (Point end, double endDy) = arcControls[i + 1];
            Point midpoint = new((start.X + end.X) * 0.5, (start.Y + end.Y) * 0.5);
            AddControl(controls, affectedPoints, midpoint, 0, (startDy + endDy) * 0.5);
        }
    }

    private static void AddUpperLipArcControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        double maxDy)
    {
        if (Math.Abs(maxDy) < 0.01)
        {
            return;
        }

        Dictionary<int, double> weights = new()
        {
            [40] = 0.40,
            [39] = 0.48,
            [37] = 0.56,
            [0] = 0.58,
            [267] = 0.56,
            [269] = 0.48,
            [270] = 0.40
        };
        foreach (int index in indices)
        {
            if (!TryGetLandmark(landmarks, index, out Point point) ||
                !weights.TryGetValue(index, out double weight))
            {
                continue;
            }

            AddControl(controls, affectedPoints, point, 0, maxDy * weight);
        }
    }

    private static void AddHardLipAnchorControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        int repeatCount)
    {
        foreach (int index in indices)
        {
            if (!TryGetLandmark(landmarks, index, out Point point))
            {
                continue;
            }

            affectedPoints.Add(point);
            for (int repeat = 0; repeat < Math.Max(1, repeatCount); repeat++)
            {
                AddSupportControl(controls, point, 0, 0);
            }
        }
    }

    private static void AddLipLineSegmentAnchors(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> lineIndices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        int repeatCount)
    {
        for (int i = 0; i < lineIndices.Count - 1; i++)
        {
            if (!TryGetLandmark(landmarks, lineIndices[i], out Point start) ||
                !TryGetLandmark(landmarks, lineIndices[i + 1], out Point end))
            {
                continue;
            }

            Point midpoint = new((start.X + end.X) * 0.5, (start.Y + end.Y) * 0.5);
            affectedPoints.Add(midpoint);
            for (int repeat = 0; repeat < Math.Max(1, repeatCount); repeat++)
            {
                AddSupportControl(controls, midpoint, 0, 0);
            }
        }
    }

    private static void AddLipAnchorControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls)
    {
        foreach (int index in indices)
        {
            if (TryGetLandmark(landmarks, index, out Point point))
            {
                AddSupportControl(controls, point, 0, 0);
            }
        }
    }

    private static void AddStrongLipAnchorControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        int repeatCount)
    {
        for (int repeat = 0; repeat < Math.Max(1, repeatCount); repeat++)
        {
            AddLipAnchorControls(landmarks, indices, controls);
        }
    }

    private static void AddMouthLandmarkControls(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        double dx,
        double dy)
    {
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
        {
            return;
        }

        foreach (int index in indices)
        {
            if (TryGetLandmark(landmarks, index, out Point point))
            {
                AddControl(controls, affectedPoints, point, dx, dy);
            }
        }
    }

    private static void AddLowerFaceDetailControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, 152, out Point chin) ||
            !TryGetLandmark(landmarks, 172, out Point leftJaw) ||
            !TryGetLandmark(landmarks, 397, out Point rightJaw))
        {
            return;
        }

        double jawWidth = Math.Max(1.0, rightJaw.X - leftJaw.X);
        double leftNeckSlim =
            CenteredStrength(s.NeckSlim, jawWidth * 0.030) +
            CenteredStrength(s.LeftSideNeck, jawWidth * 0.030);
        double rightNeckSlim =
            CenteredStrength(s.NeckSlim, jawWidth * 0.030) +
            CenteredStrength(s.RightSideNeck, jawWidth * 0.030);
        double doubleChinLift = Strength(s.DoubleChin, jawWidth * 0.115);
        double chinDy =
            CenteredStrength(s.NeckLength, jawWidth * 0.045) -
            (doubleChinLift * 0.34);
        double leftTrapeziusDrop = CenteredStrength(s.LeftTrapezius, jawWidth * 0.085);
        double rightTrapeziusDrop = CenteredStrength(s.RightTrapezius, jawWidth * 0.085);

        AddControl(controls, affectedPoints, leftJaw, leftNeckSlim + (doubleChinLift * 0.14), -doubleChinLift * 0.16);
        AddControl(controls, affectedPoints, rightJaw, -rightNeckSlim - (doubleChinLift * 0.14), -doubleChinLift * 0.16);
        AddControl(controls, affectedPoints, chin, 0, chinDy);
        AddControl(controls, affectedPoints, new Point((leftJaw.X + chin.X) * 0.5, chin.Y + jawWidth * 0.050), doubleChinLift * 0.30, -doubleChinLift * 0.76);
        AddControl(controls, affectedPoints, new Point((rightJaw.X + chin.X) * 0.5, chin.Y + jawWidth * 0.050), -doubleChinLift * 0.30, -doubleChinLift * 0.76);
        AddControl(controls, affectedPoints, new Point(chin.X, chin.Y + jawWidth * 0.090), 0, -doubleChinLift * 1.06);
        AddControl(controls, affectedPoints, new Point(chin.X, chin.Y + jawWidth * 0.160), 0, -doubleChinLift * 0.70);
        AddControl(controls, affectedPoints, new Point(leftJaw.X - (jawWidth * 0.22), chin.Y + (jawWidth * 0.105)), 0, leftTrapeziusDrop * 0.74);
        AddControl(controls, affectedPoints, new Point(leftJaw.X - (jawWidth * 0.36), chin.Y + (jawWidth * 0.190)), 0, leftTrapeziusDrop);
        AddControl(controls, affectedPoints, new Point(rightJaw.X + (jawWidth * 0.22), chin.Y + (jawWidth * 0.105)), 0, rightTrapeziusDrop * 0.74);
        AddControl(controls, affectedPoints, new Point(rightJaw.X + (jawWidth * 0.36), chin.Y + (jawWidth * 0.190)), 0, rightTrapeziusDrop);
    }

    private static FaceShapeWeightProfile CreateFaceDetailWeightProfile(
        Rect bounds,
        double radiusScale,
        double solidRadius)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Pose,
            bounds,
            bounds.Left + (bounds.Width * 0.5),
            bounds.Top + (bounds.Height * 0.5),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            Math.Max(1.0, bounds.Width * radiusScale),
            Math.Max(1.0, bounds.Height * radiusScale),
            solidRadius);
    }

    private static BitmapSource ApplyFaceDetailToneAdjustments(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot snapshot,
        Func<bool>? shouldCancel)
    {
        if (snapshot.LeftDarkCircle <= 0.001 &&
            snapshot.RightDarkCircle <= 0.001 &&
            snapshot.LeftUnderEye <= 0.001 &&
            snapshot.RightUnderEye <= 0.001 &&
            snapshot.DoubleChin <= 0.001 &&
            snapshot.NeckWrinkle <= 0.001)
        {
            return source;
        }

        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] pixels = new byte[stride * height];
        bgraSource.CopyPixels(pixels, stride, 0);

        ApplyUnderEyeTone(landmarks, pixels, width, height, stride, isLeft: true, snapshot.LeftDarkCircle, snapshot.LeftUnderEye, shouldCancel);
        ApplyUnderEyeTone(landmarks, pixels, width, height, stride, isLeft: false, snapshot.RightDarkCircle, snapshot.RightUnderEye, shouldCancel);
        ApplyDoubleChinTone(landmarks, pixels, width, height, stride, snapshot.DoubleChin, shouldCancel);
        ApplyNeckWrinkleTone(landmarks, pixels, width, height, stride, snapshot.NeckWrinkle, shouldCancel);

        if (shouldCancel?.Invoke() == true)
        {
            return source;
        }

        WriteableBitmap result = new(width, height, bgraSource.DpiX, bgraSource.DpiY, PixelFormats.Bgra32, null);
        result.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        result.Freeze();
        return result;
    }

    private static void ApplyUnderEyeTone(
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] pixels,
        int width,
        int height,
        int stride,
        bool isLeft,
        double darkStrength,
        double wrinkleStrength,
        Func<bool>? shouldCancel)
    {
        int outerIndex = isLeft ? 33 : 263;
        int innerIndex = isLeft ? 133 : 362;
        int bottomIndex = isLeft ? 145 : 374;
        if (!TryGetLandmark(landmarks, outerIndex, out Point outer) ||
            !TryGetLandmark(landmarks, innerIndex, out Point inner) ||
            !TryGetLandmark(landmarks, bottomIndex, out Point bottom))
        {
            return;
        }

        double eyeWidth = Math.Max(1.0, Math.Abs(inner.X - outer.X));
        Point center = new((outer.X + inner.X) * 0.5, bottom.Y + eyeWidth * 0.15);
        double radiusX = eyeWidth * 0.58;
        double radiusY = eyeWidth * 0.20;
        double liftAmount = Math.Clamp(darkStrength / 100.0, 0.0, 1.0) * 0.22;
        double softenAmount = Math.Clamp(wrinkleStrength / 100.0, 0.0, 1.0) * 0.16;
        if (liftAmount <= 0.001 && softenAmount <= 0.001)
        {
            return;
        }

        int left = Math.Max(0, (int)Math.Floor(center.X - radiusX));
        int top = Math.Max(0, (int)Math.Floor(center.Y - radiusY));
        int right = Math.Min(width - 1, (int)Math.Ceiling(center.X + radiusX));
        int bottomY = Math.Min(height - 1, (int)Math.Ceiling(center.Y + radiusY));
        for (int y = top; y <= bottomY; y++)
        {
            if (shouldCancel?.Invoke() == true)
            {
                return;
            }

            double ny = (y - center.Y) / Math.Max(1.0, radiusY);
            for (int x = left; x <= right; x++)
            {
                double nx = (x - center.X) / Math.Max(1.0, radiusX);
                double distance2 = (nx * nx) + (ny * ny);
                if (distance2 >= 1.0)
                {
                    continue;
                }

                double weight = 1.0 - SmoothStep01(Math.Sqrt(distance2));
                int index = (y * stride) + (x * 4);
                double amount = (liftAmount + softenAmount) * weight;
                pixels[index] = LiftChannel(pixels[index], amount * 0.78);
                pixels[index + 1] = LiftChannel(pixels[index + 1], amount * 0.92);
                pixels[index + 2] = LiftChannel(pixels[index + 2], amount);
            }
        }
    }

    private static void ApplyDoubleChinTone(
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] pixels,
        int width,
        int height,
        int stride,
        double doubleChinStrength,
        Func<bool>? shouldCancel)
    {
        if (!TryGetLandmark(landmarks, 152, out Point chin) ||
            !TryGetLandmark(landmarks, 172, out Point leftJaw) ||
            !TryGetLandmark(landmarks, 397, out Point rightJaw))
        {
            return;
        }

        double jawWidth = Math.Max(1.0, rightJaw.X - leftJaw.X);
        double amount = Math.Clamp(doubleChinStrength / 100.0, 0.0, 1.0);
        if (amount <= 0.001)
        {
            return;
        }

        Point center = new(chin.X, chin.Y + jawWidth * 0.095);
        double radiusX = jawWidth * 0.44;
        double radiusY = jawWidth * 0.24;
        double liftAmount = amount * 0.24;
        int left = Math.Max(0, (int)Math.Floor(center.X - radiusX));
        int top = Math.Max(0, (int)Math.Floor(center.Y - radiusY));
        int right = Math.Min(width - 1, (int)Math.Ceiling(center.X + radiusX));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(center.Y + radiusY));
        for (int y = top; y <= bottom; y++)
        {
            if (shouldCancel?.Invoke() == true)
            {
                return;
            }

            double ny = (y - center.Y) / Math.Max(1.0, radiusY);
            for (int x = left; x <= right; x++)
            {
                double nx = (x - center.X) / Math.Max(1.0, radiusX);
                double distance2 = (nx * nx) + (ny * ny);
                if (distance2 >= 1.0)
                {
                    continue;
                }

                double weight = 1.0 - SmoothStep01(Math.Sqrt(distance2));
                int index = (y * stride) + (x * 4);
                double weightedLift = liftAmount * weight;
                pixels[index] = LiftChannel(pixels[index], weightedLift * 0.74);
                pixels[index + 1] = LiftChannel(pixels[index + 1], weightedLift * 0.88);
                pixels[index + 2] = LiftChannel(pixels[index + 2], weightedLift);
            }
        }
    }

    private static void ApplyNeckWrinkleTone(
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] pixels,
        int width,
        int height,
        int stride,
        double neckWrinkleStrength,
        Func<bool>? shouldCancel)
    {
        double amount = Math.Clamp(neckWrinkleStrength / 100.0, 0.0, 1.0);
        if (amount <= 0.001 ||
            !TryGetLandmark(landmarks, 152, out Point chin) ||
            !TryGetLandmark(landmarks, 172, out Point leftJaw) ||
            !TryGetLandmark(landmarks, 397, out Point rightJaw))
        {
            return;
        }

        double jawWidth = Math.Max(1.0, rightJaw.X - leftJaw.X);
        Point center = new(chin.X, chin.Y + jawWidth * 0.300);
        double radiusX = jawWidth * 0.36;
        double radiusY = jawWidth * 0.34;
        double softenAmount = amount * 0.42;
        byte[] source = (byte[])pixels.Clone();
        int left = Math.Max(0, (int)Math.Floor(center.X - radiusX));
        int top = Math.Max(0, (int)Math.Floor(center.Y - radiusY));
        int right = Math.Min(width - 1, (int)Math.Ceiling(center.X + radiusX));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(center.Y + radiusY));
        for (int y = top; y <= bottom; y++)
        {
            if (shouldCancel?.Invoke() == true)
            {
                return;
            }

            double ny = (y - center.Y) / Math.Max(1.0, radiusY);
            for (int x = left; x <= right; x++)
            {
                double nx = (x - center.X) / Math.Max(1.0, radiusX);
                double distance2 = (nx * nx) + (ny * ny);
                if (distance2 >= 1.0)
                {
                    continue;
                }

                int index = (y * stride) + (x * 4);
                int avgB = AverageTextureChannel(source, width, height, stride, x, y, 0);
                int avgG = AverageTextureChannel(source, width, height, stride, x, y, 1);
                int avgR = AverageTextureChannel(source, width, height, stride, x, y, 2);
                double sourceBrightness = (source[index] + source[index + 1] + source[index + 2]) / 3.0;
                double averageBrightness = (avgB + avgG + avgR) / 3.0;
                double creaseWeight = Math.Clamp(0.58 + Math.Max(0.0, averageBrightness - sourceBrightness) / 42.0, 0.58, 1.0);
                double weight = (1.0 - SmoothStep01(Math.Sqrt(distance2))) * creaseWeight;
                double blend = softenAmount * weight;

                pixels[index] = LiftChannel(BlendChannel(pixels[index], avgB, blend), blend * 0.05);
                pixels[index + 1] = LiftChannel(BlendChannel(pixels[index + 1], avgG, blend), blend * 0.06);
                pixels[index + 2] = LiftChannel(BlendChannel(pixels[index + 2], avgR, blend), blend * 0.07);
            }
        }
    }

    private static void AddMouthWidthDeltas(
        Dictionary<int, (double Dx, double Dy)> deltas,
        double dx,
        bool isLeft)
    {
        if (Math.Abs(dx) < 0.01)
        {
            return;
        }

        (int Index, double Weight)[] entries = isLeft
            ? [
                (61, 1.00),
                (57, 0.34),
                (76, 0.32),
                (185, 0.30),
                (186, 0.28),
                (78, 0.16),
                (95, 0.14),
                (146, 0.14),
                (191, 0.14)
            ]
            : [
                (291, 1.00),
                (287, 0.34),
                (306, 0.32),
                (409, 0.30),
                (410, 0.28),
                (308, 0.16),
                (324, 0.14),
                (375, 0.14),
                (415, 0.14)
            ];

        foreach ((int index, double weight) in entries)
        {
            AddMouthDelta(deltas, index, dx * weight, 0);
        }
    }

    private static void AddMouthCornerVectorDeltas(
        Dictionary<int, (double Dx, double Dy)> deltas,
        double dx,
        double dy,
        bool isLeft)
    {
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
        {
            return;
        }

        (int Index, double XWeight, double YWeight)[] entries = isLeft
            ? [
                (61, 1.00, 1.00),
                (185, 0.66, 0.82),
                (146, 0.52, 0.72),
                (57, 0.32, 0.42),
                (76, 0.30, 0.44),
                (186, 0.22, 0.32)
            ]
            : [
                (291, 1.00, 1.00),
                (409, 0.66, 0.82),
                (375, 0.52, 0.72),
                (287, 0.32, 0.42),
                (306, 0.30, 0.44),
                (410, 0.22, 0.32)
            ];

        foreach ((int index, double xWeight, double yWeight) in entries)
        {
            AddMouthDelta(deltas, index, dx * xWeight, dy * yWeight);
        }
    }

    private static void AddMouthCornerDestinationBoosts(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double dx,
        double dy,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        (int Index, double XWeight, double YWeight, int Repeat)[] entries = isLeft
            ? [
                (61, 1.00, 1.00, 3),
                (185, 0.66, 0.82, 2),
                (146, 0.52, 0.72, 2)
            ]
            : [
                (291, 1.00, 1.00, 3),
                (409, 0.66, 0.82, 2),
                (375, 0.52, 0.72, 2)
            ];

        foreach ((int index, double xWeight, double yWeight, int repeat) in entries)
        {
            if (!TryGetLandmark(landmarks, index, out Point point))
            {
                continue;
            }

            for (int i = 0; i < repeat; i++)
            {
                AddControl(controls, affectedPoints, point, dx * xWeight, dy * yWeight);
            }
        }
    }

    private static void AddMouthCornerBrushControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double dx,
        double dy,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int cornerIndex = isLeft ? 61 : 291;
        int upperCornerIndex = isLeft ? 185 : 409;
        int lowerCornerIndex = isLeft ? 146 : 375;
        int upperFollowerIndex = isLeft ? 57 : 287;
        int lowerFollowerIndex = isLeft ? 76 : 306;
        if (!TryGetLandmark(landmarks, cornerIndex, out Point corner))
        {
            return;
        }

        if (TryGetLandmark(landmarks, upperCornerIndex, out Point upperCorner))
        {
            AddControl(controls, affectedPoints, AveragePoints(corner, upperCorner), dx * 0.72, dy * 0.82);
        }

        if (TryGetLandmark(landmarks, lowerCornerIndex, out Point lowerCorner))
        {
            AddControl(controls, affectedPoints, AveragePoints(corner, lowerCorner), dx * 0.58, dy * 0.68);
        }

        if (TryGetLandmark(landmarks, upperFollowerIndex, out Point upperFollower))
        {
            AddControl(controls, affectedPoints, AveragePoints(corner, upperFollower), dx * 0.38, dy * 0.42);
        }

        if (TryGetLandmark(landmarks, lowerFollowerIndex, out Point lowerFollower))
        {
            AddControl(controls, affectedPoints, AveragePoints(corner, lowerFollower), dx * 0.36, dy * 0.42);
        }

        if (TryGetLandmark(landmarks, upperCornerIndex, out upperCorner) &&
            TryGetLandmark(landmarks, lowerCornerIndex, out lowerCorner))
        {
            AddControl(
                controls,
                affectedPoints,
                AveragePoints(corner, upperCorner, lowerCorner),
                dx * 0.58,
                dy * 0.66);
        }
    }

    private static void AddMouthCornerBodyAnchors(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int[] lowerLineIndices = isLeft
            ? [78, 95, 88, 178, 87]
            : [308, 324, 318, 402, 317];
        int[] upperLineIndices = isLeft
            ? [78, 191, 80, 81, 82]
            : [308, 415, 310, 311, 312];

        foreach (int index in lowerLineIndices)
        {
            AddMouthCornerAnchorPoint(landmarks, index, controls, affectedPoints);
        }

        foreach (int index in upperLineIndices)
        {
            AddMouthCornerAnchorPoint(landmarks, index, controls, affectedPoints);
        }

        AddMouthCornerAnchorSegments(landmarks, lowerLineIndices, controls, affectedPoints);
        AddMouthCornerAnchorSegments(landmarks, upperLineIndices, controls, affectedPoints);
        AddMouthCornerInnerGuardAnchors(landmarks, isLeft, controls, affectedPoints);
    }

    private static void AddMouthCornerAnchorPoint(
        IReadOnlyDictionary<int, Point> landmarks,
        int index,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        if (!TryGetLandmark(landmarks, index, out Point point))
        {
            return;
        }

        affectedPoints.Add(point);
        int repeatCount = IsMouthInnerCornerAnchor(index) ? 32 : 14;
        for (int repeat = 0; repeat < repeatCount; repeat++)
        {
            AddSupportControl(controls, point, 0, 0);
        }
    }

    private static void AddMouthCornerAnchorSegments(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        for (int i = 0; i < indices.Count - 1; i++)
        {
            if (!TryGetLandmark(landmarks, indices[i], out Point start) ||
                !TryGetLandmark(landmarks, indices[i + 1], out Point end))
            {
                continue;
            }

            Point midpoint = new((start.X + end.X) * 0.5, (start.Y + end.Y) * 0.5);
            affectedPoints.Add(midpoint);
            for (int repeat = 0; repeat < 12; repeat++)
            {
                AddSupportControl(controls, midpoint, 0, 0);
            }
        }
    }

    private static void AddMouthCornerInnerGuardAnchors(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        int cornerIndex = isLeft ? 61 : 291;
        int[] innerIndices = isLeft ? [78, 95] : [308, 324];
        if (!TryGetLandmark(landmarks, cornerIndex, out Point corner))
        {
            return;
        }

        foreach (int innerIndex in innerIndices)
        {
            if (!TryGetLandmark(landmarks, innerIndex, out Point inner))
            {
                continue;
            }

            Point guard = new(
                (inner.X * 0.72) + (corner.X * 0.28),
                (inner.Y * 0.72) + (corner.Y * 0.28));
            affectedPoints.Add(guard);
            for (int repeat = 0; repeat < 18; repeat++)
            {
                AddSupportControl(controls, guard, 0, 0);
            }
        }
    }

    private static bool IsMouthInnerCornerAnchor(int index)
    {
        return index is 78 or 95 or 308 or 324;
    }

    private static void AddMouthDelta(
        Dictionary<int, (double Dx, double Dy)> deltas,
        int index,
        double dx,
        double dy)
    {
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
        {
            return;
        }

        if (deltas.TryGetValue(index, out (double Dx, double Dy) current))
        {
            deltas[index] = (current.Dx + dx, current.Dy + dy);
            return;
        }

        deltas.Add(index, (dx, dy));
    }

    private static void AddMouthDeltaControls(
        IReadOnlyDictionary<int, Point> landmarks,
        Dictionary<int, (double Dx, double Dy)> deltas,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        foreach (KeyValuePair<int, (double Dx, double Dy)> delta in deltas)
        {
            if (TryGetLandmark(landmarks, delta.Key, out Point point))
            {
                AddControl(controls, affectedPoints, point, delta.Value.Dx, delta.Value.Dy);
            }
        }
    }

    private static byte LiftChannel(byte value, double amount)
    {
        return (byte)Math.Clamp((int)Math.Round(value + ((255 - value) * amount)), 0, 255);
    }

    private static byte BlendChannel(byte value, int target, double amount)
    {
        return (byte)Math.Clamp((int)Math.Round(value + ((target - value) * amount)), 0, 255);
    }

    private static int AverageTextureChannel(byte[] source, int width, int height, int stride, int x, int y, int channel)
    {
        int sum = 0;
        int count = 0;
        for (int oy = -4; oy <= 4; oy += 2)
        {
            int sy = Math.Clamp(y + oy, 0, height - 1);
            for (int ox = -2; ox <= 2; ox += 2)
            {
                int sx = Math.Clamp(x + ox, 0, width - 1);
                sum += source[(sy * stride) + (sx * 4) + channel];
                count++;
            }
        }

        return count == 0 ? source[(y * stride) + (x * 4) + channel] : sum / count;
    }

    private static Rect BuildFaceDetailBounds(
        IReadOnlyList<Point> points,
        int width,
        int height,
        double marginRatio = 0.30)
    {
        double left = points.Min(p => p.X);
        double top = points.Min(p => p.Y);
        double right = points.Max(p => p.X);
        double bottom = points.Max(p => p.Y);
        double margin = Math.Max(8.0, Math.Max(right - left, bottom - top) * marginRatio);
        left = Math.Clamp(left - margin, 0, Math.Max(0, width - 1));
        top = Math.Clamp(top - margin, 0, Math.Max(0, height - 1));
        right = Math.Clamp(right + margin, 0, Math.Max(0, width - 1));
        bottom = Math.Clamp(bottom + margin, 0, Math.Max(0, height - 1));
        return new Rect(left, top, Math.Max(1.0, right - left), Math.Max(1.0, bottom - top));
    }

    private static void AddFaceDetailAnchorFrame(List<FaceShapeControlPoint> controls, Rect bounds, int width, int height)
    {
        double left = Math.Clamp(bounds.Left, 0, Math.Max(0, width - 1));
        double right = Math.Clamp(bounds.Right, 0, Math.Max(0, width - 1));
        double top = Math.Clamp(bounds.Top, 0, Math.Max(0, height - 1));
        double bottom = Math.Clamp(bounds.Bottom, 0, Math.Max(0, height - 1));
        double centerX = (left + right) * 0.5;
        double centerY = (top + bottom) * 0.5;
        controls.Add(new FaceShapeControlPoint(left, top, 0, 0));
        controls.Add(new FaceShapeControlPoint(centerX, top, 0, 0));
        controls.Add(new FaceShapeControlPoint(right, top, 0, 0));
        controls.Add(new FaceShapeControlPoint(left, centerY, 0, 0));
        controls.Add(new FaceShapeControlPoint(right, centerY, 0, 0));
        controls.Add(new FaceShapeControlPoint(left, bottom, 0, 0));
        controls.Add(new FaceShapeControlPoint(centerX, bottom, 0, 0));
        controls.Add(new FaceShapeControlPoint(right, bottom, 0, 0));
    }

    private static void AddControl(
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints,
        Point point,
        double dx,
        double dy)
    {
        if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
        {
            return;
        }

        controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, dy));
        affectedPoints.Add(point);
    }

    private static void AddSupportControl(
        List<FaceShapeControlPoint> controls,
        Point point,
        double dx,
        double dy)
    {
        controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, dy));
    }

    private static bool TryGetLandmark(IReadOnlyDictionary<int, Point> landmarks, int index, out Point point)
    {
        return landmarks.TryGetValue(index, out point);
    }

    private static List<Point> GetExistingPoints(IReadOnlyDictionary<int, Point> landmarks, IReadOnlyList<int> indices)
    {
        List<Point> points = new(indices.Count);
        foreach (int index in indices)
        {
            if (TryGetLandmark(landmarks, index, out Point point))
            {
                points.Add(point);
            }
        }

        return points;
    }

    private static Point AveragePoints(params Point[] points)
    {
        return AveragePoints((IReadOnlyList<Point>)points);
    }

    private static Point AveragePoints(IReadOnlyList<Point> points)
    {
        if (points.Count == 0)
        {
            return default;
        }

        double x = 0;
        double y = 0;
        for (int i = 0; i < points.Count; i++)
        {
            x += points[i].X;
            y += points[i].Y;
        }

        return new Point(x / points.Count, y / points.Count);
    }

    private static double Lerp(double start, double end, double amount)
    {
        return start + ((end - start) * amount);
    }

    private static double Strength(double value, double maxAmount)
    {
        return Math.Clamp(value / 100.0, 0.0, 1.0) * maxAmount;
    }

    private static double CenteredStrength(double value, double maxAmount)
    {
        double normalized = (Math.Clamp(value, 0.0, 100.0) - FaceDetailNeutralSliderValue) / FaceDetailNeutralSliderValue;
        return Math.Clamp(normalized, -1.0, 1.0) * maxAmount;
    }

    private static double CenteredStrengthInDirection(double value, double maxAmount, int direction)
    {
        return CenteredStrength(value, maxAmount) * Math.Sign(direction);
    }

    private static bool HasCenteredEffect(double value)
    {
        return Math.Abs(Math.Clamp(value, 0.0, 100.0) - FaceDetailNeutralSliderValue) > 0.001;
    }

    private readonly record struct FaceDetailWarpPlan(
        Rect Bounds,
        double Sigma,
        List<FaceShapeControlPoint> Controls,
        double RadiusScale,
        double SolidRadius);
}
