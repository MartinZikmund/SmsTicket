using Microsoft.UI.Xaml.Data;

namespace SmsTicket.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (!Invert)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return (bool)value ? Visibility.Collapsed: Visibility.Visible;
        }

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
