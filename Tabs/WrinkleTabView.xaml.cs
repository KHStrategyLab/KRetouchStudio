using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class WrinkleTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum WrinkleMode
    {
        Forehead,
        Frown,
        Eye,
        Smile,
        Neck
    }

    private WrinkleMode _activeWrinkleMode = WrinkleMode.Forehead;
    private double _foreheadStrength;
    private double _frownStrength;
    private double _eyeStrength;
    private double _smileStrength = 50;
    private double _neckStrength;

    public WrinkleTabView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsForeheadWrinkleModeActive => _activeWrinkleMode == WrinkleMode.Forehead;

    public bool IsFrownWrinkleModeActive => _activeWrinkleMode == WrinkleMode.Frown;

    public bool IsEyeWrinkleModeActive => _activeWrinkleMode == WrinkleMode.Eye;

    public bool IsSmileWrinkleModeActive => _activeWrinkleMode == WrinkleMode.Smile;

    public bool IsNeckWrinkleModeActive => _activeWrinkleMode == WrinkleMode.Neck;

    public double ActiveWrinkleStrength
    {
        get => GetWrinkleStrength(_activeWrinkleMode);
        set
        {
            if (!SetWrinkleStrength(_activeWrinkleMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        WrinkleExpander.IsExpanded = false;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void ForeheadWrinkleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveWrinkleMode(WrinkleMode.Forehead, forceRefresh: true);
        e.Handled = true;
    }

    private void FrownWrinkleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveWrinkleMode(WrinkleMode.Frown, forceRefresh: true);
        e.Handled = true;
    }

    private void EyeWrinkleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveWrinkleMode(WrinkleMode.Eye, forceRefresh: true);
        e.Handled = true;
    }

    private void SmileWrinkleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveWrinkleMode(WrinkleMode.Smile, forceRefresh: true);
        e.Handled = true;
    }

    private void NeckWrinkleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveWrinkleMode(WrinkleMode.Neck, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveWrinkleMode(WrinkleMode mode, bool forceRefresh = false)
    {
        if (_activeWrinkleMode == mode)
        {
            if (forceRefresh)
            {
                NotifyWrinkleModeProperties();
            }

            return;
        }

        _activeWrinkleMode = mode;
        NotifyWrinkleModeProperties();
        OnPropertyChanged(nameof(ActiveWrinkleStrength));
    }

    private void NotifyWrinkleModeProperties()
    {
        OnPropertyChanged(nameof(IsForeheadWrinkleModeActive));
        OnPropertyChanged(nameof(IsFrownWrinkleModeActive));
        OnPropertyChanged(nameof(IsEyeWrinkleModeActive));
        OnPropertyChanged(nameof(IsSmileWrinkleModeActive));
        OnPropertyChanged(nameof(IsNeckWrinkleModeActive));
    }

    private double GetWrinkleStrength(WrinkleMode mode)
    {
        return mode switch
        {
            WrinkleMode.Frown => _frownStrength,
            WrinkleMode.Eye => _eyeStrength,
            WrinkleMode.Smile => _smileStrength,
            WrinkleMode.Neck => _neckStrength,
            _ => _foreheadStrength
        };
    }

    private bool SetWrinkleStrength(WrinkleMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetWrinkleStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetWrinkleStrengthStorage(WrinkleMode mode)
    {
        switch (mode)
        {
            case WrinkleMode.Frown:
                return ref _frownStrength;
            case WrinkleMode.Eye:
                return ref _eyeStrength;
            case WrinkleMode.Smile:
                return ref _smileStrength;
            case WrinkleMode.Neck:
                return ref _neckStrength;
            default:
                return ref _foreheadStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
