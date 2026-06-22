using System.Windows.Controls;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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

    public BackgroundTabView()
    {
        CustomBackgroundBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 240, 242));
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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
    }

    private void WhiteBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundMode.White, forceRefresh: true);
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
