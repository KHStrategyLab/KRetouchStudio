using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class FaceShapeTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum FaceShapeMode
    {
        Sym,
        Cheek,
        Bone,
        Jaw,
        Chin
    }

    private FaceShapeMode _activeFaceShapeMode = FaceShapeMode.Sym;
    private double _symStrength;
    private double _cheekStrength;
    private double _boneStrength;
    private double _jawStrength;
    private double _chinStrength;

    public FaceShapeTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSymFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Sym;

    public bool IsCheekFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Cheek;

    public bool IsBoneFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Bone;

    public bool IsJawFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Jaw;

    public bool IsChinFaceShapeModeActive => _activeFaceShapeMode == FaceShapeMode.Chin;

    public double ActiveFaceShapeStrength
    {
        get => GetFaceShapeStrength(_activeFaceShapeMode);
        set
        {
            if (!SetFaceShapeStrength(_activeFaceShapeMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        FaceShapeExpander.IsExpanded = false;
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
        SetActiveFaceShapeMode(FaceShapeMode.Sym, forceRefresh: true);
        e.Handled = true;
    }

    private void CheekFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeMode(FaceShapeMode.Cheek, forceRefresh: true);
        e.Handled = true;
    }

    private void BoneFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeMode(FaceShapeMode.Bone, forceRefresh: true);
        e.Handled = true;
    }

    private void JawFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeMode(FaceShapeMode.Jaw, forceRefresh: true);
        e.Handled = true;
    }

    private void ChinFaceShapeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveFaceShapeMode(FaceShapeMode.Chin, forceRefresh: true);
        e.Handled = true;
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
        OnPropertyChanged(nameof(ActiveFaceShapeStrength));
    }

    private void NotifyFaceShapeModeProperties()
    {
        OnPropertyChanged(nameof(IsSymFaceShapeModeActive));
        OnPropertyChanged(nameof(IsCheekFaceShapeModeActive));
        OnPropertyChanged(nameof(IsBoneFaceShapeModeActive));
        OnPropertyChanged(nameof(IsJawFaceShapeModeActive));
        OnPropertyChanged(nameof(IsChinFaceShapeModeActive));
    }

    private double GetFaceShapeStrength(FaceShapeMode mode)
    {
        return mode switch
        {
            FaceShapeMode.Cheek => _cheekStrength,
            FaceShapeMode.Bone => _boneStrength,
            FaceShapeMode.Jaw => _jawStrength,
            FaceShapeMode.Chin => _chinStrength,
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
            default:
                return ref _symStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
