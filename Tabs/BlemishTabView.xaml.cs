using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace KRetouchStudio.Tabs;

public partial class BlemishTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum BlemishMode
    {
        Acne,
        Spot,
        Mole,
        Freckle,
        Scar
    }

    private BlemishMode _activeBlemishMode = BlemishMode.Acne;
    private double _acneReduce;
    private double _acneRedness;
    private double _acneBump;
    private double _spotRemove;
    private double _spotBlend;
    private double _spotTextureMatch;
    private double _moleReduce;
    private double _moleProtect;
    private double _moleEdgeBlend;
    private double _freckleFade;
    private double _freckleDensity;
    private double _freckleProtect;
    private double _scarSoften;
    private double _scarToneBlend;
    private double _scarTextureMatch;
    private bool _canResetBlemishTab;
    private bool _isSliderInteracting;
    private string? _lastPreviewOperationId;
    private double _lastPreviewValue = double.NaN;

    public BlemishTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<BlemishAdjustmentEventArgs>? BlemishAdjustmentPreviewChanged;

    public event EventHandler<BlemishAdjustmentEventArgs>? BlemishAdjustmentCommitted;

    public event EventHandler? BlemishResetRequested;

    public bool IsAcneModeActive => _activeBlemishMode == BlemishMode.Acne;
    public bool IsSpotModeActive => _activeBlemishMode == BlemishMode.Spot;
    public bool IsMoleModeActive => _activeBlemishMode == BlemishMode.Mole;
    public bool IsFreckleModeActive => _activeBlemishMode == BlemishMode.Freckle;
    public bool IsScarModeActive => _activeBlemishMode == BlemishMode.Scar;

    public Visibility AcnePanelVisibility => GetPanelVisibility(BlemishMode.Acne);
    public Visibility SpotPanelVisibility => GetPanelVisibility(BlemishMode.Spot);
    public Visibility MolePanelVisibility => GetPanelVisibility(BlemishMode.Mole);
    public Visibility FrecklePanelVisibility => GetPanelVisibility(BlemishMode.Freckle);
    public Visibility ScarPanelVisibility => GetPanelVisibility(BlemishMode.Scar);

    public bool CanResetBlemishTab
    {
        get => _canResetBlemishTab;
        set
        {
            if (_canResetBlemishTab == value)
            {
                return;
            }

            _canResetBlemishTab = value;
            OnPropertyChanged();
        }
    }

    public double AcneReduce { get => _acneReduce; set => SetSliderValue(ref _acneReduce, value); }
    public double AcneRedness { get => _acneRedness; set => SetSliderValue(ref _acneRedness, value); }
    public double AcneBump { get => _acneBump; set => SetSliderValue(ref _acneBump, value); }
    public double SpotRemove { get => _spotRemove; set => SetSliderValue(ref _spotRemove, value); }
    public double SpotBlend { get => _spotBlend; set => SetSliderValue(ref _spotBlend, value); }
    public double SpotTextureMatch { get => _spotTextureMatch; set => SetSliderValue(ref _spotTextureMatch, value); }
    public double MoleReduce { get => _moleReduce; set => SetSliderValue(ref _moleReduce, value); }
    public double MoleProtect { get => _moleProtect; set => SetSliderValue(ref _moleProtect, value); }
    public double MoleEdgeBlend { get => _moleEdgeBlend; set => SetSliderValue(ref _moleEdgeBlend, value); }
    public double FreckleFade { get => _freckleFade; set => SetSliderValue(ref _freckleFade, value); }
    public double FreckleDensity { get => _freckleDensity; set => SetSliderValue(ref _freckleDensity, value); }
    public double FreckleProtect { get => _freckleProtect; set => SetSliderValue(ref _freckleProtect, value); }
    public double ScarSoften { get => _scarSoften; set => SetSliderValue(ref _scarSoften, value); }
    public double ScarToneBlend { get => _scarToneBlend; set => SetSliderValue(ref _scarToneBlend, value); }
    public double ScarTextureMatch { get => _scarTextureMatch; set => SetSliderValue(ref _scarTextureMatch, value); }

    public void Collapse()
    {
        BlemishExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        ResetBlemishTabValues();
    }

    public void ResetAfterHistoryReset()
    {
        ResetBlemishTabValues();
    }

    public void RestoreSnapshot(BlemishAdjustmentSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            ResetBlemishTabValues();
            return;
        }

        _acneReduce = snapshot.AcneReduce;
        _acneRedness = snapshot.AcneRedness;
        _acneBump = snapshot.AcneBump;
        _spotRemove = snapshot.SpotRemove;
        _spotBlend = snapshot.SpotBlend;
        _spotTextureMatch = snapshot.SpotTextureMatch;
        _moleReduce = snapshot.MoleReduce;
        _moleProtect = snapshot.MoleProtect;
        _moleEdgeBlend = snapshot.MoleEdgeBlend;
        _freckleFade = snapshot.FreckleFade;
        _freckleDensity = snapshot.FreckleDensity;
        _freckleProtect = snapshot.FreckleProtect;
        _scarSoften = snapshot.ScarSoften;
        _scarToneBlend = snapshot.ScarToneBlend;
        _scarTextureMatch = snapshot.ScarTextureMatch;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        UpdateCanResetBlemishTab();
        OnPropertyChanged(string.Empty);
    }

    private void ResetBlemishTabButton_Click(object sender, RoutedEventArgs e)
    {
        BlemishResetRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ResetBlemishTabValues()
    {
        _activeBlemishMode = BlemishMode.Acne;
        _acneReduce = 0;
        _acneRedness = 0;
        _acneBump = 0;
        _spotRemove = 0;
        _spotBlend = 0;
        _spotTextureMatch = 0;
        _moleReduce = 0;
        _moleProtect = 0;
        _moleEdgeBlend = 0;
        _freckleFade = 0;
        _freckleDensity = 0;
        _freckleProtect = 0;
        _scarSoften = 0;
        _scarToneBlend = 0;
        _scarTextureMatch = 0;
        _isSliderInteracting = false;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
        CanResetBlemishTab = false;
        OnPropertyChanged(string.Empty);
    }

    private void BlemishSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSliderInteracting = true;
        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
    }

    private void BlemishSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        _isSliderInteracting = false;
        RaiseBlemishCommitted(GetOperationId(slider), slider.Value);
    }

    private void BlemishSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        RaiseBlemishPreview(operationId, previewValue);
    }

    private void BlemishSlider_KeyUp(object sender, KeyEventArgs e)
    {
        if (!IsSliderCommitKey(e.Key) || sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        RaiseBlemishCommitted(GetOperationId(slider), slider.Value);
    }

    private void RaiseBlemishPreview(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        BlemishAdjustmentPreviewChanged?.Invoke(
            this,
            new BlemishAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private void RaiseBlemishCommitted(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        BlemishAdjustmentCommitted?.Invoke(
            this,
            new BlemishAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private BlemishAdjustmentSnapshot CreateSnapshot()
    {
        return new BlemishAdjustmentSnapshot(
            AcneReduce,
            AcneRedness,
            AcneBump,
            SpotRemove,
            SpotBlend,
            SpotTextureMatch,
            MoleReduce,
            MoleProtect,
            MoleEdgeBlend,
            FreckleFade,
            FreckleDensity,
            FreckleProtect,
            ScarSoften,
            ScarToneBlend,
            ScarTextureMatch);
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
        SetActiveBlemishMode(BlemishMode.Acne);
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void AcneButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBlemishMode(BlemishMode.Acne);
        e.Handled = true;
    }

    private void SpotButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBlemishMode(BlemishMode.Spot);
        e.Handled = true;
    }

    private void MoleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBlemishMode(BlemishMode.Mole);
        e.Handled = true;
    }

    private void FreckleButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBlemishMode(BlemishMode.Freckle);
        e.Handled = true;
    }

    private void ScarButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBlemishMode(BlemishMode.Scar);
        e.Handled = true;
    }

    private void SetActiveBlemishMode(BlemishMode mode)
    {
        if (_activeBlemishMode == mode)
        {
            NotifyBlemishModeProperties();
            return;
        }

        _activeBlemishMode = mode;
        NotifyBlemishModeProperties();
    }

    private Visibility GetPanelVisibility(BlemishMode mode)
    {
        return _activeBlemishMode == mode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotifyBlemishModeProperties()
    {
        OnPropertyChanged(nameof(IsAcneModeActive));
        OnPropertyChanged(nameof(IsSpotModeActive));
        OnPropertyChanged(nameof(IsMoleModeActive));
        OnPropertyChanged(nameof(IsFreckleModeActive));
        OnPropertyChanged(nameof(IsScarModeActive));
        OnPropertyChanged(nameof(AcnePanelVisibility));
        OnPropertyChanged(nameof(SpotPanelVisibility));
        OnPropertyChanged(nameof(MolePanelVisibility));
        OnPropertyChanged(nameof(FrecklePanelVisibility));
        OnPropertyChanged(nameof(ScarPanelVisibility));
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
        UpdateCanResetBlemishTab();
        return true;
    }

    private void UpdateCanResetBlemishTab()
    {
        CanResetBlemishTab =
            IsNonDefault(_acneReduce, 0) ||
            IsNonDefault(_acneRedness, 0) ||
            IsNonDefault(_acneBump, 0) ||
            IsNonDefault(_spotRemove, 0) ||
            IsNonDefault(_spotBlend, 0) ||
            IsNonDefault(_spotTextureMatch, 0) ||
            IsNonDefault(_moleReduce, 0) ||
            IsNonDefault(_moleProtect, 0) ||
            IsNonDefault(_moleEdgeBlend, 0) ||
            IsNonDefault(_freckleFade, 0) ||
            IsNonDefault(_freckleDensity, 0) ||
            IsNonDefault(_freckleProtect, 0) ||
            IsNonDefault(_scarSoften, 0) ||
            IsNonDefault(_scarToneBlend, 0) ||
            IsNonDefault(_scarTextureMatch, 0);
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

public sealed class BlemishAdjustmentEventArgs(
    string operationId,
    double value,
    BlemishAdjustmentSnapshot snapshot) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public BlemishAdjustmentSnapshot Snapshot { get; } = snapshot;
}

public sealed record BlemishAdjustmentSnapshot(
    double AcneReduce,
    double AcneRedness,
    double AcneBump,
    double SpotRemove,
    double SpotBlend,
    double SpotTextureMatch,
    double MoleReduce,
    double MoleProtect,
    double MoleEdgeBlend,
    double FreckleFade,
    double FreckleDensity,
    double FreckleProtect,
    double ScarSoften,
    double ScarToneBlend,
    double ScarTextureMatch);
