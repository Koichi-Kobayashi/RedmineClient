using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;

namespace RedmineClient.Behaviors
{
    public static class DragMoveBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DragMoveBehavior), new PropertyMetadata(false, OnChanged));
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static bool _dragging; private static Point _startPos; private static int _startEs; private static WbsSampleTask? _task; private static WbsSampleViewModel? _vm;
        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe)
            {
                if ((bool)e.NewValue)
                {
                    fe.PreviewMouseLeftButtonDown += OnDown;
                    fe.PreviewMouseMove += OnMove;
                    fe.PreviewMouseLeftButtonUp += OnUp;
                }
                else
                {
                    fe.PreviewMouseLeftButtonDown -= OnDown;
                    fe.PreviewMouseMove -= OnMove;
                    fe.PreviewMouseLeftButtonUp -= OnUp;
                }
            }
        }
        private static void OnDown(object s, MouseButtonEventArgs e)
        { if (s is FrameworkElement fe && fe.DataContext is WbsSampleTask t){ _task=t; _vm=FindVm(fe); if(_vm==null)return; _startPos=e.GetPosition(fe); _startEs=t.ES; _dragging=true; fe.CaptureMouse(); } }
        private static void OnMove(object s, MouseEventArgs e) { }
        private static void OnUp(object s, MouseButtonEventArgs e)
        {
            if (!_dragging || _vm==null || _task==null) return;
            if (s is FrameworkElement fe)
            {
                var p = e.GetPosition(fe); double dx = p.X - _startPos.X; int delta = (int)System.Math.Round(dx / _vm.DayWidth);
                int newEs = System.Math.Max(0, _startEs + delta); _vm.ApplyStartConstraint(_task, newEs);
                _dragging=false; fe.ReleaseMouseCapture();
            }
        }
        private static WbsSampleViewModel? FindVm(DependencyObject d)
        {
            DependencyObject? cur=d; while(cur!=null){ if(cur is FrameworkElement fe && fe.DataContext is WbsSampleViewModel vm) return vm; cur=VisualTreeHelper.GetParent(cur);} return null;
        }
    }
}


