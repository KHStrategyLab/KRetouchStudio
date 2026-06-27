using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class MakeupTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum MakeupMode
    {
        Base,
        Brow,
        Eye,
        Cheek,
        Lip
    }

    private MakeupMode _activeMakeupMode = MakeupMode.Base;
    private double _baseCoverage;
    private double _baseEvenness;
    private double _baseFinish;
    private double _browDensity;
    private double _browShape;
    private double _browColor;
    private double _eyeShadow;
    private double _eyeLiner;
    private double _eyeLash;
    private double _cheekBlush;
    private double _cheekContour;
    private double _cheekHighlight;
    private double _lipColor;
    private double _lipSaturation;
    private double _lipGloss;

    public MakeupTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBaseModeActive => _activeMakeupMode == MakeupMode.Base;
    public bool IsBrowModeActive => _activeMakeupMode == MakeupMode.Brow;
    public bool IsEyeModeActive => _activeMakeupMode == MakeupMode.Eye;
    public bool IsCheekModeActive => _activeMakeupMode == MakeupMode.Cheek;
    public bool IsLipModeActive => _activeMakeupMode == MakeupMode.Lip;

    public Visibility BasePanelVisibility => GetPanelVisibility(MakeupMode.Base);
    public Visibility BrowPanelVisibility => GetPanelVisibility(MakeupMode.Brow);
    public Visibility EyePanelVisibility => GetPanelVisibility(MakeupMode.Eye);
    public Visibility CheekPanelVisibility => GetPanelVisibility(MakeupMode.Cheek);
    public Visibility LipPanelVisibility => GetPanelVisibility(MakeupMode.Lip);

    public double BaseCoverage { get => _baseCoverage; set => SetSliderValue(ref _baseCoverage, value); }
    public double BaseEvenness { get => _baseEvenness; set => SetSliderValue(ref _baseEvenness, value); }
    public double BaseFinish { get => _baseFinish; set => SetSliderValue(ref _baseFinish, value); }
    public double BrowDensity { get => _browDensity; set => SetSliderValue(ref _browDensity, value); }
    public double BrowShape { get => _browShape; set => SetSliderValue(ref _browShape, value); }
    public double BrowColor { get => _browColor; set => SetSliderValue(ref _browColor, value); }
    public double EyeShadow { get => _eyeShadow; set => SetSliderValue(ref _eyeShadow, value); }
    public double EyeLiner { get => _eyeLiner; set => SetSliderValue(ref _eyeLiner, value); }
    public double EyeLash { get => _eyeLash; set => SetSliderValue(ref _eyeLash, value); }
    public double CheekBlush { get => _cheekBlush; set => SetSliderValue(ref _cheekBlush, value); }
    public double CheekContour { get => _cheekContour; set => SetSliderValue(ref _cheekContour, value); }
    public double CheekHighlight { get => _cheekHighlight; set => SetSliderValue(ref _cheekHighlight, value); }
    public double LipColor { get => _lipColor; set => SetSliderValue(ref _lipColor, value); }
    public double LipSaturation { get => _lipSaturation; set => SetSliderValue(ref _lipSaturation, value); }
    public double LipGloss { get => _lipGloss; set => SetSliderValue(ref _lipGloss, value); }

    public void Collapse()
    {
        MakeupExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeMakeupMode = MakeupMode.Base;
        _baseCoverage = 0;
        _baseEvenness = 0;
        _baseFinish = 0;
        _browDensity = 0;
        _browShape = 0;
        _browColor = 0;
        _eyeShadow = 0;
        _eyeLiner = 0;
        _eyeLash = 0;
        _cheekBlush = 0;
        _cheekContour = 0;
        _cheekHighlight = 0;
        _lipColor = 0;
        _lipSaturation = 0;
        _lipGloss = 0;
        OnPropertyChanged(string.Empty);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Base);
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void BaseButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Base);
        e.Handled = true;
    }

    private void BrowButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Brow);
        e.Handled = true;
    }

    private void EyeButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Eye);
        e.Handled = true;
    }

    private void CheekButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Cheek);
        e.Handled = true;
    }

    private void LipButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMakeupMode(MakeupMode.Lip);
        e.Handled = true;
    }

    private void SetActiveMakeupMode(MakeupMode mode)
    {
        if (_activeMakeupMode == mode)
        {
            NotifyMakeupModeProperties();
            return;
        }

        _activeMakeupMode = mode;
        NotifyMakeupModeProperties();
    }

    private Visibility GetPanelVisibility(MakeupMode mode)
    {
        return _activeMakeupMode == mode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotifyMakeupModeProperties()
    {
        OnPropertyChanged(nameof(IsBaseModeActive));
        OnPropertyChanged(nameof(IsBrowModeActive));
        OnPropertyChanged(nameof(IsEyeModeActive));
        OnPropertyChanged(nameof(IsCheekModeActive));
        OnPropertyChanged(nameof(IsLipModeActive));
        OnPropertyChanged(nameof(BasePanelVisibility));
        OnPropertyChanged(nameof(BrowPanelVisibility));
        OnPropertyChanged(nameof(EyePanelVisibility));
        OnPropertyChanged(nameof(CheekPanelVisibility));
        OnPropertyChanged(nameof(LipPanelVisibility));
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
