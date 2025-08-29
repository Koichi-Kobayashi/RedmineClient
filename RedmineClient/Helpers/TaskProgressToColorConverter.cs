using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// タスクの進捗に応じて色を返すコンバーター
    /// </summary>
    public class TaskProgressToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                // 進捗に応じて色を返す
                if (progress >= 100)
                {
                    return new SolidColorBrush(Colors.Green); // 完了：緑
                }
                else if (progress >= 75)
                {
                    return new SolidColorBrush(Colors.LightGreen); // 75%以上：薄緑
                }
                else if (progress >= 50)
                {
                    return new SolidColorBrush(Colors.Yellow); // 50%以上：黄色
                }
                else if (progress >= 25)
                {
                    return new SolidColorBrush(Colors.Orange); // 25%以上：オレンジ
                }
                else
                {
                    return new SolidColorBrush(Colors.LightBlue); // 25%未満：薄青
                }
            }

            return new SolidColorBrush(Colors.LightBlue); // デフォルト：薄青
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
