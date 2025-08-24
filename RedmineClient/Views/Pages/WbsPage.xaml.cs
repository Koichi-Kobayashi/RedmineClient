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
            
            // ViewModelのフォーカス要求イベントに応答
            ViewModel.RequestFocus += SetFocus;
            
            // 初期化完了後にタスク詳細の幅を設定
            this.Loaded += WbsPage_InitialLoaded;
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            // 保存された幅を適用
            ApplyTaskDetailWidth();
            
            // GridSplitterのイベントを登録
            if (ScheduleSplitter != null)
            {
                ScheduleSplitter.DragCompleted += TaskDetailSplitter_DragCompleted;
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

        /// <summary>
        /// モードに応じてフォーカスを設定する
        /// </summary>
        /// <param name="isEditModeAfterAdd">追加後編集モードかどうか</param>
        public void SetFocus(bool isEditModeAfterAdd)
        {
            if (isEditModeAfterAdd)
            {
                // 追加後編集モード：タイトルフィールドにフォーカス
                SetTitleFocus();
            }
            else
            {
                // 連続追加モード：DataGridにフォーカス
                SetDataGridFocus();
            }
        }

        /// <summary>
        /// 日付テキストボックスのキーイベントハンドラー
        /// </summary>
        private void DateTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (sender is Wpf.Ui.Controls.TextBox dateTextBox)
                {
                    if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Tab)
                    {
                        // エンターキーまたはタブキーで次の項目に移動
                        e.Handled = true;
                        MoveToNextField(dateTextBox, e.Key == System.Windows.Input.Key.Tab && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift);
                        System.Diagnostics.Debug.WriteLine($"DateTextBox: {(e.Key == System.Windows.Input.Key.Tab && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift ? "逆方向" : "順方向")}に移動");
                    }
                    else if (e.Key == System.Windows.Input.Key.Up || e.Key == System.Windows.Input.Key.Down || 
                             e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.Right)
                    {
                        // 矢印キーで日付を調整
                        e.Handled = true;
                        AdjustDate(dateTextBox, e.Key);
                        System.Diagnostics.Debug.WriteLine($"DateTextBox: 矢印キーで日付調整");
                    }
                }
            }
            catch (Exception ex)
            {
                // 例外をキャッチしてログ出力
                System.Diagnostics.Debug.WriteLine($"DateTextBox キーイベント処理中に例外: {ex.Message}");
            }
        }

        /// <summary>
        /// 次のフィールドまたは前のフィールドに移動する
        /// </summary>
        /// <param name="currentDateTextBox">現在の日付テキストボックス</param>
        /// <param name="reverse">逆方向に移動するかどうか</param>
        private void MoveToNextField(Wpf.Ui.Controls.TextBox currentDateTextBox, bool reverse)
        {
            try
            {
                if (reverse)
                {
                    // 逆方向に移動
                    if (currentDateTextBox == EndDateTextBox)
                    {
                        StartDateTextBox?.Focus();
                    }
                    else if (currentDateTextBox == StartDateTextBox)
                    {
                        DescriptionTextBox?.Focus();
                    }
                }
                else
                {
                    // 順方向に移動
                    if (currentDateTextBox == StartDateTextBox)
                    {
                        EndDateTextBox?.Focus();
                    }
                    else if (currentDateTextBox == EndDateTextBox)
                    {
                        ProgressSlider?.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"フィールド移動中にエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 矢印キーで日付を調整する
        /// </summary>
        /// <param name="dateTextBox">対象の日付テキストボックス</param>
        /// <param name="key">押されたキー</param>
        private void AdjustDate(Wpf.Ui.Controls.TextBox dateTextBox, System.Windows.Input.Key key)
        {
            try
            {
                // 現在の日付を取得
                if (DateTime.TryParse(dateTextBox.Text, out DateTime currentDate))
                {
                    DateTime newDate = currentDate;

                    switch (key)
                    {
                        case System.Windows.Input.Key.Up:
                            // 上キー：月をインクリメント
                            newDate = currentDate.AddMonths(1);
                            break;
                        case System.Windows.Input.Key.Down:
                            // 下キー：月をデクリメント
                            newDate = currentDate.AddMonths(-1);
                            break;
                        case System.Windows.Input.Key.Right:
                            // 右キー：日をインクリメント
                            newDate = currentDate.AddDays(1);
                            break;
                        case System.Windows.Input.Key.Left:
                            // 左キー：日をデクリメント
                            newDate = currentDate.AddDays(-1);
                            break;
                    }

                    // 新しい日付をテキストボックスに設定
                    dateTextBox.Text = newDate.ToString("yyyy/MM/dd");
                    System.Diagnostics.Debug.WriteLine($"DateTextBox: 日付を {currentDate:yyyy/MM/dd} から {newDate:yyyy/MM/dd} に変更");
                }
                else
                {
                    // 日付が解析できない場合は今日の日付を設定
                    var today = DateTime.Today;
                    dateTextBox.Text = today.ToString("yyyy/MM/dd");
                    System.Diagnostics.Debug.WriteLine($"DateTextBox: 無効な日付のため今日の日付 {today:yyyy/MM/dd} を設定");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"日付調整中にエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 日付テキストボックスのKeyUpイベントハンドラー
        /// </summary>
        private void DateTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (sender is Wpf.Ui.Controls.TextBox dateTextBox)
                {
                    // カーソル位置に基づいて日付の特定の部分を調整
                    var cursorPosition = dateTextBox.CaretIndex;
                    var text = dateTextBox.Text;
                    
                    if (DateTime.TryParse(text, out DateTime currentDate))
                    {
                        DateTime newDate = currentDate;
                        bool dateChanged = false;

                        switch (e.Key)
                        {
                            case System.Windows.Input.Key.Up:
                                if (cursorPosition <= 4) // 年
                                {
                                    newDate = currentDate.AddYears(1);
                                    dateChanged = true;
                                }
                                else if (cursorPosition <= 7) // 月
                                {
                                    newDate = currentDate.AddMonths(1);
                                    dateChanged = true;
                                }
                                else // 日
                                {
                                    newDate = currentDate.AddDays(1);
                                    dateChanged = true;
                                }
                                break;

                            case System.Windows.Input.Key.Down:
                                if (cursorPosition <= 4) // 年
                                {
                                    newDate = currentDate.AddYears(-1);
                                    dateChanged = true;
                                }
                                else if (cursorPosition <= 7) // 月
                                {
                                    newDate = currentDate.AddMonths(-1);
                                    dateChanged = true;
                                }
                                else // 日
                                {
                                    newDate = currentDate.AddDays(-1);
                                    dateChanged = true;
                                }
                                break;
                        }

                        if (dateChanged)
                        {
                            dateTextBox.Text = newDate.ToString("yyyy/MM/dd");
                            // カーソル位置を復元
                            dateTextBox.CaretIndex = cursorPosition;
                            e.Handled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DateTextBox KeyUp処理中にエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// タイトルフィールドのキーイベントハンドラー
        /// </summary>
        private void TitleTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    // エンターキーで説明欄に移動
                    e.Handled = true;
                    DescriptionTextBox?.Focus();
                    System.Diagnostics.Debug.WriteLine("タイトルフィールド: エンターキーで説明欄に移動");
                }
                else if (e.Key == System.Windows.Input.Key.Tab)
                {
                    // タブキーで説明欄に移動
                    e.Handled = true;
                    DescriptionTextBox?.Focus();
                    System.Diagnostics.Debug.WriteLine("タイトルフィールド: タブキーで説明欄に移動");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"タイトルフィールド キーイベント処理中に例外: {ex.Message}");
            }
        }



        /// <summary>
        /// タイトルフィールドにフォーカスを設定する
        /// </summary>
        private void SetTitleFocus()
        {
            // UIの更新が完了してからフォーカスを設定
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (TitleTextBox != null)
                    {
                        TitleTextBox.Focus();
                        TitleTextBox.SelectAll(); // テキストを全選択
                        System.Diagnostics.Debug.WriteLine($"追加後編集モード: タイトルフィールドにフォーカス設定 '{ViewModel.SelectedItem?.Title ?? "null"}'");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("追加後編集モード: タイトルフィールドが見つかりません");
                        // フォールバック：DataGridにフォーカス
                        SetDataGridFocus();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"追加後編集モード: タイトルフィールドフォーカス設定中にエラー: {ex.Message}");
                    // エラーが発生した場合はDataGridにフォーカス
                    SetDataGridFocus();
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// DataGridにフォーカスを設定する
        /// </summary>
        public void SetDataGridFocus()
        {
            // UIの更新が完了してからフォーカスを設定（複数段階で試行）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TrySetFocusWithRetry(0);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// リトライ付きでフォーカスを設定する
        /// </summary>
        private void TrySetFocusWithRetry(int retryCount)
        {
            if (WbsDataGrid == null || ViewModel.SelectedItem == null)
            {
                WbsDataGrid?.Focus();
                System.Diagnostics.Debug.WriteLine($"編集モード: 基本条件不満足のためDataGrid全体にフォーカス設定");
                return;
            }

            try
            {
                // 選択されたアイテムの行を特定
                var selectedIndex = -1;
                for (int i = 0; i < WbsDataGrid.Items.Count; i++)
                {
                    if (WbsDataGrid.Items[i] == ViewModel.SelectedItem)
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                if (selectedIndex >= 0)
                {
                    // 選択された行を選択状態にする
                    WbsDataGrid.SelectedIndex = selectedIndex;
                    
                    // 選択された行にスクロール
                    WbsDataGrid.ScrollIntoView(WbsDataGrid.Items[selectedIndex]);
                    
                    // 少し待ってから行のコンテナを取得
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TrySetFocusOnRow(selectedIndex, retryCount);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    // 選択されたアイテムが見つからない場合
                    if (retryCount < 3)
                    {
                        // リトライ
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TrySetFocusWithRetry(retryCount + 1);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    else
                    {
                        WbsDataGrid.Focus();
                        System.Diagnostics.Debug.WriteLine($"編集モード: リトライ上限に達したためDataGrid全体にフォーカス設定");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"編集モード: フォーカス設定中にエラー: {ex.Message}");
                WbsDataGrid.Focus();
            }
        }

        /// <summary>
        /// 特定の行にフォーカスを設定する
        /// </summary>
        private void TrySetFocusOnRow(int rowIndex, int retryCount)
        {
            try
            {
                // 行のコンテナを取得
                var row = WbsDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
                
                if (row != null)
                {
                    // 行が見つかった場合、セルにフォーカスを設定
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TrySetFocusOnCell(row, rowIndex, retryCount);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    // 行が見つからない場合
                    if (retryCount < 3)
                    {
                        // リトライ
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            TrySetFocusWithRetry(retryCount + 1);
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    }
                    else
                    {
                        // リトライ上限に達した場合はDataGrid全体にフォーカス
                        WbsDataGrid.Focus();
                        System.Diagnostics.Debug.WriteLine($"編集モード: 行コンテナが見つからないためDataGrid全体にフォーカス設定");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"編集モード: 行フォーカス設定中にエラー: {ex.Message}");
                WbsDataGrid.Focus();
            }
        }

        /// <summary>
        /// 特定のセルにフォーカスを設定する
        /// </summary>
        private void TrySetFocusOnCell(DataGridRow row, int rowIndex, int retryCount)
        {
            try
            {
                // 最初のセル（Title列）にフォーカスを設定
                if (WbsDataGrid.Columns.Count > 0)
                {
                    var cell = WbsDataGrid.Columns[0].GetCellContent(row) as FrameworkElement;
                    if (cell != null)
                    {
                        cell.Focus();
                        System.Diagnostics.Debug.WriteLine($"編集モード: セルにフォーカス設定成功 '{ViewModel.SelectedItem?.Title ?? "null"}'");
                        return;
                    }
                }
                
                // セルが見つからない場合は行にフォーカス
                row.Focus();
                System.Diagnostics.Debug.WriteLine($"編集モード: 行にフォーカス設定成功 '{ViewModel.SelectedItem?.Title ?? "null"}'");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"編集モード: セルフォーカス設定中にエラー: {ex.Message}");
                
                // エラーが発生した場合は行にフォーカス
                try
                {
                    row.Focus();
                    System.Diagnostics.Debug.WriteLine($"編集モード: エラー後の行フォーカス設定成功 '{ViewModel.SelectedItem?.Title ?? "null"}'");
                }
                catch
                {
                    // 最終手段としてDataGrid全体にフォーカス
                    WbsDataGrid.Focus();
                    System.Diagnostics.Debug.WriteLine($"編集モード: 最終手段としてDataGrid全体にフォーカス設定");
                }
            }
        }
    }
}
