using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RedmineClient.Services;

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
                try
                {
                    // 祝日は赤色（日曜日と同じ）
                    if (HolidayService.IsHoliday(date))
                        return Brushes.LightPink;
                }
                catch (Exception)
                {
                    // 祝日判定でエラーが発生した場合は無視して続行
                }
                
                // 土曜日は薄いシアン色
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    return Brushes.LightCyan;
                // 日曜日はピンク色
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                    return Brushes.LightPink;
                // 平日は白色
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
