using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPoint = System.Windows.Point;

namespace KRetouchStudio;

public partial class MainWindow
{
    private enum RectangleSelectionHitZone
    {
        None,
        Inside,
        Left,
        Right,
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        RotateTopLeft,
        RotateTopRight,
        RotateBottomLeft,
        RotateBottomRight
    }

    private const double CropRotateHandleSize = 6;
    private const double CropRotateIconSize = 16;
    private const double CropRotateOutsideHitRadius = 26;
    private const double CropRotateCursorSize = 24;
    private const double CropRotateSnapAngle = 15.0;
    private const double RectangleSelectionHitTolerance = 8;

    public Visibility RectangleToolOptionsVisibility => string.Equals(ActiveToolId, "rectangle", StringComparison.OrdinalIgnoreCase)
        ? Visibility.Visible
        : Visibility.Collapsed;

    private bool IsCropOrRectangleToolActive =>
        string.Equals(ActiveToolId, "rectangle", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase);

    public Visibility CropOutlineSelectionVisibility =>
        RectangleSelectionVisibility == Visibility.Visible &&
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility RectangleOutlineSelectionVisibility =>
        RectangleSelectionVisibility == Visibility.Visible &&
        string.Equals(ActiveToolId, "rectangle", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(RectangleShapeMode, "rectangle", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CropOutsideOverlayVisibility =>
        CropOutlineSelectionVisibility == Visibility.Visible &&
        CropOutsideOverlayOpacityPercent > 0 &&
        CropOutsideSelectionGeometry != Geometry.Empty
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Geometry CropOutsideSelectionGeometry
    {
        get
        {
            if (PreviewImageWidth <= 0 ||
                PreviewImageHeight <= 0 ||
                RectangleSelectionVisibility != Visibility.Visible ||
                RectangleSelectionWidth < 4 ||
                RectangleSelectionHeight < 4 ||
                !string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase))
            {
                return Geometry.Empty;
            }

            RectangleGeometry imageGeometry = new(new Rect(
                PreviewImageLeft,
                PreviewImageTop,
                PreviewImageWidth,
                PreviewImageHeight));
            RectangleGeometry cropGeometry = new(new Rect(
                RectangleSelectionLeft,
                RectangleSelectionTop,
                RectangleSelectionWidth,
                RectangleSelectionHeight));

            if (Math.Abs(RectangleSelectionRotationAngle) >= 0.01)
            {
                WpfPoint center = GetRectangleSelectionCenter();
                cropGeometry.Transform = new RotateTransform(RectangleSelectionRotationAngle, center.X, center.Y);
            }

            return new CombinedGeometry(GeometryCombineMode.Exclude, imageGeometry, cropGeometry);
        }
    }

    public Visibility RectangleEllipseSelectionVisibility =>
        RectangleSelectionVisibility == Visibility.Visible &&
        string.Equals(ActiveToolId, "rectangle", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(RectangleShapeMode, "ellipse", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility RectanglePolygonSelectionVisibility =>
        RectangleSelectionVisibility == Visibility.Visible &&
        string.Equals(ActiveToolId, "rectangle", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(RectangleShapeMode, "polygon", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CropRotateHandleVisibility =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        RectangleSelectionVisibility == Visibility.Visible &&
        RectangleSelectionWidth >= 4 &&
        RectangleSelectionHeight >= 4
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility CropCenterGuideVisibility =>
        string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase) &&
        RectangleSelectionVisibility == Visibility.Visible &&
        RectangleSelectionWidth >= 12 &&
        RectangleSelectionHeight >= 12
            ? Visibility.Visible
            : Visibility.Collapsed;

    public double CropCenterGuideCenterX => RectangleSelectionLeft + (RectangleSelectionWidth * 0.5);

    public double CropCenterGuideCenterY => RectangleSelectionTop + (RectangleSelectionHeight * 0.5);

    public double CropCenterGuideHorizontalX1 => CropCenterGuideCenterX - 9;

    public double CropCenterGuideHorizontalX2 => CropCenterGuideCenterX + 9;

    public double CropCenterGuideVerticalY1 => CropCenterGuideCenterY - 9;

    public double CropCenterGuideVerticalY2 => CropCenterGuideCenterY + 9;

    public double CropRotateTopLeftHandleLeft => GetCropRotateHandleLeft(GetCropSelectionCornerPoints()[0]);

    public double CropRotateTopLeftHandleTop => GetCropRotateHandleTop(GetCropSelectionCornerPoints()[0]);

    public double CropRotateTopLeftIconLeft => GetCropRotateIconLeft(GetCropSelectionCornerPoints()[0]);

    public double CropRotateTopLeftIconTop => GetCropRotateIconTop(GetCropSelectionCornerPoints()[0]);

    public double CropRotateTopRightHandleLeft => GetCropRotateHandleLeft(GetCropSelectionCornerPoints()[1]);

    public double CropRotateTopRightHandleTop => GetCropRotateHandleTop(GetCropSelectionCornerPoints()[1]);

    public double CropRotateTopRightIconLeft => GetCropRotateIconLeft(GetCropSelectionCornerPoints()[1]);

    public double CropRotateTopRightIconTop => GetCropRotateIconTop(GetCropSelectionCornerPoints()[1]);

    public double CropRotateBottomRightHandleLeft => GetCropRotateHandleLeft(GetCropSelectionCornerPoints()[2]);

    public double CropRotateBottomRightHandleTop => GetCropRotateHandleTop(GetCropSelectionCornerPoints()[2]);

    public double CropRotateBottomRightIconLeft => GetCropRotateIconLeft(GetCropSelectionCornerPoints()[2]);

    public double CropRotateBottomRightIconTop => GetCropRotateIconTop(GetCropSelectionCornerPoints()[2]);

    public double CropRotateBottomLeftHandleLeft => GetCropRotateHandleLeft(GetCropSelectionCornerPoints()[3]);

    public double CropRotateBottomLeftHandleTop => GetCropRotateHandleTop(GetCropSelectionCornerPoints()[3]);

    public double CropRotateBottomLeftIconLeft => GetCropRotateIconLeft(GetCropSelectionCornerPoints()[3]);

    public double CropRotateBottomLeftIconTop => GetCropRotateIconTop(GetCropSelectionCornerPoints()[3]);

    public Visibility CropRotateCursorVisibility => _cropRotateCursorVisibility;

    public double CropRotateCursorLeft
    {
        get => _cropRotateCursorLeft;
        private set
        {
            _cropRotateCursorLeft = value;
            OnPropertyChanged();
        }
    }

    public double CropRotateCursorTop
    {
        get => _cropRotateCursorTop;
        private set
        {
            _cropRotateCursorTop = value;
            OnPropertyChanged();
        }
    }

    public PointCollection RectangleSelectionPolygonPoints
    {
        get
        {
            PointCollection points = [];
            if (RectangleSelectionVisibility != Visibility.Visible ||
                RectangleSelectionWidth < 4 ||
                RectangleSelectionHeight < 4)
            {
                return points;
            }

            int sides = Math.Max(3, RectanglePolygonSides);
            double centerX = RectangleSelectionLeft + (RectangleSelectionWidth * 0.5);
            double centerY = RectangleSelectionTop + (RectangleSelectionHeight * 0.5);
            double radiusX = RectangleSelectionWidth * 0.5;
            double radiusY = RectangleSelectionHeight * 0.5;
            double startAngle = -Math.PI / 2.0;

            for (int i = 0; i < sides; i++)
            {
                double angle = startAngle + ((Math.PI * 2.0 * i) / sides);
                points.Add(new WpfPoint(
                    centerX + (Math.Cos(angle) * radiusX),
                    centerY + (Math.Sin(angle) * radiusY)));
            }

            return points;
        }
    }

    public double RectangleSelectionLeft
    {
        get => _rectangleSelectionLeft;
        private set
        {
            _rectangleSelectionLeft = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
            RaiseRectangleSelectionHandlePropertyChanged();
        }
    }

    public double RectangleSelectionTop
    {
        get => _rectangleSelectionTop;
        private set
        {
            _rectangleSelectionTop = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
            RaiseRectangleSelectionHandlePropertyChanged();
        }
    }

    public double RectangleSelectionWidth
    {
        get => _rectangleSelectionWidth;
        private set
        {
            _rectangleSelectionWidth = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
            RaiseRectangleSelectionHandlePropertyChanged();
        }
    }

    public double RectangleSelectionHeight
    {
        get => _rectangleSelectionHeight;
        private set
        {
            _rectangleSelectionHeight = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
            RaiseRectangleSelectionHandlePropertyChanged();
        }
    }

    public Visibility RectangleSelectionVisibility
    {
        get => _rectangleSelectionVisibility;
        private set
        {
            _rectangleSelectionVisibility = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleOutlineSelectionVisibility));
            OnPropertyChanged(nameof(CropOutlineSelectionVisibility));
            OnPropertyChanged(nameof(CropOutsideOverlayVisibility));
            OnPropertyChanged(nameof(CropOutsideSelectionGeometry));
            OnPropertyChanged(nameof(RectangleEllipseSelectionVisibility));
            OnPropertyChanged(nameof(RectanglePolygonSelectionVisibility));
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
            RaiseRectangleSelectionHandlePropertyChanged();
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double RectangleSelectionImageX
    {
        get => _rectangleSelectionImageX;
        set
        {
            double clamped = ClampRectangleImageValue(value, 0);
            if (Math.Abs(_rectangleSelectionImageX - clamped) < 0.01)
            {
                return;
            }

            _rectangleSelectionImageX = clamped;
            OnPropertyChanged();
            UpdateRectangleSelectionOverlayFromImageRect();
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double RectangleSelectionImageY
    {
        get => _rectangleSelectionImageY;
        set
        {
            double clamped = ClampRectangleImageValue(value, 0);
            if (Math.Abs(_rectangleSelectionImageY - clamped) < 0.01)
            {
                return;
            }

            _rectangleSelectionImageY = clamped;
            OnPropertyChanged();
            UpdateRectangleSelectionOverlayFromImageRect();
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double RectangleSelectionImageWidth
    {
        get => _rectangleSelectionImageWidth;
        set
        {
            double clamped = Math.Max(0, ClampRectangleImageValue(value, 0));
            if (Math.Abs(_rectangleSelectionImageWidth - clamped) < 0.01)
            {
                return;
            }

            _rectangleSelectionImageWidth = clamped;
            OnPropertyChanged();
            UpdateRectangleSelectionOverlayFromImageRect();
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double RectangleSelectionImageHeight
    {
        get => _rectangleSelectionImageHeight;
        set
        {
            double clamped = Math.Max(0, ClampRectangleImageValue(value, 0));
            if (Math.Abs(_rectangleSelectionImageHeight - clamped) < 0.01)
            {
                return;
            }

            _rectangleSelectionImageHeight = clamped;
            OnPropertyChanged();
            UpdateRectangleSelectionOverlayFromImageRect();
            RaiseCropTelemetryPropertyChanged();
        }
    }

    public double RectangleSelectionFeather
    {
        get => _rectangleSelectionFeather;
        set
        {
            double clamped = Math.Clamp(Math.Round(value), 0, 500);
            if (Math.Abs(_rectangleSelectionFeather - clamped) < 0.01)
            {
                return;
            }

            _rectangleSelectionFeather = clamped;
            OnPropertyChanged();
        }
    }

    public int RectanglePolygonSides
    {
        get => _rectanglePolygonSides;
        set
        {
            int clamped = Math.Clamp(value, 3, 32);
            if (_rectanglePolygonSides == clamped)
            {
                return;
            }

            _rectanglePolygonSides = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RectangleSelectionPolygonPoints));
        }
    }

    private void RectangleSelectionResetButton_Click(object sender, RoutedEventArgs e)
    {
        ResetRectangleSelection();
    }

    private void RectangleSelectionClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearRectangleSelection();
    }

    private void RectangleSelectionApplyButton_Click(object sender, RoutedEventArgs e)
    {
        TryApplyRectangleSelection();
    }

    private bool TryApplyRectangleSelection()
    {
        if (!CanUseRectangleSelectionTool() ||
            RectangleSelectionVisibility != Visibility.Visible ||
            RectangleSelectionWidth < 4 ||
            RectangleSelectionHeight < 4)
        {
            return false;
        }

        StopRectangleSelectionMove();
        StopRectangleSelectionResize();
        StopRectangleSelectionRotate();
        StopRectangleSelection();
        ClampRectangleSelectionOverlayToImageBounds();
        SyncRectangleSelectionImageRectFromOverlay();
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));

        if (string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase))
        {
            return TryApplyCropSelection();
        }

        return true;
    }

    private bool TryCancelCropSelectionInput()
    {
        if (!string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool hasCropInput =
            RectangleSelectionVisibility == Visibility.Visible ||
            _isRectangleSelectionCreating ||
            _isRectangleSelectionMoving ||
            _isRectangleSelectionResizing ||
            _isRectangleSelectionRotating ||
            Math.Abs(CropRotationAngle) >= 0.01;
        if (!hasCropInput)
        {
            return false;
        }

        StopRectangleSelectionMove();
        StopRectangleSelectionResize();
        StopRectangleSelectionRotate();
        StopRectangleSelection();
        Mouse.Capture(null);
        ClearRectangleSelection();
        CropRotationAngle = 0;
        return true;
    }

    private bool CanUseRectangleSelectionTool()
    {
        return IsCropOrRectangleToolActive &&
               CanUseSinglePreviewTool();
    }

    private BitmapSource GetRectangleSelectionCoordinateSource(PhotoItem photo)
    {
        return string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase)
            ? GetCropSourceBitmapSource(photo)
            : GetCurrentDisplayBitmapSource(photo);
    }

    private double ClampRectangleImageValue(double value, double fallback)
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return fallback;
        }

        BitmapSource source = GetRectangleSelectionCoordinateSource(photo);
        return Math.Clamp(Math.Round(value, 2), 0, Math.Max(source.PixelWidth, source.PixelHeight));
    }

    private void StartRectangleSelection(System.Windows.Point startPoint)
    {
        _isRectangleSelectionCreating = true;
        _rectangleSelectionStartPoint = ClampPointToPreviewImage(startPoint);
        RectangleSelectionLeft = _rectangleSelectionStartPoint.X;
        RectangleSelectionTop = _rectangleSelectionStartPoint.Y;
        RectangleSelectionWidth = 0;
        RectangleSelectionHeight = 0;
        RectangleSelectionVisibility = Visibility.Visible;
        SyncRectangleSelectionImageRectFromOverlay();
        PreviewSurface.Cursor = System.Windows.Input.Cursors.Cross;
        Mouse.Capture(PreviewSurface);
    }

    private void UpdateRectangleSelection(System.Windows.Point currentPoint)
    {
        System.Windows.Point clampedPoint = ClampPointToPreviewImage(currentPoint);
        if (TryGetCropAspectRatio(out double aspectRatio))
        {
            UpdateConstrainedRectangleSelection(_rectangleSelectionStartPoint, clampedPoint, aspectRatio);
            return;
        }

        double left = Math.Min(_rectangleSelectionStartPoint.X, clampedPoint.X);
        double top = Math.Min(_rectangleSelectionStartPoint.Y, clampedPoint.Y);
        double right = Math.Max(_rectangleSelectionStartPoint.X, clampedPoint.X);
        double bottom = Math.Max(_rectangleSelectionStartPoint.Y, clampedPoint.Y);

        RectangleSelectionLeft = left;
        RectangleSelectionTop = top;
        RectangleSelectionWidth = Math.Max(4, right - left);
        RectangleSelectionHeight = Math.Max(4, bottom - top);
        ClampRectangleSelectionOverlayToImageBounds();
        SyncRectangleSelectionImageRectFromOverlay();
    }

    private void StartRectangleSelectionMove(System.Windows.Point startPoint)
    {
        _isRectangleSelectionMoving = true;
        _rectangleSelectionMoveStartPoint = startPoint;
        _rectangleSelectionStartLeft = RectangleSelectionLeft;
        _rectangleSelectionStartTop = RectangleSelectionTop;
        PreviewSurface.Cursor = System.Windows.Input.Cursors.SizeAll;
        Mouse.Capture(PreviewSurface);
    }

    private void StartRectangleSelectionRotate(System.Windows.Point startPoint)
    {
        _isRectangleSelectionRotating = true;
        _rectangleSelectionRotateStartAngle = GetRectangleSelectionPointerAngle(startPoint);
        _rectangleSelectionRotateStartRotationAngle = CropRotationAngle;
        HideCropRotateCursor();
        PreviewSurface.Cursor = GetCropRotateCursor();
        Mouse.Capture(PreviewSurface);
    }

    private void MoveRectangleSelection(System.Windows.Point currentPoint)
    {
        Vector delta = currentPoint - _rectangleSelectionMoveStartPoint;
        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        double maxLeft = imageRight - RectangleSelectionWidth;
        double maxTop = imageBottom - RectangleSelectionHeight;

        RectangleSelectionLeft = Math.Clamp(_rectangleSelectionStartLeft + delta.X, imageLeft, Math.Max(imageLeft, maxLeft));
        RectangleSelectionTop = Math.Clamp(_rectangleSelectionStartTop + delta.Y, imageTop, Math.Max(imageTop, maxTop));
        SyncRectangleSelectionImageRectFromOverlay();
    }

    private void StartRectangleSelectionResize(System.Windows.Point startPoint, RectangleSelectionHitZone hitZone)
    {
        _isRectangleSelectionResizing = true;
        _rectangleSelectionResizeHitZone = hitZone;
        _rectangleSelectionMoveStartPoint = startPoint;
        _rectangleSelectionStartLeft = RectangleSelectionLeft;
        _rectangleSelectionStartTop = RectangleSelectionTop;
        _rectangleSelectionStartWidth = RectangleSelectionWidth;
        _rectangleSelectionStartHeight = RectangleSelectionHeight;
        PreviewSurface.Cursor = GetCursorForRectangleHitZone(hitZone);
        Mouse.Capture(PreviewSurface);
    }

    private void ResizeRectangleSelection(System.Windows.Point currentPoint)
    {
        if (TryGetCropAspectRatio(out double aspectRatio))
        {
            ResizeConstrainedRectangleSelection(currentPoint, aspectRatio);
            return;
        }

        Vector delta = currentPoint - _rectangleSelectionMoveStartPoint;
        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        const double minSize = 4;

        double left = _rectangleSelectionStartLeft;
        double top = _rectangleSelectionStartTop;
        double right = _rectangleSelectionStartLeft + _rectangleSelectionStartWidth;
        double bottom = _rectangleSelectionStartTop + _rectangleSelectionStartHeight;

        switch (_rectangleSelectionResizeHitZone)
        {
            case RectangleSelectionHitZone.Left:
            case RectangleSelectionHitZone.TopLeft:
            case RectangleSelectionHitZone.BottomLeft:
                left = Math.Clamp(_rectangleSelectionStartLeft + delta.X, imageLeft, right - minSize);
                break;
            case RectangleSelectionHitZone.Right:
            case RectangleSelectionHitZone.TopRight:
            case RectangleSelectionHitZone.BottomRight:
                right = Math.Clamp(_rectangleSelectionStartLeft + _rectangleSelectionStartWidth + delta.X, left + minSize, imageRight);
                break;
        }

        switch (_rectangleSelectionResizeHitZone)
        {
            case RectangleSelectionHitZone.Top:
            case RectangleSelectionHitZone.TopLeft:
            case RectangleSelectionHitZone.TopRight:
                top = Math.Clamp(_rectangleSelectionStartTop + delta.Y, imageTop, bottom - minSize);
                break;
            case RectangleSelectionHitZone.Bottom:
            case RectangleSelectionHitZone.BottomLeft:
            case RectangleSelectionHitZone.BottomRight:
                bottom = Math.Clamp(_rectangleSelectionStartTop + _rectangleSelectionStartHeight + delta.Y, top + minSize, imageBottom);
                break;
        }

        RectangleSelectionLeft = left;
        RectangleSelectionTop = top;
        RectangleSelectionWidth = Math.Max(minSize, right - left);
        RectangleSelectionHeight = Math.Max(minSize, bottom - top);
        ClampRectangleSelectionOverlayToImageBounds();
        SyncRectangleSelectionImageRectFromOverlay();
    }

    private void UpdateConstrainedRectangleSelection(WpfPoint anchorPoint, WpfPoint currentPoint, double aspectRatio)
    {
        double deltaX = currentPoint.X - anchorPoint.X;
        double deltaY = currentPoint.Y - anchorPoint.Y;
        bool dragToRight = deltaX >= 0;
        bool dragToBottom = deltaY >= 0;
        double width = Math.Abs(deltaX);
        double height = Math.Abs(deltaY);

        if (width <= 0.01 && height <= 0.01)
        {
            width = 4;
            height = 4;
        }

        FitSizeToAspectRatio(ref width, ref height, aspectRatio);

        RectangleSelectionLeft = dragToRight ? anchorPoint.X : anchorPoint.X - width;
        RectangleSelectionTop = dragToBottom ? anchorPoint.Y : anchorPoint.Y - height;
        RectangleSelectionWidth = width;
        RectangleSelectionHeight = height;
        ClampRectangleSelectionOverlayToImageBounds();
        SyncRectangleSelectionImageRectFromOverlay();
    }

    private void ResizeConstrainedRectangleSelection(WpfPoint currentPoint, double aspectRatio)
    {
        Vector delta = currentPoint - _rectangleSelectionMoveStartPoint;
        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;
        WpfPoint selectionCenter = new(
            _rectangleSelectionStartLeft + (_rectangleSelectionStartWidth * 0.5),
            _rectangleSelectionStartTop + (_rectangleSelectionStartHeight * 0.5));
        double left = _rectangleSelectionStartLeft;
        double top = _rectangleSelectionStartTop;
        double right = _rectangleSelectionStartLeft + _rectangleSelectionStartWidth;
        double bottom = _rectangleSelectionStartTop + _rectangleSelectionStartHeight;

        switch (_rectangleSelectionResizeHitZone)
        {
            case RectangleSelectionHitZone.Left:
            {
                double width = right - Math.Clamp(_rectangleSelectionStartLeft + delta.X, imageLeft, right - 4);
                double height = width / aspectRatio;
                FitSizeToAspectRatio(ref width, ref height, aspectRatio, preferWidth: true);
                left = right - width;
                top = selectionCenter.Y - (height * 0.5);
                bottom = selectionCenter.Y + (height * 0.5);
                break;
            }
            case RectangleSelectionHitZone.Right:
            {
                double width = Math.Clamp(right + delta.X, left + 4, imageRight) - left;
                double height = width / aspectRatio;
                FitSizeToAspectRatio(ref width, ref height, aspectRatio, preferWidth: true);
                right = left + width;
                top = selectionCenter.Y - (height * 0.5);
                bottom = selectionCenter.Y + (height * 0.5);
                break;
            }
            case RectangleSelectionHitZone.Top:
            {
                double height = bottom - Math.Clamp(_rectangleSelectionStartTop + delta.Y, imageTop, bottom - 4);
                double width = height * aspectRatio;
                FitSizeToAspectRatio(ref width, ref height, aspectRatio, preferWidth: false);
                top = bottom - height;
                left = selectionCenter.X - (width * 0.5);
                right = selectionCenter.X + (width * 0.5);
                break;
            }
            case RectangleSelectionHitZone.Bottom:
            {
                double height = Math.Clamp(bottom + delta.Y, top + 4, imageBottom) - top;
                double width = height * aspectRatio;
                FitSizeToAspectRatio(ref width, ref height, aspectRatio, preferWidth: false);
                bottom = top + height;
                left = selectionCenter.X - (width * 0.5);
                right = selectionCenter.X + (width * 0.5);
                break;
            }
            case RectangleSelectionHitZone.TopLeft:
            {
                WpfPoint anchor = new(right, bottom);
                WpfPoint target = new(
                    Math.Clamp(currentPoint.X, imageLeft, right - 4),
                    Math.Clamp(currentPoint.Y, imageTop, bottom - 4));
                ApplyConstrainedCornerResize(anchor, target, aspectRatio, false, false, out left, out top, out right, out bottom);
                break;
            }
            case RectangleSelectionHitZone.TopRight:
            {
                WpfPoint anchor = new(left, bottom);
                WpfPoint target = new(
                    Math.Clamp(currentPoint.X, left + 4, imageRight),
                    Math.Clamp(currentPoint.Y, imageTop, bottom - 4));
                ApplyConstrainedCornerResize(anchor, target, aspectRatio, true, false, out left, out top, out right, out bottom);
                break;
            }
            case RectangleSelectionHitZone.BottomLeft:
            {
                WpfPoint anchor = new(right, top);
                WpfPoint target = new(
                    Math.Clamp(currentPoint.X, imageLeft, right - 4),
                    Math.Clamp(currentPoint.Y, top + 4, imageBottom));
                ApplyConstrainedCornerResize(anchor, target, aspectRatio, false, true, out left, out top, out right, out bottom);
                break;
            }
            case RectangleSelectionHitZone.BottomRight:
            {
                WpfPoint anchor = new(left, top);
                WpfPoint target = new(
                    Math.Clamp(currentPoint.X, left + 4, imageRight),
                    Math.Clamp(currentPoint.Y, top + 4, imageBottom));
                ApplyConstrainedCornerResize(anchor, target, aspectRatio, true, true, out left, out top, out right, out bottom);
                break;
            }
        }

        RectangleSelectionLeft = left;
        RectangleSelectionTop = top;
        RectangleSelectionWidth = Math.Max(4, right - left);
        RectangleSelectionHeight = Math.Max(4, bottom - top);
        ClampRectangleSelectionOverlayToImageBounds();
        SyncRectangleSelectionImageRectFromOverlay();
    }

    private void ApplyConstrainedCornerResize(
        WpfPoint anchorPoint,
        WpfPoint targetPoint,
        double aspectRatio,
        bool expandRight,
        bool expandBottom,
        out double left,
        out double top,
        out double right,
        out double bottom)
    {
        double width = Math.Abs(targetPoint.X - anchorPoint.X);
        double height = Math.Abs(targetPoint.Y - anchorPoint.Y);
        FitSizeToAspectRatio(ref width, ref height, aspectRatio);

        if (expandRight)
        {
            left = anchorPoint.X;
            right = anchorPoint.X + width;
        }
        else
        {
            left = anchorPoint.X - width;
            right = anchorPoint.X;
        }

        if (expandBottom)
        {
            top = anchorPoint.Y;
            bottom = anchorPoint.Y + height;
        }
        else
        {
            top = anchorPoint.Y - height;
            bottom = anchorPoint.Y;
        }
    }

    private void FitSizeToAspectRatio(ref double width, ref double height, double aspectRatio, bool? preferWidth = null)
    {
        width = Math.Max(0.01, width);
        height = Math.Max(0.01, height);

        bool useWidth = preferWidth ?? (width >= height * aspectRatio);
        if (useWidth)
        {
            height = width / aspectRatio;
        }
        else
        {
            width = height * aspectRatio;
        }

        double scale = Math.Max(4 / width, 4 / height);
        if (scale > 1)
        {
            width *= scale;
            height *= scale;
        }
    }

    private void RotateRectangleSelection(System.Windows.Point currentPoint)
    {
        double currentAngle = GetRectangleSelectionPointerAngle(currentPoint);
        double nextAngle = _rectangleSelectionRotateStartRotationAngle + (currentAngle - _rectangleSelectionRotateStartAngle);
        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
        {
            nextAngle = Math.Round(nextAngle / CropRotateSnapAngle) * CropRotateSnapAngle;
        }

        CropRotationAngle = nextAngle;
        HideCropRotateCursor();
        PreviewSurface.Cursor = GetCropRotateCursor();
    }

    private void StopRectangleSelectionMove()
    {
        if (!_isRectangleSelectionMoving)
        {
            return;
        }

        _isRectangleSelectionMoving = false;
        Mouse.Capture(null);
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void StopRectangleSelectionResize()
    {
        if (!_isRectangleSelectionResizing)
        {
            return;
        }

        _isRectangleSelectionResizing = false;
        _rectangleSelectionResizeHitZone = RectangleSelectionHitZone.None;
        Mouse.Capture(null);
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void StopRectangleSelectionRotate()
    {
        if (!_isRectangleSelectionRotating)
        {
            return;
        }

        _isRectangleSelectionRotating = false;
        HideCropRotateCursor();
        Mouse.Capture(null);
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void StopRectangleSelection()
    {
        if (!_isRectangleSelectionCreating)
        {
            return;
        }

        _isRectangleSelectionCreating = false;
        Mouse.Capture(null);
        UpdateRectangleSelectionHoverCursor(Mouse.GetPosition(PreviewSurface));
    }

    private void UpdateRectangleSelectionHoverCursor(System.Windows.Point point)
    {
        if (!CanUseRectangleSelectionTool() ||
            _isRectangleSelectionCreating ||
            _isRectangleSelectionMoving ||
            _isRectangleSelectionResizing ||
            _isRectangleSelectionRotating ||
            SelectedPreviewPhotos.Count != 1)
        {
            return;
        }

        RectangleSelectionHitZone hitZone = GetRectangleSelectionHitZone(point);
        if (hitZone is RectangleSelectionHitZone.RotateTopLeft or
            RectangleSelectionHitZone.RotateTopRight or
            RectangleSelectionHitZone.RotateBottomLeft or
            RectangleSelectionHitZone.RotateBottomRight)
        {
            HideCropRotateCursor();
            PreviewSurface.Cursor = GetCropRotateCursor();
            return;
        }

        HideCropRotateCursor();
        PreviewSurface.Cursor = GetCursorForRectangleHitZone(hitZone);
    }

    private RectangleSelectionHitZone GetRectangleSelectionHitZone(System.Windows.Point point)
    {
        if (RectangleSelectionVisibility != Visibility.Visible || RectangleSelectionWidth < 4 || RectangleSelectionHeight < 4)
        {
            return RectangleSelectionHitZone.None;
        }

        WpfPoint hitPoint = point;
        if (string.Equals(ActiveToolId, "crop", StringComparison.OrdinalIgnoreCase))
        {
            hitPoint = RotatePoint(point, GetRectangleSelectionCenter(), -RectangleSelectionRotationAngle);
            RectangleSelectionHitZone outsideRotateHitZone = GetCropOutsideRotateHitZone(hitPoint);
            if (outsideRotateHitZone != RectangleSelectionHitZone.None)
            {
                return outsideRotateHitZone;
            }
        }

        return GetAxisAlignedRectangleSelectionHitZone(hitPoint, RectangleSelectionHitTolerance);
    }

    private RectangleSelectionHitZone GetAxisAlignedRectangleSelectionHitZone(WpfPoint point, double hitTolerance)
    {
        double left = RectangleSelectionLeft;
        double top = RectangleSelectionTop;
        double right = left + RectangleSelectionWidth;
        double bottom = top + RectangleSelectionHeight;

        bool nearLeft = Math.Abs(point.X - left) <= hitTolerance;
        bool nearRight = Math.Abs(point.X - right) <= hitTolerance;
        bool nearTop = Math.Abs(point.Y - top) <= hitTolerance;
        bool nearBottom = Math.Abs(point.Y - bottom) <= hitTolerance;
        bool insideX = point.X >= left && point.X <= right;
        bool insideY = point.Y >= top && point.Y <= bottom;
        bool insideExpanded = point.X >= left - hitTolerance &&
                              point.X <= right + hitTolerance &&
                              point.Y >= top - hitTolerance &&
                              point.Y <= bottom + hitTolerance;
        if (!insideExpanded)
        {
            return RectangleSelectionHitZone.None;
        }

        if (nearLeft && nearTop) return RectangleSelectionHitZone.TopLeft;
        if (nearRight && nearTop) return RectangleSelectionHitZone.TopRight;
        if (nearLeft && nearBottom) return RectangleSelectionHitZone.BottomLeft;
        if (nearRight && nearBottom) return RectangleSelectionHitZone.BottomRight;
        if (nearLeft && insideY) return RectangleSelectionHitZone.Left;
        if (nearRight && insideY) return RectangleSelectionHitZone.Right;
        if (nearTop && insideX) return RectangleSelectionHitZone.Top;
        if (nearBottom && insideX) return RectangleSelectionHitZone.Bottom;
        if (insideX && insideY) return RectangleSelectionHitZone.Inside;

        return RectangleSelectionHitZone.None;
    }

    private System.Windows.Input.Cursor GetCursorForRectangleHitZone(RectangleSelectionHitZone hitZone)
    {
        return hitZone switch
        {
            RectangleSelectionHitZone.Inside => System.Windows.Input.Cursors.SizeAll,
            RectangleSelectionHitZone.Left or RectangleSelectionHitZone.Right => System.Windows.Input.Cursors.SizeWE,
            RectangleSelectionHitZone.Top or RectangleSelectionHitZone.Bottom => System.Windows.Input.Cursors.SizeNS,
            RectangleSelectionHitZone.TopLeft or RectangleSelectionHitZone.BottomRight => System.Windows.Input.Cursors.SizeNWSE,
            RectangleSelectionHitZone.TopRight or RectangleSelectionHitZone.BottomLeft => System.Windows.Input.Cursors.SizeNESW,
            RectangleSelectionHitZone.RotateTopLeft or
            RectangleSelectionHitZone.RotateTopRight or
            RectangleSelectionHitZone.RotateBottomLeft or
            RectangleSelectionHitZone.RotateBottomRight => GetCropRotateCursor(),
            _ => System.Windows.Input.Cursors.Cross
        };
    }

    private static System.Windows.Input.Cursor GetCropRotateCursor()
    {
        return System.Windows.Input.Cursors.SizeWE;
    }

    private void ResetRectangleSelection()
    {
        if (SelectedPhoto is not PhotoItem photo)
        {
            return;
        }

        BitmapSource source = GetRectangleSelectionCoordinateSource(photo);
        if (source.PixelWidth <= 0 || source.PixelHeight <= 0)
        {
            return;
        }

        double width = Math.Min(source.PixelWidth, Math.Max(80, source.PixelWidth * 0.5));
        double height = Math.Min(source.PixelHeight, Math.Max(80, source.PixelHeight * 0.5));
        RectangleSelectionImageX = Math.Max(0, (source.PixelWidth - width) * 0.5);
        RectangleSelectionImageY = Math.Max(0, (source.PixelHeight - height) * 0.5);
        RectangleSelectionImageWidth = width;
        RectangleSelectionImageHeight = height;
        RectangleSelectionVisibility = Visibility.Visible;
        UpdateRectangleSelectionOverlayFromImageRect();
    }

    private void ClearRectangleSelection()
    {
        _isRectangleSelectionCreating = false;
        _isRectangleSelectionMoving = false;
        _isRectangleSelectionResizing = false;
        _isRectangleSelectionRotating = false;
        _rectangleSelectionResizeHitZone = RectangleSelectionHitZone.None;
        RectangleSelectionLeft = 0;
        RectangleSelectionTop = 0;
        RectangleSelectionWidth = 0;
        RectangleSelectionHeight = 0;
        RectangleSelectionVisibility = Visibility.Collapsed;
        _rectangleSelectionImageX = 0;
        _rectangleSelectionImageY = 0;
        _rectangleSelectionImageWidth = 0;
        _rectangleSelectionImageHeight = 0;
        OnPropertyChanged(nameof(RectangleSelectionImageX));
        OnPropertyChanged(nameof(RectangleSelectionImageY));
        OnPropertyChanged(nameof(RectangleSelectionImageWidth));
        OnPropertyChanged(nameof(RectangleSelectionImageHeight));
        OnPropertyChanged(nameof(SinglePreviewImageSource));
        HideCropRotateCursor();
        RaiseCropTelemetryPropertyChanged();
        PreviewSurface.Cursor = null;
    }

    private RectangleSelectionHitZone GetCropOutsideRotateHitZone(WpfPoint localPoint)
    {
        if (CropRotateHandleVisibility != Visibility.Visible)
        {
            return RectangleSelectionHitZone.None;
        }

        double left = RectangleSelectionLeft;
        double top = RectangleSelectionTop;
        double right = left + RectangleSelectionWidth;
        double bottom = top + RectangleSelectionHeight;

        if (localPoint.X < left &&
            localPoint.Y < top &&
            IsPointNearCorner(localPoint, new WpfPoint(left, top), CropRotateOutsideHitRadius))
        {
            return RectangleSelectionHitZone.RotateTopLeft;
        }

        if (localPoint.X > right &&
            localPoint.Y < top &&
            IsPointNearCorner(localPoint, new WpfPoint(right, top), CropRotateOutsideHitRadius))
        {
            return RectangleSelectionHitZone.RotateTopRight;
        }

        if (localPoint.X > right &&
            localPoint.Y > bottom &&
            IsPointNearCorner(localPoint, new WpfPoint(right, bottom), CropRotateOutsideHitRadius))
        {
            return RectangleSelectionHitZone.RotateBottomRight;
        }

        if (localPoint.X < left &&
            localPoint.Y > bottom &&
            IsPointNearCorner(localPoint, new WpfPoint(left, bottom), CropRotateOutsideHitRadius))
        {
            return RectangleSelectionHitZone.RotateBottomLeft;
        }

        return RectangleSelectionHitZone.None;
    }

    private static bool IsPointNearCorner(WpfPoint point, WpfPoint corner, double radius)
    {
        return (point - corner).Length <= radius;
    }

    private WpfPoint[] GetCropSelectionCornerPoints()
    {
        WpfPoint topLeft = new(RectangleSelectionLeft, RectangleSelectionTop);
        WpfPoint topRight = new(RectangleSelectionLeft + RectangleSelectionWidth, RectangleSelectionTop);
        WpfPoint bottomRight = new(RectangleSelectionLeft + RectangleSelectionWidth, RectangleSelectionTop + RectangleSelectionHeight);
        WpfPoint bottomLeft = new(RectangleSelectionLeft, RectangleSelectionTop + RectangleSelectionHeight);
        if (Math.Abs(RectangleSelectionRotationAngle) < 0.01)
        {
            return [topLeft, topRight, bottomRight, bottomLeft];
        }

        WpfPoint center = GetRectangleSelectionCenter();
        return
        [
            RotatePoint(topLeft, center, RectangleSelectionRotationAngle),
            RotatePoint(topRight, center, RectangleSelectionRotationAngle),
            RotatePoint(bottomRight, center, RectangleSelectionRotationAngle),
            RotatePoint(bottomLeft, center, RectangleSelectionRotationAngle)
        ];
    }

    private WpfPoint GetRectangleSelectionCenter()
    {
        return new(
            RectangleSelectionLeft + (RectangleSelectionWidth * 0.5),
            RectangleSelectionTop + (RectangleSelectionHeight * 0.5));
    }

    private double GetRectangleSelectionPointerAngle(WpfPoint point)
    {
        WpfPoint center = GetRectangleSelectionCenter();
        return Math.Atan2(point.Y - center.Y, point.X - center.X) * 180.0 / Math.PI;
    }

    private double GetCropRotateHandleLeft(WpfPoint centerPoint)
    {
        return centerPoint.X - (CropRotateHandleSize * 0.5);
    }

    private double GetCropRotateHandleTop(WpfPoint centerPoint)
    {
        return centerPoint.Y - (CropRotateHandleSize * 0.5);
    }

    private double GetCropRotateIconLeft(WpfPoint centerPoint)
    {
        return centerPoint.X - (CropRotateIconSize * 0.5);
    }

    private double GetCropRotateIconTop(WpfPoint centerPoint)
    {
        return centerPoint.Y - (CropRotateIconSize * 0.5);
    }

    private void RaiseRectangleSelectionHandlePropertyChanged()
    {
        OnPropertyChanged(nameof(CropOutlineSelectionVisibility));
        OnPropertyChanged(nameof(RectangleOutlineSelectionVisibility));
        OnPropertyChanged(nameof(CropOutsideOverlayVisibility));
        OnPropertyChanged(nameof(CropOutsideSelectionGeometry));
        OnPropertyChanged(nameof(CropRotateHandleVisibility));
        OnPropertyChanged(nameof(CropRotateCursorVisibility));
        OnPropertyChanged(nameof(CropRotateCursorLeft));
        OnPropertyChanged(nameof(CropRotateCursorTop));
        OnPropertyChanged(nameof(CropRotateTopLeftHandleLeft));
        OnPropertyChanged(nameof(CropRotateTopLeftHandleTop));
        OnPropertyChanged(nameof(CropRotateTopLeftIconLeft));
        OnPropertyChanged(nameof(CropRotateTopLeftIconTop));
        OnPropertyChanged(nameof(CropRotateTopRightHandleLeft));
        OnPropertyChanged(nameof(CropRotateTopRightHandleTop));
        OnPropertyChanged(nameof(CropRotateTopRightIconLeft));
        OnPropertyChanged(nameof(CropRotateTopRightIconTop));
        OnPropertyChanged(nameof(CropRotateBottomRightHandleLeft));
        OnPropertyChanged(nameof(CropRotateBottomRightHandleTop));
        OnPropertyChanged(nameof(CropRotateBottomRightIconLeft));
        OnPropertyChanged(nameof(CropRotateBottomRightIconTop));
        OnPropertyChanged(nameof(CropRotateBottomLeftHandleLeft));
        OnPropertyChanged(nameof(CropRotateBottomLeftHandleTop));
        OnPropertyChanged(nameof(CropRotateBottomLeftIconLeft));
        OnPropertyChanged(nameof(CropRotateBottomLeftIconTop));
        OnPropertyChanged(nameof(CropCenterGuideVisibility));
        OnPropertyChanged(nameof(CropCenterGuideCenterX));
        OnPropertyChanged(nameof(CropCenterGuideCenterY));
        OnPropertyChanged(nameof(CropCenterGuideHorizontalX1));
        OnPropertyChanged(nameof(CropCenterGuideHorizontalX2));
        OnPropertyChanged(nameof(CropCenterGuideVerticalY1));
        OnPropertyChanged(nameof(CropCenterGuideVerticalY2));
    }

    private void ShowCropRotateCursor(WpfPoint point)
    {
        CropRotateCursorLeft = point.X + (CropRotateCursorSize * 0.5);
        CropRotateCursorTop = point.Y + (CropRotateCursorSize * 0.5);
        if (_cropRotateCursorVisibility != Visibility.Visible)
        {
            _cropRotateCursorVisibility = Visibility.Visible;
            OnPropertyChanged(nameof(CropRotateCursorVisibility));
        }
    }

    private void HideCropRotateCursor()
    {
        if (_cropRotateCursorVisibility != Visibility.Collapsed)
        {
            _cropRotateCursorVisibility = Visibility.Collapsed;
            OnPropertyChanged(nameof(CropRotateCursorVisibility));
        }
    }

    private void SyncRectangleSelectionImageRectFromOverlay()
    {
        if (SelectedPhoto is not PhotoItem photo ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            return;
        }

        BitmapSource source = GetRectangleSelectionCoordinateSource(photo);
        double scaleX = source.PixelWidth / PreviewImageWidth;
        double scaleY = source.PixelHeight / PreviewImageHeight;

        if (IsCropImageRotationActive)
        {
            WpfPoint screenCenter = new(
                RectangleSelectionLeft + (RectangleSelectionWidth * 0.5),
                RectangleSelectionTop + (RectangleSelectionHeight * 0.5));
            WpfPoint unrotatedCenter = RotatePoint(screenCenter, GetPreviewImageCenter(), -CropRotationAngle);
            double imageWidth = Math.Clamp(RectangleSelectionWidth * scaleX, 0, source.PixelWidth);
            double imageHeight = Math.Clamp(RectangleSelectionHeight * scaleY, 0, source.PixelHeight);
            double imageCenterX = (unrotatedCenter.X - PreviewImageLeft) * scaleX;
            double imageCenterY = (unrotatedCenter.Y - PreviewImageTop) * scaleY;
            _rectangleSelectionImageX = Math.Clamp(imageCenterX - (imageWidth * 0.5), 0, Math.Max(0, source.PixelWidth - imageWidth));
            _rectangleSelectionImageY = Math.Clamp(imageCenterY - (imageHeight * 0.5), 0, Math.Max(0, source.PixelHeight - imageHeight));
            _rectangleSelectionImageWidth = imageWidth;
            _rectangleSelectionImageHeight = imageHeight;
        }
        else
        {
            _rectangleSelectionImageX = Math.Clamp((RectangleSelectionLeft - PreviewImageLeft) * scaleX, 0, source.PixelWidth);
            _rectangleSelectionImageY = Math.Clamp((RectangleSelectionTop - PreviewImageTop) * scaleY, 0, source.PixelHeight);
            _rectangleSelectionImageWidth = Math.Clamp(RectangleSelectionWidth * scaleX, 0, source.PixelWidth);
            _rectangleSelectionImageHeight = Math.Clamp(RectangleSelectionHeight * scaleY, 0, source.PixelHeight);
        }
        OnPropertyChanged(nameof(RectangleSelectionImageX));
        OnPropertyChanged(nameof(RectangleSelectionImageY));
        OnPropertyChanged(nameof(RectangleSelectionImageWidth));
        OnPropertyChanged(nameof(RectangleSelectionImageHeight));
        RaiseCropTelemetryPropertyChanged();
    }

    private void UpdateRectangleSelectionOverlayFromImageRect()
    {
        if (SelectedPhoto is not PhotoItem photo ||
            PreviewImageWidth <= 0 ||
            PreviewImageHeight <= 0)
        {
            RectangleSelectionVisibility = Visibility.Collapsed;
            return;
        }

        if (RectangleSelectionImageWidth <= 0 || RectangleSelectionImageHeight <= 0)
        {
            RectangleSelectionVisibility = Visibility.Collapsed;
            return;
        }

        BitmapSource source = GetRectangleSelectionCoordinateSource(photo);
        double clampedX = Math.Clamp(RectangleSelectionImageX, 0, source.PixelWidth);
        double clampedY = Math.Clamp(RectangleSelectionImageY, 0, source.PixelHeight);
        double clampedWidth = Math.Clamp(RectangleSelectionImageWidth, 0, Math.Max(0, source.PixelWidth - clampedX));
        double clampedHeight = Math.Clamp(RectangleSelectionImageHeight, 0, Math.Max(0, source.PixelHeight - clampedY));
        if (Math.Abs(_rectangleSelectionImageX - clampedX) > 0.01 ||
            Math.Abs(_rectangleSelectionImageY - clampedY) > 0.01 ||
            Math.Abs(_rectangleSelectionImageWidth - clampedWidth) > 0.01 ||
            Math.Abs(_rectangleSelectionImageHeight - clampedHeight) > 0.01)
        {
            _rectangleSelectionImageX = clampedX;
            _rectangleSelectionImageY = clampedY;
            _rectangleSelectionImageWidth = clampedWidth;
            _rectangleSelectionImageHeight = clampedHeight;
            OnPropertyChanged(nameof(RectangleSelectionImageX));
            OnPropertyChanged(nameof(RectangleSelectionImageY));
            OnPropertyChanged(nameof(RectangleSelectionImageWidth));
            OnPropertyChanged(nameof(RectangleSelectionImageHeight));
            RaiseCropTelemetryPropertyChanged();
        }

        double scaleX = PreviewImageWidth / source.PixelWidth;
        double scaleY = PreviewImageHeight / source.PixelHeight;

        if (IsCropImageRotationActive)
        {
            double imageCenterX = _rectangleSelectionImageX + (_rectangleSelectionImageWidth * 0.5);
            double imageCenterY = _rectangleSelectionImageY + (_rectangleSelectionImageHeight * 0.5);
            WpfPoint unrotatedCenter = new(
                PreviewImageLeft + (imageCenterX * scaleX),
                PreviewImageTop + (imageCenterY * scaleY));
            WpfPoint screenCenter = RotatePoint(unrotatedCenter, GetPreviewImageCenter(), CropRotationAngle);
            RectangleSelectionWidth = Math.Max(4, _rectangleSelectionImageWidth * scaleX);
            RectangleSelectionHeight = Math.Max(4, _rectangleSelectionImageHeight * scaleY);
            RectangleSelectionLeft = screenCenter.X - (RectangleSelectionWidth * 0.5);
            RectangleSelectionTop = screenCenter.Y - (RectangleSelectionHeight * 0.5);
        }
        else
        {
            RectangleSelectionLeft = PreviewImageLeft + (_rectangleSelectionImageX * scaleX);
            RectangleSelectionTop = PreviewImageTop + (_rectangleSelectionImageY * scaleY);
            RectangleSelectionWidth = Math.Max(4, _rectangleSelectionImageWidth * scaleX);
            RectangleSelectionHeight = Math.Max(4, _rectangleSelectionImageHeight * scaleY);
        }

        ClampRectangleSelectionOverlayToImageBounds();
        RectangleSelectionVisibility = Visibility.Visible;
    }

    private void ClampRectangleSelectionOverlayToImageBounds()
    {
        if (PreviewImageWidth <= 0 || PreviewImageHeight <= 0)
        {
            return;
        }

        if (IsCropImageRotationActive &&
            SelectedPhoto is PhotoItem photo)
        {
            BitmapSource source = GetRectangleSelectionCoordinateSource(photo);
            double previewScaleX = PreviewImageWidth / source.PixelWidth;
            double previewScaleY = PreviewImageHeight / source.PixelHeight;
            double imageScaleX = source.PixelWidth / PreviewImageWidth;
            double imageScaleY = source.PixelHeight / PreviewImageHeight;
            double imageWidth = Math.Clamp(RectangleSelectionWidth * imageScaleX, 4, source.PixelWidth);
            double imageHeight = Math.Clamp(RectangleSelectionHeight * imageScaleY, 4, source.PixelHeight);
            double minCenterX = imageWidth * 0.5;
            double maxCenterX = Math.Max(minCenterX, source.PixelWidth - (imageWidth * 0.5));
            double minCenterY = imageHeight * 0.5;
            double maxCenterY = Math.Max(minCenterY, source.PixelHeight - (imageHeight * 0.5));
            WpfPoint screenCenter = new(
                RectangleSelectionLeft + (RectangleSelectionWidth * 0.5),
                RectangleSelectionTop + (RectangleSelectionHeight * 0.5));
            WpfPoint unrotatedCenter = RotatePoint(screenCenter, GetPreviewImageCenter(), -CropRotationAngle);
            double centerImageX = Math.Clamp((unrotatedCenter.X - PreviewImageLeft) * imageScaleX, minCenterX, maxCenterX);
            double centerImageY = Math.Clamp((unrotatedCenter.Y - PreviewImageTop) * imageScaleY, minCenterY, maxCenterY);
            WpfPoint clampedUnrotatedCenter = new(
                PreviewImageLeft + (centerImageX * previewScaleX),
                PreviewImageTop + (centerImageY * previewScaleY));
            WpfPoint clampedScreenCenter = RotatePoint(clampedUnrotatedCenter, GetPreviewImageCenter(), CropRotationAngle);

            RectangleSelectionWidth = Math.Max(4, imageWidth * previewScaleX);
            RectangleSelectionHeight = Math.Max(4, imageHeight * previewScaleY);
            RectangleSelectionLeft = clampedScreenCenter.X - (RectangleSelectionWidth * 0.5);
            RectangleSelectionTop = clampedScreenCenter.Y - (RectangleSelectionHeight * 0.5);
            return;
        }

        double imageLeft = PreviewImageLeft;
        double imageTop = PreviewImageTop;
        double imageRight = imageLeft + PreviewImageWidth;
        double imageBottom = imageTop + PreviewImageHeight;

        RectangleSelectionWidth = Math.Max(4, Math.Min(RectangleSelectionWidth, PreviewImageWidth));
        RectangleSelectionHeight = Math.Max(4, Math.Min(RectangleSelectionHeight, PreviewImageHeight));
        RectangleSelectionLeft = Math.Clamp(RectangleSelectionLeft, imageLeft, Math.Max(imageLeft, imageRight - RectangleSelectionWidth));
        RectangleSelectionTop = Math.Clamp(RectangleSelectionTop, imageTop, Math.Max(imageTop, imageBottom - RectangleSelectionHeight));
    }
}
