using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace KRetouchStudio;

public partial class MainWindow
{
    public Visibility HealingToolOptionsVisibility => string.Equals(ActiveToolId, "healing", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string HealingMode
    {
        get => _healingMode;
        private set
        {
            if (string.Equals(_healingMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _healingMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealingPatchOptionsVisibility));
            OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
            OnPropertyChanged(nameof(HealingModeHintText));
            ClearHealingPatchSelection();
            UpdateHealingCircleVisibility();
            UpdateHealingSourceMarkerVisibility();
            SaveToolboxDefaults();
        }
    }

    public string HealingModeHintText
    {
        get
        {
            if (_isHealingOperationRunning)
            {
                return "Healing: applying OpenCV ROI...";
            }

            return HealingMode switch
            {
                "patch" => HealingPatchMode == "destination"
                    ? "Patch Destination: draw source, drag selection to target"
                    : "Patch Source: draw target, drag selection to source",
                "spot" => "Spot Healing: click or drag over marks",
                _ => "Alt+Click source, drag target"
            };
        }
    }

    public Visibility HealingPatchOptionsVisibility =>
        string.Equals(ActiveToolId, "healing", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(HealingMode, "patch", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string HealingPatchMode
    {
        get => _healingPatchMode;
        private set
        {
            if (string.Equals(_healingPatchMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _healingPatchMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealingModeHintText));
            ClearHealingPatchSelection();
            UpdateHealingCircleVisibility();
            UpdateHealingSourceMarkerVisibility();
            UpdateHealingPatchModeSelection();
            SaveToolboxDefaults();
        }
    }

    public double HealingPatchSelectionLeft
    {
        get => _healingPatchSelectionLeft;
        private set
        {
            _healingPatchSelectionLeft = value;
            OnPropertyChanged();
        }
    }

    public double HealingPatchSelectionTop
    {
        get => _healingPatchSelectionTop;
        private set
        {
            _healingPatchSelectionTop = value;
            OnPropertyChanged();
        }
    }

    public double HealingPatchSelectionWidth
    {
        get => _healingPatchSelectionWidth;
        private set
        {
            _healingPatchSelectionWidth = value;
            OnPropertyChanged();
        }
    }

    public double HealingPatchSelectionHeight
    {
        get => _healingPatchSelectionHeight;
        private set
        {
            _healingPatchSelectionHeight = value;
            OnPropertyChanged();
        }
    }

    public Visibility HealingPatchSelectionVisibility =>
        CanUseHealingPatchPreview() && (_isHealingPatchCreating || _isHealingPatchDragging || _hasHealingPatchSelection)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Geometry? HealingPatchSelectionGeometry
    {
        get => _healingPatchSelectionGeometry;
        private set
        {
            _healingPatchSelectionGeometry = value;
            OnPropertyChanged();
        }
    }

    public Geometry? HealingSourceMarkerGeometry
    {
        get => _healingSourceMarkerGeometry;
        private set
        {
            _healingSourceMarkerGeometry = value;
            OnPropertyChanged();
        }
    }

    public Visibility HealingSourceMarkerVisibility
    {
        get => _healingSourceMarkerVisibility;
        private set
        {
            _healingSourceMarkerVisibility = value;
            OnPropertyChanged();
        }
    }

    public double HealingSize
    {
        get => _healingSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_healingSize - clamped) < 0.01)
            {
                return;
            }

            _healingSize = clamped;
            HealingCircleSize = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double HealingSoftness
    {
        get => _healingSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_healingSoftness - clamped) < 0.01)
            {
                return;
            }

            _healingSoftness = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealingHardness));
            SaveToolboxDefaults();
        }
    }

    public double HealingHardness
    {
        get => 100 - _healingSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            double softness = 100 - clamped;
            if (Math.Abs(_healingSoftness - softness) < 0.01)
            {
                return;
            }

            _healingSoftness = softness;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HealingSoftness));
            SaveToolboxDefaults();
        }
    }

    public double HealingStrength
    {
        get => _healingStrength;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_healingStrength - clamped) < 0.01)
            {
                return;
            }

            _healingStrength = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public bool ShowHealingCircle
    {
        get => _showHealingCircle;
        set
        {
            if (_showHealingCircle == value)
            {
                return;
            }

            _showHealingCircle = value;
            OnPropertyChanged();
            UpdateHealingCircleVisibility();
            SaveToolboxDefaults();
        }
    }

    public double HealingCircleLeft
    {
        get => _healingCircleLeft;
        private set
        {
            _healingCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double HealingCircleTop
    {
        get => _healingCircleTop;
        private set
        {
            _healingCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double HealingCircleSize
    {
        get => _healingCircleSize;
        private set
        {
            _healingCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility HealingCircleVisibility
    {
        get => _healingCircleVisibility;
        private set
        {
            _healingCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    public Geometry? HealingStrokePreviewGeometry
    {
        get => _healingStrokePreviewGeometry;
        private set
        {
            _healingStrokePreviewGeometry = value;
            OnPropertyChanged();
        }
    }

    public Visibility HealingStrokePreviewVisibility
    {
        get => _healingStrokePreviewVisibility;
        private set
        {
            _healingStrokePreviewVisibility = value;
            OnPropertyChanged();
        }
    }

    public double HealingStrokePreviewThickness
    {
        get => _healingStrokePreviewThickness;
        private set
        {
            _healingStrokePreviewThickness = value;
            OnPropertyChanged();
        }
    }

    private void HealingModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        HealingMode = mode;
        UpdateHealingModeSelection();
    }

    private void HealingResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryResetLatestHealingHistory())
        {
            MediaPipeStatusText = "Healing Reset: no current healing history";
        }
    }

    private bool TryResetLatestHealingHistory()
    {
        if (SelectedPhoto is not PhotoItem photo ||
            _editorUndoHistory.Count <= 1 ||
            !IsHealingToolHistoryTitle(_editorUndoHistory[^1].Title))
        {
            return false;
        }

        _editorUndoHistory.RemoveAt(_editorUndoHistory.Count - 1);
        _editorRedoHistory.Clear();
        RestoreEditorHistoryState(_editorUndoHistory[^1]);
        RefreshEditorHistoryPanel();
        StoreCurrentEditorHistorySession(photo, persistToDisk: false);
        ClearOpenCvHealingStroke();
        ClearHealingPatchSelection();
        MediaPipeStatusText = "Healing Reset: last brush step removed";
        return true;
    }

    private static bool IsHealingToolHistoryTitle(string title)
    {
        return string.Equals(title, "Healing", StringComparison.Ordinal) ||
               string.Equals(title, "Patch", StringComparison.Ordinal);
    }

    private void HealingPatchModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        HealingPatchMode = mode;
    }

    private void UpdateHealingModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetHealingModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, HealingMode, StringComparison.OrdinalIgnoreCase);
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

        UpdateHealingPatchModeSelection();
    }

    private void UpdateHealingPatchModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetHealingPatchModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, HealingPatchMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetHealingModeButtons()
    {
        yield return HealingBrushModeButton;
        yield return PatchHealingModeButton;
        yield return SpotHealingModeButton;
    }

    private IEnumerable<System.Windows.Controls.Button> GetHealingPatchModeButtons()
    {
        yield return HealingPatchSourceModeButton;
        yield return HealingPatchDestinationModeButton;
    }

    private bool CanUseHealingPreview()
    {
        return string.Equals(ActiveToolId, "healing", StringComparison.OrdinalIgnoreCase) &&
               !_isHealingOperationRunning &&
               CanUseSinglePreviewTool();
    }

    private bool CanUseHealingPatchPreview()
    {
        return CanUseHealingPreview() &&
               string.Equals(HealingMode, "patch", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryBeginHealingOperation(string statusText)
    {
        if (_isHealingOperationRunning)
        {
            return false;
        }

        _isHealingOperationRunning = true;
        _healingOperationPreviousHitTestVisible = PreviewSurface.IsHitTestVisible;
        PreviewSurface.IsHitTestVisible = false;
        MediaPipeStatusText = statusText;
        OnPropertyChanged(nameof(HealingModeHintText));
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
        UpdateHealingCircleVisibility();
        UpdateHealingSourceMarkerVisibility();
        return true;
    }

    private void EndHealingOperation(string statusText)
    {
        _isHealingOperationRunning = false;
        PreviewSurface.IsHitTestVisible = _healingOperationPreviousHitTestVisible;
        MediaPipeStatusText = statusText;
        OnPropertyChanged(nameof(HealingModeHintText));
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
        UpdateHealingCircleVisibility();
        UpdateHealingSourceMarkerVisibility();
    }

    private static string BuildHealingStatus(string statusText, string? error)
    {
        return string.IsNullOrWhiteSpace(error)
            ? statusText
            : statusText + " | " + error;
    }

    private void StartHealingStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (_isHealingOperationRunning)
        {
            return;
        }

        if (CanUseHealingPatchPreview())
        {
            StartHealingPatchInteraction(previewPoint);
            return;
        }

        if (!CanUseHealingPreview() ||
            SelectedPhoto is not PhotoItem photo ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        bool isSpot = string.Equals(HealingMode, "spot", StringComparison.OrdinalIgnoreCase);
        bool setSource = !isSpot && ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt || !_hasHealingSource);
        if (setSource)
        {
            _hasHealingSource = true;
            _healingSourceImagePoint = imagePoint;
            _healingSourceBitmap = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
            UpdateHealingSourceMarker(_healingSourceImagePoint);
            MediaPipeStatusText = $"Healing source: {imagePoint.X:0}, {imagePoint.Y:0}";
            return;
        }

        _isHealingDragging = true;
        _healingStrokeStartSourcePoint = _healingSourceImagePoint;
        _healingStrokeStartTargetPoint = imagePoint;
        _healingLastImagePoint = imagePoint;
        _isOpenCvSpotHealingStroke = isSpot;
        _isOpenCvHealingStroke = !isSpot && _healingSourceBitmap is not null;

        if (_isOpenCvSpotHealingStroke || _isOpenCvHealingStroke)
        {
            _healingStrokeTargetBitmap = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
            BeginOpenCvHealingMask(_healingStrokeTargetBitmap.PixelWidth, _healingStrokeTargetBitmap.PixelHeight);
            AddOpenCvHealingMaskDab(imagePoint, pressure);
            UpdateHealingSourceMarker(GetCurrentHealingSourceImagePoint(imagePoint));
            System.Windows.Input.Mouse.Capture(PreviewSurface);
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            _isHealingDragging = false;
            return;
        }

        ApplyHealingDab(target, imagePoint, pressure);
        UpdateHealingSourceMarker(GetCurrentHealingSourceImagePoint(imagePoint));
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueHealingStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (CanUseHealingPatchPreview())
        {
            ContinueHealingPatchInteraction(previewPoint);
            return;
        }

        if (!_isHealingDragging ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        if (_isOpenCvSpotHealingStroke || _isOpenCvHealingStroke)
        {
            AddOpenCvHealingMaskSegment(_healingLastImagePoint, imagePoint, pressure);
            _healingLastImagePoint = imagePoint;
            UpdateHealingSourceMarker(GetCurrentHealingSourceImagePoint(imagePoint));
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyHealingStrokeSegment(target, _healingLastImagePoint, imagePoint, pressure);
        _healingLastImagePoint = imagePoint;
        UpdateHealingSourceMarker(GetCurrentHealingSourceImagePoint(imagePoint));
    }

    private async void StopHealingStroke()
    {
        if (CanUseHealingPatchPreview())
        {
            StopHealingPatchInteraction();
            return;
        }

        if (!_isHealingDragging)
        {
            return;
        }

        _isHealingDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        if (_isOpenCvSpotHealingStroke || _isOpenCvHealingStroke)
        {
            bool isSpotStroke = _isOpenCvSpotHealingStroke;
            PhotoItem? operationPhoto = SelectedPhoto;
            if (!TryBeginHealingOperation(isSpotStroke ? "Spot Healing: applying inpaint..." : "Healing: applying OpenCV ROI..."))
            {
                ClearOpenCvHealingStroke();
                return;
            }

            bool applied = false;
            bool fallbackApplied = false;
            string? operationError = null;
            try
            {
                (applied, operationError) = isSpotStroke
                    ? await TryApplyOpenCvSpotHealingStrokeAsync(operationPhoto)
                    : await TryApplyOpenCvHealingStrokeAsync(operationPhoto);
                if (!applied && ReferenceEquals(SelectedPhoto, operationPhoto))
                {
                    fallbackApplied = isSpotStroke
                        ? ApplyOpenCvSpotHealingFallback(operationPhoto)
                        : ApplyOpenCvHealingFallback(operationPhoto);
                }
            }
            finally
            {
                string statusText = applied
                    ? isSpotStroke ? "Spot Healing: inpaint applied" : "Healing: OpenCV ROI applied"
                    : fallbackApplied
                        ? BuildHealingStatus(isSpotStroke ? "Spot Healing: fallback applied" : "Healing: fallback applied", operationError)
                        : BuildHealingStatus(isSpotStroke ? "Spot Healing: skipped" : "Healing: skipped", operationError);
                EndHealingOperation(statusText);
                ClearOpenCvHealingStroke();
            }

            if (applied)
            {
                PushEditorHistorySnapshot("Healing", isSpotStroke
                    ? $"spot {HealingSize:0}px / OpenCV inpaint"
                    : $"{HealingMode} {HealingSize:0}px / OpenCV ROI");
            }
            else if (fallbackApplied)
            {
                PushEditorHistorySnapshot("Healing", $"{HealingMode} {HealingSize:0}px / opacity {HealingStrength:0}%");
            }

            return;
        }
        else
        {
            EndSourceCopyStroke();
        }

        PushEditorHistorySnapshot("Healing", $"{HealingMode} {HealingSize:0}px / opacity {HealingStrength:0}%");
    }

    private void ApplyHealingDab(System.Windows.Media.Imaging.WriteableBitmap target, System.Windows.Point imagePoint, double pressure)
    {
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        double opacity = ApplyToolPressureToOpacity(Math.Clamp(HealingStrength / 100.0, 0.0, 1.0), pressure);
        if (string.Equals(HealingMode, "spot", StringComparison.OrdinalIgnoreCase) || _healingSourceBitmap is null)
        {
            ApplyBlurSharpDab(target, imagePoint, size, HealingSoftness, size * 0.18, opacity * 100.0, false);
            return;
        }

        System.Windows.Point sourcePoint = new(
            _healingStrokeStartSourcePoint.X + (imagePoint.X - _healingStrokeStartTargetPoint.X),
            _healingStrokeStartSourcePoint.Y + (imagePoint.Y - _healingStrokeStartTargetPoint.Y));
        ApplySourceCopyDab(target, _healingSourceBitmap, imagePoint, sourcePoint, size, HealingSoftness, opacity, matchTargetTone: true);
    }

    private void UpdateHealingCircle(System.Windows.Point previewPoint, double pressure)
    {
        if (CanUseHealingPatchPreview())
        {
            UpdateHealingPatchHoverCursor(previewPoint);
            UpdateHealingCircleVisibility();
            return;
        }

        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        HealingCircleSize = size;
        HealingCircleLeft = center.X - (size * 0.5);
        HealingCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        if (_isHealingDragging && TryPreviewPointToImagePoint(center, out System.Windows.Point imagePoint))
        {
            UpdateHealingSourceMarker(GetCurrentHealingSourceImagePoint(imagePoint));
        }
        else
        {
            UpdateHealingSourceMarkerVisibility();
        }

        UpdateHealingCircleVisibility();
    }

    private void UpdateHealingCircleVisibility()
    {
        HealingCircleVisibility = CanUseHealingPreview() && !CanUseHealingPatchPreview() && ShowHealingCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateHealingSourceMarkerVisibility()
    {
        if (!CanShowHealingSourceMarker())
        {
            HealingSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        UpdateHealingSourceMarker(_healingSourceImagePoint);
    }

    private bool CanShowHealingSourceMarker()
    {
        return CanUseHealingPreview() &&
               _hasHealingSource &&
               !string.Equals(HealingMode, "spot", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(HealingMode, "patch", StringComparison.OrdinalIgnoreCase) &&
               !_isHealingOperationRunning;
    }

    private System.Windows.Point GetCurrentHealingSourceImagePoint(System.Windows.Point targetImagePoint)
    {
        if (!_isHealingDragging)
        {
            return _healingSourceImagePoint;
        }

        return new System.Windows.Point(
            _healingStrokeStartSourcePoint.X + (targetImagePoint.X - _healingStrokeStartTargetPoint.X),
            _healingStrokeStartSourcePoint.Y + (targetImagePoint.Y - _healingStrokeStartTargetPoint.Y));
    }

    private void UpdateHealingSourceMarker(System.Windows.Point sourceImagePoint)
    {
        if (!CanShowHealingSourceMarker() || SelectedPhoto is not PhotoItem photo)
        {
            HealingSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        sourceImagePoint = new System.Windows.Point(
            Math.Clamp(sourceImagePoint.X, 0, Math.Max(0, source.PixelWidth - 1)),
            Math.Clamp(sourceImagePoint.Y, 0, Math.Max(0, source.PixelHeight - 1)));
        if (!TryGetCurrentPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out double scale))
        {
            HealingSourceMarkerVisibility = Visibility.Collapsed;
            return;
        }

        System.Windows.Point previewPoint = ToPreviewPoint(sourceImagePoint, offsetX, offsetY, scale);
        const double radius = 7.0;
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(new System.Windows.Point(previewPoint.X - radius, previewPoint.Y), false, false);
            context.LineTo(new System.Windows.Point(previewPoint.X + radius, previewPoint.Y), true, false);
            context.BeginFigure(new System.Windows.Point(previewPoint.X, previewPoint.Y - radius), false, false);
            context.LineTo(new System.Windows.Point(previewPoint.X, previewPoint.Y + radius), true, false);
        }

        geometry.Freeze();
        HealingSourceMarkerGeometry = geometry;
        HealingSourceMarkerVisibility = Visibility.Visible;
    }

    private void ApplyHealingStrokeSegment(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point fromImagePoint,
        System.Windows.Point toImagePoint,
        double pressure)
    {
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        ForEachToolStrokePoint(fromImagePoint, toImagePoint, size, point =>
        {
            ApplyHealingDab(target, point, pressure);
        });
    }

    private void BeginOpenCvHealingMask(int width, int height)
    {
        _healingStrokeMaskWidth = width;
        _healingStrokeMaskHeight = height;
        _healingStrokeMaskPixels = new byte[width * height];
        _healingStrokePoints.Clear();
        ClearHealingStrokePreview();
    }

    private void AddOpenCvHealingMaskSegment(System.Windows.Point fromImagePoint, System.Windows.Point toImagePoint, double pressure)
    {
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        ForEachToolStrokePoint(fromImagePoint, toImagePoint, size, point =>
        {
            AddOpenCvHealingMaskDab(point, pressure);
        });
    }

    private void AddOpenCvHealingMaskDab(System.Windows.Point imagePoint, double pressure)
    {
        if (_healingStrokeMaskPixels is null ||
            _healingStrokeMaskWidth <= 0 ||
            _healingStrokeMaskHeight <= 0)
        {
            return;
        }

        _healingStrokePoints.Add(imagePoint);
        double size = ApplyToolPressureToSize(HealingSize, pressure);
        bool isSpotStroke = _isOpenCvSpotHealingStroke;
        double softness = isSpotStroke ? HealingSoftness : 0;
        ForEachDabPixel(_healingStrokeMaskWidth, _healingStrokeMaskHeight, imagePoint, size, softness, hardEdge: !isSpotStroke, opacity: 1.0, (x, y, alpha) =>
        {
            int index = y * _healingStrokeMaskWidth + x;
            _healingStrokeMaskPixels[index] = Math.Max(_healingStrokeMaskPixels[index], (byte)Math.Clamp((int)Math.Round(alpha * 255.0), 0, 255));
        });
        RebuildHealingStrokePreview();
    }

    private void RebuildHealingStrokePreview()
    {
        if (_healingStrokePoints.Count == 0 ||
            !_isHealingDragging ||
            (!_isOpenCvSpotHealingStroke && !_isOpenCvHealingStroke) ||
            SelectedPhoto is not PhotoItem photo)
        {
            ClearHealingStrokePreview();
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(photo);
        if (!TryGetCurrentPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out double scale))
        {
            ClearHealingStrokePreview();
            return;
        }

        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            System.Windows.Point firstPoint = ToPreviewPoint(_healingStrokePoints[0], offsetX, offsetY, scale);
            if (_healingStrokePoints.Count == 1)
            {
                context.BeginFigure(new System.Windows.Point(firstPoint.X - 0.25, firstPoint.Y), false, false);
                context.LineTo(new System.Windows.Point(firstPoint.X + 0.25, firstPoint.Y), true, false);
            }
            else
            {
                context.BeginFigure(firstPoint, false, false);
                for (int i = 1; i < _healingStrokePoints.Count; i++)
                {
                    context.LineTo(ToPreviewPoint(_healingStrokePoints[i], offsetX, offsetY, scale), true, false);
                }
            }
        }

        geometry.Freeze();
        HealingStrokePreviewThickness = Math.Max(2.0, HealingSize * scale);
        HealingStrokePreviewGeometry = geometry;
        HealingStrokePreviewVisibility = Visibility.Visible;
    }

    private void ClearHealingStrokePreview()
    {
        HealingStrokePreviewGeometry = null;
        HealingStrokePreviewVisibility = Visibility.Collapsed;
        HealingStrokePreviewThickness = 1;
    }

    private async Task<(bool Applied, string? Error)> TryApplyOpenCvHealingStrokeAsync(PhotoItem? operationPhoto)
    {
        if (operationPhoto is null ||
            !ReferenceEquals(SelectedPhoto, operationPhoto) ||
            _healingStrokeTargetBitmap is null ||
            _healingSourceBitmap is null ||
            _healingStrokeMaskPixels is null)
        {
            return (false, "Healing target is not ready.");
        }

        System.Windows.Media.Imaging.BitmapSource targetBitmap = _healingStrokeTargetBitmap;
        System.Windows.Media.Imaging.BitmapSource sourceBitmap = _healingSourceBitmap;
        byte[] maskPixels = (byte[])_healingStrokeMaskPixels.Clone();
        int maskWidth = _healingStrokeMaskWidth;
        int maskHeight = _healingStrokeMaskHeight;
        System.Windows.Vector sourceOffset = _healingStrokeStartTargetPoint - _healingStrokeStartSourcePoint;
        double hardness = HealingHardness;
        double strength = HealingStrength;

        (bool applied, System.Windows.Media.Imaging.BitmapSource? result, string? error) = await RunOpenCvHealingAsync(
            targetBitmap,
            sourceBitmap,
            maskPixels,
            maskWidth,
            maskHeight,
            sourceOffset,
            hardness,
            strength);

        if (!applied || result is null)
        {
            return (false, error);
        }

        if (!ReferenceEquals(SelectedPhoto, operationPhoto))
        {
            return (false, "Healing target changed during operation.");
        }

        operationPhoto.SetAdjustedImage(result);
        UpdatePreviewLayoutPreservingSinglePreviewPan();
        return (true, null);
    }

    private async Task<(bool Applied, string? Error)> TryApplyOpenCvSpotHealingStrokeAsync(PhotoItem? operationPhoto)
    {
        if (operationPhoto is null ||
            !ReferenceEquals(SelectedPhoto, operationPhoto) ||
            _healingStrokeTargetBitmap is null ||
            _healingStrokeMaskPixels is null)
        {
            return (false, "Spot healing target is not ready.");
        }

        System.Windows.Media.Imaging.BitmapSource targetBitmap = _healingStrokeTargetBitmap;
        byte[] maskPixels = (byte[])_healingStrokeMaskPixels.Clone();
        int maskWidth = _healingStrokeMaskWidth;
        int maskHeight = _healingStrokeMaskHeight;
        double hardness = HealingHardness;
        double strength = HealingStrength;

        (bool applied, System.Windows.Media.Imaging.BitmapSource? result, string? error) = await RunOpenCvSpotHealingAsync(
            targetBitmap,
            maskPixels,
            maskWidth,
            maskHeight,
            hardness,
            strength);

        if (!applied || result is null)
        {
            return (false, error);
        }

        if (!ReferenceEquals(SelectedPhoto, operationPhoto))
        {
            return (false, "Spot healing target changed during operation.");
        }

        operationPhoto.SetAdjustedImage(result);
        UpdatePreviewLayoutPreservingSinglePreviewPan();
        return (true, null);
    }

    private bool ApplyOpenCvHealingFallback(PhotoItem? operationPhoto)
    {
        if (operationPhoto is null ||
            !ReferenceEquals(SelectedPhoto, operationPhoto) ||
            _healingStrokeTargetBitmap is null ||
            _healingSourceBitmap is null ||
            _healingStrokePoints.Count == 0)
        {
            return false;
        }

        operationPhoto.SetAdjustedImage(new System.Windows.Media.Imaging.WriteableBitmap(_healingStrokeTargetBitmap));
        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return false;
        }

        BeginSourceCopyStroke(target);
        foreach (System.Windows.Point point in _healingStrokePoints)
        {
            ApplyHealingDab(target, point, pressure: 1.0);
        }

        EndSourceCopyStroke();
        UpdatePreviewLayoutPreservingSinglePreviewPan();
        return true;
    }

    private bool ApplyOpenCvSpotHealingFallback(PhotoItem? operationPhoto)
    {
        if (operationPhoto is null ||
            !ReferenceEquals(SelectedPhoto, operationPhoto) ||
            _healingStrokeTargetBitmap is null ||
            _healingStrokePoints.Count == 0)
        {
            return false;
        }

        operationPhoto.SetAdjustedImage(new System.Windows.Media.Imaging.WriteableBitmap(_healingStrokeTargetBitmap));
        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return false;
        }

        double size = HealingSize;
        double opacity = Math.Clamp(HealingStrength / 100.0, 0.0, 1.0) * 100.0;
        foreach (System.Windows.Point point in _healingStrokePoints)
        {
            ApplyBlurSharpDab(target, point, size, HealingSoftness, size * 0.18, opacity, false);
        }

        UpdatePreviewLayoutPreservingSinglePreviewPan();
        return true;
    }

    private void ClearOpenCvHealingStroke()
    {
        _isOpenCvHealingStroke = false;
        _isOpenCvSpotHealingStroke = false;
        _healingStrokeTargetBitmap = null;
        _healingStrokeMaskPixels = null;
        _healingStrokeMaskWidth = 0;
        _healingStrokeMaskHeight = 0;
        _healingStrokePoints.Clear();
        ClearHealingStrokePreview();
    }

    private void StartHealingPatchInteraction(System.Windows.Point previewPoint)
    {
        if (!CanUseHealingPatchPreview() ||
            SelectedPhoto is null)
        {
            return;
        }

        System.Windows.Point clampedPoint = ClampPointToPreviewImage(previewPoint);
        if (_hasHealingPatchSelection && IsPreviewPointInsideHealingPatchSelection(clampedPoint))
        {
            if (!TryPreviewPointToImagePoint(clampedPoint, out _healingPatchDragStartImagePoint))
            {
                return;
            }

            _isHealingPatchDragging = true;
            _healingPatchDragStartPreviewPoint = clampedPoint;
            _healingPatchDragStartImageRect = _healingPatchSelectionImageRect;
            _healingPatchDragStartImagePoints.Clear();
            _healingPatchDragStartImagePoints.AddRange(_healingPatchSelectionImagePoints);
            PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
            System.Windows.Input.Mouse.Capture(PreviewSurface);
            return;
        }

        _isHealingPatchCreating = true;
        _hasHealingPatchSelection = false;
        _healingPatchStartPreviewPoint = clampedPoint;
        _healingPatchSelectionImagePoints.Clear();
        _healingPatchOriginalImagePoints.Clear();
        AddHealingPatchSelectionPoint(clampedPoint, force: true);
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueHealingPatchInteraction(System.Windows.Point previewPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(previewPoint);
        if (_isHealingPatchCreating)
        {
            AddHealingPatchSelectionPoint(clampedPoint, force: false);
            return;
        }

        if (_isHealingPatchDragging)
        {
            MoveHealingPatchSelection(clampedPoint);
        }
    }

    private void StopHealingPatchInteraction()
    {
        if (_isHealingPatchCreating)
        {
            _isHealingPatchCreating = false;
            System.Windows.Input.Mouse.Capture(null);
            _healingPatchSelectionImageRect = GetHealingPatchPointBounds(_healingPatchSelectionImagePoints);
            _hasHealingPatchSelection = _healingPatchSelectionImagePoints.Count >= 3 &&
                                        _healingPatchSelectionImageRect.Width >= 4 &&
                                        _healingPatchSelectionImageRect.Height >= 4;
            if (_hasHealingPatchSelection)
            {
                _healingPatchOriginalImagePoints.Clear();
                _healingPatchOriginalImagePoints.AddRange(_healingPatchSelectionImagePoints);
                _healingPatchOriginalImageRect = _healingPatchSelectionImageRect;
                MediaPipeStatusText = "Patch: selection ready, drag inside it";
            }
            else
            {
                ClearHealingPatchSelection();
                return;
            }

            RebuildHealingPatchSelectionGeometry();
            OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
            return;
        }

        if (_isHealingPatchDragging)
        {
            _isHealingPatchDragging = false;
            System.Windows.Input.Mouse.Capture(null);
            ApplyHealingPatchSelection();
            return;
        }
    }

    private void AddHealingPatchSelectionPoint(System.Windows.Point previewPoint, bool force)
    {
        if (SelectedPhoto is null ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        imagePoint = ClampHealingPatchImagePoint(imagePoint, source.PixelWidth, source.PixelHeight);
        if (!force && _healingPatchSelectionImagePoints.Count > 0)
        {
            System.Windows.Point last = _healingPatchSelectionImagePoints[^1];
            double dx = imagePoint.X - last.X;
            double dy = imagePoint.Y - last.Y;
            if ((dx * dx) + (dy * dy) < 16.0)
            {
                return;
            }
        }

        _healingPatchSelectionImagePoints.Add(imagePoint);
        _healingPatchSelectionImageRect = GetHealingPatchPointBounds(_healingPatchSelectionImagePoints);
        RebuildHealingPatchSelectionGeometry();
    }

    private void MoveHealingPatchSelection(System.Windows.Point currentPoint)
    {
        if (SelectedPhoto is null ||
            _healingPatchDragStartImagePoints.Count == 0 ||
            !TryPreviewPointToImagePoint(currentPoint, out System.Windows.Point currentImagePoint))
        {
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        Vector delta = currentImagePoint - _healingPatchDragStartImagePoint;
        delta.X = Math.Clamp(delta.X, -_healingPatchDragStartImageRect.Left, source.PixelWidth - _healingPatchDragStartImageRect.Right);
        delta.Y = Math.Clamp(delta.Y, -_healingPatchDragStartImageRect.Top, source.PixelHeight - _healingPatchDragStartImageRect.Bottom);

        _healingPatchSelectionImagePoints.Clear();
        foreach (System.Windows.Point point in _healingPatchDragStartImagePoints)
        {
            _healingPatchSelectionImagePoints.Add(new System.Windows.Point(point.X + delta.X, point.Y + delta.Y));
        }

        _healingPatchSelectionImageRect = new Rect(
            _healingPatchDragStartImageRect.Left + delta.X,
            _healingPatchDragStartImageRect.Top + delta.Y,
            _healingPatchDragStartImageRect.Width,
            _healingPatchDragStartImageRect.Height);
        RebuildHealingPatchSelectionGeometry();
    }

    private async void ApplyHealingPatchSelection()
    {
        if (_isHealingOperationRunning ||
            !_hasHealingPatchSelection ||
            SelectedPhoto is not PhotoItem photo ||
            _healingPatchSelectionImagePoints.Count < 3 ||
            _healingPatchOriginalImagePoints.Count < 3)
        {
            return;
        }

        IReadOnlyList<System.Windows.Point> sourcePoints;
        IReadOnlyList<System.Windows.Point> targetPoints;
        if (string.Equals(HealingPatchMode, "destination", StringComparison.OrdinalIgnoreCase))
        {
            sourcePoints = _healingPatchOriginalImagePoints;
            targetPoints = _healingPatchSelectionImagePoints;
        }
        else
        {
            sourcePoints = _healingPatchSelectionImagePoints;
            targetPoints = _healingPatchOriginalImagePoints;
        }

        System.Windows.Media.Imaging.BitmapSource currentSource = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
        if (!TryBuildHealingPatchMask(currentSource.PixelWidth, currentSource.PixelHeight, targetPoints, out byte[] maskPixels, out Rect clippedTargetRect))
        {
            return;
        }

        Rect sourceRect = ClipHealingPatchRect(GetHealingPatchPointBounds(sourcePoints), currentSource.PixelWidth, currentSource.PixelHeight);
        if (sourceRect.Width < 2 || sourceRect.Height < 2)
        {
            return;
        }

        Vector sourceOffset = targetPoints[0] - sourcePoints[0];
        if (!TryBeginHealingOperation("Patch: applying OpenCV ROI..."))
        {
            return;
        }

        bool applied = false;
        string? operationError = null;
        try
        {
            double hardness = HealingHardness;
            double strength = HealingStrength;
            (bool patchApplied, System.Windows.Media.Imaging.BitmapSource? result, string? patchError) = await RunOpenCvHealingAsync(
                currentSource,
                currentSource,
                maskPixels,
                currentSource.PixelWidth,
                currentSource.PixelHeight,
                sourceOffset,
                hardness,
                strength);
            applied = patchApplied;
            operationError = patchError;

            if (!applied || result is null)
            {
                return;
            }

            if (!ReferenceEquals(SelectedPhoto, photo))
            {
                applied = false;
                operationError = "Patch target changed during operation.";
                return;
            }

            photo.SetAdjustedImage(result);
            UpdatePreviewLayoutPreservingSinglePreviewPan();
            PushEditorHistorySnapshot("Patch", $"{HealingPatchMode} {clippedTargetRect.Width:0}x{clippedTargetRect.Height:0} / OpenCV ROI");
            ClearHealingPatchSelection();
        }
        finally
        {
            EndHealingOperation(applied
                ? "Patch: OpenCV ROI applied"
                : BuildHealingStatus("Patch: OpenCV failed", operationError));
        }
    }

    private static Task<(bool Applied, System.Windows.Media.Imaging.BitmapSource? Result, string? Error)> RunOpenCvHealingAsync(
        System.Windows.Media.Imaging.BitmapSource targetSource,
        System.Windows.Media.Imaging.BitmapSource sourceSnapshot,
        byte[] maskPixels,
        int maskWidth,
        int maskHeight,
        Vector sourceOffset,
        double hardness,
        double opacity)
    {
        return Task.Run(() =>
        {
            try
            {
                bool applied = OpenCvHealingBrushEngine.TryApplyHealing(
                    targetSource,
                    sourceSnapshot,
                    maskPixels,
                    maskWidth,
                    maskHeight,
                    sourceOffset,
                    hardness,
                    opacity,
                    "NORMAL",
                    out System.Windows.Media.Imaging.BitmapSource? result,
                    out string? error);
                return (applied, result, error);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        });
    }

    private static Task<(bool Applied, System.Windows.Media.Imaging.BitmapSource? Result, string? Error)> RunOpenCvSpotHealingAsync(
        System.Windows.Media.Imaging.BitmapSource targetSource,
        byte[] maskPixels,
        int maskWidth,
        int maskHeight,
        double hardness,
        double opacity)
    {
        return Task.Run(() =>
        {
            try
            {
                bool applied = OpenCvHealingBrushEngine.TryApplySpotHealing(
                    targetSource,
                    maskPixels,
                    maskWidth,
                    maskHeight,
                    hardness,
                    opacity,
                    out System.Windows.Media.Imaging.BitmapSource? result,
                    out string? error);
                return (applied, result, error);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        });
    }

    private bool TryBuildHealingPatchMask(int width, int height, IReadOnlyList<System.Windows.Point> targetPoints, out byte[] maskPixels, out Rect clippedRect)
    {
        maskPixels = new byte[width * height];
        clippedRect = ClipHealingPatchRect(GetHealingPatchPointBounds(targetPoints), width, height);
        int left = Math.Clamp((int)Math.Floor(clippedRect.Left), 0, width - 1);
        int top = Math.Clamp((int)Math.Floor(clippedRect.Top), 0, height - 1);
        int right = Math.Clamp((int)Math.Ceiling(clippedRect.Right), left + 1, width);
        int bottom = Math.Clamp((int)Math.Ceiling(clippedRect.Bottom), top + 1, height);
        if (right - left < 2 || bottom - top < 2)
        {
            return false;
        }

        int painted = 0;
        for (int y = top; y < bottom; y++)
        {
            int row = y * width;
            for (int x = left; x < right; x++)
            {
                if (IsPointInsidePolygon(new System.Windows.Point(x + 0.5, y + 0.5), targetPoints))
                {
                    maskPixels[row + x] = 255;
                    painted++;
                }
            }
        }

        clippedRect = new Rect(left, top, right - left, bottom - top);
        return painted > 0;
    }

    private bool TryGetHealingPatchImageRectFromOverlay(out Rect imageRect)
    {
        imageRect = _healingPatchSelectionImageRect;
        return imageRect.Width >= 2 && imageRect.Height >= 2;
    }

    private void SyncHealingPatchSelectionImageRectFromOverlay()
    {
        _healingPatchSelectionImageRect = GetHealingPatchPointBounds(_healingPatchSelectionImagePoints);
    }

    private static System.Windows.Point ClampHealingPatchImagePoint(System.Windows.Point point, int width, int height)
    {
        return new System.Windows.Point(
            Math.Clamp(point.X, 0, Math.Max(0, width - 1)),
            Math.Clamp(point.Y, 0, Math.Max(0, height - 1)));
    }

    private static Rect GetHealingPatchPointBounds(IReadOnlyList<System.Windows.Point> points)
    {
        if (points.Count == 0)
        {
            return Rect.Empty;
        }

        double left = points[0].X;
        double top = points[0].Y;
        double right = points[0].X;
        double bottom = points[0].Y;
        for (int i = 1; i < points.Count; i++)
        {
            System.Windows.Point point = points[i];
            left = Math.Min(left, point.X);
            top = Math.Min(top, point.Y);
            right = Math.Max(right, point.X);
            bottom = Math.Max(bottom, point.Y);
        }

        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    private static bool IsPointInsidePolygon(System.Windows.Point point, IReadOnlyList<System.Windows.Point> polygon)
    {
        if (polygon.Count < 3)
        {
            return false;
        }

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            System.Windows.Point pi = polygon[i];
            System.Windows.Point pj = polygon[j];
            bool crossesY = (pi.Y > point.Y) != (pj.Y > point.Y);
            if (!crossesY)
            {
                continue;
            }

            double xAtY = ((pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y)) + pi.X;
            if (point.X < xAtY)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static Rect ClipHealingPatchRect(Rect rect, int width, int height)
    {
        double left = Math.Clamp(rect.Left, 0, width);
        double top = Math.Clamp(rect.Top, 0, height);
        double right = Math.Clamp(rect.Right, left, width);
        double bottom = Math.Clamp(rect.Bottom, top, height);
        return new Rect(left, top, right - left, bottom - top);
    }

    private void RebuildHealingPatchSelectionGeometry()
    {
        if (_healingPatchSelectionImagePoints.Count < 2 || SelectedPhoto is null)
        {
            HealingPatchSelectionGeometry = null;
            OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
            return;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        if (!TryGetCurrentPreviewImageTransform(source.PixelWidth, source.PixelHeight, out double offsetX, out double offsetY, out double scale))
        {
            return;
        }

        bool closed = _healingPatchSelectionImagePoints.Count >= 3 && !_isHealingPatchCreating;
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open())
        {
            System.Windows.Point firstPoint = ToPreviewPoint(_healingPatchSelectionImagePoints[0], offsetX, offsetY, scale);
            context.BeginFigure(firstPoint, closed, closed);
            for (int i = 1; i < _healingPatchSelectionImagePoints.Count; i++)
            {
                context.LineTo(ToPreviewPoint(_healingPatchSelectionImagePoints[i], offsetX, offsetY, scale), true, false);
            }
        }

        geometry.Freeze();
        HealingPatchSelectionGeometry = geometry;
        Rect imageBounds = GetHealingPatchPointBounds(_healingPatchSelectionImagePoints);
        HealingPatchSelectionLeft = offsetX + (imageBounds.Left * scale);
        HealingPatchSelectionTop = offsetY + (imageBounds.Top * scale);
        HealingPatchSelectionWidth = imageBounds.Width * scale;
        HealingPatchSelectionHeight = imageBounds.Height * scale;
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
    }

    private bool IsPreviewPointInsideHealingPatchSelection(System.Windows.Point point)
    {
        if (_healingPatchSelectionImagePoints.Count < 3 ||
            !TryPreviewPointToImagePoint(point, out System.Windows.Point imagePoint))
        {
            return false;
        }

        return IsPointInsidePolygon(imagePoint, _healingPatchSelectionImagePoints);
    }

    private void UpdateHealingPatchHoverCursor(System.Windows.Point previewPoint)
    {
        if (_hasHealingPatchSelection && IsPreviewPointInsideHealingPatchSelection(previewPoint))
        {
            PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
            return;
        }

        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
    }

    private void ClearHealingPatchSelection()
    {
        _isHealingPatchCreating = false;
        _isHealingPatchDragging = false;
        _hasHealingPatchSelection = false;
        _healingPatchSelectionImageRect = Rect.Empty;
        _healingPatchOriginalImageRect = Rect.Empty;
        _healingPatchDragStartImageRect = Rect.Empty;
        _healingPatchSelectionImagePoints.Clear();
        _healingPatchOriginalImagePoints.Clear();
        _healingPatchDragStartImagePoints.Clear();
        HealingPatchSelectionGeometry = null;
        HealingPatchSelectionLeft = 0;
        HealingPatchSelectionTop = 0;
        HealingPatchSelectionWidth = 0;
        HealingPatchSelectionHeight = 0;
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
    }
}
