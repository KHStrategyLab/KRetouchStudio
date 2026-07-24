namespace KRetouchStudio.Pipeline;

public enum RetouchStageId
{
    WorkingBase = 0,
    Tone = 1,
    FaceShape = 2,
    FaceDetail = 3,
    Skin = 4,
    Blemish = 5,
    Wrinkle = 6,
    Makeup = 7,
    Hair = 8,
    Background = 9,
    TextOverlay = 10
}

public static class RetouchStageCatalog
{
    public static IReadOnlyList<RetouchStageId> RasterStages { get; } =
    [
        RetouchStageId.WorkingBase,
        RetouchStageId.Tone,
        RetouchStageId.FaceShape,
        RetouchStageId.FaceDetail,
        RetouchStageId.Skin,
        RetouchStageId.Blemish,
        RetouchStageId.Wrinkle,
        RetouchStageId.Makeup,
        RetouchStageId.Hair,
        RetouchStageId.Background
    ];

    public static RetouchStageId GetFirstDirtyStage(RetouchStageId changedStage)
    {
        return changedStage == RetouchStageId.WorkingBase
            ? RetouchStageId.Tone
            : changedStage;
    }

    public static RetouchStageId GetPreviousRasterStage(RetouchStageId stage)
    {
        if (stage <= RetouchStageId.WorkingBase || stage > RetouchStageId.Background)
        {
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "The stage has no raster predecessor.");
        }

        return stage - 1;
    }

    public static IEnumerable<RetouchStageId> EnumerateRasterRange(
        RetouchStageId first,
        RetouchStageId last = RetouchStageId.Background)
    {
        if (first < RetouchStageId.WorkingBase ||
            last > RetouchStageId.Background ||
            first > last)
        {
            throw new ArgumentOutOfRangeException(nameof(first), "The requested raster stage range is invalid.");
        }

        for (int value = (int)first; value <= (int)last; value++)
        {
            yield return (RetouchStageId)value;
        }
    }
}
