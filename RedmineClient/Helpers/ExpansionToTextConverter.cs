using System;
using System.Globalization;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 展開/折りたたみ状態をテキストに変換するコンバーター
    /// </summary>
    public class ExpansionToTextConverter : IValueConverter
    {
        public static readonly ExpansionToTextConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "▼" : "▶";
            }
            return "▶";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
