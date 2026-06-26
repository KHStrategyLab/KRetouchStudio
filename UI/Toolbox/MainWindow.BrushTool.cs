using System;
using System.Collections.Generic;
using System.Windows;

namespace KRetouchStudio;

public partial class MainWindow
{
    private string _brushMode = "brush";

    public Visibility BrushToolOptionsVisibility => string.Equals(ActiveToolId, "brush", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    public string BrushMode
    {
        get => _brushMode;
        private set
        {
            if (string.Equals(_brushMode, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _brushMode = value;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BrushSize
    {
        get => _brushSize;
        set
        {
            double clamped = Math.Clamp(value, 1, 600);
            if (Math.Abs(_brushSize - clamped) < 0.01)
            {
                return;
            }

            _brushSize = clamped;
            BrushCircleSize = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BrushSoftness
    {
        get => _brushSoftness;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_brushSoftness - clamped) < 0.01)
            {
                return;
            }

            _brushSoftness = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public double BrushOpacity
    {
        get => _brushOpacity;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_brushOpacity - clamped) < 0.01)
            {
                return;
            }

            _brushOpacity = clamped;
            OnPropertyChanged();
            SaveToolboxDefaults();
        }
    }

    public bool ShowBrushCircle
    {
        get => _showBrushCircle;
        set
        {
            if (_showBrushCircle == value)
            {
                return;
            }

            _showBrushCircle = value;
            OnPropertyChanged();
            UpdateBrushCircleVisibility();
            SaveToolboxDefaults();
        }
    }

    public System.Windows.Media.Brush BrushColorPreview
    {
        get => _brushColorPreview;
        private set
        {
            _brushColorPreview = value;
            OnPropertyChanged();
        }
    }

    public double BrushCircleLeft
    {
        get => _brushCircleLeft;
        private set
        {
            _brushCircleLeft = value;
            OnPropertyChanged();
        }
    }

    public double BrushCircleTop
    {
        get => _brushCircleTop;
        private set
        {
            _brushCircleTop = value;
            OnPropertyChanged();
        }
    }

    public double BrushCircleSize
    {
        get => _brushCircleSize;
        private set
        {
            _brushCircleSize = value;
            OnPropertyChanged();
        }
    }

    public Visibility BrushCircleVisibility
    {
        get => _brushCircleVisibility;
        private set
        {
            _brushCircleVisibility = value;
            OnPropertyChanged();
        }
    }

    private void BrushModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string mode || string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        BrushMode = mode;
        UpdateBrushModeSelection();
    }

    private void BrushResetButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ResetBrushPreviewToOriginal();
    }

    private void ResetBrushPreviewToOriginal()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        _isBrushDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        ClearDodgeBurnSession(false);
        ClearLiquifySession(false);
        ClearFaceShapeSymmetrySession();
        photo.ResetAdjustedImage();
        _editorUndoHistory.Clear();
        _editorRedoHistory.Clear();
        HistoryPanelItems.Clear();
        SelectedHistoryPanelItem = null;
        UpdatePreviewLayout();
        PushEditorHistorySnapshot("Open Photo", $"Session restarted for {photo.FileName}");
    }

    private void UpdateBrushModeSelection()
    {
        foreach (System.Windows.Controls.Button button in GetBrushModeButtons())
        {
            bool isActive = button.Tag is string mode &&
                            string.Equals(mode, BrushMode, StringComparison.OrdinalIgnoreCase);
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

    private IEnumerable<System.Windows.Controls.Button> GetBrushModeButtons()
    {
        yield return BrushModeButton;
        yield return PencilModeButton;
    }

    private void BrushColorPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is string colorText)
        {
            SetBrushColorFromText(colorText);
        }
    }

    private void BrushColorPickerButton_Click(object sender, RoutedEventArgs e)
    {
        using System.Windows.Forms.ColorDialog dialog = new();
        if (BrushColorPreview is System.Windows.Media.SolidColorBrush currentBrush)
        {
            System.Windows.Media.Color currentColor = currentBrush.Color;
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        System.Drawing.Color pickedColor = dialog.Color;
        BrushColorPreview = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(pickedColor.R, pickedColor.G, pickedColor.B));
    }

    private void SetBrushColorFromText(string colorText)
    {
        object? converted = System.Windows.Media.ColorConverter.ConvertFromString(colorText);
        if (converted is System.Windows.Media.Color color)
        {
            BrushColorPreview = new System.Windows.Media.SolidColorBrush(color);
        }
    }

    private bool CanUseBrushPreview()
    {
        return string.Equals(ActiveToolId, "brush", StringComparison.OrdinalIgnoreCase) &&
               CanUseSinglePreviewTool();
    }

    private void StartBrushStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (!CanUseBrushPreview() ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint))
        {
            return;
        }

        if (!IsPencilBrushMode &&
            (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt)
        {
            TrySetBrushColorFromCurrentImagePoint(imagePoint);
            return;
        }

        if (!TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        _isBrushDragging = true;
        _brushLastImagePoint = imagePoint;
        double size = ApplyToolPressureToSize(BrushSize, pressure);
        double opacity = ApplyToolPressureToOpacity(BrushOpacity / 100.0, pressure);
        ApplyPaintDab(target, imagePoint, size, BrushSoftness, GetCurrentBrushColor(), IsPencilBrushMode, opacity);
        System.Windows.Input.Mouse.Capture(PreviewSurface);
    }

    private void ContinueBrushStroke(System.Windows.Point previewPoint, double pressure)
    {
        if (!_isBrushDragging ||
            !TryPreviewPointToImagePoint(previewPoint, out System.Windows.Point imagePoint) ||
            !TryGetToolWorkingBitmap(out _, out System.Windows.Media.Imaging.WriteableBitmap target))
        {
            return;
        }

        ApplyBrushStrokeSegment(target, _brushLastImagePoint, imagePoint, pressure);
        _brushLastImagePoint = imagePoint;
    }

    private void StopBrushStroke()
    {
        if (!_isBrushDragging)
        {
            return;
        }

        _isBrushDragging = false;
        System.Windows.Input.Mouse.Capture(null);
        PushEditorHistorySnapshot(IsPencilBrushMode ? "Pencil" : "Brush", $"{BrushSize:0}px");
    }

    private void UpdateBrushCircle(System.Windows.Point previewPoint, double pressure)
    {
        System.Windows.Point center = ClampPointToPreviewImage(previewPoint);
        double size = ApplyToolPressureToSize(BrushSize, pressure);
        BrushCircleSize = size;
        BrushCircleLeft = center.X - (size * 0.5);
        BrushCircleTop = center.Y - (size * 0.5);
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        UpdateBrushCircleVisibility();
    }

    private void UpdateBrushCircleVisibility()
    {
        BrushCircleVisibility = CanUseBrushPreview() && ShowBrushCircle
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyBrushStrokeSegment(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point fromImagePoint,
        System.Windows.Point toImagePoint,
        double pressure)
    {
        System.Windows.Media.Color color = GetCurrentBrushColor();
        double size = ApplyToolPressureToSize(BrushSize, pressure);
        double opacity = ApplyToolPressureToOpacity(BrushOpacity / 100.0, pressure);
        ForEachToolStrokePoint(fromImagePoint, toImagePoint, size, point =>
        {
            ApplyPaintDab(target, point, size, BrushSoftness, color, IsPencilBrushMode, opacity);
        });
    }

    private bool IsPencilBrushMode => string.Equals(BrushMode, "pencil", StringComparison.OrdinalIgnoreCase);

    private bool TrySetBrushColorFromCurrentImagePoint(System.Windows.Point imagePoint)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return false;
        }

        System.Windows.Media.Imaging.BitmapSource source = EnsureBgraBitmapSource(GetCurrentDisplayBitmapSource(photo));
        int x = Math.Clamp((int)Math.Round(imagePoint.X), 0, source.PixelWidth - 1);
        int y = Math.Clamp((int)Math.Round(imagePoint.Y), 0, source.PixelHeight - 1);
        byte[] pixel = new byte[4];
        source.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);

        System.Windows.Media.SolidColorBrush brush = new(System.Windows.Media.Color.FromRgb(pixel[2], pixel[1], pixel[0]));
        brush.Freeze();
        BrushColorPreview = brush;
        return true;
    }

    private System.Windows.Media.Color GetCurrentBrushColor()
    {
        return BrushColorPreview is System.Windows.Media.SolidColorBrush brush
            ? brush.Color
            : System.Windows.Media.Colors.White;
    }

    private bool TryGetToolWorkingBitmap(out PhotoItem photo, out System.Windows.Media.Imaging.WriteableBitmap target)
    {
        target = null!;
        if (SelectedPhoto is not PhotoItem selectedPhoto)
        {
            photo = null!;
            return false;
        }

        photo = selectedPhoto;
        target = EnsureToolWorkingBitmap(photo);
        return true;
    }

    private System.Windows.Media.Imaging.WriteableBitmap EnsureToolWorkingBitmap(PhotoItem photo)
    {
        if (photo.Image is System.Windows.Media.Imaging.WriteableBitmap writable &&
            writable.Format == System.Windows.Media.PixelFormats.Bgra32)
        {
            return writable;
        }

        System.Windows.Media.Imaging.BitmapSource currentSource = GetCurrentDisplayBitmapSource(photo);
        System.Windows.Media.Imaging.BitmapSource bgraSource = currentSource.Format == System.Windows.Media.PixelFormats.Bgra32
            ? currentSource
            : new System.Windows.Media.Imaging.FormatConvertedBitmap(currentSource, System.Windows.Media.PixelFormats.Bgra32, null, 0);
        System.Windows.Media.Imaging.WriteableBitmap workingBitmap = new(bgraSource);
        photo.SetAdjustedImage(workingBitmap);
        return workingBitmap;
    }

    private static bool ApplyPaintDab(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point center,
        double size,
        double softness,
        System.Windows.Media.Color color,
        bool hardEdge,
        double opacity)
    {
        CopyBgraPixels(target, out byte[] pixels, out int stride);
        int width = target.PixelWidth;
        int height = target.PixelHeight;
        bool changed = false;

        ForEachDabPixel(width, height, center, size, softness, hardEdge, opacity, (x, y, alpha) =>
        {
            int index = y * stride + x * 4;
            pixels[index + 0] = BlendByte(pixels[index + 0], color.B, alpha);
            pixels[index + 1] = BlendByte(pixels[index + 1], color.G, alpha);
            pixels[index + 2] = BlendByte(pixels[index + 2], color.R, alpha);
            pixels[index + 3] = 255;
            changed = true;
        });

        if (changed)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        return changed;
    }

    private static bool ApplyRestoreDab(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Media.Imaging.BitmapSource restoreSource,
        System.Windows.Point center,
        double size,
        double softness,
        double opacity)
    {
        CopyBgraPixels(target, out byte[] pixels, out int stride);
        System.Windows.Media.Imaging.BitmapSource source = EnsureBgraBitmapSource(restoreSource);
        CopyBgraPixels(source, out byte[] sourcePixels, out int sourceStride);
        int width = target.PixelWidth;
        int height = target.PixelHeight;
        bool changed = false;

        ForEachDabPixel(width, height, center, size, softness, false, opacity, (x, y, alpha) =>
        {
            int sourceX = ScaleCoordinate(x, width, source.PixelWidth);
            int sourceY = ScaleCoordinate(y, height, source.PixelHeight);
            int index = y * stride + x * 4;
            int sourceIndex = sourceY * sourceStride + sourceX * 4;
            pixels[index + 0] = BlendByte(pixels[index + 0], sourcePixels[sourceIndex + 0], alpha);
            pixels[index + 1] = BlendByte(pixels[index + 1], sourcePixels[sourceIndex + 1], alpha);
            pixels[index + 2] = BlendByte(pixels[index + 2], sourcePixels[sourceIndex + 2], alpha);
            pixels[index + 3] = 255;
            changed = true;
        });

        if (changed)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        return changed;
    }

    private bool ApplySourceCopyDab(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Media.Imaging.BitmapSource sourceBitmap,
        System.Windows.Point targetCenter,
        System.Windows.Point sourceCenter,
        double size,
        double softness,
        double opacity,
        bool matchTargetTone = false)
    {
        CopyBgraPixels(target, out byte[] pixels, out int stride);
        System.Windows.Media.Imaging.BitmapSource source = EnsureBgraBitmapSource(sourceBitmap);
        CopyBgraPixels(source, out byte[] sourcePixels, out int sourceStride);
        int width = target.PixelWidth;
        int height = target.PixelHeight;
        bool changed = false;
        bool useStrokeBase = IsSourceCopyStrokeBaseValid(target);
        double toneDeltaB = 0;
        double toneDeltaG = 0;
        double toneDeltaR = 0;

        if (matchTargetTone)
        {
            double sourceSumB = 0;
            double sourceSumG = 0;
            double sourceSumR = 0;
            double targetSumB = 0;
            double targetSumG = 0;
            double targetSumR = 0;
            double weightSum = 0;

            ForEachDabPixel(width, height, targetCenter, size, softness, false, opacity, (x, y, alpha) =>
            {
                int sourceX = (int)Math.Round(sourceCenter.X + (x - targetCenter.X));
                int sourceY = (int)Math.Round(sourceCenter.Y + (y - targetCenter.Y));
                if (sourceX < 0 || sourceY < 0 || sourceX >= source.PixelWidth || sourceY >= source.PixelHeight)
                {
                    return;
                }

                int index = y * stride + x * 4;
                int sourceIndex = sourceY * sourceStride + sourceX * 4;
                int baseIndex = useStrokeBase ? y * _sourceCopyStrokeStride + x * 4 : index;
                byte baseB = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 0] : pixels[index + 0];
                byte baseG = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 1] : pixels[index + 1];
                byte baseR = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 2] : pixels[index + 2];
                double weight = Math.Clamp(alpha, 0.0, 1.0);

                targetSumB += baseB * weight;
                targetSumG += baseG * weight;
                targetSumR += baseR * weight;
                sourceSumB += sourcePixels[sourceIndex + 0] * weight;
                sourceSumG += sourcePixels[sourceIndex + 1] * weight;
                sourceSumR += sourcePixels[sourceIndex + 2] * weight;
                weightSum += weight;
            });

            if (weightSum > 0.001)
            {
                toneDeltaB = (targetSumB - sourceSumB) / weightSum;
                toneDeltaG = (targetSumG - sourceSumG) / weightSum;
                toneDeltaR = (targetSumR - sourceSumR) / weightSum;
            }
        }

        ForEachDabPixel(width, height, targetCenter, size, softness, false, opacity, (x, y, alpha) =>
        {
            int sourceX = (int)Math.Round(sourceCenter.X + (x - targetCenter.X));
            int sourceY = (int)Math.Round(sourceCenter.Y + (y - targetCenter.Y));
            if (sourceX < 0 || sourceY < 0 || sourceX >= source.PixelWidth || sourceY >= source.PixelHeight)
            {
                return;
            }

            int index = y * stride + x * 4;
            int sourceIndex = sourceY * sourceStride + sourceX * 4;
            int coverageIndex = y * width + x;
            byte coverage = (byte)Math.Clamp((int)Math.Round(alpha * 255.0), 0, 255);
            if (useStrokeBase &&
                _sourceCopyStrokeCoverage is not null &&
                coverage + 1 < _sourceCopyStrokeCoverage[coverageIndex])
            {
                return;
            }

            if (useStrokeBase && _sourceCopyStrokeCoverage is not null)
            {
                _sourceCopyStrokeCoverage[coverageIndex] = Math.Max(_sourceCopyStrokeCoverage[coverageIndex], coverage);
            }

            int baseIndex = useStrokeBase ? y * _sourceCopyStrokeStride + x * 4 : index;
            byte baseB = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 0] : pixels[index + 0];
            byte baseG = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 1] : pixels[index + 1];
            byte baseR = useStrokeBase && _sourceCopyStrokeBasePixels is not null ? _sourceCopyStrokeBasePixels[baseIndex + 2] : pixels[index + 2];
            byte sourceB = matchTargetTone ? ClampByte(sourcePixels[sourceIndex + 0] + toneDeltaB) : sourcePixels[sourceIndex + 0];
            byte sourceG = matchTargetTone ? ClampByte(sourcePixels[sourceIndex + 1] + toneDeltaG) : sourcePixels[sourceIndex + 1];
            byte sourceR = matchTargetTone ? ClampByte(sourcePixels[sourceIndex + 2] + toneDeltaR) : sourcePixels[sourceIndex + 2];

            pixels[index + 0] = BlendByte(baseB, sourceB, alpha);
            pixels[index + 1] = BlendByte(baseG, sourceG, alpha);
            pixels[index + 2] = BlendByte(baseR, sourceR, alpha);
            pixels[index + 3] = 255;
            changed = true;
        });

        if (changed)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        return changed;
    }

    private void BeginSourceCopyStroke(System.Windows.Media.Imaging.WriteableBitmap target)
    {
        CopyBgraPixels(target, out _sourceCopyStrokeBasePixels, out _sourceCopyStrokeStride);
        _sourceCopyStrokeWidth = target.PixelWidth;
        _sourceCopyStrokeHeight = target.PixelHeight;
        _sourceCopyStrokeCoverage = new byte[target.PixelWidth * target.PixelHeight];
    }

    private void EndSourceCopyStroke()
    {
        _sourceCopyStrokeBasePixels = null;
        _sourceCopyStrokeCoverage = null;
        _sourceCopyStrokeStride = 0;
        _sourceCopyStrokeWidth = 0;
        _sourceCopyStrokeHeight = 0;
    }

    private bool IsSourceCopyStrokeBaseValid(System.Windows.Media.Imaging.WriteableBitmap target)
    {
        int pixelCount = target.PixelWidth * target.PixelHeight;
        return _sourceCopyStrokeBasePixels is not null &&
               _sourceCopyStrokeCoverage is not null &&
               _sourceCopyStrokeStride == target.PixelWidth * 4 &&
               _sourceCopyStrokeWidth == target.PixelWidth &&
               _sourceCopyStrokeHeight == target.PixelHeight &&
               _sourceCopyStrokeCoverage.Length == pixelCount &&
               _sourceCopyStrokeBasePixels.Length == _sourceCopyStrokeStride * target.PixelHeight;
    }

    private static bool ApplyBlurSharpDab(
        System.Windows.Media.Imaging.WriteableBitmap target,
        System.Windows.Point center,
        double size,
        double softness,
        double radius,
        double strength,
        bool sharpen)
    {
        CopyBgraPixels(target, out byte[] sourcePixels, out int stride);
        byte[] outputPixels = (byte[])sourcePixels.Clone();
        int width = target.PixelWidth;
        int height = target.PixelHeight;
        int kernelRadius = Math.Clamp((int)Math.Round(radius / 4.0), 1, 6);
        double opacity = Math.Clamp(strength / 100.0, 0.0, 1.0);
        bool changed = false;

        ForEachDabPixel(width, height, center, size, softness, false, opacity, (x, y, alpha) =>
        {
            int count = 0;
            int sumB = 0;
            int sumG = 0;
            int sumR = 0;
            for (int yy = Math.Max(0, y - kernelRadius); yy <= Math.Min(height - 1, y + kernelRadius); yy++)
            {
                for (int xx = Math.Max(0, x - kernelRadius); xx <= Math.Min(width - 1, x + kernelRadius); xx++)
                {
                    int sampleIndex = yy * stride + xx * 4;
                    sumB += sourcePixels[sampleIndex + 0];
                    sumG += sourcePixels[sampleIndex + 1];
                    sumR += sourcePixels[sampleIndex + 2];
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            int index = y * stride + x * 4;
            byte avgB = (byte)(sumB / count);
            byte avgG = (byte)(sumG / count);
            byte avgR = (byte)(sumR / count);
            if (sharpen)
            {
                outputPixels[index + 0] = BlendByte(sourcePixels[index + 0], ClampByte(sourcePixels[index + 0] + (sourcePixels[index + 0] - avgB)), alpha);
                outputPixels[index + 1] = BlendByte(sourcePixels[index + 1], ClampByte(sourcePixels[index + 1] + (sourcePixels[index + 1] - avgG)), alpha);
                outputPixels[index + 2] = BlendByte(sourcePixels[index + 2], ClampByte(sourcePixels[index + 2] + (sourcePixels[index + 2] - avgR)), alpha);
            }
            else
            {
                outputPixels[index + 0] = BlendByte(sourcePixels[index + 0], avgB, alpha);
                outputPixels[index + 1] = BlendByte(sourcePixels[index + 1], avgG, alpha);
                outputPixels[index + 2] = BlendByte(sourcePixels[index + 2], avgR, alpha);
            }

            outputPixels[index + 3] = 255;
            changed = true;
        });

        if (changed)
        {
            target.WritePixels(new Int32Rect(0, 0, width, height), outputPixels, stride, 0);
        }

        return changed;
    }

    private static void ForEachDabPixel(
        int width,
        int height,
        System.Windows.Point center,
        double size,
        double softness,
        bool hardEdge,
        double opacity,
        Action<int, int, double> action)
    {
        double radius = Math.Max(0.5, size * 0.5);
        int minX = Math.Max(0, (int)Math.Floor(center.X - radius));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(center.X + radius));
        int minY = Math.Max(0, (int)Math.Floor(center.Y - radius));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(center.Y + radius));
        double soft = hardEdge ? 0 : Math.Clamp(softness / 100.0, 0.0, 1.0);
        double innerRadius = radius * (1.0 - soft);
        double featherWidth = Math.Max(0.001, radius - innerRadius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                double dx = x + 0.5 - center.X;
                double dy = y + 0.5 - center.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > radius)
                {
                    continue;
                }

                double alpha = 1.0;
                if (!hardEdge && distance > innerRadius)
                {
                    double t = Math.Clamp((radius - distance) / featherWidth, 0.0, 1.0);
                    alpha = t * t * (3.0 - 2.0 * t);
                }

                alpha *= Math.Clamp(opacity, 0.0, 1.0);
                if (alpha <= 0.001)
                {
                    continue;
                }

                action(x, y, alpha);
            }
        }
    }

    private static System.Windows.Media.Imaging.BitmapSource EnsureBgraBitmapSource(System.Windows.Media.Imaging.BitmapSource source)
    {
        return source.Format == System.Windows.Media.PixelFormats.Bgra32
            ? source
            : new System.Windows.Media.Imaging.FormatConvertedBitmap(source, System.Windows.Media.PixelFormats.Bgra32, null, 0);
    }

    private static void CopyBgraPixels(System.Windows.Media.Imaging.BitmapSource source, out byte[] pixels, out int stride)
    {
        System.Windows.Media.Imaging.BitmapSource bgraSource = EnsureBgraBitmapSource(source);
        stride = bgraSource.PixelWidth * 4;
        pixels = new byte[stride * bgraSource.PixelHeight];
        bgraSource.CopyPixels(pixels, stride, 0);
    }

    private static int ScaleCoordinate(int value, int sourceSize, int targetSize)
    {
        if (sourceSize <= 1 || targetSize <= 1)
        {
            return 0;
        }

        return Math.Clamp((int)Math.Round(value * (targetSize - 1.0) / (sourceSize - 1.0)), 0, targetSize - 1);
    }

    private static void ForEachToolStrokePoint(
        System.Windows.Point fromImagePoint,
        System.Windows.Point toImagePoint,
        double brushSize,
        Action<System.Windows.Point> apply)
    {
        double dx = toImagePoint.X - fromImagePoint.X;
        double dy = toImagePoint.Y - fromImagePoint.Y;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));
        double spacing = Math.Max(0.75, brushSize * 0.08);
        int steps = Math.Clamp((int)Math.Ceiling(distance / spacing), 1, 160);
        for (int step = 1; step <= steps; step++)
        {
            double t = step / (double)steps;
            apply(new System.Windows.Point(
                fromImagePoint.X + (dx * t),
                fromImagePoint.Y + (dy * t)));
        }
    }

    private static byte BlendByte(byte current, byte target, double alpha)
    {
        return ClampByte(current + (target - current) * alpha);
    }

    private static byte ClampByte(double value)
    {
        return (byte)Math.Clamp((int)Math.Round(value), 0, 255);
    }

    private static double GetToolInputPressure(System.Windows.Input.MouseEventArgs e, System.Windows.IInputElement relativeTo)
    {
        if (e.StylusDevice is null)
        {
            return 1.0;
        }

        try
        {
            System.Windows.Input.StylusPointCollection points = e.StylusDevice.GetStylusPoints(relativeTo);
            if (points.Count == 0)
            {
                return 1.0;
            }

            double pressure = points[^1].PressureFactor;
            if (double.IsNaN(pressure) || double.IsInfinity(pressure) || pressure <= 0)
            {
                return 1.0;
            }

            return Math.Clamp(pressure, 0.05, 1.0);
        }
        catch (InvalidOperationException)
        {
            return 1.0;
        }
    }

    private static double ApplyToolPressureToSize(double baseSize, double pressure)
    {
        double ratio = 0.25 + (Math.Clamp(pressure, 0.0, 1.0) * 0.75);
        return Math.Max(1.0, baseSize * ratio);
    }

    private static double ApplyToolPressureToOpacity(double baseOpacity, double pressure)
    {
        double ratio = 0.15 + (Math.Clamp(pressure, 0.0, 1.0) * 0.85);
        return Math.Clamp(baseOpacity * ratio, 0.0, 1.0);
    }
}
