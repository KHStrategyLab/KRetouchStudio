namespace KRetouchStudio.Pipeline;

public enum RetouchRenderQuality
{
    Preview,
    FullResolution
}

public readonly record struct RetouchStageCacheKey(
    RetouchStageId StageId,
    RetouchRenderQuality Quality);
