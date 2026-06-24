using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class SkinTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum SkinMode
    {
        Heal,
        Acne,
        Mole,
        Freck,
        Smooth
    }

    private SkinMode _activeSkinMode = SkinMode.Heal;
    private double _healStrength;
    private double _acneStrength;
    private double _moleStrength;
    private double _freckStrength;
    private double _smoothStrength;

    public SkinTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsHealSkinModeActive => _activeSkinMode == SkinMode.Heal;

    public bool IsAcneSkinModeActive => _activeSkinMode == SkinMode.Acne;

    public bool IsMoleSkinModeActive => _activeSkinMode == SkinMode.Mole;

    public bool IsFreckSkinModeActive => _activeSkinMode == SkinMode.Freck;

    public bool IsSmoothSkinModeActive => _activeSkinMode == SkinMode.Smooth;

    public double ActiveSkinStrength
    {
        get => GetSkinStrength(_activeSkinMode);
        set
        {
            if (!SetSkinStrength(_activeSkinMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        SkinExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeSkinMode = SkinMode.Heal;
        _healStrength = 0;
        _acneStrength = 0;
        _moleStrength = 0;
        _freckStrength = 0;
        _smoothStrength = 0;

        NotifySkinModeProperties();
        OnPropertyChanged(nameof(ActiveSkinStrength));
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void HealSkinButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Heal, forceRefresh: true);
        e.Handled = true;
    }

    private void AcneSkinButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Acne, forceRefresh: true);
        e.Handled = true;
    }

    private void MoleSkinButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Mole, forceRefresh: true);
        e.Handled = true;
    }

    private void FreckSkinButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Freck, forceRefresh: true);
        e.Handled = true;
    }

    private void SmoothSkinButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Smooth, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveSkinMode(SkinMode mode, bool forceRefresh = false)
    {
        if (_activeSkinMode == mode)
        {
            if (forceRefresh)
            {
                NotifySkinModeProperties();
            }

            return;
        }

        _activeSkinMode = mode;
        NotifySkinModeProperties();
        OnPropertyChanged(nameof(ActiveSkinStrength));
    }

    private void NotifySkinModeProperties()
    {
        OnPropertyChanged(nameof(IsHealSkinModeActive));
        OnPropertyChanged(nameof(IsAcneSkinModeActive));
        OnPropertyChanged(nameof(IsMoleSkinModeActive));
        OnPropertyChanged(nameof(IsFreckSkinModeActive));
        OnPropertyChanged(nameof(IsSmoothSkinModeActive));
    }

    private double GetSkinStrength(SkinMode mode)
    {
        return mode switch
        {
            SkinMode.Acne => _acneStrength,
            SkinMode.Mole => _moleStrength,
            SkinMode.Freck => _freckStrength,
            SkinMode.Smooth => _smoothStrength,
            _ => _healStrength
        };
    }

    private bool SetSkinStrength(SkinMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetSkinStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetSkinStrengthStorage(SkinMode mode)
    {
        switch (mode)
        {
            case SkinMode.Acne:
                return ref _acneStrength;
            case SkinMode.Mole:
                return ref _moleStrength;
            case SkinMode.Freck:
                return ref _freckStrength;
            case SkinMode.Smooth:
                return ref _smoothStrength;
            default:
                return ref _healStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
