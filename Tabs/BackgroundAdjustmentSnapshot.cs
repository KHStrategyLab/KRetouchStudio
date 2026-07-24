using System.Text.Json.Serialization;

namespace KRetouchStudio.Tabs;

[Serializable]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackgroundReplacementMode
{
    None,
    White,
    Gray,
    SolidColor,
    Image
}

[Serializable]
public sealed record BackgroundAdjustmentSnapshot(
    int SchemaVersion,
    BackgroundReplacementMode Mode,
    uint SolidColorArgb,
    string? SelectedImagePath,
    double BackgroundOpacity,
    double BoundaryProbeStrength,
    double BoundaryCleanStrength,
    double EdgeBlurStrength,
    double AlphaShrinkStrength,
    double SoftAlphaStrength,
    double AlphaGammaStrength)
{
    public const int CurrentSchemaVersion = 1;
    public const uint DefaultSolidColorArgb = 0xFFEEF0F2;

    public static BackgroundAdjustmentSnapshot Neutral { get; } = new(
        CurrentSchemaVersion,
        BackgroundReplacementMode.None,
        DefaultSolidColorArgb,
        null,
        100,
        0,
        0,
        0,
        0,
        0,
        0);

    [JsonIgnore]
    public bool IsNeutral => Mode == BackgroundReplacementMode.None;

    [JsonIgnore]
    public bool HasEffectiveAdjustment =>
        BackgroundOpacity > 0.01 &&
        Mode switch
        {
            BackgroundReplacementMode.White or
            BackgroundReplacementMode.Gray or
            BackgroundReplacementMode.SolidColor => true,
            BackgroundReplacementMode.Image => !string.IsNullOrWhiteSpace(SelectedImagePath),
            _ => false
        };
}
