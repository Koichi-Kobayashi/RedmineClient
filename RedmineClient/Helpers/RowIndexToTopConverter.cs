using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    public class RowIndexToTopConverter : IValueConverter
    {
        public double RowHeight { get; set; } = 28.0;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is int i) ? i * RowHeight : 0.0;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


