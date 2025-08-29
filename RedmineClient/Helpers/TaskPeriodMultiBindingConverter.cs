using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// タスクの開始日と終了日から、特定の日付が期間内かどうかを判定するコンバーター
    /// </summary>
    public class TaskPeriodMultiBindingConverter : IMultiValueConverter
    {
        private readonly DateTime _targetDate;

        public TaskPeriodMultiBindingConverter(DateTime targetDate)
        {
            _targetDate = targetDate;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is DateTime startDate && values[1] is DateTime endDate)
            {
                // 日付のみで比較（時刻は無視）
                var start = startDate.Date;
                var end = endDate.Date;
                var target = _targetDate.Date;

                // 開始日から終了日までの期間内かチェック
                if (target >= start && target <= end)
                {
                    return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
