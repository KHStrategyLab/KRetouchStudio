using KRetouchStudio.Tabs;

namespace KRetouchStudio.Pipeline;

[Serializable]
public sealed record PhotoEditStateSnapshot(
    int SchemaVersion,
    string PhotoPath,
    long StateVersion,
    long BaseRevision,
    ToneAdjustmentSnapshot? ToneState,
    FaceShapeAdjustmentSnapshot? FaceShapeState,
    FaceDetailAdjustmentSnapshot? FaceDetailState,
    SkinAdjustmentSnapshot? SkinState,
    BlemishAdjustmentSnapshot? BlemishState,
    WrinkleAdjustmentSnapshot? WrinkleState,
    MakeupAdjustmentSnapshot? MakeupState,
    HairAdjustmentSnapshot? HairState,
    BackgroundAdjustmentSnapshot? BackgroundState)
{
    public const int CurrentSchemaVersion = 1;

    public string? FlattenBarrierTitle { get; init; }

    public static PhotoEditStateSnapshot Neutral(string photoPath)
    {
        return new PhotoEditStateSnapshot(
            CurrentSchemaVersion,
            photoPath,
            0,
            0,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasAnyAdjustment =>
        ToneState is not null ||
        FaceShapeState is not null ||
        FaceDetailState is not null ||
        SkinState is not null ||
        BlemishState is not null ||
        WrinkleState is not null ||
        MakeupState is not null ||
        HairState is not null ||
        BackgroundState is not null;
}

public sealed class PhotoEditState
{
    public PhotoEditState(string photoPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoPath);
        PhotoPath = photoPath;
    }

    public string PhotoPath { get; }

    public long StateVersion { get; private set; }

    public long BaseRevision { get; private set; }

    public ToneAdjustmentSnapshot? ToneState { get; private set; }

    public FaceShapeAdjustmentSnapshot? FaceShapeState { get; private set; }

    public FaceDetailAdjustmentSnapshot? FaceDetailState { get; private set; }

    public SkinAdjustmentSnapshot? SkinState { get; private set; }

    public BlemishAdjustmentSnapshot? BlemishState { get; private set; }

    public WrinkleAdjustmentSnapshot? WrinkleState { get; private set; }

    public MakeupAdjustmentSnapshot? MakeupState { get; private set; }

    public HairAdjustmentSnapshot? HairState { get; private set; }

    public BackgroundAdjustmentSnapshot? BackgroundState { get; private set; }

    public string? FlattenBarrierTitle { get; private set; }

    public PhotoEditStateSnapshot CaptureSnapshot()
    {
        return new PhotoEditStateSnapshot(
            PhotoEditStateSnapshot.CurrentSchemaVersion,
            PhotoPath,
            StateVersion,
            BaseRevision,
            CloneToneSnapshot(ToneState),
            FaceShapeState,
            FaceDetailState,
            SkinState,
            BlemishState,
            WrinkleState,
            MakeupState,
            HairState,
            BackgroundState)
        {
            FlattenBarrierTitle = FlattenBarrierTitle
        };
    }

    public void RestoreSnapshot(PhotoEditStateSnapshot? snapshot)
    {
        PhotoEditStateSnapshot restored = snapshot ?? PhotoEditStateSnapshot.Neutral(PhotoPath);
        if (!string.Equals(restored.PhotoPath, PhotoPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The edit state belongs to a different photo.", nameof(snapshot));
        }

        StateVersion = Math.Max(0, restored.StateVersion);
        BaseRevision = Math.Max(0, restored.BaseRevision);
        ToneState = NormalizeToneState(restored.ToneState);
        FaceShapeState = NormalizeFaceShapeState(restored.FaceShapeState);
        FaceDetailState = NormalizeFaceDetailState(restored.FaceDetailState);
        SkinState = restored.SkinState;
        BlemishState = restored.BlemishState;
        WrinkleState = restored.WrinkleState;
        MakeupState = restored.MakeupState;
        HairState = restored.HairState;
        BackgroundState = NormalizeBackgroundState(restored.BackgroundState);
        FlattenBarrierTitle = string.IsNullOrWhiteSpace(restored.FlattenBarrierTitle)
            ? null
            : restored.FlattenBarrierTitle;
    }

    public void AdvanceBaseRevision()
    {
        BaseRevision = checked(BaseRevision + 1);
        StateVersion = checked(StateVersion + 1);
    }

    public bool SetToneState(ToneAdjustmentSnapshot? state)
    {
        ToneAdjustmentSnapshot? normalized = NormalizeToneState(state);
        if (AreToneStatesEquivalent(ToneState, normalized))
        {
            return false;
        }

        ToneState = CloneToneSnapshot(normalized);
        AdvanceStateVersion();
        return true;
    }

    public bool SetFaceShapeState(FaceShapeAdjustmentSnapshot? state)
    {
        return SetState(
            NormalizeFaceShapeState(state),
            FaceShapeState,
            value => FaceShapeState = value);
    }

    public bool SetFaceDetailState(FaceDetailAdjustmentSnapshot? state)
    {
        return SetState(
            NormalizeFaceDetailState(state),
            FaceDetailState,
            value => FaceDetailState = value);
    }

    public bool SetSkinState(SkinAdjustmentSnapshot? state)
    {
        return SetState(state, SkinState, value => SkinState = value);
    }

    public bool SetBlemishState(BlemishAdjustmentSnapshot? state)
    {
        return SetState(state, BlemishState, value => BlemishState = value);
    }

    public bool SetWrinkleState(WrinkleAdjustmentSnapshot? state)
    {
        return SetState(state, WrinkleState, value => WrinkleState = value);
    }

    public bool SetMakeupState(MakeupAdjustmentSnapshot? state)
    {
        return SetState(state, MakeupState, value => MakeupState = value);
    }

    public bool SetHairState(HairAdjustmentSnapshot? state)
    {
        return SetState(state, HairState, value => HairState = value);
    }

    public bool SetBackgroundState(BackgroundAdjustmentSnapshot? state)
    {
        return SetState(
            NormalizeBackgroundState(state),
            BackgroundState,
            value => BackgroundState = value);
    }

    public bool SetFlattenBarrier(string? title)
    {
        string? normalized = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        if (string.Equals(FlattenBarrierTitle, normalized, StringComparison.Ordinal))
        {
            return false;
        }

        FlattenBarrierTitle = normalized;
        AdvanceStateVersion();
        return true;
    }

    public static RetouchStageId? FindEarliestChangedStage(
        PhotoEditStateSnapshot current,
        PhotoEditStateSnapshot restored)
    {
        if (current.BaseRevision != restored.BaseRevision)
        {
            return RetouchStageId.WorkingBase;
        }

        if (!string.Equals(
                current.FlattenBarrierTitle,
                restored.FlattenBarrierTitle,
                StringComparison.Ordinal))
        {
            return RetouchStageId.WorkingBase;
        }

        if (!AreToneStatesEquivalent(current.ToneState, restored.ToneState))
        {
            return RetouchStageId.Tone;
        }

        if (current.FaceShapeState != restored.FaceShapeState)
        {
            return RetouchStageId.FaceShape;
        }

        if (current.FaceDetailState != restored.FaceDetailState)
        {
            return RetouchStageId.FaceDetail;
        }

        if (current.SkinState != restored.SkinState)
        {
            return RetouchStageId.Skin;
        }

        if (current.BlemishState != restored.BlemishState)
        {
            return RetouchStageId.Blemish;
        }

        if (current.WrinkleState != restored.WrinkleState)
        {
            return RetouchStageId.Wrinkle;
        }

        if (current.MakeupState != restored.MakeupState)
        {
            return RetouchStageId.Makeup;
        }

        if (current.HairState != restored.HairState)
        {
            return RetouchStageId.Hair;
        }

        if (current.BackgroundState != restored.BackgroundState)
        {
            return RetouchStageId.Background;
        }

        return null;
    }

    private bool SetState<T>(T? value, T? current, Action<T?> assign)
        where T : class
    {
        if (EqualityComparer<T?>.Default.Equals(current, value))
        {
            return false;
        }

        assign(value);
        AdvanceStateVersion();
        return true;
    }

    private void AdvanceStateVersion()
    {
        StateVersion = checked(StateVersion + 1);
    }

    private static ToneAdjustmentSnapshot? NormalizeToneState(ToneAdjustmentSnapshot? state)
    {
        return state is null || state.IsNeutral
            ? null
            : CloneToneSnapshot(state);
    }

    private static FaceShapeAdjustmentSnapshot? NormalizeFaceShapeState(FaceShapeAdjustmentSnapshot? state)
    {
        return state is null || state.IsNeutral ? null : state;
    }

    private static FaceDetailAdjustmentSnapshot? NormalizeFaceDetailState(FaceDetailAdjustmentSnapshot? state)
    {
        return state is null || state.IsNeutral ? null : state;
    }

    private static BackgroundAdjustmentSnapshot? NormalizeBackgroundState(BackgroundAdjustmentSnapshot? state)
    {
        return state is null ||
               state.IsNeutral ||
               (state.Mode == BackgroundReplacementMode.Image &&
                string.IsNullOrWhiteSpace(state.SelectedImagePath))
            ? null
            : state;
    }

    private static ToneAdjustmentSnapshot? CloneToneSnapshot(ToneAdjustmentSnapshot? state)
    {
        if (state is null)
        {
            return null;
        }

        ToneCurveStateSnapshot curve = state.Curve;
        return state with
        {
            Curve = new ToneCurveStateSnapshot(
                curve.GetPoints(CurveChannel.All),
                curve.GetPoints(CurveChannel.Red),
                curve.GetPoints(CurveChannel.Green),
                curve.GetPoints(CurveChannel.Blue),
                curve.Strength)
        };
    }

    private static bool AreToneStatesEquivalent(
        ToneAdjustmentSnapshot? left,
        ToneAdjustmentSnapshot? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null ||
            Math.Abs(left.Exposure - right.Exposure) > 0.001 ||
            Math.Abs(left.Contrast - right.Contrast) > 0.001 ||
            Math.Abs(left.Saturation - right.Saturation) > 0.001 ||
            Math.Abs(left.WhiteBalance - right.WhiteBalance) > 0.001 ||
            Math.Abs(left.Sharpness - right.Sharpness) > 0.001 ||
            Math.Abs(left.Curve.Strength - right.Curve.Strength) > 0.001)
        {
            return false;
        }

        foreach (CurveChannel channel in Enum.GetValues<CurveChannel>())
        {
            ToneCurvePointSnapshot[] leftPoints = left.Curve.GetPoints(channel);
            ToneCurvePointSnapshot[] rightPoints = right.Curve.GetPoints(channel);
            if (leftPoints.Length != rightPoints.Length)
            {
                return false;
            }

            for (int index = 0; index < leftPoints.Length; index++)
            {
                if (Math.Abs(leftPoints[index].Input - rightPoints[index].Input) > 0.001 ||
                    Math.Abs(leftPoints[index].Output - rightPoints[index].Output) > 0.001)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
