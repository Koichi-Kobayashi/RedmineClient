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

        private static bool _dragging; private static Point _startPos; private static int _startEs; private static int _startDuration; private static int _lastDelta; private static WbsSampleTask? _task; private static WbsV2ViewModel? _vm;
        private const double EDGE_HANDLE_WIDTH = 8.0;
        private enum DragKind { None, ResizeStart, ResizeEnd, Move }
        private static DragKind _dragKind = DragKind.None;
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
        { 
            if (s is FrameworkElement fe && fe.DataContext is WbsSampleTask t)
            { 
                _task=t; _vm=FindVm(fe); if(_vm==null)return; _startPos=e.GetPosition(fe); _startEs=t.ES; _startDuration=t.Duration; _lastDelta=0;
                // 端領域か中央かを判定
                double width = fe.ActualWidth;
                double x = _startPos.X;
                if (x <= EDGE_HANDLE_WIDTH)
                {
                    _dragKind = DragKind.ResizeStart; fe.Cursor = Cursors.SizeWE;
                }
                else if (x >= width - EDGE_HANDLE_WIDTH)
                {
                    _dragKind = DragKind.ResizeEnd; fe.Cursor = Cursors.SizeWE;
                }
                else
                {
                    _dragKind = DragKind.Move; fe.Cursor = Cursors.SizeAll;
                }
                _dragging=true; fe.CaptureMouse();
            } 
        }
        private static void OnMove(object s, MouseEventArgs e)
        {
            if (_vm == null || _task == null) return;
            if (s is FrameworkElement fe)
            {
                // ドラッグしていないときはカーソルだけ更新
                if (!_dragging)
                {
                    var pos = e.GetPosition(fe);
                    fe.Cursor = (pos.X <= EDGE_HANDLE_WIDTH || pos.X >= fe.ActualWidth - EDGE_HANDLE_WIDTH) ? Cursors.SizeWE : Cursors.SizeAll;
                    return;
                }

                var p = e.GetPosition(fe);
                double dx = p.X - _startPos.X;
                int delta = (int)System.Math.Round(dx / _vm.DayWidth);
                if (delta == _lastDelta) return; // 変化がある時だけ更新してチラつきを抑制
                _lastDelta = delta;

                switch (_dragKind)
                {
                    case DragKind.Move:
                        _task.ES = System.Math.Max(0, _startEs + delta);
                        break;
                    case DragKind.ResizeStart:
                        {
                            int newEs = System.Math.Max(0, _startEs + delta);
                            int newDuration = System.Math.Max(1, _startDuration - (newEs - _startEs));
                            _task.ES = newEs;
                            _task.Duration = newDuration;
                            break;
                        }
                    case DragKind.ResizeEnd:
                        {
                            int newDuration = System.Math.Max(1, _startDuration + delta);
                            _task.Duration = newDuration;
                            break;
                        }
                }
            }
        }
        private static void OnUp(object s, MouseButtonEventArgs e)
        {
            if (!_dragging || _vm==null || _task==null) return;
            if (s is FrameworkElement fe)
            {
                var p = e.GetPosition(fe); double dx = p.X - _startPos.X; int delta = (int)System.Math.Round(dx / _vm.DayWidth);
                if (delta == 0)
                {
                    _dragging=false; _dragKind = DragKind.None; fe.ReleaseMouseCapture(); fe.Cursor = Cursors.Arrow; return;
                }
                switch (_dragKind)
                {
                    case DragKind.Move:
                    {
                        int newEs = System.Math.Max(0, _startEs + delta);
                        _vm.ApplyStartConstraint(_task, newEs);
                        break;
                    }
                    case DragKind.ResizeStart:
                    {
                        int newEs = System.Math.Max(0, _startEs + delta);
                        int newDuration = System.Math.Max(1, _startDuration - (newEs - _startEs));
                        _task.Duration = newDuration; // 先に期間を確定
                        _vm.ApplyStartConstraint(_task, newEs); // ES確定と再計算
                        _vm.Recalculate();
                        break;
                    }
                    case DragKind.ResizeEnd:
                    {
                        int newDuration = System.Math.Max(1, _startDuration + delta);
                        _task.Duration = newDuration;
                        _vm.Recalculate();
                        break;
                    }
                }

                // 確定後にRedmineへ反映
                var start = _task.BaseDate.AddDays(_task.ES);
                var due = start.AddDays(System.Math.Max(1, _task.Duration) - 1);
                _task.StartDate = start;
                _task.DueDate = due;

                _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        if (_vm != null)
                        {
                            await _vm.UpdateIssueDatesAsync(_task);
                        }
                    }
                    catch { }
                });

                _dragging=false; _dragKind = DragKind.None; fe.ReleaseMouseCapture(); fe.Cursor = Cursors.Arrow;
            }
        }
        private static WbsV2ViewModel? FindVm(DependencyObject d)
        {
            DependencyObject? cur=d; while(cur!=null){ if(cur is FrameworkElement fe && fe.DataContext is WbsV2ViewModel vm) return vm; cur=VisualTreeHelper.GetParent(cur);} return null;
        }
    }
}



