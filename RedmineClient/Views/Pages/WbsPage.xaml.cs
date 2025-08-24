using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace RedmineClient.Views.Pages
{
    /// <summary>
    /// WbsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class WbsPage : INavigableView<WbsViewModel>, INavigationAware
    {
        public WbsViewModel ViewModel { get; }

        public WbsPage(WbsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            
            // 初期化完了後にタスク詳細の幅を設定
            this.Loaded += WbsPage_InitialLoaded;
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            // 保存された幅を適用
            ApplyTaskDetailWidth();
            
            // GridSplitterのイベントを登録
            if (TaskDetailSplitter != null)
            {
                TaskDetailSplitter.DragCompleted += TaskDetailSplitter_DragCompleted;
            }
            

            
            // このイベントは一度だけ実行
            this.Loaded -= WbsPage_InitialLoaded;
        }

        private void TaskDetailSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // GridSplitterのドラッグが完了したら幅を保存
            SaveTaskDetailWidth();
        }

        public Task OnNavigatedToAsync()
        {
            // ページ表示時にタスク詳細の幅を適用
            ApplyTaskDetailWidth();
            return Task.CompletedTask;
        }

        public virtual Task OnNavigatedTo()
        {
            return OnNavigatedToAsync();
        }

        public Task OnNavigatedFromAsync()
        {
            // ページから移動する際にタスク詳細の幅を保存
            SaveTaskDetailWidth();
            return Task.CompletedTask;
        }

        public virtual Task OnNavigatedFrom()
        {
            return OnNavigatedFromAsync();
        }

        private void ApplyTaskDetailWidth()
        {
            try
            {
                if (MainContentGrid != null && MainContentGrid.ColumnDefinitions.Count >= 3)
                {
                    var detailWidth = AppConfig.TaskDetailWidth;
                    
                    // 最小幅と最大幅を確保
                    if (detailWidth < 300) detailWidth = 300;
                    if (detailWidth > 800) detailWidth = 800;
                    
                    // タスク詳細列の幅を設定
                    MainContentGrid.ColumnDefinitions[2].Width = new GridLength(detailWidth, GridUnitType.Pixel);
                    
                    System.Diagnostics.Debug.WriteLine($"タスク詳細の幅を適用: {detailWidth}px");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"タスク詳細の幅の適用に失敗: {ex.Message}");
            }
        }

        private void SaveTaskDetailWidth()
        {
            try
            {
                if (MainContentGrid != null && MainContentGrid.ColumnDefinitions.Count >= 3)
                {
                    var detailWidth = MainContentGrid.ColumnDefinitions[2].Width.Value;
                    if (detailWidth > 0)
                    {
                        AppConfig.TaskDetailWidth = detailWidth;
                        AppConfig.Save();
                        System.Diagnostics.Debug.WriteLine($"タスク詳細の幅を保存: {detailWidth}px");
                    }
                }
            }
            catch (Exception ex)
            {
                // エラーログを出力（必要に応じて）
                System.Diagnostics.Debug.WriteLine($"タスク詳細の幅の保存に失敗: {ex.Message}");
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            
            // ページが初期化された後にウィンドウのClosingイベントを登録
            this.Loaded += WbsPage_Loaded;
        }

        private void WbsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // ウィンドウが閉じられる際のイベントを登録
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Closing += Window_Closing;
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // ウィンドウが閉じられる際にタスク詳細の幅を保存
            SaveTaskDetailWidth();
        }

        /// <summary>
        /// コンテキストメニューが開かれた際のイベントハンドラー
        /// </summary>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement target)
            {
                // プレースメントターゲットからWbsItemのDataContextを取得
                var wbsItem = target.DataContext;
                
                // コンテキストメニューの各MenuItemにViewModelとWbsItemの両方を設定
                foreach (MenuItem menuItem in contextMenu.Items.OfType<MenuItem>())
                {
                    // MenuItemのDataContextはWbsItemのまま保持
                    menuItem.DataContext = wbsItem;
                    
                    // CommandはViewModelから取得
                    switch (menuItem.Header?.ToString())
                    {
                        case "サブタスク追加":
                            menuItem.Command = ViewModel.AddChildItemCommand;
                            break;
                        case "編集":
                            menuItem.Command = ViewModel.EditItemCommand;
                            break;
                        case "削除":
                            menuItem.Command = ViewModel.DeleteItemCommand;
                            break;
                    }
                    
                    // CommandParameterはWbsItemを設定
                    menuItem.CommandParameter = wbsItem;
                }
            }
        }

        /// <summary>
        /// TreeViewの選択変更イベントハンドラー
        /// </summary>
        private void WbsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WbsItem selectedItem)
            {
                ViewModel.SelectedItem = selectedItem;
            }
        }
    }
}
