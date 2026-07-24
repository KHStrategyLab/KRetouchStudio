namespace KRetouchStudio.Pipeline;

public readonly record struct RetouchDirtyRange(
    RetouchStageId First,
    RetouchStageId Last)
{
    public static RetouchDirtyRange FromChangedStage(RetouchStageId changedStage)
    {
        RetouchStageId first = RetouchStageCatalog.GetFirstDirtyStage(changedStage);
        RetouchStageId last = changedStage == RetouchStageId.TextOverlay
            ? RetouchStageId.TextOverlay
            : RetouchStageId.Background;
        return new RetouchDirtyRange(first, last);
    }

    public bool Contains(RetouchStageId stage)
    {
        return stage >= First && stage <= Last;
    }

    public IEnumerable<RetouchStageId> EnumerateRasterStages()
    {
        if (First == RetouchStageId.TextOverlay)
        {
            yield break;
        }

        foreach (RetouchStageId stage in RetouchStageCatalog.EnumerateRasterRange(First, Last))
        {
            yield return stage;
        }
    }
}
