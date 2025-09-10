using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;

namespace RedmineClient.Behaviors
{
    public static class DragDropDependencyBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(DragDropDependencyBehavior), new PropertyMetadata(false, OnChanged));
        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        private static bool _isDragging = false;
        private static WbsSampleTask? _sourceTask = null;
        private static WbsV2ViewModel? _viewModel = null;
        private static Point _dragStartPoint;

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe)
            {
                if ((bool)e.NewValue)
                {
                    fe.PreviewMouseLeftButtonDown += OnMouseDown;
                    fe.PreviewMouseMove += OnMouseMove;
                    fe.PreviewMouseLeftButtonUp += OnMouseUp;
                    fe.DragEnter += OnDragEnter;
                    fe.DragOver += OnDragOver;
                    fe.Drop += OnDrop;
                }
                else
                {
                    fe.PreviewMouseLeftButtonDown -= OnMouseDown;
                    fe.PreviewMouseMove -= OnMouseMove;
                    fe.PreviewMouseLeftButtonUp -= OnMouseUp;
                    fe.DragEnter -= OnDragEnter;
                    fe.DragOver -= OnDragOver;
                    fe.Drop -= OnDrop;
                }
            }
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is WbsSampleTask task)
            {
                _sourceTask = task;
                _viewModel = FindViewModel(fe);
                _dragStartPoint = e.GetPosition(fe);
                _isDragging = false;
            }
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_sourceTask != null && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(sender as FrameworkElement);
                var delta = currentPosition - _dragStartPoint;
                
                if (!_isDragging && (System.Math.Abs(delta.X) > 5 || System.Math.Abs(delta.Y) > 5))
                {
                    _isDragging = true;
                    var dataObject = new DataObject("WbsSampleTask", _sourceTask);
                    DragDrop.DoDragDrop(sender as FrameworkElement, dataObject, DragDropEffects.Copy);
                }
            }
        }

        private static void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _sourceTask = null;
        }

        private static void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("WbsSampleTask"))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private static void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("WbsSampleTask"))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("WbsSampleTask") && sender is FrameworkElement fe && fe.DataContext is WbsSampleTask targetTask)
            {
                var sourceTask = e.Data.GetData("WbsSampleTask") as WbsSampleTask;
                if (sourceTask != null && sourceTask != targetTask && _viewModel != null)
                {
                    // 既存の依存関係をチェック
                    var existingLink = targetTask.Preds.FirstOrDefault(p => p.PredId == sourceTask.WbsNo);
                    if (existingLink == null)
                    {
                        // 新しい依存関係を追加
                        targetTask.Preds.Add(new DependencyLink 
                        { 
                            PredId = sourceTask.WbsNo, 
                            Type = LinkType.FS, 
                            LagDays = 0 
                        });
                        _viewModel.Recalculate();
                        
                        // 成功メッセージを表示
                        MessageBox.Show($"依存関係を追加しました: {sourceTask.Name} → {targetTask.Name}", 
                                      "依存関係追加", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // 既に存在する依存関係
                        MessageBox.Show($"依存関係は既に存在します: {sourceTask.Name} → {targetTask.Name}", 
                                      "依存関係追加", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private static WbsV2ViewModel? FindViewModel(DependencyObject d)
        {
            DependencyObject? current = d;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is WbsV2ViewModel vm)
                    return vm;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
