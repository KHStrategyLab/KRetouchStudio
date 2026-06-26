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
            s.EyeSize > 0.001 ||
            s.LeftEyeHeight > 0.001 ||
            s.RightEyeHeight > 0.001 ||
            s.LeftEyeWidth > 0.001 ||
            s.RightEyeWidth > 0.001 ||
            s.LeftEyeTilt > 0.001 ||
            s.RightEyeTilt > 0.001 ||
            s.EyeDistance > 0.001 ||
            s.LeftDarkCircle > 0.001 ||
            s.RightDarkCircle > 0.001 ||
            s.LeftUnderEye > 0.001 ||
            s.RightUnderEye > 0.001 ||
            s.LeftBrowThickness > 0.001 ||
            s.RightBrowThickness > 0.001 ||
            s.BrowDistance > 0.001 ||
            s.LeftBrowTilt > 0.001 ||
            s.RightBrowTilt > 0.001 ||
            s.LeftBrowArch > 0.001 ||
            s.RightBrowArch > 0.001 ||
            s.LeftBrowPosition > 0.001 ||
            s.RightBrowPosition > 0.001 ||
            s.LeftBrowTail > 0.001 ||
            s.RightBrowTail > 0.001 ||
            s.NoseSize > 0.001 ||
            s.NoseLength > 0.001 ||
            s.NoseBridge > 0.001 ||
            s.NoseWidth > 0.001 ||
            s.NoseTip > 0.001 ||
            s.LeftNostril > 0.001 ||
            s.RightNostril > 0.001 ||
            s.MouthSize > 0.001 ||
            s.MouthWidth > 0.001 ||
            s.MouthVertical > 0.001 ||
            s.LeftMouthCorner > 0.001 ||
            s.RightMouthCorner > 0.001 ||
            s.LeftSmileBalance > 0.001 ||
            s.RightSmileBalance > 0.001 ||
            s.UpperLip > 0.001 ||
            s.LowerLip > 0.001 ||
            s.NeckSlim > 0.001 ||
            s.NeckLength > 0.001 ||
            s.NeckWrinkle > 0.001 ||
            s.DoubleChin > 0.001 ||
            s.LeftSideNeck > 0.001 ||
            s.RightSideNeck > 0.001 ||
            s.LeftShoulderNeck > 0.001 ||
            s.RightShoulderNeck > 0.001;
    }

    private static BitmapSource BuildFaceDetailRetouchPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot snapshot,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        BitmapSource current = bgraSource;
        if (TryBuildFaceDetailWarpPlan(landmarks, bgraSource.PixelWidth, bgraSource.PixelHeight, snapshot, out FaceDetailWarpPlan warpPlan))
        {
            current = BuildFaceShapeControlWarpPreview(
                bgraSource,
                warpPlan.Controls,
                warpPlan.Sigma,
                CreateFaceDetailWeightProfile(warpPlan.Bounds),
                shouldCancel);
        }

        if (shouldCancel?.Invoke() == true)
        {
            return current;
        }

        return ApplyFaceDetailToneAdjustments(current, landmarks, snapshot, shouldCancel);
    }

    private static bool TryBuildFaceDetailWarpPlan(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        FaceDetailAdjustmentSnapshot snapshot,
        out FaceDetailWarpPlan plan)
    {
        List<FaceShapeControlPoint> controls = [];
        List<Point> affectedPoints = [];

        AddEyeDetailControls(landmarks, snapshot, controls, affectedPoints);
        AddBrowDetailControls(landmarks, snapshot, controls, affectedPoints);
        AddNoseDetailControls(landmarks, snapshot, controls, affectedPoints);
        AddMouthDetailControls(landmarks, snapshot, controls, affectedPoints);
        AddLowerFaceDetailControls(landmarks, snapshot, controls, affectedPoints);

        if (controls.Count == 0 || affectedPoints.Count == 0)
        {
            plan = default;
            return false;
        }

        Rect bounds = BuildFaceDetailBounds(affectedPoints, width, height);
        AddFaceDetailAnchorFrame(controls, bounds, width, height);
        double sigma = Math.Max(8.0, Math.Max(bounds.Width, bounds.Height) * 0.18);
        plan = new FaceDetailWarpPlan(bounds, sigma, controls);
        return true;
    }

    private static void AddEyeDetailControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        AddSingleEyeControls(landmarks, isLeft: true, s.EyeSize, s.LeftEyeHeight, s.LeftEyeWidth, s.LeftEyeTilt, s.EyeDistance, controls, affectedPoints);
        AddSingleEyeControls(landmarks, isLeft: false, s.EyeSize, s.RightEyeHeight, s.RightEyeWidth, s.RightEyeTilt, s.EyeDistance, controls, affectedPoints);
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
        double distanceDx = SignedStrength(distance, eyeWidth * 0.22, isLeft ? -1 : 1);
        double sizeX = Strength(size, eyeWidth * 0.13);
        double sizeY = Strength(size, eyeHeight * 0.32);
        double heightY = Strength(height, eyeHeight * 0.42);
        double widthX = Strength(width, eyeWidth * 0.16);
        double tiltY = Strength(tilt, eyeHeight * 0.55);

        AddControl(controls, affectedPoints, outer, distanceDx + (isLeft ? -sizeX - widthX : sizeX + widthX), isLeft ? -tiltY : -tiltY);
        AddControl(controls, affectedPoints, inner, distanceDx + (isLeft ? sizeX + widthX : -sizeX - widthX), isLeft ? tiltY : tiltY);
        AddControl(controls, affectedPoints, top, distanceDx, -sizeY - heightY);
        AddControl(controls, affectedPoints, bottom, distanceDx, sizeY + heightY);
        AddControl(controls, affectedPoints, center, distanceDx, 0);
    }

    private static void AddBrowDetailControls(
        IReadOnlyDictionary<int, Point> landmarks,
        FaceDetailAdjustmentSnapshot s,
        List<FaceShapeControlPoint> controls,
        List<Point> affectedPoints)
    {
        AddSingleBrowControls(landmarks, isLeft: true, s.LeftBrowThickness, s.LeftBrowTilt, s.LeftBrowArch, s.LeftBrowPosition, s.LeftBrowTail, s.BrowDistance, controls, affectedPoints);
        AddSingleBrowControls(landmarks, isLeft: false, s.RightBrowThickness, s.RightBrowTilt, s.RightBrowArch, s.RightBrowPosition, s.RightBrowTail, s.BrowDistance, controls, affectedPoints);
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
        double lift = -Strength(position, browWidth * 0.10);
        double archLift = -Strength(arch, browWidth * 0.07);
        double thicknessLift = -Strength(thickness, browWidth * 0.035);
        double distanceDx = SignedStrength(distance, browWidth * 0.12, isLeft ? -1 : 1);
        double tiltY = Strength(tilt, browWidth * 0.055);
        double tailY = -Strength(tail, browWidth * 0.075);

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
        double sizePull = Strength(s.NoseSize, noseWidth * 0.13);
        double widthPull = Strength(s.NoseWidth, noseWidth * 0.18);
        double bridgePull = Strength(s.NoseBridge, noseWidth * 0.09);
        double tipLift = -Strength(s.NoseTip + s.NoseLength, noseHeight * 0.09);
        double nostrilPullL = Strength(s.LeftNostril, noseWidth * 0.16);
        double nostrilPullR = Strength(s.RightNostril, noseWidth * 0.16);

        AddControl(controls, affectedPoints, leftNostril, sizePull + widthPull + nostrilPullL, 0);
        AddControl(controls, affectedPoints, rightNostril, -sizePull - widthPull - nostrilPullR, 0);
        AddControl(controls, affectedPoints, tip, 0, tipLift);
        AddControl(controls, affectedPoints, bridge, 0, -bridgePull * 0.25);
        AddControl(controls, affectedPoints, new Point(center.X - noseWidth * 0.22, center.Y), bridgePull, 0);
        AddControl(controls, affectedPoints, new Point(center.X + noseWidth * 0.22, center.Y), -bridgePull, 0);
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
        double sizeX = Strength(s.MouthSize, mouthWidth * 0.08);
        double sizeY = Strength(s.MouthSize, mouthHeight * 0.24);
        double widthX = Strength(s.MouthWidth, mouthWidth * 0.12);
        double verticalLift = -Strength(s.MouthVertical, mouthHeight * 0.35);
        double upperLift = -Strength(s.UpperLip, mouthHeight * 0.40);
        double lowerDrop = Strength(s.LowerLip, mouthHeight * 0.40);
        double leftSmile = -Strength(s.LeftSmileBalance + s.LeftMouthCorner, mouthHeight * 0.42);
        double rightSmile = -Strength(s.RightSmileBalance + s.RightMouthCorner, mouthHeight * 0.42);

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
        double neckSlim = Strength(
            s.NeckSlim + s.LeftSideNeck + s.RightSideNeck + s.LeftShoulderNeck + s.RightShoulderNeck,
            jawWidth * 0.045);
        double chinLift = -Strength(s.DoubleChin + s.NeckLength + s.NeckWrinkle, jawWidth * 0.035);

        AddControl(controls, affectedPoints, leftJaw, neckSlim, 0);
        AddControl(controls, affectedPoints, rightJaw, -neckSlim, 0);
        AddControl(controls, affectedPoints, chin, 0, chinLift);
    }

    private static FaceShapeWeightProfile CreateFaceDetailWeightProfile(Rect bounds)
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
            Math.Max(1.0, bounds.Width * 0.58),
            Math.Max(1.0, bounds.Height * 0.58),
            0.70);
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
            snapshot.RightUnderEye <= 0.001)
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

    private static byte LiftChannel(byte value, double amount)
    {
        return (byte)Math.Clamp((int)Math.Round(value + ((255 - value) * amount)), 0, 255);
    }

    private static Rect BuildFaceDetailBounds(IReadOnlyList<Point> points, int width, int height)
    {
        double left = points.Min(p => p.X);
        double top = points.Min(p => p.Y);
        double right = points.Max(p => p.X);
        double bottom = points.Max(p => p.Y);
        double margin = Math.Max(12.0, Math.Max(right - left, bottom - top) * 0.45);
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

    private static double SignedStrength(double value, double maxAmount, int direction)
    {
        return Strength(value, maxAmount) * Math.Sign(direction);
    }

    private readonly record struct FaceDetailWarpPlan(
        Rect Bounds,
        double Sigma,
        List<FaceShapeControlPoint> Controls);
}
