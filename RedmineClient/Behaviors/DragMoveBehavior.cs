using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;
using System.Diagnostics;

namespace RedmineClient.Behaviors
{
    public static class DragMoveBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DragMoveBehavior), new PropertyMetadata(false, OnChanged));
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static bool _dragging; private static Point _startPos; private static int _startEs; private static int _startDuration; private static int _lastDelta; private static WbsSampleTask? _task; private static WbsV2ViewModel? _vm;
        private static FrameworkElement? _dragRoot; private static double _startMouseX;
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
                Debug.WriteLine($"[DragMoveBehavior] OnDown: Task={t.Name}, Position={e.GetPosition(fe)}");
                _task=t; _vm=FindVm(fe); if(_vm==null)return; _startPos=e.GetPosition(fe); _startEs=t.ES; _startDuration=t.Duration; _lastDelta=0;
                _dragRoot = FindDragRoot(fe);
                _startMouseX = Mouse.GetPosition(_dragRoot ?? fe).X;
                Debug.WriteLine($"[DragMoveBehavior] OnDown: StartMouseX={_startMouseX}, StartPos={_startPos}");
                // 端領域か中央かを判定
                double width = fe.ActualWidth;
                double x = _startPos.X;
                if (x <= EDGE_HANDLE_WIDTH)
                {
                    _dragKind = DragKind.ResizeStart; fe.Cursor = Cursors.SizeWE;
                    Debug.WriteLine($"[DragMoveBehavior] OnDown: ResizeStart mode");
                }
                else if (x >= width - EDGE_HANDLE_WIDTH)
                {
                    _dragKind = DragKind.ResizeEnd; fe.Cursor = Cursors.SizeWE;
                    Debug.WriteLine($"[DragMoveBehavior] OnDown: ResizeEnd mode");
                }
                else
                {
                    _dragKind = DragKind.Move; fe.Cursor = Cursors.SizeAll;
                    Debug.WriteLine($"[DragMoveBehavior] OnDown: Move mode");
                }
                // ドラッグ開始はしない（OnMoveで最小距離チェック後に開始）
            } 
        }
        private static void OnMove(object s, MouseEventArgs e)
        {
            if (_vm == null || _task == null) return;
            if (s is FrameworkElement fe)
            {
                // ドラッグしていないときは最小距離チェックしてドラッグ開始
                if (!_dragging)
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        var currentPos = Mouse.GetPosition(_dragRoot ?? fe);
                        var deltaX = System.Math.Abs(currentPos.X - _startMouseX);
                        var deltaY = System.Math.Abs(currentPos.Y - _startPos.Y);
                        Debug.WriteLine($"[DragMoveBehavior] OnMove: DeltaX={deltaX}, DeltaY={deltaY}, MinH={SystemParameters.MinimumHorizontalDragDistance}, MinV={SystemParameters.MinimumVerticalDragDistance}");
                        
                        if (deltaX >= SystemParameters.MinimumHorizontalDragDistance ||
                            deltaY >= SystemParameters.MinimumVerticalDragDistance)
                        {
                            _dragging = true;
                            fe.CaptureMouse();
                            Debug.WriteLine($"[DragMoveBehavior] OnMove: Drag started");
                        }
                        else
                        {
                            Debug.WriteLine($"[DragMoveBehavior] OnMove: Not enough distance, skipping");
                            return; // 最小距離に達していない場合は何もしない
                        }
                    }
                    else
                    {
                        var pos = e.GetPosition(fe);
                        fe.Cursor = (pos.X <= EDGE_HANDLE_WIDTH || pos.X >= fe.ActualWidth - EDGE_HANDLE_WIDTH) ? Cursors.SizeWE : Cursors.SizeAll;
                        return;
                    }
                }

                var currentX = Mouse.GetPosition(_dragRoot ?? fe).X;
                double dx = currentX - _startMouseX;
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
                
                // 矢印を再描画
                RefreshArrows(fe);
            }
        }
        private static void OnUp(object s, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[DragMoveBehavior] OnUp: Dragging={_dragging}, Task={_task?.Name}");
            if (!_dragging || _vm==null || _task==null) return;
            if (s is FrameworkElement fe)
            {
                var currentX = Mouse.GetPosition(_dragRoot ?? fe).X; double dx = currentX - _startMouseX; int delta = (int)System.Math.Round(dx / _vm.DayWidth);
                Debug.WriteLine($"[DragMoveBehavior] OnUp: Delta={delta}, AbsDelta={System.Math.Abs(delta)}");
                // クリック（移動距離が小さい）の場合は何もしない
                if (System.Math.Abs(delta) <= 1)
                {
                    Debug.WriteLine($"[DragMoveBehavior] OnUp: Click detected, no action");
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
                
                // 最終的な矢印を再描画
                RefreshArrows(fe);
            }
        }
        private static FrameworkElement? FindDragRoot(DependencyObject d)
        {
            // キャンバス/ItemsControl/ScrollViewerなど動かない親を探す
            DependencyObject? cur = d;
            while (cur != null)
            {
                if (cur is Canvas canvas) return canvas;
                if (cur is ItemsControl ic) return ic;
                if (cur is ScrollViewer sv) return sv;
                cur = VisualTreeHelper.GetParent(cur);
            }
            return d as FrameworkElement;
        }
        private static WbsV2ViewModel? FindVm(DependencyObject d)
        {
            DependencyObject? cur=d; while(cur!=null){ if(cur is FrameworkElement fe && fe.DataContext is WbsV2ViewModel vm) return vm; cur=VisualTreeHelper.GetParent(cur);} return null;
        }
        
        private static void RefreshArrows(FrameworkElement element)
        {
            // WbsPageV2のインスタンスを探して矢印を再描画
            DependencyObject? cur = element;
            while (cur != null)
            {
                if (cur is RedmineClient.Views.Pages.WbsPageV2 page)
                {
                    // DrawArrowsメソッドを直接呼び出し
                    page.DrawArrows();
                    break;
                }
                cur = VisualTreeHelper.GetParent(cur);
            }
        }
    }
}



