using System;
using System.Globalization;
using System.Windows.Data;
using RedmineClient.Services;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 日付を透明度に変換するコンバーター（土日祝の場合は透明度を下げる）
    /// </summary>
    public class DateToOpacityConverter : IValueConverter
    {
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static DateToOpacityConverter Instance { get; } = new DateToOpacityConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                try
                {
                    // 祝日は透明度を下げる（日付の背景色を優先）
                    if (HolidayService.IsHoliday(date))
                        return 0.3;
                }
                catch (Exception)
                {
                    // 祝日判定でエラーが発生した場合は無視して続行
                }
                
                // 土曜日は透明度を下げる（日付の背景色を優先）
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    return 0.3;
                // 日曜日は透明度を下げる（日付の背景色を優先）
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                    return 0.3;
                // 平日は通常の透明度
                else
                    return 0.8;
            }
            
            return 0.8;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
