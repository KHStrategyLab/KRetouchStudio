using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KRetouchStudio;

public sealed class ToneCurveEditorState : INotifyPropertyChanged
{
    private const double CurvePlotWidth = 270;
    private const double CurvePlotHeight = 180;
    private const double CurvePointRadius = 4;
    private const double CurvePlotOffset = CurvePointRadius;
    private const int MaxCurvePoints = 7;
    private const int CurveHistogramSampleLongSide = 512;

    private readonly Dictionary<CurveChannel, ObservableCollection<CurvePoint>> _curvePointsByChannel = new();
    private Dictionary<CurveChannel, PointCollection> _curveHistogramPointsByChannel = CreateEmptyCurveHistogramPointsByChannel();
    private CurveChannel _curveChannel = CurveChannel.All;
    private CurvePoint? _selectedCurvePoint;
    private PointCollection _curveHistogramPoints = new();
    private double _value = 100;

    public ToneCurveEditorState()
    {
        InitializeCurvePoints();
        RefreshCurveHistogramPoints();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CurvePoint> CurvePoints => GetCurvePoints(CurveChannel);

    public CurvePoint? SelectedCurvePoint
    {
        get => _selectedCurvePoint;
        set
        {
            if (ReferenceEquals(_selectedCurvePoint, value))
            {
                return;
            }

            if (_selectedCurvePoint is not null)
            {
                _selectedCurvePoint.PropertyChanged -= SelectedCurvePoint_PropertyChanged;
            }

            _selectedCurvePoint = value;
            if (_selectedCurvePoint is not null)
            {
                _selectedCurvePoint.PropertyChanged += SelectedCurvePoint_PropertyChanged;
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCurveInputText));
            OnPropertyChanged(nameof(SelectedCurveOutputText));
            OnPropertyChanged(nameof(HasSelectedCurvePoint));
            OnPropertyChanged(nameof(IsSelectedCurveInputEditable));
        }
    }

    public bool HasSelectedCurvePoint => SelectedCurvePoint is not null;
    public bool IsSelectedCurveInputEditable => SelectedCurvePoint is not null;
    public string SelectedCurveInputText => SelectedCurvePoint is null ? "-" : SelectedCurvePoint.Input.ToString("0");
    public string SelectedCurveOutputText => SelectedCurvePoint is null ? "-" : SelectedCurvePoint.Output.ToString("0");

    public PointCollection CurveHistogramPoints
    {
        get => _curveHistogramPoints;
        private set
        {
            _curveHistogramPoints = value;
            OnPropertyChanged();
        }
    }

    public PointCollection CurvePolylinePoints
    {
        get
        {
            PointCollection points = new();
            byte[] lut = BuildCurveLookupTable(CurveChannel);
            for (int input = 0; input < lut.Length; input++)
            {
                points.Add(new System.Windows.Point(InputToCanvasX(input), OutputToCanvasY(lut[input])));
            }

            return points;
        }
    }

    public PointCollection CurveControlPolylinePoints
    {
        get
        {
            PointCollection points = new();
            foreach (CurvePoint point in CurvePoints.OrderBy(point => point.Input))
            {
                points.Add(new System.Windows.Point(point.CanvasLeft + 4, point.CanvasTop + 4));
            }

            return points;
        }
    }

    public CurveChannel CurveChannel
    {
        get => _curveChannel;
        set
        {
            if (_curveChannel == value)
            {
                return;
            }

            _curveChannel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurvePoints));
            OnPropertyChanged(nameof(CurvePolylinePoints));
            OnPropertyChanged(nameof(CurveControlPolylinePoints));
            OnPropertyChanged(nameof(CurveStrengthLabel));
            RefreshCurveHistogramPoints();
        }
    }

    public double Value
    {
        get => _value;
        set
        {
            double clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_value - clamped) < 0.001)
            {
                return;
            }

            _value = clamped;
            OnPropertyChanged();
        }
    }

    public string CurveStrengthLabel => $"커브 적용량 ({CurveChannelDisplayName})";

    private string CurveChannelDisplayName => CurveChannel switch
    {
        CurveChannel.Red => "R",
        CurveChannel.Green => "G",
        CurveChannel.Blue => "B",
        _ => "전체"
    };

    public void SetCurveHistogramSource(BitmapSource? source)
    {
        _curveHistogramPointsByChannel = source is null
            ? CreateEmptyCurveHistogramPointsByChannel()
            : CreateCurveHistogramPointsByChannel(source);
        RefreshCurveHistogramPoints();
    }

    public CurvePoint? AddCurvePointFromCanvas(double canvasX, double canvasY)
    {
        ObservableCollection<CurvePoint> points = GetCurvePoints(CurveChannel);
        if (points.Count >= MaxCurvePoints || !IsInsideCurvePlot(canvasX, canvasY))
        {
            return null;
        }

        double input = CanvasXToInput(canvasX);
        if (points.Any(point => Math.Abs(point.Input - input) <= 2))
        {
            return null;
        }

        CurvePoint point = new(input, CanvasYToOutput(canvasY), isEndpoint: false);
        points.Add(point);
        SortCurvePoints(points);
        NotifyCurveChanged();
        return point;
    }

    public void MoveCurvePoint(CurvePoint point, double canvasX, double canvasY)
    {
        ObservableCollection<CurvePoint> points = GetCurvePoints(CurveChannel);
        if (!points.Contains(point))
        {
            return;
        }

        point.Input = Math.Clamp(CanvasXToInput(canvasX), 0, 255);
        point.Output = CanvasYToOutput(canvasY);
        SortCurvePoints(points);
        NotifyCurveChanged();
    }

    public bool SetSelectedCurvePointInput(double input)
    {
        if (SelectedCurvePoint is null)
        {
            return false;
        }

        double clamped = Math.Clamp(input, 0, 255);
        if (Math.Abs(SelectedCurvePoint.Input - clamped) < 0.001)
        {
            return false;
        }

        ObservableCollection<CurvePoint> points = GetCurvePoints(CurveChannel);
        SelectedCurvePoint.Input = clamped;
        SortCurvePoints(points);
        NotifyCurveChanged();
        return true;
    }

    public bool SetSelectedCurvePointOutput(double output)
    {
        if (SelectedCurvePoint is null)
        {
            return false;
        }

        double clamped = Math.Clamp(output, 0, 255);
        if (Math.Abs(SelectedCurvePoint.Output - clamped) < 0.001)
        {
            return false;
        }

        SelectedCurvePoint.Output = clamped;
        NotifyCurveChanged();
        return true;
    }

    public void MarkCurvePointForDeletion(CurvePoint point)
    {
        point.IsPendingDelete = true;
    }

    public bool DeleteCurvePointIfMarked(CurvePoint point)
    {
        return point.IsPendingDelete && DeleteCurvePoint(point);
    }

    public bool ResetCurrentCurveChannel()
    {
        ObservableCollection<CurvePoint> points = GetCurvePoints(CurveChannel);
        bool isDefault = IsDefaultCurve(points);

        if (isDefault)
        {
            return false;
        }

        SelectedCurvePoint = null;
        points.Clear();
        points.Add(new CurvePoint(0, 0, isEndpoint: false));
        points.Add(new CurvePoint(255, 255, isEndpoint: false));
        NotifyCurveChanged();
        return true;
    }

    public void ResetAllChannels()
    {
        SelectedCurvePoint = null;

        foreach (CurveChannel channel in Enum.GetValues<CurveChannel>())
        {
            ObservableCollection<CurvePoint> points = GetCurvePoints(channel);
            points.Clear();
            points.Add(new CurvePoint(0, 0, isEndpoint: false));
            points.Add(new CurvePoint(255, 255, isEndpoint: false));
        }

        Value = 100;
        CurveChannel = CurveChannel.All;
        NotifyCurveChanged();
        RefreshCurveHistogramPoints();
    }

    public bool HasEffectiveAdjustment()
    {
        if (Value <= 0.001)
        {
            return false;
        }

        foreach (CurveChannel channel in Enum.GetValues<CurveChannel>())
        {
            if (!IsDefaultCurve(GetCurvePoints(channel)))
            {
                return true;
            }
        }

        return false;
    }

    public byte[] BuildCurveLookupTable(CurveChannel channel)
    {
        ObservableCollection<CurvePoint> points = GetCurvePoints(channel);
        CurvePoint[] ordered = points.OrderBy(point => point.Input).ToArray();
        byte[] lut = new byte[256];
        if (ordered.Length < 2)
        {
            for (int index = 0; index < lut.Length; index++)
            {
                lut[index] = (byte)index;
            }

            return lut;
        }

        double[] tangents = CalculateCurveTangents(ordered);
        int segmentIndex = 0;
        for (int input = 0; input < lut.Length; input++)
        {
            while (segmentIndex < ordered.Length - 2 && input > ordered[segmentIndex + 1].Input)
            {
                segmentIndex++;
            }

            CurvePoint left = ordered[segmentIndex];
            CurvePoint right = ordered[segmentIndex + 1];
            double output = EvaluateCubicHermite(
                left.Input,
                right.Input,
                left.Output,
                right.Output,
                tangents[segmentIndex],
                tangents[segmentIndex + 1],
                input);
            lut[input] = (byte)Math.Clamp((int)Math.Round(output), 0, 255);
        }

        return lut;
    }

    private static bool IsInsideCurvePlot(double canvasX, double canvasY)
    {
        return canvasX >= CurvePlotOffset &&
               canvasX <= CurvePlotOffset + CurvePlotWidth &&
               canvasY >= CurvePlotOffset &&
               canvasY <= CurvePlotOffset + CurvePlotHeight;
    }

    private static bool IsDefaultCurve(IReadOnlyCollection<CurvePoint> points)
    {
        return points.Count == 2 &&
               points.Any(point => Math.Abs(point.Input) < 0.001 && Math.Abs(point.Output) < 0.001) &&
               points.Any(point => Math.Abs(point.Input - 255) < 0.001 && Math.Abs(point.Output - 255) < 0.001);
    }

    private bool DeleteCurvePoint(CurvePoint point)
    {
        ObservableCollection<CurvePoint> points = GetCurvePoints(CurveChannel);
        bool removed = points.Remove(point);
        if (removed)
        {
            NotifyCurveChanged();
        }

        return removed;
    }

    private void SelectedCurvePoint_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CurvePoint.Input) or nameof(CurvePoint.Output))
        {
            OnPropertyChanged(nameof(SelectedCurveInputText));
            OnPropertyChanged(nameof(SelectedCurveOutputText));
        }
    }

    private void RefreshCurveHistogramPoints()
    {
        CurveHistogramPoints = _curveHistogramPointsByChannel.TryGetValue(CurveChannel, out PointCollection? points)
            ? points
            : new PointCollection();
    }

    private static Dictionary<CurveChannel, PointCollection> CreateEmptyCurveHistogramPointsByChannel()
    {
        return Enum.GetValues<CurveChannel>()
            .ToDictionary(channel => channel, _ => new PointCollection());
    }

    private static Dictionary<CurveChannel, PointCollection> CreateCurveHistogramPointsByChannel(BitmapSource source)
    {
        BitmapSource sample = CreateCurveHistogramSample(source);
        BitmapSource bitmap = sample.Format == PixelFormats.Bgra32
            ? sample
            : new FormatConvertedBitmap(sample, PixelFormats.Bgra32, null, 0);

        int stride = bitmap.PixelWidth * 4;
        byte[] pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        int[] allBins = new int[256];
        int[] redBins = new int[256];
        int[] greenBins = new int[256];
        int[] blueBins = new int[256];

        for (int index = 0; index < pixels.Length; index += 4)
        {
            if (pixels[index + 3] == 0)
            {
                continue;
            }

            int blue = pixels[index];
            int green = pixels[index + 1];
            int red = pixels[index + 2];
            int luminance = Math.Clamp((int)Math.Round(red * 0.2126 + green * 0.7152 + blue * 0.0722), 0, 255);
            allBins[luminance]++;
            redBins[red]++;
            greenBins[green]++;
            blueBins[blue]++;
        }

        return new Dictionary<CurveChannel, PointCollection>
        {
            [CurveChannel.All] = CreateCurveHistogramPoints(allBins),
            [CurveChannel.Red] = CreateCurveHistogramPoints(redBins),
            [CurveChannel.Green] = CreateCurveHistogramPoints(greenBins),
            [CurveChannel.Blue] = CreateCurveHistogramPoints(blueBins)
        };
    }

    private static PointCollection CreateCurveHistogramPoints(int[] bins)
    {
        int maximum = bins.Max();
        if (maximum == 0)
        {
            return new PointCollection();
        }

        double logMaximum = Math.Log(maximum + 1);
        PointCollection points = new()
        {
            new System.Windows.Point(CurvePlotOffset, CurvePlotOffset + CurvePlotHeight)
        };

        for (int index = 0; index < bins.Length; index++)
        {
            double x = InputToCanvasX(index);
            double normalized = Math.Log(bins[index] + 1) / logMaximum;
            double y = CurvePlotOffset + CurvePlotHeight - normalized * (CurvePlotHeight * 0.88);
            points.Add(new System.Windows.Point(x, y));
        }

        points.Add(new System.Windows.Point(CurvePlotOffset + CurvePlotWidth, CurvePlotOffset + CurvePlotHeight));
        return points;
    }

    private static BitmapSource CreateCurveHistogramSample(BitmapSource source)
    {
        int longestSide = Math.Max(source.PixelWidth, source.PixelHeight);
        if (longestSide <= CurveHistogramSampleLongSide)
        {
            return source;
        }

        double scale = (double)CurveHistogramSampleLongSide / longestSide;
        TransformedBitmap sample = new(source, new ScaleTransform(scale, scale));
        sample.Freeze();
        return sample;
    }

    private static double[] CalculateCurveTangents(CurvePoint[] points)
    {
        double[] tangents = new double[points.Length];
        if (points.Length < 2)
        {
            return tangents;
        }

        if (points.Length == 2)
        {
            double slope = SegmentSlope(points[0], points[1]);
            tangents[0] = slope;
            tangents[1] = slope;
            return tangents;
        }

        double[] widths = new double[points.Length - 1];
        double[] slopes = new double[points.Length - 1];
        for (int index = 0; index < widths.Length; index++)
        {
            widths[index] = Math.Max(0.001, points[index + 1].Input - points[index].Input);
            slopes[index] = (points[index + 1].Output - points[index].Output) / widths[index];
        }

        tangents[0] = CalculateEndpointTangent(widths[0], widths[1], slopes[0], slopes[1]);
        tangents[^1] = CalculateEndpointTangent(widths[^1], widths[^2], slopes[^1], slopes[^2]);

        for (int index = 1; index < points.Length - 1; index++)
        {
            double previousSlope = slopes[index - 1];
            double nextSlope = slopes[index];
            if (Math.Abs(previousSlope) < 0.0001 ||
                Math.Abs(nextSlope) < 0.0001 ||
                Math.Sign(previousSlope) != Math.Sign(nextSlope))
            {
                tangents[index] = 0;
                continue;
            }

            double previousWidth = widths[index - 1];
            double nextWidth = widths[index];
            double weightA = 2 * nextWidth + previousWidth;
            double weightB = nextWidth + 2 * previousWidth;
            tangents[index] = (weightA + weightB) / (weightA / previousSlope + weightB / nextSlope);
        }

        return tangents;
    }

    private static double SegmentSlope(CurvePoint left, CurvePoint right)
    {
        return (right.Output - left.Output) / Math.Max(0.001, right.Input - left.Input);
    }

    private static double CalculateEndpointTangent(double width, double nextWidth, double slope, double nextSlope)
    {
        double tangent = ((2 * width + nextWidth) * slope - width * nextSlope) / (width + nextWidth);
        if (Math.Sign(tangent) != Math.Sign(slope))
        {
            return 0;
        }

        if (Math.Sign(slope) != Math.Sign(nextSlope) && Math.Abs(tangent) > Math.Abs(3 * slope))
        {
            return 3 * slope;
        }

        return tangent;
    }

    private static double EvaluateCubicHermite(
        double leftInput,
        double rightInput,
        double leftOutput,
        double rightOutput,
        double leftTangent,
        double rightTangent,
        double input)
    {
        double width = Math.Max(0.001, rightInput - leftInput);
        double t = Math.Clamp((input - leftInput) / width, 0, 1);
        double t2 = t * t;
        double t3 = t2 * t;

        double leftBlend = 2 * t3 - 3 * t2 + 1;
        double leftTangentBlend = t3 - 2 * t2 + t;
        double rightBlend = -2 * t3 + 3 * t2;
        double rightTangentBlend = t3 - t2;

        return leftBlend * leftOutput +
               leftTangentBlend * width * leftTangent +
               rightBlend * rightOutput +
               rightTangentBlend * width * rightTangent;
    }

    private void InitializeCurvePoints()
    {
        foreach (CurveChannel channel in Enum.GetValues<CurveChannel>())
        {
            _curvePointsByChannel[channel] = new ObservableCollection<CurvePoint>
            {
                new CurvePoint(0, 0, isEndpoint: false),
                new CurvePoint(255, 255, isEndpoint: false)
            };
        }
    }

    private ObservableCollection<CurvePoint> GetCurvePoints(CurveChannel channel)
    {
        if (!_curvePointsByChannel.TryGetValue(channel, out ObservableCollection<CurvePoint>? points))
        {
            points = new ObservableCollection<CurvePoint>
            {
                new CurvePoint(0, 0, isEndpoint: false),
                new CurvePoint(255, 255, isEndpoint: false)
            };
            _curvePointsByChannel[channel] = points;
        }

        return points;
    }

    private static double CanvasXToInput(double canvasX)
    {
        return Math.Clamp((canvasX - CurvePlotOffset) / CurvePlotWidth * 255, 0, 255);
    }

    private static double CanvasYToOutput(double canvasY)
    {
        return Math.Clamp(255 - (canvasY - CurvePlotOffset) / CurvePlotHeight * 255, 0, 255);
    }

    private static double InputToCanvasX(double input)
    {
        return CurvePlotOffset + input / 255d * CurvePlotWidth;
    }

    private static double OutputToCanvasY(double output)
    {
        return CurvePlotOffset + (255 - output) / 255d * CurvePlotHeight;
    }

    private void SortCurvePoints(ObservableCollection<CurvePoint> points)
    {
        CurvePoint[] ordered = points.OrderBy(point => point.Input).ToArray();
        for (int index = 0; index < ordered.Length; index++)
        {
            int currentIndex = points.IndexOf(ordered[index]);
            if (currentIndex != index)
            {
                points.Move(currentIndex, index);
            }
        }
    }

    private void NotifyCurveChanged()
    {
        OnPropertyChanged(nameof(CurvePoints));
        OnPropertyChanged(nameof(CurvePolylinePoints));
        OnPropertyChanged(nameof(CurveControlPolylinePoints));
        OnPropertyChanged(nameof(CurveChannel));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
