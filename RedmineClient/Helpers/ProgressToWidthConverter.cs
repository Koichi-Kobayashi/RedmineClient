using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public static readonly ProgressToWidthConverter Instance = new ProgressToWidthConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // 進捗率を0-100の範囲に制限
                progress = Math.Max(0, Math.Min(100, progress));
                
                // プログレスバーの最大幅を100として、進捗率に応じて幅を計算
                // 最小幅は5px、最大幅は100px
                double width = Math.Max(5, (progress / 100.0) * 100);
                return width;
            }
            
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
