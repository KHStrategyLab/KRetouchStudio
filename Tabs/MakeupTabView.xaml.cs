using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
    private bool _canResetMakeupTab;
    private bool _isSliderInteracting;
    private string? _lastPreviewOperationId;
    private double _lastPreviewValue = double.NaN;

    public MakeupTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<MakeupAdjustmentEventArgs>? MakeupAdjustmentPreviewChanged;

    public event EventHandler<MakeupAdjustmentEventArgs>? MakeupAdjustmentCommitted;

    public event EventHandler? MakeupResetRequested;

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

    public bool CanResetMakeupTab
    {
        get => _canResetMakeupTab;
        set
        {
            if (_canResetMakeupTab == value)
            {
                return;
            }

            _canResetMakeupTab = value;
            OnPropertyChanged();
        }
    }

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
        ResetMakeupTabValues();
    }

    public void ResetAfterHistoryReset()
    {
        ResetMakeupTabValues();
    }

    public void RestoreSnapshot(MakeupAdjustmentSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            ResetMakeupTabValues();
            return;
        }

        _baseCoverage = snapshot.BaseCoverage;
        _baseEvenness = snapshot.BaseEvenness;
        _baseFinish = snapshot.BaseFinish;
        _browDensity = snapshot.BrowDensity;
        _browShape = snapshot.BrowShape;
        _browColor = snapshot.BrowColor;
        _eyeShadow = snapshot.EyeShadow;
        _eyeLiner = snapshot.EyeLiner;
        _eyeLash = snapshot.EyeLash;
        _cheekBlush = snapshot.CheekBlush;
        _cheekContour = snapshot.CheekContour;
        _cheekHighlight = snapshot.CheekHighlight;
        _lipColor = snapshot.LipColor;
        _lipSaturation = snapshot.LipSaturation;
        _lipGloss = snapshot.LipGloss;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        UpdateCanResetMakeupTab();
        OnPropertyChanged(string.Empty);
    }

    private void ResetMakeupTabButton_Click(object sender, RoutedEventArgs e)
    {
        MakeupResetRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ResetMakeupTabValues()
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
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        CanResetMakeupTab = false;
        OnPropertyChanged(string.Empty);
    }

    private void MakeupSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSliderInteracting = true;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
    }

    private void MakeupSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        _isSliderInteracting = false;
        RaiseMakeupCommitted(GetOperationId(slider), slider.Value);
    }

    private void MakeupSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        RaiseMakeupPreview(operationId, previewValue);
    }

    private void MakeupSlider_KeyUp(object sender, KeyEventArgs e)
    {
        if (!IsSliderCommitKey(e.Key) || sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        RaiseMakeupCommitted(GetOperationId(slider), slider.Value);
    }

    private void RaiseMakeupPreview(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        MakeupAdjustmentPreviewChanged?.Invoke(
            this,
            new MakeupAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private void RaiseMakeupCommitted(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        MakeupAdjustmentCommitted?.Invoke(
            this,
            new MakeupAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private MakeupAdjustmentSnapshot CreateSnapshot()
    {
        return new MakeupAdjustmentSnapshot(
            BaseCoverage,
            BaseEvenness,
            BaseFinish,
            BrowDensity,
            BrowShape,
            BrowColor,
            EyeShadow,
            EyeLiner,
            EyeLash,
            CheekBlush,
            CheekContour,
            CheekHighlight,
            LipColor,
            LipSaturation,
            LipGloss);
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
        UpdateCanResetMakeupTab();
        return true;
    }

    private void UpdateCanResetMakeupTab()
    {
        CanResetMakeupTab =
            IsNonDefault(_baseCoverage, 0) ||
            IsNonDefault(_baseEvenness, 0) ||
            IsNonDefault(_baseFinish, 0) ||
            IsNonDefault(_browDensity, 0) ||
            IsNonDefault(_browShape, 0) ||
            IsNonDefault(_browColor, 0) ||
            IsNonDefault(_eyeShadow, 0) ||
            IsNonDefault(_eyeLiner, 0) ||
            IsNonDefault(_eyeLash, 0) ||
            IsNonDefault(_cheekBlush, 0) ||
            IsNonDefault(_cheekContour, 0) ||
            IsNonDefault(_cheekHighlight, 0) ||
            IsNonDefault(_lipColor, 0) ||
            IsNonDefault(_lipSaturation, 0) ||
            IsNonDefault(_lipGloss, 0);
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

public sealed class MakeupAdjustmentEventArgs(
    string operationId,
    double value,
    MakeupAdjustmentSnapshot snapshot) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public MakeupAdjustmentSnapshot Snapshot { get; } = snapshot;
}

public sealed record MakeupAdjustmentSnapshot(
    double BaseCoverage,
    double BaseEvenness,
    double BaseFinish,
    double BrowDensity,
    double BrowShape,
    double BrowColor,
    double EyeShadow,
    double EyeLiner,
    double EyeLash,
    double CheekBlush,
    double CheekContour,
    double CheekHighlight,
    double LipColor,
    double LipSaturation,
    double LipGloss);
