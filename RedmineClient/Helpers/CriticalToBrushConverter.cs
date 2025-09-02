using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    public class CriticalToBrushConverter : IValueConverter
    {
        public Brush Critical { get; set; } = new SolidColorBrush(Color.FromRgb(220, 53, 69));
        public Brush Normal { get; set; } = new SolidColorBrush(Color.FromRgb(80, 140, 200));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Critical : Normal;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


