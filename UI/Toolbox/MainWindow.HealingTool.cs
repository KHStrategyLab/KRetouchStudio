using System;
using System.Collections.Generic;
using System.Windows;

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
            SaveToolboxDefaults();
        }
    }

    public string HealingModeHintText => HealingMode switch
    {
        "patch" => HealingPatchMode == "destination"
            ? "Patch Destination: select source, drag to target"
            : "Patch Source: select target, drag to source",
        "spot" => "Auto spot healing",
        _ => "Alt+Click source, drag target"
    };

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
        HealingMode = "healing";
        HealingSize = 80;
        HealingHardness = 50;
        HealingStrength = 100;
        ShowHealingCircle = true;
        UpdateHealingModeSelection();
        SaveToolboxDefaults();
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
               CanUseSinglePreviewTool();
    }

    private bool CanUseHealingPatchPreview()
    {
        return CanUseHealingPreview() &&
               string.Equals(HealingMode, "patch", StringComparison.OrdinalIgnoreCase);
    }

    private void StartHealingStroke(System.Windows.Point previewPoint, double pressure)
    {
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
            return;
        }

        _isHealingDragging = true;
        _healingStrokeStartSourcePoint = _healingSourceImagePoint;
        _healingStrokeStartTargetPoint = imagePoint;
        _healingLastImagePoint = imagePoint;
        _isOpenCvHealingStroke = !isSpot && _healingSourceBitmap is not null;

        if (_isOpenCvHealingStroke)
        {
            _healingStrokeTargetBitmap = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
            BeginOpenCvHealingMask(_healingStrokeTargetBitmap.PixelWidth, _healingStrokeTargetBitmap.PixelHeight);
            AddOpenCvHealingMaskDab(imagePoint, pressure);
            System.Windows.Input.Mouse.Capture(PreviewSurface);
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            _isHealingDragging = false;
            return;
        }

        ApplyHealingDab(target, imagePoint, pressure);
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

        if (_isOpenCvHealingStroke)
        {
            AddOpenCvHealingMaskSegment(_healingLastImagePoint, imagePoint, pressure);
            _healingLastImagePoint = imagePoint;
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyHealingStrokeSegment(target, _healingLastImagePoint, imagePoint, pressure);
        _healingLastImagePoint = imagePoint;
    }

    private void StopHealingStroke()
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
        if (_isOpenCvHealingStroke)
        {
            bool applied = TryApplyOpenCvHealingStroke();
            if (applied)
            {
                ClearOpenCvHealingStroke();
                PushEditorHistorySnapshot("Healing", $"{HealingMode} {HealingSize:0}px / OpenCV ROI");
                return;
            }

            ApplyOpenCvHealingFallback();
            ClearOpenCvHealingStroke();
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
        UpdateHealingCircleVisibility();
    }

    private void UpdateHealingCircleVisibility()
    {
        HealingCircleVisibility = CanUseHealingPreview() && !CanUseHealingPatchPreview() && ShowHealingCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
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
        ForEachDabPixel(_healingStrokeMaskWidth, _healingStrokeMaskHeight, imagePoint, size, 0, hardEdge: true, opacity: 1.0, (x, y, alpha) =>
        {
            int index = y * _healingStrokeMaskWidth + x;
            _healingStrokeMaskPixels[index] = Math.Max(_healingStrokeMaskPixels[index], (byte)Math.Clamp((int)Math.Round(alpha * 255.0), 0, 255));
        });
    }

    private bool TryApplyOpenCvHealingStroke()
    {
        if (SelectedPhoto is not PhotoItem photo ||
            _healingStrokeTargetBitmap is null ||
            _healingSourceBitmap is null ||
            _healingStrokeMaskPixels is null)
        {
            return false;
        }

        System.Windows.Vector sourceOffset = _healingStrokeStartTargetPoint - _healingStrokeStartSourcePoint;
        if (!OpenCvHealingBrushEngine.TryApplyHealing(
                _healingStrokeTargetBitmap,
                _healingSourceBitmap,
                _healingStrokeMaskPixels,
                _healingStrokeMaskWidth,
                _healingStrokeMaskHeight,
                sourceOffset,
                HealingHardness,
                HealingStrength,
                "NORMAL",
                out System.Windows.Media.Imaging.BitmapSource? result,
                out _))
        {
            return false;
        }

        if (result is null)
        {
            return false;
        }

        photo.SetAdjustedImage(result);
        UpdatePreviewLayout();
        return true;
    }

    private void ApplyOpenCvHealingFallback()
    {
        if (SelectedPhoto is not PhotoItem photo ||
            _healingStrokeTargetBitmap is null ||
            _healingSourceBitmap is null ||
            _healingStrokePoints.Count == 0)
        {
            return;
        }

        photo.SetAdjustedImage(new System.Windows.Media.Imaging.WriteableBitmap(_healingStrokeTargetBitmap));
        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        BeginSourceCopyStroke(target);
        foreach (System.Windows.Point point in _healingStrokePoints)
        {
            ApplyHealingDab(target, point, pressure: 1.0);
        }

        EndSourceCopyStroke();
        UpdatePreviewLayout();
    }

    private void ClearOpenCvHealingStroke()
    {
        _isOpenCvHealingStroke = false;
        _healingStrokeTargetBitmap = null;
        _healingStrokeMaskPixels = null;
        _healingStrokeMaskWidth = 0;
        _healingStrokeMaskHeight = 0;
        _healingStrokePoints.Clear();
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
            _isHealingPatchDragging = true;
            _healingPatchDragStartPreviewPoint = clampedPoint;
            _healingPatchDragStartLeft = HealingPatchSelectionLeft;
            _healingPatchDragStartTop = HealingPatchSelectionTop;
            PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
            System.Windows.Input.Mouse.Capture(PreviewSurface);
            return;
        }

        _isHealingPatchCreating = true;
        _hasHealingPatchSelection = false;
        _healingPatchStartPreviewPoint = clampedPoint;
        HealingPatchSelectionLeft = clampedPoint.X;
        HealingPatchSelectionTop = clampedPoint.Y;
        HealingPatchSelectionWidth = 0;
        HealingPatchSelectionHeight = 0;
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueHealingPatchInteraction(System.Windows.Point previewPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(previewPoint);
        if (_isHealingPatchCreating)
        {
            UpdateHealingPatchSelection(clampedPoint);
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
            _hasHealingPatchSelection = HealingPatchSelectionWidth >= 4 && HealingPatchSelectionHeight >= 4;
            SyncHealingPatchSelectionImageRectFromOverlay();
            OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
            System.Windows.Input.Mouse.Capture(null);
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

    private void UpdateHealingPatchSelection(System.Windows.Point currentPoint)
    {
        double left = Math.Min(_healingPatchStartPreviewPoint.X, currentPoint.X);
        double top = Math.Min(_healingPatchStartPreviewPoint.Y, currentPoint.Y);
        double right = Math.Max(_healingPatchStartPreviewPoint.X, currentPoint.X);
        double bottom = Math.Max(_healingPatchStartPreviewPoint.Y, currentPoint.Y);

        HealingPatchSelectionLeft = left;
        HealingPatchSelectionTop = top;
        HealingPatchSelectionWidth = Math.Max(0, right - left);
        HealingPatchSelectionHeight = Math.Max(0, bottom - top);
        ClampHealingPatchSelectionOverlayToImageBounds();
    }

    private void MoveHealingPatchSelection(System.Windows.Point currentPoint)
    {
        Vector delta = currentPoint - _healingPatchDragStartPreviewPoint;
        HealingPatchSelectionLeft = _healingPatchDragStartLeft + delta.X;
        HealingPatchSelectionTop = _healingPatchDragStartTop + delta.Y;
        ClampHealingPatchSelectionOverlayToImageBounds();
    }

    private void ApplyHealingPatchSelection()
    {
        if (!_hasHealingPatchSelection ||
            SelectedPhoto is not PhotoItem photo ||
            !TryGetHealingPatchImageRectFromOverlay(out Rect movedRect))
        {
            return;
        }

        Rect sourceRect;
        Rect targetRect;
        if (string.Equals(HealingPatchMode, "destination", StringComparison.OrdinalIgnoreCase))
        {
            sourceRect = _healingPatchSelectionImageRect;
            targetRect = movedRect;
        }
        else
        {
            sourceRect = movedRect;
            targetRect = _healingPatchSelectionImageRect;
        }

        System.Windows.Media.Imaging.BitmapSource currentSource = CloneBitmapSource(GetCurrentDisplayBitmapSource(photo));
        if (!TryBuildHealingPatchMask(currentSource.PixelWidth, currentSource.PixelHeight, targetRect, out byte[] maskPixels, out Rect clippedTargetRect))
        {
            return;
        }

        sourceRect = ClipHealingPatchRect(sourceRect, currentSource.PixelWidth, currentSource.PixelHeight);
        if (sourceRect.Width < 2 || sourceRect.Height < 2)
        {
            return;
        }

        Vector sourceOffset = new(clippedTargetRect.Left - sourceRect.Left, clippedTargetRect.Top - sourceRect.Top);
        if (!OpenCvHealingBrushEngine.TryApplyHealing(
                currentSource,
                currentSource,
                maskPixels,
                currentSource.PixelWidth,
                currentSource.PixelHeight,
                sourceOffset,
                HealingHardness,
                HealingStrength,
                "NORMAL",
                out System.Windows.Media.Imaging.BitmapSource? result,
                out _) ||
            result is null)
        {
            return;
        }

        photo.SetAdjustedImage(result);
        UpdatePreviewLayout();
        PushEditorHistorySnapshot("Patch", $"{HealingPatchMode} {clippedTargetRect.Width:0}x{clippedTargetRect.Height:0} / OpenCV ROI");
        ClearHealingPatchSelection();
    }

    private bool TryBuildHealingPatchMask(int width, int height, Rect targetRect, out byte[] maskPixels, out Rect clippedRect)
    {
        maskPixels = new byte[width * height];
        clippedRect = ClipHealingPatchRect(targetRect, width, height);
        int left = Math.Clamp((int)Math.Floor(clippedRect.Left), 0, width - 1);
        int top = Math.Clamp((int)Math.Floor(clippedRect.Top), 0, height - 1);
        int right = Math.Clamp((int)Math.Ceiling(clippedRect.Right), left + 1, width);
        int bottom = Math.Clamp((int)Math.Ceiling(clippedRect.Bottom), top + 1, height);
        if (right - left < 2 || bottom - top < 2)
        {
            return false;
        }

        for (int y = top; y < bottom; y++)
        {
            int row = y * width;
            for (int x = left; x < right; x++)
            {
                maskPixels[row + x] = 255;
            }
        }

        clippedRect = new Rect(left, top, right - left, bottom - top);
        return true;
    }

    private bool TryGetHealingPatchImageRectFromOverlay(out Rect imageRect)
    {
        imageRect = Rect.Empty;
        if (SelectedPhoto is null ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            return false;
        }

        System.Windows.Media.Imaging.BitmapSource source = GetCurrentDisplayBitmapSource(SelectedPhoto);
        double scaleX = source.PixelWidth / PreviewImageWidth;
        double scaleY = source.PixelHeight / PreviewImageHeight;
        imageRect = new Rect(
            Math.Clamp((HealingPatchSelectionLeft - PreviewImageLeft) * scaleX, 0, source.PixelWidth),
            Math.Clamp((HealingPatchSelectionTop - PreviewImageTop) * scaleY, 0, source.PixelHeight),
            Math.Clamp(HealingPatchSelectionWidth * scaleX, 0, source.PixelWidth),
            Math.Clamp(HealingPatchSelectionHeight * scaleY, 0, source.PixelHeight));
        imageRect = ClipHealingPatchRect(imageRect, source.PixelWidth, source.PixelHeight);
        return imageRect.Width >= 2 && imageRect.Height >= 2;
    }

    private void SyncHealingPatchSelectionImageRectFromOverlay()
    {
        if (TryGetHealingPatchImageRectFromOverlay(out Rect imageRect))
        {
            _healingPatchSelectionImageRect = imageRect;
        }
    }

    private static Rect ClipHealingPatchRect(Rect rect, int width, int height)
    {
        double left = Math.Clamp(rect.Left, 0, width);
        double top = Math.Clamp(rect.Top, 0, height);
        double right = Math.Clamp(rect.Right, left, width);
        double bottom = Math.Clamp(rect.Bottom, top, height);
        return new Rect(left, top, right - left, bottom - top);
    }

    private void ClampHealingPatchSelectionOverlayToImageBounds()
    {
        if (PreviewImageWidth <= 0 || PreviewImageHeight <= 0)
        {
            return;
        }

        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        HealingPatchSelectionWidth = Math.Max(0, Math.Min(HealingPatchSelectionWidth, PreviewImageWidth));
        HealingPatchSelectionHeight = Math.Max(0, Math.Min(HealingPatchSelectionHeight, PreviewImageHeight));
        HealingPatchSelectionLeft = Math.Clamp(HealingPatchSelectionLeft, imageLeft, Math.Max(imageLeft, imageRight - HealingPatchSelectionWidth));
        HealingPatchSelectionTop = Math.Clamp(HealingPatchSelectionTop, imageTop, Math.Max(imageTop, imageBottom - HealingPatchSelectionHeight));
    }

    private bool IsPreviewPointInsideHealingPatchSelection(System.Windows.Point point)
    {
        return point.X >= HealingPatchSelectionLeft &&
               point.X <= HealingPatchSelectionLeft + HealingPatchSelectionWidth &&
               point.Y >= HealingPatchSelectionTop &&
               point.Y <= HealingPatchSelectionTop + HealingPatchSelectionHeight;
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
        HealingPatchSelectionLeft = 0;
        HealingPatchSelectionTop = 0;
        HealingPatchSelectionWidth = 0;
        HealingPatchSelectionHeight = 0;
        OnPropertyChanged(nameof(HealingPatchSelectionVisibility));
    }
}
