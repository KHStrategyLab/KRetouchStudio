using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KRetouchStudio;

public sealed class ManualAnchorPoint : INotifyPropertyChanged
{
    private double _displayLeft;
    private double _displayTop;
    private double _originalX;
    private double _originalY;

    public ManualAnchorPoint(string id, string label, string color, double originalX, double originalY, double visualSize = 18, double circleSize = 10)
    {
        Id = id;
        Label = label;
        Color = color;
        VisualSize = visualSize;
        CircleSize = circleSize;
        _originalX = originalX;
        _originalY = originalY;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Label { get; }

    public string Color { get; }

    public double VisualSize { get; }

    public double CircleSize { get; }

    public double OriginalX
    {
        get => _originalX;
        private set
        {
            if (Math.Abs(_originalX - value) < 0.01)
            {
                return;
            }

            _originalX = value;
            OnPropertyChanged();
        }
    }

    public double OriginalY
    {
        get => _originalY;
        private set
        {
            if (Math.Abs(_originalY - value) < 0.01)
            {
                return;
            }

            _originalY = value;
            OnPropertyChanged();
        }
    }

    public double DisplayLeft
    {
        get => _displayLeft;
        set
        {
            if (Math.Abs(_displayLeft - value) < 0.01)
            {
                return;
            }

            _displayLeft = value;
            OnPropertyChanged();
        }
    }

    public double DisplayTop
    {
        get => _displayTop;
        set
        {
            if (Math.Abs(_displayTop - value) < 0.01)
            {
                return;
            }

            _displayTop = value;
            OnPropertyChanged();
        }
    }

    public void MoveOriginal(double originalX, double originalY)
    {
        OriginalX = originalX;
        OriginalY = originalY;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
