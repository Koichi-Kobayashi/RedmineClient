using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    public class ProgressToColorConverter : IValueConverter
    {
        public static readonly ProgressToColorConverter Instance = new ProgressToColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // 進捗率を0-100の範囲に制限
                progress = Math.Max(0, Math.Min(100, progress));
                
                // 進捗率に応じて色を決定
                if (progress >= 80)
                {
                    // 80%以上：緑（完了に近い）
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else if (progress >= 50)
                {
                    // 50-79%：青（進行中）
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                }
                else if (progress >= 20)
                {
                    // 20-49%：オレンジ（開始済み）
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    // 0-19%：赤（未着手）
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
            }
            
            // デフォルト：グレー
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
