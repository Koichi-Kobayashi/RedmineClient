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
                // ダークモードに対応した色設定
                return level switch
                {
                    1 => Application.Current.Resources["SystemAccentColorBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(0, 120, 215)),      // システムアクセント色（ルート）
                    2 => new SolidColorBrush(Color.FromRgb(0, 180, 160)),      // 緑（ダークモードでも見やすい）
                    3 => new SolidColorBrush(Color.FromRgb(255, 170, 0)),      // オレンジ（ダークモードでも見やすい）
                    4 => new SolidColorBrush(Color.FromRgb(180, 80, 200)),     // 紫（ダークモードでも見やすい）
                    _ => Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(180, 180, 180))     // セカンダリテキスト色（5レベル以上）
                };
            }
            return Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush ?? new SolidColorBrush(Colors.Gray);
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

    /// <summary>
    /// 階層レベルを階層インジケーターに変換するコンバーター
    /// </summary>
    public class LevelToHierarchyIndicatorConverter : IValueConverter
    {
        public static readonly LevelToHierarchyIndicatorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level switch
                {
                    1 => "📁", // ルートレベル（フォルダー）
                    2 => "📂", // 第2レベル（開いたフォルダー）
                    3 => "📄", // 第3レベル（ドキュメント）
                    4 => "📋", // 第4レベル（クリップボード）
                    _ => "📌"  // 5レベル以上（ピン）
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 階層レベルをフォントウェイトに変換するコンバーター
    /// </summary>
    public class LevelToFontWeightConverter : IValueConverter
    {
        public static readonly LevelToFontWeightConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level switch
                {
                    1 => FontWeights.Bold,      // ルートレベル（太字）
                    2 => FontWeights.SemiBold,  // 第2レベル（中太字）
                    3 => FontWeights.Normal,    // 第3レベル（通常）
                    4 => FontWeights.Normal,    // 第4レベル（通常）
                    _ => FontWeights.Light      // 5レベル以上（細字）
                };
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 階層レベルをフォントサイズに変換するコンバーター
    /// </summary>
    public class LevelToFontSizeConverter : IValueConverter
    {
        public static readonly LevelToFontSizeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level switch
                {
                    1 => 14.0,  // ルートレベル（大きめ）
                    2 => 13.0,  // 第2レベル（やや大きめ）
                    3 => 12.0,  // 第3レベル（標準）
                    4 => 11.0,  // 第4レベル（やや小さめ）
                    _ => 10.0   // 5レベル以上（小さめ）
                };
            }
            return 12.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 階層レベルをテキスト色に変換するコンバーター
    /// </summary>
    public class LevelToTextColorConverter : IValueConverter
    {
        public static readonly LevelToTextColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                // ダークモードに対応した色設定
                return level switch
                {
                    1 => Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255)),      // プライマリテキスト色（ルート）
                    2 => new SolidColorBrush(Color.FromRgb(200, 200, 200)),      // 明るいグレー（第2レベル）
                    3 => new SolidColorBrush(Color.FromRgb(180, 180, 180)),      // グレー（第3レベル）
                    4 => new SolidColorBrush(Color.FromRgb(160, 160, 160)),      // 暗いグレー（第4レベル）
                    _ => Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(140, 140, 140))     // セカンダリテキスト色（5レベル以上）
                };
            }
            return Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush ?? new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
