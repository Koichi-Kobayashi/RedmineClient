using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media;

namespace RedmineClient.Helpers
{
    /// <summary>
    /// インデックスベースでカンマの表示を制御するコンバーター
    /// 最後のアイテムにはカンマを表示しない
    /// </summary>
    public class IndexToCommaVisibilityConverter : IValueConverter
    {
        public static readonly IndexToCommaVisibilityConverter Instance = new IndexToCommaVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FrameworkElement element)
            {
                // ItemsControlを探す
                var itemsControl = FindAncestor<ItemsControl>(element);
                if (itemsControl != null && itemsControl.ItemsSource is System.Collections.IList items)
                {
                    var item = element.DataContext;
                    var index = items.IndexOf(item);
                    
                    // 最後のアイテムでない場合のみカンマを表示
                    return index < items.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定された型の親要素を探す
        /// </summary>
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                current = VisualTreeHelper.GetParent(current);
                if (current is T result)
                {
                    return result;
                }
            }
            return null;
        }
    }
}
