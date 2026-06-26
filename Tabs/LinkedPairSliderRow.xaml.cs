using System.Windows;

namespace KRetouchStudio.Tabs;

public partial class LinkedPairSliderRow : System.Windows.Controls.UserControl
{
    private bool _syncingPairValues;

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
}
