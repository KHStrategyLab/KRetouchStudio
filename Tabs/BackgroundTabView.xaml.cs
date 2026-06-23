using System.Windows.Controls;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KRetouchStudio.Tabs;

public partial class BackgroundTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum BackgroundMode
    {
        White,
        Gray,
        Color,
        Pick,
        Image
    }

    private BackgroundMode _activeBackgroundMode = BackgroundMode.White;
    private double _backgroundOpacity = 100;
    private double _boundaryProbeStrength;
    private double _boundaryCleanStrength;

    public BackgroundTabView()
    {
        CustomBackgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 240, 242));
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? WhiteBackgroundRequested;

    public event EventHandler? WhiteBackgroundAdjustmentCommitted;

    public event EventHandler? BackgroundTabOpened;

    public System.Windows.Media.Brush CustomBackgroundBrush { get; }

    public bool IsWhiteBackgroundModeActive => _activeBackgroundMode == BackgroundMode.White;

    public bool IsGrayBackgroundModeActive => _activeBackgroundMode == BackgroundMode.Gray;

    public bool IsColorBackgroundModeActive => _activeBackgroundMode == BackgroundMode.Color;

    public bool IsPickBackgroundModeActive => _activeBackgroundMode == BackgroundMode.Pick;

    public bool IsImageBackgroundModeActive => _activeBackgroundMode == BackgroundMode.Image;

    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_backgroundOpacity - clamped) < 0.01)
            {
                return;
            }

            _backgroundOpacity = clamped;
            OnPropertyChanged();
        }
    }

    public double BoundaryProbeStrength
    {
        get => _boundaryProbeStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_boundaryProbeStrength - clamped) < 0.01)
            {
                return;
            }

            _boundaryProbeStrength = clamped;
            OnPropertyChanged();
        }
    }

    public double BoundaryCleanStrength
    {
        get => _boundaryCleanStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_boundaryCleanStrength - clamped) < 0.01)
            {
                return;
            }

            _boundaryCleanStrength = clamped;
            OnPropertyChanged();
        }
    }

    public void Collapse()
    {
        BackgroundExpander.IsExpanded = false;
    }

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }

        BackgroundTabOpened?.Invoke(this, EventArgs.Empty);
    }

    private void WhiteBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.White, forceRefresh: true);
        WhiteBackgroundRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void GrayBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.Gray, forceRefresh: true);
        e.Handled = true;
    }

    private void ColorBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.Color, forceRefresh: true);
        e.Handled = true;
    }

    private void PickBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.Pick, forceRefresh: true);
        e.Handled = true;
    }

    private void ImageBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.Image, forceRefresh: true);
        e.Handled = true;
    }

    private void BoundarySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CommitWhiteBackgroundAdjustment();
    }

    private void BoundarySlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Left or
            System.Windows.Input.Key.Right or
            System.Windows.Input.Key.Up or
            System.Windows.Input.Key.Down or
            System.Windows.Input.Key.PageUp or
            System.Windows.Input.Key.PageDown or
            System.Windows.Input.Key.Home or
            System.Windows.Input.Key.End)
        {
            CommitWhiteBackgroundAdjustment();
        }
    }

    private void CommitWhiteBackgroundAdjustment()
    {
        if (_activeBackgroundMode != BackgroundMode.White)
        {
            return;
        }

        WhiteBackgroundAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void SetActiveBackgroundMode(BackgroundMode mode, bool forceRefresh = false)
    {
        if (_activeBackgroundMode == mode)
        {
            if (forceRefresh)
            {
                NotifyBackgroundModeProperties();
            }

            return;
        }

        _activeBackgroundMode = mode;
        NotifyBackgroundModeProperties();
    }

    private void NotifyBackgroundModeProperties()
    {
        OnPropertyChanged(nameof(IsWhiteBackgroundModeActive));
        OnPropertyChanged(nameof(IsGrayBackgroundModeActive));
        OnPropertyChanged(nameof(IsColorBackgroundModeActive));
        OnPropertyChanged(nameof(IsPickBackgroundModeActive));
        OnPropertyChanged(nameof(IsImageBackgroundModeActive));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
