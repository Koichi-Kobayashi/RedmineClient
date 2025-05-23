﻿using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    public class DateTimeToYYYYMMDDConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToYYYYMMDD();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
