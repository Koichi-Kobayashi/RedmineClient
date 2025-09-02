using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    public class LevelToIndentDoubleConverter : IValueConverter
    {
        public double Step { get; set; } = 16.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is int lv) ? lv * Step : 0.0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


