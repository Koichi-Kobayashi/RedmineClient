using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 階層レベルを左マージンに変換するコンバーター
    /// </summary>
    public class LevelToIndentConverter : IValueConverter
    {
        public static readonly LevelToIndentConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                // レベル1につき20pxのインデント
                var indent = level * 20;
                return new Thickness(indent, 0, 0, 0);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 階層レベルをインデント文字列に変換するコンバーター
    /// </summary>
    public class LevelToIndentStringConverter : IValueConverter
    {
        public static readonly LevelToIndentStringConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level && level > 0)
            {
                // レベルに応じて階層マーカーを生成
                if (level == 1)
                {
                    return "● "; // ルートレベル
                }
                else if (level == 2)
                {
                    return "├─ "; // 第2レベル
                }
                else if (level == 3)
                {
                    return "│  ├─ "; // 第3レベル
                }
                else if (level == 4)
                {
                    return "│  │  ├─ "; // 第4レベル
                }
                else
                {
                    // 5レベル以上は省略記号で表現
                    var dots = new string('·', level - 1);
                    return $"{dots} ├─ ";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 階層レベルを色に変換するコンバーター
    /// </summary>
    public class LevelToColorConverter : IValueConverter
    {
        public static readonly LevelToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level switch
                {
                    1 => new SolidColorBrush(Color.FromRgb(0, 120, 215)),      // 青（ルート）
                    2 => new SolidColorBrush(Color.FromRgb(0, 150, 136)),      // 緑
                    3 => new SolidColorBrush(Color.FromRgb(255, 152, 0)),      // オレンジ
                    4 => new SolidColorBrush(Color.FromRgb(156, 39, 176)),     // 紫
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))     // グレー（5レベル以上）
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 展開状態をアイコンに変換するコンバーター
    /// </summary>
    public class ExpansionToIconConverter : IValueConverter
    {
        public static readonly ExpansionToIconConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "ChevronDown16" : "ChevronRight16";
            }
            return "ChevronRight16";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
