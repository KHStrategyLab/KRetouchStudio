using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility TypeToolOptionsVisibility => string.Equals(ActiveToolId, "type", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public Visibility TypeTextOverlayVisibility => SelectedPhoto is not null &&
                                                   SelectedPreviewPhotos.Count == 1 &&
                                                   TypeTextItems.Count > 0
        ? Visibility.Visible
        : Visibility.Collapsed;

    public bool TypeToolHitTestEnabled => CanUseTypeTool();

    public string TypeToolFontFamily
    {
        get => _typeToolFontFamily;
        set
        {
            string next = string.IsNullOrWhiteSpace(value) ? "Malgun Gothic" : value.Trim();
            if (string.Equals(_typeToolFontFamily, next, StringComparison.Ordinal))
            {
                return;
            }

            _typeToolFontFamily = next;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public double TypeToolFontSize
    {
        get => _typeToolFontSize;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 6, 400);
            if (Math.Abs(_typeToolFontSize - clamped) < 0.01)
            {
                return;
            }

            _typeToolFontSize = clamped;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public string TypeToolColorHex
    {
        get => _typeToolColorHex;
        set
        {
            if (!TryBuildTypeToolBrush(value, out System.Windows.Media.Brush? brush, out string? normalized))
            {
                return;
            }

            if (string.Equals(_typeToolColorHex, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _typeToolColorHex = normalized!;
            _typeToolColorPreview = brush!;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TypeToolColorPreview));
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public System.Windows.Media.Brush TypeToolColorPreview => _typeToolColorPreview;

    public double TypeToolOpacity
    {
        get => _typeToolOpacity;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 100);
            if (Math.Abs(_typeToolOpacity - clamped) < 0.01)
            {
                return;
            }

            _typeToolOpacity = clamped;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public bool TypeToolBold
    {
        get => _typeToolBold;
        set
        {
            if (_typeToolBold == value)
            {
                return;
            }

            _typeToolBold = value;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public bool TypeToolItalic
    {
        get => _typeToolItalic;
        set
        {
            if (_typeToolItalic == value)
            {
                return;
            }

            _typeToolItalic = value;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
        }
    }

    public TextAlignment TypeToolTextAlignment
    {
        get => _typeToolTextAlignment;
        private set
        {
            if (_typeToolTextAlignment == value)
            {
                return;
            }

            _typeToolTextAlignment = value;
            OnPropertyChanged();
            ApplyTypeToolSettingsToSelectedItem();
            UpdateTypeToolAlignmentSelection();
        }
    }

    public string TypeToolStatusText
    {
        get => _typeToolStatusText;
        private set
        {
            _typeToolStatusText = value;
            OnPropertyChanged();
        }
    }

    private void TypeToolAlignmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string alignment)
        {
            return;
        }

        TypeToolTextAlignment = alignment.ToLowerInvariant() switch
        {
            "center" => TextAlignment.Center,
            "right" => TextAlignment.Right,
            _ => TextAlignment.Left
        };
    }

    private void TypeToolDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveSelectedTypeTextItem();
    }

    private bool CanUseTypeTool()
    {
        return string.Equals(ActiveToolId, "type", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void BeginTypeTextCreation(System.Windows.Point previewPoint)
    {
        if (!CanUseTypeTool() || SelectedPhoto is null || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        CommitTypeTextEdit();
        PreviewTextItem item = new($"type_{TypeTextItems.Count + 1}", string.Empty, imagePoint.X, imagePoint.Y);
        ApplyCurrentTypeToolDefaults(item);
        TypeTextItems.Add(item);

        _creatingTypeTextItem = item;
        _isTypeTextCreating = true;
        _typeTextCreateStartPoint = previewPoint;
        _typeTextCreateStartImageX = imagePoint.X;
        _typeTextCreateStartImageY = imagePoint.Y;

        SelectTypeTextItem(item);
        UpdateTypeTextItemPositions();
        UpdateTypeToolVisualState();
        Mouse.Capture(PreviewSurface);
    }

    private void UpdateTypeTextCreation(System.Windows.Point previewPoint)
    {
        if (!_isTypeTextCreating || _creatingTypeTextItem is null || SelectedPhoto is null || !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        bool useTextBox = Math.Abs(previewPoint.X - _typeTextCreateStartPoint.X) >= 6 ||
                          Math.Abs(previewPoint.Y - _typeTextCreateStartPoint.Y) >= 6;

        if (!useTextBox)
        {
            _creatingTypeTextItem.IsPointText = true;
            _creatingTypeTextItem.MoveOriginal(_typeTextCreateStartImageX, _typeTextCreateStartImageY);
            _creatingTypeTextItem.ResizeOriginal(0, 0);
            UpdateTypeTextItemPositions();
            return;
        }

        double left = Math.Min(_typeTextCreateStartImageX, imagePoint.X);
        double top = Math.Min(_typeTextCreateStartImageY, imagePoint.Y);
        double width = Math.Abs(imagePoint.X - _typeTextCreateStartImageX);
        double height = Math.Abs(imagePoint.Y - _typeTextCreateStartImageY);

        _creatingTypeTextItem.IsPointText = false;
        _creatingTypeTextItem.MoveOriginal(left, top);
        _creatingTypeTextItem.ResizeOriginal(width, height);
        UpdateTypeTextItemPositions();
    }

    private void StopTypeTextCreation()
    {
        if (!_isTypeTextCreating)
        {
            return;
        }

        PreviewTextItem? item = _creatingTypeTextItem;
        _isTypeTextCreating = false;
        _creatingTypeTextItem = null;
        Mouse.Capture(null);

        if (item is not null)
        {
            BeginTypeTextEdit(item);
        }
    }

    private void TypeTextItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!CanUseTypeTool() ||
            sender is not FrameworkElement element ||
            element.DataContext is not PreviewTextItem item)
        {
            return;
        }

        if (_selectedTypeTextItem is not null &&
            !ReferenceEquals(_selectedTypeTextItem, item) &&
            _selectedTypeTextItem.IsEditing)
        {
            CommitTypeTextEdit();
        }

        SelectTypeTextItem(item);
        if (e.ClickCount >= 2)
        {
            BeginTypeTextEdit(item);
            e.Handled = true;
            return;
        }

        if (item.IsEditing)
        {
            return;
        }

        _draggingTypeTextItem = item;
        _isTypeTextDragging = true;
        _typeTextDragStartPoint = e.GetPosition(PreviewSurface);
        _typeTextDragStartX = item.OriginalX;
        _typeTextDragStartY = item.OriginalY;
        Mouse.Capture(element);
        e.Handled = true;
    }

    private void TypeTextItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!CanUseTypeTool() || !_isTypeTextDragging || _draggingTypeTextItem is null || SelectedPhoto is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        double imageWidth = SelectedPhoto.BaseImage.PixelWidth;
        double imageHeight = SelectedPhoto.BaseImage.PixelHeight;
        if (!TryGetPreviewImageTransform(imageWidth, imageHeight, out _, out _, out double scale) || scale <= 0)
        {
            return;
        }

        System.Windows.Point currentPoint = e.GetPosition(PreviewSurface);
        Vector delta = currentPoint - _typeTextDragStartPoint;
        double maxX = Math.Max(0, imageWidth - (_draggingTypeTextItem.IsPointText ? 1 : _draggingTypeTextItem.OriginalWidth));
        double maxY = Math.Max(0, imageHeight - (_draggingTypeTextItem.IsPointText ? 1 : _draggingTypeTextItem.OriginalHeight));
        _draggingTypeTextItem.MoveOriginal(
            Math.Clamp(_typeTextDragStartX + delta.X / scale, 0, maxX),
            Math.Clamp(_typeTextDragStartY + delta.Y / scale, 0, maxY));
        UpdateTypeTextItemPositions();
        e.Handled = true;
    }

    private void TypeTextItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewTextItem? draggedItem = _draggingTypeTextItem;
        if (draggedItem is null)
        {
            return;
        }

        bool moved = Math.Abs(draggedItem.OriginalX - _typeTextDragStartX) >= 0.01 ||
                     Math.Abs(draggedItem.OriginalY - _typeTextDragStartY) >= 0.01;
        _draggingTypeTextItem = null;
        _isTypeTextDragging = false;
        Mouse.Capture(null);
        if (moved)
        {
            PushEditorHistorySnapshot("Type", "Text moved");
        }

        e.Handled = true;
    }

    private void TypeTextEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox ||
            textBox.DataContext is not PreviewTextItem item ||
            !item.IsEditing)
        {
            return;
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (item.IsEditing)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void TypeTextEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox textBox ||
            textBox.DataContext is not PreviewTextItem item ||
            !ReferenceEquals(item, _selectedTypeTextItem) ||
            !item.IsEditing)
        {
            return;
        }

        CommitTypeTextEdit();
    }

    private void UpdateTypeTextItemPositions()
    {
        if (SelectedPhoto is null || TypeTextItems.Count == 0)
        {
            OnPropertyChanged(nameof(TypeTextOverlayVisibility));
            return;
        }

        double imageWidth = SelectedPhoto.BaseImage.PixelWidth;
        double imageHeight = SelectedPhoto.BaseImage.PixelHeight;
        if (!TryGetPreviewImageTransform(imageWidth, imageHeight, out double offsetX, out double offsetY, out double scale))
        {
            OnPropertyChanged(nameof(TypeTextOverlayVisibility));
            return;
        }

        foreach (PreviewTextItem item in TypeTextItems)
        {
            item.UpdateDisplayBox(
                offsetX + item.OriginalX * scale,
                offsetY + item.OriginalY * scale,
                item.OriginalWidth * scale,
                item.OriginalHeight * scale);
        }

        OnPropertyChanged(nameof(TypeTextOverlayVisibility));
    }

    private void SelectTypeTextItem(PreviewTextItem? item)
    {
        _selectedTypeTextItem = item;
        PullTypeToolSettingsFromSelection(item);
        UpdateTypeToolVisualState();
    }

    private void BeginTypeTextEdit(PreviewTextItem item)
    {
        if (!CanUseTypeTool())
        {
            return;
        }

        SelectTypeTextItem(item);
        _typeTextEditSnapshot = item.Text;
        item.IsEditing = true;
        UpdateTypeToolVisualState();
    }

    private void CommitTypeTextEdit()
    {
        if (_selectedTypeTextItem is null || !_selectedTypeTextItem.IsEditing)
        {
            return;
        }

        _selectedTypeTextItem.IsEditing = false;
        _selectedTypeTextItem.Text = _selectedTypeTextItem.Text?.TrimEnd('\r', '\n') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_selectedTypeTextItem.Text))
        {
            PreviewTextItem itemToRemove = _selectedTypeTextItem;
            _selectedTypeTextItem = null;
            TypeTextItems.Remove(itemToRemove);
        }

        UpdateTypeToolVisualState();
        PushEditorHistorySnapshot("Type", "Text committed");
    }

    private void CancelTypeTextEdit()
    {
        if (_selectedTypeTextItem is null || !_selectedTypeTextItem.IsEditing)
        {
            return;
        }

        _selectedTypeTextItem.Text = _typeTextEditSnapshot;
        _selectedTypeTextItem.IsEditing = false;
        if (string.IsNullOrWhiteSpace(_selectedTypeTextItem.Text))
        {
            PreviewTextItem itemToRemove = _selectedTypeTextItem;
            _selectedTypeTextItem = null;
            TypeTextItems.Remove(itemToRemove);
        }

        UpdateTypeToolVisualState();
    }

    private void RemoveSelectedTypeTextItem()
    {
        if (_selectedTypeTextItem is null)
        {
            return;
        }

        PreviewTextItem itemToRemove = _selectedTypeTextItem;
        _selectedTypeTextItem = null;
        TypeTextItems.Remove(itemToRemove);
        UpdateTypeToolVisualState();
        PushEditorHistorySnapshot("Type", "Text removed");
    }

    private void UpdateTypeToolVisualState()
    {
        bool interactive = CanUseTypeTool();
        foreach (PreviewTextItem item in TypeTextItems)
        {
            bool isSelected = ReferenceEquals(item, _selectedTypeTextItem);
            item.SetVisualState(isSelected, isSelected && item.IsEditing, interactive);
        }

        UpdateTypeToolStatus();
        OnPropertyChanged(nameof(TypeTextOverlayVisibility));
        OnPropertyChanged(nameof(TypeToolHitTestEnabled));
    }

    private void UpdateTypeToolStatus()
    {
        TypeToolStatusText = TypeTextItems.Count == 0
            ? "No text"
            : _selectedTypeTextItem is null
                ? $"Items {TypeTextItems.Count}"
                : _selectedTypeTextItem.IsEditing
                    ? $"Items {TypeTextItems.Count} / Editing"
                    : $"Items {TypeTextItems.Count} / Selected";
    }

    private void ApplyCurrentTypeToolDefaults(PreviewTextItem item)
    {
        item.FontFamilyName = TypeToolFontFamily;
        item.FontSize = TypeToolFontSize;
        item.FontColor = TypeToolColorHex;
        item.OpacityPercent = TypeToolOpacity;
        item.IsBold = TypeToolBold;
        item.IsItalic = TypeToolItalic;
        item.TextAlignment = TypeToolTextAlignment;
    }

    private void ApplyTypeToolSettingsToSelectedItem()
    {
        if (_isTypeToolSettingsSyncing || _selectedTypeTextItem is null)
        {
            return;
        }

        _selectedTypeTextItem.FontFamilyName = TypeToolFontFamily;
        _selectedTypeTextItem.FontSize = TypeToolFontSize;
        _selectedTypeTextItem.FontColor = TypeToolColorHex;
        _selectedTypeTextItem.OpacityPercent = TypeToolOpacity;
        _selectedTypeTextItem.IsBold = TypeToolBold;
        _selectedTypeTextItem.IsItalic = TypeToolItalic;
        _selectedTypeTextItem.TextAlignment = TypeToolTextAlignment;
        PushEditorHistorySnapshot("Type", "Text style updated");
    }

    private void PullTypeToolSettingsFromSelection(PreviewTextItem? item)
    {
        if (item is null)
        {
            UpdateTypeToolAlignmentSelection();
            return;
        }

        _isTypeToolSettingsSyncing = true;
        _typeToolFontFamily = item.FontFamilyName;
        _typeToolFontSize = item.FontSize;
        _typeToolColorHex = item.FontColor;
        if (TryBuildTypeToolBrush(item.FontColor, out System.Windows.Media.Brush? brush, out string? normalized))
        {
            _typeToolColorPreview = brush!;
            _typeToolColorHex = normalized!;
        }

        _typeToolOpacity = item.OpacityPercent;
        _typeToolBold = item.IsBold;
        _typeToolItalic = item.IsItalic;
        _typeToolTextAlignment = item.TextAlignment;
        _isTypeToolSettingsSyncing = false;

        OnPropertyChanged(nameof(TypeToolFontFamily));
        OnPropertyChanged(nameof(TypeToolFontSize));
        OnPropertyChanged(nameof(TypeToolColorHex));
        OnPropertyChanged(nameof(TypeToolColorPreview));
        OnPropertyChanged(nameof(TypeToolOpacity));
        OnPropertyChanged(nameof(TypeToolBold));
        OnPropertyChanged(nameof(TypeToolItalic));
        OnPropertyChanged(nameof(TypeToolTextAlignment));
        UpdateTypeToolAlignmentSelection();
    }

    private void UpdateTypeToolAlignmentSelection()
    {
        UpdateTypeToolAlignmentButton(TypeAlignLeftButton, TypeToolTextAlignment == TextAlignment.Left);
        UpdateTypeToolAlignmentButton(TypeAlignCenterButton, TypeToolTextAlignment == TextAlignment.Center);
        UpdateTypeToolAlignmentButton(TypeAlignRightButton, TypeToolTextAlignment == TextAlignment.Right);
    }

    private void UpdateTypeToolAlignmentButton(System.Windows.Controls.Button? button, bool isActive)
    {
        if (button is null)
        {
            return;
        }

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

    private static bool TryBuildTypeToolBrush(string? colorText, out System.Windows.Media.Brush? brush, out string? normalized)
    {
        brush = null;
        normalized = null;
        if (string.IsNullOrWhiteSpace(colorText))
        {
            return false;
        }

        try
        {
            object? converted = System.Windows.Media.ColorConverter.ConvertFromString(colorText.Trim());
            if (converted is not System.Windows.Media.Color color)
            {
                return false;
            }

            SolidColorBrush solidBrush = new(color);
            solidBrush.Freeze();
            brush = solidBrush;
            normalized = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ClearTypeTextTool()
    {
        TypeTextItems.Clear();
        _selectedTypeTextItem = null;
        _draggingTypeTextItem = null;
        _creatingTypeTextItem = null;
        _isTypeTextCreating = false;
        _isTypeTextDragging = false;
        _typeTextEditSnapshot = string.Empty;
        Mouse.Capture(null);
        UpdateTypeToolVisualState();
    }
}
