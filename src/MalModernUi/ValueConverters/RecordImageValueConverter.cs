using System;
using System.Globalization;
using Xamarin.Forms;

namespace MalModernUi.ValueConverters
{
    public class RecordImageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool.TryParse(value.ToString(), out var isRecording);
            return isRecording ? ImageSource.FromResource("MalModernUi.images.baseline_mic_black_18dp.png") : ImageSource.FromResource("MalModernUi.images.baseline_mic_none_black_18dp.png");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
