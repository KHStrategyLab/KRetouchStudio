using System;
using System.Windows;
using System.Windows.Input;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility SelectToolOptionsVisibility => string.Equals(ActiveToolId, "select", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    private bool CanUseSelectTool()
    {
        return string.Equals(ActiveToolId, "select", StringComparison.OrdinalIgnoreCase);
    }

    private void HandleSelectToolPreviewTileMouseDown(PhotoItem photo, MouseButtonEventArgs e)
    {
        // Select mode is a neutral browsing mode for now.
        // keep clicks in preview as no-op (no selection mutation).
        e.Handled = true;
    }
}
