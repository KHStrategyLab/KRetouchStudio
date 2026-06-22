using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio.Tabs;

public partial class PhotoAdjustTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum ToneQuickControlKind
    {
        Exposure,
        Contrast,
        Saturation,
        WhiteBalance,
        Sharpness
    }

    private CurvePoint? _draggingCurvePoint;
    private CurvePoint? _trackedGuideCurvePoint;
    private readonly System.Windows.Threading.DispatcherTimer _curveDragPreviewTimer;
    private readonly System.Windows.Threading.DispatcherTimer _toneQuickPreviewTimer;
    private readonly System.Windows.Threading.DispatcherTimer _curveHistogramRefreshTimer;
    private ToneQuickControlKind _activeToneQuickControlKind = ToneQuickControlKind.Exposure;
    private BitmapSource? _pendingCurveHistogramSource;
    private double _toneExposureValue;
    private double _toneContrastValue;
    private double _toneSaturationValue;
    private double _toneWhiteBalanceValue;
    private double _toneSharpnessValue;
    private bool _isRefreshingCurvePreview;
    private bool _isDraggingCurvePoint;
    private bool _isDraggingCurveStrengthControl;
    private bool _hasDeferredCurvePreview;
    private bool _isDraggingToneQuickControl;
    private bool _hasDeferredToneQuickPreview;
    private bool _hasPendingCurveHistogramRefresh;

    public PhotoAdjustTabView()
    {
        CurveState = new ToneCurveEditorState();
        CurveState.PropertyChanged += CurveState_PropertyChanged;
        _curveDragPreviewTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        _curveDragPreviewTimer.Tick += CurveDragPreviewTimer_Tick;
        _toneQuickPreviewTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(350)
        };
        _toneQuickPreviewTimer.Tick += ToneQuickPreviewTimer_Tick;
        _curveHistogramRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(700)
        };
        _curveHistogramRefreshTimer.Tick += CurveHistogramRefreshTimer_Tick;
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<TonePreviewChangedEventArgs>? CurvePreviewChanged;

    public ToneCurveEditorState CurveState { get; }

    public BitmapSource? CurvePreviewBaseSource { get; private set; }

    public Visibility SelectedCurveGuideVisibility => CurveState.SelectedCurvePoint is null
        ? Visibility.Collapsed
        : Visibility.Visible;

    public double SelectedCurveGuideX => CurveState.SelectedCurvePoint?.CanvasLeft + 4 ?? 4;

    public double SelectedCurveGuideY => CurveState.SelectedCurvePoint?.CanvasTop + 4 ?? 184;

    public bool IsExposureToneQuickControlActive => _activeToneQuickControlKind == ToneQuickControlKind.Exposure;

    public bool IsContrastToneQuickControlActive => _activeToneQuickControlKind == ToneQuickControlKind.Contrast;

    public bool IsSaturationToneQuickControlActive => _activeToneQuickControlKind == ToneQuickControlKind.Saturation;

    public bool IsWhiteBalanceToneQuickControlActive => _activeToneQuickControlKind == ToneQuickControlKind.WhiteBalance;

    public bool IsSharpnessToneQuickControlActive => _activeToneQuickControlKind == ToneQuickControlKind.Sharpness;

    public string ActiveToneQuickControlLabel => _activeToneQuickControlKind switch
    {
        ToneQuickControlKind.Exposure => "노출",
        ToneQuickControlKind.Contrast => "대비",
        ToneQuickControlKind.Saturation => "채도",
        ToneQuickControlKind.WhiteBalance => "화이트밸런스",
        ToneQuickControlKind.Sharpness => "선명도",
        _ => string.Empty
    };

    public double ActiveToneQuickControlMinimum => _activeToneQuickControlKind switch
    {
        ToneQuickControlKind.Exposure => -15,
        ToneQuickControlKind.Contrast => -25,
        _ => -100
    };

    public double ActiveToneQuickControlMaximum => _activeToneQuickControlKind switch
    {
        ToneQuickControlKind.Exposure => 15,
        ToneQuickControlKind.Contrast => 25,
        _ => 100
    };

    public double ActiveToneQuickControlTickFrequency => _activeToneQuickControlKind switch
    {
        ToneQuickControlKind.Exposure => 0.5,
        _ => 1
    };

    public string ActiveToneQuickControlValueDisplay => $"{ActiveToneQuickControlValue:0.#}";

    public bool HasEffectiveToneQuickAdjustment =>
        Math.Abs(_toneExposureValue) > 0.001 ||
        Math.Abs(_toneContrastValue) > 0.001 ||
        Math.Abs(_toneSaturationValue) > 0.001 ||
        Math.Abs(_toneWhiteBalanceValue) > 0.001 ||
        Math.Abs(_toneSharpnessValue) > 0.001;

    public double ToneExposureValue => _toneExposureValue;

    public double ToneContrastValue => _toneContrastValue;

    public double ToneSaturationValue => _toneSaturationValue;

    public double ToneWhiteBalanceValue => _toneWhiteBalanceValue;

    public double ToneSharpnessValue => _toneSharpnessValue;

    public double ActiveToneQuickControlValue
    {
        get => _activeToneQuickControlKind switch
        {
            ToneQuickControlKind.Exposure => _toneExposureValue,
            ToneQuickControlKind.Contrast => _toneContrastValue,
            ToneQuickControlKind.Saturation => _toneSaturationValue,
            ToneQuickControlKind.WhiteBalance => _toneWhiteBalanceValue,
            ToneQuickControlKind.Sharpness => _toneSharpnessValue,
            _ => 0
        };
        set
        {
            double clamped = Math.Clamp(value, ActiveToneQuickControlMinimum, ActiveToneQuickControlMaximum);
            bool changed = _activeToneQuickControlKind switch
            {
                ToneQuickControlKind.Exposure => SetToneQuickControlValue(ref _toneExposureValue, clamped),
                ToneQuickControlKind.Contrast => SetToneQuickControlValue(ref _toneContrastValue, clamped),
                ToneQuickControlKind.Saturation => SetToneQuickControlValue(ref _toneSaturationValue, clamped),
                ToneQuickControlKind.WhiteBalance => SetToneQuickControlValue(ref _toneWhiteBalanceValue, clamped),
                ToneQuickControlKind.Sharpness => SetToneQuickControlValue(ref _toneSharpnessValue, clamped),
                _ => false
            };

            if (!changed)
            {
                return;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(ActiveToneQuickControlValueDisplay));
            OnPropertyChanged(nameof(HasEffectiveToneQuickAdjustment));

            RequestToneQuickPreviewChanged();
        }
    }

    public void Collapse()
    {
        PhotoAdjustExpander.IsExpanded = false;
    }

    public void RefreshForPhoto(BitmapSource? source)
    {
        _curveHistogramRefreshTimer.Stop();
        _pendingCurveHistogramSource = null;
        _hasPendingCurveHistogramRefresh = false;

        _isRefreshingCurvePreview = true;
        try
        {
            CurvePreviewBaseSource = source;
            ClearSelectedCurvePoint();
            CurveState.ResetAllChannels();
            CurveState.SetCurveHistogramSource(source);
            UpdateTrackedCurveGuidePoint();
            RaiseCurveGuidePropertyChanged();
        }
        finally
        {
            _isRefreshingCurvePreview = false;
        }
    }

    public void QueueCurveHistogramRefresh(BitmapSource? source)
    {
        _pendingCurveHistogramSource = source;
        _hasPendingCurveHistogramRefresh = true;
        _curveHistogramRefreshTimer.Stop();
        _curveHistogramRefreshTimer.Start();
    }

    private void CurveHistogramRefreshTimer_Tick(object? sender, EventArgs e)
    {
        _curveHistogramRefreshTimer.Stop();

        if (!_hasPendingCurveHistogramRefresh)
        {
            return;
        }

        BitmapSource? source = _pendingCurveHistogramSource;
        _pendingCurveHistogramSource = null;
        _hasPendingCurveHistogramRefresh = false;

        _isRefreshingCurvePreview = true;
        try
        {
            CurveState.SetCurveHistogramSource(source);
        }
        finally
        {
            _isRefreshingCurvePreview = false;
        }
    }

    private void Expander_Expanded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (System.Windows.Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void ExposureToneButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetActiveToneQuickControl(ToneQuickControlKind.Exposure);
    }

    private void ContrastToneButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetActiveToneQuickControl(ToneQuickControlKind.Contrast);
    }

    private void SaturationToneButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetActiveToneQuickControl(ToneQuickControlKind.Saturation);
    }

    private void WhiteBalanceToneButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetActiveToneQuickControl(ToneQuickControlKind.WhiteBalance);
    }

    private void SharpnessToneButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        SetActiveToneQuickControl(ToneQuickControlKind.Sharpness);
    }

    private void ResetToneCorrectionButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ResetToneCorrection();
        e.Handled = true;
    }

    private void CurveStrengthSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BeginCurveStrengthControlDrag();
    }

    private void CurveStrengthSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        FinishCurveStrengthControlDrag();
    }

    private void CurveStrengthSlider_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        FinishCurveStrengthControlDrag();
    }

    private void ToneQuickSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        BeginToneQuickControlDrag();
    }

    private void ToneQuickSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        FinishToneQuickControlDrag();
    }

    private void ToneQuickSlider_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
    {
        FinishToneQuickControlDrag();
    }

    private void ResetCurveChannelButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (CurveState.ResetCurrentCurveChannel())
        {
            ClearSelectedCurvePoint();
        }
    }

    private void CurveChannelComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ClearSelectedCurvePoint();
    }

    private void CurvePointValueTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Return)
        {
            return;
        }

        CommitCurvePointValueTextBox(sender as System.Windows.Controls.TextBox);
        e.Handled = true;
    }

    private void CurvePointValueTextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        CommitCurvePointValueTextBox(sender as System.Windows.Controls.TextBox);
    }

    private void CurveCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Canvas canvas)
        {
            return;
        }

        BeginCurvePointDrag();
        System.Windows.Point point = e.GetPosition(canvas);
        CurvePoint? curvePoint = CurveState.AddCurvePointFromCanvas(point.X, point.Y);
        if (curvePoint is null)
        {
            CancelCurvePointDrag();
            return;
        }

        _draggingCurvePoint = curvePoint;
        SelectCurvePoint(curvePoint);
        Mouse.Capture(canvas);
        e.Handled = true;
    }

    private void CurveCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is System.Windows.Controls.Canvas canvas)
        {
            MoveDraggingCurvePoint(canvas, e);
        }
    }

    private void CurveCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        FinishDraggingCurvePoint();
        e.Handled = true;
    }

    private void CurvePoint_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement element ||
            element.DataContext is not CurvePoint point ||
            FindVisualParent<System.Windows.Controls.Canvas>(element) is not System.Windows.Controls.Canvas canvas)
        {
            return;
        }

        BeginCurvePointDrag();
        _draggingCurvePoint = point;
        SelectCurvePoint(point);
        Mouse.Capture(canvas);
        e.Handled = true;
    }

    private void CurvePoint_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (Mouse.Captured is System.Windows.Controls.Canvas canvas)
        {
            MoveDraggingCurvePoint(canvas, e);
        }
    }

    private void CurvePoint_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        FinishDraggingCurvePoint();
        e.Handled = true;
    }

    private void CommitCurvePointValueTextBox(System.Windows.Controls.TextBox? textBox)
    {
        if (textBox is null || CurveState.SelectedCurvePoint is null)
        {
            RefreshCurvePointValueTextBox(textBox);
            return;
        }

        if (!int.TryParse(textBox.Text.Trim(), out int value))
        {
            RefreshCurvePointValueTextBox(textBox);
            return;
        }

        bool changed = textBox.Tag switch
        {
            "Input" => CurveState.SetSelectedCurvePointInput(value),
            "Output" => CurveState.SetSelectedCurvePointOutput(value),
            _ => false
        };

        if (!changed)
        {
            RefreshCurvePointValueTextBox(textBox);
        }
    }

    private static void RefreshCurvePointValueTextBox(System.Windows.Controls.TextBox? textBox)
    {
        textBox?.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateTarget();
    }

    private void CurveState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ToneCurveEditorState.SelectedCurvePoint))
        {
            UpdateTrackedCurveGuidePoint();
            RaiseCurveGuidePropertyChanged();
        }

        if (_isRefreshingCurvePreview)
        {
            return;
        }

        if (e.PropertyName is nameof(ToneCurveEditorState.CurvePolylinePoints) or nameof(ToneCurveEditorState.Value))
        {
            RequestCurvePreviewChanged();
        }
    }

    private void UpdateTrackedCurveGuidePoint()
    {
        if (ReferenceEquals(_trackedGuideCurvePoint, CurveState.SelectedCurvePoint))
        {
            return;
        }

        if (_trackedGuideCurvePoint is not null)
        {
            _trackedGuideCurvePoint.PropertyChanged -= TrackedGuideCurvePoint_PropertyChanged;
        }

        _trackedGuideCurvePoint = CurveState.SelectedCurvePoint;
        if (_trackedGuideCurvePoint is not null)
        {
            _trackedGuideCurvePoint.PropertyChanged += TrackedGuideCurvePoint_PropertyChanged;
        }
    }

    private void TrackedGuideCurvePoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CurvePoint.CanvasLeft) or nameof(CurvePoint.CanvasTop) or nameof(CurvePoint.Input) or nameof(CurvePoint.Output))
        {
            RaiseCurveGuidePropertyChanged();
        }
    }

    private void RaiseCurveGuidePropertyChanged()
    {
        OnPropertyChanged(nameof(SelectedCurveGuideVisibility));
        OnPropertyChanged(nameof(SelectedCurveGuideX));
        OnPropertyChanged(nameof(SelectedCurveGuideY));
    }

    private void SetActiveToneQuickControl(ToneQuickControlKind kind)
    {
        if (_activeToneQuickControlKind == kind)
        {
            return;
        }

        _activeToneQuickControlKind = kind;
        OnPropertyChanged(nameof(IsExposureToneQuickControlActive));
        OnPropertyChanged(nameof(IsContrastToneQuickControlActive));
        OnPropertyChanged(nameof(IsSaturationToneQuickControlActive));
        OnPropertyChanged(nameof(IsWhiteBalanceToneQuickControlActive));
        OnPropertyChanged(nameof(IsSharpnessToneQuickControlActive));
        OnPropertyChanged(nameof(ActiveToneQuickControlLabel));
        OnPropertyChanged(nameof(ActiveToneQuickControlMinimum));
        OnPropertyChanged(nameof(ActiveToneQuickControlMaximum));
        OnPropertyChanged(nameof(ActiveToneQuickControlTickFrequency));
        OnPropertyChanged(nameof(ActiveToneQuickControlValue));
        OnPropertyChanged(nameof(ActiveToneQuickControlValueDisplay));
    }

    private static bool SetToneQuickControlValue(ref double storage, double value)
    {
        if (Math.Abs(storage - value) < 0.001)
        {
            return false;
        }

        storage = value;
        return true;
    }

    private void ResetToneCorrection()
    {
        _isDraggingCurvePoint = false;
        _isDraggingCurveStrengthControl = false;
        _hasDeferredCurvePreview = false;
        _isDraggingToneQuickControl = false;
        _hasDeferredToneQuickPreview = false;
        _curveDragPreviewTimer.Stop();
        _toneQuickPreviewTimer.Stop();

        _isRefreshingCurvePreview = true;
        try
        {
            CurveState.ResetAllChannels();
            _toneExposureValue = 0;
            _toneContrastValue = 0;
            _toneSaturationValue = 0;
            _toneWhiteBalanceValue = 0;
            _toneSharpnessValue = 0;
        }
        finally
        {
            _isRefreshingCurvePreview = false;
        }

        NotifyToneQuickControlValuesChanged();
        UpdateTrackedCurveGuidePoint();
        RaiseCurveGuidePropertyChanged();
        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void NotifyToneQuickControlValuesChanged()
    {
        OnPropertyChanged(nameof(ToneExposureValue));
        OnPropertyChanged(nameof(ToneContrastValue));
        OnPropertyChanged(nameof(ToneSaturationValue));
        OnPropertyChanged(nameof(ToneWhiteBalanceValue));
        OnPropertyChanged(nameof(ToneSharpnessValue));
        OnPropertyChanged(nameof(ActiveToneQuickControlValue));
        OnPropertyChanged(nameof(ActiveToneQuickControlValueDisplay));
        OnPropertyChanged(nameof(HasEffectiveToneQuickAdjustment));
    }

    private void MoveDraggingCurvePoint(System.Windows.Controls.Canvas canvas, System.Windows.Input.MouseEventArgs e)
    {
        if (_draggingCurvePoint is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        System.Windows.Point point = e.GetPosition(canvas);
        if (IsOutsideCurveCanvas(canvas, point))
        {
            CurveState.MarkCurvePointForDeletion(_draggingCurvePoint);
        }
        else
        {
            _draggingCurvePoint.IsPendingDelete = false;
            CurveState.MoveCurvePoint(_draggingCurvePoint, point.X, point.Y);
        }

        e.Handled = true;
    }

    private void FinishDraggingCurvePoint()
    {
        CurvePoint? point = _draggingCurvePoint;
        if (point is null)
        {
            CancelCurvePointDrag();
            return;
        }

        if (Mouse.Captured is not null)
        {
            Mouse.Capture(null);
        }

        if (CurveState.DeleteCurvePointIfMarked(point))
        {
            ClearSelectedCurvePoint();
        }
        else
        {
            point.IsPendingDelete = false;
        }

        _draggingCurvePoint = null;
        _isDraggingCurvePoint = false;
        StopCurvePreviewTimerIfIdle();
        _hasDeferredCurvePreview = false;

        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void BeginCurvePointDrag()
    {
        _isDraggingCurvePoint = true;
        _hasDeferredCurvePreview = false;
        _curveDragPreviewTimer.Start();
    }

    private void CancelCurvePointDrag()
    {
        _draggingCurvePoint = null;
        _isDraggingCurvePoint = false;
        _hasDeferredCurvePreview = false;
        StopCurvePreviewTimerIfIdle();
    }

    private void RequestCurvePreviewChanged()
    {
        if (_isRefreshingCurvePreview)
        {
            return;
        }

        if (_isDraggingCurvePoint || _isDraggingCurveStrengthControl)
        {
            _hasDeferredCurvePreview = true;
            if (!_curveDragPreviewTimer.IsEnabled)
            {
                _curveDragPreviewTimer.Start();
            }

            return;
        }

        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void BeginCurveStrengthControlDrag()
    {
        _isDraggingCurveStrengthControl = true;
        _hasDeferredCurvePreview = false;
        _curveDragPreviewTimer.Start();
    }

    private void FinishCurveStrengthControlDrag()
    {
        if (!_isDraggingCurveStrengthControl)
        {
            return;
        }

        _isDraggingCurveStrengthControl = false;
        StopCurvePreviewTimerIfIdle();
        _hasDeferredCurvePreview = false;

        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void BeginToneQuickControlDrag()
    {
        _isDraggingToneQuickControl = true;
        _hasDeferredToneQuickPreview = false;
        _toneQuickPreviewTimer.Start();
    }

    private void FinishToneQuickControlDrag()
    {
        if (!_isDraggingToneQuickControl)
        {
            return;
        }

        _isDraggingToneQuickControl = false;
        _toneQuickPreviewTimer.Stop();
        _hasDeferredToneQuickPreview = false;

        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void RequestToneQuickPreviewChanged()
    {
        if (_isRefreshingCurvePreview)
        {
            return;
        }

        if (_isDraggingToneQuickControl)
        {
            _hasDeferredToneQuickPreview = true;
            if (!_toneQuickPreviewTimer.IsEnabled)
            {
                _toneQuickPreviewTimer.Start();
            }

            return;
        }

        RaiseCurvePreviewChanged(useFastPreview: false);
    }

    private void CurveDragPreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isDraggingCurvePoint && !_isDraggingCurveStrengthControl)
        {
            _curveDragPreviewTimer.Stop();
            return;
        }

        if (!_hasDeferredCurvePreview)
        {
            return;
        }

        _hasDeferredCurvePreview = false;
        RaiseCurvePreviewChanged(useFastPreview: true);
    }

    private void StopCurvePreviewTimerIfIdle()
    {
        if (!_isDraggingCurvePoint && !_isDraggingCurveStrengthControl)
        {
            _curveDragPreviewTimer.Stop();
        }
    }

    private void ToneQuickPreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isDraggingToneQuickControl)
        {
            _toneQuickPreviewTimer.Stop();
            return;
        }

        if (!_hasDeferredToneQuickPreview)
        {
            return;
        }

        _hasDeferredToneQuickPreview = false;
        RaiseCurvePreviewChanged(useFastPreview: true);
    }

    private void RaiseCurvePreviewChanged(bool useFastPreview)
    {
        CurvePreviewChanged?.Invoke(this, new TonePreviewChangedEventArgs(useFastPreview));
    }

    private void SelectCurvePoint(CurvePoint point)
    {
        if (CurveState.SelectedCurvePoint is not null)
        {
            CurveState.SelectedCurvePoint.IsSelected = false;
            CurveState.SelectedCurvePoint.IsPendingDelete = false;
        }

        point.IsSelected = true;
        point.IsPendingDelete = false;
        CurveState.SelectedCurvePoint = point;
    }

    private void ClearSelectedCurvePoint()
    {
        if (CurveState.SelectedCurvePoint is not null)
        {
            CurveState.SelectedCurvePoint.IsSelected = false;
            CurveState.SelectedCurvePoint.IsPendingDelete = false;
            CurveState.SelectedCurvePoint = null;
        }
    }

    private static bool IsOutsideCurveCanvas(System.Windows.Controls.Canvas canvas, System.Windows.Point point)
    {
        return point.X < 0 ||
               point.Y < 0 ||
               point.X > canvas.Width ||
               point.Y > canvas.Height;
    }

    private static T? FindVisualParent<T>(System.Windows.DependencyObject child)
        where T : System.Windows.DependencyObject
    {
        System.Windows.DependencyObject? current = child;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class TonePreviewChangedEventArgs : EventArgs
{
    public TonePreviewChangedEventArgs(bool useFastPreview)
    {
        UseFastPreview = useFastPreview;
    }

    public bool UseFastPreview { get; }
}
