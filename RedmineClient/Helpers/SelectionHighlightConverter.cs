using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 選択状態を背景色に変換するコンバーター
    /// </summary>
    public class SelectionHighlightConverter : IValueConverter
    {
        public static readonly SelectionHighlightConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                // ダークモードに対応した選択ハイライト色
                return Application.Current.Resources["SystemFillColorAttentionBrush"] as Brush ?? new SolidColorBrush(Color.FromArgb(255, 229, 243, 255)); // #E5F3FF
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 選択状態を境界線に変換するコンバーター
    /// </summary>
    public class SelectionBorderConverter : IValueConverter
    {
        public static readonly SelectionBorderConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)); // #0078D7
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
