using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class WrinkleTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private double _forehead;
    private double _frown;
    private double _leftCrowsFeet;
    private double _rightCrowsFeet;
    private bool _crowsFeetLinked = true;
    private double _leftUnderEye;
    private double _rightUnderEye;
    private bool _underEyeLinked = true;
    private double _leftBunny;
    private double _rightBunny;
    private bool _bunnyLinked = true;
    private double _leftSmileFold;
    private double _rightSmileFold;
    private bool _smileFoldLinked = true;
    private double _lipLines;
    private double _leftMarionette;
    private double _rightMarionette;
    private bool _marionetteLinked = true;
    private double _chinCrease;
    private double _neck;
    private bool _canResetWrinkleTab;

    public WrinkleTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool CanResetWrinkleTab
    {
        get => _canResetWrinkleTab;
        private set
        {
            if (_canResetWrinkleTab == value)
            {
                return;
            }

            _canResetWrinkleTab = value;
            OnPropertyChanged();
        }
    }

    public double Forehead
    {
        get => _forehead;
        set => SetSliderValue(ref _forehead, value);
    }

    public double Frown
    {
        get => _frown;
        set => SetSliderValue(ref _frown, value);
    }

    public double LeftCrowsFeet
    {
        get => _leftCrowsFeet;
        set => SetSliderValue(ref _leftCrowsFeet, value);
    }

    public double RightCrowsFeet
    {
        get => _rightCrowsFeet;
        set => SetSliderValue(ref _rightCrowsFeet, value);
    }

    public bool CrowsFeetLinked
    {
        get => _crowsFeetLinked;
        set => SetValue(ref _crowsFeetLinked, value);
    }

    public double LeftUnderEye
    {
        get => _leftUnderEye;
        set => SetSliderValue(ref _leftUnderEye, value);
    }

    public double RightUnderEye
    {
        get => _rightUnderEye;
        set => SetSliderValue(ref _rightUnderEye, value);
    }

    public bool UnderEyeLinked
    {
        get => _underEyeLinked;
        set => SetValue(ref _underEyeLinked, value);
    }

    public double LeftBunny
    {
        get => _leftBunny;
        set => SetSliderValue(ref _leftBunny, value);
    }

    public double RightBunny
    {
        get => _rightBunny;
        set => SetSliderValue(ref _rightBunny, value);
    }

    public bool BunnyLinked
    {
        get => _bunnyLinked;
        set => SetValue(ref _bunnyLinked, value);
    }

    public double LeftSmileFold
    {
        get => _leftSmileFold;
        set => SetSliderValue(ref _leftSmileFold, value);
    }

    public double RightSmileFold
    {
        get => _rightSmileFold;
        set => SetSliderValue(ref _rightSmileFold, value);
    }

    public bool SmileFoldLinked
    {
        get => _smileFoldLinked;
        set => SetValue(ref _smileFoldLinked, value);
    }

    public double LipLines
    {
        get => _lipLines;
        set => SetSliderValue(ref _lipLines, value);
    }

    public double LeftMarionette
    {
        get => _leftMarionette;
        set => SetSliderValue(ref _leftMarionette, value);
    }

    public double RightMarionette
    {
        get => _rightMarionette;
        set => SetSliderValue(ref _rightMarionette, value);
    }

    public bool MarionetteLinked
    {
        get => _marionetteLinked;
        set => SetValue(ref _marionetteLinked, value);
    }

    public double ChinCrease
    {
        get => _chinCrease;
        set => SetSliderValue(ref _chinCrease, value);
    }

    public double Neck
    {
        get => _neck;
        set => SetSliderValue(ref _neck, value);
    }

    public void Collapse()
    {
        WrinkleExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        ResetWrinkleTabValues();
    }

    private void ResetWrinkleTabButton_Click(object sender, RoutedEventArgs e)
    {
        ResetWrinkleTabValues();
        e.Handled = true;
    }

    private void ResetWrinkleTabValues()
    {
        _forehead = 0;
        _frown = 0;
        _leftCrowsFeet = 0;
        _rightCrowsFeet = 0;
        _crowsFeetLinked = true;
        _leftUnderEye = 0;
        _rightUnderEye = 0;
        _underEyeLinked = true;
        _leftBunny = 0;
        _rightBunny = 0;
        _bunnyLinked = true;
        _leftSmileFold = 0;
        _rightSmileFold = 0;
        _smileFoldLinked = true;
        _lipLines = 0;
        _leftMarionette = 0;
        _rightMarionette = 0;
        _marionetteLinked = true;
        _chinCrease = 0;
        _neck = 0;
        CanResetWrinkleTab = false;
        OnPropertyChanged(string.Empty);
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
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
        UpdateCanResetWrinkleTab();
        return true;
    }

    private bool SetValue<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        where T : IEquatable<T>
    {
        if (storage.Equals(value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        UpdateCanResetWrinkleTab();
        return true;
    }

    private void UpdateCanResetWrinkleTab()
    {
        CanResetWrinkleTab =
            IsNonDefault(_forehead, 0) ||
            IsNonDefault(_frown, 0) ||
            IsNonDefault(_leftCrowsFeet, 0) ||
            IsNonDefault(_rightCrowsFeet, 0) ||
            !_crowsFeetLinked ||
            IsNonDefault(_leftUnderEye, 0) ||
            IsNonDefault(_rightUnderEye, 0) ||
            !_underEyeLinked ||
            IsNonDefault(_leftBunny, 0) ||
            IsNonDefault(_rightBunny, 0) ||
            !_bunnyLinked ||
            IsNonDefault(_leftSmileFold, 0) ||
            IsNonDefault(_rightSmileFold, 0) ||
            !_smileFoldLinked ||
            IsNonDefault(_lipLines, 0) ||
            IsNonDefault(_leftMarionette, 0) ||
            IsNonDefault(_rightMarionette, 0) ||
            !_marionetteLinked ||
            IsNonDefault(_chinCrease, 0) ||
            IsNonDefault(_neck, 0);
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
