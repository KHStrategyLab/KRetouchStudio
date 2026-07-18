using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
    private double _textureProtect;
    private double _detailReturn;
    private double _poreReduce;
    private double _fineTexture;
    private double _poreEdgeProtect;
    private double _redReduce;
    private double _toneBlend;
    private double _naturalColor;
    private double _shineReduce;
    private double _highlightProtect;
    private double _shineTextureReturn;
    private bool _canResetSkinTab;
    private bool _isSliderInteracting;
    private string? _lastPreviewOperationId;
    private double _lastPreviewValue = double.NaN;

    public SkinTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<SkinAdjustmentEventArgs>? SkinAdjustmentPreviewChanged;

    public event EventHandler<SkinAdjustmentEventArgs>? SkinAdjustmentCommitted;

    public event EventHandler? SkinResetRequested;

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

    public bool CanResetSkinTab
    {
        get => _canResetSkinTab;
        set
        {
            if (_canResetSkinTab == value)
            {
                return;
            }

            _canResetSkinTab = value;
            OnPropertyChanged();
        }
    }

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
        ResetSkinTabValues();
    }

    public void ResetAfterHistoryReset()
    {
        ResetSkinTabValues();
    }

    public void RestoreSnapshot(SkinAdjustmentSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            ResetSkinTabValues();
            return;
        }

        _evenTone = snapshot.EvenTone;
        _toneLift = snapshot.ToneLift;
        _colorCast = snapshot.ColorCast;
        _softness = snapshot.Softness;
        _textureProtect = snapshot.TextureProtect;
        _detailReturn = snapshot.DetailReturn;
        _poreReduce = snapshot.PoreReduce;
        _fineTexture = snapshot.FineTexture;
        _poreEdgeProtect = snapshot.PoreEdgeProtect;
        _redReduce = snapshot.RedReduce;
        _toneBlend = snapshot.ToneBlend;
        _naturalColor = snapshot.NaturalColor;
        _shineReduce = snapshot.ShineReduce;
        _highlightProtect = snapshot.HighlightProtect;
        _shineTextureReturn = snapshot.ShineTextureReturn;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        UpdateCanResetSkinTab();
        OnPropertyChanged(string.Empty);
    }

    private void ResetSkinTabButton_Click(object sender, RoutedEventArgs e)
    {
        SkinResetRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ResetSkinTabValues()
    {
        _activeSkinMode = SkinMode.Tone;
        _evenTone = 0;
        _toneLift = 0;
        _colorCast = 0;
        _softness = 0;
        _textureProtect = 0;
        _detailReturn = 0;
        _poreReduce = 0;
        _fineTexture = 0;
        _poreEdgeProtect = 0;
        _redReduce = 0;
        _toneBlend = 0;
        _naturalColor = 0;
        _shineReduce = 0;
        _highlightProtect = 0;
        _shineTextureReturn = 0;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        CanResetSkinTab = false;
        OnPropertyChanged(string.Empty);
    }

    private void SkinSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSliderInteracting = true;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
    }

    private void SkinSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        _isSliderInteracting = false;
        RaiseSkinCommitted(GetOperationId(slider), slider.Value);
    }

    private void SkinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        if (!_isSliderInteracting &&
            (Mouse.LeftButton != MouseButtonState.Pressed || !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!_isSliderInteracting)
        {
            _isSliderInteracting = true;
            _lastPreviewOperationId = null;
            _lastPreviewValue = double.NaN;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        string operationId = GetOperationId(slider);
        double previewValue = Math.Clamp(Math.Round(e.NewValue), 0, 100);
        if (string.IsNullOrWhiteSpace(operationId) ||
            (string.Equals(_lastPreviewOperationId, operationId, StringComparison.Ordinal) &&
             Math.Abs(_lastPreviewValue - previewValue) <= 0.001))
        {
            return;
        }

        _lastPreviewOperationId = operationId;
        _lastPreviewValue = previewValue;
        RaiseSkinPreview(operationId, previewValue);
    }

    private void SkinSlider_KeyUp(object sender, KeyEventArgs e)
    {
        if (!IsSliderCommitKey(e.Key) || sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        RaiseSkinCommitted(GetOperationId(slider), slider.Value);
    }

    private void RaiseSkinPreview(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        SkinAdjustmentPreviewChanged?.Invoke(
            this,
            new SkinAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private void RaiseSkinCommitted(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        SkinAdjustmentCommitted?.Invoke(
            this,
            new SkinAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private SkinAdjustmentSnapshot CreateSnapshot()
    {
        return new SkinAdjustmentSnapshot(
            EvenTone,
            ToneLift,
            ColorCast,
            Softness,
            TextureProtect,
            DetailReturn,
            PoreReduce,
            FineTexture,
            PoreEdgeProtect,
            RedReduce,
            ToneBlend,
            NaturalColor,
            ShineReduce,
            HighlightProtect,
            ShineTextureReturn);
    }

    private static string GetOperationId(Slider slider)
    {
        return slider.Tag as string ?? string.Empty;
    }

    private static bool IsSliderCommitKey(Key key)
    {
        return key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown;
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
        UpdateCanResetSkinTab();
        return true;
    }

    private void UpdateCanResetSkinTab()
    {
        CanResetSkinTab =
            IsNonDefault(_evenTone, 0) ||
            IsNonDefault(_toneLift, 0) ||
            IsNonDefault(_colorCast, 0) ||
            IsNonDefault(_softness, 0) ||
            IsNonDefault(_textureProtect, 0) ||
            IsNonDefault(_detailReturn, 0) ||
            IsNonDefault(_poreReduce, 0) ||
            IsNonDefault(_fineTexture, 0) ||
            IsNonDefault(_poreEdgeProtect, 0) ||
            IsNonDefault(_redReduce, 0) ||
            IsNonDefault(_toneBlend, 0) ||
            IsNonDefault(_naturalColor, 0) ||
            IsNonDefault(_shineReduce, 0) ||
            IsNonDefault(_highlightProtect, 0) ||
            IsNonDefault(_shineTextureReturn, 0);
    }

    private static bool IsNonDefault(double value, double defaultValue)
    {
        return Math.Abs(value - defaultValue) > 0.01;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class SkinAdjustmentEventArgs(
    string operationId,
    double value,
    SkinAdjustmentSnapshot snapshot) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public SkinAdjustmentSnapshot Snapshot { get; } = snapshot;
}

public sealed record SkinAdjustmentSnapshot(
    double EvenTone,
    double ToneLift,
    double ColorCast,
    double Softness,
    double TextureProtect,
    double DetailReturn,
    double PoreReduce,
    double FineTexture,
    double PoreEdgeProtect,
    double RedReduce,
    double ToneBlend,
    double NaturalColor,
    double ShineReduce,
    double HighlightProtect,
    double ShineTextureReturn);
