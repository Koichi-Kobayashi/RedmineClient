using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    internal class BooleanInverseConverter : IValueConverter
    {
        public static readonly BooleanInverseConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
