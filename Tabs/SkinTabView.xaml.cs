using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class SkinTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum SkinMode
    {
        Tone,
        Smooth,
        Pore,
        Redness,
        Shine
    }

    private SkinMode _activeSkinMode = SkinMode.Tone;
    private double _evenTone;
    private double _toneLift;
    private double _colorCast;
    private double _softness;
    private double _textureProtect = 50;
    private double _detailReturn = 50;
    private double _poreReduce;
    private double _fineTexture = 50;
    private double _poreEdgeProtect = 50;
    private double _redReduce;
    private double _toneBlend;
    private double _naturalColor = 50;
    private double _shineReduce;
    private double _highlightProtect = 50;
    private double _shineTextureReturn = 50;

    public SkinTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsToneModeActive => _activeSkinMode == SkinMode.Tone;

    public bool IsSmoothModeActive => _activeSkinMode == SkinMode.Smooth;

    public bool IsPoreModeActive => _activeSkinMode == SkinMode.Pore;

    public bool IsRednessModeActive => _activeSkinMode == SkinMode.Redness;

    public bool IsShineModeActive => _activeSkinMode == SkinMode.Shine;

    public Visibility TonePanelVisibility => GetPanelVisibility(SkinMode.Tone);

    public Visibility SmoothPanelVisibility => GetPanelVisibility(SkinMode.Smooth);

    public Visibility PorePanelVisibility => GetPanelVisibility(SkinMode.Pore);

    public Visibility RednessPanelVisibility => GetPanelVisibility(SkinMode.Redness);

    public Visibility ShinePanelVisibility => GetPanelVisibility(SkinMode.Shine);

    public double EvenTone
    {
        get => _evenTone;
        set => SetSliderValue(ref _evenTone, value);
    }

    public double ToneLift
    {
        get => _toneLift;
        set => SetSliderValue(ref _toneLift, value);
    }

    public double ColorCast
    {
        get => _colorCast;
        set => SetSliderValue(ref _colorCast, value);
    }

    public double Softness
    {
        get => _softness;
        set => SetSliderValue(ref _softness, value);
    }

    public double TextureProtect
    {
        get => _textureProtect;
        set => SetSliderValue(ref _textureProtect, value);
    }

    public double DetailReturn
    {
        get => _detailReturn;
        set => SetSliderValue(ref _detailReturn, value);
    }

    public double PoreReduce
    {
        get => _poreReduce;
        set => SetSliderValue(ref _poreReduce, value);
    }

    public double FineTexture
    {
        get => _fineTexture;
        set => SetSliderValue(ref _fineTexture, value);
    }

    public double PoreEdgeProtect
    {
        get => _poreEdgeProtect;
        set => SetSliderValue(ref _poreEdgeProtect, value);
    }

    public double RedReduce
    {
        get => _redReduce;
        set => SetSliderValue(ref _redReduce, value);
    }

    public double ToneBlend
    {
        get => _toneBlend;
        set => SetSliderValue(ref _toneBlend, value);
    }

    public double NaturalColor
    {
        get => _naturalColor;
        set => SetSliderValue(ref _naturalColor, value);
    }

    public double ShineReduce
    {
        get => _shineReduce;
        set => SetSliderValue(ref _shineReduce, value);
    }

    public double HighlightProtect
    {
        get => _highlightProtect;
        set => SetSliderValue(ref _highlightProtect, value);
    }

    public double ShineTextureReturn
    {
        get => _shineTextureReturn;
        set => SetSliderValue(ref _shineTextureReturn, value);
    }

    public void Collapse()
    {
        SkinExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeSkinMode = SkinMode.Tone;
        _evenTone = 0;
        _toneLift = 0;
        _colorCast = 0;
        _softness = 0;
        _textureProtect = 50;
        _detailReturn = 50;
        _poreReduce = 0;
        _fineTexture = 50;
        _poreEdgeProtect = 50;
        _redReduce = 0;
        _toneBlend = 0;
        _naturalColor = 50;
        _shineReduce = 0;
        _highlightProtect = 50;
        _shineTextureReturn = 50;
        OnPropertyChanged(string.Empty);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Tone);
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void ToneButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Tone);
        e.Handled = true;
    }

    private void SmoothButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Smooth);
        e.Handled = true;
    }

    private void PoreButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Pore);
        e.Handled = true;
    }

    private void RednessButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Redness);
        e.Handled = true;
    }

    private void ShineButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveSkinMode(SkinMode.Shine);
        e.Handled = true;
    }

    private void SetActiveSkinMode(SkinMode mode)
    {
        if (_activeSkinMode == mode)
        {
            NotifySkinModeProperties();
            return;
        }

        _activeSkinMode = mode;
        NotifySkinModeProperties();
    }

    private Visibility GetPanelVisibility(SkinMode mode)
    {
        return _activeSkinMode == mode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotifySkinModeProperties()
    {
        OnPropertyChanged(nameof(IsToneModeActive));
        OnPropertyChanged(nameof(IsSmoothModeActive));
        OnPropertyChanged(nameof(IsPoreModeActive));
        OnPropertyChanged(nameof(IsRednessModeActive));
        OnPropertyChanged(nameof(IsShineModeActive));
        OnPropertyChanged(nameof(TonePanelVisibility));
        OnPropertyChanged(nameof(SmoothPanelVisibility));
        OnPropertyChanged(nameof(PorePanelVisibility));
        OnPropertyChanged(nameof(RednessPanelVisibility));
        OnPropertyChanged(nameof(ShinePanelVisibility));
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
