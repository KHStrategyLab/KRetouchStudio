using System.Buffers;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const string FaceShapeSymmetryHistoryTitle = "Face Sym";
    private const string FaceShapeSymmetryHistoryDetail = "Sym";
    private const double FaceShapeSymmetryMaxCorrection = 1.0;
    private const string FaceShapeUpperHistoryTitle = "Face Upper";
    private const string FaceShapeUpperHistoryDetail = "Upper";
    private const string FaceShapeCheekHistoryTitle = "Face Cheek";
    private const string FaceShapeCheekHistoryDetail = "Cheek";
    private const double FaceShapeCheekMaxInwardRatio = 0.075;
    private const string FaceShapeBoneHistoryTitle = "Face Bone";
    private const string FaceShapeBoneHistoryDetail = "Bone";
    private const double FaceShapeBoneMaxInwardRatio = 0.065;
    private const string FaceShapeJawHistoryTitle = "Face Jaw";
    private const string FaceShapeJawHistoryDetail = "Jaw";
    private const double FaceShapeJawMaxInwardRatio = 0.085;
    private const string FaceShapeChinHistoryTitle = "Face Chin";
    private const string FaceShapeChinHistoryDetail = "Chin";
    private const double FaceShapeChinMaxInwardRatio = 0.075;
    private const double FaceShapeChinMaxLiftRatio = 0.028;
    private const string FaceShapeFaceTiltHistoryTitle = "Face F-Tilt";
    private const string FaceShapeFaceTiltHistoryDetail = "F-Tilt";
    private const double FaceShapeFaceTiltMaxDegrees = 2.5;
    private const double FaceShapeFaceTiltOutsideFeatherPx = 28.0;
    private const string FaceShapeFaceTurnHistoryTitle = "Face Turn";
    private const string FaceShapeFaceTurnHistoryDetail = "Turn";
    private const double FaceShapeFaceTurnMaxShiftRatio = 0.065;
    private const double FaceShapeFaceTurnProjectionMaxDegrees = 24.0;
    private const double FaceShapeFaceTurnContourDepthRatio = 0.05;
    private const double FaceShapeFaceTurnVerticalProjectionRatio = 0.03;
    private const double FaceShapeHeadPoseBaseFocalLengthMm = 50.0;
    private const double FaceShapeHeadPoseBaseCameraDistanceMm = 1500.0;
    private const double FaceShapeHeadPoseCameraFocalLengthMm = 50.0;
    private const double FaceShapeHeadPoseCameraDistanceMm = 1500.0;
    private const double FaceShapeHeadPoseCameraFocalLengthRatio =
        2.8 * FaceShapeHeadPoseCameraFocalLengthMm / FaceShapeHeadPoseBaseFocalLengthMm;
    private const double FaceShapeHeadPoseCameraDistanceRatio =
        3.4 * FaceShapeHeadPoseCameraDistanceMm / FaceShapeHeadPoseBaseCameraDistanceMm;
    private const string FaceShapeHeadTiltHistoryTitle = "Face Up/Dn";
    private const string FaceShapeHeadTiltHistoryDetail = "Up/Dn";
    private const double FaceShapeHeadTiltMaxShiftRatio = 0.060;
    private const double FaceShapeHeadTiltProjectionMaxDegrees = 8.75;
    private const double FaceShapeHeadTiltProjectionZScale = 0.35;
    private const double FaceShapeHeadTiltProjectionPivotDepthRatio = 0.10;
    private const double FaceShapeHeadTiltMaskLiftDepthRatio = 0.12;
    private const double FaceShapeHeadTiltCanonicalDepthRatio = 0.09;
    private const double FaceShapeHeadTiltCanonicalMinZ = -2.435867;
    private const double FaceShapeHeadTiltCanonicalNoseTipZ = 7.586580;
    private const double FaceShapeHeadTiltLowerMaskExtensionRatio = 0.03;
    private const double FaceShapeHeadTiltRigidEdgeFeatherPx = 8.0;
    private const double FaceShapeHeadTiltLowerEdgeFeatherPx = 10.0;
    private const double FaceShapeHeadTiltAttachBandInnerPx = 10.0;
    private const double FaceShapeHeadTiltAttachBandOuterPx = 50.0;
    private const double FaceShapeHeadTiltAttachBandStrength = 0.45;
    private const double FaceShapeHeadTiltJawRiseCompensationStrength = 0.85;
    private const double FaceShapeHeadTiltJawDropCompensationStrength = 0.35;
    private const double FaceShapeHeadTiltReliefFeatherPx = 15.0;
    private const double FaceShapeHeadTiltOutsideFeatherPx = 28.0;
    private const double FaceShapeHeadTiltOutsideFeatherStrength = 0.60;
    private const double FaceShapeHeadTiltReliefSigmaRatio = 0.095;
    private const int FaceShapeHeadPoseDragPreviewLongSide = PhotoItem.PreviewProxyLongSide;
    private const double FaceShapeHeadPoseDragPreviewUnsharpAmount = 1.0;

    private static readonly int[] FaceShapeHeadTiltJawCompensationIndices = [400, 377, 152, 148, 176];

    private static readonly double[] FaceShapeHeadTiltCanonicalZ =
    [
        5.979507, 7.475604, 6.058267, 6.633583, 7.586580, 7.242870, 5.788627, 3.279702,
        5.284764, 5.385258, 4.481535, 5.864924, 5.569430, 5.219482, 5.404754, 5.529457,
        5.601448, 5.535441, 5.071372, 7.112196, 6.447657, 0.099620, 3.848121, 3.796952,
        3.646194, 3.155168, 3.851822, 4.115822, 4.092203, 3.972409, 3.719554, 2.776093,
        4.389812, 3.173422, 0.073150, 2.204059, 4.433130, 5.877044, 5.444923, 5.496189,
        5.028930, 5.189648, 4.842263, 4.188483, 7.360529, 7.440358, 3.363159, 4.558359,
        5.814193, 5.581319, 3.534632, 7.101013, 4.550454, 4.048484, 1.425850, 5.106035,
        4.000575, 4.095905, -1.745401, 5.737747, 5.833208, 4.283884, 4.162499, 3.751977,
        5.456949, 4.921559, 5.015990, 3.729163, 2.724259, 4.529981, 2.830852, 1.591691,
        5.737804, 5.417779, 5.000579, 5.662455, 4.258156, 4.520883, 4.038093, 6.512274,
        4.604908, 4.926693, 5.138202, 4.985883, 5.448304, 5.509612, 5.449371, 5.339869,
        4.745470, 4.813632, 4.854463, 4.823737, 4.868096, -2.431321, 6.601390, 4.399021,
        4.497052, 5.866456, 5.241087, 5.863759, 4.294303, 4.113860, 5.273355, 2.660442,
        3.683424, 4.466315, 4.455874, 5.316032, 4.921106, 4.274997, 3.375099, 2.431726,
        3.843181, 3.083858, 4.831091, 6.415959, 1.689873, 2.974312, 3.522615, 3.872784,
        4.039035, 4.224124, 5.566789, 1.881175, 2.687839, 7.071491, 4.895421, -2.005167,
        4.448999, 4.850470, 3.084075, 6.097047, -2.252455, 3.757904, 6.671944, 1.892523,
        0.714711, -0.072044, 0.924091, 0.724695, 4.119213, 6.573301, 4.566119, 1.560843,
        3.635230, 3.775704, 4.479786, 1.663542, 4.094063, 2.802001, 1.925119, 5.027311,
        4.264492, 3.777151, 3.697603, 3.689148, 2.038516, 3.775031, 3.871767, 3.876973,
        3.724325, 3.482983, -0.991917, 3.440898, 5.932265, 5.213116, 5.952465, 5.815343,
        5.236015, 2.671970, 3.382963, 4.702456, -0.631468, 3.744030, 5.754256, 4.891441,
        3.609070, -0.131163, 5.120727, 5.189564, 5.237051, 5.205010, 4.757893, 4.431713,
        4.555096, 4.582438, 4.484994, 2.861224, 5.196779, 4.231404, 3.881555, 4.247264,
        2.109852, 4.861453, 4.521085, 6.774605, 6.148363, 6.316750, 5.681036, 5.181173,
        5.153478, 4.979576, 3.795752, 4.590085, 4.096152, 4.137731, 4.440943, 3.643886,
        4.969286, 5.201008, 3.311079, 3.814195, 3.726453, 1.473482, 2.983221, 0.070268,
        4.130198, 5.307551, 6.538785, 5.905127, 7.003423, 4.327696, 4.364629, 4.309028,
        4.076063, 3.646321, 2.670867, -0.048769, 3.109532, 3.476944, 3.731945, 3.865444,
        3.961782, 4.084996, -2.435867, 5.630378, 6.233232, 7.077932, 6.787447, 6.798303,
        5.480490, 6.957705, 6.508676, 3.863736, 4.203800, 4.615849, 3.315305, 3.339685,
        6.633583, 3.279702, 6.447657, 0.099620, 3.848121, 3.796952, 3.646194, 3.155168,
        3.851822, 4.115822, 4.092203, 3.972409, 3.719554, 2.776093, 4.389812, 3.173422,
        0.073150, 2.204059, 4.433130, 5.877044, 5.444923, 5.496189, 5.028930, 5.189648,
        4.842263, 4.188483, 7.360529, 7.440358, 3.363159, 4.558359, 5.814193, 5.581319,
        3.534632, 7.101013, 4.550454, 4.048484, 1.425850, 5.106035, 4.000575, 4.095905,
        -1.745401, 5.737747, 5.833208, 4.283884, 4.162499, 3.751977, 5.456949, 4.921559,
        5.015990, 3.729163, 2.724259, 4.529981, 2.830852, 1.591691, 5.737804, 5.417779,
        5.000579, 5.662455, 4.258156, 4.520883, 4.038093, 6.512274, 4.604908, 4.926693,
        5.138202, 4.985883, 5.448304, 5.509612, 5.449371, 5.339869, 4.745470, 4.813632,
        4.854463, 4.823737, 4.868096, -2.431321, 4.399021, 4.497052, 5.866456, 5.241087,
        5.863759, 4.294303, 4.113860, 5.273355, 2.660442, 3.683424, 4.466315, 4.455874,
        5.316032, 4.921106, 4.274997, 3.375099, 2.431726, 3.843181, 3.083858, 4.831091,
        6.415959, 1.689873, 2.974312, 3.522615, 3.872784, 4.039035, 4.224124, 5.566789,
        1.881175, 2.687839, 7.071491, 4.895421, -2.005167, 4.448999, 4.850470, 3.084075,
        6.097047, -2.252455, 3.757904, 6.671944, 1.892523, 0.714711, -0.072044, 0.924091,
        0.724695, 4.119213, 6.573301, 4.566119, 1.560843, 3.635230, 3.775704, 4.479786,
        1.663542, 4.094063, 2.802001, 1.925119, 3.777151, 3.697603, 3.689148, 2.038516,
        3.775031, 3.871767, 3.876973, 3.724325, 3.482983, -0.991917, 3.440898, 5.213116,
        5.952465, 5.815343, 2.671970, 3.382963, 4.702456, -0.631468, 3.744030, 5.754256,
        3.609070, -0.131163, 5.120727, 5.189564, 5.237051, 5.205010, 4.757893, 4.431713,
        4.555096, 4.582438, 4.484994, 2.861224, 5.196779, 4.231404, 3.881555, 4.247264,
        2.109852, 4.861453, 4.521085, 6.148363, 5.681036, 4.979576, 3.795752, 4.590085,
        4.096152, 4.137731, 4.440943, 3.643886, 4.969286, 5.201008, 3.311079, 3.814195,
        3.726453, 1.473482, 2.983221, 0.070268, 4.130198, 5.307551, 6.538785, 5.905127,
        7.003423, 4.327696, 4.364629, 4.309028, 4.076063, 3.646321, 2.670867, -0.048769,
        3.109532, 3.476944, 3.731945, 3.865444, 3.961782, 4.084996, -2.435867, 5.630378,
        6.233232, 7.077932, 6.787447, 6.798303, 5.480490, 6.957705, 6.508676, 3.863736,
        4.203800, 4.615849, 3.315305, 3.339685,
    ];

    private static readonly int[] FaceShapeHeadTiltLeftEyeOuterCornerIndices = [33];
    private static readonly int[] FaceShapeHeadTiltRightEyeOuterCornerIndices = [263];
    private static readonly int[] FaceShapeHeadPoseBrowCenterPivotIndices = [8];
    private static readonly int[] FaceShapeHeadTiltNoseTipIndices = [4];
    private static readonly int[] FaceShapeHeadTiltChinTipIndices = [152];
    private static readonly int[] FaceShapeHeadTiltLeftMouthCornerIndices = [61];
    private static readonly int[] FaceShapeHeadTiltRightMouthCornerIndices = [291];

    private static readonly (int[] Indices, bool Closed)[] FaceShapeHeadTiltFeaturePaths =
    [
        ([70, 63, 105, 66, 107, 55, 65, 52, 53, 46], false),
        ([336, 296, 334, 293, 300, 276, 283, 282, 295, 285], false),
        ([33, 246, 161, 160, 159, 158, 157, 173, 133, 155, 154, 153, 145, 144, 163, 7], true),
        ([362, 398, 384, 385, 386, 387, 388, 466, 263, 249, 390, 373, 374, 380, 381, 382], true),
        ([61, 146, 91, 181, 84, 17, 314, 405, 321, 375, 291, 409, 270, 269, 267, 0, 37, 39, 40, 185], true),
        ([78, 95, 88, 178, 87, 14, 317, 402, 318, 324, 308, 415, 310, 311, 312, 13, 82, 81, 80, 191], true)
    ];

    private static readonly int[] FaceShapeHeadTiltFaceOvalIndices =
    [
        10, 338, 297, 332, 284, 251, 389, 356, 454, 323, 361, 288, 397, 365, 379, 378,
        400, 377, 152, 148, 176, 149, 150, 136, 172, 58, 132, 93, 234, 127, 162, 21,
        54, 103, 67, 109
    ];

    private static readonly int[] FaceShapeFaceTurnCentralIndices =
    [
        1, 2, 4, 5, 6, 19, 94, 168, 195, 197
    ];

    private static readonly int[] FaceShapeFaceTurnFeatureEdgeIndices =
    [
        33, 46, 61, 70, 78, 107, 133, 152, 172, 234,
        263, 276, 291, 300, 308, 336, 362, 397, 454
    ];

    private static readonly int[] FaceShapeFaceTurnFeatherBoostIndices =
    [
        58, 93, 132, 136, 148, 149, 150, 172, 176, 234,
        288, 323, 361, 365, 377, 378, 379, 397, 400, 454
    ];

    private static readonly int[] FaceShapeSymmetryMidlineIndices =
    [
        10, 168, 6, 197, 195, 5, 4, 1, 19, 94, 2, 152
    ];

    private static readonly (int Left, int Right)[] FaceShapeSymmetryPairs =
    [
        (33, 263),
        (133, 362),
        (159, 386),
        (145, 374),
        (61, 291),
        (78, 308),
        (70, 300),
        (63, 293),
        (105, 334),
        (66, 296),
        (107, 336),
        (234, 454),
        (93, 323),
        (132, 361),
        (58, 288),
        (172, 397),
        (136, 365),
        (150, 379),
        (149, 378),
        (176, 400),
        (148, 377)
    ];

    private static readonly (int Left, int Right, double Weight)[] FaceShapeJawPairs =
    [
        (234, 454, 0.00),
        (93, 323, 0.05),
        (132, 361, 0.15),
        (58, 288, 0.60),
        (172, 397, 1.00),
        (136, 365, 0.85),
        (150, 379, 0.40),
        (149, 378, 0.20),
        (176, 400, 0.05),
        (148, 377, 0.00)
    ];

    private static readonly (int Left, int Right, double Weight)[] FaceShapeChinPairs =
    [
        (136, 365, 0.20),
        (150, 379, 0.36),
        (149, 378, 0.62),
        (176, 400, 0.88),
        (148, 377, 1.00)
    ];

    private static readonly int[] FaceShapeChinCenterIndices =
    [
        152, 175, 199, 200
    ];

    private static readonly (int Left, int Right, double Weight)[] FaceShapeCheekPairs =
    [
        (234, 454, 0.12),
        (93, 323, 0.22),
        (132, 361, 0.25),
        (50, 280, 0.40),
        (101, 330, 0.55),
        (118, 347, 0.80),
        (123, 352, 0.92),
        (187, 411, 0.90),
        (205, 425, 1.00),
        (206, 426, 0.88),
        (207, 427, 0.68),
        (213, 433, 0.48)
    ];

    private static readonly (int Left, int Right, double Weight)[] FaceShapeBonePairs =
    [
        (127, 356, 0.65),
        (234, 454, 1.00),
        (93, 323, 0.92),
        (132, 361, 0.40),
        (50, 280, 0.36),
        (101, 330, 0.48),
        (118, 347, 0.45),
        (123, 352, 0.35),
        (187, 411, 0.20),
        (205, 425, 0.16)
    ];

    private static readonly int[] FaceShapeJawAnchorIndices =
    [
        1, 4, 5, 6, 13, 14, 17, 78, 308, 152, 199, 200
    ];

    private static readonly int[] FaceShapeChinAnchorIndices =
    [
        1, 4, 5, 6, 13, 14, 17, 61, 78, 93, 132, 172, 234, 288, 291, 308, 323, 361, 397, 454
    ];

    private static readonly int[] FaceShapeCheekAnchorIndices =
    [
        1, 4, 5, 6, 13, 14, 17, 33, 61, 78, 133, 152, 159, 168, 263, 291, 308, 362, 386
    ];

    private static readonly int[] FaceShapeBoneAnchorIndices =
    [
        1, 4, 5, 6, 10, 13, 14, 17, 33, 61, 78, 133, 152, 159, 168, 199, 263, 291, 308, 362, 386
    ];

    private static readonly int[] FaceShapeFaceTiltMoveIndices =
    [
        1, 2, 4, 5, 6, 13, 14, 17, 19, 21, 33, 46, 50, 52, 53, 54, 55, 61, 63, 65, 66, 70,
        78, 80, 81, 82, 84, 87, 88, 91, 94, 95, 103, 105, 107, 124, 127, 133, 143, 144, 145,
        146, 153, 154, 155, 156, 157, 158, 159, 160, 161, 163, 168, 173, 178, 181, 190, 191,
        193, 195, 197, 221, 222, 223, 225, 226, 243, 244, 246, 249, 251, 263, 276, 280, 282,
        283, 284, 285, 291, 293, 295, 296, 300, 308, 310, 311, 312, 314, 317, 318, 321, 324,
        332, 334, 336, 353, 356, 362, 372, 373, 374, 375, 380, 381, 382, 383, 384, 385, 386,
        387, 388, 390, 398, 402, 414, 415, 417, 441, 442, 443, 445, 446, 463, 464, 466
    ];

    private static readonly int[] FaceShapeFaceTiltAnchorIndices =
    [
        10, 58, 67, 93, 109, 132, 136, 148, 149, 150, 152, 162, 172, 176, 199, 200, 234,
        288, 297, 323, 338, 361, 365, 377, 378, 379, 389, 397, 400, 454
    ];

    private static readonly int[] FaceShapeFaceTiltPivotIndices =
    [
        1, 4, 5, 6, 168, 195, 197
    ];

    private PhotoItem? _faceShapeSymmetrySessionPhoto;
    private BitmapSource? _faceShapeSymmetrySessionBaseImage;
    private PhotoItem? _faceShapeUpperSessionPhoto;
    private BitmapSource? _faceShapeUpperSessionBaseImage;
    private PhotoItem? _faceShapeCheekSessionPhoto;
    private BitmapSource? _faceShapeCheekSessionBaseImage;
    private PhotoItem? _faceShapeBoneSessionPhoto;
    private BitmapSource? _faceShapeBoneSessionBaseImage;
    private PhotoItem? _faceShapeJawSessionPhoto;
    private BitmapSource? _faceShapeJawSessionBaseImage;
    private PhotoItem? _faceShapeChinSessionPhoto;
    private BitmapSource? _faceShapeChinSessionBaseImage;
    private PhotoItem? _faceShapeFaceTiltSessionPhoto;
    private BitmapSource? _faceShapeFaceTiltSessionBaseImage;
    private PhotoItem? _faceShapeFaceTurnSessionPhoto;
    private BitmapSource? _faceShapeFaceTurnSessionBaseImage;
    private PhotoItem? _faceShapeHeadTiltSessionPhoto;
    private BitmapSource? _faceShapeHeadTiltSessionBaseImage;
    private readonly SemaphoreSlim _faceShapeLandmarkCacheGate = new(1, 1);
    private PhotoItem? _faceShapeLandmarkCachePhoto;
    private string? _faceShapeLandmarkCachePath;
    private List<MediaPipeLandmarkPoint> _faceShapeLandmarkCache = [];
    private PhotoItem? _faceShapePointMapCachePhoto;
    private string? _faceShapePointMapCachePath;
    private int _faceShapePointMapCacheWidth;
    private int _faceShapePointMapCacheHeight;
    private Dictionary<int, Point> _faceShapePointMapCache = [];
    private PhotoItem? _faceShapePointArrayCachePhoto;
    private string? _faceShapePointArrayCachePath;
    private int _faceShapePointArrayCacheWidth;
    private int _faceShapePointArrayCacheHeight;
    private FaceShapePointArray _faceShapePointArrayCache = FaceShapePointArray.Empty;
    private PhotoItem? _faceShapeHeadPoseDragPreviewPhoto;
    private string? _faceShapeHeadPoseDragPreviewPath;
    private BitmapSource? _faceShapeHeadPoseDragPreviewImageSource;
    private double _faceShapeHeadPoseDragPreviewFrameWidth;
    private double _faceShapeHeadPoseDragPreviewFrameHeight;
    private PhotoItem? _faceShapeHeadPoseDragProxyPhoto;
    private string? _faceShapeHeadPoseDragProxyPath;
    private BitmapSource? _faceShapeHeadPoseDragProxyBaseSource;
    private BitmapSource? _faceShapeHeadPoseDragProxySource;
    private int _faceShapeSymmetryRenderVersion;
    private int _faceShapeUpperPrewarmVersion;

    private readonly struct FaceShapePointArray(Point[] points, double[] zValues, bool[] hasPoint, int count)
    {
        public static FaceShapePointArray Empty { get; } = new([], [], [], 0);

        public Point[] Points { get; } = points;

        public double[] ZValues { get; } = zValues;

        public bool[] HasPoint { get; } = hasPoint;

        public int Count { get; } = count;
    }

    private sealed record FaceShapeRigidMaskPlan(
        Point[] SourcePoints,
        Point[] ProjectedPoints,
        List<FaceShapeRigidTriangle> Triangles,
        List<Point> SourcePolygon,
        List<Point> ProjectedPolygon);

    private readonly record struct FaceShapeRigidTriangle(int A, int B, int C);

    private readonly record struct FaceShapeDelaunayEdge(int A, int B);

    private readonly record struct FaceShapeDelaunayTriangle(
        int A,
        int B,
        int C,
        double CircumX,
        double CircumY,
        double CircumRadiusSquared);

    private async void FaceShapeRetouchTab_HeadPoseAdjustmentPreviewChanged(object? sender, EventArgs e)
    {
        try
        {
            await ApplyFaceShapeHeadPoseDragPreviewAsync();
        }
        catch (Exception ex)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"Face Head Pose preview failed: {ex.Message}";
        }
    }

    private async void FaceShapeRetouchTab_FaceShapeAdjustmentCommitted(object? sender, EventArgs e)
    {
        if (FaceShapeRetouchTab is null)
        {
            return;
        }

        bool isHeadPoseCommit =
            FaceShapeRetouchTab.IsFaceTiltFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsFaceTurnFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected;
        bool isFaceShapeControlCommit =
            FaceShapeRetouchTab.IsCheekFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsBoneFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsJawFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsChinFaceShapeModeSelected;
        bool isSymmetrizeCommit =
            FaceShapeRetouchTab.IsSymFaceShapeModeSelected ||
            FaceShapeRetouchTab.IsAlignFaceShapeModeSelected;
        if (!isHeadPoseCommit && !isFaceShapeControlCommit && !isSymmetrizeCommit)
        {
            ClearFaceShapeHeadPoseDragPreview();
        }

        if (!FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected)
        {
            ClearFaceShapeProjectionDebugOverlay();
        }

        if (FaceShapeRetouchTab.IsSymFaceShapeModeSelected)
        {
            await ApplyFaceShapeSymmetryPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsCheekFaceShapeModeSelected)
        {
            await ApplyFaceShapeCheekPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsBoneFaceShapeModeSelected)
        {
            await ApplyFaceShapeBonePreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsJawFaceShapeModeSelected)
        {
            await ApplyFaceShapeJawPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsChinFaceShapeModeSelected)
        {
            await ApplyFaceShapeChinPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsFaceTiltFaceShapeModeSelected)
        {
            await ApplyFaceShapeFaceTiltPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsFaceTurnFaceShapeModeSelected)
        {
            await ApplyFaceShapeFaceTurnPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected)
        {
            await ApplyFaceShapeHeadTiltPreviewAsync();
            return;
        }

        if (FaceShapeRetouchTab.IsAlignFaceShapeModeSelected)
        {
            await ApplyFaceShapeUpperPreviewAsync();
            return;
        }

        MediaPipeStatusText = "Facial Reshape: select a mode";
    }

    private async Task ApplyFaceShapeSymmetryPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Sym: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.SymFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeSymmetryRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Sym: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeSymmetryHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Sym: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Sym");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Sym: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Sym: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeSymmetryPreview(
                safeBase,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceFaceShapeSymmetryHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Sym: applied {strength:0}";
    }

    private async Task ApplyFaceShapeUpperPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Upper: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.AlignFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeUpperRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Upper: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeUpperHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Upper: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Upper");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Upper: no landmarks";
            return;
        }

        string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(alphaPath))
        {
            MediaPipeStatusText = "Face Upper: no person mask";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        byte[] alphaPixels = GetOrCreateRefinedPersonAlphaMask(alphaPath, baseSource.PixelWidth, baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Upper: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeUpperPreview(
                safeBase,
                landmarkPoints,
                alphaPixels,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        PushOrReplaceFaceShapeUpperHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Upper: applied {strength:0}";
    }

    private async Task ApplyFaceShapeSymmetrizeDragPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || FaceShapeRetouchTab is null)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        bool isFace = FaceShapeRetouchTab.IsSymFaceShapeModeSelected;
        bool isUpper = FaceShapeRetouchTab.IsAlignFaceShapeModeSelected;
        if (!isFace && !isUpper)
        {
            return;
        }

        double strength = isFace
            ? Math.Clamp(Math.Round(FaceShapeRetouchTab.SymFaceShapeStrength), 0, 100)
            : Math.Clamp(Math.Round(FaceShapeRetouchTab.AlignFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = isFace
            ? GetFaceShapeSymmetryRenderSource(targetPhoto)
            : GetFaceShapeUpperRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);
        string statusPrefix = isFace ? "Face Sym" : "Face Upper";

        if (strength <= 0.001)
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, statusPrefix);
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = $"{statusPrefix}: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            proxySource.PixelWidth,
            proxySource.PixelHeight);
        byte[]? alphaPixels = null;
        if (isUpper)
        {
            string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
            if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
                renderVersion != _faceShapeSymmetryRenderVersion)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(alphaPath))
            {
                MediaPipeStatusText = "Face Upper: no person mask";
                return;
            }

            alphaPixels = GetOrCreateRefinedPersonAlphaMask(alphaPath, proxySource.PixelWidth, proxySource.PixelHeight);
        }

        BitmapSource safeProxy = CloneBitmapSource(proxySource);
        double renderStrength = strength;
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {strength:0}...";

        BitmapSource preview = await Task.Run(() => isFace
            ? BuildFaceShapeSymmetryPreview(
                safeProxy,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion)
            : BuildFaceShapeUpperPreview(
                safeProxy,
                landmarkPoints,
                alphaPixels!,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {strength:0}";
    }

    private async Task PrewarmFaceShapeUpperAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            return;
        }

        int prewarmVersion = Interlocked.Increment(ref _faceShapeUpperPrewarmVersion);
        BitmapSource baseSource = GetFaceShapeUpperRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Upper");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            prewarmVersion != _faceShapeUpperPrewarmVersion)
        {
            return;
        }

        if (landmarks.Count > 0)
        {
            _ = GetOrCreateFaceShapePointMap(
                targetPhoto,
                landmarks,
                proxySource.PixelWidth,
                proxySource.PixelHeight);
        }

        string? alphaPath = await GetOrCreatePersonAlphaPathAsync(targetPhoto);
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            prewarmVersion != _faceShapeUpperPrewarmVersion)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(alphaPath))
        {
            _ = GetOrCreateRefinedPersonAlphaMask(alphaPath, proxySource.PixelWidth, proxySource.PixelHeight);
        }

        if (FaceShapeRetouchTab?.IsAlignFaceShapeModeSelected == true)
        {
            MediaPipeStatusText = "Face Upper: warmed";
        }
    }

    private async Task ApplyFaceShapeCheekPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Cheek: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.CheekFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeCheekRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Cheek: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeCheekHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Cheek: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Cheek");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Cheek: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Cheek: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeCheekPreview(
                safeBase,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeCheekHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Cheek: applied {strength:0}";
    }

    private async Task ApplyFaceShapeBonePreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Bone: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.BoneFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeBoneRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Bone: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeBoneHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Bone: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Bone");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Bone: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Bone: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeBonePreview(
                safeBase,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeBoneHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Bone: applied {strength:0}";
    }

    private async Task ApplyFaceShapeJawPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Jaw: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.JawFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeJawRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Jaw: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeJawHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Jaw: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Jaw");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Jaw: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Jaw: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeJawPreview(
                safeBase,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeJawHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Jaw: applied {strength:0}";
    }

    private async Task ApplyFaceShapeChinPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Chin: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.ChinFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeChinRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Chin: original-size image only";
            return;
        }

        if (strength <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeChinHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Chin: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Chin");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = "Face Chin: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Chin: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeChinPreview(
                safeBase,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeChinHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Chin: applied {strength:0}";
    }

    private async Task ApplyFaceShapeFaceTiltPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face F-Tilt: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.FaceTiltFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeFaceTiltRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face F-Tilt: original-size image only";
            return;
        }

        if (Math.Abs(strength - 50) <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            ClearFaceShapeProjectionDebugOverlay();
            PushOrReplaceFaceShapeFaceTiltHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face F-Tilt: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face F-Tilt");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face F-Tilt: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        HideMediaPipeFeatureOverlayForFaceShapeDebug();
        if (!TryBuildFaceShapeFaceTiltReliefPoints(
                landmarkPoints,
                baseSource.PixelWidth,
                baseSource.PixelHeight,
                strength,
                out List<Point> sourcePoints,
                out List<Point> projectedPoints,
                out List<IReadOnlyList<Point>> meshPaths,
                out List<FaceShapeControlPoint> reliefControls,
                out List<Point> facePolygon))
        {
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeProjectionDebugOverlay();
            MediaPipeStatusText = "Face F-Tilt: face-line points unavailable";
            return;
        }

        BitmapSource safeBase = CloneBitmapSource(baseSource);
        MediaPipeStatusText = $"Face F-Tilt: relief warp {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeHeadTiltReliefPreview(
                safeBase,
                reliefControls,
                facePolygon,
                () => renderVersion != _faceShapeSymmetryRenderVersion,
                outsideFeather: FaceShapeFaceTiltOutsideFeatherPx));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeFaceTiltHistory(targetPhoto, strength);
        ClearFaceShapeProjectionDebugOverlay();
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face F-Tilt: relief {projectedPoints.Count:0} pts | lines {meshPaths.Count:0} | {strength:0}";
    }

    private async Task ApplyFaceShapeFaceTurnPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Turn: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.FaceTurnFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeFaceTurnRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Turn: original-size image only";
            return;
        }

        if (Math.Abs(strength - 50) <= 0.001)
        {
            targetPhoto.SetAdjustedImage(baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource));
            ClearFaceShapeProjectionDebugOverlay();
            PushOrReplaceFaceShapeFaceTurnHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Turn: reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Turn");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Turn: no landmarks";
            return;
        }

        HideMediaPipeFeatureOverlayForFaceShapeDebug();
        FaceShapePointArray landmarkPoints = GetOrCreateFaceShapePointArray(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        if (!TryBuildFaceShapeFaceTurnRigidMaskPlan(
                landmarkPoints,
                baseSource.PixelWidth,
                baseSource.PixelHeight,
                strength,
                out int projectedPointCount,
                out FaceShapeRigidMaskPlan? rigidMaskPlan))
        {
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeProjectionDebugOverlay();
            MediaPipeStatusText = "Face Turn: rigid mask unavailable";
            return;
        }

        FaceShapeRigidMaskPlan committedRigidMaskPlan = rigidMaskPlan!;
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        MediaPipeStatusText = $"Face Turn: rigid mask {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeHeadTiltRigidMaskPreview(
                safeBase,
                committedRigidMaskPlan,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeFaceTurnHistory(targetPhoto, strength);
        ClearFaceShapeProjectionDebugOverlay();
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Turn: rigid {projectedPointCount:0} pts | mesh {committedRigidMaskPlan.Triangles.Count:0} | {strength:0}";
    }

    private async Task ApplyFaceShapeHeadTiltPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Up/Dn: load photo first";
            return;
        }

        double sliderStrength = Math.Clamp(Math.Round(FaceShapeRetouchTab.HeadTiltFaceShapeStrength), 0, 100);
        double strength = 100.0 - sliderStrength;
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeHeadTiltRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Up/Dn: original-size image only";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, "Face Up/Dn");
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = "Face Up/Dn: no landmarks";
            return;
        }

        HideMediaPipeFeatureOverlayForFaceShapeDebug();
        FaceShapePointArray landmarkPoints = GetOrCreateFaceShapePointArray(
            targetPhoto,
            landmarks,
            baseSource.PixelWidth,
            baseSource.PixelHeight);
        if (!TryBuildFaceShapeHeadTiltRigidMaskPlan(
                landmarkPoints,
                baseSource.PixelWidth,
                baseSource.PixelHeight,
                strength,
                out int projectedPointCount,
                out FaceShapeRigidMaskPlan? rigidMaskPlan))
        {
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeProjectionDebugOverlay();
            MediaPipeStatusText = "Face Up/Dn: rigid mask unavailable";
            return;
        }

        FaceShapeRigidMaskPlan committedRigidMaskPlan = rigidMaskPlan!;
        BitmapSource safeBase = CloneBitmapSource(baseSource);
        double renderStrength = sliderStrength;
        MediaPipeStatusText = $"Face Up/Dn: rigid mask {sliderStrength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeHeadTiltRigidMaskPreview(
                safeBase,
                committedRigidMaskPlan,
                () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeHeadTiltHistory(targetPhoto, renderStrength);
        ClearFaceShapeProjectionDebugOverlay();
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Up/Dn: rigid {projectedPointCount:0} pts | mesh {committedRigidMaskPlan.Triangles.Count:0} | {sliderStrength:0}";
    }

    private async Task ApplyFaceShapeControlDragPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || FaceShapeRetouchTab is null)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        bool isCheek = FaceShapeRetouchTab.IsCheekFaceShapeModeSelected;
        bool isBone = FaceShapeRetouchTab.IsBoneFaceShapeModeSelected;
        bool isJaw = FaceShapeRetouchTab.IsJawFaceShapeModeSelected;
        bool isChin = FaceShapeRetouchTab.IsChinFaceShapeModeSelected;
        if (!isCheek && !isBone && !isJaw && !isChin)
        {
            return;
        }

        double strength = isCheek
            ? Math.Clamp(Math.Round(FaceShapeRetouchTab.CheekFaceShapeStrength), 0, 100)
            : isBone
                ? Math.Clamp(Math.Round(FaceShapeRetouchTab.BoneFaceShapeStrength), 0, 100)
                : isJaw
                    ? Math.Clamp(Math.Round(FaceShapeRetouchTab.JawFaceShapeStrength), 0, 100)
                    : Math.Clamp(Math.Round(FaceShapeRetouchTab.ChinFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = isCheek
            ? GetFaceShapeCheekRenderSource(targetPhoto)
            : isBone
                ? GetFaceShapeBoneRenderSource(targetPhoto)
                : isJaw
                    ? GetFaceShapeJawRenderSource(targetPhoto)
                    : GetFaceShapeChinRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);

        string statusPrefix = isCheek
            ? "Face Cheek"
            : isBone
                ? "Face Bone"
                : isJaw
                    ? "Face Jaw"
                    : "Face Chin";
        if (strength <= 0.001)
        {
            SetFaceShapeHeadPoseDragPreview(targetPhoto, proxySource, baseSource.PixelWidth, baseSource.PixelHeight);
            MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview reset";
            return;
        }

        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, statusPrefix);
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = $"{statusPrefix}: no landmarks";
            return;
        }

        IReadOnlyDictionary<int, Point> landmarkPoints = GetOrCreateFaceShapePointMap(
            targetPhoto,
            landmarks,
            proxySource.PixelWidth,
            proxySource.PixelHeight);
        BitmapSource safeProxy = CloneBitmapSource(proxySource);
        double renderStrength = strength;
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {strength:0}...";

        BitmapSource preview = await Task.Run(() => isCheek
            ? BuildFaceShapeCheekPreview(
                safeProxy,
                landmarkPoints,
                renderStrength,
                () => renderVersion != _faceShapeSymmetryRenderVersion)
            : isBone
                ? BuildFaceShapeBonePreview(
                    safeProxy,
                    landmarkPoints,
                    renderStrength,
                    () => renderVersion != _faceShapeSymmetryRenderVersion)
                : isJaw
                    ? BuildFaceShapeJawPreview(
                        safeProxy,
                        landmarkPoints,
                        renderStrength,
                        () => renderVersion != _faceShapeSymmetryRenderVersion)
                    : BuildFaceShapeChinPreview(
                        safeProxy,
                        landmarkPoints,
                        renderStrength,
                        () => renderVersion != _faceShapeSymmetryRenderVersion));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {strength:0}";
    }

    private async Task ApplyFaceShapeHeadPoseDragPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto || FaceShapeRetouchTab is null)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        bool isFaceTilt = FaceShapeRetouchTab.IsFaceTiltFaceShapeModeSelected;
        bool isFaceTurn = FaceShapeRetouchTab.IsFaceTurnFaceShapeModeSelected;
        bool isHeadTilt = FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected;
        if (!isFaceTilt && !isFaceTurn && !isHeadTilt)
        {
            return;
        }

        double sliderStrength = isFaceTilt
            ? Math.Clamp(Math.Round(FaceShapeRetouchTab.FaceTiltFaceShapeStrength), 0, 100)
            : isFaceTurn
                ? Math.Clamp(Math.Round(FaceShapeRetouchTab.FaceTurnFaceShapeStrength), 0, 100)
                : Math.Clamp(Math.Round(FaceShapeRetouchTab.HeadTiltFaceShapeStrength), 0, 100);
        if (Math.Abs(sliderStrength - 50) <= 0.001 && !isHeadTilt && !isFaceTurn && !isFaceTilt)
        {
            ClearFaceShapeHeadPoseDragPreview();
            return;
        }

        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = isFaceTilt
            ? GetFaceShapeFaceTiltRenderSource(targetPhoto)
            : isFaceTurn
                ? GetFaceShapeFaceTurnRenderSource(targetPhoto)
                : GetFaceShapeHeadTiltRenderSource(targetPhoto);
        BitmapSource proxySource = GetOrCreateFaceShapeHeadPoseDragProxy(targetPhoto, baseSource);

        string statusPrefix = isFaceTilt
            ? "Face F-Tilt"
            : isFaceTurn
                ? "Face Turn"
                : "Face Up/Dn";
        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(targetPhoto, statusPrefix);
        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        if (landmarks.Count == 0)
        {
            MediaPipeStatusText = $"{statusPrefix}: no landmarks";
            return;
        }

        HideMediaPipeFeatureOverlayForFaceShapeDebug();

        FaceShapePointArray landmarkPoints = GetOrCreateFaceShapePointArray(
            targetPhoto,
            landmarks,
            proxySource.PixelWidth,
            proxySource.PixelHeight);
        bool hasPlan;
        int projectedPointCount;
        List<FaceShapeControlPoint> reliefControls = [];
        List<Point> facePolygon = [];
        FaceShapeRigidMaskPlan? rigidMaskPlan = null;
        if (isFaceTilt)
        {
            hasPlan = TryBuildFaceShapeFaceTiltDragPreviewPlan(
                landmarkPoints,
                proxySource.PixelWidth,
                proxySource.PixelHeight,
                sliderStrength,
                out projectedPointCount,
                out reliefControls,
                out facePolygon);
        }
        else if (isFaceTurn)
        {
            hasPlan = TryBuildFaceShapeFaceTurnRigidMaskPlan(
                landmarkPoints,
                proxySource.PixelWidth,
                proxySource.PixelHeight,
                sliderStrength,
                out projectedPointCount,
                out rigidMaskPlan);
        }
        else
        {
            hasPlan = TryBuildFaceShapeHeadTiltRigidMaskPlan(
                landmarkPoints,
                proxySource.PixelWidth,
                proxySource.PixelHeight,
                100.0 - sliderStrength,
                out projectedPointCount,
                out rigidMaskPlan);
        }

        if (!hasPlan)
        {
            ClearFaceShapeHeadPoseDragPreview();
            MediaPipeStatusText = $"{statusPrefix}: preview points unavailable";
            return;
        }

        BitmapSource safeProxy = CloneBitmapSource(proxySource);
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {sliderStrength:0}...";
        BitmapSource preview = await Task.Run(() => rigidMaskPlan is not null
            ? BuildFaceShapeHeadTiltRigidMaskPreview(
                safeProxy,
                rigidMaskPlan,
                () => renderVersion != _faceShapeSymmetryRenderVersion)
            : BuildFaceShapeHeadTiltReliefPreview(
                safeProxy,
                reliefControls,
                facePolygon,
                () => renderVersion != _faceShapeSymmetryRenderVersion,
                outsideFeather: isFaceTilt ? FaceShapeFaceTiltOutsideFeatherPx : 0.0));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        SetFaceShapeHeadPoseDragPreview(targetPhoto, preview, baseSource.PixelWidth, baseSource.PixelHeight);
        ClearFaceShapeProjectionDebugOverlay();
        MediaPipeStatusText = $"{statusPrefix}: {FaceShapeHeadPoseDragPreviewLongSide:0} preview {projectedPointCount:0} pts | {sliderStrength:0}";
    }

    private void ClearFaceShapeSymmetrySession()
    {
        _faceShapeSymmetrySessionPhoto = null;
        _faceShapeSymmetrySessionBaseImage = null;
        _faceShapeUpperSessionPhoto = null;
        _faceShapeUpperSessionBaseImage = null;
        _faceShapeCheekSessionPhoto = null;
        _faceShapeCheekSessionBaseImage = null;
        _faceShapeBoneSessionPhoto = null;
        _faceShapeBoneSessionBaseImage = null;
        _faceShapeJawSessionPhoto = null;
        _faceShapeJawSessionBaseImage = null;
        _faceShapeChinSessionPhoto = null;
        _faceShapeChinSessionBaseImage = null;
        _faceShapeFaceTiltSessionPhoto = null;
        _faceShapeFaceTiltSessionBaseImage = null;
        _faceShapeFaceTurnSessionPhoto = null;
        _faceShapeFaceTurnSessionBaseImage = null;
        _faceShapeHeadTiltSessionPhoto = null;
        _faceShapeHeadTiltSessionBaseImage = null;
        ClearFaceShapeHeadPoseDragPreview();
        ClearFaceShapeHeadPoseDragProxy();
        Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        Interlocked.Increment(ref _faceShapeUpperPrewarmVersion);
    }

    private BitmapSource GetOrCreateFaceShapeHeadPoseDragProxy(PhotoItem targetPhoto, BitmapSource source)
    {
        if (_faceShapeHeadPoseDragProxySource is not null &&
            ReferenceEquals(_faceShapeHeadPoseDragProxyPhoto, targetPhoto) &&
            ReferenceEquals(_faceShapeHeadPoseDragProxyBaseSource, source) &&
            !string.IsNullOrWhiteSpace(_faceShapeHeadPoseDragProxyPath) &&
            string.Equals(_faceShapeHeadPoseDragProxyPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase))
        {
            return _faceShapeHeadPoseDragProxySource;
        }

        BitmapSource safeSource = source.IsFrozen ? source : CloneBitmapSource(source);
        int longSide = Math.Max(safeSource.PixelWidth, safeSource.PixelHeight);
        BitmapSource proxySource;
        if (longSide <= FaceShapeHeadPoseDragPreviewLongSide)
        {
            proxySource = safeSource;
        }
        else
        {
            double scale = FaceShapeHeadPoseDragPreviewLongSide / (double)longSide;
            TransformedBitmap transformed = new(safeSource, new ScaleTransform(scale, scale));
            transformed.Freeze();
            proxySource = CreateFaceShapeHeadPoseDragPreviewProxy(transformed);
        }

        _faceShapeHeadPoseDragProxyPhoto = targetPhoto;
        _faceShapeHeadPoseDragProxyPath = targetPhoto.Path;
        _faceShapeHeadPoseDragProxyBaseSource = source;
        _faceShapeHeadPoseDragProxySource = proxySource;
        return proxySource;
    }

    private void SetFaceShapeHeadPoseDragPreview(PhotoItem targetPhoto, BitmapSource preview, double frameWidth, double frameHeight)
    {
        _faceShapeHeadPoseDragPreviewPhoto = targetPhoto;
        _faceShapeHeadPoseDragPreviewPath = targetPhoto.Path;
        _faceShapeHeadPoseDragPreviewImageSource = preview;
        _faceShapeHeadPoseDragPreviewFrameWidth = frameWidth;
        _faceShapeHeadPoseDragPreviewFrameHeight = frameHeight;
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewImageFrame();
    }

    private void ClearFaceShapeHeadPoseDragPreview()
    {
        if (_faceShapeHeadPoseDragPreviewPhoto is null &&
            _faceShapeHeadPoseDragPreviewPath is null &&
            _faceShapeHeadPoseDragPreviewImageSource is null)
        {
            return;
        }

        _faceShapeHeadPoseDragPreviewPhoto = null;
        _faceShapeHeadPoseDragPreviewPath = null;
        _faceShapeHeadPoseDragPreviewImageSource = null;
        _faceShapeHeadPoseDragPreviewFrameWidth = 0;
        _faceShapeHeadPoseDragPreviewFrameHeight = 0;
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        UpdatePreviewImageFrame();
    }

    private void ClearFaceShapeHeadPoseDragProxy()
    {
        _faceShapeHeadPoseDragProxyPhoto = null;
        _faceShapeHeadPoseDragProxyPath = null;
        _faceShapeHeadPoseDragProxyBaseSource = null;
        _faceShapeHeadPoseDragProxySource = null;
    }

    private static BitmapSource CreateFaceShapeHeadPoseDragPreviewProxy(BitmapSource source)
    {
        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = bgraSource.PixelWidth * 4;
        byte[] pixels = new byte[stride * bgraSource.PixelHeight];
        bgraSource.CopyPixels(pixels, stride, 0);
        ApplyFaceShapeHeadPoseDragPreviewUnsharp(pixels, bgraSource.PixelWidth, bgraSource.PixelHeight, stride);

        WriteableBitmap proxy = new(
            bgraSource.PixelWidth,
            bgraSource.PixelHeight,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null);
        proxy.WritePixels(new Int32Rect(0, 0, bgraSource.PixelWidth, bgraSource.PixelHeight), pixels, stride, 0);
        proxy.Freeze();
        return proxy;
    }

    private static void ApplyFaceShapeHeadPoseDragPreviewUnsharp(byte[] pixels, int pixelWidth, int pixelHeight, int stride)
    {
        if (pixelWidth < 3 || pixelHeight < 3)
        {
            return;
        }

        byte[] sourcePixels = (byte[])pixels.Clone();
        for (int y = 1; y < pixelHeight - 1; y++)
        {
            for (int x = 1; x < pixelWidth - 1; x++)
            {
                int index = (y * stride) + (x * 4);
                for (int channel = 0; channel < 3; channel++)
                {
                    int blur =
                        sourcePixels[((y - 1) * stride) + ((x - 1) * 4) + channel] +
                        (sourcePixels[((y - 1) * stride) + (x * 4) + channel] * 2) +
                        sourcePixels[((y - 1) * stride) + ((x + 1) * 4) + channel] +
                        (sourcePixels[(y * stride) + ((x - 1) * 4) + channel] * 2) +
                        (sourcePixels[(y * stride) + (x * 4) + channel] * 4) +
                        (sourcePixels[(y * stride) + ((x + 1) * 4) + channel] * 2) +
                        sourcePixels[((y + 1) * stride) + ((x - 1) * 4) + channel] +
                        (sourcePixels[((y + 1) * stride) + (x * 4) + channel] * 2) +
                        sourcePixels[((y + 1) * stride) + ((x + 1) * 4) + channel];

                    double original = sourcePixels[index + channel];
                    double blurred = blur / 16.0;
                    double adjusted = original + ((original - blurred) * FaceShapeHeadPoseDragPreviewUnsharpAmount);
                    pixels[index + channel] = (byte)Math.Clamp((int)Math.Round(adjusted), 0, 255);
                }
            }
        }
    }

    private bool TryGetFaceShapeHeadPoseDragPreviewBitmapSource(PhotoItem photo, out BitmapSource preview)
    {
        if (_faceShapeHeadPoseDragPreviewImageSource is not null &&
            ReferenceEquals(_faceShapeHeadPoseDragPreviewPhoto, photo) &&
            !string.IsNullOrWhiteSpace(_faceShapeHeadPoseDragPreviewPath) &&
            string.Equals(_faceShapeHeadPoseDragPreviewPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            preview = _faceShapeHeadPoseDragPreviewImageSource;
            return true;
        }

        preview = null!;
        return false;
    }

    private bool TryGetFaceShapeHeadPoseDragPreviewFrameSize(PhotoItem photo, out double width, out double height)
    {
        if (_faceShapeHeadPoseDragPreviewImageSource is not null &&
            ReferenceEquals(_faceShapeHeadPoseDragPreviewPhoto, photo) &&
            _faceShapeHeadPoseDragPreviewFrameWidth > 0 &&
            _faceShapeHeadPoseDragPreviewFrameHeight > 0 &&
            !string.IsNullOrWhiteSpace(_faceShapeHeadPoseDragPreviewPath) &&
            string.Equals(_faceShapeHeadPoseDragPreviewPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            width = _faceShapeHeadPoseDragPreviewFrameWidth;
            height = _faceShapeHeadPoseDragPreviewFrameHeight;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }

    private void ClearFaceShapeLandmarkCache()
    {
        _faceShapeLandmarkCachePhoto = null;
        _faceShapeLandmarkCachePath = null;
        _faceShapeLandmarkCache = [];
        ClearFaceShapePointMapCache();
    }

    private void ClearFaceShapePointMapCache()
    {
        _faceShapePointMapCachePhoto = null;
        _faceShapePointMapCachePath = null;
        _faceShapePointMapCacheWidth = 0;
        _faceShapePointMapCacheHeight = 0;
        _faceShapePointMapCache = [];
        _faceShapePointArrayCachePhoto = null;
        _faceShapePointArrayCachePath = null;
        _faceShapePointArrayCacheWidth = 0;
        _faceShapePointArrayCacheHeight = 0;
        _faceShapePointArrayCache = FaceShapePointArray.Empty;
    }

    private bool TryGetFaceShapeLandmarkCache(PhotoItem targetPhoto, out List<MediaPipeLandmarkPoint> landmarks)
    {
        landmarks = [];
        if (_faceShapeLandmarkCache.Count == 0 ||
            !ReferenceEquals(_faceShapeLandmarkCachePhoto, targetPhoto) ||
            string.IsNullOrWhiteSpace(_faceShapeLandmarkCachePath) ||
            !string.Equals(_faceShapeLandmarkCachePath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        landmarks = _faceShapeLandmarkCache;
        return true;
    }

    private void StoreFaceShapeLandmarkCache(PhotoItem targetPhoto, IReadOnlyList<MediaPipeLandmarkPoint> landmarks)
    {
        _faceShapeLandmarkCachePhoto = targetPhoto;
        _faceShapeLandmarkCachePath = targetPhoto.Path;
        _faceShapeLandmarkCache = landmarks.ToList();
        ClearFaceShapePointMapCache();
    }

    private IReadOnlyDictionary<int, Point> GetOrCreateFaceShapePointMap(
        PhotoItem targetPhoto,
        IReadOnlyList<MediaPipeLandmarkPoint> landmarks,
        int width,
        int height)
    {
        if (_faceShapePointMapCache.Count > 0 &&
            ReferenceEquals(_faceShapePointMapCachePhoto, targetPhoto) &&
            !string.IsNullOrWhiteSpace(_faceShapePointMapCachePath) &&
            string.Equals(_faceShapePointMapCachePath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase) &&
            _faceShapePointMapCacheWidth == width &&
            _faceShapePointMapCacheHeight == height)
        {
            return _faceShapePointMapCache;
        }

        _faceShapePointMapCachePhoto = targetPhoto;
        _faceShapePointMapCachePath = targetPhoto.Path;
        _faceShapePointMapCacheWidth = width;
        _faceShapePointMapCacheHeight = height;
        _faceShapePointMapCache = BuildFaceShapePointMap(landmarks, width, height);
        return _faceShapePointMapCache;
    }

    private FaceShapePointArray GetOrCreateFaceShapePointArray(
        PhotoItem targetPhoto,
        IReadOnlyList<MediaPipeLandmarkPoint> landmarks,
        int width,
        int height)
    {
        if (_faceShapePointArrayCache.Count > 0 &&
            ReferenceEquals(_faceShapePointArrayCachePhoto, targetPhoto) &&
            !string.IsNullOrWhiteSpace(_faceShapePointArrayCachePath) &&
            string.Equals(_faceShapePointArrayCachePath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase) &&
            _faceShapePointArrayCacheWidth == width &&
            _faceShapePointArrayCacheHeight == height)
        {
            return _faceShapePointArrayCache;
        }

        _faceShapePointArrayCachePhoto = targetPhoto;
        _faceShapePointArrayCachePath = targetPhoto.Path;
        _faceShapePointArrayCacheWidth = width;
        _faceShapePointArrayCacheHeight = height;
        _faceShapePointArrayCache = BuildFaceShapePointArray(landmarks, width, height);
        return _faceShapePointArrayCache;
    }

    private BitmapSource GetFaceShapeSymmetryRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeSymmetrySessionPhoto, photo) &&
            _faceShapeSymmetrySessionBaseImage is not null)
        {
            return _faceShapeSymmetrySessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeSymmetry() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeSymmetrySessionPhoto = photo;
        _faceShapeSymmetrySessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeSymmetrySessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeSymmetry()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeSymmetryHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeSymmetryHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeSymmetryHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeSymmetryHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeSymmetry())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeSymmetryHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeSymmetryHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeUpperRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeUpperSessionPhoto, photo) &&
            _faceShapeUpperSessionBaseImage is not null)
        {
            return _faceShapeUpperSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeUpper() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeUpperSessionPhoto = photo;
        _faceShapeUpperSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeUpperSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeUpper()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeUpperHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeUpperHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeUpperHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeUpperHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeUpper())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeUpperHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeUpperHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeCheekRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeCheekSessionPhoto, photo) &&
            _faceShapeCheekSessionBaseImage is not null)
        {
            return _faceShapeCheekSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeCheek() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeCheekSessionPhoto = photo;
        _faceShapeCheekSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeCheekSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeCheek()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeCheekHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeCheekHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeCheekHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeCheekHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeCheek())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeCheekHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeCheekHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeBoneRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeBoneSessionPhoto, photo) &&
            _faceShapeBoneSessionBaseImage is not null)
        {
            return _faceShapeBoneSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeBone() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeBoneSessionPhoto = photo;
        _faceShapeBoneSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeBoneSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeBone()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeBoneHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeBoneHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeBoneHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeBoneHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeBone())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeBoneHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeBoneHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeJawRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeJawSessionPhoto, photo) &&
            _faceShapeJawSessionBaseImage is not null)
        {
            return _faceShapeJawSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeJaw() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeJawSessionPhoto = photo;
        _faceShapeJawSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeJawSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeJaw()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeJawHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeJawHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeJawHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeJawHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeJaw())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeJawHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeJawHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeChinRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeChinSessionPhoto, photo) &&
            _faceShapeChinSessionBaseImage is not null)
        {
            return _faceShapeChinSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeChin() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeChinSessionPhoto = photo;
        _faceShapeChinSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeChinSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeChin()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeChinHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeChinHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeChinHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeChinHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeChin())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeChinHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeChinHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeFaceTiltRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeFaceTiltSessionPhoto, photo) &&
            _faceShapeFaceTiltSessionBaseImage is not null)
        {
            return _faceShapeFaceTiltSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeFaceTilt() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeFaceTiltSessionPhoto = photo;
        _faceShapeFaceTiltSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeFaceTiltSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeFaceTilt()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeFaceTiltHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeFaceTiltHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeFaceTiltHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeFaceTiltHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeFaceTilt())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeFaceTiltHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeFaceTiltHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeFaceTurnRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeFaceTurnSessionPhoto, photo) &&
            _faceShapeFaceTurnSessionBaseImage is not null)
        {
            return _faceShapeFaceTurnSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeFaceTurn() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeFaceTurnSessionPhoto = photo;
        _faceShapeFaceTurnSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeFaceTurnSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeFaceTurn()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeFaceTurnHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeFaceTurnHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeFaceTurnHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeFaceTurnHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeFaceTurn())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeFaceTurnHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeFaceTurnHistoryTitle, detail);
    }

    private BitmapSource GetFaceShapeHeadTiltRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_faceShapeHeadTiltSessionPhoto, photo) &&
            _faceShapeHeadTiltSessionBaseImage is not null)
        {
            return _faceShapeHeadTiltSessionBaseImage;
        }

        BitmapSource source;
        if (IsCurrentHistoryFaceShapeHeadTilt() && _editorUndoHistory.Count >= 2)
        {
            EditorHistoryState previous = _editorUndoHistory[^2];
            source = previous.AdjustedImage ?? photo.BaseImage;
        }
        else
        {
            source = GetCurrentDisplayBitmapSource(photo);
        }

        _faceShapeHeadTiltSessionPhoto = photo;
        _faceShapeHeadTiltSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _faceShapeHeadTiltSessionBaseImage;
    }

    private bool IsCurrentHistoryFaceShapeHeadTilt()
    {
        return _editorUndoHistory.Count > 0 &&
               string.Equals(_editorUndoHistory[^1].Title, FaceShapeHeadTiltHistoryTitle, StringComparison.Ordinal) &&
               _editorUndoHistory[^1].Detail.StartsWith(FaceShapeHeadTiltHistoryDetail, StringComparison.Ordinal);
    }

    private void PushOrReplaceFaceShapeHeadTiltHistory(PhotoItem photo, double strength)
    {
        string detail = $"{FaceShapeHeadTiltHistoryDetail} {Math.Clamp(Math.Round(strength), 0, 100):0}";
        if (IsCurrentHistoryFaceShapeHeadTilt())
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, FaceShapeHeadTiltHistoryTitle, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(FaceShapeHeadTiltHistoryTitle, detail);
    }

    private async Task<List<MediaPipeLandmarkPoint>> GetOrCreateFaceShapeLandmarksAsync(
        PhotoItem targetPhoto,
        string statusPrefix)
    {
        if (TryGetFaceShapeLandmarkCache(targetPhoto, out List<MediaPipeLandmarkPoint> cachedLandmarks))
        {
            HideMediaPipeFeatureOverlayForFaceShapeDebug();
            return cachedLandmarks;
        }

        await _faceShapeLandmarkCacheGate.WaitAsync();
        try
        {
            if (TryGetFaceShapeLandmarkCache(targetPhoto, out cachedLandmarks))
            {
                HideMediaPipeFeatureOverlayForFaceShapeDebug();
                return cachedLandmarks;
            }

            if (_mediaPipeAllLandmarkPoints.Count > 0 &&
                !string.IsNullOrWhiteSpace(_mediaPipeOverlayPhotoPath) &&
                string.Equals(_mediaPipeOverlayPhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase))
            {
                StoreFaceShapeLandmarkCache(targetPhoto, _mediaPipeAllLandmarkPoints);
                HideMediaPipeFeatureOverlayForFaceShapeDebug();
                return _faceShapeLandmarkCache;
            }

            if (_isMediaPipeConnectionRunning)
            {
                MediaPipeStatusText = $"{statusPrefix}: MediaPipe busy";
                return [];
            }

            _isMediaPipeConnectionRunning = true;
            MediaPipeStatusText = $"{statusPrefix}: landmarks...";

            string outputDirectory = Path.Combine(
                MediaPipeOutputRoot,
                DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + "_faceshape");
            try
            {
                MediaPipeConnectionRunRequest request = new(
                    _appConfig.MediaPipe.HelperRuntime,
                    Path.Combine(AppContext.BaseDirectory, "Tools", "MediaPipe", "mediapipe_helper.py"),
                    Path.Combine(AppContext.BaseDirectory, "Assets", "AiModels", "MediaPipe"),
                    targetPhoto.Path,
                    outputDirectory);

                MediaPipeConnectionRunResult result = await MediaPipeConnectionService.RunAsync(
                    request,
                    CancellationToken.None);

                if (!ReferenceEquals(SelectedPhoto, targetPhoto))
                {
                    return [];
                }

                if (!result.Succeeded)
                {
                    MediaPipeStatusText = result.SummaryText;
                    return [];
                }

                HideMediaPipeFeatureOverlayForFaceShapeDebug();
                LoadMediaPipePreviewOverlay(outputDirectory, targetPhoto.Path);
                StoreFaceShapeLandmarkCache(targetPhoto, _mediaPipeAllLandmarkPoints);
                return _faceShapeLandmarkCache;
            }
            catch (Exception ex)
            {
                MediaPipeStatusText = $"{statusPrefix}: failed | " + ex.Message;
                return [];
            }
            finally
            {
                _isMediaPipeConnectionRunning = false;
            }
        }
        finally
        {
            _faceShapeLandmarkCacheGate.Release();
        }
    }

    private static double GetFaceShapeHeadTiltPitchRadians(double strength)
    {
        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        return -normalized * FaceShapeHeadTiltProjectionMaxDegrees * Math.PI / 180.0;
    }

    private static bool IsFaceShapeHeadTiltCanonicalIndex(int index)
    {
        return index >= 0 && index < FaceShapeHeadTiltCanonicalZ.Length;
    }

    private static double GetFaceShapeHeadTiltCanonicalDepth(int index, double faceWidth)
    {
        if (!IsFaceShapeHeadTiltCanonicalIndex(index))
        {
            return 0.0;
        }

        double range = Math.Max(0.0001, FaceShapeHeadTiltCanonicalNoseTipZ - FaceShapeHeadTiltCanonicalMinZ);
        double normalized = Math.Clamp(
            (FaceShapeHeadTiltCanonicalZ[index] - FaceShapeHeadTiltCanonicalMinZ) / range,
            0.0,
            1.0);
        return normalized * faceWidth * FaceShapeHeadTiltCanonicalDepthRatio;
    }

    private static double GetFaceShapeFaceTurnCanonicalDepth(int index, double faceWidth)
    {
        double depth = GetFaceShapeHeadTiltCanonicalDepth(index, faceWidth);
        if (ContainsFaceShapeIndex(FaceShapeHeadTiltFaceOvalIndices, index))
        {
            return depth * FaceShapeFaceTurnContourDepthRatio;
        }

        return depth;
    }

    private static bool TryBuildFaceShapeHeadTiltProjectionDebugPoints(
        IReadOnlyList<MediaPipeLandmarkPoint> landmarks,
        int width,
        int height,
        double strength,
        out List<Point> sourcePoints,
        out List<Point> projectedPoints,
        out List<IReadOnlyList<Point>> meshPaths,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        sourcePoints = [];
        projectedPoints = [];
        meshPaths = [];
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                width,
                height,
                out FaceShapeProjectionSample nose) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltChinTipIndices,
                width,
                height,
                out FaceShapeProjectionSample chin) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltLeftMouthCornerIndices,
                width,
                height,
                out FaceShapeProjectionSample leftMouth) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltRightMouthCornerIndices,
                width,
                height,
                out FaceShapeProjectionSample rightMouth))
        {
            return false;
        }

        List<Point> allPoints = new(landmarks.Count);
        foreach (MediaPipeLandmarkPoint landmark in landmarks)
        {
            allPoints.Add(new Point(landmark.X * width, landmark.Y * height));
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(allPoints, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double pitchRadians = GetFaceShapeHeadTiltPitchRadians(strength);
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = nose.X;
        double pivotY = nose.Y;
        double pivotZ = 0.0;
        double maskLiftZ = faceWidth * FaceShapeHeadTiltMaskLiftDepthRatio;
        Dictionary<int, Point> sourcePointMap = new(landmarks.Count);
        Dictionary<int, Point> projectedPointMap = new(landmarks.Count);

        foreach (MediaPipeLandmarkPoint landmark in landmarks)
        {
            if (!IsFaceShapeHeadTiltCanonicalIndex(landmark.Index))
            {
                continue;
            }

            FaceShapeProjectionSample sample = new(
                landmark.X * width,
                landmark.Y * height,
                landmark.Z);
            Point sourcePoint = new(sample.X, sample.Y);
            sourcePoints.Add(sourcePoint);
            double correctedZ = maskLiftZ + GetFaceShapeHeadTiltCanonicalDepth(landmark.Index, faceWidth);
            projectedPoints.Add(ProjectFaceShapeHeadTiltPoint(
                sample.X,
                sample.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                pitchRadians,
                focalLength,
                cameraDistance));
            sourcePointMap[landmark.Index] = sourcePoint;
            projectedPointMap[landmark.Index] = projectedPoints[^1];
        }

        meshPaths = BuildFaceShapeHeadTiltProjectedGuidePaths(projectedPointMap);
        reliefControls = BuildFaceShapeHeadTiltRigidReliefControls(sourcePointMap, projectedPointMap);
        facePolygon = BuildFaceShapeHeadTiltChinMaskPolygon(sourcePointMap, width, height);
        return sourcePoints.Count > 0 &&
               projectedPoints.Count == sourcePoints.Count &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static bool TryBuildFaceShapeFaceTurnProjectionDebugPoints(
        IReadOnlyList<MediaPipeLandmarkPoint> landmarks,
        int width,
        int height,
        double strength,
        out List<Point> sourcePoints,
        out List<Point> projectedPoints,
        out List<IReadOnlyList<Point>> meshPaths,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        sourcePoints = [];
        projectedPoints = [];
        meshPaths = [];
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                width,
                height,
                out FaceShapeProjectionSample turnPivot))
        {
            return false;
        }

        List<Point> allPoints = new(landmarks.Count);
        foreach (MediaPipeLandmarkPoint landmark in landmarks)
        {
            allPoints.Add(new Point(landmark.X * width, landmark.Y * height));
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(allPoints, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double yawRadians = normalized * FaceShapeFaceTurnProjectionMaxDegrees * Math.PI / 180.0;
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = turnPivot.X;
        double pivotY = turnPivot.Y;
        double pivotZ = 0.0;
        double maskLiftZ = 0.0;
        Dictionary<int, Point> sourcePointMap = new(landmarks.Count);
        Dictionary<int, Point> projectedPointMap = new(landmarks.Count);

        foreach (MediaPipeLandmarkPoint landmark in landmarks)
        {
            if (!IsFaceShapeHeadTiltCanonicalIndex(landmark.Index))
            {
                continue;
            }

            FaceShapeProjectionSample sample = new(
                landmark.X * width,
                landmark.Y * height,
                landmark.Z);
            Point sourcePoint = new(sample.X, sample.Y);
            sourcePoints.Add(sourcePoint);
            double correctedZ = maskLiftZ + GetFaceShapeFaceTurnCanonicalDepth(landmark.Index, faceWidth);

            projectedPoints.Add(ProjectFaceShapeFaceTurnPoint(
                sample.X,
                sample.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                yawRadians,
                focalLength,
                cameraDistance));
            sourcePointMap[landmark.Index] = sourcePoint;
            projectedPointMap[landmark.Index] = projectedPoints[^1];
        }

        meshPaths = BuildFaceShapeHeadTiltProjectedGuidePaths(projectedPointMap);
        reliefControls = BuildFaceShapeFaceTurnReliefControls(sourcePointMap, projectedPointMap, width, height);
        facePolygon = BuildFaceShapeHeadTiltFacePolygon(sourcePointMap);
        return sourcePoints.Count > 0 &&
               projectedPoints.Count == sourcePoints.Count &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static bool TryBuildFaceShapeFaceTiltReliefPoints(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out List<Point> sourcePoints,
        out List<Point> projectedPoints,
        out List<IReadOnlyList<Point>> meshPaths,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        sourcePoints = [];
        projectedPoints = [];
        meshPaths = [];
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count < 32 || width <= 0 || height <= 0)
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double centerX = bounds.Left + (bounds.Width * 0.5);
        double centerY = bounds.Top + (bounds.Height * 0.46);
        if (TryGetFaceShapeAveragePoint(landmarks, FaceShapeFaceTiltPivotIndices, out Point pivot))
        {
            centerX = pivot.X;
            centerY = pivot.Y;
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double angle = normalized * FaceShapeFaceTiltMaxDegrees * Math.PI / 180.0;
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        Dictionary<int, Point> sourcePointMap = new(landmarks.Count);
        Dictionary<int, Point> projectedPointMap = new(landmarks.Count);

        foreach ((int index, Point sourcePoint) in landmarks)
        {
            double dx = sourcePoint.X - centerX;
            double dy = sourcePoint.Y - centerY;
            Point projectedPoint = new(
                centerX + (dx * cos) - (dy * sin),
                centerY + (dx * sin) + (dy * cos));
            sourcePoints.Add(sourcePoint);
            projectedPoints.Add(projectedPoint);
            sourcePointMap[index] = sourcePoint;
            projectedPointMap[index] = projectedPoint;
        }

        meshPaths = BuildFaceShapeHeadTiltProjectedGuidePaths(projectedPointMap);
        reliefControls = BuildFaceShapeFaceTiltReliefControls(sourcePointMap, projectedPointMap);
        facePolygon = BuildFaceShapeHeadTiltFacePolygon(sourcePointMap);
        return sourcePoints.Count > 0 &&
               projectedPoints.Count == sourcePoints.Count &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static bool TryBuildFaceShapeHeadTiltDragPreviewPlan(
        FaceShapePointArray landmarks,
        int width,
        int height,
        double strength,
        out int projectedPointCount,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        projectedPointCount = 0;
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                out FaceShapeProjectionSample nose) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltChinTipIndices,
                out FaceShapeProjectionSample chin) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltLeftMouthCornerIndices,
                out FaceShapeProjectionSample leftMouth) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltRightMouthCornerIndices,
                out FaceShapeProjectionSample rightMouth))
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double pitchRadians = GetFaceShapeHeadTiltPitchRadians(strength);
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = nose.X;
        double pivotY = nose.Y;
        double pivotZ = 0.0;
        double maskLiftZ = faceWidth * FaceShapeHeadTiltMaskLiftDepthRatio;
        Point[] projectedPoints = new Point[landmarks.Points.Length];

        int canonicalPointCount = Math.Min(landmarks.Points.Length, FaceShapeHeadTiltCanonicalZ.Length);
        for (int index = 0; index < canonicalPointCount; index++)
        {
            if (index >= landmarks.HasPoint.Length || !landmarks.HasPoint[index])
            {
                continue;
            }

            Point sourcePoint = landmarks.Points[index];
            double correctedZ = maskLiftZ + GetFaceShapeHeadTiltCanonicalDepth(index, faceWidth);
            projectedPoints[index] = ProjectFaceShapeHeadTiltPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                pitchRadians,
                focalLength,
                cameraDistance);
            projectedPointCount++;
        }

        reliefControls = BuildFaceShapeHeadTiltRigidReliefControls(landmarks, projectedPoints);
        facePolygon = BuildFaceShapeHeadTiltChinMaskPolygon(landmarks, width, height);
        return projectedPointCount > 0 &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static bool TryBuildFaceShapeHeadTiltRigidMaskPlan(
        FaceShapePointArray landmarks,
        int width,
        int height,
        double strength,
        out int projectedPointCount,
        out FaceShapeRigidMaskPlan? plan)
    {
        projectedPointCount = 0;
        plan = null;
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                out FaceShapeProjectionSample nose) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltChinTipIndices,
                out FaceShapeProjectionSample chin) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltLeftMouthCornerIndices,
                out FaceShapeProjectionSample leftMouth) ||
            !TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltRightMouthCornerIndices,
                out FaceShapeProjectionSample rightMouth))
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        List<Point> sourcePolygon = BuildFaceShapeHeadTiltChinMaskPolygon(landmarks, width, height);
        if (sourcePolygon.Count < 12)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double pitchRadians = GetFaceShapeHeadTiltPitchRadians(strength);
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = nose.X;
        double pivotY = nose.Y;
        double pivotZ = 0.0;
        double maskLiftZ = faceWidth * FaceShapeHeadTiltMaskLiftDepthRatio;
        int canonicalPointCount = Math.Min(landmarks.Points.Length, FaceShapeHeadTiltCanonicalZ.Length);
        if (canonicalPointCount < FaceShapeHeadTiltCanonicalZ.Length)
        {
            return false;
        }

        List<Point> sourceVertices = new(canonicalPointCount + sourcePolygon.Count);
        List<Point> projectedVertices = new(canonicalPointCount + sourcePolygon.Count);
        List<Point> projectedPolygon = new(sourcePolygon.Count);

        Point[] projectedCanonicalPoints = new Point[canonicalPointCount];
        bool[] hasProjectedCanonicalPoint = new bool[canonicalPointCount];
        for (int index = 0; index < canonicalPointCount; index++)
        {
            if (index >= landmarks.HasPoint.Length || !landmarks.HasPoint[index])
            {
                return false;
            }

            Point sourcePoint = landmarks.Points[index];
            double correctedZ = maskLiftZ + GetFaceShapeHeadTiltCanonicalDepth(index, faceWidth);
            Point projectedPoint = ProjectFaceShapeHeadTiltPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                pitchRadians,
                focalLength,
                cameraDistance);
            sourceVertices.Add(sourcePoint);
            projectedVertices.Add(projectedPoint);
            projectedCanonicalPoints[index] = projectedPoint;
            hasProjectedCanonicalPoint[index] = true;
            projectedPointCount++;
        }

        foreach (Point sourcePoint in sourcePolygon)
        {
            double correctedZ = maskLiftZ;
            Point projectedPoint = ProjectFaceShapeHeadTiltPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                pitchRadians,
                focalLength,
                cameraDistance);
            AddFaceShapeRigidMaskVertex(sourceVertices, projectedVertices, sourcePoint, projectedPoint);
            projectedPolygon.Add(projectedPoint);
        }

        double verticalCompensationY = CalculateFaceShapeHeadTiltJawCompensationY(
            landmarks,
            projectedCanonicalPoints,
            hasProjectedCanonicalPoint,
            faceHeight);
        if (Math.Abs(verticalCompensationY) > 0.001)
        {
            OffsetFaceShapePointsY(projectedVertices, verticalCompensationY);
            OffsetFaceShapePointsY(projectedPolygon, verticalCompensationY);
        }

        List<FaceShapeRigidTriangle> triangles = BuildFaceShapeRigidMaskTriangles(
            sourceVertices,
            sourcePolygon,
            width,
            height);
        if (sourceVertices.Count < 12 || triangles.Count == 0)
        {
            return false;
        }

        plan = new FaceShapeRigidMaskPlan(
            sourceVertices.ToArray(),
            projectedVertices.ToArray(),
            triangles,
            sourcePolygon,
            projectedPolygon);
        return true;
    }

    private static bool TryBuildFaceShapeFaceTurnRigidMaskPlan(
        FaceShapePointArray landmarks,
        int width,
        int height,
        double strength,
        out int projectedPointCount,
        out FaceShapeRigidMaskPlan? plan)
    {
        projectedPointCount = 0;
        plan = null;
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                out FaceShapeProjectionSample turnPivot))
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        List<Point> sourcePolygon = BuildFaceShapeHeadTiltChinMaskPolygon(landmarks, width, height);
        if (sourcePolygon.Count < 12)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double yawRadians = normalized * FaceShapeFaceTurnProjectionMaxDegrees * Math.PI / 180.0;
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = turnPivot.X;
        double pivotY = turnPivot.Y;
        double pivotZ = 0.0;
        double maskLiftZ = 0.0;
        int canonicalPointCount = Math.Min(landmarks.Points.Length, FaceShapeHeadTiltCanonicalZ.Length);
        if (canonicalPointCount < FaceShapeHeadTiltCanonicalZ.Length)
        {
            return false;
        }

        List<Point> sourceVertices = new(canonicalPointCount + sourcePolygon.Count);
        List<Point> projectedVertices = new(canonicalPointCount + sourcePolygon.Count);
        List<Point> projectedPolygon = new(sourcePolygon.Count);

        for (int index = 0; index < canonicalPointCount; index++)
        {
            if (index >= landmarks.HasPoint.Length || !landmarks.HasPoint[index])
            {
                return false;
            }

            Point sourcePoint = landmarks.Points[index];
            double correctedZ = maskLiftZ + GetFaceShapeFaceTurnCanonicalDepth(index, faceWidth);
            Point projectedPoint = ProjectFaceShapeFaceTurnPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                yawRadians,
                focalLength,
                cameraDistance);
            sourceVertices.Add(sourcePoint);
            projectedVertices.Add(projectedPoint);
            projectedPointCount++;
        }

        foreach (Point sourcePoint in sourcePolygon)
        {
            double correctedZ = maskLiftZ;
            Point projectedPoint = ProjectFaceShapeFaceTurnPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                yawRadians,
                focalLength,
                cameraDistance);
            AddFaceShapeRigidMaskVertex(sourceVertices, projectedVertices, sourcePoint, projectedPoint);
            projectedPolygon.Add(projectedPoint);
        }

        List<FaceShapeRigidTriangle> triangles = BuildFaceShapeRigidMaskTriangles(
            sourceVertices,
            sourcePolygon,
            width,
            height);
        if (sourceVertices.Count < 12 || triangles.Count == 0)
        {
            return false;
        }

        plan = new FaceShapeRigidMaskPlan(
            sourceVertices.ToArray(),
            projectedVertices.ToArray(),
            triangles,
            sourcePolygon,
            projectedPolygon);
        return true;
    }

    private static bool TryBuildFaceShapeFaceTurnDragPreviewPlan(
        FaceShapePointArray landmarks,
        int width,
        int height,
        double strength,
        out int projectedPointCount,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        projectedPointCount = 0;
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count == 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (!TryGetFaceShapeProjectionAverage(
                landmarks,
                FaceShapeHeadTiltNoseTipIndices,
                out FaceShapeProjectionSample turnPivot))
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double faceWidth = Math.Max(1.0, bounds.Width);
        double faceHeight = Math.Max(1.0, bounds.Height);
        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double yawRadians = normalized * FaceShapeFaceTurnProjectionMaxDegrees * Math.PI / 180.0;
        double focalLength = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraFocalLengthRatio;
        double cameraDistance = Math.Max(faceWidth, faceHeight) * FaceShapeHeadPoseCameraDistanceRatio;
        double pivotX = turnPivot.X;
        double pivotY = turnPivot.Y;
        double pivotZ = 0.0;
        double maskLiftZ = 0.0;
        Point[] projectedPoints = new Point[landmarks.Points.Length];

        int canonicalPointCount = Math.Min(landmarks.Points.Length, FaceShapeHeadTiltCanonicalZ.Length);
        for (int index = 0; index < canonicalPointCount; index++)
        {
            if (index >= landmarks.HasPoint.Length || !landmarks.HasPoint[index])
            {
                continue;
            }

            Point sourcePoint = landmarks.Points[index];
            double correctedZ = maskLiftZ + GetFaceShapeFaceTurnCanonicalDepth(index, faceWidth);

            projectedPoints[index] = ProjectFaceShapeFaceTurnPoint(
                sourcePoint.X,
                sourcePoint.Y,
                correctedZ,
                pivotX,
                pivotY,
                pivotZ,
                yawRadians,
                focalLength,
                cameraDistance);
            projectedPointCount++;
        }

        reliefControls = BuildFaceShapeFaceTurnReliefControls(landmarks, projectedPoints, width, height);
        facePolygon = BuildFaceShapeHeadTiltFacePolygon(landmarks);
        return projectedPointCount > 0 &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static bool TryBuildFaceShapeFaceTiltDragPreviewPlan(
        FaceShapePointArray landmarks,
        int width,
        int height,
        double strength,
        out int projectedPointCount,
        out List<FaceShapeControlPoint> reliefControls,
        out List<Point> facePolygon)
    {
        projectedPointCount = 0;
        reliefControls = [];
        facePolygon = [];
        if (landmarks.Count < 32 || width <= 0 || height <= 0)
        {
            return false;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        double centerX = bounds.Left + (bounds.Width * 0.5);
        double centerY = bounds.Top + (bounds.Height * 0.46);
        if (TryGetFaceShapeAveragePoint(landmarks, FaceShapeFaceTiltPivotIndices, out Point pivot))
        {
            centerX = pivot.X;
            centerY = pivot.Y;
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double angle = normalized * FaceShapeFaceTiltMaxDegrees * Math.PI / 180.0;
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        Point[] projectedPoints = new Point[landmarks.Points.Length];

        for (int index = 0; index < landmarks.Points.Length; index++)
        {
            if (index >= landmarks.HasPoint.Length || !landmarks.HasPoint[index])
            {
                continue;
            }

            Point sourcePoint = landmarks.Points[index];
            double dx = sourcePoint.X - centerX;
            double dy = sourcePoint.Y - centerY;
            projectedPoints[index] = new Point(
                centerX + (dx * cos) - (dy * sin),
                centerY + (dx * sin) + (dy * cos));
            projectedPointCount++;
        }

        reliefControls = BuildFaceShapeFaceTiltReliefControls(landmarks, projectedPoints);
        facePolygon = BuildFaceShapeHeadTiltFacePolygon(landmarks);
        return projectedPointCount > 0 &&
               reliefControls.Count >= 8 &&
               facePolygon.Count >= 12;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeHeadTiltReliefControls(
        IReadOnlyDictionary<int, Point> sourcePointMap,
        IReadOnlyDictionary<int, Point> projectedPointMap)
    {
        HashSet<int> addedIndices = [];
        List<FaceShapeControlPoint> controls = [];

        void AddControl(int index, double strength)
        {
            if (!addedIndices.Add(index) ||
                !sourcePointMap.TryGetValue(index, out Point source) ||
                !projectedPointMap.TryGetValue(index, out Point projected))
            {
                return;
            }

            double dx = (projected.X - source.X) * strength;
            double dy = (projected.Y - source.Y) * strength;
            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            AddControl(index, 0.35);
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeHeadTiltRigidReliefControls(
        IReadOnlyDictionary<int, Point> sourcePointMap,
        IReadOnlyDictionary<int, Point> projectedPointMap)
    {
        List<FaceShapeControlPoint> controls = [];
        foreach ((int index, Point source) in sourcePointMap)
        {
            if (projectedPointMap.TryGetValue(index, out Point projected))
            {
                controls.Add(new FaceShapeControlPoint(
                    source.X,
                    source.Y,
                    projected.X - source.X,
                    projected.Y - source.Y));
            }
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeFaceTiltReliefControls(
        IReadOnlyDictionary<int, Point> sourcePointMap,
        IReadOnlyDictionary<int, Point> projectedPointMap)
    {
        HashSet<int> addedIndices = [];
        List<FaceShapeControlPoint> controls = [];

        void AddControl(int index, double strength)
        {
            if (!addedIndices.Add(index) ||
                !sourcePointMap.TryGetValue(index, out Point source) ||
                !projectedPointMap.TryGetValue(index, out Point projected))
            {
                return;
            }

            double dx = (projected.X - source.X) * strength;
            double dy = (projected.Y - source.Y) * strength;

            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            AddControl(index, 1.0);
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeFaceTurnReliefControls(
        IReadOnlyDictionary<int, Point> sourcePointMap,
        IReadOnlyDictionary<int, Point> projectedPointMap,
        int width,
        int height)
    {
        HashSet<int> addedIndices = [];
        HashSet<int> centralIndices =
        [
            1, 2, 4, 5, 6, 19, 94, 168, 195, 197
        ];
        HashSet<int> featureEdgeIndices =
        [
            33, 46, 61, 70, 78, 107, 133, 152, 172, 234,
            263, 276, 291, 300, 308, 336, 362, 397, 454
        ];
        HashSet<int> featherBoostIndices =
        [
            58, 93, 132, 136, 148, 149, 150, 172, 176, 234,
            288, 323, 361, 365, 377, 378, 379, 397, 400, 454
        ];
        List<FaceShapeControlPoint> controls = [];
        Rect bounds = BuildFaceShapeLandmarkBounds(sourcePointMap.Values, width, height);
        double centerX = bounds.IsEmpty ? width * 0.5 : bounds.Left + (bounds.Width * 0.5);
        double halfWidth = Math.Max(1.0, bounds.IsEmpty ? width * 0.25 : bounds.Width * 0.5);

        void AddControl(int index, double strength, bool protectOpeningEdge)
        {
            if (!addedIndices.Add(index) ||
                !sourcePointMap.TryGetValue(index, out Point source) ||
                !projectedPointMap.TryGetValue(index, out Point projected))
            {
                return;
            }

            double rawDx = projected.X - source.X;
            double rawDy = projected.Y - source.Y;
            double finalStrength = strength;
            if (protectOpeningEdge)
            {
                double side = Math.Clamp((source.X - centerX) / halfWidth, -1.0, 1.0);
                bool opensOuterEdge = (side < -0.42 && rawDx > 0) ||
                                      (side > 0.42 && rawDx < 0);
                if (opensOuterEdge)
                {
                    finalStrength = Math.Min(finalStrength, 0.58);
                }
            }

            double dx = rawDx * finalStrength;
            double dy = rawDy * finalStrength;
            if (Math.Abs(dx) < 0.005 && Math.Abs(dy) < 0.005)
            {
                return;
            }

            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            double strength = featherBoostIndices.Contains(index) ? 1.12 : 1.0;
            AddControl(index, strength, true);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            double strength = featherBoostIndices.Contains(index) ? 1.12 : 1.0;
            AddControl(index, strength, true);
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            double strength = centralIndices.Contains(index)
                ? 0.72
                : featureEdgeIndices.Contains(index)
                    ? 1.0
                    : 0.92;
            AddControl(index, strength, false);
        }

        return controls;
    }

    private static List<Point> BuildFaceShapeHeadTiltFacePolygon(
        IReadOnlyDictionary<int, Point> sourcePointMap)
    {
        List<Point> polygon = new(FaceShapeHeadTiltFaceOvalIndices.Length);
        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            if (!sourcePointMap.TryGetValue(index, out Point point))
            {
                return [];
            }

            polygon.Add(point);
        }

        return polygon;
    }

    private static List<Point> BuildFaceShapeHeadTiltChinMaskPolygon(
        IReadOnlyDictionary<int, Point> sourcePointMap,
        int width,
        int height)
    {
        return ExtendFaceShapeHeadTiltLowerMaskPolygon(
            BuildFaceShapeHeadTiltFacePolygon(sourcePointMap),
            width,
            height);
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeHeadTiltReliefControls(
        FaceShapePointArray sourcePointMap,
        Point[] projectedPointMap)
    {
        bool[] addedIndices = new bool[sourcePointMap.Points.Length];
        List<FaceShapeControlPoint> controls = [];

        void AddControl(int index, double strength)
        {
            if (index < 0 ||
                index >= addedIndices.Length ||
                index >= projectedPointMap.Length ||
                !sourcePointMap.HasPoint[index] ||
                addedIndices[index])
            {
                return;
            }

            addedIndices[index] = true;
            Point source = sourcePointMap.Points[index];
            Point projected = projectedPointMap[index];
            double dx = (projected.X - source.X) * strength;
            double dy = (projected.Y - source.Y) * strength;
            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            AddControl(index, 0.35);
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeHeadTiltRigidReliefControls(
        FaceShapePointArray sourcePointMap,
        Point[] projectedPointMap)
    {
        List<FaceShapeControlPoint> controls = [];
        int count = Math.Min(
            Math.Min(sourcePointMap.Points.Length, projectedPointMap.Length),
            FaceShapeHeadTiltCanonicalZ.Length);
        for (int index = 0; index < count; index++)
        {
            if (!sourcePointMap.HasPoint[index])
            {
                continue;
            }

            Point source = sourcePointMap.Points[index];
            Point projected = projectedPointMap[index];
            controls.Add(new FaceShapeControlPoint(
                source.X,
                source.Y,
                projected.X - source.X,
                projected.Y - source.Y));
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeFaceTiltReliefControls(
        FaceShapePointArray sourcePointMap,
        Point[] projectedPointMap)
    {
        bool[] addedIndices = new bool[sourcePointMap.Points.Length];
        List<FaceShapeControlPoint> controls = [];

        void AddControl(int index, double strength)
        {
            if (index < 0 ||
                index >= addedIndices.Length ||
                index >= projectedPointMap.Length ||
                !sourcePointMap.HasPoint[index] ||
                addedIndices[index])
            {
                return;
            }

            addedIndices[index] = true;
            Point source = sourcePointMap.Points[index];
            Point projected = projectedPointMap[index];
            double dx = (projected.X - source.X) * strength;
            double dy = (projected.Y - source.Y) * strength;

            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            AddControl(index, 1.0);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            AddControl(index, 1.0);
        }

        return controls;
    }

    private static List<FaceShapeControlPoint> BuildFaceShapeFaceTurnReliefControls(
        FaceShapePointArray sourcePointMap,
        Point[] projectedPointMap,
        int width,
        int height)
    {
        bool[] addedIndices = new bool[sourcePointMap.Points.Length];
        List<FaceShapeControlPoint> controls = [];
        Rect bounds = BuildFaceShapeLandmarkBounds(sourcePointMap, width, height);
        double centerX = bounds.IsEmpty ? width * 0.5 : bounds.Left + (bounds.Width * 0.5);
        double halfWidth = Math.Max(1.0, bounds.IsEmpty ? width * 0.25 : bounds.Width * 0.5);

        void AddControl(int index, double strength, bool protectOpeningEdge)
        {
            if (index < 0 ||
                index >= addedIndices.Length ||
                index >= projectedPointMap.Length ||
                !sourcePointMap.HasPoint[index] ||
                addedIndices[index])
            {
                return;
            }

            addedIndices[index] = true;
            Point source = sourcePointMap.Points[index];
            Point projected = projectedPointMap[index];
            double rawDx = projected.X - source.X;
            double rawDy = projected.Y - source.Y;
            double finalStrength = strength;
            if (protectOpeningEdge)
            {
                double side = Math.Clamp((source.X - centerX) / halfWidth, -1.0, 1.0);
                bool opensOuterEdge = (side < -0.42 && rawDx > 0) ||
                                      (side > 0.42 && rawDx < 0);
                if (opensOuterEdge)
                {
                    finalStrength = Math.Min(finalStrength, 0.58);
                }
            }

            double dx = rawDx * finalStrength;
            double dy = rawDy * finalStrength;
            if (Math.Abs(dx) < 0.005 && Math.Abs(dy) < 0.005)
            {
                return;
            }

            controls.Add(new FaceShapeControlPoint(source.X, source.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            double strength = ContainsFaceShapeIndex(FaceShapeFaceTurnFeatherBoostIndices, index) ? 1.12 : 1.0;
            AddControl(index, strength, true);
        }

        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            double strength = ContainsFaceShapeIndex(FaceShapeFaceTurnFeatherBoostIndices, index) ? 1.12 : 1.0;
            AddControl(index, strength, true);
        }

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            double strength = ContainsFaceShapeIndex(FaceShapeFaceTurnCentralIndices, index)
                ? 0.72
                : ContainsFaceShapeIndex(FaceShapeFaceTurnFeatureEdgeIndices, index)
                    ? 1.0
                    : 0.92;
            AddControl(index, strength, false);
        }

        return controls;
    }

    private static List<Point> BuildFaceShapeHeadTiltFacePolygon(FaceShapePointArray sourcePointMap)
    {
        List<Point> polygon = new(FaceShapeHeadTiltFaceOvalIndices.Length);
        foreach (int index in FaceShapeHeadTiltFaceOvalIndices)
        {
            if (!TryGetFaceShapePoint(sourcePointMap, index, out Point point))
            {
                return [];
            }

            polygon.Add(point);
        }

        return polygon;
    }

    private static List<Point> BuildFaceShapeHeadTiltChinMaskPolygon(
        FaceShapePointArray sourcePointMap,
        int width,
        int height)
    {
        return ExtendFaceShapeHeadTiltLowerMaskPolygon(
            BuildFaceShapeHeadTiltFacePolygon(sourcePointMap),
            width,
            height);
    }

    private static List<Point> ExtendFaceShapeHeadTiltLowerMaskPolygon(
        List<Point> polygon,
        int width,
        int height)
    {
        if (polygon.Count < 3)
        {
            return polygon;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(polygon, width, height);
        if (bounds.IsEmpty || bounds.Height < 2)
        {
            return polygon;
        }

        double centerY = bounds.Top + (bounds.Height * 0.5);
        double lowerRange = Math.Max(1.0, bounds.Bottom - centerY);
        double maxDrop = Math.Max(1.0, bounds.Height * FaceShapeHeadTiltLowerMaskExtensionRatio);
        List<Point> extended = new(polygon.Count);
        foreach (Point point in polygon)
        {
            if (point.Y <= centerY)
            {
                extended.Add(point);
                continue;
            }

            double lowerWeight = SmoothStep01((point.Y - centerY) / lowerRange);
            extended.Add(new Point(
                point.X,
                Math.Min(height - 1, point.Y + (maxDrop * lowerWeight))));
        }

        return extended;
    }

    private static List<IReadOnlyList<Point>> BuildFaceShapeHeadTiltProjectedGuidePaths(
        IReadOnlyDictionary<int, Point> projectedPointMap)
    {
        List<IReadOnlyList<Point>> paths = [];
        foreach ((int[] indices, bool closed) in FaceShapeHeadTiltFeaturePaths)
        {
            AddFaceShapeHeadTiltFeaturePath(paths, projectedPointMap, indices, closed);
        }

        return paths;
    }

    private static void AddFaceShapeHeadTiltFeaturePath(
        List<IReadOnlyList<Point>> paths,
        IReadOnlyDictionary<int, Point> projectedPointMap,
        IReadOnlyList<int> indices,
        bool closed)
    {
        List<Point> path = new(indices.Count + (closed ? 1 : 0));
        foreach (int index in indices)
        {
            if (!projectedPointMap.TryGetValue(index, out Point point))
            {
                return;
            }

            path.Add(point);
        }

        if (closed && path.Count > 2)
        {
            path.Add(path[0]);
        }

        if (path.Count > 1)
        {
            paths.Add(path);
        }
    }

    private static bool TryGetFaceShapeProjectionAverage(
        IReadOnlyList<MediaPipeLandmarkPoint> landmarks,
        IReadOnlyList<int> indices,
        int width,
        int height,
        out FaceShapeProjectionSample sample)
    {
        double sumX = 0;
        double sumY = 0;
        double sumZ = 0;
        int count = 0;
        foreach (int index in indices)
        {
            foreach (MediaPipeLandmarkPoint landmark in landmarks)
            {
                if (landmark.Index != index)
                {
                    continue;
                }

                sumX += landmark.X * width;
                sumY += landmark.Y * height;
                sumZ += landmark.Z;
                count++;
                break;
            }
        }

        if (count == 0)
        {
            sample = default;
            return false;
        }

        sample = new FaceShapeProjectionSample(sumX / count, sumY / count, sumZ / count);
        return true;
    }

    private static bool TryGetFaceShapeProjectionAverage(
        FaceShapePointArray landmarks,
        IReadOnlyList<int> indices,
        out FaceShapeProjectionSample sample)
    {
        double sumX = 0;
        double sumY = 0;
        double sumZ = 0;
        int count = 0;
        foreach (int index in indices)
        {
            if (index < 0 ||
                index >= landmarks.Points.Length ||
                index >= landmarks.ZValues.Length ||
                index >= landmarks.HasPoint.Length ||
                !landmarks.HasPoint[index])
            {
                continue;
            }

            Point point = landmarks.Points[index];
            sumX += point.X;
            sumY += point.Y;
            sumZ += landmarks.ZValues[index];
            count++;
        }

        if (count == 0)
        {
            sample = default;
            return false;
        }

        sample = new FaceShapeProjectionSample(sumX / count, sumY / count, sumZ / count);
        return true;
    }

    private static Point ProjectFaceShapeHeadTiltPoint(
        double imageX,
        double imageY,
        double correctedZ,
        double pivotX,
        double pivotY,
        double pivotZ,
        double pitchRadians,
        double focalLength,
        double cameraDistance)
    {
        double z = (correctedZ - pivotZ) * FaceShapeHeadTiltProjectionZScale;
        double depth = Math.Max(1.0, cameraDistance - z);
        double x3 = (imageX - pivotX) * depth / focalLength;
        double y3 = (imageY - pivotY) * depth / focalLength;
        double sin = Math.Sin(pitchRadians);
        double cos = Math.Cos(pitchRadians);
        double rotatedY = (y3 * cos) - (z * sin);
        double rotatedZ = (y3 * sin) + (z * cos);
        double projectedDepth = Math.Max(1.0, cameraDistance - rotatedZ);
        return new Point(
            pivotX + (x3 * focalLength / projectedDepth),
            pivotY + (rotatedY * focalLength / projectedDepth));
    }

    private static double CalculateFaceShapeHeadTiltJawCompensationY(
        FaceShapePointArray landmarks,
        IReadOnlyList<Point> projectedCanonicalPoints,
        IReadOnlyList<bool> hasProjectedCanonicalPoint,
        double faceHeight)
    {
        double sourceY = 0.0;
        double projectedY = 0.0;
        int count = 0;

        foreach (int index in FaceShapeHeadTiltJawCompensationIndices)
        {
            if (index < 0 ||
                index >= landmarks.Points.Length ||
                index >= landmarks.HasPoint.Length ||
                index >= projectedCanonicalPoints.Count ||
                index >= hasProjectedCanonicalPoint.Count ||
                !landmarks.HasPoint[index] ||
                !hasProjectedCanonicalPoint[index])
            {
                continue;
            }

            sourceY += landmarks.Points[index].Y;
            projectedY += projectedCanonicalPoints[index].Y;
            count++;
        }

        if (count < 3)
        {
            return 0.0;
        }

        double jawDeltaY = (projectedY / count) - (sourceY / count);
        double compensationY = jawDeltaY < 0.0
            ? -jawDeltaY * FaceShapeHeadTiltJawRiseCompensationStrength
            : -jawDeltaY * FaceShapeHeadTiltJawDropCompensationStrength;
        double maxCompensationY = Math.Max(2.0, faceHeight * 0.10);
        return Math.Clamp(compensationY, -maxCompensationY, maxCompensationY);
    }

    private static void OffsetFaceShapePointsY(List<Point> points, double offsetY)
    {
        for (int index = 0; index < points.Count; index++)
        {
            Point point = points[index];
            points[index] = new Point(point.X, point.Y + offsetY);
        }
    }

    private static Point ProjectFaceShapeFaceTurnPoint(
        double imageX,
        double imageY,
        double correctedZ,
        double pivotX,
        double pivotY,
        double pivotZ,
        double yawRadians,
        double focalLength,
        double cameraDistance)
    {
        _ = cameraDistance;

        double referenceLength = Math.Max(1.0, focalLength / FaceShapeHeadPoseCameraFocalLengthRatio);
        double maxDepth = Math.Max(1.0, referenceLength * FaceShapeHeadTiltCanonicalDepthRatio);
        double depthWeight = Math.Clamp((correctedZ - pivotZ) / maxDepth, 0.0, 1.0);
        depthWeight = SmoothStep01(depthWeight);
        double turnShiftX = Math.Sin(yawRadians) *
            referenceLength *
            FaceShapeFaceTurnMaxShiftRatio *
            depthWeight;
        double turnShiftY = (imageY - pivotY) *
            FaceShapeFaceTurnVerticalProjectionRatio *
            depthWeight *
            Math.Abs(Math.Sin(yawRadians));

        return new Point(
            imageX + turnShiftX,
            imageY + turnShiftY);
    }

    private static void AddFaceShapeRigidMaskVertex(
        List<Point> sourceVertices,
        List<Point> projectedVertices,
        Point sourcePoint,
        Point projectedPoint)
    {
        if (!IsFiniteFaceShapePoint(sourcePoint) || !IsFiniteFaceShapePoint(projectedPoint))
        {
            return;
        }

        for (int i = 0; i < sourceVertices.Count; i++)
        {
            Point existing = sourceVertices[i];
            double dx = existing.X - sourcePoint.X;
            double dy = existing.Y - sourcePoint.Y;
            if ((dx * dx) + (dy * dy) < 0.16)
            {
                return;
            }
        }

        sourceVertices.Add(sourcePoint);
        projectedVertices.Add(projectedPoint);
    }

    private static bool IsFiniteFaceShapePoint(Point point)
    {
        return !double.IsNaN(point.X) &&
               !double.IsNaN(point.Y) &&
               !double.IsInfinity(point.X) &&
               !double.IsInfinity(point.Y);
    }

    private static List<FaceShapeRigidTriangle> BuildFaceShapeRigidMaskTriangles(
        IReadOnlyList<Point> sourcePoints,
        IReadOnlyList<Point> facePolygon,
        int width,
        int height)
    {
        List<FaceShapeRigidTriangle> result = [];
        HashSet<FaceShapeRigidTriangle> added = [];
        if (sourcePoints.Count < 3 || facePolygon.Count < 3)
        {
            return result;
        }

        int canonicalPointCount = Math.Min(sourcePoints.Count, FaceShapeHeadTiltCanonicalZ.Length);
        bool useCanonicalTopology = canonicalPointCount == FaceShapeHeadTiltCanonicalZ.Length;
        if (useCanonicalTopology)
        {
            foreach (FaceShapeRigidTriangle triangle in FaceShapeHeadTiltCanonicalTriangles)
            {
                if (!IsFaceShapeRigidTriangleInsideMask(triangle, sourcePoints, facePolygon))
                {
                    continue;
                }

                AddFaceShapeRigidTriangle(result, added, triangle);
            }
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(sourcePoints, width, height);
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return result;
        }

        List<Point> points = new(sourcePoints.Count + 3);
        points.AddRange(sourcePoints);
        int originalCount = sourcePoints.Count;
        double spread = Math.Max(bounds.Width, bounds.Height);
        double margin = Math.Max(128.0, spread * 16.0);
        points.Add(new Point(bounds.Left - margin, bounds.Bottom + margin));
        points.Add(new Point(bounds.Left + (bounds.Width * 0.5), bounds.Top - margin));
        points.Add(new Point(bounds.Right + margin, bounds.Bottom + margin));

        int superA = originalCount;
        int superB = originalCount + 1;
        int superC = originalCount + 2;
        List<FaceShapeDelaunayTriangle> triangles = [];
        if (!TryCreateFaceShapeDelaunayTriangle(superA, superB, superC, points, out FaceShapeDelaunayTriangle superTriangle))
        {
            return result;
        }

        triangles.Add(superTriangle);
        for (int pointIndex = 0; pointIndex < originalCount; pointIndex++)
        {
            Point point = points[pointIndex];
            if (!IsFiniteFaceShapePoint(point))
            {
                continue;
            }

            List<FaceShapeDelaunayEdge> boundaryEdges = [];
            List<FaceShapeDelaunayTriangle> nextTriangles = new(triangles.Count + 8);
            foreach (FaceShapeDelaunayTriangle triangle in triangles)
            {
                if (IsPointInsideFaceShapeDelaunayCircumcircle(point, triangle))
                {
                    AddFaceShapeDelaunayBoundaryEdge(boundaryEdges, triangle.A, triangle.B);
                    AddFaceShapeDelaunayBoundaryEdge(boundaryEdges, triangle.B, triangle.C);
                    AddFaceShapeDelaunayBoundaryEdge(boundaryEdges, triangle.C, triangle.A);
                    continue;
                }

                nextTriangles.Add(triangle);
            }

            foreach (FaceShapeDelaunayEdge edge in boundaryEdges)
            {
                if (TryCreateFaceShapeDelaunayTriangle(edge.A, edge.B, pointIndex, points, out FaceShapeDelaunayTriangle triangle))
                {
                    nextTriangles.Add(triangle);
                }
            }

            triangles = nextTriangles;
        }

        foreach (FaceShapeDelaunayTriangle triangle in triangles)
        {
            if (triangle.A >= originalCount ||
                triangle.B >= originalCount ||
                triangle.C >= originalCount ||
                (useCanonicalTopology &&
                    triangle.A < canonicalPointCount &&
                    triangle.B < canonicalPointCount &&
                    triangle.C < canonicalPointCount) ||
                !IsFaceShapeRigidTriangleInsideMask(triangle, sourcePoints, facePolygon))
            {
                continue;
            }

            AddFaceShapeRigidTriangle(result, added, new FaceShapeRigidTriangle(triangle.A, triangle.B, triangle.C));
        }

        return result;
    }

    private static void AddFaceShapeRigidTriangle(
        List<FaceShapeRigidTriangle> triangles,
        HashSet<FaceShapeRigidTriangle> added,
        FaceShapeRigidTriangle triangle)
    {
        if (added.Add(triangle))
        {
            triangles.Add(triangle);
        }
    }

    private static bool TryCreateFaceShapeDelaunayTriangle(
        int a,
        int b,
        int c,
        IReadOnlyList<Point> points,
        out FaceShapeDelaunayTriangle triangle)
    {
        triangle = default;
        if (a == b || b == c || c == a)
        {
            return false;
        }

        Point pa = points[a];
        Point pb = points[b];
        Point pc = points[c];
        double area2 = ((pb.X - pa.X) * (pc.Y - pa.Y)) - ((pb.Y - pa.Y) * (pc.X - pa.X));
        if (Math.Abs(area2) < 0.0001)
        {
            return false;
        }

        double denominator = 2.0 *
            ((pa.X * (pb.Y - pc.Y)) +
             (pb.X * (pc.Y - pa.Y)) +
             (pc.X * (pa.Y - pb.Y)));
        if (Math.Abs(denominator) < 0.000001)
        {
            return false;
        }

        double pa2 = (pa.X * pa.X) + (pa.Y * pa.Y);
        double pb2 = (pb.X * pb.X) + (pb.Y * pb.Y);
        double pc2 = (pc.X * pc.X) + (pc.Y * pc.Y);
        double circumX =
            (pa2 * (pb.Y - pc.Y) +
             pb2 * (pc.Y - pa.Y) +
             pc2 * (pa.Y - pb.Y)) / denominator;
        double circumY =
            (pa2 * (pc.X - pb.X) +
             pb2 * (pa.X - pc.X) +
             pc2 * (pb.X - pa.X)) / denominator;
        double dx = circumX - pa.X;
        double dy = circumY - pa.Y;
        double radiusSquared = (dx * dx) + (dy * dy);
        if (double.IsNaN(radiusSquared) || double.IsInfinity(radiusSquared))
        {
            return false;
        }

        triangle = new FaceShapeDelaunayTriangle(a, b, c, circumX, circumY, radiusSquared);
        return true;
    }

    private static bool IsPointInsideFaceShapeDelaunayCircumcircle(
        Point point,
        FaceShapeDelaunayTriangle triangle)
    {
        double dx = point.X - triangle.CircumX;
        double dy = point.Y - triangle.CircumY;
        double distanceSquared = (dx * dx) + (dy * dy);
        return distanceSquared <= triangle.CircumRadiusSquared + 0.01;
    }

    private static void AddFaceShapeDelaunayBoundaryEdge(
        List<FaceShapeDelaunayEdge> edges,
        int a,
        int b)
    {
        if (a == b)
        {
            return;
        }

        if (a > b)
        {
            (a, b) = (b, a);
        }

        FaceShapeDelaunayEdge edge = new(a, b);
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].A == edge.A && edges[i].B == edge.B)
            {
                edges.RemoveAt(i);
                return;
            }
        }

        edges.Add(edge);
    }

    private static bool IsFaceShapeRigidTriangleInsideMask(
        FaceShapeDelaunayTriangle triangle,
        IReadOnlyList<Point> points,
        IReadOnlyList<Point> facePolygon)
    {
        return IsFaceShapeRigidTriangleInsideMask(triangle.A, triangle.B, triangle.C, points, facePolygon);
    }

    private static bool IsFaceShapeRigidTriangleInsideMask(
        FaceShapeRigidTriangle triangle,
        IReadOnlyList<Point> points,
        IReadOnlyList<Point> facePolygon)
    {
        return IsFaceShapeRigidTriangleInsideMask(triangle.A, triangle.B, triangle.C, points, facePolygon);
    }

    private static bool IsFaceShapeRigidTriangleInsideMask(
        int indexA,
        int indexB,
        int indexC,
        IReadOnlyList<Point> points,
        IReadOnlyList<Point> facePolygon)
    {
        if (indexA < 0 ||
            indexB < 0 ||
            indexC < 0 ||
            indexA >= points.Count ||
            indexB >= points.Count ||
            indexC >= points.Count)
        {
            return false;
        }

        Point a = points[indexA];
        Point b = points[indexB];
        Point c = points[indexC];
        if (!IsFiniteFaceShapePoint(a) ||
            !IsFiniteFaceShapePoint(b) ||
            !IsFiniteFaceShapePoint(c))
        {
            return false;
        }

        double centerX = (a.X + b.X + c.X) / 3.0;
        double centerY = (a.Y + b.Y + c.Y) / 3.0;
        return IsPointInsideFaceShapePolygon(centerX, centerY, facePolygon);
    }

    private static BitmapSource BuildFaceShapeHeadTiltRigidMaskPreview(
        BitmapSource source,
        FaceShapeRigidMaskPlan plan,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 ||
            height < 2 ||
            plan.SourcePoints.Length < 12 ||
            plan.SourcePoints.Length != plan.ProjectedPoints.Length ||
            plan.Triangles.Count == 0 ||
            plan.SourcePolygon.Count < 12)
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = width * 4;
        int bufferSize = stride * height;
        byte[] sourcePixels = ArrayPool<byte>.Shared.Rent(bufferSize);
        byte[] resultPixels = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            bgraSource.CopyPixels(sourcePixels, stride, 0);
            Buffer.BlockCopy(sourcePixels, 0, resultPixels, 0, bufferSize);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            RenderFaceShapeHeadTiltRigidMaskPixels(
                sourcePixels,
                resultPixels,
                width,
                height,
                stride,
                plan,
                shouldCancel);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            RenderFaceShapeHeadTiltAttachBandPixels(
                sourcePixels,
                resultPixels,
                width,
                height,
                stride,
                plan,
                shouldCancel);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            WriteableBitmap preview = new(
                width,
                height,
                bgraSource.DpiX,
                bgraSource.DpiY,
                PixelFormats.Bgra32,
                null);
            preview.WritePixels(new Int32Rect(0, 0, width, height), resultPixels, stride, 0);
            preview.Freeze();
            return preview;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sourcePixels);
            ArrayPool<byte>.Shared.Return(resultPixels);
        }
    }

    private static void RenderFaceShapeHeadTiltRigidMaskPixels(
        byte[] sourcePixels,
        byte[] resultPixels,
        int width,
        int height,
        int stride,
        FaceShapeRigidMaskPlan plan,
        Func<bool>? shouldCancel)
    {
        Rect sourcePolygonBounds = BuildFaceShapeLandmarkBounds(plan.SourcePolygon, width, height);
        double lowerFeatherStartY = sourcePolygonBounds.Bottom - (sourcePolygonBounds.Height * 0.18);

        for (int triangleIndex = 0; triangleIndex < plan.Triangles.Count; triangleIndex++)
        {
            if ((triangleIndex & 31) == 0 && shouldCancel?.Invoke() == true)
            {
                return;
            }

            FaceShapeRigidTriangle triangle = plan.Triangles[triangleIndex];
            if (!TryGetFaceShapeRigidTrianglePoints(
                    plan,
                    triangle,
                    out Point sourceA,
                    out Point sourceB,
                    out Point sourceC,
                    out Point targetA,
                    out Point targetB,
                    out Point targetC))
            {
                continue;
            }

            double denominator =
                ((targetB.Y - targetC.Y) * (targetA.X - targetC.X)) +
                ((targetC.X - targetB.X) * (targetA.Y - targetC.Y));
            if (Math.Abs(denominator) < 0.0001)
            {
                continue;
            }

            double minX = Math.Min(targetA.X, Math.Min(targetB.X, targetC.X));
            double maxX = Math.Max(targetA.X, Math.Max(targetB.X, targetC.X));
            double minY = Math.Min(targetA.Y, Math.Min(targetB.Y, targetC.Y));
            double maxY = Math.Max(targetA.Y, Math.Max(targetB.Y, targetC.Y));
            int left = Math.Max(0, (int)Math.Floor(minX) - 1);
            int top = Math.Max(0, (int)Math.Floor(minY) - 1);
            int right = Math.Min(width - 1, (int)Math.Ceiling(maxX) + 1);
            int bottom = Math.Min(height - 1, (int)Math.Ceiling(maxY) + 1);
            if (left > right || top > bottom)
            {
                continue;
            }

            for (int y = top; y <= bottom; y++)
            {
                int rowOffset = y * stride;
                double pixelY = y + 0.5;
                for (int x = left; x <= right; x++)
                {
                    double pixelX = x + 0.5;
                    double weightA =
                        (((targetB.Y - targetC.Y) * (pixelX - targetC.X)) +
                         ((targetC.X - targetB.X) * (pixelY - targetC.Y))) / denominator;
                    if (weightA < -0.0005 || weightA > 1.0005)
                    {
                        continue;
                    }

                    double weightB =
                        (((targetC.Y - targetA.Y) * (pixelX - targetC.X)) +
                         ((targetA.X - targetC.X) * (pixelY - targetC.Y))) / denominator;
                    if (weightB < -0.0005 || weightB > 1.0005)
                    {
                        continue;
                    }

                    double weightC = 1.0 - weightA - weightB;
                    if (weightC < -0.0005 || weightC > 1.0005)
                    {
                        continue;
                    }

                    double sourceX = (sourceA.X * weightA) + (sourceB.X * weightB) + (sourceC.X * weightC);
                    double sourceY = (sourceA.Y * weightA) + (sourceB.Y * weightB) + (sourceC.Y * weightC);
                    double edgeFeather = sourceY >= lowerFeatherStartY
                        ? FaceShapeHeadTiltLowerEdgeFeatherPx
                        : FaceShapeHeadTiltRigidEdgeFeatherPx;
                    double faceWeight = GetFaceShapePolygonFeatherWeight(
                        sourceX,
                        sourceY,
                        plan.SourcePolygon,
                        edgeFeather,
                        0.0);
                    if (faceWeight <= 0.001)
                    {
                        continue;
                    }

                    SampleBilinearBgra32(
                        sourcePixels,
                        width,
                        height,
                        stride,
                        sourceX,
                        sourceY,
                        out byte b,
                        out byte g,
                        out byte r,
                        out byte a);

                    int offset = rowOffset + (x * 4);
                    resultPixels[offset] = BlendFaceShapeByte(resultPixels[offset], b, faceWeight);
                    resultPixels[offset + 1] = BlendFaceShapeByte(resultPixels[offset + 1], g, faceWeight);
                    resultPixels[offset + 2] = BlendFaceShapeByte(resultPixels[offset + 2], r, faceWeight);
                    resultPixels[offset + 3] = BlendFaceShapeByte(resultPixels[offset + 3], a, faceWeight);
                }
            }
        }
    }

    private static void RenderFaceShapeHeadTiltAttachBandPixels(
        byte[] sourcePixels,
        byte[] resultPixels,
        int width,
        int height,
        int stride,
        FaceShapeRigidMaskPlan plan,
        Func<bool>? shouldCancel)
    {
        if (plan.SourcePolygon.Count < 3 ||
            plan.SourcePolygon.Count != plan.ProjectedPolygon.Count ||
            shouldCancel?.Invoke() == true)
        {
            return;
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(plan.SourcePolygon, width, height);
        GetFaceShapePolygonMaxDelta(plan.SourcePolygon, plan.ProjectedPolygon, out double maxDx, out double maxDy);
        bounds.Inflate(
            FaceShapeHeadTiltAttachBandOuterPx + maxDx + 2.0,
            FaceShapeHeadTiltAttachBandOuterPx + maxDy + 2.0);
        bounds.Intersect(new Rect(0, 0, width, height));

        int left = Math.Max(0, (int)Math.Floor(bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(bounds.Bottom));
        if (left > right || top > bottom)
        {
            return;
        }

        void RenderRow(int y)
        {
            int rowOffset = y * stride;
            for (int x = left; x <= right; x++)
            {
                double pixelX = x + 0.5;
                double pixelY = y + 0.5;
                if (IsPointInsideFaceShapePolygon(pixelX, pixelY, plan.ProjectedPolygon))
                {
                    continue;
                }

                if (!TryGetFaceShapeNearestPolygonDelta(
                        pixelX,
                        pixelY,
                        plan.SourcePolygon,
                        plan.ProjectedPolygon,
                        out double distance,
                        out double deltaX,
                        out double deltaY) ||
                    distance > FaceShapeHeadTiltAttachBandOuterPx)
                {
                    continue;
                }

                double bandWeight = distance <= FaceShapeHeadTiltAttachBandInnerPx
                    ? 1.0
                    : 1.0 - SmoothStep01(
                        (distance - FaceShapeHeadTiltAttachBandInnerPx) /
                        Math.Max(1.0, FaceShapeHeadTiltAttachBandOuterPx - FaceShapeHeadTiltAttachBandInnerPx));
                double attachWeight = bandWeight * FaceShapeHeadTiltAttachBandStrength;
                if (attachWeight <= 0.001)
                {
                    continue;
                }

                double appliedDx = deltaX * attachWeight;
                double appliedDy = deltaY * attachWeight;
                if (Math.Abs(appliedDx) < 0.01 && Math.Abs(appliedDy) < 0.01)
                {
                    continue;
                }

                SampleBilinearBgra32(
                    sourcePixels,
                    width,
                    height,
                    stride,
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = rowOffset + (x * 4);
                resultPixels[offset] = BlendFaceShapeByte(resultPixels[offset], b, attachWeight);
                resultPixels[offset + 1] = BlendFaceShapeByte(resultPixels[offset + 1], g, attachWeight);
                resultPixels[offset + 2] = BlendFaceShapeByte(resultPixels[offset + 2], r, attachWeight);
                resultPixels[offset + 3] = BlendFaceShapeByte(resultPixels[offset + 3], a, attachWeight);
            }
        }

        int pixelCount = (right - left + 1) * (bottom - top + 1);
        if (pixelCount < 20000)
        {
            for (int y = top; y <= bottom; y++)
            {
                if (shouldCancel?.Invoke() == true)
                {
                    return;
                }

                RenderRow(y);
            }

            return;
        }

        Parallel.For(
            top,
            bottom + 1,
            (y, state) =>
            {
                if (shouldCancel?.Invoke() == true)
                {
                    state.Stop();
                    return;
                }

                RenderRow(y);
            });
    }

    private static void GetFaceShapePolygonMaxDelta(
        IReadOnlyList<Point> sourcePolygon,
        IReadOnlyList<Point> projectedPolygon,
        out double maxDx,
        out double maxDy)
    {
        maxDx = 0;
        maxDy = 0;
        int count = Math.Min(sourcePolygon.Count, projectedPolygon.Count);
        for (int i = 0; i < count; i++)
        {
            maxDx = Math.Max(maxDx, Math.Abs(projectedPolygon[i].X - sourcePolygon[i].X));
            maxDy = Math.Max(maxDy, Math.Abs(projectedPolygon[i].Y - sourcePolygon[i].Y));
        }
    }

    private static bool TryGetFaceShapeNearestPolygonDelta(
        double x,
        double y,
        IReadOnlyList<Point> sourcePolygon,
        IReadOnlyList<Point> projectedPolygon,
        out double distance,
        out double deltaX,
        out double deltaY)
    {
        distance = 0;
        deltaX = 0;
        deltaY = 0;
        int count = Math.Min(sourcePolygon.Count, projectedPolygon.Count);
        if (count < 3)
        {
            return false;
        }

        double bestDistance2 = double.PositiveInfinity;
        double bestDeltaX = 0;
        double bestDeltaY = 0;
        for (int i = 0; i < count; i++)
        {
            Point sourceA = sourcePolygon[i];
            Point sourceB = sourcePolygon[(i + 1) % count];
            Point projectedA = projectedPolygon[i];
            Point projectedB = projectedPolygon[(i + 1) % count];
            double segmentX = sourceB.X - sourceA.X;
            double segmentY = sourceB.Y - sourceA.Y;
            double length2 = (segmentX * segmentX) + (segmentY * segmentY);
            double t = 0.0;
            if (length2 > 0.000001)
            {
                t = Math.Clamp((((x - sourceA.X) * segmentX) + ((y - sourceA.Y) * segmentY)) / length2, 0.0, 1.0);
            }

            double nearestX = sourceA.X + (segmentX * t);
            double nearestY = sourceA.Y + (segmentY * t);
            double dx = x - nearestX;
            double dy = y - nearestY;
            double distance2 = (dx * dx) + (dy * dy);
            if (distance2 >= bestDistance2)
            {
                continue;
            }

            double deltaAX = projectedA.X - sourceA.X;
            double deltaAY = projectedA.Y - sourceA.Y;
            double deltaBX = projectedB.X - sourceB.X;
            double deltaBY = projectedB.Y - sourceB.Y;
            bestDistance2 = distance2;
            bestDeltaX = deltaAX + ((deltaBX - deltaAX) * t);
            bestDeltaY = deltaAY + ((deltaBY - deltaAY) * t);
        }

        if (double.IsPositiveInfinity(bestDistance2))
        {
            return false;
        }

        distance = Math.Sqrt(bestDistance2);
        deltaX = bestDeltaX;
        deltaY = bestDeltaY;
        return true;
    }

    private static bool TryGetFaceShapeRigidTrianglePoints(
        FaceShapeRigidMaskPlan plan,
        FaceShapeRigidTriangle triangle,
        out Point sourceA,
        out Point sourceB,
        out Point sourceC,
        out Point targetA,
        out Point targetB,
        out Point targetC)
    {
        sourceA = default;
        sourceB = default;
        sourceC = default;
        targetA = default;
        targetB = default;
        targetC = default;
        if (triangle.A < 0 ||
            triangle.B < 0 ||
            triangle.C < 0 ||
            triangle.A >= plan.SourcePoints.Length ||
            triangle.B >= plan.SourcePoints.Length ||
            triangle.C >= plan.SourcePoints.Length ||
            triangle.A >= plan.ProjectedPoints.Length ||
            triangle.B >= plan.ProjectedPoints.Length ||
            triangle.C >= plan.ProjectedPoints.Length)
        {
            return false;
        }

        sourceA = plan.SourcePoints[triangle.A];
        sourceB = plan.SourcePoints[triangle.B];
        sourceC = plan.SourcePoints[triangle.C];
        targetA = plan.ProjectedPoints[triangle.A];
        targetB = plan.ProjectedPoints[triangle.B];
        targetC = plan.ProjectedPoints[triangle.C];
        return true;
    }

    private static BitmapSource BuildFaceShapeHeadTiltReliefPreview(
        BitmapSource source,
        List<FaceShapeControlPoint> controls,
        IReadOnlyList<Point> facePolygon,
        Func<bool>? shouldCancel = null,
        double outsideFeather = 0.0)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2 || controls.Count < 8 || facePolygon.Count < 12)
        {
            return CloneBitmapSource(bgraSource);
        }

        Rect bounds = BuildFaceShapeLandmarkBounds(facePolygon, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return CloneBitmapSource(bgraSource);
        }

        double feather = FaceShapeHeadTiltReliefFeatherPx;
        outsideFeather = Math.Max(0.0, outsideFeather);
        double featherMargin = Math.Max(feather, outsideFeather);
        bounds.Inflate(featherMargin + 2.0, featherMargin + 2.0);
        bounds.Intersect(new Rect(0, 0, width, height));

        int left = Math.Max(0, (int)Math.Floor(bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(bounds.Bottom));
        if (left > right || top > bottom)
        {
            return CloneBitmapSource(bgraSource);
        }

        int regionWidth = right - left + 1;
        int regionHeight = bottom - top + 1;
        int regionStride = regionWidth * 4;
        int regionBufferSize = regionStride * regionHeight;
        GetFaceShapeMaxControlOffset(controls, out double maxControlDx, out double maxControlDy);
        int sampleMarginX = Math.Max(3, (int)Math.Ceiling(maxControlDx) + 4);
        int sampleMarginY = Math.Max(3, (int)Math.Ceiling(maxControlDy) + 4);
        int sourceLeft = Math.Max(0, left - sampleMarginX);
        int sourceTop = Math.Max(0, top - sampleMarginY);
        int sourceRight = Math.Min(width - 1, right + sampleMarginX);
        int sourceBottom = Math.Min(height - 1, bottom + sampleMarginY);
        int sourceWidth = sourceRight - sourceLeft + 1;
        int sourceHeight = sourceBottom - sourceTop + 1;
        int sourceStride = sourceWidth * 4;
        int sourceBufferSize = sourceStride * sourceHeight;
        byte[] sourcePixels = ArrayPool<byte>.Shared.Rent(sourceBufferSize);
        byte[] regionPixels = ArrayPool<byte>.Shared.Rent(regionBufferSize);

        try
        {
            bgraSource.CopyPixels(
                new Int32Rect(sourceLeft, sourceTop, sourceWidth, sourceHeight),
                sourcePixels,
                sourceStride,
                0);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            for (int row = 0; row < regionHeight; row++)
            {
                Buffer.BlockCopy(
                    sourcePixels,
                    ((top + row - sourceTop) * sourceStride) + ((left - sourceLeft) * 4),
                    regionPixels,
                    row * regionStride,
                    regionStride);
            }

            double sigma = Math.Max(16.0, Math.Max(bounds.Width, bounds.Height) * FaceShapeHeadTiltReliefSigmaRatio);
            double sigma2 = sigma * sigma * 2.0;
            RenderFaceShapeHeadTiltReliefPixels(
                sourcePixels,
                regionPixels,
                sourceWidth,
                sourceHeight,
                sourceStride,
                regionStride,
                sourceLeft,
                sourceTop,
                left,
                top,
                right,
                bottom,
                controls,
                facePolygon,
                sigma2,
                feather,
                outsideFeather,
                shouldCancel);

            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            WriteableBitmap preview = new(bgraSource);
            preview.WritePixels(new Int32Rect(left, top, regionWidth, regionHeight), regionPixels, regionStride, 0);
            preview.Freeze();
            return preview;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sourcePixels);
            ArrayPool<byte>.Shared.Return(regionPixels);
        }
    }

    private static void RenderFaceShapeHeadTiltReliefPixels(
        byte[] sourcePixels,
        byte[] resultPixels,
        int width,
        int height,
        int stride,
        int resultStride,
        int sourceLeft,
        int sourceTop,
        int left,
        int top,
        int right,
        int bottom,
        List<FaceShapeControlPoint> controls,
        IReadOnlyList<Point> facePolygon,
        double sigma2,
        double feather,
        double outsideFeather,
        Func<bool>? shouldCancel = null)
    {
        if (left > right || top > bottom || controls.Count == 0 || facePolygon.Count < 3 || shouldCancel?.Invoke() == true)
        {
            return;
        }

        int controlCount = controls.Count;
        double cutoffDistance2 = sigma2 * 7.600902459542082;
        double invSigma2 = 1.0 / sigma2;

        void RenderRow(int y)
        {
            int rowOffset = (y - top) * resultStride;
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapePolygonFeatherWeight(x + 0.5, y + 0.5, facePolygon, feather, outsideFeather);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                for (int i = 0; i < controlCount; i++)
                {
                    FaceShapeControlPoint control = controls[i];
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    if (distance2 >= cutoffDistance2)
                    {
                        continue;
                    }

                    double weight = Math.Exp(-distance2 * invSigma2);
                    if (weight <= 0.0005)
                    {
                        continue;
                    }

                    weightedDx += control.Dx * weight;
                    weightedDy += control.Dy * weight;
                    totalWeight += weight;
                }

                if (totalWeight <= 0.0001)
                {
                    continue;
                }

                double appliedDx = weightedDx / totalWeight * faceWeight;
                double appliedDy = weightedDy / totalWeight * faceWeight;
                if (Math.Abs(appliedDx) < 0.01 && Math.Abs(appliedDy) < 0.01)
                {
                    continue;
                }

                SampleBilinearBgra32(
                    sourcePixels,
                    width,
                    height,
                    stride,
                    x - appliedDx - sourceLeft,
                    y - appliedDy - sourceTop,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = rowOffset + ((x - left) * 4);
                resultPixels[offset] = BlendFaceShapeByte(resultPixels[offset], b, faceWeight);
                resultPixels[offset + 1] = BlendFaceShapeByte(resultPixels[offset + 1], g, faceWeight);
                resultPixels[offset + 2] = BlendFaceShapeByte(resultPixels[offset + 2], r, faceWeight);
                resultPixels[offset + 3] = BlendFaceShapeByte(resultPixels[offset + 3], a, faceWeight);
            }
        }

        int pixelCount = (right - left + 1) * (bottom - top + 1);
        if (pixelCount < 20000)
        {
            for (int y = top; y <= bottom; y++)
            {
                if (shouldCancel?.Invoke() == true)
                {
                    return;
                }

                RenderRow(y);
            }

            return;
        }

        Parallel.For(
            top,
            bottom + 1,
            (y, state) =>
            {
                if (shouldCancel?.Invoke() == true)
                {
                    state.Stop();
                    return;
                }

                RenderRow(y);
            });
    }

    private static byte BlendFaceShapeByte(byte original, byte warped, double weight)
    {
        return (byte)Math.Clamp(
            (int)Math.Round(original + ((warped - original) * Math.Clamp(weight, 0.0, 1.0))),
            0,
            255);
    }

    private static double GetFaceShapePolygonFeatherWeight(
        double x,
        double y,
        IReadOnlyList<Point> polygon,
        double feather,
        double outsideFeather)
    {
        bool inside = IsPointInsideFaceShapePolygon(x, y, polygon);

        double distance2 = double.PositiveInfinity;
        for (int i = 0; i < polygon.Count; i++)
        {
            Point a = polygon[i];
            Point b = polygon[(i + 1) % polygon.Count];
            distance2 = Math.Min(distance2, GetDistanceSquaredToFaceShapeSegment(x, y, a, b));
        }

        double distance = Math.Sqrt(distance2);
        if (inside)
        {
            double innerWeight = SmoothStep01(distance / Math.Max(1.0, feather));
            return outsideFeather > 0.001
                ? Math.Max(innerWeight, FaceShapeHeadTiltOutsideFeatherStrength)
                : innerWeight;
        }

        if (outsideFeather <= 0.001)
        {
            return 0.0;
        }

        double outsideWeight = 1.0 - SmoothStep01(distance / Math.Max(1.0, outsideFeather));
        return outsideWeight * FaceShapeHeadTiltOutsideFeatherStrength;
    }

    private static bool IsPointInsideFaceShapePolygon(double x, double y, IReadOnlyList<Point> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Point pi = polygon[i];
            Point pj = polygon[j];
            double denominator = pj.Y - pi.Y;
            bool intersects = Math.Abs(denominator) > 0.000001 &&
                (pi.Y > y) != (pj.Y > y) &&
                x < ((pj.X - pi.X) * (y - pi.Y) / denominator) + pi.X;
            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double GetDistanceSquaredToFaceShapeSegment(double x, double y, Point a, Point b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double length2 = (dx * dx) + (dy * dy);
        if (length2 <= 0.000001)
        {
            double pointDx = x - a.X;
            double pointDy = y - a.Y;
            return (pointDx * pointDx) + (pointDy * pointDy);
        }

        double t = Math.Clamp((((x - a.X) * dx) + ((y - a.Y) * dy)) / length2, 0.0, 1.0);
        double projectionX = a.X + (t * dx);
        double projectionY = a.Y + (t * dy);
        double projectionDx = x - projectionX;
        double projectionDy = y - projectionY;
        return (projectionDx * projectionDx) + (projectionDy * projectionDy);
    }

    private static BitmapSource BuildFaceShapeSymmetryPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeSymmetryControls(landmarks, width, height, strength, out FaceShapeSymmetryPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(24.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.18);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeSymmetryWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeUpperPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] alphaPixels,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2 || alphaPixels.Length < width * height)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeUpperControls(landmarks, alphaPixels, width, height, strength, out FaceShapePosePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(24.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.16);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapePoseWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeCheekPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeCheekControls(landmarks, width, height, strength, out FaceShapeCheekPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, plan.Bounds.Width * 0.12);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeCheekWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeBonePreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeBoneControls(landmarks, width, height, strength, out FaceShapeBonePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(16.0, plan.Bounds.Width * 0.11);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeBoneWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeJawPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeJawControls(landmarks, width, height, strength, out FaceShapeJawPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, plan.Bounds.Width * 0.13);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeJawWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeChinPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeChinControls(landmarks, width, height, strength, out FaceShapeChinPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(14.0, plan.Bounds.Width * 0.12);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeChinWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeFaceTiltPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeFaceTiltControls(landmarks, width, height, strength, out FaceShapeFaceTiltPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.13);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapeFaceTiltWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeFaceTurnPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeFaceTurnControls(landmarks, width, height, strength, out FaceShapePosePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.14);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapePoseWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeHeadTiltPreview(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> landmarks,
        double strength,
        Func<bool>? shouldCancel = null)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeHeadTiltControls(landmarks, width, height, strength, out FaceShapePosePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.14);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Controls,
            sigma,
            CreateFaceShapePoseWeightProfile(plan),
            shouldCancel);
    }

    private static BitmapSource BuildFaceShapeControlWarpPreview(
        BitmapSource bgraSource,
        List<FaceShapeControlPoint> controls,
        double sigma,
        FaceShapeWeightProfile weightProfile,
        Func<bool>? shouldCancel = null)
    {
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        Rect bounds = weightProfile.Bounds;
        int left = Math.Max(0, (int)Math.Floor(bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(bounds.Bottom));
        if (left > right || top > bottom || controls.Count == 0)
        {
            return bgraSource;
        }

        int regionWidth = right - left + 1;
        int regionHeight = bottom - top + 1;
        int regionStride = regionWidth * 4;
        int regionBufferSize = regionStride * regionHeight;
        GetFaceShapeMaxControlOffset(controls, out double maxControlDx, out double maxControlDy);
        int sampleMarginX = Math.Max(2, (int)Math.Ceiling(maxControlDx) + 2);
        int sampleMarginY = Math.Max(2, (int)Math.Ceiling(maxControlDy) + 2);
        int sourceLeft = Math.Max(0, left - sampleMarginX);
        int sourceTop = Math.Max(0, top - sampleMarginY);
        int sourceRight = Math.Min(width - 1, right + sampleMarginX);
        int sourceBottom = Math.Min(height - 1, bottom + sampleMarginY);
        int sourceWidth = sourceRight - sourceLeft + 1;
        int sourceHeight = sourceBottom - sourceTop + 1;
        int sourceStride = sourceWidth * 4;
        int sourceBufferSize = sourceStride * sourceHeight;
        byte[] sourcePixels = ArrayPool<byte>.Shared.Rent(sourceBufferSize);
        byte[] regionPixels = ArrayPool<byte>.Shared.Rent(regionBufferSize);

        try
        {
            bgraSource.CopyPixels(
                new Int32Rect(sourceLeft, sourceTop, sourceWidth, sourceHeight),
                sourcePixels,
                sourceStride,
                0);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            for (int row = 0; row < regionHeight; row++)
            {
                Buffer.BlockCopy(
                    sourcePixels,
                    ((top + row - sourceTop) * sourceStride) + ((left - sourceLeft) * 4),
                    regionPixels,
                    row * regionStride,
                    regionStride);
            }

            double sigma2 = sigma * sigma * 2.0;
            RenderFaceShapeControlWarpPixels(
                sourcePixels,
                regionPixels,
                sourceWidth,
                sourceHeight,
                sourceStride,
                regionStride,
                sourceLeft,
                sourceTop,
                left,
                top,
                right,
                bottom,
                controls,
                sigma2,
                weightProfile,
                shouldCancel);
            if (shouldCancel?.Invoke() == true)
            {
                return bgraSource;
            }

            WriteableBitmap preview = new(
                bgraSource);
            preview.WritePixels(new Int32Rect(left, top, regionWidth, regionHeight), regionPixels, regionStride, 0);
            preview.Freeze();
            return preview;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(sourcePixels);
            ArrayPool<byte>.Shared.Return(regionPixels);
        }
    }

    private static void RenderFaceShapeControlWarpPixels(
        byte[] sourcePixels,
        byte[] resultPixels,
        int width,
        int height,
        int stride,
        int resultStride,
        int sourceLeft,
        int sourceTop,
        int left,
        int top,
        int right,
        int bottom,
        List<FaceShapeControlPoint> controls,
        double sigma2,
        FaceShapeWeightProfile weightProfile,
        Func<bool>? shouldCancel = null)
    {
        if (left > right || top > bottom || controls.Count == 0 || shouldCancel?.Invoke() == true)
        {
            return;
        }

        int controlCount = controls.Count;
        double cutoffDistance2 = sigma2 * 7.600902459542082;
        double invSigma2 = 1.0 / sigma2;

        void RenderRow(int y)
        {
            int rowOffset = (y - top) * resultStride;
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeWeight(x, y, in weightProfile);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                for (int i = 0; i < controlCount; i++)
                {
                    FaceShapeControlPoint control = controls[i];
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    if (distance2 >= cutoffDistance2)
                    {
                        continue;
                    }

                    double weight = Math.Exp(-distance2 * invSigma2);
                    if (weight <= 0.0005)
                    {
                        continue;
                    }

                    weightedDx += control.Dx * weight;
                    weightedDy += control.Dy * weight;
                    totalWeight += weight;
                }

                if (totalWeight <= 0.0001)
                {
                    continue;
                }

                double appliedDx = weightedDx / totalWeight * faceWeight;
                double appliedDy = weightedDy / totalWeight * faceWeight;
                if (Math.Abs(appliedDx) < 0.01 && Math.Abs(appliedDy) < 0.01)
                {
                    continue;
                }

                SampleBilinearBgra32(
                    sourcePixels,
                    width,
                    height,
                    stride,
                    x - appliedDx - sourceLeft,
                    y - appliedDy - sourceTop,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = rowOffset + ((x - left) * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        int pixelCount = (right - left + 1) * (bottom - top + 1);
        if (pixelCount < 20000)
        {
            for (int y = top; y <= bottom; y++)
            {
                if (shouldCancel?.Invoke() == true)
                {
                    return;
                }

                RenderRow(y);
            }

            return;
        }

        Parallel.For(
            top,
            bottom + 1,
            (y, state) =>
            {
                if (shouldCancel?.Invoke() == true)
                {
                    state.Stop();
                    return;
                }

                RenderRow(y);
            });
    }

    private static void GetFaceShapeMaxControlOffset(
        List<FaceShapeControlPoint> controls,
        out double maxDx,
        out double maxDy)
    {
        maxDx = 0;
        maxDy = 0;
        for (int i = 0; i < controls.Count; i++)
        {
            FaceShapeControlPoint control = controls[i];
            maxDx = Math.Max(maxDx, Math.Abs(control.Dx));
            maxDy = Math.Max(maxDy, Math.Abs(control.Dy));
        }
    }

    private static Dictionary<int, Point> BuildFaceShapePointMap(
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        int width,
        int height)
    {
        Dictionary<int, Point> points = new(normalizedLandmarks.Count);
        foreach (MediaPipeLandmarkPoint landmark in normalizedLandmarks)
        {
            if (landmark.Index < 0 ||
                double.IsNaN(landmark.X) ||
                double.IsNaN(landmark.Y))
            {
                continue;
            }

            points[landmark.Index] = new Point(
                Math.Clamp(landmark.X * width, 0, Math.Max(0, width - 1)),
                Math.Clamp(landmark.Y * height, 0, Math.Max(0, height - 1)));
        }

        return points;
    }

    private static FaceShapePointArray BuildFaceShapePointArray(
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        int width,
        int height)
    {
        int length = 478;
        foreach (MediaPipeLandmarkPoint landmark in normalizedLandmarks)
        {
            if (landmark.Index >= length)
            {
                length = landmark.Index + 1;
            }
        }

        Point[] points = new Point[length];
        double[] zValues = new double[length];
        bool[] hasPoint = new bool[length];
        int count = 0;
        foreach (MediaPipeLandmarkPoint landmark in normalizedLandmarks)
        {
            if (landmark.Index < 0 ||
                landmark.Index >= length ||
                double.IsNaN(landmark.X) ||
                double.IsNaN(landmark.Y))
            {
                continue;
            }

            if (!hasPoint[landmark.Index])
            {
                count++;
            }

            hasPoint[landmark.Index] = true;
            points[landmark.Index] = new Point(
                Math.Clamp(landmark.X * width, 0, Math.Max(0, width - 1)),
                Math.Clamp(landmark.Y * height, 0, Math.Max(0, height - 1)));
            zValues[landmark.Index] = double.IsNaN(landmark.Z) ? 0 : landmark.Z;
        }

        return new FaceShapePointArray(points, zValues, hasPoint, count);
    }

    private static bool TryGetFaceShapePoint(FaceShapePointArray landmarks, int index, out Point point)
    {
        if (index >= 0 &&
            index < landmarks.Points.Length &&
            index < landmarks.HasPoint.Length &&
            landmarks.HasPoint[index])
        {
            point = landmarks.Points[index];
            return true;
        }

        point = default;
        return false;
    }

    private static bool ContainsFaceShapeIndex(IReadOnlyList<int> indices, int index)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            if (indices[i] == index)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryBuildFaceShapeSymmetryControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeSymmetryPlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!landmarks.TryGetValue(168, out Point bridge) ||
            !landmarks.TryGetValue(4, out Point nose) ||
            !landmarks.TryGetValue(152, out Point chin))
        {
            return false;
        }

        double centerX = (bridge.X + nose.X + chin.X) / 3.0;
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            FaceShapeSymmetryMaxCorrection *
            1.45;
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas = [];

        void AddDelta(int index, double dx, double dy)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                return;
            }

            if (controlDeltas.TryGetValue(index, out (Point Point, double Dx, double Dy) existing))
            {
                controlDeltas[index] = (existing.Point, existing.Dx + dx, existing.Dy + dy);
                return;
            }

            controlDeltas[index] = (point, dx, dy);
        }

        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (!landmarks.TryGetValue(leftIndex, out Point left) ||
                !landmarks.TryGetValue(rightIndex, out Point right))
            {
                continue;
            }

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            double leftDistance = centerX - left.X;
            double rightDistance = right.X - centerX;
            if (leftDistance <= 1 || rightDistance <= 1)
            {
                continue;
            }

            double balancedDistance = (leftDistance + rightDistance) * 0.5;
            AddDelta(leftIndex, ((centerX - balancedDistance) - left.X) * amount, 0);
            AddDelta(rightIndex, ((centerX + balancedDistance) - right.X) * amount, 0);
        }

        int[] noseMidline =
        [
            168, 6, 197, 195, 5, 4, 1, 2
        ];
        foreach (int index in noseMidline)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            AddDelta(index, (centerX - point.X) * amount, 0);
        }

        AddFaceShapeHorizontalTiltDeltas(
            landmarks,
            controlDeltas,
            [33, 133, 159, 145, 468, 130, 246, 161, 160, 158, 157, 173, 153, 144, 163, 7],
            [263, 362, 386, 374, 473, 359, 466, 388, 387, 385, 384, 398, 380, 373, 390, 249],
            33,
            263,
            amount);
        AddFaceShapeHorizontalTiltDeltas(
            landmarks,
            controlDeltas,
            [46, 53, 52, 65, 55, 70, 63, 105, 66, 107],
            [276, 283, 282, 295, 285, 300, 293, 334, 296, 336],
            46,
            276,
            amount);
        AddFaceShapeHorizontalTiltDeltas(
            landmarks,
            controlDeltas,
            [61, 146, 91, 181, 84, 37, 39, 40],
            [291, 375, 321, 405, 314, 267, 269, 270],
            61,
            291,
            amount);
        AddFaceShapeHorizontalTiltDeltas(
            landmarks,
            controlDeltas,
            [58, 172, 136, 150, 149, 176, 148],
            [288, 397, 365, 379, 378, 400, 377],
            136,
            365,
            amount * 0.55);
        AddFaceShapeHorizontalTiltDeltas(
            landmarks,
            controlDeltas,
            [67, 109],
            [297, 338],
            109,
            338,
            amount * 0.22);

        AddFaceShapeForeheadBalanceDeltas(landmarks, controlDeltas, centerX, amount);

        int[] anchorIndices =
        [
            1, 4, 5, 6, 33, 263, 61, 291, 152
        ];
        foreach (int index in anchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controlDeltas[index] = (point, 0, 0);
        }

        List<FaceShapeControlPoint> controls = new(controlDeltas.Count);
        double upperFollowDx = GetFaceShapeAverageDeltaX(controlDeltas, bounds.Top + (bounds.Height * 0.55));
        double upperFollowDy = GetFaceShapeAverageDeltaY(controlDeltas, bounds.Top + (bounds.Height * 0.55));
        foreach ((Point point, double dx, double dy) in controlDeltas.Values)
        {
            controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, dy));
        }
        AddFaceShapeHairBalanceControls(controls, bounds, centerX, width, height, amount, upperFollowDx, upperFollowDy);

        if (controls.Count < 8)
        {
            return false;
        }

        Rect controlBounds = BuildFaceShapeLandmarkBounds(
            controls.Select(control => new Point(control.X, control.Y)),
            width,
            height);
        if (!controlBounds.IsEmpty)
        {
            bounds.Union(controlBounds);
        }

        double expansionX = bounds.Width * 0.18;
        double expansionTop = bounds.Height * 0.30;
        double expansionBottom = bounds.Height * 0.18;
        bounds = new Rect(
            bounds.Left - expansionX,
            bounds.Top - expansionTop,
            bounds.Width + (expansionX * 2.0),
            bounds.Height + expansionTop + expansionBottom);
        bounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeSymmetryPlan(bounds, centerX, controls);
        return true;
    }

    private static bool TryBuildFaceShapeUpperControls(
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] alphaPixels,
        int width,
        int height,
        double strength,
        out FaceShapePosePlan plan)
    {
        plan = default;
        Rect faceBounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (faceBounds.Width < 20 || faceBounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX))
        {
            return false;
        }

        if (!landmarks.TryGetValue(152, out Point chin))
        {
            return false;
        }

        double faceWidth = faceBounds.Width;
        double faceHeight = faceBounds.Height;
        if (!TryFindFaceShapeUpperShoulderLineFromAlpha(
            alphaPixels,
            width,
            height,
            centerX,
            chin,
            faceBounds,
            out FaceShapeUpperShoulderLine shoulderLine))
        {
            return false;
        }

        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) * 1.25;
        Point leftShoulder = shoulderLine.Left;
        Point rightShoulder = shoulderLine.Right;
        double targetY = (leftShoulder.Y + rightShoulder.Y) * 0.5;
        double leftDy = (targetY - leftShoulder.Y) * amount;
        double rightDy = (targetY - rightShoulder.Y) * amount;
        bool hasShoulderLeveling = Math.Abs(leftDy) >= 0.05 || Math.Abs(rightDy) >= 0.05;

        double shoulderBottomY = Math.Min(height - 1, Math.Max(leftShoulder.Y, rightShoulder.Y) + (faceHeight * 0.85));
        List<FaceShapeControlPoint> controls =
        [
            new(leftShoulder.X, leftShoulder.Y, 0, leftDy),
            new(leftShoulder.X, shoulderBottomY, 0, leftDy * 0.82),
            new(shoulderLine.NeckLeft.X, shoulderLine.NeckLeft.Y, 0, leftDy * 0.35),
            new(rightShoulder.X, rightShoulder.Y, 0, rightDy),
            new(rightShoulder.X, shoulderBottomY, 0, rightDy * 0.82),
            new(shoulderLine.NeckRight.X, shoulderLine.NeckRight.Y, 0, rightDy * 0.35)
        ];

        AddFaceShapeUpperHeadBlockControls(
            controls,
            landmarks,
            alphaPixels,
            width,
            height,
            centerX,
            chin,
            faceBounds,
            amount,
            out Rect headBounds,
            out double maxHeadOffset);

        if (!hasShoulderLeveling && maxHeadOffset < 0.05)
        {
            return false;
        }

        Rect bounds = new(
            Math.Max(0, Math.Min(leftShoulder.X, rightShoulder.X) - (faceWidth * 0.35)),
            Math.Max(0, shoulderLine.NeckY - (faceHeight * 0.15)),
            Math.Min(width, Math.Abs(rightShoulder.X - leftShoulder.X) + (faceWidth * 0.70)),
            Math.Min(height, shoulderBottomY - (shoulderLine.NeckY - (faceHeight * 0.15)) + (faceHeight * 0.20)));
        if (!headBounds.IsEmpty)
        {
            bounds.Union(headBounds);
        }

        bounds.Intersect(new Rect(0, 0, width, height));
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        plan = new FaceShapePosePlan(bounds, centerX, bounds.Top + (bounds.Height * 0.55), controls);
        return true;
    }

    private static void AddFaceShapeUpperHeadBlockControls(
        List<FaceShapeControlPoint> controls,
        IReadOnlyDictionary<int, Point> landmarks,
        byte[] alphaPixels,
        int width,
        int height,
        double centerX,
        Point chin,
        Rect faceBounds,
        double amount,
        out Rect bounds,
        out double maxOffset)
    {
        bounds = Rect.Empty;
        maxOffset = 0;
        double faceWidth = faceBounds.Width;
        double faceHeight = faceBounds.Height;
        int top = Math.Clamp((int)Math.Round(faceBounds.Top - (faceHeight * 0.65)), 0, Math.Max(0, height - 1));
        int bottom = Math.Clamp((int)Math.Round(chin.Y + (faceHeight * 0.14)), 0, Math.Max(0, height - 1));
        int leftLimit = Math.Clamp((int)Math.Round(centerX - (faceWidth * 1.45)), 0, Math.Max(0, width - 1));
        int rightLimit = Math.Clamp((int)Math.Round(centerX + (faceWidth * 1.45)), 0, Math.Max(0, width - 1));
        if (leftLimit >= rightLimit || top >= bottom)
        {
            return;
        }

        List<FaceShapeAlphaContourRow> rows = [];
        for (int y = top; y <= bottom; y++)
        {
            if (!TryGetFaceShapeAlphaContourRow(alphaPixels, width, y, leftLimit, rightLimit, out FaceShapeAlphaContourRow row))
            {
                continue;
            }

            if (row.Width < faceWidth * 0.28)
            {
                continue;
            }

            rows.Add(row);
        }

        if (rows.Count < 6)
        {
            return;
        }

        foreach (FaceShapeAlphaContourRow row in rows)
        {
            Rect rowBounds = new(row.Left, row.Y, Math.Max(1, row.Right - row.Left), 1);
            if (bounds.IsEmpty)
            {
                bounds = rowBounds;
            }
            else
            {
                bounds.Union(rowBounds);
            }
        }

        if (bounds.IsEmpty)
        {
            return;
        }

        double headCenterX = bounds.Left + (bounds.Width * 0.5);
        double centerShift = (centerX - headCenterX) * amount * 0.28;
        double correctionRadians = 0;
        if (landmarks.TryGetValue(33, out Point leftEye) &&
            landmarks.TryGetValue(263, out Point rightEye) &&
            Math.Abs(rightEye.X - leftEye.X) > 1)
        {
            double currentRadians = Math.Atan2(rightEye.Y - leftEye.Y, rightEye.X - leftEye.X);
            double maxRadians = 7.0 * Math.PI / 180.0;
            correctionRadians = Math.Clamp(-currentRadians * amount, -maxRadians, maxRadians);
        }

        double pivotX = centerX;
        double pivotY = chin.Y + (faceHeight * 0.08);
        double[][] blockPoints =
        [
            [bounds.Left, bounds.Top],
            [bounds.Left + (bounds.Width * 0.5), bounds.Top],
            [bounds.Right, bounds.Top],
            [bounds.Left, bounds.Top + (bounds.Height * 0.45)],
            [bounds.Right, bounds.Top + (bounds.Height * 0.45)],
            [bounds.Left, bounds.Bottom],
            [bounds.Left + (bounds.Width * 0.5), bounds.Bottom],
            [bounds.Right, bounds.Bottom]
        ];

        foreach (double[] blockPoint in blockPoints)
        {
            double x = Math.Clamp(blockPoint[0], 0, Math.Max(0, width - 1));
            double y = Math.Clamp(blockPoint[1], 0, Math.Max(0, height - 1));
            RotateFaceShapePoint(x, y, pivotX, pivotY, correctionRadians, out double rotatedX, out double rotatedY);
            double dx = (rotatedX - x) + centerShift;
            double dy = rotatedY - y;
            controls.Add(new FaceShapeControlPoint(x, y, dx, dy));
            maxOffset = Math.Max(maxOffset, Math.Max(Math.Abs(dx), Math.Abs(dy)));
        }

        bounds.Inflate(faceWidth * 0.18, faceHeight * 0.10);
    }

    private static void RotateFaceShapePoint(
        double x,
        double y,
        double pivotX,
        double pivotY,
        double radians,
        out double rotatedX,
        out double rotatedY)
    {
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double offsetX = x - pivotX;
        double offsetY = y - pivotY;
        rotatedX = pivotX + (offsetX * cos) - (offsetY * sin);
        rotatedY = pivotY + (offsetX * sin) + (offsetY * cos);
    }

    private static void AddFaceShapeHorizontalTiltDeltas(
        IReadOnlyDictionary<int, Point> landmarks,
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas,
        IReadOnlyList<int> leftIndices,
        IReadOnlyList<int> rightIndices,
        int leftAnchorIndex,
        int rightAnchorIndex,
        double amount)
    {
        if (!landmarks.TryGetValue(leftAnchorIndex, out Point leftAnchor) ||
            !landmarks.TryGetValue(rightAnchorIndex, out Point rightAnchor))
        {
            return;
        }

        double averageY = (leftAnchor.Y + rightAnchor.Y) * 0.5;
        double leftDy = (averageY - leftAnchor.Y) * amount;
        double rightDy = (averageY - rightAnchor.Y) * amount;

        foreach (int index in leftIndices)
        {
            AddFaceShapeDelta(landmarks, controlDeltas, index, 0, leftDy);
        }

        foreach (int index in rightIndices)
        {
            AddFaceShapeDelta(landmarks, controlDeltas, index, 0, rightDy);
        }
    }

    private static void AddFaceShapeDelta(
        IReadOnlyDictionary<int, Point> landmarks,
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas,
        int index,
        double dx,
        double dy)
    {
        if (!landmarks.TryGetValue(index, out Point point))
        {
            return;
        }

        if (controlDeltas.TryGetValue(index, out (Point Point, double Dx, double Dy) existing))
        {
            controlDeltas[index] = (existing.Point, existing.Dx + dx, existing.Dy + dy);
            return;
        }

        controlDeltas[index] = (point, dx, dy);
    }

    private static void AddFaceShapeHairBalanceControls(
        List<FaceShapeControlPoint> controls,
        Rect faceBounds,
        double centerX,
        int width,
        int height,
        double amount,
        double followDx,
        double followDy)
    {
        double topY = faceBounds.Top - (faceBounds.Height * 0.12);
        double upperY = faceBounds.Top + (faceBounds.Height * 0.04);
        double sideUpperY = faceBounds.Top + (faceBounds.Height * 0.20);
        double sideLowerY = faceBounds.Top + (faceBounds.Height * 0.54);
        double leftOuterX = faceBounds.Left - (faceBounds.Width * 0.10);
        double leftInnerX = faceBounds.Left + (faceBounds.Width * 0.04);
        double rightInnerX = faceBounds.Right - (faceBounds.Width * 0.04);
        double rightOuterX = faceBounds.Right + (faceBounds.Width * 0.10);
        double faceCenterOffset = (centerX - (faceBounds.Left + (faceBounds.Width * 0.5))) * amount * 0.45;
        double hairFollowDx = (followDx * 0.85) + faceCenterOffset;
        double hairFollowDy = followDy * 0.55;
        AddFaceShapeStaticBalanceControlPair(controls, leftOuterX, rightOuterX, topY, centerX, width, height, amount * 0.18, hairFollowDx, hairFollowDy);
        AddFaceShapeStaticBalanceControlPair(
            controls,
            faceBounds.Left + (faceBounds.Width * 0.24),
            faceBounds.Right - (faceBounds.Width * 0.24),
            upperY,
            centerX,
            width,
            height,
            amount * 0.22,
            hairFollowDx,
            hairFollowDy);
        AddFaceShapeStaticBalanceControlPair(controls, leftOuterX, rightOuterX, sideUpperY, centerX, width, height, amount * 0.35, hairFollowDx, hairFollowDy);
        AddFaceShapeStaticBalanceControlPair(controls, leftInnerX, rightInnerX, sideUpperY, centerX, width, height, amount * 0.28, hairFollowDx, hairFollowDy);
        AddFaceShapeStaticBalanceControlPair(controls, leftOuterX, rightOuterX, sideLowerY, centerX, width, height, amount * 0.30, hairFollowDx * 0.65, hairFollowDy * 0.45);
    }

    private static void AddFaceShapeStaticBalanceControlPair(
        List<FaceShapeControlPoint> controls,
        double leftX,
        double rightX,
        double y,
        double centerX,
        int width,
        int height,
        double amount,
        double followDx,
        double followDy)
    {
        double clampedY = Math.Clamp(y, 0, Math.Max(0, height - 1));
        double leftDistance = centerX - leftX;
        double rightDistance = rightX - centerX;
        if (leftDistance <= 1 || rightDistance <= 1)
        {
            return;
        }

        double balancedDistance = (leftDistance + rightDistance) * 0.5;
        double targetLeftX = centerX - balancedDistance;
        double targetRightX = centerX + balancedDistance;
        double clampedLeftX = Math.Clamp(leftX, 0, Math.Max(0, width - 1));
        double clampedRightX = Math.Clamp(rightX, 0, Math.Max(0, width - 1));
        controls.Add(new FaceShapeControlPoint(clampedLeftX, clampedY, ((targetLeftX - leftX) * amount) + followDx, followDy));
        controls.Add(new FaceShapeControlPoint(clampedRightX, clampedY, ((targetRightX - rightX) * amount) + followDx, followDy));
    }

    private static double GetFaceShapeAverageDeltaX(
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas,
        double maxY)
    {
        double total = 0;
        int count = 0;
        foreach ((Point point, double dx, _) in controlDeltas.Values)
        {
            if (point.Y > maxY)
            {
                continue;
            }

            total += dx;
            count++;
        }

        return count > 0 ? total / count : 0;
    }

    private static double GetFaceShapeAverageDeltaY(
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas,
        double maxY)
    {
        double total = 0;
        int count = 0;
        foreach ((Point point, _, double dy) in controlDeltas.Values)
        {
            if (point.Y > maxY)
            {
                continue;
            }

            total += dy;
            count++;
        }

        return count > 0 ? total / count : 0;
    }

    private static bool TryFindFaceShapeUpperShoulderLineFromAlpha(
        byte[] alphaPixels,
        int width,
        int height,
        double centerX,
        Point chin,
        Rect faceBounds,
        out FaceShapeUpperShoulderLine shoulderLine)
    {
        shoulderLine = default;
        double faceWidth = faceBounds.Width;
        double faceHeight = faceBounds.Height;
        int top = Math.Clamp((int)Math.Round(chin.Y + (faceHeight * 0.04)), 0, Math.Max(0, height - 1));
        int bottom = Math.Clamp((int)Math.Round(chin.Y + (faceHeight * 1.45)), 0, Math.Max(0, height - 1));
        int leftLimit = Math.Clamp((int)Math.Round(centerX - (faceWidth * 1.70)), 0, Math.Max(0, width - 1));
        int rightLimit = Math.Clamp((int)Math.Round(centerX + (faceWidth * 1.70)), 0, Math.Max(0, width - 1));
        if (leftLimit >= rightLimit || top >= bottom)
        {
            return false;
        }

        List<FaceShapeAlphaContourRow> rows = [];
        for (int y = top; y <= bottom; y++)
        {
            if (TryGetFaceShapeAlphaContourRow(alphaPixels, width, y, leftLimit, rightLimit, out FaceShapeAlphaContourRow row))
            {
                rows.Add(row);
            }
        }

        if (rows.Count < 8)
        {
            return false;
        }

        int neckCandidateLimit = Math.Min(rows.Count, Math.Max(6, (int)Math.Round(faceHeight * 0.20)));
        FaceShapeAlphaContourRow neckRow = rows[0];
        double narrowestWidth = double.MaxValue;
        for (int i = 0; i < neckCandidateLimit; i++)
        {
            FaceShapeAlphaContourRow row = rows[i];
            if (row.Width < faceWidth * 0.18)
            {
                continue;
            }

            if (row.Width < narrowestWidth)
            {
                narrowestWidth = row.Width;
                neckRow = row;
            }
        }

        double maxLeftExtension = 0;
        double maxRightExtension = 0;
        foreach (FaceShapeAlphaContourRow row in rows)
        {
            maxLeftExtension = Math.Max(maxLeftExtension, neckRow.Left - row.Left);
            maxRightExtension = Math.Max(maxRightExtension, row.Right - neckRow.Right);
        }

        if (maxLeftExtension < faceWidth * 0.12 || maxRightExtension < faceWidth * 0.12)
        {
            return false;
        }

        double leftTargetExtension = maxLeftExtension * 0.52;
        double rightTargetExtension = maxRightExtension * 0.52;
        Point leftShoulder = default;
        Point rightShoulder = default;
        bool hasLeftShoulder = false;
        bool hasRightShoulder = false;

        foreach (FaceShapeAlphaContourRow row in rows)
        {
            if (!hasLeftShoulder && neckRow.Left - row.Left >= leftTargetExtension)
            {
                leftShoulder = new Point(row.Left, row.Y);
                hasLeftShoulder = true;
            }

            if (!hasRightShoulder && row.Right - neckRow.Right >= rightTargetExtension)
            {
                rightShoulder = new Point(row.Right, row.Y);
                hasRightShoulder = true;
            }

            if (hasLeftShoulder && hasRightShoulder)
            {
                break;
            }
        }

        if (!hasLeftShoulder || !hasRightShoulder)
        {
            return false;
        }

        shoulderLine = new FaceShapeUpperShoulderLine(
            leftShoulder,
            rightShoulder,
            new Point(neckRow.Left, neckRow.Y),
            new Point(neckRow.Right, neckRow.Y),
            neckRow.Y);
        return true;
    }

    private static bool TryGetFaceShapeAlphaContourRow(
        byte[] alphaPixels,
        int width,
        int y,
        int leftLimit,
        int rightLimit,
        out FaceShapeAlphaContourRow row)
    {
        row = default;
        int rowOffset = y * width;
        int left = -1;
        int right = -1;
        for (int x = leftLimit; x <= rightLimit; x++)
        {
            if (alphaPixels[rowOffset + x] < 96)
            {
                continue;
            }

            left = x;
            break;
        }

        if (left < 0)
        {
            return false;
        }

        for (int x = rightLimit; x >= left; x--)
        {
            if (alphaPixels[rowOffset + x] < 96)
            {
                continue;
            }

            right = x;
            break;
        }

        if (right <= left)
        {
            return false;
        }

        row = new FaceShapeAlphaContourRow(y, left, right);
        return true;
    }

    private static void AddFaceShapeForeheadBalanceDeltas(
        IReadOnlyDictionary<int, Point> landmarks,
        Dictionary<int, (Point Point, double Dx, double Dy)> controlDeltas,
        double centerX,
        double amount)
    {
        foreach ((int leftIndex, int rightIndex) in new (int Left, int Right)[]
        {
            (67, 297),
            (109, 338)
        })
        {
            if (!landmarks.TryGetValue(leftIndex, out Point left) ||
                !landmarks.TryGetValue(rightIndex, out Point right))
            {
                continue;
            }

            double leftDistance = centerX - left.X;
            double rightDistance = right.X - centerX;
            if (leftDistance <= 1 || rightDistance <= 1)
            {
                continue;
            }

            double balancedDistance = (leftDistance + rightDistance) * 0.5;
            AddFaceShapeDelta(landmarks, controlDeltas, leftIndex, ((centerX - balancedDistance) - left.X) * amount * 0.25, 0);
            AddFaceShapeDelta(landmarks, controlDeltas, rightIndex, ((centerX + balancedDistance) - right.X) * amount * 0.25, 0);
        }

        foreach (int index in new[] { 10, 168 })
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            AddFaceShapeDelta(landmarks, controlDeltas, index, (centerX - point.X) * amount * 0.20, 0);
        }
    }

    private static bool TryBuildFaceShapeCheekControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeCheekPlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX))
        {
            return false;
        }

        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeCheekMaxInwardRatio;
        List<FaceShapeControlPoint> controls = new(
            (FaceShapeCheekPairs.Length * 2) + FaceShapeCheekAnchorIndices.Length);

        foreach ((int leftIndex, int rightIndex, double weight) in FaceShapeCheekPairs)
        {
            if (!landmarks.TryGetValue(leftIndex, out Point left) ||
                !landmarks.TryGetValue(rightIndex, out Point right))
            {
                continue;
            }

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            double pairAmount = amount * Math.Clamp(weight, 0.0, 1.0);
            controls.Add(new FaceShapeControlPoint(left.X, left.Y, pairAmount, 0));
            controls.Add(new FaceShapeControlPoint(right.X, right.Y, -pairAmount, 0));
        }

        foreach (int index in FaceShapeCheekAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 8)
        {
            return false;
        }

        double upperStartY = bounds.Top + (bounds.Height * 0.22);
        double peakY = bounds.Top + (bounds.Height * 0.47);
        double lowerEndY = bounds.Top + (bounds.Height * 0.72);
        Rect cheekBounds = new(
            bounds.Left,
            upperStartY - (bounds.Height * 0.12),
            bounds.Width,
            lowerEndY - upperStartY + (bounds.Height * 0.24));
        cheekBounds.Inflate(bounds.Width * 0.18, bounds.Height * 0.08);
        cheekBounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeCheekPlan(cheekBounds, centerX, upperStartY, peakY, lowerEndY, controls);
        return true;
    }

    private static bool TryBuildFaceShapeBoneControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeBonePlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX))
        {
            return false;
        }

        double upperStartY = bounds.Top + (bounds.Height * 0.18);
        double peakY = bounds.Top + (bounds.Height * 0.39);
        double lowerEndY = bounds.Top + (bounds.Height * 0.61);
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeBoneMaxInwardRatio;
        List<FaceShapeControlPoint> controls = new(
            (FaceShapeBonePairs.Length * 2) + FaceShapeBoneAnchorIndices.Length);

        foreach ((int leftIndex, int rightIndex, double weight) in FaceShapeBonePairs)
        {
            if (!landmarks.TryGetValue(leftIndex, out Point left) ||
                !landmarks.TryGetValue(rightIndex, out Point right))
            {
                continue;
            }

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            double pairMaxAmount = amount * Math.Clamp(weight, 0.0, 1.0);
            double leftAmount = GetFaceShapeTargetOvalPointPullAmount(
                left,
                centerX,
                bounds,
                pairMaxAmount,
                toleranceRatio: 0.02,
                excessRangeRatio: 0.14,
                ovalWidthRatio: 0.76,
                ovalTopInsetRatio: 0.02,
                ovalBottomInsetRatio: 0.02);
            double rightAmount = GetFaceShapeTargetOvalPointPullAmount(
                right,
                centerX,
                bounds,
                pairMaxAmount,
                toleranceRatio: 0.02,
                excessRangeRatio: 0.14,
                ovalWidthRatio: 0.76,
                ovalTopInsetRatio: 0.02,
                ovalBottomInsetRatio: 0.02);
            controls.Add(new FaceShapeControlPoint(left.X, left.Y, leftAmount, 0));
            controls.Add(new FaceShapeControlPoint(right.X, right.Y, -rightAmount, 0));
        }

        foreach (int index in FaceShapeBoneAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 8)
        {
            return false;
        }

        Rect boneBounds = new(
            bounds.Left,
            upperStartY - (bounds.Height * 0.10),
            bounds.Width,
            lowerEndY - upperStartY + (bounds.Height * 0.20));
        boneBounds.Inflate(bounds.Width * 0.16, bounds.Height * 0.08);
        boneBounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeBonePlan(boneBounds, centerX, upperStartY, peakY, lowerEndY, controls);
        return true;
    }

    private static bool TryBuildFaceShapeJawControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeJawPlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX))
        {
            return false;
        }

        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeJawMaxInwardRatio;
        List<FaceShapeControlPoint> controls = new(
            (FaceShapeJawPairs.Length * 2) + FaceShapeJawAnchorIndices.Length);

        foreach ((int leftIndex, int rightIndex, double weight) in FaceShapeJawPairs)
        {
            AddFaceShapeJawDirectionalControl(landmarks, leftIndex, centerX, amount, weight, controls);
            AddFaceShapeJawDirectionalControl(landmarks, rightIndex, centerX, amount, weight, controls);
        }

        foreach (int index in FaceShapeJawAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 8)
        {
            return false;
        }

        double lowerStartY = bounds.Top + (bounds.Height * 0.34);
        double fullEffectY = bounds.Top + (bounds.Height * 0.54);
        Rect jawBounds = new(
            bounds.Left,
            lowerStartY - (bounds.Height * 0.08),
            bounds.Width,
            bounds.Bottom - lowerStartY + (bounds.Height * 0.16));
        jawBounds.Inflate(bounds.Width * 0.22, bounds.Height * 0.10);
        jawBounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeJawPlan(jawBounds, centerX, lowerStartY, fullEffectY, controls);
        return true;
    }

    private static void AddFaceShapeJawDirectionalControl(
        IReadOnlyDictionary<int, Point> landmarks,
        int index,
        double centerX,
        double amount,
        double weight,
        List<FaceShapeControlPoint> controls)
    {
        if (!landmarks.TryGetValue(index, out Point point))
        {
            return;
        }

        double direction = Math.Sign(centerX - point.X);
        if (Math.Abs(direction) < 0.001)
        {
            return;
        }

        double dx = direction * amount * Math.Clamp(weight, 0.0, 1.0);
        controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, 0));
    }

    private static double GetFaceShapeTargetOvalPointPullAmount(
        Point point,
        double centerX,
        Rect bounds,
        double maxPullAmount,
        double toleranceRatio,
        double excessRangeRatio,
        double ovalWidthRatio,
        double ovalTopInsetRatio,
        double ovalBottomInsetRatio)
    {
        double faceHalfWidth = Math.Max(1.0, bounds.Width * 0.5);
        double ovalTop = bounds.Top + (bounds.Height * ovalTopInsetRatio);
        double ovalBottom = bounds.Bottom - (bounds.Height * ovalBottomInsetRatio);
        double ovalCenterY = (ovalTop + ovalBottom) * 0.5;
        double ovalRadiusY = Math.Max(1.0, (ovalBottom - ovalTop) * 0.5);
        double normalizedY = (point.Y - ovalCenterY) / ovalRadiusY;
        if (normalizedY <= -1.0 || normalizedY >= 1.0)
        {
            return 0.0;
        }

        double ovalRadiusX = faceHalfWidth * ovalWidthRatio;
        double targetHalfWidth = ovalRadiusX * Math.Sqrt(Math.Max(0.0, 1.0 - (normalizedY * normalizedY)));
        double actualHalfWidth = Math.Abs(point.X - centerX);
        double tolerance = faceHalfWidth * toleranceRatio;
        double excess = actualHalfWidth - targetHalfWidth - tolerance;
        if (excess <= 0.0)
        {
            return 0.0;
        }

        double excessWeight = SmoothStep01(excess / Math.Max(1.0, faceHalfWidth * excessRangeRatio));
        return Math.Min(Math.Max(0.0, maxPullAmount) * excessWeight, excess);
    }

    private static bool TryBuildFaceShapeChinControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeChinPlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX) ||
            !landmarks.TryGetValue(152, out Point chinTip))
        {
            return false;
        }

        double inwardAmount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeChinMaxInwardRatio;
        double liftAmount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Height *
            FaceShapeChinMaxLiftRatio;
        List<FaceShapeControlPoint> controls = new(
            (FaceShapeChinPairs.Length * 2) + FaceShapeChinCenterIndices.Length + FaceShapeChinAnchorIndices.Length);

        foreach ((int leftIndex, int rightIndex, double weight) in FaceShapeChinPairs)
        {
            if (!landmarks.TryGetValue(leftIndex, out Point left) ||
                !landmarks.TryGetValue(rightIndex, out Point right))
            {
                continue;
            }

            if (left.X > right.X)
            {
                (left, right) = (right, left);
            }

            double pairAmount = inwardAmount * Math.Clamp(weight, 0.0, 1.0);
            double pairLift = -liftAmount * Math.Clamp(weight, 0.0, 1.0) * 0.45;
            controls.Add(new FaceShapeControlPoint(left.X, left.Y, pairAmount, pairLift));
            controls.Add(new FaceShapeControlPoint(right.X, right.Y, -pairAmount, pairLift));
        }

        foreach (int index in FaceShapeChinCenterIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            double centerLift = index == 152 ? -liftAmount : -liftAmount * 0.35;
            controls.Add(new FaceShapeControlPoint(point.X, point.Y, (centerX - point.X) * 0.35, centerLift));
        }

        foreach (int index in FaceShapeChinAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 8)
        {
            return false;
        }

        double startY = bounds.Top + (bounds.Height * 0.56);
        Rect chinBounds = new(
            bounds.Left,
            startY - (bounds.Height * 0.08),
            bounds.Width,
            bounds.Bottom - startY + (bounds.Height * 0.16));
        chinBounds.Inflate(bounds.Width * 0.16, bounds.Height * 0.08);
        chinBounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeChinPlan(chinBounds, centerX, startY, chinTip.Y, controls);
        return true;
    }

    private static bool TryBuildFaceShapeFaceTiltControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapeFaceTiltPlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapeCenterX(landmarks, out double centerX))
        {
            return false;
        }

        double centerY = bounds.Top + (bounds.Height * 0.46);
        if (TryGetFaceShapeAveragePoint(landmarks, FaceShapeFaceTiltPivotIndices, out Point pivot))
        {
            centerX = pivot.X;
            centerY = pivot.Y;
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double angle = normalized * FaceShapeFaceTiltMaxDegrees * Math.PI / 180.0;
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        List<FaceShapeControlPoint> controls = new(
            FaceShapeFaceTiltMoveIndices.Length + FaceShapeFaceTiltAnchorIndices.Length);
        List<Point> movingPoints = new(FaceShapeFaceTiltMoveIndices.Length);

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            movingPoints.Add(point);
            double dx = point.X - centerX;
            double dy = point.Y - centerY;
            double targetX = centerX + (dx * cos) - (dy * sin);
            double targetY = centerY + (dx * sin) + (dy * cos);
            controls.Add(new FaceShapeControlPoint(point.X, point.Y, targetX - point.X, targetY - point.Y));
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 12)
        {
            return false;
        }

        Rect faceBounds = movingPoints.Count > 0
            ? BuildFaceShapeLandmarkBounds(movingPoints, width, height)
            : bounds;
        faceBounds.Inflate(faceBounds.Width * 0.34, faceBounds.Height * 0.24);
        faceBounds.Intersect(new Rect(0, 0, width, height));
        plan = new FaceShapeFaceTiltPlan(faceBounds, centerX, centerY, controls);
        return true;
    }

    private static bool TryBuildFaceShapeFaceTurnControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapePosePlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapePoseCenter(landmarks, bounds, out double centerX, out double centerY))
        {
            return false;
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double baseShift = normalized * bounds.Width * FaceShapeFaceTurnMaxShiftRatio;
        double halfWidth = Math.Max(1.0, bounds.Width * 0.5);
        List<FaceShapeControlPoint> controls = new(
            FaceShapeFaceTiltMoveIndices.Length + FaceShapeFaceTiltAnchorIndices.Length);
        List<Point> movingPoints = new(FaceShapeFaceTiltMoveIndices.Length);

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            movingPoints.Add(point);
            double side = Math.Clamp((point.X - centerX) / halfWidth, -1.0, 1.0);
            double centerWeight = 1.0 - (Math.Abs(side) * 0.45);
            double perspective = -side * Math.Abs(baseShift) * 0.24;
            double dx = (baseShift * Math.Clamp(centerWeight, 0.45, 1.0)) + perspective;
            controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, 0));
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 12)
        {
            return false;
        }

        Rect faceBounds = movingPoints.Count > 0
            ? BuildFaceShapeLandmarkBounds(movingPoints, width, height)
            : bounds;
        faceBounds.Inflate(faceBounds.Width * 0.36, faceBounds.Height * 0.24);
        faceBounds.Intersect(new Rect(0, 0, width, height));
        plan = new FaceShapePosePlan(faceBounds, centerX, centerY, controls);
        return true;
    }

    private static bool TryBuildFaceShapeHeadTiltControls(
        IReadOnlyDictionary<int, Point> landmarks,
        int width,
        int height,
        double strength,
        out FaceShapePosePlan plan)
    {
        plan = default;
        Rect bounds = BuildFaceShapeLandmarkBounds(landmarks.Values, width, height);
        if (bounds.Width < 20 || bounds.Height < 20)
        {
            return false;
        }

        if (!TryGetFaceShapePoseCenter(landmarks, bounds, out double centerX, out double centerY))
        {
            return false;
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double baseShift = normalized * bounds.Height * FaceShapeHeadTiltMaxShiftRatio;
        List<FaceShapeControlPoint> controls = new(
            FaceShapeFaceTiltMoveIndices.Length + FaceShapeFaceTiltAnchorIndices.Length);
        List<Point> movingPoints = new(FaceShapeFaceTiltMoveIndices.Length);

        foreach (int index in FaceShapeFaceTiltMoveIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            movingPoints.Add(point);
            double vertical = Math.Clamp((point.Y - bounds.Top) / Math.Max(1.0, bounds.Height), 0.0, 1.0);
            double featureWeight = vertical switch
            {
                < 0.18 => 0.55,
                > 0.78 => 0.62,
                _ => 1.0
            };
            double dx = (centerX - point.X) * Math.Abs(normalized) * 0.018;
            double dy = baseShift * featureWeight;
            controls.Add(new FaceShapeControlPoint(point.X, point.Y, dx, dy));
        }

        foreach (int index in FaceShapeFaceTiltAnchorIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(point.X, point.Y, 0, 0));
        }

        if (controls.Count < 12)
        {
            return false;
        }

        Rect faceBounds = movingPoints.Count > 0
            ? BuildFaceShapeLandmarkBounds(movingPoints, width, height)
            : bounds;
        faceBounds.Inflate(faceBounds.Width * 0.34, faceBounds.Height * 0.28);
        faceBounds.Intersect(new Rect(0, 0, width, height));
        plan = new FaceShapePosePlan(faceBounds, centerX, centerY, controls);
        return true;
    }

    private static bool TryGetFaceShapePoseCenter(
        IReadOnlyDictionary<int, Point> landmarks,
        Rect bounds,
        out double centerX,
        out double centerY)
    {
        if (!TryGetFaceShapeCenterX(landmarks, out centerX))
        {
            centerY = 0;
            return false;
        }

        centerY = bounds.Top + (bounds.Height * 0.46);
        if (TryGetFaceShapeAveragePoint(landmarks, FaceShapeFaceTiltPivotIndices, out Point pivot))
        {
            centerX = pivot.X;
            centerY = pivot.Y;
        }

        return true;
    }

    private static bool TryGetFaceShapeAveragePoint(
        IReadOnlyDictionary<int, Point> landmarks,
        IReadOnlyList<int> indices,
        out Point averagePoint)
    {
        double sumX = 0;
        double sumY = 0;
        int count = 0;
        foreach (int index in indices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                sumX += point.X;
                sumY += point.Y;
                count++;
            }
        }

        if (count == 0)
        {
            averagePoint = default;
            return false;
        }

        averagePoint = new Point(sumX / count, sumY / count);
        return true;
    }

    private static bool TryGetFaceShapeAveragePoint(
        FaceShapePointArray landmarks,
        IReadOnlyList<int> indices,
        out Point averagePoint)
    {
        double sumX = 0;
        double sumY = 0;
        int count = 0;
        foreach (int index in indices)
        {
            if (TryGetFaceShapePoint(landmarks, index, out Point point))
            {
                sumX += point.X;
                sumY += point.Y;
                count++;
            }
        }

        if (count == 0)
        {
            averagePoint = default;
            return false;
        }

        averagePoint = new Point(sumX / count, sumY / count);
        return true;
    }

    private static bool TryGetFaceShapeCenterX(IReadOnlyDictionary<int, Point> landmarks, out double centerX)
    {
        double sum = 0;
        int count = 0;
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                sum += (left.X + right.X) * 0.5;
                count++;
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                sum += point.X;
                count++;
            }
        }

        if (count < 3)
        {
            centerX = 0;
            return false;
        }

        centerX = sum / count;
        return true;
    }

    private static Rect BuildFaceShapeLandmarkBounds(IEnumerable<Point> points, int width, int height)
    {
        double left = width;
        double top = height;
        double right = 0;
        double bottom = 0;
        bool hasPoint = false;

        foreach (Point point in points)
        {
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
            hasPoint = true;
        }

        return hasPoint
            ? new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top))
            : Rect.Empty;
    }

    private static Rect BuildFaceShapeLandmarkBounds(FaceShapePointArray landmarks, int width, int height)
    {
        double left = width;
        double top = height;
        double right = 0;
        double bottom = 0;
        bool hasPoint = false;

        int count = Math.Min(landmarks.Points.Length, landmarks.HasPoint.Length);
        for (int i = 0; i < count; i++)
        {
            if (!landmarks.HasPoint[i])
            {
                continue;
            }

            Point point = landmarks.Points[i];
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
            hasPoint = true;
        }

        return hasPoint
            ? new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top))
            : Rect.Empty;
    }

    private static FaceShapeWeightProfile CreateFaceShapeSymmetryWeightProfile(FaceShapeSymmetryPlan plan)
    {
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.5);
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Symmetry,
            plan.Bounds,
            plan.CenterX,
            plan.Bounds.Top + radiusY,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.5),
            radiusY,
            0.76);
    }

    private static FaceShapeWeightProfile CreateFaceShapeCheekWeightProfile(FaceShapeCheekPlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Cheek,
            plan.Bounds,
            plan.CenterX,
            plan.Bounds.Top + (plan.Bounds.Height * 0.50),
            plan.UpperStartY,
            plan.PeakY,
            plan.LowerEndY,
            0,
            0,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.5),
            Math.Max(1.0, plan.Bounds.Height * 0.56),
            0.70);
    }

    private static FaceShapeWeightProfile CreateFaceShapeBoneWeightProfile(FaceShapeBonePlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Bone,
            plan.Bounds,
            plan.CenterX,
            plan.Bounds.Top + (plan.Bounds.Height * 0.42),
            plan.UpperStartY,
            plan.PeakY,
            plan.LowerEndY,
            0,
            0,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.5),
            Math.Max(1.0, plan.Bounds.Height * 0.48),
            0.68);
    }

    private static FaceShapeWeightProfile CreateFaceShapeJawWeightProfile(FaceShapeJawPlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Jaw,
            plan.Bounds,
            plan.CenterX,
            plan.Bounds.Top + (plan.Bounds.Height * 0.46),
            0,
            0,
            0,
            plan.LowerStartY,
            plan.FullEffectY,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.5),
            Math.Max(1.0, plan.Bounds.Height * 0.56),
            0.72);
    }

    private static FaceShapeWeightProfile CreateFaceShapeChinWeightProfile(FaceShapeChinPlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Chin,
            plan.Bounds,
            plan.CenterX,
            plan.Bounds.Top + (plan.Bounds.Height * 0.58),
            0,
            0,
            0,
            0,
            0,
            plan.StartY,
            plan.ChinTipY,
            Math.Max(1.0, plan.Bounds.Width * 0.42),
            Math.Max(1.0, plan.Bounds.Height * 0.58),
            0.70);
    }

    private static FaceShapeWeightProfile CreateFaceShapeFaceTiltWeightProfile(FaceShapeFaceTiltPlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.FaceTilt,
            plan.Bounds,
            plan.CenterX,
            plan.CenterY,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.52),
            Math.Max(1.0, plan.Bounds.Height * 0.54),
            0.78);
    }

    private static FaceShapeWeightProfile CreateFaceShapePoseWeightProfile(FaceShapePosePlan plan)
    {
        return new FaceShapeWeightProfile(
            FaceShapeWeightMode.Pose,
            plan.Bounds,
            plan.CenterX,
            plan.CenterY,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            Math.Max(1.0, plan.Bounds.Width * 0.54),
            Math.Max(1.0, plan.Bounds.Height * 0.56),
            0.76);
    }

    private static double GetFaceShapeWeight(int x, int y, in FaceShapeWeightProfile profile)
    {
        double verticalWeight = 1.0;
        if (profile.Mode is FaceShapeWeightMode.Cheek or FaceShapeWeightMode.Bone)
        {
            verticalWeight = GetFaceShapeBandWeight(y, profile.UpperStartY, profile.PeakY, profile.LowerEndY);
            if (verticalWeight <= 0.001)
            {
                return 0.0;
            }
        }
        else if (profile.Mode == FaceShapeWeightMode.Jaw)
        {
            verticalWeight = SmoothStep01((y - profile.LowerStartY) / Math.Max(1.0, profile.FullEffectY - profile.LowerStartY));
            if (verticalWeight <= 0.001)
            {
                return 0.0;
            }
        }
        else if (profile.Mode == FaceShapeWeightMode.Chin)
        {
            verticalWeight = SmoothStep01((y - profile.StartY) / Math.Max(1.0, profile.ChinTipY - profile.StartY));
            if (verticalWeight <= 0.001)
            {
                return 0.0;
            }
        }

        double nx = (x - profile.CenterX) / profile.RadiusX;
        double ny = (y - profile.CenterY) / profile.RadiusY;
        double ellipseWeight = GetFaceShapeEllipseFalloff(nx, ny, profile.SolidRadius);
        if (ellipseWeight <= 0.001)
        {
            return 0.0;
        }

        return profile.Mode switch
        {
            FaceShapeWeightMode.Cheek => verticalWeight *
                ellipseWeight *
                SmoothStep01(((Math.Abs(x - profile.CenterX) / profile.RadiusX) - 0.20) / 0.18),
            FaceShapeWeightMode.Bone => verticalWeight *
                ellipseWeight *
                SmoothStep01(((Math.Abs(x - profile.CenterX) / profile.RadiusX) - 0.30) / 0.18),
            FaceShapeWeightMode.Jaw or FaceShapeWeightMode.Chin => verticalWeight * ellipseWeight,
            _ => ellipseWeight
        };
    }

    private static double GetFaceShapeBandWeight(int y, double upperStartY, double peakY, double lowerEndY)
    {
        if (y <= peakY)
        {
            return SmoothStep01((y - upperStartY) / Math.Max(1.0, peakY - upperStartY));
        }

        return 1.0 - SmoothStep01((y - peakY) / Math.Max(1.0, lowerEndY - peakY));
    }

    private static double GetFaceShapeEllipseFalloff(double nx, double ny, double solidRadius)
    {
        double distance2 = (nx * nx) + (ny * ny);
        if (distance2 >= 1.0)
        {
            return 0.0;
        }

        double solidRadius2 = solidRadius * solidRadius;
        if (distance2 <= solidRadius2)
        {
            return 1.0;
        }

        double distance = Math.Sqrt(distance2);
        return 1.0 - SmoothStep01((distance - solidRadius) / Math.Max(0.001, 1.0 - solidRadius));
    }

    private enum FaceShapeWeightMode
    {
        Symmetry,
        Cheek,
        Bone,
        Jaw,
        Chin,
        FaceTilt,
        Pose
    }

    private readonly record struct FaceShapeWeightProfile(
        FaceShapeWeightMode Mode,
        Rect Bounds,
        double CenterX,
        double CenterY,
        double UpperStartY,
        double PeakY,
        double LowerEndY,
        double LowerStartY,
        double FullEffectY,
        double StartY,
        double ChinTipY,
        double RadiusX,
        double RadiusY,
        double SolidRadius);

    private readonly record struct FaceShapeControlPoint(double X, double Y, double Dx, double Dy);

    private readonly record struct FaceShapeAlphaContourRow(int Y, int Left, int Right)
    {
        public int Width => Right - Left;
    }

    private readonly record struct FaceShapeUpperShoulderLine(
        Point Left,
        Point Right,
        Point NeckLeft,
        Point NeckRight,
        double NeckY);

    private readonly record struct FaceShapeProjectionSample(double X, double Y, double Z);

    private readonly record struct FaceShapeSymmetryPlan(
        Rect Bounds,
        double CenterX,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapeCheekPlan(
        Rect Bounds,
        double CenterX,
        double UpperStartY,
        double PeakY,
        double LowerEndY,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapeBonePlan(
        Rect Bounds,
        double CenterX,
        double UpperStartY,
        double PeakY,
        double LowerEndY,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapeJawPlan(
        Rect Bounds,
        double CenterX,
        double LowerStartY,
        double FullEffectY,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapeChinPlan(
        Rect Bounds,
        double CenterX,
        double StartY,
        double ChinTipY,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapeFaceTiltPlan(
        Rect Bounds,
        double CenterX,
        double CenterY,
        List<FaceShapeControlPoint> Controls);

    private readonly record struct FaceShapePosePlan(
        Rect Bounds,
        double CenterX,
        double CenterY,
        List<FaceShapeControlPoint> Controls);
}
