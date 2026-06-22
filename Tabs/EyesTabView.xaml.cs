using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class EyesTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum EyesMode
    {
        Size,
        Shape,
        Tilt,
        Bright,
        Under
    }

    private EyesMode _activeEyesMode = EyesMode.Size;
    private double _sizeStrength;
    private double _shapeStrength;
    private double _tiltStrength;
    private double _brightStrength;
    private double _underStrength;

    public EyesTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSizeEyesModeActive => _activeEyesMode == EyesMode.Size;

    public bool IsShapeEyesModeActive => _activeEyesMode == EyesMode.Shape;

    public bool IsTiltEyesModeActive => _activeEyesMode == EyesMode.Tilt;

    public bool IsBrightEyesModeActive => _activeEyesMode == EyesMode.Bright;

    public bool IsUnderEyesModeActive => _activeEyesMode == EyesMode.Under;

    public double ActiveEyesStrength
    {
        get => GetEyesStrength(_activeEyesMode);
        set
        {
            if (!SetEyesStrength(_activeEyesMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        EyesExpander.IsExpanded = false;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void SizeEyesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveEyesMode(EyesMode.Size, forceRefresh: true);
        e.Handled = true;
    }

    private void ShapeEyesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveEyesMode(EyesMode.Shape, forceRefresh: true);
        e.Handled = true;
    }

    private void TiltEyesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveEyesMode(EyesMode.Tilt, forceRefresh: true);
        e.Handled = true;
    }

    private void BrightEyesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveEyesMode(EyesMode.Bright, forceRefresh: true);
        e.Handled = true;
    }

    private void UnderEyesButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveEyesMode(EyesMode.Under, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveEyesMode(EyesMode mode, bool forceRefresh = false)
    {
        if (_activeEyesMode == mode)
        {
            if (forceRefresh)
            {
                NotifyEyesModeProperties();
            }

            return;
        }

        _activeEyesMode = mode;
        NotifyEyesModeProperties();
        OnPropertyChanged(nameof(ActiveEyesStrength));
    }

    private void NotifyEyesModeProperties()
    {
        OnPropertyChanged(nameof(IsSizeEyesModeActive));
        OnPropertyChanged(nameof(IsShapeEyesModeActive));
        OnPropertyChanged(nameof(IsTiltEyesModeActive));
        OnPropertyChanged(nameof(IsBrightEyesModeActive));
        OnPropertyChanged(nameof(IsUnderEyesModeActive));
    }

    private double GetEyesStrength(EyesMode mode)
    {
        return mode switch
        {
            EyesMode.Shape => _shapeStrength,
            EyesMode.Tilt => _tiltStrength,
            EyesMode.Bright => _brightStrength,
            EyesMode.Under => _underStrength,
            _ => _sizeStrength
        };
    }

    private bool SetEyesStrength(EyesMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetEyesStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetEyesStrengthStorage(EyesMode mode)
    {
        switch (mode)
        {
            case EyesMode.Shape:
                return ref _shapeStrength;
            case EyesMode.Tilt:
                return ref _tiltStrength;
            case EyesMode.Bright:
                return ref _brightStrength;
            case EyesMode.Under:
                return ref _underStrength;
            default:
                return ref _sizeStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
