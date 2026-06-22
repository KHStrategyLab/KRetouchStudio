using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class MouthTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum MouthMode
    {
        Smile,
        Lip,
        Size,
        Phil,
        Corner
    }

    private MouthMode _activeMouthMode = MouthMode.Smile;
    private double _smileStrength;
    private double _lipStrength;
    private double _sizeStrength;
    private double _philStrength;
    private double _cornerStrength;

    public MouthTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsSmileMouthModeActive => _activeMouthMode == MouthMode.Smile;

    public bool IsLipMouthModeActive => _activeMouthMode == MouthMode.Lip;

    public bool IsSizeMouthModeActive => _activeMouthMode == MouthMode.Size;

    public bool IsPhilMouthModeActive => _activeMouthMode == MouthMode.Phil;

    public bool IsCornerMouthModeActive => _activeMouthMode == MouthMode.Corner;

    public double ActiveMouthStrength
    {
        get => GetMouthStrength(_activeMouthMode);
        set
        {
            if (!SetMouthStrength(_activeMouthMode, value))
            {
                return;
            }

            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        MouthExpander.IsExpanded = false;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void SmileMouthButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMouthMode(MouthMode.Smile, forceRefresh: true);
        e.Handled = true;
    }

    private void LipMouthButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMouthMode(MouthMode.Lip, forceRefresh: true);
        e.Handled = true;
    }

    private void SizeMouthButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMouthMode(MouthMode.Size, forceRefresh: true);
        e.Handled = true;
    }

    private void PhilMouthButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMouthMode(MouthMode.Phil, forceRefresh: true);
        e.Handled = true;
    }

    private void CornerMouthButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveMouthMode(MouthMode.Corner, forceRefresh: true);
        e.Handled = true;
    }

    private void SetActiveMouthMode(MouthMode mode, bool forceRefresh = false)
    {
        if (_activeMouthMode == mode)
        {
            if (forceRefresh)
            {
                NotifyMouthModeProperties();
            }

            return;
        }

        _activeMouthMode = mode;
        NotifyMouthModeProperties();
        OnPropertyChanged(nameof(ActiveMouthStrength));
    }

    private void NotifyMouthModeProperties()
    {
        OnPropertyChanged(nameof(IsSmileMouthModeActive));
        OnPropertyChanged(nameof(IsLipMouthModeActive));
        OnPropertyChanged(nameof(IsSizeMouthModeActive));
        OnPropertyChanged(nameof(IsPhilMouthModeActive));
        OnPropertyChanged(nameof(IsCornerMouthModeActive));
    }

    private double GetMouthStrength(MouthMode mode)
    {
        return mode switch
        {
            MouthMode.Lip => _lipStrength,
            MouthMode.Size => _sizeStrength,
            MouthMode.Phil => _philStrength,
            MouthMode.Corner => _cornerStrength,
            _ => _smileStrength
        };
    }

    private bool SetMouthStrength(MouthMode mode, double value)
    {
        double clamped = Math.Clamp(Math.Round(value), 0, 100);
        ref double storage = ref GetMouthStrengthStorage(mode);
        if (Math.Abs(storage - clamped) < 0.01)
        {
            return false;
        }

        storage = clamped;
        return true;
    }

    private ref double GetMouthStrengthStorage(MouthMode mode)
    {
        switch (mode)
        {
            case MouthMode.Lip:
                return ref _lipStrength;
            case MouthMode.Size:
                return ref _sizeStrength;
            case MouthMode.Phil:
                return ref _philStrength;
            case MouthMode.Corner:
                return ref _cornerStrength;
            default:
                return ref _smileStrength;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
