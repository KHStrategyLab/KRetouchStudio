using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

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
    private double _spotBlend = 50;
    private double _spotTextureMatch = 50;
    private double _moleReduce;
    private double _moleProtect = 50;
    private double _moleEdgeBlend = 50;
    private double _freckleFade;
    private double _freckleDensity = 50;
    private double _freckleProtect = 50;
    private double _scarSoften;
    private double _scarToneBlend;
    private double _scarTextureMatch = 50;

    public BlemishTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
        _activeBlemishMode = BlemishMode.Acne;
        _acneReduce = 0;
        _acneRedness = 0;
        _acneBump = 0;
        _spotRemove = 0;
        _spotBlend = 50;
        _spotTextureMatch = 50;
        _moleReduce = 0;
        _moleProtect = 50;
        _moleEdgeBlend = 50;
        _freckleFade = 0;
        _freckleDensity = 50;
        _freckleProtect = 50;
        _scarSoften = 0;
        _scarToneBlend = 0;
        _scarTextureMatch = 50;
        OnPropertyChanged(string.Empty);
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
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
