using KRetouchStudio.Pipeline;
using KRetouchStudio.Tabs;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const string ToneHistoryTitle = "Tone Correction";
    private const string ToneHistoryDetail = "Tone";
    private const string ToneResetHistoryDetail = "Reset";

    private void PreparePhotoEditPipelineStageChange(
        PhotoItem photo,
        RetouchStageId stage)
    {
        CaptureCurrentPhotoEditState(photo);
        PhotoRetouchPipelineSession session = GetOrCreateRetouchPipelineSession(photo);
        session.MarkStageChanged(stage);
        if (stage is RetouchStageId.FaceShape or RetouchStageId.FaceDetail or RetouchStageId.Hair)
        {
            session.AdvanceGeometryRevision();
        }
    }

    private async Task<bool> RenderAndPublishPhotoEditPipelineAsync(
        PhotoItem photo,
        RetouchStageId changedStage,
        RetouchRenderQuality quality,
        string statusPrefix)
    {
        PhotoEditStateSnapshot editState = GetOrCreatePhotoEditState(photo).CaptureSnapshot();
        if (!string.IsNullOrWhiteSpace(editState.FlattenBarrierTitle))
        {
            MediaPipeStatusText =
                $"{statusPrefix}: blocked after {editState.FlattenBarrierTitle}; undo the pixel tool before changing retouch tabs";
            return false;
        }

        BitmapSource? result = await RenderPhotoEditPipelineAsync(
            photo,
            changedStage,
            quality,
            statusPrefix);
        if (result is null || !ReferenceEquals(SelectedPhoto, photo))
        {
            return false;
        }

        if (quality == RetouchRenderQuality.Preview)
        {
            SetFaceShapeHeadPoseDragPreview(
                photo,
                result,
                photo.BaseImage.PixelWidth,
                photo.BaseImage.PixelHeight);
        }
        else
        {
            photo.SetAdjustedImage(result);
            ClearFaceShapeHeadPoseDragPreview();
            ClearFaceShapeHeadPoseDragProxy();
            ClearBackgroundPreview();
            PhotoAdjustRetouchTab?.QueueCurveHistogramRefresh(result);
        }

        UpdatePreviewLayout();
        MediaPipeStatusText = $"{statusPrefix}: {(quality == RetouchRenderQuality.Preview ? "preview" : "applied")}";
        return true;
    }

    private void PushOrReplacePipelineHistory(
        PhotoItem photo,
        string title,
        string detail)
    {
        if (_editorUndoHistory.Count > 0 &&
            string.Equals(_editorUndoHistory[^1].Title, title, StringComparison.Ordinal))
        {
            _editorUndoHistory[^1] = CaptureEditorHistoryState(photo, title, detail);
            RefreshEditorHistoryPanel();
            StoreCurrentEditorHistorySession(photo, persistToDisk: false);
            return;
        }

        PushEditorHistorySnapshot(title, detail);
    }

    private static string CreateTonePipelineHistoryDetail(ToneAdjustmentSnapshot state)
    {
        return state.IsNeutral
            ? ToneResetHistoryDetail
            : $"{ToneHistoryDetail} | Exp {state.Exposure:0.#} | Con {state.Contrast:0.#} | Sat {state.Saturation:0.#} | WB {state.WhiteBalance:0.#} | Sharp {state.Sharpness:0.#}";
    }

    private void PushCurrentFaceShapePipelineHistory(PhotoItem photo)
    {
        if (FaceShapeRetouchTab.IsSymFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeSymmetryHistory(photo, FaceShapeRetouchTab.SymFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsAlignFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeUpperHistory(photo, FaceShapeRetouchTab.AlignFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsCheekFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeCheekHistory(photo, FaceShapeRetouchTab.CheekFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsBoneFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeBoneHistory(photo, FaceShapeRetouchTab.BoneFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsJawFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeJawHistory(photo, FaceShapeRetouchTab.JawFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsChinFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeChinHistory(photo, FaceShapeRetouchTab.ChinFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsFaceTiltFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeFaceTiltHistory(photo, FaceShapeRetouchTab.FaceTiltFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsFaceTurnFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeFaceTurnHistory(photo, FaceShapeRetouchTab.FaceTurnFaceShapeStrength);
        }
        else if (FaceShapeRetouchTab.IsHeadTiltFaceShapeModeSelected)
        {
            PushOrReplaceFaceShapeHeadTiltHistory(photo, FaceShapeRetouchTab.HeadTiltFaceShapeStrength);
        }
    }

    private static string CreateBackgroundPipelineHistoryDetail(BackgroundAdjustmentSnapshot state)
    {
        string mode = state.Mode switch
        {
            BackgroundReplacementMode.Gray => GrayBackgroundHistoryDetail,
            BackgroundReplacementMode.SolidColor => ColorBackgroundHistoryDetail,
            BackgroundReplacementMode.Image => ImageBackgroundHistoryDetail,
            _ => WhiteBackgroundHistoryDetail
        };
        return CreateBackgroundReplacementHistoryDetail(
            mode,
            state.BackgroundOpacity,
            state.BoundaryProbeStrength,
            state.BoundaryCleanStrength,
            state.EdgeBlurStrength,
            state.AlphaShrinkStrength,
            state.SoftAlphaStrength,
            state.AlphaGammaStrength);
    }
}
