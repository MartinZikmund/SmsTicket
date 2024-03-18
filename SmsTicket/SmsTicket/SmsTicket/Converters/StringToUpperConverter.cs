using Microsoft.UI.Xaml.Data;

namespace SmsTicket.Converters;

public class StringToUpperConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString().ToUpper();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
