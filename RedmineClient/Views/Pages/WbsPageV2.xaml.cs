using System.Windows;
using Wpf.Ui.Abstractions.Controls;
using RedmineClient.ViewModels.Pages;
using System.ComponentModel;
using System.Windows.Controls;
using RedmineClient.Models;
using System.Windows.Input;

namespace RedmineClient.Views.Pages
{
    public partial class WbsPageV2 : INavigableView<WbsV2ViewModel>
    {
        public WbsV2ViewModel ViewModel { get; }

        public WbsPageV2(WbsV2ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateScheduleColumnsVisibility(ViewModel.ShowScheduleColumns);
        }

        private void Recalc_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Recalculate();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WbsV2ViewModel.ShowScheduleColumns))
            {
                UpdateScheduleColumnsVisibility(ViewModel.ShowScheduleColumns);
            }
        }

        private void UpdateScheduleColumnsVisibility(bool show)
        {
            var vis = show ? Visibility.Visible : Visibility.Collapsed;
            if (ColES != null) ColES.Visibility = vis;
            if (ColEF != null) ColEF.Visibility = vis;
            if (ColLS != null) ColLS.Visibility = vis;
            if (ColLF != null) ColLF.Visibility = vis;
            if (ColSlack != null) ColSlack.Visibility = vis;
        }

        // LeftGridのセル編集確定でRedmine更新
        private async void LeftGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row?.Item is WbsSampleTask task)
            {
                try
                {
                    await ViewModel.UpdateIssueDatesAsync(task);
                }
                catch
                {
                    // 失敗は握りつぶす（UIは先に反映）
                }
            }
        }

        // 先行タスク列 D&D
        private void PredecessorCell_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(WbsSampleTask)))
            {
                if (sender is Border border)
                {
                    var source = e.Data.GetData(typeof(WbsSampleTask)) as WbsSampleTask;
                    var target = border.DataContext as WbsSampleTask;
                    if (source != null && target != null && ViewModel.CanSetPredecessor(source, target))
                    {
                        e.Effects = DragDropEffects.Copy;
                        border.Background = System.Windows.Media.Brushes.LightGreen;
                        border.BorderBrush = System.Windows.Media.Brushes.Green;
                        border.BorderThickness = new Thickness(2);
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void PredecessorCell_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(WbsSampleTask)))
            {
                var source = e.Data.GetData(typeof(WbsSampleTask)) as WbsSampleTask;
                var target = (sender as Border)?.DataContext as WbsSampleTask;
                e.Effects = (source != null && target != null && ViewModel.CanSetPredecessor(source, target))
                    ? DragDropEffects.Copy
                    : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void PredecessorCell_DragLeave(object sender, DragEventArgs e)
        {
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.WhiteSmoke;
                border.BorderBrush = System.Windows.Media.Brushes.Black;
                border.BorderThickness = new Thickness(1);
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // タスク名セルからのドラッグ開始
        private Point _dragStartPoint;
        private void TaskCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 行選択を有効化して罫線スタイルを発火
            if (sender is FrameworkElement fe)
            {
                var row = FindAncestor<DataGridRow>(fe);
                if (row != null)
                {
                    row.IsSelected = true;
                    if (row.Item is WbsSampleTask task)
                    {
                        System.Diagnostics.Debug.WriteLine($"[WbsPageV2] TaskCell_PreviewMouseLeftButtonDown: Selected task = {task.Name}");
                    }
                }
            }
            _dragStartPoint = e.GetPosition(null);
        }

        private void TaskCell_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            if (SystemParameters.MinimumHorizontalDragDistance <= Math.Abs(pos.X - _dragStartPoint.X) ||
                SystemParameters.MinimumVerticalDragDistance <= Math.Abs(pos.Y - _dragStartPoint.Y))
            {
                if (sender is FrameworkElement fe && fe.DataContext is WbsSampleTask task)
                {
                    var data = new DataObject(typeof(WbsSampleTask), task);
                    DragDrop.DoDragDrop(fe, data, DragDropEffects.Copy);
                }
            }
        }

        // DataGrid全体でも行データをドラッグ開始できるように補完
        private void LeftGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[WbsPageV2] LeftGrid_PreviewMouseLeftButtonDown called");
            _dragStartPoint = e.GetPosition(null);
        }

        // DataGridRowのクリックイベント
        private void DataGridRow_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[WbsPageV2] DataGridRow_PreviewMouseLeftButtonDown called");
            if (sender is DataGridRow row && row.Item is WbsSampleTask task)
            {
                System.Diagnostics.Debug.WriteLine($"[WbsPageV2] DataGridRow clicked: Task = {task.Name}");
                row.IsSelected = true;
            }
        }

        private void LeftGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var pos = e.GetPosition(null);
            if (SystemParameters.MinimumHorizontalDragDistance <= Math.Abs(pos.X - _dragStartPoint.X) ||
                SystemParameters.MinimumVerticalDragDistance <= Math.Abs(pos.Y - _dragStartPoint.Y))
            {
                if (sender is DataGrid grid)
                {
                    if (e.OriginalSource is DependencyObject dep)
                    {
                        var row = ItemsControl.ContainerFromElement(grid, dep) as DataGridRow;
                        if (row?.Item is WbsSampleTask task)
                        {
                            var data = new DataObject(typeof(WbsSampleTask), task);
                            DragDrop.DoDragDrop(grid, data, DragDropEffects.Copy);
                        }
                    }
                }
            }
        }

        // DataGridの選択変更でも右ペインに反映
        private void LeftGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[WbsPageV2] LeftGrid_SelectionChanged called: Added={e.AddedItems.Count}, Removed={e.RemovedItems.Count}");
            
            if (sender is DataGrid grid)
            {
                var added = e.AddedItems.OfType<WbsSampleTask>();
                var removed = e.RemovedItems.OfType<WbsSampleTask>();
                foreach (var t in removed) t.IsSelected = false;
                foreach (var t in added) t.IsSelected = true;
                var sel = grid.SelectedItem as WbsSampleTask;
                System.Diagnostics.Debug.WriteLine($"[WbsPageV2] LeftGrid_SelectionChanged: Selected task = {sel?.Name ?? "null"}");
            }
        }

        private async void PredecessorCell_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(typeof(WbsSampleTask))) return;
                var source = e.Data.GetData(typeof(WbsSampleTask)) as WbsSampleTask;
                if (source == null) return;

                if (sender is Border border && border.DataContext is WbsSampleTask target)
                {
                    if (ReferenceEquals(source, target)) return;
                    if (!ViewModel.CanSetPredecessor(source, target)) return;
                    try
                    {
                        await ViewModel.SetPredecessorAsync(source, target);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(
                            ex.Message,
                            "先行タスク設定エラー",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }
            finally
            {
                if (sender is Border border)
                {
                    border.Background = System.Windows.Media.Brushes.WhiteSmoke;
                    border.BorderBrush = System.Windows.Media.Brushes.Black;
                    border.BorderThickness = new Thickness(1);
                }
                e.Handled = true;
            }
        }

        // ヘルパー: 祖先探索
        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match) return match;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}

