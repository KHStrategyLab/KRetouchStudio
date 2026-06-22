using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class NoseTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum NoseMode
    {
        Bridge,
        Tip,
        Slim,
        Nostril,
        Length
    }

    private NoseMode _activeNoseMode = NoseMode.Bridge;
    private double _bridgeStrength;
    private double _tipStrength;
    private double _slimStrength;
    private double _nostrilStrength;
    private double _lengthStrength;

    public NoseTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBridgeNoseModeActive => _activeNoseMode == NoseMode.Bridge;

    public bool IsTipNoseModeActive => _activeNoseMode == NoseMode.Tip;

    public bool IsSlimNoseModeActive => _activeNoseMode == NoseMode.Slim;

    public bool IsNostrilNoseModeActive => _activeNoseMode == NoseMode.Nostril;

    public bool IsLengthNoseModeActive => _activeNoseMode == NoseMode.Length;

    public double ActiveNoseStrength
    {
        get => GetNoseStrength(_activeNoseMode);
        set
        {
            if (!SetNoseStrength(_activeNoseMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        NoseExpander.IsExpanded = false;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void BridgeNoseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNoseMode(NoseMode.Bridge, forceRefresh: true);
        e.Handled = true;
    }

    private void TipNoseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNoseMode(NoseMode.Tip, forceRefresh: true);
        e.Handled = true;
    }

    private void SlimNoseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNoseMode(NoseMode.Slim, forceRefresh: true);
        e.Handled = true;
    }

    private void NostrilNoseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNoseMode(NoseMode.Nostril, forceRefresh: true);
        e.Handled = true;
    }

    private void LengthNoseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveNoseMode(NoseMode.Length, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveNoseMode(NoseMode mode, bool forceRefresh = false)
    {
        if (_activeNoseMode == mode)
        {
            if (forceRefresh)
            {
                NotifyNoseModeProperties();
            }

            return;
        }

        _activeNoseMode = mode;
        NotifyNoseModeProperties();
        OnPropertyChanged(nameof(ActiveNoseStrength));
    }

    private void NotifyNoseModeProperties()
    {
        OnPropertyChanged(nameof(IsBridgeNoseModeActive));
        OnPropertyChanged(nameof(IsTipNoseModeActive));
        OnPropertyChanged(nameof(IsSlimNoseModeActive));
        OnPropertyChanged(nameof(IsNostrilNoseModeActive));
        OnPropertyChanged(nameof(IsLengthNoseModeActive));
    }

    private double GetNoseStrength(NoseMode mode)
    {
        return mode switch
        {
            NoseMode.Tip => _tipStrength,
            NoseMode.Slim => _slimStrength,
            NoseMode.Nostril => _nostrilStrength,
            NoseMode.Length => _lengthStrength,
            _ => _bridgeStrength
        };
    }

    private bool SetNoseStrength(NoseMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetNoseStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetNoseStrengthStorage(NoseMode mode)
    {
        switch (mode)
        {
            case NoseMode.Tip:
                return ref _tipStrength;
            case NoseMode.Slim:
                return ref _slimStrength;
            case NoseMode.Nostril:
                return ref _nostrilStrength;
            case NoseMode.Length:
                return ref _lengthStrength;
            default:
                return ref _bridgeStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
