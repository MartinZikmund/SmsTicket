using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace SmsTicket.Controls;

// UniformToFill that crops around a vertical focus point instead of the center
public sealed partial class FocusedImage : Panel
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(FocusedImage), new PropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty FocusYProperty = DependencyProperty.Register(
        nameof(FocusY), typeof(double), typeof(FocusedImage), new PropertyMetadata(0.5, OnFocusChanged));

    private readonly Image _image = new()
    {
        Stretch = Stretch.Fill,
        Opacity = 0,
    };

    private double _imageWidth;
    private double _imageHeight;

    public FocusedImage()
    {
        Children.Add(_image);
        _image.ImageOpened += OnImageOpened;
    }

    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public double FocusY
    {
        get => (double)GetValue(FocusYProperty);
        set => SetValue(FocusYProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FocusedImage)d).ApplySource();

    private static void OnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((FocusedImage)d).InvalidateArrange();

    private void ApplySource()
    {
        _imageWidth = 0;
        _imageHeight = 0;
        _image.Opacity = 0;
        _image.Source = Source;
    }

    private void OnImageOpened(object sender, RoutedEventArgs e)
    {
        if (_image.Source is BitmapImage bitmap)
        {
            _imageWidth = bitmap.PixelWidth;
            _imageHeight = bitmap.PixelHeight;
        }

        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var height = double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height;

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var crop = HeroCrop.Compute(_imageWidth, _imageHeight, finalSize.Width, finalSize.Height, FocusY);

        _image.Measure(new Size(crop.Width, crop.Height));
        _image.Arrange(new Rect(crop.Left, crop.Top, crop.Width, crop.Height));
        _image.Opacity = _imageWidth > 0 ? 1 : 0;
        Clip = new RectangleGeometry { Rect = new Rect(0, 0, finalSize.Width, finalSize.Height) };

        return finalSize;
    }
}
