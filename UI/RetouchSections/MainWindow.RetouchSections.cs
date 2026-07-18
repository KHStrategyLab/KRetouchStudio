using KRetouchStudio.Tabs;

using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private const string SkinHistoryTitle = "Skin";
    private const string SkinHistoryDetail = "Skin";
    private const string SkinResetHistoryDetail = "Reset";
    private const string BlemishHistoryTitle = "Blemish";
    private const string BlemishHistoryDetail = "Blemish";
    private const string BlemishResetHistoryDetail = "Reset";
    private const string WrinkleHistoryTitle = "Wrinkle";
    private const string WrinkleHistoryDetail = "Wrinkle";
    private const string WrinkleResetHistoryDetail = "Reset";
    private const string MakeupHistoryTitle = "Makeup";
    private const string MakeupHistoryDetail = "Makeup";
    private const string MakeupResetHistoryDetail = "Reset";
    private const string HairHistoryTitle = "Hair";
    private const string HairHistoryDetail = "Hair";
    private const string HairResetHistoryDetail = "Reset";

    private SkinAdjustmentSnapshot? _committedSkinSectionState;
    private BlemishAdjustmentSnapshot? _committedBlemishSectionState;
    private WrinkleAdjustmentSnapshot? _committedWrinkleSectionState;
    private MakeupAdjustmentSnapshot? _committedMakeupSectionState;
    private HairAdjustmentSnapshot? _committedHairSectionState;
    private PhotoItem? _connectedRetouchSessionPhoto;
    private string? _connectedRetouchSessionPath;
    private BitmapSource? _connectedRetouchSessionBaseImage;
    private int _connectedRetouchRenderVersion;

    private void SetCommittedSkinSectionState(SkinAdjustmentSnapshot? snapshot)
    {
        _committedSkinSectionState = snapshot;
    }

    private SkinAdjustmentSnapshot? CaptureSkinSectionState()
    {
        return _committedSkinSectionState;
    }

    private void SetCommittedBlemishSectionState(BlemishAdjustmentSnapshot? snapshot)
    {
        _committedBlemishSectionState = snapshot;
    }

    private BlemishAdjustmentSnapshot? CaptureBlemishSectionState()
    {
        return _committedBlemishSectionState;
    }

    private void SetCommittedWrinkleSectionState(WrinkleAdjustmentSnapshot? snapshot)
    {
        _committedWrinkleSectionState = snapshot;
    }

    private WrinkleAdjustmentSnapshot? CaptureWrinkleSectionState()
    {
        return _committedWrinkleSectionState;
    }

    private void SetCommittedMakeupSectionState(MakeupAdjustmentSnapshot? snapshot)
    {
        _committedMakeupSectionState = snapshot;
    }

    private MakeupAdjustmentSnapshot? CaptureMakeupSectionState()
    {
        return _committedMakeupSectionState;
    }

    private void SetCommittedHairSectionState(HairAdjustmentSnapshot? snapshot)
    {
        _committedHairSectionState = snapshot;
    }

    private HairAdjustmentSnapshot? CaptureHairSectionState()
    {
        return _committedHairSectionState;
    }

    private void RestoreRetouchSectionState(
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState,
        WrinkleAdjustmentSnapshot? wrinkleState,
        MakeupAdjustmentSnapshot? makeupState,
        HairAdjustmentSnapshot? hairState)
    {
        _committedSkinSectionState = skinState;
        _committedBlemishSectionState = blemishState;
        _committedWrinkleSectionState = wrinkleState;
        _committedMakeupSectionState = makeupState;
        _committedHairSectionState = hairState;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
        ClearWrinkleRetouchSession();
        ClearMakeupRetouchSession();
        ClearHairRetouchSession();
        SkinRetouchTab?.RestoreSnapshot(skinState);
        BlemishRetouchTab?.RestoreSnapshot(blemishState);
        WrinkleRetouchTab?.RestoreSnapshot(wrinkleState);
        MakeupRetouchTab?.RestoreSnapshot(makeupState);
        HairRetouchTab?.RestoreSnapshot(hairState);
    }

    private void ClearRetouchSectionStateForPhotoChange()
    {
        _committedSkinSectionState = null;
        _committedBlemishSectionState = null;
        _committedWrinkleSectionState = null;
        _committedMakeupSectionState = null;
        _committedHairSectionState = null;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
        ClearWrinkleRetouchSession();
        ClearMakeupRetouchSession();
        ClearHairRetouchSession();
    }

    private void PrepareRetouchSectionStateForHistoryCapture(string historyTitle, string historyDetail)
    {
        if (IsComposableRetouchSectionHistory(historyTitle, historyDetail) ||
            (_committedSkinSectionState is null &&
             _committedBlemishSectionState is null &&
             _committedWrinkleSectionState is null &&
             _committedMakeupSectionState is null &&
             _committedHairSectionState is null))
        {
            return;
        }

        // Legacy tools flatten their result. Clear live section state so later connected edits
        // start from that flattened image instead of discarding the newer operation.
        _committedSkinSectionState = null;
        _committedBlemishSectionState = null;
        _committedWrinkleSectionState = null;
        _committedMakeupSectionState = null;
        _committedHairSectionState = null;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
        ClearWrinkleRetouchSession();
        ClearMakeupRetouchSession();
        ClearHairRetouchSession();
        SkinRetouchTab?.RestoreSnapshot(null);
        BlemishRetouchTab?.RestoreSnapshot(null);
        WrinkleRetouchTab?.RestoreSnapshot(null);
        MakeupRetouchTab?.RestoreSnapshot(null);
        HairRetouchTab?.RestoreSnapshot(null);
    }

    private static bool IsComposableRetouchSectionHistoryTitle(string historyTitle)
    {
        return string.Equals(historyTitle, SkinHistoryTitle, StringComparison.Ordinal) ||
               string.Equals(historyTitle, BlemishHistoryTitle, StringComparison.Ordinal) ||
               string.Equals(historyTitle, WrinkleHistoryTitle, StringComparison.Ordinal) ||
               string.Equals(historyTitle, MakeupHistoryTitle, StringComparison.Ordinal) ||
               string.Equals(historyTitle, HairHistoryTitle, StringComparison.Ordinal);
    }

    private static bool IsComposableRetouchSectionHistory(string historyTitle, string historyDetail)
    {
        return IsComposableRetouchSectionHistoryTitle(historyTitle) ||
               (string.Equals(historyTitle, FaceShapeResetHistoryTitle, StringComparison.Ordinal) &&
                string.Equals(historyDetail, FaceShapeResetHistoryDetail, StringComparison.Ordinal)) ||
               (string.Equals(historyTitle, FaceDetailHistoryTitle, StringComparison.Ordinal) &&
                string.Equals(historyDetail, FaceDetailResetHistoryDetail, StringComparison.Ordinal)) ||
               (string.Equals(historyTitle, BackgroundReplacementHistoryTitle, StringComparison.Ordinal) &&
                string.Equals(historyDetail, BackgroundResetHistoryDetail, StringComparison.Ordinal));
    }

    private static bool IsComposableRetouchSectionHistory(EditorHistoryState history)
    {
        return IsComposableRetouchSectionHistory(history.Title, history.Detail);
    }

    private BitmapSource GetConnectedRetouchRenderSource(PhotoItem photo)
    {
        if (ReferenceEquals(_connectedRetouchSessionPhoto, photo) &&
            !string.IsNullOrWhiteSpace(_connectedRetouchSessionPath) &&
            string.Equals(_connectedRetouchSessionPath, photo.Path, StringComparison.OrdinalIgnoreCase) &&
            _connectedRetouchSessionBaseImage is not null)
        {
            return _connectedRetouchSessionBaseImage;
        }

        BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        int historyIndex = _editorUndoHistory.Count - 1;
        if (historyIndex >= 0 && IsComposableRetouchSectionHistory(_editorUndoHistory[historyIndex]))
        {
            while (historyIndex >= 0 && IsComposableRetouchSectionHistory(_editorUndoHistory[historyIndex]))
            {
                historyIndex--;
            }

            source = historyIndex >= 0
                ? _editorUndoHistory[historyIndex].AdjustedImage ?? photo.BaseImage
                : photo.BaseImage;
        }

        _connectedRetouchSessionPhoto = photo;
        _connectedRetouchSessionPath = photo.Path;
        _connectedRetouchSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
        return _connectedRetouchSessionBaseImage;
    }

    private void ClearConnectedRetouchSession()
    {
        _connectedRetouchSessionPhoto = null;
        _connectedRetouchSessionPath = null;
        _connectedRetouchSessionBaseImage = null;
    }

    private void SetConnectedRetouchSessionBase(PhotoItem photo, BitmapSource source)
    {
        _connectedRetouchSessionPhoto = photo;
        _connectedRetouchSessionPath = photo.Path;
        _connectedRetouchSessionBaseImage = source.IsFrozen ? source : CloneBitmapSource(source);
    }

    private bool TryGetSafeTabResetSource(
        PhotoItem photo,
        Func<EditorHistoryState, bool> isTargetEffect,
        Func<EditorHistoryState, bool> isTargetReset,
        out BitmapSource source,
        out string blockingHistoryTitle)
    {
        source = photo.BaseImage;
        blockingHistoryTitle = string.Empty;
        int firstActiveTargetIndex = -1;
        for (int i = 0; i < _editorUndoHistory.Count; i++)
        {
            EditorHistoryState history = _editorUndoHistory[i];
            if (isTargetReset(history))
            {
                firstActiveTargetIndex = -1;
                continue;
            }

            if (firstActiveTargetIndex < 0 && isTargetEffect(history))
            {
                firstActiveTargetIndex = i;
            }
        }

        if (firstActiveTargetIndex < 0)
        {
            return false;
        }

        for (int i = firstActiveTargetIndex + 1; i < _editorUndoHistory.Count; i++)
        {
            EditorHistoryState history = _editorUndoHistory[i];
            if (isTargetEffect(history) || isTargetReset(history) || IsComposableRetouchSectionHistory(history))
            {
                continue;
            }

            blockingHistoryTitle = history.Title;
            return false;
        }

        if (firstActiveTargetIndex == 0)
        {
            source = photo.BaseImage;
            return true;
        }

        EditorHistoryState resetBase = _editorUndoHistory[firstActiveTargetIndex - 1];
        source = resetBase.AdjustedImage ?? photo.BaseImage;
        return true;
    }

    private async Task<BitmapSource?> RebuildConnectedRetouchSectionsAsync(
        PhotoItem photo,
        BitmapSource source,
        string statusPrefix)
    {
        if (!HasEffectiveConnectedRetouchAdjustment(
                _committedSkinSectionState,
                _committedBlemishSectionState,
                _committedWrinkleSectionState,
                _committedMakeupSectionState,
                _committedHairSectionState))
        {
            return source.IsFrozen ? source : CloneBitmapSource(source);
        }

        int renderVersion = Interlocked.Increment(ref _connectedRetouchRenderVersion);
        List<MediaPipeLandmarkPoint> landmarks = await GetOrCreateFaceShapeLandmarksAsync(photo, statusPrefix);
        if (!ReferenceEquals(SelectedPhoto, photo) ||
            renderVersion != _connectedRetouchRenderVersion ||
            landmarks.Count == 0)
        {
            return null;
        }

        IReadOnlyDictionary<int, Point> pointMap = GetOrCreateFaceShapePointMap(
            photo,
            landmarks,
            source.PixelWidth,
            source.PixelHeight);
        BitmapSource safeSource = CloneBitmapSource(source);
        SkinAdjustmentSnapshot? skinState = _committedSkinSectionState;
        BlemishAdjustmentSnapshot? blemishState = _committedBlemishSectionState;
        WrinkleAdjustmentSnapshot? wrinkleState = _committedWrinkleSectionState;
        MakeupAdjustmentSnapshot? makeupState = _committedMakeupSectionState;
        HairAdjustmentSnapshot? hairState = _committedHairSectionState;

        return await Task.Run(() => RenderConnectedRetouchSections(
            safeSource,
            pointMap,
            skinState,
            blemishState,
            wrinkleState,
            makeupState,
            hairState,
            () => renderVersion != _connectedRetouchRenderVersion));
    }

    private static bool HasEffectiveConnectedRetouchAdjustment(
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState,
        WrinkleAdjustmentSnapshot? wrinkleState,
        MakeupAdjustmentSnapshot? makeupState,
        HairAdjustmentSnapshot? hairState)
    {
        return (skinState is not null && HasEffectiveSkinAdjustment(skinState)) ||
               (blemishState is not null && HasEffectiveBlemishAdjustment(blemishState)) ||
               (wrinkleState is not null && HasEffectiveWrinkleAdjustment(wrinkleState)) ||
               (makeupState is not null && HasEffectiveMakeupAdjustment(makeupState)) ||
               (hairState is not null && HasEffectiveHairAdjustment(hairState));
    }

    private static BitmapSource RenderConnectedRetouchSections(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState,
        WrinkleAdjustmentSnapshot? wrinkleState,
        MakeupAdjustmentSnapshot? makeupState,
        HairAdjustmentSnapshot? hairState,
        Func<bool> shouldCancel)
    {
        BitmapSource result = source;
        if (skinState is not null && HasEffectiveSkinAdjustment(skinState))
        {
            result = RenderSkinAdjustments(result, pointMap, skinState, shouldCancel);
        }

        if (!shouldCancel() && blemishState is not null && HasEffectiveBlemishAdjustment(blemishState))
        {
            result = RenderBlemishAdjustments(result, pointMap, blemishState, shouldCancel);
        }

        if (!shouldCancel() && wrinkleState is not null && HasEffectiveWrinkleAdjustment(wrinkleState))
        {
            result = RenderWrinkleAdjustments(result, pointMap, wrinkleState, shouldCancel);
        }

        if (!shouldCancel() && makeupState is not null && HasEffectiveMakeupAdjustment(makeupState))
        {
            result = RenderMakeupAdjustments(result, pointMap, makeupState, shouldCancel);
        }

        if (!shouldCancel() && hairState is not null && HasEffectiveHairAdjustment(hairState))
        {
            result = RenderHairAdjustments(result, pointMap, hairState, shouldCancel);
        }

        return ReferenceEquals(result, source) ? CloneBitmapSource(source) : result;
    }
}
