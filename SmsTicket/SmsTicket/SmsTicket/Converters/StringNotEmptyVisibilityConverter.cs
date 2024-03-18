using Microsoft.UI.Xaml.Data;

namespace SmsTicket.Converters;

public class StringNotEmptyVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (!string.IsNullOrEmpty(value?.ToString())) return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
