using System.Windows.Controls;
using RedmineClient.Models;
using RedmineClient.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

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

            // DataGridのLoadedイベントでも日付列の生成を試行
            this.Loaded += WbsPage_DataGridLoaded;

            // 祝日データを初期化（非同期で実行）
            _ = InitializeHolidayDataAsync();
        }

        /// <summary>
        /// 祝日データを初期化する
        /// </summary>
        private async Task InitializeHolidayDataAsync()
        {
            try
            {
                await RedmineClient.Services.HolidayService.ForceUpdateAsync();
                System.Diagnostics.Debug.WriteLine("祝日データの初期化が完了しました");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"祝日データの初期化に失敗: {ex.Message}");
            }
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("WbsPage_InitialLoaded: 開始");

            // 保存されたウィンドウサイズを復元
            RestoreWindowSize();

            // 年月の選択肢を初期化
            InitializeYearMonthOptions();

            // 日付列の生成を遅延実行（DataGridの完全な初期化を待つ）
            System.Diagnostics.Debug.WriteLine("WbsPage_InitialLoaded: 日付列の生成を遅延実行");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("WbsPage_InitialLoaded: 遅延実行で日付列の生成を開始");
                GenerateDateColumns();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            // このイベントは一度だけ実行
            this.Loaded -= WbsPage_InitialLoaded;
            System.Diagnostics.Debug.WriteLine("WbsPage_InitialLoaded: 完了");
        }

        private void WbsPage_DataGridLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("WbsPage_DataGridLoaded: DataGridが読み込まれました");
            GenerateDateColumns();
        }

        private void ScheduleStartYearMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 設定が変更されたらスケジュール表を再生成
            if (WbsDataGrid != null && WbsDataGrid.IsLoaded)
            {
                System.Diagnostics.Debug.WriteLine("ScheduleStartYearMonthComboBox_SelectionChanged: スケジュール開始年月が変更されました");
                
                // 選択された年月をViewModelに設定
                if (ScheduleStartYearMonthComboBox.SelectedItem is string selectedYearMonth)
                {
                    ViewModel.ScheduleStartYearMonth = selectedYearMonth;
                    System.Diagnostics.Debug.WriteLine($"選択された年月: {selectedYearMonth}");
                }
                
                // 祝日データを再初期化（色設定のため）
                _ = RefreshHolidayDataAsync();
                
                // 日付列を再生成
                GenerateDateColumns();
            }
        }

        /// <summary>
        /// 祝日データを再初期化する
        /// </summary>
        private async Task RefreshHolidayDataAsync()
        {
            try
            {
                await RedmineClient.Services.HolidayService.ForceUpdateAsync();
                System.Diagnostics.Debug.WriteLine("祝日データの再初期化が完了しました");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"祝日データの再初期化に失敗: {ex.Message}");
            }
        }

        public Task OnNavigatedToAsync()
        {
            System.Diagnostics.Debug.WriteLine("OnNavigatedToAsync: 開始");

            // ページ表示時にウィンドウサイズを復元
            RestoreWindowSize();

            // 日付列の生成も遅延実行で試行
            System.Diagnostics.Debug.WriteLine("OnNavigatedToAsync: 日付列の生成を遅延実行で試行");
            Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine("OnNavigatedToAsync: 遅延実行で日付列の生成を開始");
                GenerateDateColumns();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

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
                    System.Diagnostics.Debug.WriteLine($"スケジュール開始年月を保存しました: {ViewModel.ScheduleStartYearMonth}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"スケジュール開始年月の保存に失敗: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("AppConfig.Load()を実行しました");

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
                System.Diagnostics.Debug.WriteLine($"AppConfigから読み込んだ年月: '{savedYearMonth}'");
                System.Diagnostics.Debug.WriteLine($"年月選択肢の数: {yearMonthOptions.Count}");
                System.Diagnostics.Debug.WriteLine($"選択肢の最初の5件: {string.Join(", ", yearMonthOptions.Take(5))}");
                
                if (!string.IsNullOrEmpty(savedYearMonth) && yearMonthOptions.Contains(savedYearMonth))
                {
                    ScheduleStartYearMonthComboBox.SelectedItem = savedYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = savedYearMonth;
                    System.Diagnostics.Debug.WriteLine($"保存された年月を選択: {savedYearMonth}");
                }
                else
                {
                    var currentYearMonth = DateTime.Now.ToString("yyyy/MM");
                    ScheduleStartYearMonthComboBox.SelectedItem = currentYearMonth;
                    // ViewModelに直接値を設定（AppConfigのsetアクセサーを呼び出さない）
                    ViewModel.ScheduleStartYearMonth = currentYearMonth;
                    System.Diagnostics.Debug.WriteLine($"当月を選択: {currentYearMonth}");
                    
                    if (!string.IsNullOrEmpty(savedYearMonth))
                    {
                        System.Diagnostics.Debug.WriteLine($"保存された年月 '{savedYearMonth}' が選択肢に含まれていません");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("保存された年月がありません。当月を選択しました。");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"年月選択肢を初期化: {yearMonthOptions.Count}個の選択肢を設定");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"年月選択肢の初期化に失敗: {ex.Message}");
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
            var monthText = new TextBlock
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
            var dayText = new TextBlock
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
            var dayOfWeekText = new TextBlock
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
        private void GenerateDateColumns()
        {
            if (WbsDataGrid != null && WbsDataGrid.IsLoaded && WbsDataGrid.IsInitialized)
            {
                System.Diagnostics.Debug.WriteLine("GenerateDateColumns: 開始");

                // 既存のスケジュール列を削除
                var existingColumns = WbsDataGrid.Columns.Where(c => c.Header is StackPanel || c.Header is TextBlock).ToList();
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
                    startDate = new DateTime(DateTime.Now.Year, 1, 1); // デフォルトは1月1日
                }
                var endDate = startDate.AddMonths(2).AddDays(-1); // 2か月分

                System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 日付範囲 {startDate:yyyy/MM/dd} から {endDate:yyyy/MM/dd}");

                var currentDate = startDate;
                var columnCount = 0;
                var lastMonth = -1;

                // 固定列の数を取得（タスク名、ID、説明、開始日、終了日、進捗、ステータス、優先度、担当者）
                var fixedColumnCount = 9;

                while (currentDate <= endDate)
                {
                    // 月が変わったかどうかをチェック
                    var isMonthStart = currentDate.Month != lastMonth;
                    if (isMonthStart)
                    {
                        lastMonth = currentDate.Month;
                    }

                    var dateColumn = new DataGridTemplateColumn
                    {
                        Width = 30, // 幅を少し広げて3行表示に対応
                        Header = CreateThreeRowHeader(currentDate, isMonthStart),
                        IsReadOnly = true,
                        HeaderStyle = CreateDateHeaderStyle()
                    };

                    // 日付列用のセルテンプレート
                    var cellTemplate = new DataTemplate();
                    var factory = new FrameworkElementFactory(typeof(Border));

                    factory.SetValue(Border.WidthProperty, 30.0);
                    factory.SetValue(Border.HeightProperty, 20.0);
                    factory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.Gray);
                    factory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                    factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));

                    // 土曜日は青色、日曜日はピンク色
                    var backgroundBinding = new System.Windows.Data.Binding
                    {
                        Source = currentDate,
                        Converter = new RedmineClient.Helpers.DateToBackgroundColorConverter()
                    };
                    factory.SetValue(Border.BackgroundProperty, backgroundBinding);

                    cellTemplate.VisualTree = factory;
                    dateColumn.CellTemplate = cellTemplate;

                    // 固定列の後ろに日付列を追加
                    WbsDataGrid.Columns.Insert(fixedColumnCount + columnCount, dateColumn);
                    columnCount++;
                    currentDate = currentDate.AddDays(1);
                }

                System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 完了。{columnCount}個の日付列を追加しました。現在の総列数: {WbsDataGrid.Columns.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: DataGridが準備できていません。IsLoaded: {WbsDataGrid?.IsLoaded}, IsInitialized: {WbsDataGrid?.IsInitialized}");
            }
        }

        /// <summary>
        /// ウィンドウサイズを保存する
        /// </summary>
        private void SaveWindowSize()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    AppConfig.WindowWidth = window.Width;
                    AppConfig.WindowHeight = window.Height;
                    AppConfig.WindowLeft = window.Left;
                    AppConfig.WindowTop = window.Top;
                    AppConfig.WindowState = window.WindowState.ToString();
                    AppConfig.Save();
                    System.Diagnostics.Debug.WriteLine($"ウィンドウサイズを保存: {window.Width}x{window.Height}, 位置: ({window.Left}, {window.Top}), 状態: {window.WindowState}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ウィンドウサイズの保存に失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// ウィンドウサイズを復元する
        /// </summary>
        private void RestoreWindowSize()
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    // ウィンドウサイズを復元
                    if (AppConfig.WindowWidth > 0 && AppConfig.WindowHeight > 0)
                    {
                        window.Width = AppConfig.WindowWidth;
                        window.Height = AppConfig.WindowHeight;
                    }

                    // ウィンドウ位置を復元
                    if (AppConfig.WindowLeft >= 0 && AppConfig.WindowTop >= 0)
                    {
                        window.Left = AppConfig.WindowLeft;
                        window.Top = AppConfig.WindowTop;
                    }

                    // ウィンドウ状態を復元
                    if (Enum.TryParse<WindowState>(AppConfig.WindowState, out var windowState))
                    {
                        window.WindowState = windowState;
                    }

                    System.Diagnostics.Debug.WriteLine($"ウィンドウサイズを復元: {window.Width}x{window.Height}, 位置: ({window.Left}, {window.Top}), 状態: {window.WindowState}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ウィンドウサイズの復元に失敗: {ex.Message}");
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
            // ウィンドウサイズを保存
            SaveWindowSize();
            
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
