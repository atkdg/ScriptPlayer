using System;
using System.Globalization;
using System.Windows.Data;

namespace ScriptPlayer.Converters
{
    public class TimeSpanToSecondsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((TimeSpan) value).TotalSeconds;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return TimeSpan.FromSeconds((double) value);
        }
    }
}
