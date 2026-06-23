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
    private const string FaceShapeCheekHistoryTitle = "Face Cheek";
    private const string FaceShapeCheekHistoryDetail = "Cheek";
    private const double FaceShapeCheekMaxInwardRatio = 0.075;
    private const string FaceShapeBoneHistoryTitle = "Face Bone";
    private const string FaceShapeBoneHistoryDetail = "Bone";
    private const double FaceShapeBoneMaxInwardRatio = 0.065;
    private const string FaceShapeJawHistoryTitle = "Face Jaw";
    private const string FaceShapeJawHistoryDetail = "Jaw";
    private const double FaceShapeJawMaxInwardRatio = 0.09;
    private const string FaceShapeChinHistoryTitle = "Face Chin";
    private const string FaceShapeChinHistoryDetail = "Chin";
    private const double FaceShapeChinMaxInwardRatio = 0.075;
    private const double FaceShapeChinMaxLiftRatio = 0.028;
    private const string FaceShapeFaceTiltHistoryTitle = "Face F-Tilt";
    private const string FaceShapeFaceTiltHistoryDetail = "F-Tilt";
    private const double FaceShapeFaceTiltMaxDegrees = 8.0;
    private const string FaceShapeFaceTurnHistoryTitle = "Face Turn";
    private const string FaceShapeFaceTurnHistoryDetail = "Turn";
    private const double FaceShapeFaceTurnMaxShiftRatio = 0.065;
    private const string FaceShapeHeadTiltHistoryTitle = "Face Up/Dn";
    private const string FaceShapeHeadTiltHistoryDetail = "Up/Dn";
    private const double FaceShapeHeadTiltMaxShiftRatio = 0.060;

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
        (234, 454, 0.20),
        (93, 323, 0.42),
        (132, 361, 0.58),
        (58, 288, 0.62),
        (172, 397, 0.82),
        (136, 365, 1.00),
        (150, 379, 1.00),
        (149, 378, 0.88),
        (176, 400, 0.62),
        (148, 377, 0.32)
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
        (234, 454, 0.35),
        (93, 323, 0.55),
        (132, 361, 0.45),
        (50, 280, 0.35),
        (101, 330, 0.52),
        (118, 347, 0.78),
        (123, 352, 1.00),
        (187, 411, 0.82),
        (205, 425, 1.00),
        (206, 426, 0.86),
        (207, 427, 0.64),
        (213, 433, 0.44)
    ];

    private static readonly (int Left, int Right, double Weight)[] FaceShapeBonePairs =
    [
        (127, 356, 0.50),
        (234, 454, 0.86),
        (93, 323, 1.00),
        (132, 361, 0.56),
        (50, 280, 0.40),
        (101, 330, 0.55),
        (118, 347, 0.84),
        (123, 352, 0.92),
        (187, 411, 0.58),
        (205, 425, 0.45)
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
    private int _faceShapeSymmetryRenderVersion;

    private async void FaceShapeRetouchTab_FaceShapeAdjustmentCommitted(object? sender, EventArgs e)
    {
        if (FaceShapeRetouchTab is null)
        {
            return;
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
            MediaPipeStatusText = "Upper: body landmarks needed";
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
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Sym: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeSymmetryPreview(safeBase, landmarks, renderStrength));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeSymmetryHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Sym: applied {strength:0}";
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
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Cheek: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeCheekPreview(safeBase, landmarks, renderStrength));

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
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Bone: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeBonePreview(safeBase, landmarks, renderStrength));

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
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Jaw: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeJawPreview(safeBase, landmarks, renderStrength));

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
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Chin: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeChinPreview(safeBase, landmarks, renderStrength));

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
            MediaPipeStatusText = "Face F-Tilt: original-size image only";
            return;
        }

        if (Math.Abs(strength - 50) <= 0.001)
        {
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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
            MediaPipeStatusText = "Face F-Tilt: no landmarks";
            return;
        }

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face F-Tilt: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeFaceTiltPreview(safeBase, landmarks, renderStrength));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeFaceTiltHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face F-Tilt: applied {strength:0}";
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
            MediaPipeStatusText = "Face Turn: original-size image only";
            return;
        }

        if (Math.Abs(strength - 50) <= 0.001)
        {
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
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
            MediaPipeStatusText = "Face Turn: no landmarks";
            return;
        }

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Turn: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeFaceTurnPreview(safeBase, landmarks, renderStrength));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeFaceTurnHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Turn: applied {strength:0}";
    }

    private async Task ApplyFaceShapeHeadTiltPreviewAsync()
    {
        if (SelectedPhoto is not PhotoItem targetPhoto)
        {
            MediaPipeStatusText = "Face Up/Dn: load photo first";
            return;
        }

        double strength = Math.Clamp(Math.Round(FaceShapeRetouchTab.HeadTiltFaceShapeStrength), 0, 100);
        int renderVersion = Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
        BitmapSource baseSource = GetFaceShapeHeadTiltRenderSource(targetPhoto);
        if (baseSource.PixelWidth != targetPhoto.BaseImage.PixelWidth ||
            baseSource.PixelHeight != targetPhoto.BaseImage.PixelHeight)
        {
            MediaPipeStatusText = "Face Up/Dn: original-size image only";
            return;
        }

        if (Math.Abs(strength - 50) <= 0.001)
        {
            targetPhoto.SetAdjustedImage(CloneBitmapSource(baseSource));
            PushOrReplaceFaceShapeHeadTiltHistory(targetPhoto, strength);
            UpdatePreviewLayout();
            MediaPipeStatusText = "Face Up/Dn: reset";
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
            MediaPipeStatusText = "Face Up/Dn: no landmarks";
            return;
        }

        BitmapSource safeBase = baseSource.IsFrozen ? baseSource : CloneBitmapSource(baseSource);
        double renderStrength = strength;
        MediaPipeStatusText = $"Face Up/Dn: rendering {strength:0}...";

        BitmapSource preview = await Task.Run(() =>
            BuildFaceShapeHeadTiltPreview(safeBase, landmarks, renderStrength));

        if (!ReferenceEquals(SelectedPhoto, targetPhoto) ||
            renderVersion != _faceShapeSymmetryRenderVersion)
        {
            return;
        }

        targetPhoto.SetAdjustedImage(preview);
        PushOrReplaceFaceShapeHeadTiltHistory(targetPhoto, strength);
        UpdatePreviewLayout();
        MediaPipeStatusText = $"Face Up/Dn: applied {strength:0}";
    }

    private void ClearFaceShapeSymmetrySession()
    {
        _faceShapeSymmetrySessionPhoto = null;
        _faceShapeSymmetrySessionBaseImage = null;
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
        Interlocked.Increment(ref _faceShapeSymmetryRenderVersion);
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
        _faceShapeSymmetrySessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeCheekSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeBoneSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeJawSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeChinSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeFaceTiltSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeFaceTurnSessionBaseImage = CloneBitmapSource(source);
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
        _faceShapeHeadTiltSessionBaseImage = CloneBitmapSource(source);
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
        if (_mediaPipeAllLandmarkPoints.Count > 0 &&
            !string.IsNullOrWhiteSpace(_mediaPipeOverlayPhotoPath) &&
            string.Equals(_mediaPipeOverlayPhotoPath, targetPhoto.Path, StringComparison.OrdinalIgnoreCase))
        {
            return _mediaPipeAllLandmarkPoints.ToList();
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

            LoadMediaPipePreviewOverlay(outputDirectory, targetPhoto.Path);
            return _mediaPipeAllLandmarkPoints.ToList();
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

    private static BitmapSource BuildFaceShapeSymmetryPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeSymmetryControls(landmarks, width, height, strength, out FaceShapeSymmetryPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(24.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.18);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeEllipseWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeCheekPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeCheekControls(landmarks, width, height, strength, out FaceShapeCheekPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(18.0, plan.Bounds.Width * 0.12);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeCheekWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeBonePreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeBoneControls(landmarks, width, height, strength, out FaceShapeBonePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(16.0, plan.Bounds.Width * 0.11);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeBoneWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeJawPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeJawControls(landmarks, width, height, strength, out FaceShapeJawPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(18.0, plan.Bounds.Width * 0.13);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeJawWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeChinPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeChinControls(landmarks, width, height, strength, out FaceShapeChinPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(14.0, plan.Bounds.Width * 0.12);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeChinWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeFaceTiltPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeFaceTiltControls(landmarks, width, height, strength, out FaceShapeFaceTiltPlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.13);
        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(plan.Bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(plan.Bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(plan.Bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(plan.Bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = GetFaceShapeFaceTiltWeight(x, y, plan);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in plan.Controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static BitmapSource BuildFaceShapeFaceTurnPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeFaceTurnControls(landmarks, width, height, strength, out FaceShapePosePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.14);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Bounds,
            plan.Controls,
            sigma,
            (x, y) => GetFaceShapePoseWeight(x, y, plan));
    }

    private static BitmapSource BuildFaceShapeHeadTiltPreview(
        BitmapSource source,
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        double strength)
    {
        BitmapSource bgraSource = EnsureBitmapFormat(source, PixelFormats.Bgra32);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        if (width < 2 || height < 2)
        {
            return CloneBitmapSource(bgraSource);
        }

        Dictionary<int, Point> landmarks = BuildFaceShapePointMap(normalizedLandmarks, width, height);
        if (landmarks.Count < 32 ||
            !TryBuildFaceShapeHeadTiltControls(landmarks, width, height, strength, out FaceShapePosePlan plan))
        {
            return CloneBitmapSource(bgraSource);
        }

        double sigma = Math.Max(18.0, Math.Max(plan.Bounds.Width, plan.Bounds.Height) * 0.14);
        return BuildFaceShapeControlWarpPreview(
            bgraSource,
            plan.Bounds,
            plan.Controls,
            sigma,
            (x, y) => GetFaceShapePoseWeight(x, y, plan));
    }

    private static BitmapSource BuildFaceShapeControlWarpPreview(
        BitmapSource bgraSource,
        Rect bounds,
        IReadOnlyList<FaceShapeControlPoint> controls,
        double sigma,
        Func<int, int, double> getFaceWeight)
    {
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = CalculateStride(width, PixelFormats.Bgra32);
        byte[] sourcePixels = new byte[stride * height];
        bgraSource.CopyPixels(sourcePixels, stride, 0);
        byte[] resultPixels = (byte[])sourcePixels.Clone();

        double sigma2 = sigma * sigma * 2.0;
        int left = Math.Max(0, (int)Math.Floor(bounds.Left));
        int top = Math.Max(0, (int)Math.Floor(bounds.Top));
        int right = Math.Min(width - 1, (int)Math.Ceiling(bounds.Right));
        int bottom = Math.Min(height - 1, (int)Math.Ceiling(bounds.Bottom));

        for (int y = top; y <= bottom; y++)
        {
            for (int x = left; x <= right; x++)
            {
                double faceWeight = getFaceWeight(x, y);
                if (faceWeight <= 0.001)
                {
                    continue;
                }

                double weightedDx = 0;
                double weightedDy = 0;
                double totalWeight = 0;
                foreach (FaceShapeControlPoint control in controls)
                {
                    double dx = x - control.X;
                    double dy = y - control.Y;
                    double distance2 = (dx * dx) + (dy * dy);
                    double weight = Math.Exp(-distance2 / sigma2);
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
                    x - appliedDx,
                    y - appliedDy,
                    out byte b,
                    out byte g,
                    out byte r,
                    out byte a);

                int offset = (y * stride) + (x * 4);
                resultPixels[offset] = b;
                resultPixels[offset + 1] = g;
                resultPixels[offset + 2] = r;
                resultPixels[offset + 3] = a;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            bgraSource.DpiX,
            bgraSource.DpiY,
            PixelFormats.Bgra32,
            null,
            resultPixels,
            stride);
        preview.Freeze();
        return preview;
    }

    private static Dictionary<int, Point> BuildFaceShapePointMap(
        IReadOnlyList<MediaPipeLandmarkPoint> normalizedLandmarks,
        int width,
        int height)
    {
        Dictionary<int, Point> points = new();
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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) * FaceShapeSymmetryMaxCorrection;
        List<FaceShapeControlPoint> controls = [];

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
            controls.Add(new FaceShapeControlPoint(
                left.X,
                left.Y,
                ((centerX - balancedDistance) - left.X) * amount,
                0));
            controls.Add(new FaceShapeControlPoint(
                right.X,
                right.Y,
                ((centerX + balancedDistance) - right.X) * amount,
                0));
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (!landmarks.TryGetValue(index, out Point point))
            {
                continue;
            }

            controls.Add(new FaceShapeControlPoint(
                point.X,
                point.Y,
                (centerX - point.X) * amount * 0.85,
                0));
        }

        if (controls.Count < 8)
        {
            return false;
        }

        double expansionX = bounds.Width * 0.18;
        double expansionY = bounds.Height * 0.18;
        bounds.Inflate(expansionX, expansionY);
        bounds.Intersect(new Rect(0, 0, width, height));

        plan = new FaceShapeSymmetryPlan(bounds, centerX, controls);
        return true;
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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeCheekMaxInwardRatio;
        List<FaceShapeControlPoint> controls = [];

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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeBoneMaxInwardRatio;
        List<FaceShapeControlPoint> controls = [];

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

            double pairAmount = amount * Math.Clamp(weight, 0.0, 1.0);
            controls.Add(new FaceShapeControlPoint(left.X, left.Y, pairAmount, 0));
            controls.Add(new FaceShapeControlPoint(right.X, right.Y, -pairAmount, 0));
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

        double upperStartY = bounds.Top + (bounds.Height * 0.18);
        double peakY = bounds.Top + (bounds.Height * 0.39);
        double lowerEndY = bounds.Top + (bounds.Height * 0.61);
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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double amount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeJawMaxInwardRatio;
        List<FaceShapeControlPoint> controls = [];

        foreach ((int leftIndex, int rightIndex, double weight) in FaceShapeJawPairs)
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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3 ||
            !landmarks.TryGetValue(152, out Point chinTip))
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double inwardAmount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Width *
            FaceShapeChinMaxInwardRatio;
        double liftAmount = Math.Clamp(strength / 100.0, 0.0, 1.0) *
            bounds.Height *
            FaceShapeChinMaxLiftRatio;
        List<FaceShapeControlPoint> controls = [];

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

        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            return false;
        }

        double centerX = centerSamples.Average();
        double centerY = bounds.Top + (bounds.Height * 0.46);
        List<Point> pivotPoints = [];
        foreach (int index in FaceShapeFaceTiltPivotIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                pivotPoints.Add(point);
            }
        }

        if (pivotPoints.Count > 0)
        {
            centerX = pivotPoints.Average(point => point.X);
            centerY = pivotPoints.Average(point => point.Y);
        }

        double normalized = Math.Clamp((strength - 50.0) / 50.0, -1.0, 1.0);
        double angle = normalized * FaceShapeFaceTiltMaxDegrees * Math.PI / 180.0;
        double sin = Math.Sin(angle);
        double cos = Math.Cos(angle);
        List<FaceShapeControlPoint> controls = [];
        List<Point> movingPoints = [];

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
        List<FaceShapeControlPoint> controls = [];
        List<Point> movingPoints = [];

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
        double baseShift = -normalized * bounds.Height * FaceShapeHeadTiltMaxShiftRatio;
        List<FaceShapeControlPoint> controls = [];
        List<Point> movingPoints = [];

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
        List<double> centerSamples = [];
        foreach ((int leftIndex, int rightIndex) in FaceShapeSymmetryPairs)
        {
            if (landmarks.TryGetValue(leftIndex, out Point left) &&
                landmarks.TryGetValue(rightIndex, out Point right))
            {
                centerSamples.Add((left.X + right.X) * 0.5);
            }
        }

        foreach (int index in FaceShapeSymmetryMidlineIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                centerSamples.Add(point.X);
            }
        }

        if (centerSamples.Count < 3)
        {
            centerX = 0;
            centerY = 0;
            return false;
        }

        centerX = centerSamples.Average();
        centerY = bounds.Top + (bounds.Height * 0.46);
        List<Point> pivotPoints = [];
        foreach (int index in FaceShapeFaceTiltPivotIndices)
        {
            if (landmarks.TryGetValue(index, out Point point))
            {
                pivotPoints.Add(point);
            }
        }

        if (pivotPoints.Count > 0)
        {
            centerX = pivotPoints.Average(point => point.X);
            centerY = pivotPoints.Average(point => point.Y);
        }

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

    private static double GetFaceShapeEllipseWeight(int x, int y, FaceShapeSymmetryPlan plan)
    {
        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.5);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.5);
        double centerY = plan.Bounds.Top + radiusY;
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - centerY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        if (distance <= 0.76)
        {
            return 1.0;
        }

        return 1.0 - SmoothStep01((distance - 0.76) / 0.24);
    }

    private static double GetFaceShapeCheekWeight(int x, int y, FaceShapeCheekPlan plan)
    {
        double verticalWeight;
        if (y <= plan.PeakY)
        {
            double upperRange = Math.Max(1.0, plan.PeakY - plan.UpperStartY);
            verticalWeight = SmoothStep01((y - plan.UpperStartY) / upperRange);
        }
        else
        {
            double lowerRange = Math.Max(1.0, plan.LowerEndY - plan.PeakY);
            verticalWeight = 1.0 - SmoothStep01((y - plan.PeakY) / lowerRange);
        }

        if (verticalWeight <= 0.001)
        {
            return 0.0;
        }

        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.5);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.56);
        double centerY = plan.Bounds.Top + (plan.Bounds.Height * 0.50);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - centerY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        double ellipseWeight = distance <= 0.70
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.70) / 0.30);
        double sideAmount = Math.Abs(x - plan.CenterX) / radiusX;
        double centerFade = SmoothStep01((sideAmount - 0.20) / 0.18);
        return Math.Clamp(verticalWeight * ellipseWeight * centerFade, 0.0, 1.0);
    }

    private static double GetFaceShapeBoneWeight(int x, int y, FaceShapeBonePlan plan)
    {
        double verticalWeight;
        if (y <= plan.PeakY)
        {
            double upperRange = Math.Max(1.0, plan.PeakY - plan.UpperStartY);
            verticalWeight = SmoothStep01((y - plan.UpperStartY) / upperRange);
        }
        else
        {
            double lowerRange = Math.Max(1.0, plan.LowerEndY - plan.PeakY);
            verticalWeight = 1.0 - SmoothStep01((y - plan.PeakY) / lowerRange);
        }

        if (verticalWeight <= 0.001)
        {
            return 0.0;
        }

        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.5);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.48);
        double centerY = plan.Bounds.Top + (plan.Bounds.Height * 0.42);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - centerY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        double ellipseWeight = distance <= 0.68
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.68) / 0.32);
        double sideAmount = Math.Abs(x - plan.CenterX) / radiusX;
        double centerFade = SmoothStep01((sideAmount - 0.30) / 0.18);
        return Math.Clamp(verticalWeight * ellipseWeight * centerFade, 0.0, 1.0);
    }

    private static double GetFaceShapeJawWeight(int x, int y, FaceShapeJawPlan plan)
    {
        double verticalRange = Math.Max(1.0, plan.FullEffectY - plan.LowerStartY);
        double verticalWeight = SmoothStep01((y - plan.LowerStartY) / verticalRange);
        if (verticalWeight <= 0.001)
        {
            return 0.0;
        }

        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.5);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.56);
        double centerY = plan.Bounds.Top + (plan.Bounds.Height * 0.46);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - centerY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        double ellipseWeight = distance <= 0.72
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.72) / 0.28);
        return Math.Clamp(verticalWeight * ellipseWeight, 0.0, 1.0);
    }

    private static double GetFaceShapeChinWeight(int x, int y, FaceShapeChinPlan plan)
    {
        double verticalRange = Math.Max(1.0, plan.ChinTipY - plan.StartY);
        double verticalWeight = SmoothStep01((y - plan.StartY) / verticalRange);
        if (verticalWeight <= 0.001)
        {
            return 0.0;
        }

        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.42);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.58);
        double centerY = plan.Bounds.Top + (plan.Bounds.Height * 0.58);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - centerY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        double ellipseWeight = distance <= 0.70
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.70) / 0.30);
        return Math.Clamp(verticalWeight * ellipseWeight, 0.0, 1.0);
    }

    private static double GetFaceShapeFaceTiltWeight(int x, int y, FaceShapeFaceTiltPlan plan)
    {
        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.52);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.54);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - plan.CenterY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        return distance <= 0.78
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.78) / 0.22);
    }

    private static double GetFaceShapePoseWeight(int x, int y, FaceShapePosePlan plan)
    {
        double radiusX = Math.Max(1.0, plan.Bounds.Width * 0.54);
        double radiusY = Math.Max(1.0, plan.Bounds.Height * 0.56);
        double nx = (x - plan.CenterX) / radiusX;
        double ny = (y - plan.CenterY) / radiusY;
        double distance = Math.Sqrt((nx * nx) + (ny * ny));
        if (distance >= 1.0)
        {
            return 0.0;
        }

        return distance <= 0.76
            ? 1.0
            : 1.0 - SmoothStep01((distance - 0.76) / 0.24);
    }

    private readonly record struct FaceShapeControlPoint(double X, double Y, double Dx, double Dy);

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
