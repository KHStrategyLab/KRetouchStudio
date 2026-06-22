using System.Windows;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public enum ProxyPolicy
{
    FullFrameProxy,
    LocalWorkAreaProxy
}

public sealed record LocalWorkbenchRequest(
    string ToolId,
    string UserIntent,
    string SourcePhotoId,
    Int32Rect WorkAreaRectOriginal,
    ProxyPolicy ProxyPolicy,
    int ProxyLongSide,
    string ApplyMaskId,
    IReadOnlyList<string> ProtectMaskIds,
    IReadOnlyList<string> BlockMaskIds,
    double InitialAmount,
    double CurrentAmount);

public sealed record LocalWorkbenchState(
    LocalWorkbenchRequest Request,
    LocalWorkbenchCoordinateMap CoordinateMap,
    BitmapSource WorkAreaCropSource,
    BitmapSource LocalProxySource,
    LocalWorkbenchMaskSet LocalMaskSet,
    double CurrentAmount,
    bool IsDirty,
    bool IsApplied,
    bool IsCanceled);

public sealed record LocalWorkbenchMaskSet(
    BitmapSource ApplyMaskOverlay,
    BitmapSource ProtectMaskOverlay,
    BitmapSource BlockMaskOverlay,
    BitmapSource WorkMaskOverlay);

public sealed record LocalWorkbenchCoordinateMap(
    Int32Rect OriginalImageRect,
    Int32Rect WorkAreaRectOriginal,
    Int32Rect LocalProxyRect,
    double OriginalToProxyScaleX,
    double OriginalToProxyScaleY,
    double ProxyToOriginalScaleX,
    double ProxyToOriginalScaleY)
{
    public string ToDisplayText()
    {
        return $"original {OriginalImageRect.Width}x{OriginalImageRect.Height} | " +
            $"work {WorkAreaRectOriginal.X},{WorkAreaRectOriginal.Y} {WorkAreaRectOriginal.Width}x{WorkAreaRectOriginal.Height} | " +
            $"proxy {LocalProxyRect.Width}x{LocalProxyRect.Height} | " +
            $"scale {OriginalToProxyScaleX:0.###},{OriginalToProxyScaleY:0.###}";
    }
}

public sealed record SliderToolSpec(
    string ToolId,
    string DisplayName,
    string Intent,
    ProxyPolicy ProxyPolicy,
    double MinimumAmount,
    double MaximumAmount,
    double DefaultAmount);
