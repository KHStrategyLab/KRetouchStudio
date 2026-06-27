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
            await ApplyFaceDetailDragPreviewAsync(e);
        }
        catch (Exception ex)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"Face Detail preview failed: {ex.Message}";
        }
    }

    private async void FaceDetailRetouchTab_FaceDetailAdjustmentCommitted(object? sender, FaceDetailAdjustmentEventArgs e)
    {
        await ApplyFaceDetailCommittedAsync(e);
    }

    private void FaceDetailRetouchTab_FaceDetailResetRequested(object? sender, EventArgs e)
    {
        TryResetFaceDetailHistory();
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

    private void TryResetFaceDetailHistory()
    {
        if (!CanResetFaceDetailHistory() ||
            SelectedPhoto is not PhotoItem targetPhoto ||
            !TryGetFaceDetailResetSource(targetPhoto, out BitmapSource resetSource))
        {
            UpdateFaceDetailHistoryResetState();
            return;
        }

        targetPhoto.SetAdjustedImage(resetSource.IsFrozen ? resetSource : CloneBitmapSource(resetSource));
        FaceDetailRetouchTab.ResetAfterHistoryReset();
        ClearFaceDetailRetouchSession();
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
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
            HasCenteredEffect(s.LeftEyeWidth) ||
            HasCenteredEffect(s.RightEyeWidth) ||
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
            HasCenteredEffect(s.MouthSize) ||
            HasCenteredEffect(s.MouthWidth) ||
            HasCenteredEffect(s.MouthVertical) ||
            HasCenteredEffect(s.LeftMouthCorner) ||
            HasCenteredEffect(s.RightMouthCorner) ||
            HasCenteredEffect(s.LeftSmileBalance) ||
            HasCenteredEffect(s.RightSmileBalance) ||
            HasCenteredEffect(s.UpperLip) ||
            HasCenteredEffect(s.LowerLip) ||
            HasCenteredEffect(s.NeckSlim) ||
            HasCenteredEffect(s.NeckLength) ||
            s.NeckWrinkle > 0.001 ||
            s.DoubleChin > 0.001 ||
            HasCenteredEffect(s.LeftSideNeck) ||
            HasCenteredEffect(s.RightSideNeck) ||
            HasCenteredEffect(s.LeftShoulderNeck) ||
            HasCenteredEffect(s.RightShoulderNeck);
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
                snapshot.LeftEyeWidth,
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
                snapshot.RightEyeWidth,
                snapshot.RightEyeTilt,
                snapshot.EyeDistance,
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
                snapshot.LeftBrowThickness,
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
                snapshot.RightBrowThickness,
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
        double width,
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
        double eyeWidth = Math.Max(1.0, Math.Abs(inner.X - outer.X));
        double eyeHeight = Math.Max(1.0, Math.Abs(bottom.Y - top.Y));
        double distanceDx = CenteredStrengthInDirection(distance, eyeWidth * 0.22, isLeft ? -1 : 1);
        double sizeX = CenteredStrength(size, eyeWidth * 0.13);
        double sizeY = CenteredStrength(size, eyeHeight * 0.32);
        double heightY = CenteredStrength(height, eyeHeight * 0.42);
        double widthX = CenteredStrength(width, eyeWidth * 0.16);
        double tiltY = CenteredStrength(tilt, eyeHeight * 0.55);

        AddControl(controls, affectedPoints, outer, distanceDx + (isLeft ? -sizeX - widthX : sizeX + widthX), isLeft ? -tiltY : -tiltY);
        AddControl(controls, affectedPoints, inner, distanceDx + (isLeft ? sizeX + widthX : -sizeX - widthX), isLeft ? tiltY : tiltY);
        AddControl(controls, affectedPoints, top, distanceDx, -sizeY - heightY);
        AddControl(controls, affectedPoints, bottom, distanceDx, sizeY + heightY);
        AddControl(controls, affectedPoints, center, distanceDx, 0);
    }

    private static void AddSingleBrowControls(
        IReadOnlyDictionary<int, Point> landmarks,
        bool isLeft,
        double thickness,
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
        double thicknessLift = -CenteredStrength(thickness, browWidth * 0.035);
        double distanceDx = CenteredStrengthInDirection(distance, browWidth * 0.12, isLeft ? -1 : 1);
        double tiltY = CenteredStrength(tilt, browWidth * 0.055);
        double tailY = -CenteredStrength(tail, browWidth * 0.075);

        for (int i = 0; i < points.Count; i++)
        {
            Point p = points[i];
            double normalizedX = (p.X - center.X) / Math.Max(1.0, browWidth * 0.5);
            double dy = lift + thicknessLift + (archLift * (1.0 - Math.Min(1.0, Math.Abs(normalizedX))));
            dy += normalizedX * (isLeft ? tiltY : -tiltY);
            if ((isLeft && p.X < center.X) || (!isLeft && p.X > center.X))
            {
                dy += tailY;
            }

            AddControl(controls, affectedPoints, p, distanceDx, dy);
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
        double sizeExpand = CenteredStrength(s.NoseSize, noseWidth * 0.24);
        double widthExpand = CenteredStrength(s.NoseWidth, noseWidth * 0.32);
        double bridgePull = CenteredStrength(s.NoseBridge, noseWidth * 0.20);
        double tipLift = -CenteredStrength(s.NoseTip, noseHeight * 0.18);
        double lengthDrop = CenteredStrength(s.NoseLength, noseHeight * 0.16);
        double nostrilExpandL = CenteredStrength(s.LeftNostril, noseWidth * 0.26);
        double nostrilExpandR = CenteredStrength(s.RightNostril, noseWidth * 0.26);

        AddControl(controls, affectedPoints, leftNostril, -sizeExpand - widthExpand - nostrilExpandL, 0);
        AddControl(controls, affectedPoints, rightNostril, sizeExpand + widthExpand + nostrilExpandR, 0);
        AddControl(controls, affectedPoints, tip, 0, tipLift + lengthDrop);
        AddControl(controls, affectedPoints, bridge, 0, -bridgePull * 0.32);
        AddControl(controls, affectedPoints, new Point(center.X - noseWidth * 0.22, center.Y), bridgePull * 1.05, 0);
        AddControl(controls, affectedPoints, new Point(center.X + noseWidth * 0.22, center.Y), -bridgePull * 1.05, 0);
        AddControl(controls, affectedPoints, new Point(center.X - noseWidth * 0.34, center.Y + noseHeight * 0.18), bridgePull + (widthExpand * 0.52), 0);
        AddControl(controls, affectedPoints, new Point(center.X + noseWidth * 0.34, center.Y + noseHeight * 0.18), -bridgePull - (widthExpand * 0.52), 0);
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

        Point center = AveragePoints(leftCorner, rightCorner, upper, lower);
        double mouthWidth = Math.Max(1.0, rightCorner.X - leftCorner.X);
        double mouthHeight = Math.Max(1.0, lower.Y - upper.Y);
        double sizeX = CenteredStrength(s.MouthSize, mouthWidth * 0.08);
        double sizeY = CenteredStrength(s.MouthSize, mouthHeight * 0.24);
        double widthX = CenteredStrength(s.MouthWidth, mouthWidth * 0.12);
        double verticalLift = -CenteredStrength(s.MouthVertical, mouthHeight * 0.35);
        double upperLift = -CenteredStrength(s.UpperLip, mouthHeight * 0.40);
        double lowerDrop = CenteredStrength(s.LowerLip, mouthHeight * 0.40);
        double leftSmile =
            -CenteredStrength(s.LeftSmileBalance, mouthHeight * 0.28) -
            CenteredStrength(s.LeftMouthCorner, mouthHeight * 0.28);
        double rightSmile =
            -CenteredStrength(s.RightSmileBalance, mouthHeight * 0.28) -
            CenteredStrength(s.RightMouthCorner, mouthHeight * 0.28);

        AddControl(controls, affectedPoints, leftCorner, -sizeX - widthX, verticalLift + leftSmile);
        AddControl(controls, affectedPoints, rightCorner, sizeX + widthX, verticalLift + rightSmile);
        AddControl(controls, affectedPoints, upper, 0, verticalLift - sizeY + upperLift);
        AddControl(controls, affectedPoints, lower, 0, verticalLift + sizeY + lowerDrop);
        AddControl(controls, affectedPoints, center, 0, verticalLift);
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
            CenteredStrength(s.LeftSideNeck, jawWidth * 0.030) +
            CenteredStrength(s.LeftShoulderNeck, jawWidth * 0.022);
        double rightNeckSlim =
            CenteredStrength(s.NeckSlim, jawWidth * 0.030) +
            CenteredStrength(s.RightSideNeck, jawWidth * 0.030) +
            CenteredStrength(s.RightShoulderNeck, jawWidth * 0.022);
        double doubleChinLift = Strength(s.DoubleChin, jawWidth * 0.115);
        double neckWrinkleLift = Strength(s.NeckWrinkle, jawWidth * 0.034);
        double chinDy =
            CenteredStrength(s.NeckLength, jawWidth * 0.045) -
            (doubleChinLift * 0.34) -
            neckWrinkleLift;

        AddControl(controls, affectedPoints, leftJaw, leftNeckSlim + (doubleChinLift * 0.14), -doubleChinLift * 0.16);
        AddControl(controls, affectedPoints, rightJaw, -rightNeckSlim - (doubleChinLift * 0.14), -doubleChinLift * 0.16);
        AddControl(controls, affectedPoints, chin, 0, chinDy);
        AddControl(controls, affectedPoints, new Point((leftJaw.X + chin.X) * 0.5, chin.Y + jawWidth * 0.050), doubleChinLift * 0.30, -doubleChinLift * 0.76);
        AddControl(controls, affectedPoints, new Point((rightJaw.X + chin.X) * 0.5, chin.Y + jawWidth * 0.050), -doubleChinLift * 0.30, -doubleChinLift * 0.76);
        AddControl(controls, affectedPoints, new Point(chin.X, chin.Y + jawWidth * 0.090), 0, -doubleChinLift * 1.06 - (neckWrinkleLift * 0.45));
        AddControl(controls, affectedPoints, new Point(chin.X, chin.Y + jawWidth * 0.160), 0, -doubleChinLift * 0.70 - (neckWrinkleLift * 0.30));
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
        ApplyDoubleChinTone(landmarks, pixels, width, height, stride, snapshot.DoubleChin, snapshot.NeckWrinkle, shouldCancel);

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
        double neckWrinkleStrength,
        Func<bool>? shouldCancel)
    {
        if (!TryGetLandmark(landmarks, 152, out Point chin) ||
            !TryGetLandmark(landmarks, 172, out Point leftJaw) ||
            !TryGetLandmark(landmarks, 397, out Point rightJaw))
        {
            return;
        }

        double jawWidth = Math.Max(1.0, rightJaw.X - leftJaw.X);
        double amount = Math.Clamp(((doubleChinStrength * 1.05) + (neckWrinkleStrength * 0.38)) / 100.0, 0.0, 1.0);
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

    private static byte LiftChannel(byte value, double amount)
    {
        return (byte)Math.Clamp((int)Math.Round(value + ((255 - value) * amount)), 0, 255);
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
