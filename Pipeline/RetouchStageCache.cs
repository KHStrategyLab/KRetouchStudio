using System.Windows.Media.Imaging;

namespace KRetouchStudio.Pipeline;

public sealed class RetouchStageCacheEntry
{
    public RetouchStageCacheEntry(
        RetouchStageId stageId,
        RetouchRenderQuality quality,
        long inputRevision,
        long stateRevision,
        long outputRevision,
        long geometryRevision,
        long analysisRevision,
        BitmapSource bitmap,
        long lastAccessOrdinal)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (!bitmap.IsFrozen && bitmap.CanFreeze)
        {
            bitmap.Freeze();
        }

        StageId = stageId;
        Quality = quality;
        InputRevision = inputRevision;
        StateRevision = stateRevision;
        OutputRevision = outputRevision;
        GeometryRevision = geometryRevision;
        AnalysisRevision = analysisRevision;
        Bitmap = bitmap;
        LastAccessOrdinal = lastAccessOrdinal;
        IsValid = true;
    }

    public RetouchStageId StageId { get; }

    public RetouchRenderQuality Quality { get; }

    public long InputRevision { get; }

    public long StateRevision { get; }

    public long OutputRevision { get; }

    public long GeometryRevision { get; }

    public long AnalysisRevision { get; }

    public BitmapSource Bitmap { get; }

    public bool IsValid { get; private set; }

    internal long LastAccessOrdinal { get; private set; }

    public bool Matches(
        long inputRevision,
        long stateRevision,
        long geometryRevision,
        long analysisRevision)
    {
        return IsValid &&
               InputRevision == inputRevision &&
               StateRevision == stateRevision &&
               GeometryRevision == geometryRevision &&
               AnalysisRevision == analysisRevision;
    }

    internal void Invalidate()
    {
        IsValid = false;
    }

    internal void Touch(long accessOrdinal)
    {
        LastAccessOrdinal = accessOrdinal;
    }
}

public sealed class PhotoRetouchStageCache
{
    private readonly Dictionary<RetouchStageCacheKey, RetouchStageCacheEntry> _entries = [];
    private long _nextOutputRevision;
    private long _nextAccessOrdinal;

    public long Publish(
        RetouchStageId stageId,
        RetouchRenderQuality quality,
        long inputRevision,
        long stateRevision,
        long geometryRevision,
        long analysisRevision,
        BitmapSource bitmap)
    {
        long outputRevision = checked(++_nextOutputRevision);
        RetouchStageCacheKey key = new(stageId, quality);
        if (_entries.TryGetValue(key, out RetouchStageCacheEntry? previous))
        {
            previous.Invalidate();
        }

        _entries[key] = new RetouchStageCacheEntry(
            stageId,
            quality,
            inputRevision,
            stateRevision,
            outputRevision,
            geometryRevision,
            analysisRevision,
            bitmap,
            checked(++_nextAccessOrdinal));
        return outputRevision;
    }

    public bool TryGet(
        RetouchStageId stageId,
        RetouchRenderQuality quality,
        out RetouchStageCacheEntry? entry)
    {
        if (_entries.TryGetValue(new RetouchStageCacheKey(stageId, quality), out entry) &&
            entry.IsValid)
        {
            entry.Touch(checked(++_nextAccessOrdinal));
            return true;
        }

        entry = null;
        return false;
    }

    public bool TryGetNearestValidUpstream(
        RetouchStageId dirtyStage,
        RetouchRenderQuality quality,
        out RetouchStageCacheEntry? entry)
    {
        RetouchStageId candidate = dirtyStage == RetouchStageId.WorkingBase
            ? RetouchStageId.WorkingBase
            : RetouchStageCatalog.GetPreviousRasterStage(dirtyStage);

        for (int value = (int)candidate; value >= (int)RetouchStageId.WorkingBase; value--)
        {
            if (TryGet((RetouchStageId)value, quality, out entry))
            {
                return true;
            }
        }

        entry = null;
        return false;
    }

    public void InvalidateFrom(RetouchStageId changedStage)
    {
        RetouchStageId firstDirtyStage = changedStage == RetouchStageId.WorkingBase
            ? RetouchStageId.WorkingBase
            : RetouchStageCatalog.GetFirstDirtyStage(changedStage);
        RetouchStageCacheKey[] invalidKeys = _entries
            .Where(pair => pair.Value.StageId >= firstDirtyStage)
            .Select(pair => pair.Key)
            .ToArray();
        foreach (RetouchStageCacheKey key in invalidKeys)
        {
            _entries[key].Invalidate();
            _entries.Remove(key);
        }
    }

    public void InvalidateQuality(RetouchRenderQuality quality)
    {
        RetouchStageCacheKey[] invalidKeys = _entries
            .Where(pair => pair.Value.Quality == quality)
            .Select(pair => pair.Key)
            .ToArray();
        foreach (RetouchStageCacheKey key in invalidKeys)
        {
            _entries[key].Invalidate();
            _entries.Remove(key);
        }
    }

    public void TrimQuality(
        RetouchRenderQuality quality,
        int maximumEntries,
        IReadOnlySet<RetouchStageId> pinnedStages)
    {
        if (maximumEntries < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEntries));
        }

        RetouchStageCacheKey[] candidates = _entries
            .Where(pair =>
                pair.Key.Quality == quality &&
                pair.Value.IsValid &&
                !pinnedStages.Contains(pair.Key.StageId))
            .OrderBy(pair => pair.Value.LastAccessOrdinal)
            .Select(pair => pair.Key)
            .ToArray();
        int validCount = _entries.Count(pair =>
            pair.Key.Quality == quality &&
            pair.Value.IsValid);
        foreach (RetouchStageCacheKey key in candidates)
        {
            if (validCount <= maximumEntries)
            {
                break;
            }

            _entries[key].Invalidate();
            _entries.Remove(key);
            validCount--;
        }
    }

    public void Clear()
    {
        foreach (RetouchStageCacheEntry entry in _entries.Values)
        {
            entry.Invalidate();
        }

        _entries.Clear();
    }
}
