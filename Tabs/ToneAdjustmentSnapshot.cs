namespace KRetouchStudio.Tabs;

[Serializable]
public sealed record ToneAdjustmentSnapshot(
    ToneCurveStateSnapshot Curve,
    double Exposure,
    double Contrast,
    double Saturation,
    double WhiteBalance,
    double Sharpness)
{
    public static ToneAdjustmentSnapshot Neutral { get; } = new(
        ToneCurveStateSnapshot.Neutral,
        0,
        0,
        0,
        0,
        0);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsNeutral =>
        Curve.IsNeutral &&
        Math.Abs(Exposure) <= 0.001 &&
        Math.Abs(Contrast) <= 0.001 &&
        Math.Abs(Saturation) <= 0.001 &&
        Math.Abs(WhiteBalance) <= 0.001 &&
        Math.Abs(Sharpness) <= 0.001;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasEffectiveAdjustment =>
        Curve.HasEffectiveAdjustment ||
        Math.Abs(Exposure) > 0.001 ||
        Math.Abs(Contrast) > 0.001 ||
        Math.Abs(Saturation) > 0.001 ||
        Math.Abs(WhiteBalance) > 0.001 ||
        Math.Abs(Sharpness) > 0.001;
}
