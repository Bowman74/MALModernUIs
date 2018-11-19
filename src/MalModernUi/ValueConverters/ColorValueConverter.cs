using System;
using System.Globalization;
using Xamarin.Forms;

namespace MalModernUi.ValueConverters
{
    public class ColorValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool.TryParse(value.ToString(), out var isClient);
            return isClient ? Color.CadetBlue : Color.LimeGreen;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
