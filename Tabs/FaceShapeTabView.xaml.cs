using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace KRetouchStudio.Tabs;

public partial class FaceShapeTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum FaceShapeMode
    {
        Sym,
        Cheek,
        Bone,
        Jaw,
        Chin,
        FaceTilt,
        FaceTurn,
        HeadTilt,
        Align
    }

    private FaceShapeMode _activeFaceShapeMode = FaceShapeMode.Sym;
    private FaceShapeMode _activeHeadPoseMode = FaceShapeMode.FaceTilt;
    private FaceShapeMode _activeFaceShapeControlMode = FaceShapeMode.Cheek;
    private FaceShapeMode _activeSymmetrizeMode = FaceShapeMode.Sym;
    private double _symStrength;
    private double _cheekStrength;
    private double _boneStrength;
    private double _jawStrength;
    private double _chinStrength;
    private double _faceTiltStrength = 50;
    private double _faceTurnStrength = 50;
    private double _headTiltStrength = 50;
    private double _alignStrength;
    private bool _isRestoringSnapshot;
    private bool _isHeadPoseStrengthSliderInteracting;
    private bool _isFaceShapeControlStrengthSliderInteracting;
    private bool _isSymmetrizeStrengthSliderInteracting;
    private FaceShapeMode _lastFaceShapeControlPreviewMode = FaceShapeMode.Cheek;
    private double _lastFaceShapeControlPreviewStrength = double.NaN;
    private FaceShapeMode _lastSymmetrizePreviewMode = FaceShapeMode.Sym;
    private double _lastSymmetrizePreviewStrength = double.NaN;
    private bool _canResetFaceShapeHistory;

    public FaceShapeTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? FaceShapeAdjustmentCommitted;

    public event EventHandler? HeadPoseAdjustmentCommitted;

    public event EventHandler? HeadPoseAdjustmentPreviewChanged;

    public event EventHandler? FaceShapeControlAdjustmentPreviewChanged;

    public event EventHandler? SymmetrizeAdjustmentPreviewChanged;

    public event EventHandler? FaceShapeResetRequested;

    public bool CanResetFaceShapeHistory
    {
        get => _canResetFaceShapeHistory;
        set
        {
            if (_canResetFaceShapeHistory == value)
            {
                return;
            }

            _canResetFaceShapeHistory = value;
            OnPropertyChanged();
        }
    }

    public bool IsSymFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Sym;

    public bool IsCheekFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Cheek;

    public bool IsBoneFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Bone;

    public bool IsJawFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Jaw;

    public bool IsChinFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Chin;

    public bool IsFaceTiltFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.FaceTilt;

    public bool IsFaceTurnFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.FaceTurn;

    public bool IsHeadTiltFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.HeadTilt;

    public bool IsAlignFaceShapeModeSelected => _activeFaceShapeMode == FaceShapeMode.Align;

    public bool IsSymFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Sym;

    public bool IsCheekFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Cheek;

    public bool IsBoneFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Bone;

    public bool IsJawFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Jaw;

    public bool IsChinFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Chin;

    public bool IsFaceTiltFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.FaceTilt;

    public bool IsFaceTurnFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.FaceTurn;

    public bool IsHeadTiltFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.HeadTilt;

    public bool IsAlignFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Align;

    public double ActiveFaceShapeControlStrength
    {
        get => GetFaceShapeStrength(_activeFaceShapeControlMode);
        set
        {
            if (!SetFaceShapeStrength(_activeFaceShapeControlMode, value))
            {
                return;
            }

            SetActiveFaceShapeMode(_activeFaceShapeControlMode);
            OnPropertyChanged();

            RaiseFaceShapeControlPreviewIfNeeded(value);
        }
    }

    public double ActiveSymmetrizeStrength
    {
        get => GetFaceShapeStrength(_activeSymmetrizeMode);
        set
        {
            if (!SetFaceShapeStrength(_activeSymmetrizeMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public double ActiveHeadPoseStrength
    {
        get => GetFaceShapeStrength(_activeHeadPoseMode);
        set
        {
            if (!SetFaceShapeStrength(_activeHeadPoseMode, value))
            {
                return;
            }

            OnPropertyChanged();
            SetActiveFaceShapeMode(_activeHeadPoseMode);
            if (_isHeadPoseStrengthSliderInteracting)
            {
                HeadPoseAdjustmentPreviewChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public double SymFaceShapeStrength => _symStrength;

    public double CheekFaceShapeStrength => _cheekStrength;

    public double BoneFaceShapeStrength => _boneStrength;

    public double JawFaceShapeStrength => _jawStrength;

    public double ChinFaceShapeStrength => _chinStrength;

    public double FaceTiltFaceShapeStrength => _faceTiltStrength;

    public double FaceTurnFaceShapeStrength => _faceTurnStrength;

    public double HeadTiltFaceShapeStrength => _headTiltStrength;

    public double AlignFaceShapeStrength => _alignStrength;

    public bool HasEffectiveAdjustment => !CaptureSnapshot().IsNeutral;

    public bool IsNeutral => !HasEffectiveAdjustment;

    public void Collapse()
    {
        FaceShapeExpander.IsExpanded = false;
    }

    public FaceShapeAdjustmentSnapshot CaptureSnapshot()
    {
        return new FaceShapeAdjustmentSnapshot(
            SymFaceShapeStrength,
            AlignFaceShapeStrength,
            CheekFaceShapeStrength,
            BoneFaceShapeStrength,
            JawFaceShapeStrength,
            ChinFaceShapeStrength,
            FaceTiltFaceShapeStrength,
            FaceTurnFaceShapeStrength,
            HeadTiltFaceShapeStrength);
    }

    public void RestoreSnapshot(FaceShapeAdjustmentSnapshot? snapshot)
    {
        FaceShapeAdjustmentSnapshot state = snapshot ?? FaceShapeAdjustmentSnapshot.Neutral;
        _isRestoringSnapshot = true;
        try
        {
            _symStrength = NormalizeSnapshotValue(state.Symmetry, 0);
            _alignStrength = NormalizeSnapshotValue(state.UpperAlign, 0);
            _cheekStrength = NormalizeSnapshotValue(state.Cheek, 0);
            _boneStrength = NormalizeSnapshotValue(state.Bone, 0);
            _jawStrength = NormalizeSnapshotValue(state.Jaw, 0);
            _chinStrength = NormalizeSnapshotValue(state.Chin, 0);
            _faceTiltStrength = NormalizeSnapshotValue(state.FaceTilt, 50);
            _faceTurnStrength = NormalizeSnapshotValue(state.FaceTurn, 50);
            _headTiltStrength = NormalizeSnapshotValue(state.HeadTilt, 50);
            _isHeadPoseStrengthSliderInteracting = false;
            _isFaceShapeControlStrengthSliderInteracting = false;
            _isSymmetrizeStrengthSliderInteracting = false;
            ResetFaceShapeControlPreviewTracking();
            ResetSymmetrizePreviewTracking();
            NotifyFaceShapeModeProperties();
            OnPropertyChanged(nameof(ActiveFaceShapeControlStrength));
            OnPropertyChanged(nameof(ActiveSymmetrizeStrength));
            OnPropertyChanged(nameof(ActiveHeadPoseStrength));
            OnPropertyChanged(nameof(SymFaceShapeStrength));
            OnPropertyChanged(nameof(CheekFaceShapeStrength));
            OnPropertyChanged(nameof(BoneFaceShapeStrength));
            OnPropertyChanged(nameof(JawFaceShapeStrength));
            OnPropertyChanged(nameof(ChinFaceShapeStrength));
            OnPropertyChanged(nameof(FaceTiltFaceShapeStrength));
            OnPropertyChanged(nameof(FaceTurnFaceShapeStrength));
            OnPropertyChanged(nameof(HeadTiltFaceShapeStrength));
            OnPropertyChanged(nameof(AlignFaceShapeStrength));
            OnPropertyChanged(nameof(HasEffectiveAdjustment));
            OnPropertyChanged(nameof(IsNeutral));
        }
        finally
        {
            _isRestoringSnapshot = false;
        }
    }

    public void ResetForPhotoChange()
    {
        _activeFaceShapeMode = FaceShapeMode.Sym;
        _activeHeadPoseMode = FaceShapeMode.FaceTilt;
        _activeFaceShapeControlMode = FaceShapeMode.Cheek;
        _activeSymmetrizeMode = FaceShapeMode.Sym;
        ResetFaceShapeAdjustmentValues();
    }

    public void ResetAfterHistoryReset()
    {
        ResetFaceShapeAdjustmentValues();
    }

    private void ResetFaceShapeAdjustmentValues()
    {
        RestoreSnapshot(null);
    }

    private void ResetFaceShapeHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        FaceShapeResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void SymFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSymmetrizeMode(FaceShapeMode.Sym, forceRefresh: true);
        e.Handled = true;
    }

    private void CheekFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeControlMode(FaceShapeMode.Cheek, forceRefresh: true);
        e.Handled = true;
    }

    private void BoneFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeControlMode(FaceShapeMode.Bone, forceRefresh: true);
        e.Handled = true;
    }

    private void JawFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeControlMode(FaceShapeMode.Jaw, forceRefresh: true);
        e.Handled = true;
    }

    private void ChinFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeControlMode(FaceShapeMode.Chin, forceRefresh: true);
        e.Handled = true;
    }

    private void FaceTiltFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHeadPoseMode(FaceShapeMode.FaceTilt, forceRefresh: true);
        e.Handled = true;
    }

    private void FaceTurnFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHeadPoseMode(FaceShapeMode.FaceTurn, forceRefresh: true);
        e.Handled = true;
    }

    private void HeadTiltFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHeadPoseMode(FaceShapeMode.HeadTilt, forceRefresh: true);
        e.Handled = true;
    }

    private void AlignFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSymmetrizeMode(FaceShapeMode.Align, forceRefresh: true);
        e.Handled = true;
    }

    private void HeadPoseStrengthSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isHeadPoseStrengthSliderInteracting = false;
        CommitHeadPoseAdjustment();
    }

    private void HeadPoseStrengthSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isHeadPoseStrengthSliderInteracting = true;
    }

    private void HeadPoseStrengthSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Left or
            Key.Right or
            Key.Up or
            Key.Down or
            Key.PageUp or
            Key.PageDown or
            Key.Home or
            Key.End)
        {
            CommitHeadPoseAdjustment();
        }
    }

    private void FaceShapeControlStrengthSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isFaceShapeControlStrengthSliderInteracting = false;
        CommitFaceShapeControlAdjustment();
    }

    private void FaceShapeControlStrengthSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isFaceShapeControlStrengthSliderInteracting = true;
        ResetFaceShapeControlPreviewTracking();
    }

    private void FaceShapeControlStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isRestoringSnapshot ||
            sender is not System.Windows.Controls.Slider slider ||
            (Mouse.LeftButton != MouseButtonState.Pressed && !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!_isFaceShapeControlStrengthSliderInteracting)
        {
            _isFaceShapeControlStrengthSliderInteracting = true;
            ResetFaceShapeControlPreviewTracking();
        }

        bool changed = SetFaceShapeStrength(_activeFaceShapeControlMode, e.NewValue);
        SetActiveFaceShapeMode(_activeFaceShapeControlMode);
        if (changed)
        {
            OnPropertyChanged(nameof(ActiveFaceShapeControlStrength));
        }

        RaiseFaceShapeControlPreviewIfNeeded(e.NewValue);
    }

    private void FaceShapeControlStrengthSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Left or
            Key.Right or
            Key.Up or
            Key.Down or
            Key.PageUp or
            Key.PageDown or
            Key.Home or
            Key.End)
        {
            CommitFaceShapeControlAdjustment();
        }
    }

    private void SymmetrizeStrengthSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isSymmetrizeStrengthSliderInteracting = false;
        CommitSymmetrizeAdjustment();
    }

    private void SymmetrizeStrengthSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSymmetrizeStrengthSliderInteracting = true;
        ResetSymmetrizePreviewTracking();
    }

    private void SymmetrizeStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isRestoringSnapshot ||
            sender is not System.Windows.Controls.Slider slider ||
            (Mouse.LeftButton != MouseButtonState.Pressed && !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!_isSymmetrizeStrengthSliderInteracting)
        {
            _isSymmetrizeStrengthSliderInteracting = true;
            ResetSymmetrizePreviewTracking();
        }

        bool changed = SetFaceShapeStrength(_activeSymmetrizeMode, e.NewValue);
        SetActiveFaceShapeMode(_activeSymmetrizeMode);
        if (changed)
        {
            OnPropertyChanged(nameof(ActiveSymmetrizeStrength));
        }

        RaiseSymmetrizePreviewIfNeeded(e.NewValue);
    }

    private void SymmetrizeStrengthSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is Key.Left or
            Key.Right or
            Key.Up or
            Key.Down or
            Key.PageUp or
            Key.PageDown or
            Key.Home or
            Key.End)
        {
            CommitSymmetrizeAdjustment();
        }
    }

    private void CommitFaceShapeControlAdjustment()
    {
        if (_isRestoringSnapshot)
        {
            return;
        }

        SetActiveFaceShapeMode(_activeFaceShapeControlMode);
        FaceShapeAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void CommitSymmetrizeAdjustment()
    {
        if (_isRestoringSnapshot)
        {
            return;
        }

        SetActiveFaceShapeMode(_activeSymmetrizeMode);
        FaceShapeAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void ResetFaceShapeControlPreviewTracking()
    {
        _lastFaceShapeControlPreviewMode = _activeFaceShapeControlMode;
        _lastFaceShapeControlPreviewStrength = double.NaN;
    }

    private void ResetSymmetrizePreviewTracking()
    {
        _lastSymmetrizePreviewMode = _activeSymmetrizeMode;
        _lastSymmetrizePreviewStrength = double.NaN;
    }

    private void RaiseFaceShapeControlPreviewIfNeeded(double value)
    {
        double previewStrength = Math.Clamp(Math.Round(value), 0, 100);
        if (!_isFaceShapeControlStrengthSliderInteracting ||
            (_lastFaceShapeControlPreviewMode == _activeFaceShapeControlMode &&
             Math.Abs(_lastFaceShapeControlPreviewStrength - previewStrength) <= 0.001))
        {
            return;
        }

        _lastFaceShapeControlPreviewMode = _activeFaceShapeControlMode;
        _lastFaceShapeControlPreviewStrength = previewStrength;
        FaceShapeControlAdjustmentPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseSymmetrizePreviewIfNeeded(double value)
    {
        double previewStrength = Math.Clamp(Math.Round(value), 0, 100);
        if (!_isSymmetrizeStrengthSliderInteracting ||
            (_lastSymmetrizePreviewMode == _activeSymmetrizeMode &&
             Math.Abs(_lastSymmetrizePreviewStrength - previewStrength) <= 0.001))
        {
            return;
        }

        _lastSymmetrizePreviewMode = _activeSymmetrizeMode;
        _lastSymmetrizePreviewStrength = previewStrength;
        SymmetrizeAdjustmentPreviewChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CommitHeadPoseAdjustment()
    {
        if (_isRestoringSnapshot)
        {
            return;
        }

        SetActiveFaceShapeMode(_activeHeadPoseMode);
        HeadPoseAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void SetActiveHeadPoseMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        _activeHeadPoseMode = mode;
        SetActiveFaceShapeMode(mode, forceRefresh);
        OnPropertyChanged(nameof(ActiveHeadPoseStrength));
    }

    private void SetActiveFaceShapeControlMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        _activeFaceShapeControlMode = mode;
        SetActiveFaceShapeMode(mode, forceRefresh);
        OnPropertyChanged(nameof(ActiveFaceShapeControlStrength));
    }

    private void SetActiveSymmetrizeMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        _activeSymmetrizeMode = mode;
        SetActiveFaceShapeMode(mode, forceRefresh);
        OnPropertyChanged(nameof(ActiveSymmetrizeStrength));
    }

    private void SetActiveFaceShapeMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        if (_activeFaceShapeMode == mode)
        {
            if (forceRefresh)
            {
                NotifyFaceShapeModeProperties();
            }

            return;
        }

        _activeFaceShapeMode = mode;
        NotifyFaceShapeModeProperties();
    }

    private void NotifyFaceShapeModeProperties()
    {
        OnPropertyChanged(nameof(IsSymFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsCheekFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsBoneFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsJawFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsChinFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsFaceTiltFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsFaceTurnFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsHeadTiltFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsAlignFaceShapeModeSelected));
        OnPropertyChanged(nameof(IsSymFaceShapeModeActive));
        OnPropertyChanged(nameof(IsCheekFaceShapeModeActive));
        OnPropertyChanged(nameof(IsBoneFaceShapeModeActive));
        OnPropertyChanged(nameof(IsJawFaceShapeModeActive));
        OnPropertyChanged(nameof(IsChinFaceShapeModeActive));
        OnPropertyChanged(nameof(IsFaceTiltFaceShapeModeActive));
        OnPropertyChanged(nameof(IsFaceTurnFaceShapeModeActive));
        OnPropertyChanged(nameof(IsHeadTiltFaceShapeModeActive));
        OnPropertyChanged(nameof(IsAlignFaceShapeModeActive));
    }

    private static bool IsFaceShapeControlMode(FaceShapeMode mode)
    {
        return mode is FaceShapeMode.Cheek or FaceShapeMode.Bone or FaceShapeMode.Jaw or FaceShapeMode.Chin;
    }

    private double GetFaceShapeStrength(FaceShapeMode mode)
    {
        return mode switch
        {
            FaceShapeMode.Cheek => _cheekStrength,
            FaceShapeMode.Bone => _boneStrength,
            FaceShapeMode.Jaw => _jawStrength,
            FaceShapeMode.Chin => _chinStrength,
            FaceShapeMode.FaceTilt => _faceTiltStrength,
            FaceShapeMode.FaceTurn => _faceTurnStrength,
            FaceShapeMode.HeadTilt => _headTiltStrength,
            FaceShapeMode.Align => _alignStrength,
            _ => _symStrength
        };
    }

    private bool SetFaceShapeStrength(FaceShapeMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetFaceShapeStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        OnPropertyChanged(nameof(HasEffectiveAdjustment));
        OnPropertyChanged(nameof(IsNeutral));
        return true;
    }

    private static double NormalizeSnapshotValue(double value, double neutralValue)
    {
        return double.IsFinite(value)
            ? Math.Clamp(Math.Round(value), 0, 100)
            : neutralValue;
    }

    private ref double GetFaceShapeStrengthStorage(FaceShapeMode mode)
    {
        switch (mode)
        {
            case FaceShapeMode.Cheek:
                return ref _cheekStrength;
            case FaceShapeMode.Bone:
                return ref _boneStrength;
            case FaceShapeMode.Jaw:
                return ref _jawStrength;
            case FaceShapeMode.Chin:
                return ref _chinStrength;
            case FaceShapeMode.FaceTilt:
                return ref _faceTiltStrength;
            case FaceShapeMode.FaceTurn:
                return ref _faceTurnStrength;
            case FaceShapeMode.HeadTilt:
                return ref _headTiltStrength;
            case FaceShapeMode.Align:
                return ref _alignStrength;
            default:
                return ref _symStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

[Serializable]
public sealed record FaceShapeAdjustmentSnapshot(
    double Symmetry,
    double UpperAlign,
    double Cheek,
    double Bone,
    double Jaw,
    double Chin,
    double FaceTilt,
    double FaceTurn,
    double HeadTilt)
{
    public static FaceShapeAdjustmentSnapshot Neutral { get; } = new(
        Symmetry: 0,
        UpperAlign: 0,
        Cheek: 0,
        Bone: 0,
        Jaw: 0,
        Chin: 0,
        FaceTilt: 50,
        FaceTurn: 50,
        HeadTilt: 50);

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsNeutral =>
        Math.Abs(Symmetry) <= 0.001 &&
        Math.Abs(UpperAlign) <= 0.001 &&
        Math.Abs(Cheek) <= 0.001 &&
        Math.Abs(Bone) <= 0.001 &&
        Math.Abs(Jaw) <= 0.001 &&
        Math.Abs(Chin) <= 0.001 &&
        Math.Abs(FaceTilt - 50) <= 0.001 &&
        Math.Abs(FaceTurn - 50) <= 0.001 &&
        Math.Abs(HeadTilt - 50) <= 0.001;
}
