using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers.Multi
{
    public class EsToXConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int es && values[1] is double dayWidth)
            {
                return es * dayWidth;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


