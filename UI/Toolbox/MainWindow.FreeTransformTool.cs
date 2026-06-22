using System;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    private double _freeTransformAngle;
    private bool _freeTransformKeepRatio = true;
    private string _freeTransformStatusText = "Preview box only. Transform apply is not connected yet.";

    public Visibility FreeTransformToolOptionsVisibility => string.Equals(ActiveToolId, "freetransform", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility FreeTransformOverlayVisibility => string.Equals(ActiveToolId, "freetransform", StringComparison.OrdinalIgnoreCase) &&
                                                        CanUseSinglePreviewTool()
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool FreeTransformKeepRatio
    {
        get => _freeTransformKeepRatio;
        set
        {
            if (_freeTransformKeepRatio == value)
            {
                return;
            }

            _freeTransformKeepRatio = value;
            OnPropertyChanged();
        }
    }

    public double FreeTransformAngle
    {
        get => _freeTransformAngle;
        set
        {
            double clamped = Math.Clamp(value, -180, 180);
            if (Math.Abs(_freeTransformAngle - clamped) < 0.001)
            {
                return;
            }

            _freeTransformAngle = clamped;
            OnPropertyChanged();
        }
    }

    public string FreeTransformSizeText => SelectedPhoto is null
        ? "W 0 / H 0"
        : $"W {GetCurrentDisplayBitmapSource(SelectedPhoto).PixelWidth} / H {GetCurrentDisplayBitmapSource(SelectedPhoto).PixelHeight}";

    public string FreeTransformStatusText
    {
        get => _freeTransformStatusText;
        private set
        {
            if (string.Equals(_freeTransformStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _freeTransformStatusText = value;
            OnPropertyChanged();
        }
    }

    private void ResetFreeTransformShell()
    {
        FreeTransformAngle = 0;
        FreeTransformStatusText = CanUseSinglePreviewTool()
            ? "Preview box only. Resize and rotate are not connected yet."
            : "Single image only";
        OnPropertyChanged(nameof(FreeTransformOverlayVisibility));
        OnPropertyChanged(nameof(FreeTransformSizeText));
    }

    private void FreeTransformApplyButton_Click(object sender, RoutedEventArgs e)
    {
        FreeTransformStatusText = "Apply is not connected yet. This tool currently shows the preview box only.";
    }

    private void FreeTransformCancelButton_Click(object sender, RoutedEventArgs e)
    {
        ActivateToolById("select");
    }

    private void FreeTransformResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetFreeTransformShell();
    }
}
