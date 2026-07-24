using KRetouchStudio.Pipeline;
using KRetouchStudio.Tabs;

namespace KRetouchStudio;

public partial class MainWindow
{
    private readonly Dictionary<string, PhotoRetouchPipelineSession> _retouchPipelineSessionsByPath =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PhotoEditState> _photoEditStatesByPath =
        new(StringComparer.OrdinalIgnoreCase);

    private PhotoRetouchPipelineSession GetOrCreateRetouchPipelineSession(PhotoItem photo)
    {
        string normalizedPath = NormalizeFilePath(photo.Path);
        if (_retouchPipelineSessionsByPath.TryGetValue(
                normalizedPath,
                out PhotoRetouchPipelineSession? session))
        {
            return session;
        }

        session = new PhotoRetouchPipelineSession(photo.Path);
        _retouchPipelineSessionsByPath[normalizedPath] = session;
        return session;
    }

    private PhotoEditState GetOrCreatePhotoEditState(PhotoItem photo)
    {
        string normalizedPath = NormalizeFilePath(photo.Path);
        if (_photoEditStatesByPath.TryGetValue(normalizedPath, out PhotoEditState? state))
        {
            return state;
        }

        state = new PhotoEditState(photo.Path);
        _photoEditStatesByPath[normalizedPath] = state;
        return state;
    }

    private PhotoEditStateSnapshot CaptureCurrentPhotoEditState(PhotoItem photo)
    {
        PhotoEditState state = GetOrCreatePhotoEditState(photo);
        state.SetToneState(PhotoAdjustRetouchTab?.CaptureSnapshot());
        state.SetFaceShapeState(FaceShapeRetouchTab?.CaptureSnapshot());
        state.SetFaceDetailState(FaceDetailRetouchTab?.CaptureSnapshot());
        state.SetSkinState(
            _committedSkinSectionState is not null &&
            HasEffectiveSkinAdjustment(_committedSkinSectionState)
                ? _committedSkinSectionState
                : null);
        state.SetBlemishState(
            _committedBlemishSectionState is not null &&
            HasEffectiveBlemishAdjustment(_committedBlemishSectionState)
                ? _committedBlemishSectionState
                : null);
        state.SetWrinkleState(
            _committedWrinkleSectionState is not null &&
            HasEffectiveWrinkleAdjustment(_committedWrinkleSectionState)
                ? _committedWrinkleSectionState
                : null);
        state.SetMakeupState(
            _committedMakeupSectionState is not null &&
            HasEffectiveMakeupAdjustment(_committedMakeupSectionState)
                ? _committedMakeupSectionState
                : null);
        state.SetHairState(
            _committedHairSectionState is not null &&
            HasEffectiveHairAdjustment(_committedHairSectionState)
                ? _committedHairSectionState
                : null);
        state.SetBackgroundState(BackgroundRetouchTab?.CaptureSnapshot());
        return state.CaptureSnapshot();
    }

    private void RestorePhotoEditState(
        PhotoItem photo,
        PhotoEditStateSnapshot? snapshot,
        SkinAdjustmentSnapshot? legacySkinState,
        BlemishAdjustmentSnapshot? legacyBlemishState,
        WrinkleAdjustmentSnapshot? legacyWrinkleState,
        MakeupAdjustmentSnapshot? legacyMakeupState,
        HairAdjustmentSnapshot? legacyHairState)
    {
        PhotoEditState state = GetOrCreatePhotoEditState(photo);
        PhotoEditStateSnapshot restored = snapshot ?? new PhotoEditStateSnapshot(
            PhotoEditStateSnapshot.CurrentSchemaVersion,
            photo.Path,
            0,
            0,
            null,
            null,
            null,
            legacySkinState,
            legacyBlemishState,
            legacyWrinkleState,
            legacyMakeupState,
            legacyHairState,
            null);
        if (!string.Equals(restored.PhotoPath, photo.Path, StringComparison.OrdinalIgnoreCase))
        {
            restored = restored with { PhotoPath = photo.Path };
        }

        PhotoEditStateSnapshot current = state.CaptureSnapshot();
        RetouchStageId? earliestChangedStage = PhotoEditState.FindEarliestChangedStage(current, restored);

        CancelRetouchPipelineRenders(photo);
        state.RestoreSnapshot(restored);
        PhotoAdjustRetouchTab?.RestoreSnapshot(restored.ToneState);
        FaceShapeRetouchTab?.RestoreSnapshot(restored.FaceShapeState);
        FaceDetailRetouchTab?.RestoreSnapshot(restored.FaceDetailState);
        RestoreRetouchSectionState(
            restored.SkinState,
            restored.BlemishState,
            restored.WrinkleState,
            restored.MakeupState,
            restored.HairState);
        BackgroundRetouchTab?.RestoreSnapshot(restored.BackgroundState);

        if (earliestChangedStage is RetouchStageId dirtyStage)
        {
            PhotoRetouchPipelineSession session = GetOrCreateRetouchPipelineSession(photo);
            session.MarkStageChanged(dirtyStage);
            if (dirtyStage is RetouchStageId.FaceShape or RetouchStageId.FaceDetail or RetouchStageId.Hair)
            {
                session.AdvanceGeometryRevision();
            }
        }
    }

    private void CancelRetouchPipelineRenders(PhotoItem? photo)
    {
        if (photo is null)
        {
            return;
        }

        string normalizedPath = NormalizeFilePath(photo.Path);
        if (_retouchPipelineSessionsByPath.TryGetValue(
                normalizedPath,
                out PhotoRetouchPipelineSession? session))
        {
            session.CancelOutstandingRenders();
        }
    }

    private void ClearRetouchPipelinePreviewCachesExcept(PhotoItem? selectedPhoto)
    {
        string? selectedPath = selectedPhoto is null
            ? null
            : NormalizeFilePath(selectedPhoto.Path);
        foreach ((string path, PhotoRetouchPipelineSession session) in _retouchPipelineSessionsByPath)
        {
            if (!string.Equals(path, selectedPath, StringComparison.OrdinalIgnoreCase))
            {
                session.Cache.InvalidateQuality(RetouchRenderQuality.Preview);
            }
        }
    }

    private void DisposeRetouchPipelineSessions()
    {
        foreach (PhotoRetouchPipelineSession session in _retouchPipelineSessionsByPath.Values)
        {
            session.Dispose();
        }

        _retouchPipelineSessionsByPath.Clear();
        _photoEditStatesByPath.Clear();
    }
}
