using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class BodyTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum BodyMode
    {
        Neck,
        Shoulder,
        Collar,
        Slim,
        Level
    }

    private BodyMode _activeBodyMode = BodyMode.Neck;
    private double _neckStrength;
    private double _shoulderStrength;
    private double _collarStrength;
    private double _slimStrength;
    private double _levelStrength;

    public BodyTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsNeckBodyModeActive => _activeBodyMode == BodyMode.Neck;

    public bool IsShoulderBodyModeActive => _activeBodyMode == BodyMode.Shoulder;

    public bool IsCollarBodyModeActive => _activeBodyMode == BodyMode.Collar;

    public bool IsSlimBodyModeActive => _activeBodyMode == BodyMode.Slim;

    public bool IsLevelBodyModeActive => _activeBodyMode == BodyMode.Level;

    public double ActiveBodyStrength
    {
        get => GetBodyStrength(_activeBodyMode);
        set
        {
            if (!SetBodyStrength(_activeBodyMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        BodyExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeBodyMode = BodyMode.Neck;
        _neckStrength = 0;
        _shoulderStrength = 0;
        _collarStrength = 0;
        _slimStrength = 0;
        _levelStrength = 0;

        NotifyBodyModeProperties();
        OnPropertyChanged(nameof(ActiveBodyStrength));
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void NeckBodyButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBodyMode(BodyMode.Neck, forceRefresh: true);
        e.Handled = true;
    }

    private void ShoulderBodyButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBodyMode(BodyMode.Shoulder, forceRefresh: true);
        e.Handled = true;
    }

    private void CollarBodyButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBodyMode(BodyMode.Collar, forceRefresh: true);
        e.Handled = true;
    }

    private void SlimBodyButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBodyMode(BodyMode.Slim, forceRefresh: true);
        e.Handled = true;
    }

    private void LevelBodyButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBodyMode(BodyMode.Level, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveBodyMode(BodyMode mode, bool forceRefresh = false)
    {
        if (_activeBodyMode == mode)
        {
            if (forceRefresh)
            {
                NotifyBodyModeProperties();
            }

            return;
        }

        _activeBodyMode = mode;
        NotifyBodyModeProperties();
        OnPropertyChanged(nameof(ActiveBodyStrength));
    }

    private void NotifyBodyModeProperties()
    {
        OnPropertyChanged(nameof(IsNeckBodyModeActive));
        OnPropertyChanged(nameof(IsShoulderBodyModeActive));
        OnPropertyChanged(nameof(IsCollarBodyModeActive));
        OnPropertyChanged(nameof(IsSlimBodyModeActive));
        OnPropertyChanged(nameof(IsLevelBodyModeActive));
    }

    private double GetBodyStrength(BodyMode mode)
    {
        return mode switch
        {
            BodyMode.Shoulder => _shoulderStrength,
            BodyMode.Collar => _collarStrength,
            BodyMode.Slim => _slimStrength,
            BodyMode.Level => _levelStrength,
            _ => _neckStrength
        };
    }

    private bool SetBodyStrength(BodyMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetBodyStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetBodyStrengthStorage(BodyMode mode)
    {
        switch (mode)
        {
            case BodyMode.Shoulder:
                return ref _shoulderStrength;
            case BodyMode.Collar:
                return ref _collarStrength;
            case BodyMode.Slim:
                return ref _slimStrength;
            case BodyMode.Level:
                return ref _levelStrength;
            default:
                return ref _neckStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
