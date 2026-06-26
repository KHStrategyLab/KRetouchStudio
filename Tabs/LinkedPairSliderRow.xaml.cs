using System.Windows;

using System.Windows.Controls;
using System.Windows.Input;

namespace KRetouchStudio.Tabs;

public partial class LinkedPairSliderRow : System.Windows.Controls.UserControl
{
    private bool _syncingPairValues;
    private bool _isLeftSliderInteracting;
    private bool _isRightSliderInteracting;
    private string? _lastPreviewOperationId;
    private double _lastPreviewValue = double.NaN;

    public LinkedPairSliderRow()
    {
        InitializeComponent();
        UpdateLinkButtonChrome();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(LinkedPairSliderRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty LeftValueProperty =
        DependencyProperty.Register(
            nameof(LeftValue),
            typeof(double),
            typeof(LinkedPairSliderRow),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnLeftValueChanged,
                CoerceSliderValue));

    public static readonly DependencyProperty RightValueProperty =
        DependencyProperty.Register(
            nameof(RightValue),
            typeof(double),
            typeof(LinkedPairSliderRow),
            new FrameworkPropertyMetadata(
                0d,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnRightValueChanged,
                CoerceSliderValue));

    public static readonly DependencyProperty IsLinkedProperty =
        DependencyProperty.Register(
            nameof(IsLinked),
            typeof(bool),
            typeof(LinkedPairSliderRow),
            new FrameworkPropertyMetadata(true, OnIsLinkedChanged));

    public static readonly DependencyProperty UseStackedLayoutProperty =
        DependencyProperty.Register(
            nameof(UseStackedLayout),
            typeof(bool),
            typeof(LinkedPairSliderRow),
            new PropertyMetadata(false));

    public static readonly DependencyProperty LeftOperationIdProperty =
        DependencyProperty.Register(
            nameof(LeftOperationId),
            typeof(string),
            typeof(LinkedPairSliderRow),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty RightOperationIdProperty =
        DependencyProperty.Register(
            nameof(RightOperationId),
            typeof(string),
            typeof(LinkedPairSliderRow),
            new PropertyMetadata(string.Empty));

    public event EventHandler<FaceDetailSliderAdjustmentEventArgs>? SliderPreviewChanged;

    public event EventHandler<FaceDetailSliderAdjustmentEventArgs>? SliderCommitted;

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double LeftValue
    {
        get => (double)GetValue(LeftValueProperty);
        set => SetValue(LeftValueProperty, value);
    }

    public double RightValue
    {
        get => (double)GetValue(RightValueProperty);
        set => SetValue(RightValueProperty, value);
    }

    public bool IsLinked
    {
        get => (bool)GetValue(IsLinkedProperty);
        set => SetValue(IsLinkedProperty, value);
    }

    public bool UseStackedLayout
    {
        get => (bool)GetValue(UseStackedLayoutProperty);
        set => SetValue(UseStackedLayoutProperty, value);
    }

    public string LeftOperationId
    {
        get => (string)GetValue(LeftOperationIdProperty);
        set => SetValue(LeftOperationIdProperty, value);
    }

    public string RightOperationId
    {
        get => (string)GetValue(RightOperationIdProperty);
        set => SetValue(RightOperationIdProperty, value);
    }

    private static object CoerceSliderValue(DependencyObject d, object baseValue)
    {
        return Math.Clamp(Math.Round((double)baseValue), 0, 100);
    }

    private static void OnLeftValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LinkedPairSliderRow row)
        {
            row.SyncLinkedValue(isLeftSource: true, (double)e.NewValue);
        }
    }

    private static void OnRightValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LinkedPairSliderRow row)
        {
            row.SyncLinkedValue(isLeftSource: false, (double)e.NewValue);
        }
    }

    private static void OnIsLinkedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not LinkedPairSliderRow row)
        {
            return;
        }

        row.UpdateLinkButtonChrome();

        if ((bool)e.NewValue)
        {
            row.SyncLinkedValue(isLeftSource: true, row.LeftValue);
        }
    }

    private void SyncLinkedValue(bool isLeftSource, double value)
    {
        if (_syncingPairValues || !IsLinked)
        {
            return;
        }

        _syncingPairValues = true;
        try
        {
            if (isLeftSource)
            {
                RightValue = value;
            }
            else
            {
                LeftValue = value;
            }
        }
        finally
        {
            _syncingPairValues = false;
        }
    }

    private void UpdateLinkButtonChrome()
    {
        if (InlineLinkButton is null || StackedLinkButton is null)
        {
            return;
        }

        string toolTip = IsLinked ? "Linked left/right sliders" : "Separate left/right sliders";

        InlineLinkButton.ToolTip = toolTip;
        StackedLinkButton.ToolTip = toolTip;
    }

    private void LeftSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginSliderInteraction(isLeft: true);
    }

    private void RightSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginSliderInteraction(isLeft: false);
    }

    private void LeftSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndSliderInteraction(sender, isLeft: true);
    }

    private void RightSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        EndSliderInteraction(sender, isLeft: false);
    }

    private void LeftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RaisePreviewIfUserInteracting(sender, isLeft: true, e.NewValue);
    }

    private void RightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        RaisePreviewIfUserInteracting(sender, isLeft: false, e.NewValue);
    }

    private void LeftSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        CommitKeyboardSliderIfNeeded(sender, isLeft: true, e.Key);
    }

    private void RightSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        CommitKeyboardSliderIfNeeded(sender, isLeft: false, e.Key);
    }

    private void BeginSliderInteraction(bool isLeft)
    {
        if (isLeft)
        {
            _isLeftSliderInteracting = true;
        }
        else
        {
            _isRightSliderInteracting = true;
        }

        _lastPreviewOperationId = null;
        _lastPreviewValue = double.NaN;
    }

    private void EndSliderInteraction(object sender, bool isLeft)
    {
        if (sender is Slider slider)
        {
            slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        }

        if (isLeft)
        {
            _isLeftSliderInteracting = false;
        }
        else
        {
            _isRightSliderInteracting = false;
        }

        RaiseCommitted(isLeft, isLeft ? LeftValue : RightValue);
    }

    private void RaisePreviewIfUserInteracting(object sender, bool isLeft, double value)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        bool isInteracting = isLeft ? _isLeftSliderInteracting : _isRightSliderInteracting;
        if (!isInteracting && (Mouse.LeftButton != MouseButtonState.Pressed || !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!isInteracting)
        {
            BeginSliderInteraction(isLeft);
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        string operationId = isLeft ? LeftOperationId : RightOperationId;
        double previewValue = Math.Clamp(Math.Round(value), 0, 100);
        if (string.IsNullOrWhiteSpace(operationId) ||
            (string.Equals(_lastPreviewOperationId, operationId, StringComparison.Ordinal) &&
             Math.Abs(_lastPreviewValue - previewValue) <= 0.001))
        {
            return;
        }

        _lastPreviewOperationId = operationId;
        _lastPreviewValue = previewValue;
        SliderPreviewChanged?.Invoke(this, new FaceDetailSliderAdjustmentEventArgs(operationId, previewValue, isLeft));
    }

    private void CommitKeyboardSliderIfNeeded(object sender, bool isLeft, Key key)
    {
        if (!IsSliderCommitKey(key))
        {
            return;
        }

        if (sender is Slider slider)
        {
            slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        }

        RaiseCommitted(isLeft, isLeft ? LeftValue : RightValue);
    }

    private void RaiseCommitted(bool isLeft, double value)
    {
        string operationId = isLeft ? LeftOperationId : RightOperationId;
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        double committedValue = Math.Clamp(Math.Round(value), 0, 100);
        SliderCommitted?.Invoke(this, new FaceDetailSliderAdjustmentEventArgs(operationId, committedValue, isLeft));
    }

    private static bool IsSliderCommitKey(Key key)
    {
        return key is Key.Left or
            Key.Right or
            Key.Up or
            Key.Down or
            Key.PageUp or
            Key.PageDown or
            Key.Home or
            Key.End;
    }
}
