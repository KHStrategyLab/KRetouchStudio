using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp;

namespace KRetouchStudio;

internal static class OpenCvHealingBrushEngine
{
    public static bool TryApplyHealing(
        BitmapSource targetSource,
        BitmapSource sourceSnapshot,
        byte[] maskPixels,
        int maskWidth,
        int maskHeight,
        Vector sourceOffset,
        double hardness,
        double opacity,
        string cloneMode,
        out BitmapSource? result,
        out string? error)
    {
        result = null;
        error = null;

        if (targetSource.PixelWidth != sourceSnapshot.PixelWidth ||
            targetSource.PixelHeight != sourceSnapshot.PixelHeight ||
            targetSource.PixelWidth != maskWidth ||
            targetSource.PixelHeight != maskHeight ||
            maskPixels.Length != maskWidth * maskHeight)
        {
            error = "OpenCV healing input sizes do not match.";
            return false;
        }

        int width = targetSource.PixelWidth;
        int height = targetSource.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            error = "OpenCV healing input image is empty.";
            return false;
        }

        using Mat targetBgra = BitmapSourceToBgraMat(targetSource, out byte[] targetPixels);
        using Mat sourceBgra = BitmapSourceToBgraMat(sourceSnapshot, out _);
        using Mat targetBgr = new();
        using Mat sourceBgr = new();
        Cv2.CvtColor(targetBgra, targetBgr, ColorConversionCodes.BGRA2BGR);
        Cv2.CvtColor(sourceBgra, sourceBgr, ColorConversionCodes.BGRA2BGR);

        using Mat mask = MaskToMat(maskPixels, width, height);
        OpenCvSharp.Rect maskBox = Cv2.BoundingRect(mask);
        if (maskBox.Width <= 0 || maskBox.Height <= 0)
        {
            error = "OpenCV healing mask is empty.";
            return false;
        }

        int padding = Math.Clamp((int)Math.Ceiling(Math.Max(maskBox.Width, maskBox.Height) * 0.35), 20, 240);
        int roiLeft = Math.Max(0, maskBox.X - padding);
        int roiTop = Math.Max(0, maskBox.Y - padding);
        int roiRight = Math.Min(width, maskBox.X + maskBox.Width + padding);
        int roiBottom = Math.Min(height, maskBox.Y + maskBox.Height + padding);
        OpenCvSharp.Rect targetRoiRect = new(roiLeft, roiTop, Math.Max(1, roiRight - roiLeft), Math.Max(1, roiBottom - roiTop));

        int sourceRoiLeft = (int)Math.Round(targetRoiRect.X - sourceOffset.X);
        int sourceRoiTop = (int)Math.Round(targetRoiRect.Y - sourceOffset.Y);
        OpenCvSharp.Rect sourceRoiRect = new(sourceRoiLeft, sourceRoiTop, targetRoiRect.Width, targetRoiRect.Height);
        OpenCvSharp.Rect imageRect = new(0, 0, width, height);
        if (!TryAlignHealingRoisToImageBounds(ref targetRoiRect, ref sourceRoiRect, imageRect))
        {
            error = "OpenCV healing source ROI does not overlap image bounds.";
            return false;
        }

        if (targetRoiRect.Width <= 2 || targetRoiRect.Height <= 2)
        {
            error = "OpenCV healing ROI is too small.";
            return false;
        }

        OpenCvSharp.Point centerInRoi = new(targetRoiRect.Width / 2, targetRoiRect.Height / 2);

        SeamlessCloneFlags cloneFlag = ResolveCloneFlag(cloneMode);
        using Mat targetRoi = targetBgr.SubMat(targetRoiRect);
        using Mat sourceRoi = sourceBgr.SubMat(sourceRoiRect);
        using Mat maskRoi = mask.SubMat(targetRoiRect).Clone();
        Cv2.Rectangle(
            maskRoi,
            new OpenCvSharp.Point(0, 0),
            new OpenCvSharp.Point(maskRoi.Width - 1, maskRoi.Height - 1),
            Scalar.All(0),
            1);

        if (Cv2.CountNonZero(maskRoi) <= 0)
        {
            error = "OpenCV healing mask is empty after ROI border guard.";
            return false;
        }

        using Mat clonedRoi = new();

        try
        {
            Cv2.SeamlessClone(sourceRoi, targetRoi, maskRoi, centerInRoi, clonedRoi, cloneFlag);
        }
        catch (OpenCVException ex)
        {
            error = ex.Message;
            return false;
        }

        using Mat finalRoi = BlendClonedRoi(targetRoi, clonedRoi, maskRoi, hardness, opacity);
        using Mat outputBgr = targetBgr.Clone();
        using Mat outputRoi = outputBgr.SubMat(targetRoiRect);
        finalRoi.CopyTo(outputRoi);
        result = CreateBgraBitmap(outputBgr, targetPixels, targetSource, width, height);
        return true;
    }

    public static bool TryApplySpotHealing(
        BitmapSource targetSource,
        byte[] maskPixels,
        int maskWidth,
        int maskHeight,
        double hardness,
        double opacity,
        out BitmapSource? result,
        out string? error)
    {
        result = null;
        error = null;

        if (targetSource.PixelWidth != maskWidth ||
            targetSource.PixelHeight != maskHeight ||
            maskPixels.Length != maskWidth * maskHeight)
        {
            error = "OpenCV spot healing input sizes do not match.";
            return false;
        }

        int width = targetSource.PixelWidth;
        int height = targetSource.PixelHeight;
        if (width <= 0 || height <= 0)
        {
            error = "OpenCV spot healing input image is empty.";
            return false;
        }

        using Mat targetBgra = BitmapSourceToBgraMat(targetSource, out byte[] targetPixels);
        using Mat targetBgr = new();
        Cv2.CvtColor(targetBgra, targetBgr, ColorConversionCodes.BGRA2BGR);

        using Mat mask = MaskToMat(maskPixels, width, height);
        OpenCvSharp.Rect maskBox = Cv2.BoundingRect(mask);
        if (maskBox.Width <= 0 || maskBox.Height <= 0)
        {
            error = "OpenCV spot healing mask is empty.";
            return false;
        }

        int padding = Math.Clamp((int)Math.Ceiling(Math.Max(maskBox.Width, maskBox.Height) * 0.45), 18, 220);
        int roiLeft = Math.Max(0, maskBox.X - padding);
        int roiTop = Math.Max(0, maskBox.Y - padding);
        int roiRight = Math.Min(width, maskBox.X + maskBox.Width + padding);
        int roiBottom = Math.Min(height, maskBox.Y + maskBox.Height + padding);
        OpenCvSharp.Rect targetRoiRect = new(roiLeft, roiTop, Math.Max(1, roiRight - roiLeft), Math.Max(1, roiBottom - roiTop));
        if (targetRoiRect.Width <= 2 || targetRoiRect.Height <= 2)
        {
            error = "OpenCV spot healing ROI is too small.";
            return false;
        }

        using Mat targetRoi = targetBgr.SubMat(targetRoiRect);
        using Mat maskRoi = mask.SubMat(targetRoiRect).Clone();
        if (Cv2.CountNonZero(maskRoi) <= 0)
        {
            error = "OpenCV spot healing mask ROI is empty.";
            return false;
        }

        using Mat inpaintedRoi = new();
        double inpaintRadius = Math.Clamp(Math.Max(maskBox.Width, maskBox.Height) * 0.08, 2.0, 32.0);
        try
        {
            Cv2.Inpaint(targetRoi, maskRoi, inpaintedRoi, inpaintRadius, InpaintTypes.Telea);
        }
        catch (OpenCVException ex)
        {
            error = ex.Message;
            return false;
        }

        using Mat finalRoi = BlendClonedRoi(targetRoi, inpaintedRoi, maskRoi, 100.0, opacity);
        using Mat outputBgr = targetBgr.Clone();
        using Mat outputRoi = outputBgr.SubMat(targetRoiRect);
        finalRoi.CopyTo(outputRoi);
        result = CreateBgraBitmap(outputBgr, targetPixels, targetSource, width, height);
        return true;
    }

    private static bool TryAlignHealingRoisToImageBounds(
        ref OpenCvSharp.Rect targetRoiRect,
        ref OpenCvSharp.Rect sourceRoiRect,
        OpenCvSharp.Rect imageRect)
    {
        OpenCvSharp.Rect sourceSafe = IntersectRects(sourceRoiRect, imageRect);
        if (sourceSafe.Width <= 0 || sourceSafe.Height <= 0)
        {
            return false;
        }

        int sourceShiftX = sourceSafe.X - sourceRoiRect.X;
        int sourceShiftY = sourceSafe.Y - sourceRoiRect.Y;
        OpenCvSharp.Rect targetShifted = new(
            targetRoiRect.X + sourceShiftX,
            targetRoiRect.Y + sourceShiftY,
            sourceSafe.Width,
            sourceSafe.Height);
        OpenCvSharp.Rect targetSafe = IntersectRects(targetShifted, imageRect);
        if (targetSafe.Width <= 0 || targetSafe.Height <= 0)
        {
            return false;
        }

        int targetShiftX = targetSafe.X - targetShifted.X;
        int targetShiftY = targetSafe.Y - targetShifted.Y;
        sourceSafe = new OpenCvSharp.Rect(
            sourceSafe.X + targetShiftX,
            sourceSafe.Y + targetShiftY,
            targetSafe.Width,
            targetSafe.Height);
        sourceSafe = IntersectRects(sourceSafe, imageRect);
        if (sourceSafe.Width <= 0 || sourceSafe.Height <= 0)
        {
            return false;
        }

        int width = Math.Min(sourceSafe.Width, targetSafe.Width);
        int height = Math.Min(sourceSafe.Height, targetSafe.Height);
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        sourceRoiRect = new OpenCvSharp.Rect(sourceSafe.X, sourceSafe.Y, width, height);
        targetRoiRect = new OpenCvSharp.Rect(targetSafe.X, targetSafe.Y, width, height);
        return true;
    }

    private static OpenCvSharp.Rect IntersectRects(OpenCvSharp.Rect first, OpenCvSharp.Rect second)
    {
        int left = Math.Max(first.X, second.X);
        int top = Math.Max(first.Y, second.Y);
        int right = Math.Min(first.X + first.Width, second.X + second.Width);
        int bottom = Math.Min(first.Y + first.Height, second.Y + second.Height);
        return right <= left || bottom <= top
            ? new OpenCvSharp.Rect(left, top, 0, 0)
            : new OpenCvSharp.Rect(left, top, right - left, bottom - top);
    }

    private static SeamlessCloneFlags ResolveCloneFlag(string cloneMode)
    {
        return cloneMode.ToUpperInvariant() switch
        {
            "MIXED" => SeamlessCloneFlags.MixedClone,
            "MONOCHROME" => SeamlessCloneFlags.MonochromeTransfer,
            _ => SeamlessCloneFlags.NormalClone
        };
    }

    private static Mat BlendClonedRoi(Mat targetRoi, Mat clonedRoi, Mat maskRoi, double hardness, double opacity)
    {
        using Mat softMask = new();
        double opacityScale = Math.Clamp(opacity / 100.0, 0.0, 1.0) / 255.0;
        maskRoi.ConvertTo(softMask, MatType.CV_32FC1, opacityScale);

        if (hardness < 100)
        {
            int blurSize = Math.Clamp((int)Math.Round((100.0 - hardness) * 0.5), 1, 99);
            if (blurSize % 2 == 0)
            {
                blurSize++;
            }

            if (blurSize > 1)
            {
                Cv2.GaussianBlur(softMask, softMask, new OpenCvSharp.Size(blurSize, blurSize), 0);
            }
        }

        using Mat softMask3 = new();
        Cv2.CvtColor(softMask, softMask3, ColorConversionCodes.GRAY2BGR);
        using Mat targetFloat = new();
        using Mat clonedFloat = new();
        targetRoi.ConvertTo(targetFloat, MatType.CV_32FC3);
        clonedRoi.ConvertTo(clonedFloat, MatType.CV_32FC3);

        using Mat oneMinusAlpha = new();
        Cv2.Subtract(new Scalar(1.0, 1.0, 1.0), softMask3, oneMinusAlpha);
        using Mat clonedTerm = new();
        using Mat targetTerm = new();
        Cv2.Multiply(clonedFloat, softMask3, clonedTerm);
        Cv2.Multiply(targetFloat, oneMinusAlpha, targetTerm);

        using Mat resultFloat = new();
        Cv2.Add(clonedTerm, targetTerm, resultFloat);
        Mat result = new();
        resultFloat.ConvertTo(result, MatType.CV_8UC3);
        return result;
    }

    private static Mat BitmapSourceToBgraMat(BitmapSource source, out byte[] pixels)
    {
        BitmapSource bgraSource = source.Format == PixelFormats.Bgra32
            ? source
            : new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int width = bgraSource.PixelWidth;
        int height = bgraSource.PixelHeight;
        int stride = width * 4;
        pixels = new byte[stride * height];
        bgraSource.CopyPixels(pixels, stride, 0);

        Mat mat = new(height, width, MatType.CV_8UC4);
        Marshal.Copy(pixels, 0, mat.Data, pixels.Length);
        return mat;
    }

    private static Mat MaskToMat(byte[] maskPixels, int width, int height)
    {
        Mat mat = new(height, width, MatType.CV_8UC1);
        Marshal.Copy(maskPixels, 0, mat.Data, maskPixels.Length);
        return mat;
    }

    private static BitmapSource CreateBgraBitmap(Mat outputBgr, byte[] targetPixels, BitmapSource targetSource, int width, int height)
    {
        using Mat outputBgra = new();
        Cv2.CvtColor(outputBgr, outputBgra, ColorConversionCodes.BGR2BGRA);

        byte[] outputPixels = new byte[width * height * 4];
        Marshal.Copy(outputBgra.Data, outputPixels, 0, outputPixels.Length);
        for (int i = 3; i < outputPixels.Length; i += 4)
        {
            outputPixels[i] = targetPixels[i];
        }

        BitmapSource bitmap = BitmapSource.Create(
            width,
            height,
            targetSource.DpiX,
            targetSource.DpiY,
            PixelFormats.Bgra32,
            null,
            outputPixels,
            width * 4);
        bitmap.Freeze();
        return bitmap;
    }
}
