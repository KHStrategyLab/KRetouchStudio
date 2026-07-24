namespace KRetouchStudio;

[Serializable]
public sealed record ToneCurvePointSnapshot(
    double Input,
    double Output);

[Serializable]
public sealed record ToneCurveStateSnapshot(
    ToneCurvePointSnapshot[] AllPoints,
    ToneCurvePointSnapshot[] RedPoints,
    ToneCurvePointSnapshot[] GreenPoints,
    ToneCurvePointSnapshot[] BluePoints,
    double Strength)
{
    public static ToneCurveStateSnapshot Neutral { get; } = new(
        CreateDefaultPoints(),
        CreateDefaultPoints(),
        CreateDefaultPoints(),
        CreateDefaultPoints(),
        100);

    public ToneCurvePointSnapshot[] GetPoints(CurveChannel channel)
    {
        ToneCurvePointSnapshot[] source = channel switch
        {
            CurveChannel.Red => RedPoints,
            CurveChannel.Green => GreenPoints,
            CurveChannel.Blue => BluePoints,
            _ => AllPoints
        };
        return source.Select(point => new ToneCurvePointSnapshot(point.Input, point.Output)).ToArray();
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsNeutral =>
        Math.Abs(Strength - 100) <= 0.001 &&
        IsDefaultCurve(AllPoints) &&
        IsDefaultCurve(RedPoints) &&
        IsDefaultCurve(GreenPoints) &&
        IsDefaultCurve(BluePoints);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasEffectiveAdjustment =>
        Strength > 0.001 &&
        (!IsDefaultCurve(AllPoints) ||
         !IsDefaultCurve(RedPoints) ||
         !IsDefaultCurve(GreenPoints) ||
         !IsDefaultCurve(BluePoints));

    private static ToneCurvePointSnapshot[] CreateDefaultPoints()
    {
        return
        [
            new ToneCurvePointSnapshot(0, 0),
            new ToneCurvePointSnapshot(255, 255)
        ];
    }

    private static bool IsDefaultCurve(IReadOnlyCollection<ToneCurvePointSnapshot> points)
    {
        return points.Count == 2 &&
               points.Any(point => Math.Abs(point.Input) < 0.001 && Math.Abs(point.Output) < 0.001) &&
               points.Any(point => Math.Abs(point.Input - 255) < 0.001 && Math.Abs(point.Output - 255) < 0.001);
    }
}
