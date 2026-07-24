using KRetouchStudio.Tabs;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private static BitmapSource RenderFaceShapePipelineStage(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarkPoints,
        FaceShapePointArray pointArray,
        byte[]? personAlphaPixels,
        FaceShapeAdjustmentSnapshot state,
        Func<bool> shouldCancel)
    {
        BitmapSource result = source;
        if (state.Symmetry > 0.001)
        {
            result = BuildFaceShapeSymmetryPreview(
                result,
                landmarkPoints,
                state.Symmetry,
                shouldCancel);
        }

        if (!shouldCancel() && state.UpperAlign > 0.001)
        {
            if (personAlphaPixels is null ||
                personAlphaPixels.Length < result.PixelWidth * result.PixelHeight)
            {
                throw new InvalidOperationException("Face Shape Upper requires a current person mask.");
            }

            result = BuildFaceShapeUpperPreview(
                result,
                landmarkPoints,
                personAlphaPixels,
                state.UpperAlign,
                shouldCancel);
        }

        if (!shouldCancel() && state.Cheek > 0.001)
        {
            result = BuildFaceShapeCheekPreview(
                result,
                landmarkPoints,
                state.Cheek,
                shouldCancel);
        }

        if (!shouldCancel() && state.Bone > 0.001)
        {
            result = BuildFaceShapeBonePreview(
                result,
                landmarkPoints,
                state.Bone,
                shouldCancel);
        }

        if (!shouldCancel() && state.Jaw > 0.001)
        {
            result = BuildFaceShapeJawPreview(
                result,
                landmarkPoints,
                state.Jaw,
                shouldCancel);
        }

        if (!shouldCancel() && state.Chin > 0.001)
        {
            result = BuildFaceShapeChinPreview(
                result,
                landmarkPoints,
                state.Chin,
                shouldCancel);
        }

        if (!shouldCancel() && Math.Abs(state.FaceTilt - 50) > 0.001)
        {
            if (TryBuildFaceShapeFaceTiltReliefPoints(
                    landmarkPoints,
                    result.PixelWidth,
                    result.PixelHeight,
                    state.FaceTilt,
                    out _,
                    out _,
                    out _,
                    out List<FaceShapeControlPoint> reliefControls,
                    out List<Point> facePolygon))
            {
                result = BuildFaceShapeHeadTiltReliefPreview(
                    result,
                    reliefControls,
                    facePolygon,
                    shouldCancel,
                    outsideFeather: FaceShapeFaceTiltOutsideFeatherPx);
            }
        }

        if (!shouldCancel() && Math.Abs(state.FaceTurn - 50) > 0.001)
        {
            if (TryBuildFaceShapeFaceTurnRigidMaskPlan(
                    pointArray,
                    result.PixelWidth,
                    result.PixelHeight,
                    state.FaceTurn,
                    out _,
                    out FaceShapeRigidMaskPlan? faceTurnPlan) &&
                faceTurnPlan is not null)
            {
                result = BuildFaceShapeHeadTiltRigidMaskPreview(
                    result,
                    faceTurnPlan,
                    shouldCancel);
            }
        }

        if (!shouldCancel() && Math.Abs(state.HeadTilt - 50) > 0.001)
        {
            double renderStrength = 100.0 - state.HeadTilt;
            if (TryBuildFaceShapeHeadTiltRigidMaskPlan(
                    pointArray,
                    result.PixelWidth,
                    result.PixelHeight,
                    renderStrength,
                    out _,
                    out FaceShapeRigidMaskPlan? headTiltPlan) &&
                headTiltPlan is not null)
            {
                result = BuildFaceShapeHeadTiltRigidMaskPreview(
                    result,
                    headTiltPlan,
                    shouldCancel);
            }
        }

        return ReferenceEquals(result, source)
            ? CloneBitmapSource(source)
            : result;
    }
}
