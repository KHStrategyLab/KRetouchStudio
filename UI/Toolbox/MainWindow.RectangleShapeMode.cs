using System;
using System.Collections.Generic;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    private string _rectangleShapeMode = "rectangle";

    public string RectangleShapeMode
    {
        get => _rectangleShapeMode;
        private set
        {
            if (string.Equals(_rectangleShapeMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _rectangleShapeMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleOutlineSelectionVisibility));
            OnPropertyChanged(nameof(RectangleEllipseSelectionVisibility));
            OnPropertyChanged(nameof(RectanglePolygonSelectionVisibility));
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
        }
    }

    private void RectangleShapeModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        RectangleShapeMode = mode;
        UpdateRectangleShapeModeSelection();
    }

    private void UpdateRectangleShapeModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetRectangleShapeModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, RectangleShapeMode, StringComparison.OrdinalIgnoreCase);
            button.Background = isActive
                ? (System.Windows.Media.Brush)FindResource("PanelSelectedBg")
                : (System.Windows.Media.Brush)FindResource("SurfacePrimary");
            button.BorderBrush = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("MenuBorder");
            button.Foreground = isActive
                ? (System.Windows.Media.Brush)FindResource("Accent")
                : (System.Windows.Media.Brush)FindResource("TextMain");
        }
    }

    private IEnumerable<System.Windows.Controls.Button> GetRectangleShapeModeButtons()
    {
        yield return RectangleShapeModeButton;
        yield return EllipseShapeModeButton;
        yield return PolygonShapeModeButton;
    }
}
