using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class HairTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum HairMode
    {
        Hairline,
        Volume,
        Cleanup,
        Tone,
        Color
    }

    private HairMode _activeHairMode = HairMode.Hairline;
    private double _hairlineHeight = 50;
    private double _templeBalance = 50;
    private double _babyHairProtect = 50;
    private double _topVolume = 50;
    private double _sideVolume = 50;
    private double _crownLift = 50;
    private double _strayHair;
    private double _frizz;
    private double _edgeCleanup;
    private double _shine;
    private double _depth;
    private double _scalpCover;
    private double _tint;
    private double _warmCool = 50;
    private double _darken;
    private double _colorStrength;

    public HairTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsHairlineModeActive => _activeHairMode == HairMode.Hairline;

    public bool IsVolumeModeActive => _activeHairMode == HairMode.Volume;

    public bool IsCleanupModeActive => _activeHairMode == HairMode.Cleanup;

    public bool IsToneModeActive => _activeHairMode == HairMode.Tone;

    public bool IsColorModeActive => _activeHairMode == HairMode.Color;

    public Visibility HairlinePanelVisibility => GetPanelVisibility(HairMode.Hairline);

    public Visibility VolumePanelVisibility => GetPanelVisibility(HairMode.Volume);

    public Visibility CleanupPanelVisibility => GetPanelVisibility(HairMode.Cleanup);

    public Visibility TonePanelVisibility => GetPanelVisibility(HairMode.Tone);

    public Visibility ColorPanelVisibility => GetPanelVisibility(HairMode.Color);

    public double HairlineHeight
    {
        get => _hairlineHeight;
        set => SetSliderValue(ref _hairlineHeight, value);
    }

    public double TempleBalance
    {
        get => _templeBalance;
        set => SetSliderValue(ref _templeBalance, value);
    }

    public double BabyHairProtect
    {
        get => _babyHairProtect;
        set => SetSliderValue(ref _babyHairProtect, value);
    }

    public double TopVolume
    {
        get => _topVolume;
        set => SetSliderValue(ref _topVolume, value);
    }

    public double SideVolume
    {
        get => _sideVolume;
        set => SetSliderValue(ref _sideVolume, value);
    }

    public double CrownLift
    {
        get => _crownLift;
        set => SetSliderValue(ref _crownLift, value);
    }

    public double StrayHair
    {
        get => _strayHair;
        set => SetSliderValue(ref _strayHair, value);
    }

    public double Frizz
    {
        get => _frizz;
        set => SetSliderValue(ref _frizz, value);
    }

    public double EdgeCleanup
    {
        get => _edgeCleanup;
        set => SetSliderValue(ref _edgeCleanup, value);
    }

    public double Shine
    {
        get => _shine;
        set => SetSliderValue(ref _shine, value);
    }

    public double Depth
    {
        get => _depth;
        set => SetSliderValue(ref _depth, value);
    }

    public double ScalpCover
    {
        get => _scalpCover;
        set => SetSliderValue(ref _scalpCover, value);
    }

    public double Tint
    {
        get => _tint;
        set => SetSliderValue(ref _tint, value);
    }

    public double WarmCool
    {
        get => _warmCool;
        set => SetSliderValue(ref _warmCool, value);
    }

    public double Darken
    {
        get => _darken;
        set => SetSliderValue(ref _darken, value);
    }

    public double ColorStrength
    {
        get => _colorStrength;
        set => SetSliderValue(ref _colorStrength, value);
    }

    public void Collapse()
    {
        HairExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeHairMode = HairMode.Hairline;
        _hairlineHeight = 50;
        _templeBalance = 50;
        _babyHairProtect = 50;
        _topVolume = 50;
        _sideVolume = 50;
        _crownLift = 50;
        _strayHair = 0;
        _frizz = 0;
        _edgeCleanup = 0;
        _shine = 0;
        _depth = 0;
        _scalpCover = 0;
        _tint = 0;
        _warmCool = 50;
        _darken = 0;
        _colorStrength = 0;
        OnPropertyChanged(string.Empty);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Hairline);
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void HairlineButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Hairline);
        e.Handled = true;
    }

    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Volume);
        e.Handled = true;
    }

    private void CleanupButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Cleanup);
        e.Handled = true;
    }

    private void ToneButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Tone);
        e.Handled = true;
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveHairMode(HairMode.Color);
        e.Handled = true;
    }

    private void SetActiveHairMode(HairMode mode)
    {
        if (_activeHairMode == mode)
        {
            NotifyHairModeProperties();
            return;
        }

        _activeHairMode = mode;
        NotifyHairModeProperties();
    }

    private Visibility GetPanelVisibility(HairMode mode)
    {
        return _activeHairMode == mode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotifyHairModeProperties()
    {
        OnPropertyChanged(nameof(IsHairlineModeActive));
        OnPropertyChanged(nameof(IsVolumeModeActive));
        OnPropertyChanged(nameof(IsCleanupModeActive));
        OnPropertyChanged(nameof(IsToneModeActive));
        OnPropertyChanged(nameof(IsColorModeActive));
        OnPropertyChanged(nameof(HairlinePanelVisibility));
        OnPropertyChanged(nameof(VolumePanelVisibility));
        OnPropertyChanged(nameof(CleanupPanelVisibility));
        OnPropertyChanged(nameof(TonePanelVisibility));
        OnPropertyChanged(nameof(ColorPanelVisibility));
    }

    private bool SetSliderValue(ref double storage, double value, [CallerMemberName] string? propertyName = null)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
