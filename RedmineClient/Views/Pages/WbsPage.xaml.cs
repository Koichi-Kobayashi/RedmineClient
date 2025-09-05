using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RedmineClient.Helpers;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Views.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.Views.Pages
{
    /// <summary>
    /// WbsPageV1.xaml の相互作用ロジック
    /// </summary>
    public partial class WbsPageV1 : INavigableView<WbsViewModel>, INavigationAware
    {
        public WbsViewModel ViewModel { get; }

        // 祝日データ初期化の重複実行を防ぐフラグ
        private bool _isHolidayDataInitializing = false;
        private bool _isHolidayDataRefreshing = false;

        static WbsPageV1()
        {
            // 静的コンストラクタでの非同期処理は問題を引き起こす可能性があるため削除
        }

        public WbsPageV1(WbsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            // 基本的なイベントハンドラーのみ登録
            this.Loaded += WbsPage_InitialLoaded;
            this.KeyDown += WbsPage_KeyDown;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // 日付変更の監視を有効化
            ViewModel.StartDateChangeWatching();

            // ViewModelの初期化処理を開始（完了を待機）
            _ = Task.Run(async () => 
            {
                try
                {
                    await ViewModel.InitializeViewModel();
                    
                    // 初期化完了後にDataGridのItemsSourceを設定
                    if (Application.Current != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            // 初期化完了後にDataGridのItemsSourceを設定
                            if (WbsDataGrid != null && WbsDataGrid.IsLoaded && ViewModel.FlattenedWbsItems?.Count > 0)
                            {
                                WbsDataGrid.ItemsSource = ViewModel.FlattenedWbsItems;
                            }
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // タスクがキャンセルされた場合は何もしない
                }
                catch (Exception ex)
                {
                    // ViewModelの初期化でエラーが発生した場合は無視
                }
            });

            // アプリケーション終了時の処理を追加（安全にチェック）
            if (Application.Current?.MainWindow != null)
            {
                Application.Current.MainWindow.Closing += MainWindow_Closing;
            }

            // DatePickerのプリロードを開始
            _ = Task.Run(async () => 
            {
                try
                {
                    await PreloadDatePickersAsync();
                }
                catch (TaskCanceledException)
                {
                    // タスクがキャンセルされた場合は何もしない
                }
                catch (Exception)
                {
                    // DatePickerプリロードでエラーが発生した場合は無視
                }
            });
        }

        /// <summary>
        /// 祝日データを初期化する
        /// </summary>
        private Task InitializeHolidayDataAsync()
        {
            // 重複実行を防ぐ
            if (_isHolidayDataInitializing) return Task.CompletedTask;
            
            try
            {
                _isHolidayDataInitializing = true;
                
                // 祝日データの初期化は非同期で実行し、エラーが発生しても処理を続行
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RedmineClient.Services.HolidayService.ForceUpdateAsync();
                    }
                    catch (Exception ex)
                    {
                        // 祝日データの初期化に失敗しても処理を続行
                    }
                    finally
                    {
                        _isHolidayDataInitializing = false;
                    }
                });
            }
            catch (Exception ex)
            {
                // 祝日データの初期化に失敗しても処理を続行
                _isHolidayDataInitializing = false;
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// ページレベルのキーボードショートカットを処理する
        /// </summary>
        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    // エスケープキーで選択状態をクリア
                    if (ViewModel.SelectedItem != null)
                    {
                        ViewModel.SelectedItem = null;
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// キーボードショートカットを処理する
        /// </summary>
        private void WbsPage_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    // Deleteキーで選択されたアイテムを削除
                    if (ViewModel.SelectedItem != null)
                    {
                        ViewModel.DeleteSelectedItemCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.F5:
                    // F5キーでデータを更新
                    if (ViewModel.RefreshRedmineDataCommand.CanExecute(null))
                    {
                        ViewModel.RefreshRedmineDataCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    // エスケープキーで選択状態をクリア
                    if (ViewModel.SelectedItem != null)
                    {
                        // ViewModelとDataGridの両方の選択状態をクリア
                        ViewModel.SelectedItem = null;
                        if (WbsDataGrid != null)
                        {
                            WbsDataGrid.UnselectAll();
                            WbsDataGrid.SelectedIndex = -1;
                            
                            // すべてのWbsItemのIsSelectedプロパティをfalseに設定
                            if (ViewModel.FlattenedWbsItems != null)
                            {
                                foreach (var item in ViewModel.FlattenedWbsItems)
                                {
                                    item.IsSelected = false;
                                }
                            }
                            
                            // フォーカスを安全にクリア
                            try
                            {
                                Keyboard.ClearFocus();
                            }
                            catch (Exception)
                            {
                                // フォーカス操作でエラーが発生した場合は無視
                            }
                        }
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// DataGridのキーボードショートカットを処理する
        /// </summary>
        private void WbsDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    // Deleteキーで選択されたアイテムを削除
                    if (ViewModel.SelectedItem != null)
                    {
                        ViewModel.DeleteSelectedItemCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Enter:
                    // Enterキーで選択されたアイテムを編集
                    if (ViewModel.SelectedItem != null)
                    {
                        ViewModel.EditItemCommand.Execute(ViewModel.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    // エスケープキーでDataGridの選択状態をクリア
                    if (sender is DataGrid dataGrid)
                    {
                        // 選択状態をクリア
                        dataGrid.UnselectAll();
                        dataGrid.SelectedIndex = -1;
                        ViewModel.SelectedItem = null;
                        
                        // すべてのWbsItemのIsSelectedプロパティをfalseに設定
                        if (ViewModel.FlattenedWbsItems != null)
                        {
                            foreach (var item in ViewModel.FlattenedWbsItems)
                            {
                                item.IsSelected = false;
                            }
                        }
                        
                        // フォーカスを安全にクリア
                        try
                        {
                            Keyboard.ClearFocus();
                        }
                        catch (Exception)
                        {
                            // フォーカス操作でエラーが発生した場合は無視
                        }
                        
                        e.Handled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// DataGridのサイズ変更時に矢印を再描画する
        /// </summary>


        /// <summary>
        /// DataGridのPreviewKeyDownイベントを処理する（より確実にキーイベントをキャッチ）
        /// </summary>
        private void WbsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    // Deleteキーで選択されたアイテムを削除
                    if (ViewModel.SelectedItem != null)
                    {
                        ViewModel.DeleteSelectedItemCommand.Execute(null);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    // エスケープキーでDataGridの選択状態をクリア（PreviewKeyDownで確実にキャッチ）
                    if (sender is DataGrid dataGrid)
                    {
                        // 選択状態をクリア
                        dataGrid.UnselectAll();
                        dataGrid.SelectedIndex = -1;
                        ViewModel.SelectedItem = null;
                        
                        // すべてのWbsItemのIsSelectedプロパティをfalseに設定
                        if (ViewModel.FlattenedWbsItems != null)
                        {
                            foreach (var item in ViewModel.FlattenedWbsItems)
                            {
                                item.IsSelected = false;
                            }
                        }
                        
                        // フォーカスを安全にクリア
                        try
                        {
                            Keyboard.ClearFocus();
                        }
                        catch (Exception)
                        {
                            // フォーカス操作でエラーが発生した場合は無視
                        }
                        
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 年月の選択肢を初期化
                InitializeYearMonthOptions();

                // プロジェクト選択の初期化
                if (ViewModel?.AvailableProjects != null && ViewModel.AvailableProjects.Any())
                {
                    ProjectComboBox.SelectedIndex = 0;
                }

                // スケジュール開始年月の初期化
                InitializeScheduleStartYearMonth();

                // DataGridの初期化処理を統合実行
                _ = InitializeDataGridAsync();

                // 祝日データを初期化（非同期で実行、エラーが発生しても続行）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitializeHolidayDataAsync();
                    }
                    catch (Exception)
                    {
                        // 祝日データ初期化エラーが発生した場合は無視して続行
                    }
                });

                // このイベントは一度だけ実行
                this.Loaded -= WbsPage_InitialLoaded;
            }
            catch (Exception)
            {
                // エラー処理は必要に応じて実装
            }
        }

        /// <summary>
        /// DataGridの初期化処理を統合実行する
        /// </summary>
        private async Task InitializeDataGridAsync()
        {
            try
            {
                // DataGridのイベントハンドラーを設定
                if (WbsDataGrid != null)
                {
                    WbsDataGrid.KeyDown += WbsDataGrid_KeyDown;
                    WbsDataGrid.PreviewKeyDown += WbsDataGrid_PreviewKeyDown;

                    WbsDataGrid.Loaded += WbsDataGrid_Loaded;

                }

                // ViewModelの初期化が完了するまで待機（タイムアウト付き）
                var waitCount = 0;
                while (ViewModel.FlattenedWbsItems == null || ViewModel.FlattenedWbsItems.Count == 0)
                {
                    await Task.Delay(100);
                    waitCount++;
                    // 無限ループを防ぐ（最大100回まで待機）
                    if (waitCount >= 100)
                    {
                        break;
                    }
                }

                // 初期化完了
            }
            catch (Exception ex)
            {
                // DataGrid初期化エラーが発生した場合は無視
            }
        }



        private void ScheduleStartYearMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 設定が変更されたらスケジュール表を再生成
            if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
            {
                // 選択された年月をViewModelに設定
                if (ScheduleStartYearMonthComboBox.SelectedItem is string selectedYearMonth)
                {
                    ViewModel.ScheduleStartYearMonth = selectedYearMonth;
                }

                // 祝日データを再初期化（色設定のため）
                _ = Task.Run(() => RefreshHolidayDataAsync());

                // 日付列を再生成
                _ = GenerateDateColumns();
            }
        }

        /// <summary>
        /// 祝日データを再初期化する
        /// </summary>
        private Task RefreshHolidayDataAsync()
        {
            // 重複実行を防ぐ
            if (_isHolidayDataRefreshing) return Task.CompletedTask;
            
            try
            {
                _isHolidayDataRefreshing = true;
                
                // 祝日データの再初期化は非同期で実行し、エラーが発生しても処理を続行
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RedmineClient.Services.HolidayService.ForceUpdateAsync();
                    }
                    catch (Exception ex)
                    {
                        // 祝日データの再初期化に失敗しても処理を続行
                    }
                    finally
                    {
                        _isHolidayDataRefreshing = false;
                    }
                });
            }
            catch (Exception ex)
            {
                // 祝日データの再初期化に失敗しても処理を続行
                _isHolidayDataRefreshing = false;
            }
            
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync()
        {
            // ページ遷移時は何もしない（DataGridのLoadedイベントで処理される）
            return Task.CompletedTask;
        }

        public virtual Task OnNavigatedTo()
        {
            return OnNavigatedToAsync();
        }

        public Task OnNavigatedFromAsync()
        {
            // ページ離脱時に開始月を保存
            SaveScheduleStartYearMonth();
            return Task.CompletedTask;
        }

        /// <summary>
        /// スケジュール開始年月を保存する
        /// </summary>
        private void SaveScheduleStartYearMonth()
        {
            try
            {
                if (ViewModel.ScheduleStartYearMonth != null)
                {
                    AppConfig.ScheduleStartYearMonth = ViewModel.ScheduleStartYearMonth;
                    AppConfig.Save();
                }
            }
            catch
            {
                // スケジュール開始年月の保存に失敗
            }
        }

        public virtual Task OnNavigatedFrom()
        {
            return OnNavigatedFromAsync();
        }

        /// <summary>
        /// 年月の選択肢を初期化する
        /// </summary>
        private void InitializeYearMonthOptions()
        {
            try
            {
                // 設定ファイルから値を読み込み
                AppConfig.Load();

                var yearMonthOptions = new List<string>();
                var currentDate = DateTime.Now.AddYears(-2); // 2年前から
                var endDate = DateTime.Now.AddYears(3); // 3年後まで

                while (currentDate <= endDate)
                {
                    yearMonthOptions.Add(currentDate.ToString("yyyy/MM"));
                    currentDate = currentDate.AddMonths(1);
                }

                ScheduleStartYearMonthComboBox.ItemsSource = yearMonthOptions;

                // 保存された年月がある場合はそれを選択、ない場合は当月を選択
                var savedYearMonth = AppConfig.ScheduleStartYearMonth;

                if (!string.IsNullOrEmpty(savedYearMonth) && yearMonthOptions.Contains(savedYearMonth))
                {
                    ScheduleStartYearMonthComboBox.SelectedItem = savedYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = savedYearMonth;
                }
                else
                {
                    var currentYearMonth = DateTime.Now.ToString("yyyy/MM");
                    ScheduleStartYearMonthComboBox.SelectedItem = currentYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = currentYearMonth;
                }

                // 今日の日付ライン表示設定を初期化
                ViewModel.ShowTodayLine = AppConfig.ShowTodayLine;
            }
            catch
            {
                // エラー処理は必要に応じて実装
            }
        }

        /// <summary>
        /// 曜日を日本語で取得する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>曜日の日本語表記</returns>
        private string GetDayOfWeek(DateTime date)
        {
            return date.DayOfWeek switch
            {
                DayOfWeek.Monday => "月",
                DayOfWeek.Tuesday => "火",
                DayOfWeek.Wednesday => "水",
                DayOfWeek.Thursday => "木",
                DayOfWeek.Friday => "金",
                DayOfWeek.Saturday => "土",
                DayOfWeek.Sunday => "日",
                _ => ""
            };
        }

        /// <summary>
        /// 3行ヘッダーを作成する（1行目：月、2行目：日、3行目：曜日）
        /// </summary>
        /// <param name="date">日付</param>
        /// <param name="isMonthStart">月の開始日かどうか</param>
        /// <returns>3行ヘッダー</returns>
        private object CreateThreeRowHeader(DateTime date, bool isMonthStart = false)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1行目：月（月の開始日のみ表示、それ以外は空）
            var monthText = new System.Windows.Controls.TextBlock
            {
                Text = isMonthStart ? $"{date:MM}月" : "",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20,
                Foreground = System.Windows.Media.Brushes.DarkBlue
            };
            stackPanel.Children.Add(monthText);

            // 2行目：日
            var dayText = new System.Windows.Controls.TextBlock
            {
                Text = $"{date:dd}",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20,
                Foreground = System.Windows.Media.Brushes.Black
            };
            stackPanel.Children.Add(dayText);

            // 3行目：曜日（土日祝の色を変更）
            var dayOfWeekText = new System.Windows.Controls.TextBlock
            {
                Text = GetDayOfWeek(date),
                FontSize = 12,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20,
                Foreground = GetDayOfWeekForeground(date)
            };
            stackPanel.Children.Add(dayOfWeekText);

            return stackPanel;
        }

        /// <summary>
        /// 曜日の前景色を取得する（土日祝の色を変更）
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>曜日の前景色</returns>
        private System.Windows.Media.Brush GetDayOfWeekForeground(DateTime date)
        {
            // 祝日は赤色（日曜日と同じ）
            if (RedmineClient.Services.HolidayService.IsHoliday(date))
                return System.Windows.Media.Brushes.Red;
            // 土曜日は青色
            else if (date.DayOfWeek == DayOfWeek.Saturday)
                return System.Windows.Media.Brushes.Blue;
            // 日曜日は赤色
            else if (date.DayOfWeek == DayOfWeek.Sunday)
                return System.Windows.Media.Brushes.Red;
            // 平日は濃いグレー
            else
                return System.Windows.Media.Brushes.DarkGray;
        }

        /// <summary>
        /// 日付ヘッダー用のスタイルを作成する
        /// </summary>
        /// <returns>日付ヘッダー用のスタイル</returns>
        private Style CreateDateHeaderStyle()
        {
            var style = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));

            // 前景色
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.ForegroundProperty,
                new DynamicResourceExtension("TextFillColorPrimaryBrush")));

            // 背景色
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BackgroundProperty,
                new DynamicResourceExtension("CardBackgroundFillColorSecondaryBrush")));

            // ボーダー色
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderBrushProperty,
                new DynamicResourceExtension("DividerStrokeColorDefaultBrush")));

            // ボーダー太さ
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.BorderThicknessProperty,
                new Thickness(0, 0, 1, 1)));

            // パディング
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.PaddingProperty,
                new Thickness(2, 2, 2, 2)));

            // フォントウェイト
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontWeightProperty,
                FontWeights.SemiBold));

            // フォントサイズ
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.FontSizeProperty,
                8.0));

            // 水平配置
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HorizontalContentAlignmentProperty,
                HorizontalAlignment.Center));

            // 垂直配置
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.VerticalContentAlignmentProperty,
                VerticalAlignment.Center));

            // 高さ（3行ヘッダー表示のため調整）
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HeightProperty,
                70.0));

            return style;
        }

        /// <summary>
        /// 日付列を生成する
        /// </summary>
        private bool _isGeneratingColumns = false;
        private CancellationTokenSource? _generateColumnsCancellation;

        private async Task GenerateDateColumns()
        {
            // 無限ループを防ぐためのフラグ
            if (_isGeneratingColumns)
            {
                return;
            }

            if (WbsDataGrid != null && WbsDataGrid.IsLoaded && WbsDataGrid.IsInitialized)
            {
                _isGeneratingColumns = true;
                _generateColumnsCancellation = new CancellationTokenSource();

                try
                {
                    // 他の処理が完了するまで少し待機
                    if (_generateColumnsCancellation != null)
                    {
                        await Task.Delay(200, _generateColumnsCancellation.Token);
                    }
                    
                                         // プログレスバーを表示
                     ViewModel.SetWbsLoading(true, true);
                     ViewModel.WbsProgressMessage = "日付列を生成中...";
                     ViewModel.WbsProgress = 0;
                    
                    // UIの更新を確実にするために少し待機
                    if (_generateColumnsCancellation != null)
                    {
                        await Task.Delay(100, _generateColumnsCancellation.Token);
                    }

                    // ItemsSourceを一時的にクリアして列の操作を可能にする
                    var currentItemsSource = WbsDataGrid.ItemsSource;
                    WbsDataGrid.ItemsSource = null;

                    try
                    {
                        // 既存のスケジュール列を削除
                        var existingColumns = WbsDataGrid.Columns.Where(c => c.Header is StackPanel || c.Header is System.Windows.Controls.TextBlock).ToList();
                        foreach (var column in existingColumns)
                        {
                            WbsDataGrid.Columns.Remove(column);
                        }

                        // 設定された年月の1日から開始
                        DateTime startDate;
                        if (DateTime.TryParseExact(ViewModel.ScheduleStartYearMonth, "yyyy/MM", null, System.Globalization.DateTimeStyles.None, out startDate))
                        {
                            startDate = startDate.AddDays(-startDate.Day + 1); // 月の1日に設定
                        }
                        else
                        {
                            startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); // デフォルトは今月の1日
                        }

                        // タスクの開始日が設定された開始日より前の場合は、タスクの開始日から表示
                        if (ViewModel.WbsItems != null && ViewModel.WbsItems.Count > 0)
                        {
                            var earliestStartDate = ViewModel.WbsItems.Min(item => item.StartDate);
                            if (earliestStartDate < startDate)
                            {
                                startDate = earliestStartDate.AddDays(-7); // タスク開始日の1週間前から表示
                            }
                        }

                        var endDate = startDate.AddMonths(2).AddDays(-1); // 2か月分表示（タスク期間をカバー）

                        var currentDate = startDate;
                        var lastMonth = -1;

                        // 固定列の数を取得（ID、タスク名、開始日、終了日、進捗、ステータス、優先度、担当者、先行・後続）
                        var fixedColumnCount = DraggableTaskBorder.TASK_INFO_COLUMN_COUNT;

                        // 日付列の総数を計算
                        var totalColumns = (int)((endDate - startDate).TotalDays) + 1;

                        for (int columnCount = 0; columnCount < totalColumns; columnCount++)
                        {
                            // キャンセレーションをチェック
                            if (_generateColumnsCancellation?.Token.IsCancellationRequested == true)
                            {
                                throw new OperationCanceledException();
                            }

                            // プログレスメッセージを更新（5列ごと、より細かく更新）
                            if (columnCount % 5 == 0)
                            {
                                // プログレス計算を修正：左端から正しく表示されるように
                                var progressPercent = (int)((double)columnCount / totalColumns * 50) + 50; // 50%から100%の範囲で計算
                                // 100%を超えないように制限
                                progressPercent = Math.Min(progressPercent, 100);
                                ViewModel.WbsProgress = progressPercent;
                                ViewModel.WbsProgressMessage = $"日付列を生成中... ({progressPercent}%)";

                                // UIの更新を確実にするために少し待機
                                if (_generateColumnsCancellation != null)
                                {
                                    await Task.Delay(5, _generateColumnsCancellation.Token);
                                }
                            }

                            var loopDate = startDate.AddDays(columnCount);
                            // 月が変わったかどうかをチェック
                            var isMonthStart = loopDate.Month != lastMonth;
                            if (isMonthStart)
                            {
                                lastMonth = loopDate.Month;
                            }

                            var dateColumn = new DataGridTemplateColumn
                            {
                                Width = 40, // 幅を少し広げて3行表示に対応
                                MinWidth = 40,
                                MaxWidth = 40,
                                Header = CreateThreeRowHeader(loopDate, isMonthStart),
                                IsReadOnly = true,
                                HeaderStyle = CreateDateHeaderStyle()
                            };

                            // 日付列用のセルテンプレート
                            var cellTemplate = new DataTemplate();
                            var factory = new FrameworkElementFactory(typeof(Grid));

                            // Gridの設定
                            factory.SetValue(Grid.WidthProperty, 30.0);
                            factory.SetValue(Grid.HeightProperty, 20.0);

                            // 背景用のBorder（土曜日は青色、日曜日はピンク色）
                            var backgroundFactory = new FrameworkElementFactory(typeof(Border));
                            backgroundFactory.SetValue(Border.WidthProperty, 30.0);
                            backgroundFactory.SetValue(Border.HeightProperty, 20.0);
                            backgroundFactory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.Gray);
                            backgroundFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                            backgroundFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
                            backgroundFactory.SetValue(Grid.ZIndexProperty, 0);

                            var backgroundBinding = new System.Windows.Data.Binding
                            {
                                Source = loopDate,
                                Converter = new RedmineClient.Helpers.DateToBackgroundColorConverter()
                            };
                            backgroundFactory.SetValue(Border.BackgroundProperty, backgroundBinding);

                            // 今日の日付ライン表示（設定が有効な場合のみ）
                            if (ViewModel.ShowTodayLine && loopDate.Date == DateTime.Today)
                            {
                                var todayLineFactory = new FrameworkElementFactory(typeof(Border));
                                todayLineFactory.SetValue(Border.WidthProperty, 30.0);
                                todayLineFactory.SetValue(Border.HeightProperty, 20.0);
                                todayLineFactory.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
                                todayLineFactory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.Red);
                                todayLineFactory.SetValue(Border.BorderThicknessProperty, new Thickness(3));
                                todayLineFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                                todayLineFactory.SetValue(Grid.ZIndexProperty, 3);
                                factory.AppendChild(todayLineFactory);
                            }

                            // タスク期間表示用のDraggableTaskBorder（開始日から終了日まで）
                            var taskPeriodFactory = new FrameworkElementFactory(typeof(DraggableTaskBorder));
                            taskPeriodFactory.SetValue(DraggableTaskBorder.WidthProperty, 30.0);
                            taskPeriodFactory.SetValue(DraggableTaskBorder.HeightProperty, 20.0);
                            taskPeriodFactory.SetValue(Grid.ZIndexProperty, 1);

                            // タスク期間の表示/非表示を制御（MultiBindingを使用）
                            var multiBinding = new System.Windows.Data.MultiBinding();
                            multiBinding.Converter = new TaskPeriodMultiBindingConverter(loopDate);

                            // 開始日（WbsItemから直接取得）
                            var startDateBinding = new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("StartDate")
                            };
                            multiBinding.Bindings.Add(startDateBinding);

                            // 終了日（WbsItemから直接取得）
                            var endDateBinding = new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("EndDate")
                            };
                            multiBinding.Bindings.Add(endDateBinding);

                            taskPeriodFactory.SetValue(DraggableTaskBorder.VisibilityProperty, multiBinding);

                            // 背景色：選択状態は強調色、それ以外は進捗色
                            var bgMulti = new System.Windows.Data.MultiBinding
                            {
                                Converter = SelectedProgressToBrushConverter.Instance
                            };
                            bgMulti.Bindings.Add(new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("Progress")
                            });
                            bgMulti.Bindings.Add(new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("IsSelected")
                            });
                            taskPeriodFactory.SetValue(DraggableTaskBorder.BackgroundProperty, bgMulti);

                            // 不透明度：選択時は常に1.0、非選択は土日祝で薄く
                            var opMulti = new System.Windows.Data.MultiBinding
                            {
                                Converter = SelectedDateOpacityConverter.Instance
                            };
                            opMulti.Bindings.Add(new System.Windows.Data.Binding
                            {
                                Source = loopDate
                            });
                            opMulti.Bindings.Add(new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("IsSelected")
                            });
                            taskPeriodFactory.SetValue(DraggableTaskBorder.OpacityProperty, opMulti);

                            // タスク情報を設定するためのイベントハンドラーを追加
                            var loadedEvent = new System.Windows.RoutedEventHandler((sender, args) =>
                            {
                                if (sender is DraggableTaskBorder border)
                                {
                                    var dataContext = border.DataContext as WbsItem;
                                    if (dataContext != null)
                                    {
                                        // 現在の列の実際のインデックスを計算
                                        // columnCountは日付列のインデックス（0から始まる）
                                        // 固定列9個 + 日付列の位置（columnCount）
                                        var actualColumnIndex = fixedColumnCount + columnCount;

                                        // 列0の日付（表示開始日）を基準として渡す
                                        // これにより、各列での日付計算が正しく行われる
                                        // totalColumnsには固定列9個 + 日付列の総数を渡す必要がある
                                        var actualTotalColumns = fixedColumnCount + totalColumns;

                                        // 境界条件の確認：actualColumnIndexがactualTotalColumnsの範囲内にあることを確認
                                        if (actualColumnIndex >= actualTotalColumns)
                                        {
                                            actualColumnIndex = actualTotalColumns - 1;
                                        }

                                        border.SetTaskInfo(dataContext, startDate, loopDate, actualColumnIndex, actualTotalColumns);
                                    }
                                }
                            });
                            taskPeriodFactory.AddHandler(DraggableTaskBorder.LoadedEvent, loadedEvent);

                            // Gridに要素を追加
                            factory.AppendChild(backgroundFactory);
                            factory.AppendChild(taskPeriodFactory);

                            cellTemplate.VisualTree = factory;
                            dateColumn.CellTemplate = cellTemplate;

                            // 固定列の後ろに日付列を追加
                            WbsDataGrid.Columns.Insert(fixedColumnCount + columnCount, dateColumn);
                        }

                        // ItemsSourceを復元
                        WbsDataGrid.ItemsSource = currentItemsSource;
                    }
                    catch (OperationCanceledException)
                    {
                        // キャンセルされた場合はItemsSourceを復元して終了
                        WbsDataGrid.ItemsSource = currentItemsSource;
                        return;
                    }
                    catch (Exception)
                    {
                        // エラーが発生した場合でもItemsSourceを復元
                        WbsDataGrid.ItemsSource = currentItemsSource;
                        throw;
                    }

                    // 完了メッセージを表示
                    ViewModel.WbsProgress = 100;
                    ViewModel.WbsProgressMessage = "日付列の生成が完了しました";

                    // プログレスバーが100%に達したことを確認できるように少し待機
                    if (_generateColumnsCancellation != null)
                    {
                        await Task.Delay(1000, _generateColumnsCancellation.Token); // 完了メッセージを確認できるように待機
                    }

                    // プログレスバーを非表示にする
                    ViewModel.SetWbsLoading(false);
                    ViewModel.WbsProgress = 0;
                    ViewModel.WbsProgressMessage = string.Empty;
                }
                catch (OperationCanceledException)
                {
                    // キャンセルされた場合でも、プログレスバーを非表示にする
                    ViewModel.SetWbsLoading(false);
                    ViewModel.WbsProgress = 0;
                    ViewModel.WbsProgressMessage = string.Empty;
                }
                finally
                {
                    _isGeneratingColumns = false;
                    _generateColumnsCancellation?.Dispose();
                    _generateColumnsCancellation = null;
                }
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void WbsPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // プロジェクト選択の初期化
                if (ViewModel?.AvailableProjects != null && ViewModel.AvailableProjects.Any())
                {
                    ProjectComboBox.SelectedIndex = 0;
                }

                // スケジュール開始年月の初期化
                InitializeScheduleStartYearMonth();
            }
            catch (Exception)
            {
                // エラー処理は必要に応じて実装
            }
        }

        private void InitializeScheduleStartYearMonth()
        {
            try
            {
                // 設定ファイルから値を読み込み
                AppConfig.Load();

                var yearMonthOptions = new List<string>();
                var currentDate = DateTime.Now.AddYears(-2); // 2年前から
                var endDate = DateTime.Now.AddYears(3); // 3年後まで

                while (currentDate <= endDate)
                {
                    yearMonthOptions.Add(currentDate.ToString("yyyy/MM"));
                    currentDate = currentDate.AddMonths(1);
                }

                ScheduleStartYearMonthComboBox.ItemsSource = yearMonthOptions;

                var savedYearMonth = AppConfig.ScheduleStartYearMonth;

                if (!string.IsNullOrEmpty(savedYearMonth) && yearMonthOptions.Contains(savedYearMonth))
                {
                    ScheduleStartYearMonthComboBox.SelectedItem = savedYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = savedYearMonth;
                }
                else
                {
                    var currentYearMonth = DateTime.Now.ToString("yyyy/MM");
                    ScheduleStartYearMonthComboBox.SelectedItem = currentYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = currentYearMonth;
                }
            }
            catch
            {
                // エラー処理は必要に応じて実装
            }
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // スケジュール開始年月を保存
            SaveScheduleStartYearMonth();
        }

        /// <summary>
        /// コンテキストメニューが開かれた際のイベントハンドラー
        /// </summary>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu && contextMenu.PlacementTarget is FrameworkElement target)
            {
                // プレースメントターゲットからWbsItemのDataContextを取得
                var wbsItem = target.DataContext as WbsItem;

                if (wbsItem == null) return;

                // コンテキストメニューの各MenuItemにViewModelとWbsItemの両方を設定
                foreach (System.Windows.Controls.MenuItem menuItem in contextMenu.Items.OfType<System.Windows.Controls.MenuItem>())
                {
                    // MenuItemのDataContextはWbsItemのまま保持
                    menuItem.DataContext = wbsItem;

                    // CommandはViewModelから取得
                    switch (menuItem.Header?.ToString())
                    {
                        case "サブタスク追加":
                            menuItem.Command = ViewModel.AddChildItemCommand;
                            menuItem.CommandParameter = wbsItem;
                            // 親タスクでない場合は無効化
                            menuItem.IsEnabled = wbsItem.IsParentTask;
                            break;

                        case "編集":
                            menuItem.Command = ViewModel.EditItemCommand;
                            menuItem.CommandParameter = wbsItem;
                            break;

                        case "削除":
                            menuItem.Command = ViewModel.DeleteItemCommand;
                            menuItem.CommandParameter = wbsItem;
                            break;
                    }
                }
            }
        }







        /// <summary>
        /// ViewModelのプロパティ変更時のイベントハンドラー
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(ViewModel.SelectedProject))
                {
                    // プロジェクトが変更された場合、Redmineデータを自動的に読み込む
                    if (ViewModel.SelectedProject != null && ViewModel.IsRedmineConnected)
                    {
                        // 非同期版を使用してUIをブロックしないようにする
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ViewModel.LoadRedmineDataAsync();
                            }
                            catch (Exception)
                            {
                                // エラー処理は必要に応じて実装
                            }
                        });
                    }
                }
                else if (e.PropertyName == nameof(ViewModel.AvailableProjects))
                {
                    // プロジェクト一覧が更新された場合の処理
                }
                else if (e.PropertyName == nameof(ViewModel.ShowTodayLine))
                {
                    // 今日の日付ライン表示設定が変更された場合、スケジュール表を再生成
                    if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
                    {
                        _ = GenerateDateColumns();
                    }
                }
                else if (e.PropertyName == nameof(ViewModel.FlattenedWbsItems))
                {
                    // FlattenedWbsItemsが変更された場合の処理
                    if (ViewModel.FlattenedWbsItems?.Count > 0)
                    {
                        // DataGridのItemsSourceを更新
                        if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
                        {
                            WbsDataGrid.ItemsSource = ViewModel.FlattenedWbsItems;
                        }
                    }
                }
                else if (e.PropertyName == nameof(ViewModel.WbsItems))
                {
                    // WbsItemsが変更された場合の処理
                }


            }
            catch (Exception)
            {
                // エラー処理は必要に応じて実装
            }
        }

        /// <summary>
        /// 展開/折りたたみテキストのマウスクリックイベントハンドラー（軽量化版）
        /// </summary>
        private void ExpansionText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border expansionBorder && expansionBorder.DataContext is WbsItem item)
                {
                    // ViewModelの更新処理を実行（ViewModel内でIsExpandedを切り替える）
                    ViewModel.ToggleExpansion(item);

                    // UIを即座に更新
                    WbsDataGrid.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                // エラーが発生した場合は無視
            }
        }



        /// <summary>
        /// DataGridのLoadedイベントハンドラー
        /// </summary>
        private async void WbsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // このイベントは一度だけ実行
                dataGrid.Loaded -= WbsDataGrid_Loaded;
                
                // ViewModelの初期化が完了するまで待機（タイムアウト付き）
                var waitCount = 0;
                while (ViewModel.FlattenedWbsItems == null || ViewModel.FlattenedWbsItems.Count == 0)
                {
                    await Task.Delay(100);
                    waitCount++;
                    // 無限ループを防ぐ（最大100回まで待機）
                    if (waitCount >= 100)
                    {
                        // タイムアウト後の処理：空のリストでもItemsSourceを設定
                        if (ViewModel.FlattenedWbsItems == null)
                        {
                            ViewModel.FlattenedWbsItems = new ObservableCollection<WbsItem>();
                        }
                        
                        // 空のリストでもItemsSourceを設定してDataGridを初期化
                        dataGrid.ItemsSource = ViewModel.FlattenedWbsItems;
                        break;
                    }
                }
                
                // DataGridのItemsSourceを設定（これが重要！）
                if (ViewModel.FlattenedWbsItems != null && ViewModel.FlattenedWbsItems.Count > 0)
                {
                    dataGrid.ItemsSource = ViewModel.FlattenedWbsItems;
                }
                
                // Redmineデータの読み込みと日付カラム生成を実行
                if (ViewModel.SelectedProject != null && ViewModel.IsRedmineConnected)
                {
                    try
                    {
                        // プログレスバーを表示
                        ViewModel.SetWbsLoading(true, true);
                         ViewModel.WbsProgressMessage = "Redmineデータを読み込み中...";
                         ViewModel.WbsProgress = 0;
                        
                        // Redmineデータを読み込み
                        await ViewModel.LoadRedmineDataAsync();
                        
                        // データ読み込み完了後、ItemsSourceを再設定（データが更新された可能性があるため）
                        if (ViewModel.FlattenedWbsItems != null && ViewModel.FlattenedWbsItems.Count > 0)
                        {
                            dataGrid.ItemsSource = ViewModel.FlattenedWbsItems;
                        }
                        
                        // 日付カラムを生成
                        ViewModel.WbsProgressMessage = "日付カラムを生成中...";
                        ViewModel.WbsProgress = 50;
                        
                        if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
                        {
                            _ = GenerateDateColumns();
                        }
                    }
                    catch (Exception)
                    {
                        // エラーが発生した場合でも、日付カラムは生成
                        if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
                        {
                            _ = GenerateDateColumns();
                        }
                    }
                }
                else
                {
                    // プロジェクトが選択されていない場合でも、日付カラムは生成
                    if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
                    {
                        _ = GenerateDateColumns();
                    }
                }
                
                // DataGridの完全なレンダリングを待つ
                await Dispatcher.Yield();
                
                // DataGridのアイテムが完全にレンダリングされるまで待機
                var renderWaitCount = 0;
                while (dataGrid.Items.Count > 0 && renderWaitCount < 50)
                {
                    // DataGridのレンダリングが完了するまで少し待機
                    await Task.Delay(50);
                    renderWaitCount++;
                }
                
                // プログレスバーの非表示はGenerateDateColumns()内で行われるため、
                // ここでは何もしない
            }
        }



        /// <summary>
        /// 静的DatePickerのプリロードを実行する
        /// </summary>
        private static Task StaticPreloadDatePickersAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    // アプリケーションが起動していない場合は待機
                    while (Application.Current == null)
                    {
                        await Task.Delay(100);
                    }

                    // UIスレッドでDatePickerの初期化を実行
                    if (Application.Current != null)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                // 基本的なDatePickerを初期化してカレンダーコンポーネントをプリロード
                                var datePicker = new DatePicker
                                {
                                    SelectedDate = DateTime.Today,
                                    FirstDayOfWeek = DayOfWeek.Monday,
                                    IsTodayHighlighted = true,
                                    SelectedDateFormat = DatePickerFormat.Short
                                };

                                // カレンダーを表示してプリロード
                                datePicker.IsDropDownOpen = true;

                                // 少し待ってから閉じる
                                _ = Task.Delay(150).ContinueWith(_ =>
                                {
                                    if (Application.Current != null)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            datePicker.IsDropDownOpen = false;
                                        });
                                    }
                                });
                            }
                            catch (Exception)
                            {
                                // 静的DatePickerプリロード中にエラーが発生した場合は無視
                            }
                        });
                    }
                }
                catch (Exception)
                {
                    // 静的DatePickerプリロードでエラーが発生した場合は無視
                }
            });
        }

        /// <summary>
        /// DatePickerのプリロードを実行する
        /// </summary>
        private Task PreloadDatePickersAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    // アプリケーションが終了している場合は何もしない
                    if (Application.Current == null) return;

                    // UIスレッドでDatePickerの初期化を実行
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // 複数のDatePickerをプリロードして、カレンダーコンポーネントを初期化
                            var datePickers = new List<DatePicker>();

                            // 現在の日付、前月、翌月の日付でプリロード
                            var dates = new[]
                            {
                                DateTime.Today,
                                DateTime.Today.AddMonths(-1),
                                DateTime.Today.AddMonths(1)
                            };

                            foreach (var date in dates)
                            {
                                var datePicker = new DatePicker
                                {
                                    SelectedDate = date,
                                    FirstDayOfWeek = DayOfWeek.Monday,
                                    IsTodayHighlighted = true,
                                    DisplayDateStart = new DateTime(1900, 1, 1),
                                    DisplayDateEnd = new DateTime(2100, 12, 31),
                                    SelectedDateFormat = DatePickerFormat.Short
                                };

                                datePickers.Add(datePicker);
                            }

                            // カレンダーを表示してプリロード
                            foreach (var datePicker in datePickers)
                            {
                                datePicker.IsDropDownOpen = true;
                            }

                            // 少し待ってから閉じる
                            _ = Task.Delay(200).ContinueWith(_ =>
                            {
                                if (Application.Current != null)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        foreach (var datePicker in datePickers)
                                        {
                                            datePicker.IsDropDownOpen = false;
                                        }
                                    });
                                }
                            });
                        }
                        catch (Exception)
                        {
                            // DatePickerプリロード中にエラーが発生した場合は無視
                        }
                    });
                }
                catch (Exception)
                {
                    // DatePickerプリロードでエラーが発生した場合は無視
                }
            });
        }

        /// <summary>
        /// アプリケーション終了時の処理
        /// </summary>
        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // アプリケーションが既に終了している場合は何もしない
                if (Application.Current == null) return;

                // 日付列生成処理をキャンセル
                _generateColumnsCancellation?.Cancel();

                // 未保存の変更がある場合は保存処理を実行
                if (ViewModel != null)
                {
                    // 未保存の変更を保存
                    await ViewModel.SavePendingChangesAsync();
                }
            }
            catch (Exception)
            {
                // エラー処理は必要に応じて実装
            }
        }

        // 日付変更前の値を保存する辞書
        private readonly Dictionary<DatePicker, (DateTime StartDate, DateTime EndDate)> _dateChangeCache = new();

        /// <summary>
        /// DatePickerがフォーカスを取得した時の処理
        /// </summary>
        private void DatePicker_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DatePicker datePicker && datePicker.DataContext is WbsItem task)
            {
                // 現在の日付値をキャッシュに保存
                _dateChangeCache[datePicker] = (task.StartDate, task.EndDate);
            }
        }

        /// <summary>
        /// DatePickerの日付変更イベントを処理する
        /// </summary>
        /// <param name="sender">イベントの送信元</param>
        /// <param name="e">イベント引数</param>
        private async void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is DatePicker datePicker && datePicker.DataContext is WbsItem task)
                {
                    // 新規登録時は更新処理を実行しない
                    if (int.TryParse(task.Id, out int idValue) && idValue <= 0)
                    {
                        return;
                    }

                    // 日付が変更された場合のみ処理
                    if (datePicker.SelectedDate.HasValue)
                    {
                        var newDate = datePicker.SelectedDate.Value;

                        // キャッシュから変更前の値を取得
                        var oldValues = _dateChangeCache.GetValueOrDefault(datePicker, (task.StartDate, task.EndDate));
                        var oldStartDate = oldValues.StartDate;
                        var oldEndDate = oldValues.EndDate;

                        // タグを使用して列を識別
                        string? columnType = datePicker.Tag?.ToString();

                        // 日付変更の検出と処理
                        bool dateChanged = false;
                        DateTime originalStartDate = oldStartDate;
                        DateTime originalEndDate = oldEndDate;

                        if (columnType == "StartDate")
                        {
                            if (newDate != oldStartDate)
                            {
                                originalStartDate = oldStartDate;
                                task.StartDate = newDate;
                                dateChanged = true;
                            }
                        }
                        else if (columnType == "EndDate")
                        {
                            if (newDate != oldEndDate)
                            {
                                originalEndDate = oldEndDate;
                                task.EndDate = newDate;
                                dateChanged = true;
                            }
                        }

                        // 日付が変更された場合のみ更新処理を実行
                        if (dateChanged)
                        {
                            // ViewModelの更新処理を実行
                            await ViewModel.UpdateTaskScheduleAsync(task, originalStartDate, originalEndDate);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // DatePicker日付変更処理でエラーが発生した場合は無視
            }
        }

        #region ドラッグ&ドロップ機能（タスクの順番変更）

        // このセクションではタスクのドラッグ&ドロップによる順番変更機能を実装しています。
        // 使い方：
        // 1. タスク名のセルを左クリックしてドラッグ開始
        // 2. 同じ階層レベルの他のタスクの上にドロップ
        // 3. 自動的にタスクの順番が変更されます
        //
        // 制限事項：
        // - 同じ階層レベルで同じ親を持つタスク間でのみ移動可能
        // - 異なる階層や親が異なるタスクへの移動は無効

        private Point _dragStartPoint;
        private bool _isDragging = false;

        /// <summary>
        /// DataGridのドロップイベント
        /// </summary>
        private void WbsDataGrid_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(WbsItem)))
                {
                    var sourceItem = e.Data.GetData(typeof(WbsItem)) as WbsItem;
                    if (sourceItem != null)
                    {
                        // ドロップ位置からターゲットアイテムを取得
                        var dropPoint = e.GetPosition(WbsDataGrid);
                        var targetRow = GetRowAtPoint(dropPoint);

                        if (targetRow != null && targetRow.DataContext is WbsItem targetItem)
                        {
                            // 同じ階層レベルで同じ親を持つタスク間での順番変更のみ許可
                            if (sourceItem.Level == targetItem.Level && sourceItem.Parent == targetItem.Parent)
                            {
                                ViewModel.ReorderTask(sourceItem, targetItem);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // DataGridドロップ処理でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// DataGridのドラッグエンターイベント
        /// </summary>
        private void WbsDataGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(WbsItem)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// DataGridのドラッグリーブイベント
        /// </summary>
        private void WbsDataGrid_DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// DataGrid行のドロップイベント
        /// </summary>
        private void DataGridRow_Drop(object sender, DragEventArgs e)
        {
            // 重複実行を防ぐため、ここでは処理しない
            // WbsDataGrid_Dropで一元処理される
            e.Handled = true;
        }

        /// <summary>
        /// DataGrid行のドラッグエンターイベント
        /// </summary>
        private void DataGridRow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(WbsItem)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// DataGrid行のドラッグリーブイベント
        /// </summary>
        private void DataGridRow_DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// タスク境界のドロップイベント
        /// </summary>
        private void TaskBorder_Drop(object sender, DragEventArgs e)
        {
            // 重複実行を防ぐため、ここでは処理しない
            // WbsDataGrid_Dropで一元処理される
            e.Handled = true;
        }

        /// <summary>
        /// タスク境界のドラッグエンターイベント
        /// </summary>
        private void TaskBorder_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(WbsItem)))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /// <summary>
        /// タスク境界のドラッグリーブイベント
        /// </summary>
        private void TaskBorder_DragLeave(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// 指定されたポイントにある行を取得する
        /// </summary>
        private DataGridRow GetRowAtPoint(Point point)
        {
            var element = WbsDataGrid.InputHitTest(point) as DependencyObject;
            while (element != null && !(element is DataGridRow))
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as DataGridRow;
        }

        /// <summary>
        /// タスク境界のマウス左ボタンダウンイベント（ドラッグ開始）
        /// </summary>
        private void TaskBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is WbsItem wbsItem)
                {
                    _dragStartPoint = e.GetPosition(border);
                    _isDragging = false;
                    border.CaptureMouse();
                }
            }
            catch (Exception)
            {
                // タスク境界MouseLeftButtonDownでエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// タスク境界のマウス移動イベント（ドラッグ判定）
        /// </summary>
        private void TaskBorder_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && sender is Border border && border.DataContext is WbsItem wbsItem)
                {
                    var currentPosition = e.GetPosition(border);
                    var distance = Point.Subtract(currentPosition, _dragStartPoint).Length;

                    // 一定距離移動したらドラッグ開始
                    if (distance > 10 && !_isDragging)
                    {
                        _isDragging = true;
                        var dragData = new DataObject(typeof(WbsItem), wbsItem);
                        DragDrop.DoDragDrop(border, dragData, DragDropEffects.Move);
                    }
                }
            }
            catch (Exception)
            {
                // タスク境界MouseMoveでエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// タスク境界のマウス左ボタンアップイベント（ドラッグ終了）
        /// </summary>
        private void TaskBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border)
                {
                    border.ReleaseMouseCapture();
                    _isDragging = false;
                }
            }
            catch (Exception)
            {
                // タスク境界MouseLeftButtonUpでエラーが発生した場合は無視
            }
        }

        #endregion ドラッグ&ドロップ機能（タスクの順番変更）

        #region 先行・後続の依存関係設定（ドラッグ&ドロップ）

        // このセクションでは先行・後続の関係性をドラッグ&ドロップで設定する機能を実装しています。
        // 使い方：
        // 1. タスク名のセルを左クリックしてドラッグ開始
        // 2. 先行・後続列の"D&D"エリアにドロップ
        // 3. 先行関係または後続関係が自動的に設定されます
        //
        // 制限事項：
        // - 循環参照が発生する場合は設定を拒否
        // - 自分自身への依存関係は設定不可

        /// <summary>
        /// 先行・後続列のドロップイベント
        /// </summary>
        private void DependencyDrop_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(WbsItem)))
                {
                    var sourceItem = e.Data.GetData(typeof(WbsItem)) as WbsItem;
                    if (sourceItem != null && sender is Border border && border.DataContext is WbsItem targetItem)
                    {
                        // 自分自身への依存関係は設定不可
                        if (sourceItem == targetItem) return;

                        // 先行・後続の関係性を設定
                        // タスクAをタスクBにドロップしたとき、タスクAがタスクBの先行タスクになる
                        ViewModel.SetDependency(sourceItem, targetItem, true);

                        // 背景色をリセット
                        border.Background = System.Windows.Media.Brushes.Transparent;
                        border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                        border.BorderThickness = new Thickness(1);

                        // UIを強制的に更新
                        WbsDataGrid.Items.Refresh();

                        // Redmineへの更新を非同期で実行
                    }
                }
            }
            catch (Exception)
            {
                // 依存関係ドロップ処理でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 先行・後続列のドラッグエンターイベント
        /// </summary>
        private void DependencyDrop_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(typeof(WbsItem)))
                {
                    var sourceItem = e.Data.GetData(typeof(WbsItem)) as WbsItem;
                    if (sourceItem != null && sender is Border border && border.DataContext is WbsItem targetItem)
                    {
                        // 自分自身への依存関係は設定不可
                        if (sourceItem == targetItem)
                        {
                            e.Effects = DragDropEffects.None;
                        }
                        else
                        {
                            e.Effects = DragDropEffects.Copy;
                            // 先行関係設定の視覚的フィードバック
                            border.Background = System.Windows.Media.Brushes.LightGreen;
                            border.BorderBrush = System.Windows.Media.Brushes.Green;
                            border.BorderThickness = new Thickness(2);
                        }
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
            catch (Exception)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 先行・後続列のドラッグリーブイベント
        /// </summary>
        private void DependencyDrop_DragLeave(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is Border border)
                {
                    // 視覚的フィードバックをリセット
                    border.Background = System.Windows.Media.Brushes.Transparent;
                    border.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    border.BorderThickness = new Thickness(1);
                }
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
            catch (Exception)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        #endregion 先行・後続の依存関係設定（ドラッグ&ドロップ）

        #region ID列ダブルクリック機能

        /// <summary>
        /// ID列のマウス左ボタンダウンイベントハンドラー（ダブルクリック判定用）
        /// </summary>
        private void IdColumn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is WbsItem wbsItem)
                {
                    // ダブルクリックの判定
                    var currentTime = DateTime.Now;
                    var lastClickTime = GetLastClickTime(border);
                    
                    if (lastClickTime.HasValue && (currentTime - lastClickTime.Value).TotalMilliseconds < 500)
                    {
                        // ダブルクリックと判定
                        HandleIdColumnDoubleClick(wbsItem);
                        
                        // クリック時間をリセット
                        SetLastClickTime(border, null);
                    }
                    else
                    {
                        // シングルクリックの場合は時間を記録
                        SetLastClickTime(border, currentTime);
                    }
                }
            }
            catch (Exception ex)
            {
                // ID列マウスクリックエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// ID列のダブルクリック処理
        /// </summary>
        private void HandleIdColumnDoubleClick(WbsItem wbsItem)
        {
            try
            {
                // IDが有効な場合のみ処理
                if (!string.IsNullOrEmpty(wbsItem.Id) && wbsItem.Id != "0" && wbsItem.Id != "-1")
                {
                    // RedmineのURLを構築
                    var redmineUrl = BuildRedmineUrl(wbsItem.Id);
                    
                    // ブラウザでURLを開く
                    OpenUrlInBrowser(redmineUrl);
                }
            }
            catch (Exception ex)
            {
                // ID列ダブルクリック処理エラーが発生した場合は無視
            }
        }

        // ダブルクリック判定用の辞書
        private readonly Dictionary<Border, DateTime> _lastClickTimes = new();

        /// <summary>
        /// 最後のクリック時間を取得
        /// </summary>
        private DateTime? GetLastClickTime(Border border)
        {
            return _lastClickTimes.TryGetValue(border, out var time) ? time : null;
        }

        /// <summary>
        /// 最後のクリック時間を設定
        /// </summary>
        private void SetLastClickTime(Border border, DateTime? time)
        {
            if (time.HasValue)
            {
                _lastClickTimes[border] = time.Value;
            }
            else
            {
                _lastClickTimes.Remove(border);
            }
        }

        /// <summary>
        /// RedmineのURLを構築する
        /// </summary>
        /// <param name="issueId">チケットID</param>
        /// <returns>RedmineのURL</returns>
        private string BuildRedmineUrl(string issueId)
        {
            try
            {
                // 設定からRedmineHostを取得
                var redmineHost = AppConfig.RedmineHost;
                
                if (string.IsNullOrEmpty(redmineHost))
                {
                    throw new InvalidOperationException("RedmineHostが設定されていません。設定ページでRedmineHostを設定してください。");
                }

                // URLの形式を正規化
                var normalizedHost = redmineHost.TrimEnd('/');
                if (!normalizedHost.StartsWith("http://") && !normalizedHost.StartsWith("https://"))
                {
                    normalizedHost = "https://" + normalizedHost;
                }

                // チケットURLを構築
                return $"{normalizedHost}/issues/{issueId}";
            }
            catch (Exception ex)
            {
                // Redmine URL構築エラーが発生した場合は再スロー
                throw;
            }
        }

        /// <summary>
        /// ブラウザでURLを開く
        /// </summary>
        /// <param name="url">開くURL</param>
        private void OpenUrlInBrowser(string url)
        {
            try
            {
                // Process.Startを使用してブラウザでURLを開く
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };

                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                // エラーメッセージをユーザーに表示
                System.Windows.MessageBox.Show(
                    $"ブラウザでURLを開けませんでした: {ex.Message}",
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        #endregion ID列ダブルクリック機能

        #region マウスホイールイベント処理

        /// <summary>
        /// DataGridのマウスホイールイベントハンドラー
        /// </summary>
        private void WbsDataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // DataGrid内のScrollViewerを取得
                var scrollViewer = FindVisualChild<ScrollViewer>(dataGrid);
                if (scrollViewer != null)
                {
                    // マウスホイールの回転量に基づいてスクロール
                    var delta = e.Delta;
                    var currentOffset = scrollViewer.VerticalOffset;
                    var newOffset = currentOffset - (delta / 120.0) * 20; // 20ピクセルずつスクロール
                    
                    // スクロール範囲内に制限
                    newOffset = Math.Max(0, Math.Min(newOffset, scrollViewer.ScrollableHeight));
                    
                    scrollViewer.ScrollToVerticalOffset(newOffset);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// ビジュアルツリーからScrollViewerを検索するヘルパーメソッド
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        #endregion マウスホイールイベント処理
    }
}