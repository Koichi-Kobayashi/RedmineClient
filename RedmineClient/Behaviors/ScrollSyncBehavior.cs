using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace RedmineClient.Behaviors
{
    public static class ScrollSyncBehavior
    {
        private static readonly Dictionary<string, List<WeakReference<ScrollViewer>>> Groups = new();

        public static readonly DependencyProperty GroupKeyProperty = DependencyProperty.RegisterAttached(
            "GroupKey", typeof(string), typeof(ScrollSyncBehavior), new PropertyMetadata(null, OnGroupChanged));

        public static void SetGroupKey(DependencyObject element, string value) => element.SetValue(GroupKeyProperty, value);
        public static string GetGroupKey(DependencyObject element) => (string)element.GetValue(GroupKeyProperty);

        public static readonly DependencyProperty IsMasterProperty = DependencyProperty.RegisterAttached(
            "IsMaster", typeof(bool), typeof(ScrollSyncBehavior), new PropertyMetadata(false));
        public static void SetIsMaster(DependencyObject element, bool value) => element.SetValue(IsMasterProperty, value);
        public static bool GetIsMaster(DependencyObject element) => (bool)element.GetValue(IsMasterProperty);

        private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                string? key = e.NewValue as string;
                if (key == null) return;
                if (!Groups.TryGetValue(key, out var list)) Groups[key] = list = new();
                list.Add(new WeakReference<ScrollViewer>(sv));

                sv.ScrollChanged += (s, ev) =>
                {
                    if (!GetIsMaster(sv)) return;
                    if (ev.VerticalChange == 0) return;
                    if (Groups.TryGetValue(key, out var peers))
                    {
                        foreach (var wr in peers)
                        {
                            if (wr.TryGetTarget(out var other) && !ReferenceEquals(other, sv))
                            {
                                other.ScrollToVerticalOffset(ev.VerticalOffset);
                            }
                        }
                    }
                };
            }
            else if (d is DataGrid dg)
            {
                dg.Loaded += (_, __) =>
                {
                    var sv = FindVisualChild<ScrollViewer>(dg);
                    if (sv != null)
                    {
                        SetGroupKey(sv, (string?)e.NewValue ?? string.Empty);
                        SetIsMaster(sv, GetIsMaster(dg));
                    }
                };
            }
        }

        private static T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}


