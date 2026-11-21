namespace SmsTicket.Controls;

// Cover-style crop that keeps a vertical focus point of the photo in the part of the hero the scrim leaves visible
public static class HeroCrop
{
    // Where on the viewport (fraction of its height) the focus point lands when there is room to move the photo
    public const double FocusTarget = 0.28;

    public readonly record struct Rect(double Left, double Top, double Width, double Height);

    public static Rect Compute(double imageWidth, double imageHeight, double viewWidth, double viewHeight, double focusY)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || viewWidth <= 0 || viewHeight <= 0)
        {
            return new Rect(0, 0, Math.Max(viewWidth, 0), Math.Max(viewHeight, 0));
        }

        var scale = Math.Max(viewWidth / imageWidth, viewHeight / imageHeight);
        var width = imageWidth * scale;
        var height = imageHeight * scale;

        var left = (viewWidth - width) / 2;
        var top = Math.Clamp((FocusTarget * viewHeight) - (focusY * height), viewHeight - height, 0);

        return new Rect(left, top, width, height);
    }
}
