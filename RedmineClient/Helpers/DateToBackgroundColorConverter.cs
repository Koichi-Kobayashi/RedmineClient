using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 日付を背景色に変換するコンバーター
    /// </summary>
    public class DateToBackgroundColorConverter : IValueConverter
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static DateToBackgroundColorConverter Instance { get; } = new DateToBackgroundColorConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                // 土曜日は青色、日曜日はピンク色
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    return Brushes.LightBlue;
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                    return Brushes.LightPink;
                else
                    return Brushes.White;
            }
            
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
