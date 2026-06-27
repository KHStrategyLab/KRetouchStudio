using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

using System.Windows.Controls;
using System.Windows.Input;

namespace KRetouchStudio.Tabs;

public partial class FaceDetailTabView : System.Windows.Controls.UserControl, INotifyPropertyChanged
{
    private enum FaceDetailTab
    {
        Eyes,
        Brows,
        Nose,
        Mouth,
        Neck
    }

    private FaceDetailTab _activeTab = FaceDetailTab.Eyes;
    private const double NeutralFaceDetailSliderValue = 50.0;

    private double _eyeSize = NeutralFaceDetailSliderValue;
    private double _leftEyeHeight = NeutralFaceDetailSliderValue;
    private double _rightEyeHeight = NeutralFaceDetailSliderValue;
    private double _leftEyeWidth = NeutralFaceDetailSliderValue;
    private double _rightEyeWidth = NeutralFaceDetailSliderValue;
    private double _leftEyeTilt = NeutralFaceDetailSliderValue;
    private double _rightEyeTilt = NeutralFaceDetailSliderValue;
    private double _eyeDistance = NeutralFaceDetailSliderValue;
    private double _leftDarkCircle;
    private double _rightDarkCircle;
    private double _leftUnderEye;
    private double _rightUnderEye;

    private double _leftBrowThickness = NeutralFaceDetailSliderValue;
    private double _rightBrowThickness = NeutralFaceDetailSliderValue;
    private double _browDistance = NeutralFaceDetailSliderValue;
    private double _leftBrowTilt = NeutralFaceDetailSliderValue;
    private double _rightBrowTilt = NeutralFaceDetailSliderValue;
    private double _leftBrowArch = NeutralFaceDetailSliderValue;
    private double _rightBrowArch = NeutralFaceDetailSliderValue;
    private double _leftBrowPosition = NeutralFaceDetailSliderValue;
    private double _rightBrowPosition = NeutralFaceDetailSliderValue;
    private double _leftBrowTail = NeutralFaceDetailSliderValue;
    private double _rightBrowTail = NeutralFaceDetailSliderValue;

    private double _noseSize = NeutralFaceDetailSliderValue;
    private double _noseLength = NeutralFaceDetailSliderValue;
    private double _noseBridge = NeutralFaceDetailSliderValue;
    private double _noseWidth = NeutralFaceDetailSliderValue;
    private double _noseTip = NeutralFaceDetailSliderValue;
    private double _leftNostril = NeutralFaceDetailSliderValue;
    private double _rightNostril = NeutralFaceDetailSliderValue;

    private double _mouthSize = NeutralFaceDetailSliderValue;
    private double _mouthWidth = NeutralFaceDetailSliderValue;
    private double _mouthVertical = NeutralFaceDetailSliderValue;
    private double _leftMouthCorner = NeutralFaceDetailSliderValue;
    private double _rightMouthCorner = NeutralFaceDetailSliderValue;
    private double _leftSmileBalance = NeutralFaceDetailSliderValue;
    private double _rightSmileBalance = NeutralFaceDetailSliderValue;
    private double _upperLip = NeutralFaceDetailSliderValue;
    private double _lowerLip = NeutralFaceDetailSliderValue;

    private double _neckSlim = NeutralFaceDetailSliderValue;
    private double _neckLength = NeutralFaceDetailSliderValue;
    private double _neckWrinkle;
    private double _doubleChin;
    private double _leftSideNeck = NeutralFaceDetailSliderValue;
    private double _rightSideNeck = NeutralFaceDetailSliderValue;
    private double _leftShoulderNeck = NeutralFaceDetailSliderValue;
    private double _rightShoulderNeck = NeutralFaceDetailSliderValue;
    private bool _isSingleSliderInteracting;
    private string? _lastSinglePreviewOperationId;
    private double _lastSinglePreviewValue = double.NaN;

    public FaceDetailTabView()
    {
        InitializeComponent();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<FaceDetailAdjustmentEventArgs>? FaceDetailAdjustmentPreviewChanged;

    public event EventHandler<FaceDetailAdjustmentEventArgs>? FaceDetailAdjustmentCommitted;

    public bool IsEyesTabActive => _activeTab == FaceDetailTab.Eyes;

    public bool IsBrowsTabActive => _activeTab == FaceDetailTab.Brows;

    public bool IsNoseTabActive => _activeTab == FaceDetailTab.Nose;

    public bool IsMouthTabActive => _activeTab == FaceDetailTab.Mouth;

    public bool IsNeckTabActive => _activeTab == FaceDetailTab.Neck;

    public Visibility EyesPanelVisibility => GetTabVisibility(FaceDetailTab.Eyes);

    public Visibility BrowsPanelVisibility => GetTabVisibility(FaceDetailTab.Brows);

    public Visibility NosePanelVisibility => GetTabVisibility(FaceDetailTab.Nose);

    public Visibility MouthPanelVisibility => GetTabVisibility(FaceDetailTab.Mouth);

    public Visibility NeckPanelVisibility => GetTabVisibility(FaceDetailTab.Neck);

    public double EyeSize
    {
        get => _eyeSize;
        set => SetSliderValue(ref _eyeSize, value);
    }

    public double LeftEyeHeight
    {
        get => _leftEyeHeight;
        set => SetSliderValue(ref _leftEyeHeight, value);
    }

    public double RightEyeHeight
    {
        get => _rightEyeHeight;
        set => SetSliderValue(ref _rightEyeHeight, value);
    }

    public double LeftEyeWidth
    {
        get => _leftEyeWidth;
        set => SetSliderValue(ref _leftEyeWidth, value);
    }

    public double RightEyeWidth
    {
        get => _rightEyeWidth;
        set => SetSliderValue(ref _rightEyeWidth, value);
    }

    public double LeftEyeTilt
    {
        get => _leftEyeTilt;
        set => SetSliderValue(ref _leftEyeTilt, value);
    }

    public double RightEyeTilt
    {
        get => _rightEyeTilt;
        set => SetSliderValue(ref _rightEyeTilt, value);
    }

    public double EyeDistance
    {
        get => _eyeDistance;
        set => SetSliderValue(ref _eyeDistance, value);
    }

    public double LeftDarkCircle
    {
        get => _leftDarkCircle;
        set => SetSliderValue(ref _leftDarkCircle, value);
    }

    public double RightDarkCircle
    {
        get => _rightDarkCircle;
        set => SetSliderValue(ref _rightDarkCircle, value);
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

    public double LeftBrowThickness
    {
        get => _leftBrowThickness;
        set => SetSliderValue(ref _leftBrowThickness, value);
    }

    public double RightBrowThickness
    {
        get => _rightBrowThickness;
        set => SetSliderValue(ref _rightBrowThickness, value);
    }

    public double BrowDistance
    {
        get => _browDistance;
        set => SetSliderValue(ref _browDistance, value);
    }

    public double LeftBrowTilt
    {
        get => _leftBrowTilt;
        set => SetSliderValue(ref _leftBrowTilt, value);
    }

    public double RightBrowTilt
    {
        get => _rightBrowTilt;
        set => SetSliderValue(ref _rightBrowTilt, value);
    }

    public double LeftBrowArch
    {
        get => _leftBrowArch;
        set => SetSliderValue(ref _leftBrowArch, value);
    }

    public double RightBrowArch
    {
        get => _rightBrowArch;
        set => SetSliderValue(ref _rightBrowArch, value);
    }

    public double LeftBrowPosition
    {
        get => _leftBrowPosition;
        set => SetSliderValue(ref _leftBrowPosition, value);
    }

    public double RightBrowPosition
    {
        get => _rightBrowPosition;
        set => SetSliderValue(ref _rightBrowPosition, value);
    }

    public double LeftBrowTail
    {
        get => _leftBrowTail;
        set => SetSliderValue(ref _leftBrowTail, value);
    }

    public double RightBrowTail
    {
        get => _rightBrowTail;
        set => SetSliderValue(ref _rightBrowTail, value);
    }

    public double NoseSize
    {
        get => _noseSize;
        set => SetSliderValue(ref _noseSize, value);
    }

    public double NoseLength
    {
        get => _noseLength;
        set => SetSliderValue(ref _noseLength, value);
    }

    public double NoseBridge
    {
        get => _noseBridge;
        set => SetSliderValue(ref _noseBridge, value);
    }

    public double NoseWidth
    {
        get => _noseWidth;
        set => SetSliderValue(ref _noseWidth, value);
    }

    public double NoseTip
    {
        get => _noseTip;
        set => SetSliderValue(ref _noseTip, value);
    }

    public double LeftNostril
    {
        get => _leftNostril;
        set => SetSliderValue(ref _leftNostril, value);
    }

    public double RightNostril
    {
        get => _rightNostril;
        set => SetSliderValue(ref _rightNostril, value);
    }

    public double MouthSize
    {
        get => _mouthSize;
        set => SetSliderValue(ref _mouthSize, value);
    }

    public double MouthWidth
    {
        get => _mouthWidth;
        set => SetSliderValue(ref _mouthWidth, value);
    }

    public double MouthVertical
    {
        get => _mouthVertical;
        set => SetSliderValue(ref _mouthVertical, value);
    }

    public double LeftMouthCorner
    {
        get => _leftMouthCorner;
        set => SetSliderValue(ref _leftMouthCorner, value);
    }

    public double RightMouthCorner
    {
        get => _rightMouthCorner;
        set => SetSliderValue(ref _rightMouthCorner, value);
    }

    public double LeftSmileBalance
    {
        get => _leftSmileBalance;
        set => SetSliderValue(ref _leftSmileBalance, value);
    }

    public double RightSmileBalance
    {
        get => _rightSmileBalance;
        set => SetSliderValue(ref _rightSmileBalance, value);
    }

    public double UpperLip
    {
        get => _upperLip;
        set => SetSliderValue(ref _upperLip, value);
    }

    public double LowerLip
    {
        get => _lowerLip;
        set => SetSliderValue(ref _lowerLip, value);
    }

    public double NeckSlim
    {
        get => _neckSlim;
        set => SetSliderValue(ref _neckSlim, value);
    }

    public double NeckLength
    {
        get => _neckLength;
        set => SetSliderValue(ref _neckLength, value);
    }

    public double NeckWrinkle
    {
        get => _neckWrinkle;
        set => SetSliderValue(ref _neckWrinkle, value);
    }

    public double DoubleChin
    {
        get => _doubleChin;
        set => SetSliderValue(ref _doubleChin, value);
    }

    public double LeftSideNeck
    {
        get => _leftSideNeck;
        set => SetSliderValue(ref _leftSideNeck, value);
    }

    public double RightSideNeck
    {
        get => _rightSideNeck;
        set => SetSliderValue(ref _rightSideNeck, value);
    }

    public double LeftShoulderNeck
    {
        get => _leftShoulderNeck;
        set => SetSliderValue(ref _leftShoulderNeck, value);
    }

    public double RightShoulderNeck
    {
        get => _rightShoulderNeck;
        set => SetSliderValue(ref _rightShoulderNeck, value);
    }

    public void Collapse()
    {
        FaceDetailExpander.IsExpanded = false;
    }

    public void ResetForPhotoChange()
    {
        _activeTab = FaceDetailTab.Eyes;

        _eyeSize = NeutralFaceDetailSliderValue;
        _leftEyeHeight = NeutralFaceDetailSliderValue;
        _rightEyeHeight = NeutralFaceDetailSliderValue;
        _leftEyeWidth = NeutralFaceDetailSliderValue;
        _rightEyeWidth = NeutralFaceDetailSliderValue;
        _leftEyeTilt = NeutralFaceDetailSliderValue;
        _rightEyeTilt = NeutralFaceDetailSliderValue;
        _eyeDistance = NeutralFaceDetailSliderValue;
        _leftDarkCircle = 0;
        _rightDarkCircle = 0;
        _leftUnderEye = 0;
        _rightUnderEye = 0;

        _leftBrowThickness = NeutralFaceDetailSliderValue;
        _rightBrowThickness = NeutralFaceDetailSliderValue;
        _browDistance = NeutralFaceDetailSliderValue;
        _leftBrowTilt = NeutralFaceDetailSliderValue;
        _rightBrowTilt = NeutralFaceDetailSliderValue;
        _leftBrowArch = NeutralFaceDetailSliderValue;
        _rightBrowArch = NeutralFaceDetailSliderValue;
        _leftBrowPosition = NeutralFaceDetailSliderValue;
        _rightBrowPosition = NeutralFaceDetailSliderValue;
        _leftBrowTail = NeutralFaceDetailSliderValue;
        _rightBrowTail = NeutralFaceDetailSliderValue;

        _noseSize = NeutralFaceDetailSliderValue;
        _noseLength = NeutralFaceDetailSliderValue;
        _noseBridge = NeutralFaceDetailSliderValue;
        _noseWidth = NeutralFaceDetailSliderValue;
        _noseTip = NeutralFaceDetailSliderValue;
        _leftNostril = NeutralFaceDetailSliderValue;
        _rightNostril = NeutralFaceDetailSliderValue;

        _mouthSize = NeutralFaceDetailSliderValue;
        _mouthWidth = NeutralFaceDetailSliderValue;
        _mouthVertical = NeutralFaceDetailSliderValue;
        _leftMouthCorner = NeutralFaceDetailSliderValue;
        _rightMouthCorner = NeutralFaceDetailSliderValue;
        _leftSmileBalance = NeutralFaceDetailSliderValue;
        _rightSmileBalance = NeutralFaceDetailSliderValue;
        _upperLip = NeutralFaceDetailSliderValue;
        _lowerLip = NeutralFaceDetailSliderValue;

        _neckSlim = NeutralFaceDetailSliderValue;
        _neckLength = NeutralFaceDetailSliderValue;
        _neckWrinkle = 0;
        _doubleChin = 0;
        _leftSideNeck = NeutralFaceDetailSliderValue;
        _rightSideNeck = NeutralFaceDetailSliderValue;
        _leftShoulderNeck = NeutralFaceDetailSliderValue;
        _rightShoulderNeck = NeutralFaceDetailSliderValue;

        OnPropertyChanged(string.Empty);
    }

    private void FaceDetailSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSingleSliderInteracting = true;
        _lastSinglePreviewOperationId = null;
        _lastSinglePreviewValue = double.NaN;
    }

    private void FaceDetailSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        _isSingleSliderInteracting = false;
        RaiseFaceDetailCommitted(GetOperationId(slider), slider.Value);
    }

    private void FaceDetailSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not Slider slider)
        {
            return;
        }

        if (!_isSingleSliderInteracting &&
            (Mouse.LeftButton != MouseButtonState.Pressed || !slider.IsMouseCaptureWithin))
        {
            return;
        }

        if (!_isSingleSliderInteracting)
        {
            _isSingleSliderInteracting = true;
            _lastSinglePreviewOperationId = null;
            _lastSinglePreviewValue = double.NaN;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        string operationId = GetOperationId(slider);
        double previewValue = Math.Clamp(Math.Round(e.NewValue), 0, 100);
        if (string.IsNullOrWhiteSpace(operationId) ||
            (string.Equals(_lastSinglePreviewOperationId, operationId, StringComparison.Ordinal) &&
             Math.Abs(_lastSinglePreviewValue - previewValue) <= 0.001))
        {
            return;
        }

        _lastSinglePreviewOperationId = operationId;
        _lastSinglePreviewValue = previewValue;
        RaiseFaceDetailPreview(operationId, previewValue);
    }

    private void FaceDetailSlider_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!IsSliderCommitKey(e.Key) || sender is not Slider slider)
        {
            return;
        }

        slider.GetBindingExpression(Slider.ValueProperty)?.UpdateSource();
        RaiseFaceDetailCommitted(GetOperationId(slider), slider.Value);
    }

    private void PairSlider_SliderPreviewChanged(object? sender, FaceDetailSliderAdjustmentEventArgs e)
    {
        RaiseFaceDetailPreview(e.OperationId, e.Value);
    }

    private void PairSlider_SliderCommitted(object? sender, FaceDetailSliderAdjustmentEventArgs e)
    {
        RaiseFaceDetailCommitted(e.OperationId, e.Value);
    }

    private void RaiseFaceDetailPreview(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        FaceDetailAdjustmentPreviewChanged?.Invoke(
            this,
            new FaceDetailAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private void RaiseFaceDetailCommitted(string operationId, double value)
    {
        if (string.IsNullOrWhiteSpace(operationId))
        {
            return;
        }

        FaceDetailAdjustmentCommitted?.Invoke(
            this,
            new FaceDetailAdjustmentEventArgs(operationId, Math.Clamp(Math.Round(value), 0, 100), CreateSnapshot()));
    }

    private FaceDetailAdjustmentSnapshot CreateSnapshot()
    {
        return new FaceDetailAdjustmentSnapshot(
            EyeSize,
            LeftEyeHeight,
            RightEyeHeight,
            LeftEyeWidth,
            RightEyeWidth,
            LeftEyeTilt,
            RightEyeTilt,
            EyeDistance,
            LeftDarkCircle,
            RightDarkCircle,
            LeftUnderEye,
            RightUnderEye,
            LeftBrowThickness,
            RightBrowThickness,
            BrowDistance,
            LeftBrowTilt,
            RightBrowTilt,
            LeftBrowArch,
            RightBrowArch,
            LeftBrowPosition,
            RightBrowPosition,
            LeftBrowTail,
            RightBrowTail,
            NoseSize,
            NoseLength,
            NoseBridge,
            NoseWidth,
            NoseTip,
            LeftNostril,
            RightNostril,
            MouthSize,
            MouthWidth,
            MouthVertical,
            LeftMouthCorner,
            RightMouthCorner,
            LeftSmileBalance,
            RightSmileBalance,
            UpperLip,
            LowerLip,
            NeckSlim,
            NeckLength,
            NeckWrinkle,
            DoubleChin,
            LeftSideNeck,
            RightSideNeck,
            LeftShoulderNeck,
            RightShoulderNeck);
    }

    private static string GetOperationId(Slider slider)
    {
        return slider.Tag as string ?? string.Empty;
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

    private void Expander_Expanded(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow window)
        {
            window.NotifyRetouchTabExpanded(this);
        }
    }

    private void EyesTabButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(FaceDetailTab.Eyes);
        e.Handled = true;
    }

    private void BrowsTabButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(FaceDetailTab.Brows);
        e.Handled = true;
    }

    private void NoseTabButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(FaceDetailTab.Nose);
        e.Handled = true;
    }

    private void MouthTabButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(FaceDetailTab.Mouth);
        e.Handled = true;
    }

    private void NeckTabButton_Click(object sender, RoutedEventArgs e)
    {
        SetActiveTab(FaceDetailTab.Neck);
        e.Handled = true;
    }

    private void SetActiveTab(FaceDetailTab tab)
    {
        if (_activeTab == tab)
        {
            NotifyActiveTabProperties();
            return;
        }

        _activeTab = tab;
        NotifyActiveTabProperties();
    }

    private Visibility GetTabVisibility(FaceDetailTab tab)
    {
        return _activeTab == tab ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NotifyActiveTabProperties()
    {
        OnPropertyChanged(nameof(IsEyesTabActive));
        OnPropertyChanged(nameof(IsBrowsTabActive));
        OnPropertyChanged(nameof(IsNoseTabActive));
        OnPropertyChanged(nameof(IsMouthTabActive));
        OnPropertyChanged(nameof(IsNeckTabActive));
        OnPropertyChanged(nameof(EyesPanelVisibility));
        OnPropertyChanged(nameof(BrowsPanelVisibility));
        OnPropertyChanged(nameof(NosePanelVisibility));
        OnPropertyChanged(nameof(MouthPanelVisibility));
        OnPropertyChanged(nameof(NeckPanelVisibility));
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

public sealed class FaceDetailSliderAdjustmentEventArgs(
    string operationId,
    double value,
    bool isLeftSide) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public bool IsLeftSide { get; } = isLeftSide;
}

public sealed class FaceDetailAdjustmentEventArgs(
    string operationId,
    double value,
    FaceDetailAdjustmentSnapshot snapshot) : EventArgs
{
    public string OperationId { get; } = operationId;

    public double Value { get; } = value;

    public FaceDetailAdjustmentSnapshot Snapshot { get; } = snapshot;
}

public sealed record FaceDetailAdjustmentSnapshot(
    double EyeSize,
    double LeftEyeHeight,
    double RightEyeHeight,
    double LeftEyeWidth,
    double RightEyeWidth,
    double LeftEyeTilt,
    double RightEyeTilt,
    double EyeDistance,
    double LeftDarkCircle,
    double RightDarkCircle,
    double LeftUnderEye,
    double RightUnderEye,
    double LeftBrowThickness,
    double RightBrowThickness,
    double BrowDistance,
    double LeftBrowTilt,
    double RightBrowTilt,
    double LeftBrowArch,
    double RightBrowArch,
    double LeftBrowPosition,
    double RightBrowPosition,
    double LeftBrowTail,
    double RightBrowTail,
    double NoseSize,
    double NoseLength,
    double NoseBridge,
    double NoseWidth,
    double NoseTip,
    double LeftNostril,
    double RightNostril,
    double MouthSize,
    double MouthWidth,
    double MouthVertical,
    double LeftMouthCorner,
    double RightMouthCorner,
    double LeftSmileBalance,
    double RightSmileBalance,
    double UpperLip,
    double LowerLip,
    double NeckSlim,
    double NeckLength,
    double NeckWrinkle,
    double DoubleChin,
    double LeftSideNeck,
    double RightSideNeck,
    double LeftShoulderNeck,
    double RightShoulderNeck);
