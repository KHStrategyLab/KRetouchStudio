using System.Windows.Controls;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio.Tabs;

public partial class BackgroundTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private BackgroundReplacementMode _activeBackgroundMode = BackgroundReplacementMode.None;
    private double _backgroundOpacity = 100;
    private double _boundaryProbeStrength;
    private double _boundaryCleanStrength;
    private double _edgeBlurStrength;
    private double _alphaShrinkStrength;
    private double _softAlphaStrength;
    private double _alphaGammaStrength;
    private bool _isPickBackgroundModeActive;
    private bool _isBackgroundAdjustmentSliderInteracting;
    private bool _isRestoringSnapshot;
    private bool _canResetBackgroundTab;

    public BackgroundTabView()
    {
        CustomBackgroundBrush = new SolidColorBrush(ColorFromArgb(BackgroundAdjustmentSnapshot.DefaultSolidColorArgb));
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? BackgroundReplacementRequested;

    public event EventHandler? BackgroundReplacementAdjustmentCommitted;

    public event EventHandler? BackgroundReplacementPreviewChanged;

    public event EventHandler? BackgroundImageImportRequested;

    public event EventHandler<BackgroundImageSelectedEventArgs>? BackgroundImageSelected;

    public event EventHandler<BackgroundImageRemovedEventArgs>? BackgroundImageRemoved;

    public event EventHandler? BackgroundTabOpened;

    public event EventHandler? BackgroundResetRequested;

    public System.Windows.Media.Brush CustomBackgroundBrush { get; }

    public ObservableCollection<BackgroundImageSlot> BackgroundImages { get; } = new();

    public bool CanResetBackgroundTab
    {
        get => _canResetBackgroundTab;
        set
        {
            if (_canResetBackgroundTab == value)
            {
                return;
            }

            _canResetBackgroundTab = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedBackgroundImagePath { get; private set; }

    public BackgroundReplacementMode ActiveBackgroundMode => _activeBackgroundMode;

    public bool IsWhiteBackgroundModeActive =>
        !_isPickBackgroundModeActive &&
        _activeBackgroundMode == BackgroundReplacementMode.White;

    public bool IsGrayBackgroundModeActive =>
        !_isPickBackgroundModeActive &&
        _activeBackgroundMode == BackgroundReplacementMode.Gray;

    public bool IsColorBackgroundModeActive =>
        !_isPickBackgroundModeActive &&
        _activeBackgroundMode == BackgroundReplacementMode.SolidColor;

    public bool IsPickBackgroundModeActive => _isPickBackgroundModeActive;

    public bool IsImageBackgroundModeActive =>
        !_isPickBackgroundModeActive &&
        _activeBackgroundMode == BackgroundReplacementMode.Image;

    public bool IsBackgroundAdjustmentNeutral => _activeBackgroundMode == BackgroundReplacementMode.None;

    public bool HasEffectiveBackgroundAdjustment => CaptureSnapshot().HasEffectiveAdjustment;

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

    public double EdgeBlurStrength
    {
        get => _edgeBlurStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_edgeBlurStrength - clamped) < 0.01)
            {
                return;
            }

            _edgeBlurStrength = clamped;
            OnPropertyChanged();
        }
    }

    public void ResetAfterHistoryReset()
    {
        ResetToNeutral();
    }

    public void ResetForPhotoChange()
    {
        ResetToNeutral();
    }

    public void ResetToNeutral()
    {
        RestoreSnapshot(null);
    }

    public BackgroundAdjustmentSnapshot CaptureSnapshot()
    {
        uint colorArgb = CustomBackgroundBrush is SolidColorBrush solidColorBrush
            ? ColorToArgb(solidColorBrush.Color)
            : BackgroundAdjustmentSnapshot.DefaultSolidColorArgb;

        return new BackgroundAdjustmentSnapshot(
            BackgroundAdjustmentSnapshot.CurrentSchemaVersion,
            _activeBackgroundMode,
            colorArgb,
            SelectedBackgroundImagePath,
            _backgroundOpacity,
            _boundaryProbeStrength,
            _boundaryCleanStrength,
            _edgeBlurStrength,
            _alphaShrinkStrength,
            _softAlphaStrength,
            _alphaGammaStrength);
    }

    public void RestoreSnapshot(BackgroundAdjustmentSnapshot? snapshot)
    {
        BackgroundAdjustmentSnapshot restored = snapshot ?? BackgroundAdjustmentSnapshot.Neutral;

        _isRestoringSnapshot = true;
        try
        {
            _activeBackgroundMode = Enum.IsDefined(restored.Mode)
                ? restored.Mode
                : BackgroundReplacementMode.None;
            _isPickBackgroundModeActive = false;
            _backgroundOpacity = ClampAdjustment(restored.BackgroundOpacity, 100);
            _boundaryProbeStrength = ClampAdjustment(restored.BoundaryProbeStrength);
            _boundaryCleanStrength = ClampAdjustment(restored.BoundaryCleanStrength);
            _edgeBlurStrength = ClampAdjustment(restored.EdgeBlurStrength);
            _alphaShrinkStrength = ClampAdjustment(restored.AlphaShrinkStrength);
            _softAlphaStrength = ClampAdjustment(restored.SoftAlphaStrength);
            _alphaGammaStrength = ClampAdjustment(restored.AlphaGammaStrength);
            SelectedBackgroundImagePath = string.IsNullOrWhiteSpace(restored.SelectedImagePath)
                ? null
                : restored.SelectedImagePath;

            if (CustomBackgroundBrush is SolidColorBrush solidColorBrush)
            {
                solidColorBrush.Color = ColorFromArgb(restored.SolidColorArgb);
            }

            _isBackgroundAdjustmentSliderInteracting = false;
            _canResetBackgroundTab = CaptureSnapshot().HasEffectiveAdjustment;
            OnPropertyChanged(string.Empty);
        }
        finally
        {
            _isRestoringSnapshot = false;
        }
    }

    private void ResetBackgroundTabButton_Click(object sender, RoutedEventArgs e)
    {
        BackgroundResetRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    public double AlphaShrinkStrength
    {
        get => _alphaShrinkStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_alphaShrinkStrength - clamped) < 0.01)
            {
                return;
            }

            _alphaShrinkStrength = clamped;
            OnPropertyChanged();
        }
    }

    public double SoftAlphaStrength
    {
        get => _softAlphaStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_softAlphaStrength - clamped) < 0.01)
            {
                return;
            }

            _softAlphaStrength = clamped;
            OnPropertyChanged();
        }
    }

    public double AlphaGammaStrength
    {
        get => _alphaGammaStrength;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_alphaGammaStrength - clamped) < 0.01)
            {
                return;
            }

            _alphaGammaStrength = clamped;
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
        SetActiveBackgroundMode(BackgroundReplacementMode.White, forceRefresh: true);
        BackgroundReplacementRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void GrayBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundReplacementMode.Gray, forceRefresh: true);
        BackgroundReplacementRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void ColorBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        using System.Windows.Forms.ColorDialog dialog = new();
        if (CustomBackgroundBrush is SolidColorBrush currentBrush)
        {
            System.Windows.Media.Color currentColor = currentBrush.Color;
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            System.Drawing.Color pickedColor = dialog.Color;
            ApplyCustomBackgroundColor(System.Windows.Media.Color.FromRgb(
                pickedColor.R,
                pickedColor.G,
                pickedColor.B));
        }

        e.Handled = true;
    }

    private void PickBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetPickBackgroundModeActive();
        e.Handled = true;
    }

    public void ApplyPickedBackgroundColor(System.Windows.Media.Color color)
    {
        ApplyCustomBackgroundColor(color);
    }

    private void ApplyCustomBackgroundColor(System.Windows.Media.Color color)
    {
        if (CustomBackgroundBrush is SolidColorBrush solidColorBrush)
        {
            solidColorBrush.Color = color;
            OnPropertyChanged(nameof(CustomBackgroundBrush));
        }

        SetActiveBackgroundMode(BackgroundReplacementMode.SolidColor, forceRefresh: true);
        BackgroundReplacementRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ImageBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveBackgroundMode(BackgroundReplacementMode.Image, forceRefresh: true);
        BackgroundImageImportRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    public void SetBackgroundImagePaths(IEnumerable<string> paths, string? selectedPath)
    {
        BackgroundImages.Clear();
        foreach (string path in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            BackgroundImages.Add(new BackgroundImageSlot(path));
        }

        SelectedBackgroundImagePath = BackgroundImages.Any(item => string.Equals(item.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
            ? selectedPath
            : BackgroundImages.FirstOrDefault()?.Path;
        OnPropertyChanged(nameof(SelectedBackgroundImagePath));
    }

    public void AddBackgroundImagePath(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        if (!BackgroundImages.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            BackgroundImages.Add(new BackgroundImageSlot(path));
        }

        SelectBackgroundImagePath(path, raiseEvent: false);
    }

    private void BackgroundImageSlot_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button &&
            button.CommandParameter is string path)
        {
            SelectBackgroundImagePath(path, raiseEvent: true);
            e.Handled = true;
        }
    }

    private void BackgroundImageDeleteMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem menuItem &&
            menuItem.CommandParameter is string path)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show(
                Window.GetWindow(this),
                "이 배경 이미지를 목록에서 삭제하시겠습니까?",
                "배경 이미지 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            RemoveBackgroundImagePath(path);
            e.Handled = true;
        }
    }

    private void RemoveBackgroundImagePath(string path)
    {
        BackgroundImageSlot? target = BackgroundImages.FirstOrDefault(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return;
        }

        bool wasSelected = string.Equals(SelectedBackgroundImagePath, path, StringComparison.OrdinalIgnoreCase);
        BackgroundImages.Remove(target);
        if (wasSelected)
        {
            SelectedBackgroundImagePath = null;
            OnPropertyChanged(nameof(SelectedBackgroundImagePath));
            SetActiveBackgroundMode(BackgroundReplacementMode.None, forceRefresh: true);
        }

        BackgroundImageRemoved?.Invoke(this, new BackgroundImageRemovedEventArgs(path, wasSelected));
    }

    private void SelectBackgroundImagePath(string path, bool raiseEvent)
    {
        if (!File.Exists(path))
        {
            return;
        }

        SelectedBackgroundImagePath = path;
        OnPropertyChanged(nameof(SelectedBackgroundImagePath));
        SetActiveBackgroundMode(BackgroundReplacementMode.Image, forceRefresh: true);

        if (raiseEvent)
        {
            BackgroundImageSelected?.Invoke(this, new BackgroundImageSelectedEventArgs(path));
        }
    }

    private void BoundarySlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isBackgroundAdjustmentSliderInteracting = false;
        CommitWhiteBackgroundAdjustment();
    }

    private void BoundarySlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isBackgroundAdjustmentSliderInteracting = true;
    }

    private void BoundarySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isRestoringSnapshot ||
            sender is not System.Windows.Controls.Slider slider ||
            (Mouse.LeftButton != MouseButtonState.Pressed && !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!_isBackgroundAdjustmentSliderInteracting)
        {
            _isBackgroundAdjustmentSliderInteracting = true;
        }

        SyncBackgroundAdjustmentSliderValue(slider);

        if (!_isPickBackgroundModeActive &&
            _activeBackgroundMode is BackgroundReplacementMode.White or
                BackgroundReplacementMode.Gray or
                BackgroundReplacementMode.SolidColor or
                BackgroundReplacementMode.Image)
        {
            BackgroundReplacementPreviewChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SyncBackgroundAdjustmentSliderValue(System.Windows.Controls.Slider slider)
    {
        switch (slider.Name)
        {
            case "BackgroundOpacitySlider":
                BackgroundOpacity = slider.Value;
                break;
            case "BoundaryProbeStrengthSlider":
                BoundaryProbeStrength = slider.Value;
                break;
            case "BoundaryCleanStrengthSlider":
                BoundaryCleanStrength = slider.Value;
                break;
            case "AlphaShrinkStrengthSlider":
                AlphaShrinkStrength = slider.Value;
                break;
            case "EdgeBlurStrengthSlider":
                EdgeBlurStrength = slider.Value;
                break;
            case "SoftAlphaStrengthSlider":
                SoftAlphaStrength = slider.Value;
                break;
            case "AlphaGammaStrengthSlider":
                AlphaGammaStrength = slider.Value;
                break;
        }
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
        if (_isRestoringSnapshot ||
            _isPickBackgroundModeActive ||
            _activeBackgroundMode is not (BackgroundReplacementMode.White or
                BackgroundReplacementMode.Gray or
                BackgroundReplacementMode.SolidColor or
                BackgroundReplacementMode.Image))
        {
            return;
        }

        BackgroundReplacementAdjustmentCommitted?.Invoke(this, EventArgs.Empty);
    }

    private void SetActiveBackgroundMode(BackgroundReplacementMode mode, bool forceRefresh = false)
    {
        bool modeChanged = _activeBackgroundMode != mode;
        bool pickModeChanged = _isPickBackgroundModeActive;
        if (!modeChanged && !pickModeChanged)
        {
            if (forceRefresh)
            {
                NotifyBackgroundModeProperties();
            }

            return;
        }

        _activeBackgroundMode = mode;
        _isPickBackgroundModeActive = false;
        NotifyBackgroundModeProperties();
    }

    private void SetPickBackgroundModeActive()
    {
        if (_isPickBackgroundModeActive)
        {
            NotifyBackgroundModeProperties();
            return;
        }

        _isPickBackgroundModeActive = true;
        NotifyBackgroundModeProperties();
    }

    private void NotifyBackgroundModeProperties()
    {
        OnPropertyChanged(nameof(ActiveBackgroundMode));
        OnPropertyChanged(nameof(IsWhiteBackgroundModeActive));
        OnPropertyChanged(nameof(IsGrayBackgroundModeActive));
        OnPropertyChanged(nameof(IsColorBackgroundModeActive));
        OnPropertyChanged(nameof(IsPickBackgroundModeActive));
        OnPropertyChanged(nameof(IsImageBackgroundModeActive));
        OnPropertyChanged(nameof(IsBackgroundAdjustmentNeutral));
        OnPropertyChanged(nameof(HasEffectiveBackgroundAdjustment));
    }

    private static double ClampAdjustment(double value, double fallback = 0)
    {
        return double.IsFinite(value)
            ? Math.Clamp(Math.Round(value), 0, 100)
            : fallback;
    }

    private static uint ColorToArgb(System.Windows.Media.Color color)
    {
        return ((uint)color.A << 24) |
               ((uint)color.R << 16) |
               ((uint)color.G << 8) |
               color.B;
    }

    private static System.Windows.Media.Color ColorFromArgb(uint argb)
    {
        return System.Windows.Media.Color.FromArgb(
            (byte)(argb >> 24),
            (byte)(argb >> 16),
            (byte)(argb >> 8),
            (byte)argb);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class BackgroundImageSelectedEventArgs(string path) : EventArgs
{
    public string Path { get; } = path;
}

public sealed class BackgroundImageRemovedEventArgs(string path, bool wasSelected) : EventArgs
{
    public string Path { get; } = path;

    public bool WasSelected { get; } = wasSelected;
}

public sealed class BackgroundImageSlot
{
    public BackgroundImageSlot(string path)
    {
        Path = path;
        DisplayName = System.IO.Path.GetFileNameWithoutExtension(path);
        Thumbnail = CreateThumbnail(path);
    }

    public string Path { get; }

    public string DisplayName { get; }

    public ImageSource? Thumbnail { get; }

    private static ImageSource? CreateThumbnail(string path)
    {
        try
        {
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 88;
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
