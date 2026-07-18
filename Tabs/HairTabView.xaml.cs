using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

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
    private bool _canResetHairTab;
    private bool _isSliderInteracting;
    private string? _lastPreviewOperationId;
    private double _lastPreviewValue = double.NaN;

    public HairTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<HairAdjustmentEventArgs>? HairAdjustmentPreviewChanged;

    public event EventHandler<HairAdjustmentEventArgs>? HairAdjustmentCommitted;

    public event EventHandler? HairResetRequested;

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

    public bool CanResetHairTab
    {
        get => _canResetHairTab;
        set
        {
            if (_canResetHairTab == value)
            {
                return;
            }

            _canResetHairTab = value;
            OnPropertyChanged();
        }
    }

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
        ResetHairTabValues();
    }

    public void ResetAfterHistoryReset()
    {
        ResetHairTabValues();
    }

    public void RestoreSnapshot(HairAdjustmentSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            ResetHairTabValues();
            return;
        }

        _hairlineHeight = snapshot.HairlineHeight;
        _templeBalance = snapshot.TempleBalance;
        _babyHairProtect = snapshot.BabyHairProtect;
        _topVolume = snapshot.TopVolume;
        _sideVolume = snapshot.SideVolume;
        _crownLift = snapshot.CrownLift;
        _strayHair = snapshot.StrayHair;
        _frizz = snapshot.Frizz;
        _edgeCleanup = snapshot.EdgeCleanup;
        _shine = snapshot.Shine;
        _depth = snapshot.Depth;
        _scalpCover = snapshot.ScalpCover;
        _tint = snapshot.Tint;
        _warmCool = snapshot.WarmCool;
        _darken = snapshot.Darken;
        _colorStrength = snapshot.ColorStrength;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        UpdateCanResetHairTab();
        OnPropertyChanged(string.Empty);
    }

    private void ResetHairTabButton_Click(object sender, RoutedEventArgs e)
    {
        HairResetRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ResetHairTabValues()
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
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        CanResetHairTab = false;
        OnPropertyChanged(string.Empty);
    }

    private void HairSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSliderInteracting = true;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
    }

    private void HairSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        _isSliderInteracting = false;
        RaiseHairCommitted(GetOperationId(slider), slider.Value);
    }

    private void HairSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        RaiseHairPreview(operationId, previewValue);
    }

    private void HairSlider_KeyUp(object sender, KeyEventArgs e)
    {
        if (!IsSliderCommitKey(e.Key) || sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        RaiseHairCommitted(GetOperationId(slider), slider.Value);
    }

    private void RaiseHairPreview(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        HairAdjustmentPreviewChanged?.Invoke(
            this,
            new HairAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private void RaiseHairCommitted(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        HairAdjustmentCommitted?.Invoke(
            this,
            new HairAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private HairAdjustmentSnapshot CreateSnapshot()
    {
        return new HairAdjustmentSnapshot(
            HairlineHeight,
            TempleBalance,
            BabyHairProtect,
            TopVolume,
            SideVolume,
            CrownLift,
            StrayHair,
            Frizz,
            EdgeCleanup,
            Shine,
            Depth,
            ScalpCover,
            Tint,
            WarmCool,
            Darken,
            ColorStrength);
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
        UpdateCanResetHairTab();
        return true;
    }

    private void UpdateCanResetHairTab()
    {
        CanResetHairTab =
            IsNonDefault(_hairlineHeight, 50) ||
            IsNonDefault(_templeBalance, 50) ||
            IsNonDefault(_babyHairProtect, 50) ||
            IsNonDefault(_topVolume, 50) ||
            IsNonDefault(_sideVolume, 50) ||
            IsNonDefault(_crownLift, 50) ||
            IsNonDefault(_strayHair, 0) ||
            IsNonDefault(_frizz, 0) ||
            IsNonDefault(_edgeCleanup, 0) ||
            IsNonDefault(_shine, 0) ||
            IsNonDefault(_depth, 0) ||
            IsNonDefault(_scalpCover, 0) ||
            IsNonDefault(_tint, 0) ||
            IsNonDefault(_warmCool, 50) ||
            IsNonDefault(_darken, 0) ||
            IsNonDefault(_colorStrength, 0);
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

public sealed class HairAdjustmentEventArgs(
    string operationId,
    double value,
    HairAdjustmentSnapshot snapshot) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public HairAdjustmentSnapshot Snapshot { get; } = snapshot;
}

public sealed record HairAdjustmentSnapshot(
    double HairlineHeight,
    double TempleBalance,
    double BabyHairProtect,
    double TopVolume,
    double SideVolume,
    double CrownLift,
    double StrayHair,
    double Frizz,
    double EdgeCleanup,
    double Shine,
    double Depth,
    double ScalpCover,
    double Tint,
    double WarmCool,
    double Darken,
    double ColorStrength);
