using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers.Multi
{
    public class DurationToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is int d && values[1] is double dayWidth)
            {
                var result = System.Math.Max(1.0, d * dayWidth);
                System.Diagnostics.Debug.WriteLine($"[DurationToWidthConverter] Duration={d}, DayWidth={dayWidth}, Result={result}");
                return result;
            }
            System.Diagnostics.Debug.WriteLine($"[DurationToWidthConverter] Invalid values: {string.Join(", ", values)}");
            return 1.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}


