using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    public class StatusToBrushConverter : IValueConverter
    {
        public static readonly StatusToBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "完了" => new SolidColorBrush(Colors.Green),
                    "進行中" => new SolidColorBrush(Colors.Blue),
                    "未着手" => new SolidColorBrush(Colors.Gray),
                    "保留" => new SolidColorBrush(Colors.Orange),
                    "キャンセル" => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToBrushConverter : IValueConverter
    {
        public static readonly PriorityToBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string priority)
            {
                return priority switch
                {
                    "緊急" => new SolidColorBrush(Colors.Red),
                    "高" => new SolidColorBrush(Colors.Orange),
                    "中" => new SolidColorBrush(Colors.Yellow),
                    "低" => new SolidColorBrush(Colors.Green),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToFontWeightConverter : IValueConverter
    {
        public static readonly BooleanToFontWeightConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected ? FontWeights.Bold : FontWeights.Normal;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToBrushConverter : IValueConverter
    {
        public static readonly BooleanToBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                // 接続状態に応じた色を返す
                return isConnected ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectToBooleanConverter : IValueConverter
    {
        public static readonly ObjectToBooleanConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectToVisibilityConverter : IValueConverter
    {
        public static readonly ObjectToVisibilityConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ObjectToVisibilityInverseConverter : IValueConverter
    {
        public static readonly ObjectToVisibilityInverseConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 選択状態と進捗からタイムラインバーの色を決めるコンバーター
    /// </summary>
    public class SelectedProgressToBrushConverter : IMultiValueConverter
    {
        public static readonly SelectedProgressToBrushConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = 0;
            if (values.Length > 0 && values[0] is double p)
            {
                progress = p;
            }
            bool isSelected = false;
            if (values.Length > 1 && values[1] is bool sel)
            {
                isSelected = sel;
            }

            if (isSelected)
            {
                return new SolidColorBrush(Color.FromRgb(138, 43, 226)); // BlueViolet（選択強調）
            }

            if (progress >= 100)
            {
                return new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else if (progress >= 75)
            {
                return new SolidColorBrush(Color.FromRgb(144, 238, 144));
            }
            else if (progress >= 50)
            {
                return new SolidColorBrush(Color.FromRgb(255, 235, 59));
            }
            else if (progress >= 25)
            {
                return new SolidColorBrush(Color.FromRgb(255, 152, 0));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(173, 216, 230));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 選択状態と日付（土日祝）で不透明度を決めるコンバーター
    /// </summary>
    public class SelectedDateOpacityConverter : IMultiValueConverter
    {
        public static readonly SelectedDateOpacityConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.8;

            bool isSelected = values[1] is bool b && b;
            if (isSelected)
            {
                return 1.0;
            }

            if (values[0] is DateTime d)
            {
                if (RedmineClient.Services.HolidayService.IsHoliday(d) || d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                {
                    return 0.3;
                }
            }
            return 0.8;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
