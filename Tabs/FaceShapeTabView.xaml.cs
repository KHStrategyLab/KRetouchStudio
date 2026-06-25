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
    private FaceShapeMode _activeDetailMode = FaceShapeMode.Sym;
    private double _symStrength;
    private double _cheekStrength;
    private double _boneStrength;
    private double _jawStrength;
    private double _chinStrength;
    private double _faceTiltStrength = 50;
    private double _faceTurnStrength = 50;
    private double _headTiltStrength = 50;
    private double _alignStrength;
    private bool _isHeadPoseStrengthSliderInteracting;

    public FaceShapeTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? FaceShapeAdjustmentCommitted;

    public event EventHandler? HeadPoseAdjustmentCommitted;

    public event EventHandler? HeadPoseAdjustmentPreviewChanged;

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

    public double ActiveFaceShapeStrength
    {
        get => GetFaceShapeStrength(_activeDetailMode);
        set
        {
            if (!SetFaceShapeStrength(_activeDetailMode, value))
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

    public void Collapse()
    {
        FaceShapeExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeFaceShapeMode = FaceShapeMode.Sym;
        _activeHeadPoseMode = FaceShapeMode.FaceTilt;
        _activeDetailMode = FaceShapeMode.Sym;
        _symStrength = 0;
        _cheekStrength = 0;
        _boneStrength = 0;
        _jawStrength = 0;
        _chinStrength = 0;
        _faceTiltStrength = 50;
        _faceTurnStrength = 50;
        _headTiltStrength = 50;
        _alignStrength = 0;

        NotifyFaceShapeModeProperties();
        OnPropertyChanged(nameof(ActiveFaceShapeStrength));
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
        SetActiveDetailMode(FaceShapeMode.Sym, forceRefresh: true);
        e.Handled = true;
    }

    private void CheekFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveDetailMode(FaceShapeMode.Cheek, forceRefresh: true);
        e.Handled = true;
    }

    private void BoneFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveDetailMode(FaceShapeMode.Bone, forceRefresh: true);
        e.Handled = true;
    }

    private void JawFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveDetailMode(FaceShapeMode.Jaw, forceRefresh: true);
        e.Handled = true;
    }

    private void ChinFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveDetailMode(FaceShapeMode.Chin, forceRefresh: true);
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
        SetActiveDetailMode(FaceShapeMode.Align, forceRefresh: true);
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

    private void FaceShapeStrengthSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CommitFaceShapeAdjustment();
    }

    private void FaceShapeStrengthSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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
            CommitFaceShapeAdjustment();
        }
    }

    private void CommitFaceShapeAdjustment()
    {
        SetActiveFaceShapeMode(_activeDetailMode);
        FaceShapeAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void CommitHeadPoseAdjustment()
    {
        SetActiveFaceShapeMode(_activeHeadPoseMode);
        HeadPoseAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void SetActiveHeadPoseMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        _activeHeadPoseMode = mode;
        SetActiveFaceShapeMode(mode, forceRefresh);
        OnPropertyChanged(nameof(ActiveHeadPoseStrength));
    }

    private void SetActiveDetailMode(FaceShapeMode mode, bool forceRefresh = false)
    {
        _activeDetailMode = mode;
        SetActiveFaceShapeMode(mode, forceRefresh);
        OnPropertyChanged(nameof(ActiveFaceShapeStrength));
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
        return true;
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
