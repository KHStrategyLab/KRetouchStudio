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

    private SkinAdjustmentSnapshot? _committedSkinSectionState;
    private BlemishAdjustmentSnapshot? _committedBlemishSectionState;
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

    private void RestoreRetouchSectionState(
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState)
    {
        _committedSkinSectionState = skinState;
        _committedBlemishSectionState = blemishState;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
        SkinRetouchTab?.RestoreSnapshot(skinState);
        BlemishRetouchTab?.RestoreSnapshot(blemishState);
    }

    private void ClearRetouchSectionStateForPhotoChange()
    {
        _committedSkinSectionState = null;
        _committedBlemishSectionState = null;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
    }

    private void PrepareRetouchSectionStateForHistoryCapture(string historyTitle)
    {
        if (IsComposableRetouchSectionHistoryTitle(historyTitle) ||
            (_committedSkinSectionState is null && _committedBlemishSectionState is null))
        {
            return;
        }

        // Legacy tools flatten their result. Clear live section state so later connected edits
        // start from that flattened image instead of discarding the newer operation.
        _committedSkinSectionState = null;
        _committedBlemishSectionState = null;
        ClearConnectedRetouchSession();
        ClearSkinRetouchSession();
        ClearBlemishRetouchSession();
        SkinRetouchTab?.RestoreSnapshot(null);
        BlemishRetouchTab?.RestoreSnapshot(null);
    }

    private static bool IsComposableRetouchSectionHistoryTitle(string historyTitle)
    {
        return string.Equals(historyTitle, SkinHistoryTitle, StringComparison.Ordinal) ||
               string.Equals(historyTitle, BlemishHistoryTitle, StringComparison.Ordinal);
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
        if (historyIndex >= 0 && IsComposableRetouchSectionHistoryTitle(_editorUndoHistory[historyIndex].Title))
        {
            while (historyIndex >= 0 && IsComposableRetouchSectionHistoryTitle(_editorUndoHistory[historyIndex].Title))
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

    private static bool HasEffectiveConnectedRetouchAdjustment(
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState)
    {
        return (skinState is not null && HasEffectiveSkinAdjustment(skinState)) ||
               (blemishState is not null && HasEffectiveBlemishAdjustment(blemishState));
    }

    private static BitmapSource RenderConnectedRetouchSections(
        BitmapSource source,
        IReadOnlyDictionary<int, Point> pointMap,
        SkinAdjustmentSnapshot? skinState,
        BlemishAdjustmentSnapshot? blemishState,
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

        return ReferenceEquals(result, source) ? CloneBitmapSource(source) : result;
    }
}
