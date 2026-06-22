using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public static class LocalProxyBuilder
{
    public static LocalWorkbenchState BuildDisplayOnlyState(PhotoItem photo, SliderToolSpec toolSpec)
    {
        Int32Rect workArea = toolSpec.ToolId switch
        {
            "double_chin" => DoubleChinWorkAreaBuilder.Build(photo.BaseImage),
            "nose_shape" => NoseWorkAreaBuilder.BuildNoseWorkAreaFallback(photo.BaseImage),
            _ => new Int32Rect(0, 0, photo.BaseImage.PixelWidth, photo.BaseImage.PixelHeight)
        };

        BitmapSource crop = CropBitmap(photo.BaseImage, workArea);
        BitmapSource proxy = ResizeLongSide(crop, GetProxyLongSide(workArea, toolSpec.ProxyPolicy));
        LocalWorkbenchCoordinateMap coordinateMap = CreateCoordinateMap(photo.BaseImage, workArea, proxy);
        LocalWorkbenchMaskSet maskSet = toolSpec.ToolId switch
        {
            "nose_shape" => LocalWorkbenchMaskPreviewBuilder.BuildNosePlaceholderMasks(proxy.PixelWidth, proxy.PixelHeight),
            "double_chin" => LocalWorkbenchMaskPreviewBuilder.BuildDoubleChinPlaceholderMasks(proxy.PixelWidth, proxy.PixelHeight),
            _ => LocalWorkbenchMaskPreviewBuilder.BuildDoubleChinPlaceholderMasks(proxy.PixelWidth, proxy.PixelHeight)
        };
        LocalWorkbenchRequest request = new(
            toolSpec.ToolId,
            toolSpec.Intent,
            photo.Path,
            workArea,
            toolSpec.ProxyPolicy,
            Math.Max(proxy.PixelWidth, proxy.PixelHeight),
            ApplyMaskId: toolSpec.ToolId switch
            {
                "nose_shape" => MaskIds.NoseMask,
                "double_chin" => MaskIds.DoubleChinMask,
                _ => "UnknownApplyMask"
            },
            ProtectMaskIds: toolSpec.ToolId switch
            {
                "nose_shape" => [MaskIds.LeftEyeMask, MaskIds.RightEyeMask, MaskIds.LipMask],
                "double_chin" => [MaskIds.JawlineMask, MaskIds.BeardMask],
                _ => Array.Empty<string>()
            },
            BlockMaskIds: toolSpec.ToolId switch
            {
                "nose_shape" => [MaskIds.GlassesMask, MaskIds.BeardMask],
                "double_chin" => [MaskIds.ClothingMask, MaskIds.AccessoryMask],
                _ => Array.Empty<string>()
            },
            toolSpec.DefaultAmount,
            toolSpec.DefaultAmount);

        return new LocalWorkbenchState(
            request,
            coordinateMap,
            crop,
            proxy,
            maskSet,
            toolSpec.DefaultAmount,
            IsDirty: false,
            IsApplied: false,
            IsCanceled: false);
    }

    private static int GetProxyLongSide(Int32Rect workArea, ProxyPolicy proxyPolicy)
    {
        if (proxyPolicy == ProxyPolicy.FullFrameProxy)
        {
            return 1200;
        }

        int longSide = Math.Max(workArea.Width, workArea.Height);
        return longSide switch
        {
            <= 384 => 384,
            <= 512 => 512,
            <= 768 => 768,
            _ => 1024
        };
    }

    private static BitmapSource CropBitmap(BitmapSource source, Int32Rect rect)
    {
        Int32Rect safeRect = new(
            Math.Clamp(rect.X, 0, Math.Max(0, source.PixelWidth - 1)),
            Math.Clamp(rect.Y, 0, Math.Max(0, source.PixelHeight - 1)),
            Math.Clamp(rect.Width, 1, Math.Max(1, source.PixelWidth - rect.X)),
            Math.Clamp(rect.Height, 1, Math.Max(1, source.PixelHeight - rect.Y)));
        CroppedBitmap crop = new(source, safeRect);
        crop.Freeze();
        return crop;
    }

    private static BitmapSource ResizeLongSide(BitmapSource source, int longSide)
    {
        int sourceLongSide = Math.Max(source.PixelWidth, source.PixelHeight);
        if (sourceLongSide <= 0 || sourceLongSide == longSide)
        {
            return source;
        }

        double scale = (double)longSide / sourceLongSide;
        TransformedBitmap resized = new(source, new ScaleTransform(scale, scale));
        resized.Freeze();
        return resized;
    }

    private static LocalWorkbenchCoordinateMap CreateCoordinateMap(BitmapSource source, Int32Rect workArea, BitmapSource proxy)
    {
        double originalToProxyScaleX = workArea.Width <= 0 ? 1 : (double)proxy.PixelWidth / workArea.Width;
        double originalToProxyScaleY = workArea.Height <= 0 ? 1 : (double)proxy.PixelHeight / workArea.Height;
        return new LocalWorkbenchCoordinateMap(
            new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
            workArea,
            new Int32Rect(0, 0, proxy.PixelWidth, proxy.PixelHeight),
            originalToProxyScaleX,
            originalToProxyScaleY,
            originalToProxyScaleX == 0 ? 1 : 1 / originalToProxyScaleX,
            originalToProxyScaleY == 0 ? 1 : 1 / originalToProxyScaleY);
    }
}
