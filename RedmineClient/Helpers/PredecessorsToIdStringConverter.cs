using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using RedmineClient.Models;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// 先行タスクのコレクションを「#103, #104」のような文字列に変換するコンバーター
    /// </summary>
    public class PredecessorsToIdStringConverter : IValueConverter
    {
        public static readonly PredecessorsToIdStringConverter Instance = new PredecessorsToIdStringConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<WbsItem> predecessors && predecessors.Any())
            {
                var idStrings = predecessors.Select(p => $"#{p.Id}");
                return string.Join(", ", idStrings);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
