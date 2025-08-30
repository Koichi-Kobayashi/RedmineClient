using System.Windows.Controls;
using System.Windows.Input;
using RedmineClient.Helpers;
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



            // 初期化完了後にタスク詳細の幅を設定
            this.Loaded += WbsPage_InitialLoaded;

            // DataGridのLoadedイベントでも日付列の生成を試行
            this.Loaded += WbsPage_DataGridLoaded;

            // 祝日データを初期化（非同期で実行）
            _ = Task.Run(() => InitializeHolidayDataAsync());

            // プロジェクト選択変更時のイベントを登録
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // キーボードショートカットを設定
            this.KeyDown += WbsPage_KeyDown;
            
            // DataGridにフォーカスを設定してキーボードイベントを受け取れるようにする
            this.Loaded += (s, e) => 
            {
                WbsDataGrid.Focus();
                // DataGridのキーボードイベントを確実に設定
                WbsDataGrid.KeyDown += WbsDataGrid_KeyDown;
                WbsDataGrid.PreviewKeyDown += WbsDataGrid_PreviewKeyDown;
            };
        }

        /// <summary>
        /// 祝日データを初期化する
        /// </summary>
#pragma warning disable CS1998 // 非同期メソッドには await 演算子が必要（実際には使用している）
        private async Task InitializeHolidayDataAsync()
        {
            try
            {
                await RedmineClient.Services.HolidayService.ForceUpdateAsync();
            }
            catch
            {
                // 祝日データの初期化に失敗
            }
        }
#pragma warning restore CS1998



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
            }
        }

        /// <summary>
        /// DataGridのキーボードショートカットを処理する
        /// </summary>
        private void WbsDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // デバッグ用：キーイベントが発生しているかを確認
            System.Diagnostics.Debug.WriteLine($"DataGrid KeyDown: {e.Key}");
            
            switch (e.Key)
            {
                case Key.Delete:
                    // Deleteキーで選択されたアイテムを削除
                    System.Diagnostics.Debug.WriteLine("Delete key pressed in DataGrid");
                    if (ViewModel.SelectedItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SelectedItem: {ViewModel.SelectedItem.Title}");
                        ViewModel.DeleteSelectedItemCommand.Execute(null);
                        e.Handled = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No item selected");
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
            }
        }

        /// <summary>
        /// DataGridのPreviewKeyDownイベントを処理する（より確実にキーイベントをキャッチ）
        /// </summary>
        private void WbsDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // デバッグ用：PreviewKeyDownイベントが発生しているかを確認
            System.Diagnostics.Debug.WriteLine($"DataGrid PreviewKeyDown: {e.Key}");
            
            switch (e.Key)
            {
                case Key.Delete:
                    // Deleteキーで選択されたアイテムを削除
                    System.Diagnostics.Debug.WriteLine("Delete key pressed in DataGrid PreviewKeyDown");
                    if (ViewModel.SelectedItem != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"SelectedItem: {ViewModel.SelectedItem.Title}");
                        ViewModel.DeleteSelectedItemCommand.Execute(null);
                        e.Handled = true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No item selected in PreviewKeyDown");
                    }
                    break;
            }
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            // 年月の選択肢を初期化
            InitializeYearMonthOptions();

            // プロジェクト選択の初期化
            
            // Redmineに接続してプロジェクトを取得
            if (ViewModel.AvailableProjects.Count == 0)
            {
                try
                {
                    // Redmine接続テストを実行してプロジェクトを取得（非同期で実行）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ViewModel.TestRedmineConnection();
                        }
                        catch
                        {
                            // Redmine接続テストに失敗
                        }
                    });
                }
                catch
                {
                    // Redmine接続テストの開始に失敗
                }
            }

            // プロジェクトが選択されている場合、自動的にチケット一覧を取得
            if (ViewModel.SelectedProject != null && ViewModel.IsRedmineConnected)
            {
                try
                {
                    ViewModel.LoadRedmineData();
                }
                catch
                {
                    // チケット一覧の自動取得に失敗
                }
            }

            // 日付列の生成を遅延実行（DataGridの完全な初期化を待つ）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GenerateDateColumns();
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            // このイベントは一度だけ実行
            this.Loaded -= WbsPage_InitialLoaded;
        }

        private void WbsPage_DataGridLoaded(object sender, RoutedEventArgs e)
        {
            GenerateDateColumns();
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
            }
            catch
            {
                // 祝日データの再初期化に失敗
            }
        }

        public Task OnNavigatedToAsync()
        {
            // 日付列の生成も遅延実行で試行
            Dispatcher.BeginInvoke(new Action(() =>
            {
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
        private void GenerateDateColumns()
        {
            if (WbsDataGrid != null && WbsDataGrid.IsLoaded && WbsDataGrid.IsInitialized)
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
                 var endDate = startDate.AddMonths(2).AddDays(-1); // 2か月分表示（タスク期間をカバー）

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
                        Width = 40, // 幅を少し広げて3行表示に対応
                        Header = CreateThreeRowHeader(currentDate, isMonthStart),
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
                        Source = currentDate,
                        Converter = new RedmineClient.Helpers.DateToBackgroundColorConverter()
                    };
                    backgroundFactory.SetValue(Border.BackgroundProperty, backgroundBinding);

                    // 今日の日付ライン表示（設定が有効な場合のみ）
                    if (ViewModel.ShowTodayLine && currentDate.Date == DateTime.Today)
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

                    // タスク期間表示用のBorder（開始日から終了日まで）
                    var taskPeriodFactory = new FrameworkElementFactory(typeof(Border));
                    taskPeriodFactory.SetValue(Border.WidthProperty, 30.0);
                    taskPeriodFactory.SetValue(Border.HeightProperty, 20.0);
                    taskPeriodFactory.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.Transparent);
                    taskPeriodFactory.SetValue(Border.BorderBrushProperty, System.Windows.Media.Brushes.Blue);
                    taskPeriodFactory.SetValue(Border.BorderThicknessProperty, new Thickness(2));
                    taskPeriodFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
                    taskPeriodFactory.SetValue(Border.OpacityProperty, 0.8);
                    taskPeriodFactory.SetValue(Grid.ZIndexProperty, 1);

                    // タスク期間の表示/非表示を制御（MultiBindingを使用）
                    var multiBinding = new System.Windows.Data.MultiBinding();
                    multiBinding.Converter = new TaskPeriodMultiBindingConverter(currentDate);
                    
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
                    
                    taskPeriodFactory.SetValue(Border.VisibilityProperty, multiBinding);

                    // 進捗に応じた背景色の設定（WbsItemから直接取得）
                    var progressBinding = new System.Windows.Data.Binding
                    {
                        Path = new System.Windows.PropertyPath("Progress"),
                        Converter = new TaskProgressToColorConverter()
                    };
                    taskPeriodFactory.SetValue(Border.BackgroundProperty, progressBinding);

                    // Gridに要素を追加
                    factory.AppendChild(backgroundFactory);
                    factory.AppendChild(taskPeriodFactory);

                    cellTemplate.VisualTree = factory;
                    dateColumn.CellTemplate = cellTemplate;

                    // 固定列の後ろに日付列を追加
                    WbsDataGrid.Columns.Insert(fixedColumnCount + columnCount, dateColumn);
                    columnCount++;
                    currentDate = currentDate.AddDays(1);
                }
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
        /// 日付テキストボックスのキーイベントハンドラー
        /// </summary>
        private void DateTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                // DatePickerを使用するため、このメソッドは不要
                // 日付の調整はDatePickerのカレンダーで行う
            }
            catch
            {
                // 例外をキャッチ
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
                    DescriptionTextBox?.Focus();
                }
                else
                {
                    // 順方向に移動
                    ProgressSlider?.Focus();
                }
            }
            catch
            {
                // フィールド移動中にエラー
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
                // DatePickerを使用するため、このメソッドは不要
                // 日付の調整はDatePickerのカレンダーで行う
            }
            catch
            {
                // 日付調整中にエラー
            }
        }

        /// <summary>
        /// 日付テキストボックスのKeyUpイベントハンドラー
        /// </summary>
        private void DateTextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                // DatePickerを使用するため、このメソッドは不要
                // 日付の調整はDatePickerのカレンダーで行う
            }
            catch
            {
                // DateTextBox KeyUp処理中にエラー
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
                }
                else if (e.Key == System.Windows.Input.Key.Tab)
                {
                    // タブキーで説明欄に移動
                    e.Handled = true;
                    DescriptionTextBox?.Focus();
                }
            }
            catch
            {
                // タイトルフィールド キーイベント処理中に例外
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
                    }
                    else
                    {
                        // フォールバック：DataGridにフォーカス
                        SetDataGridFocus();
                    }
                }
                catch
                {
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
                    }
                }
            }
            catch (Exception)
            {
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
                    }
                }
            }
            catch (Exception)
            {
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
                        return;
                    }
                }

                // セルが見つからない場合は行にフォーカス
                row.Focus();
            }
            catch (Exception)
            {
                // エラーが発生した場合は行にフォーカス
                try
                {
                    row.Focus();
                }
                catch
                {
                    // 最終手段としてDataGrid全体にフォーカス
                    WbsDataGrid.Focus();
                }
            }
        }

        /// <summary>
        /// ViewModelのプロパティ変更時のイベントハンドラー
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedProject))
            {
                                 
                 // プロジェクトが変更された場合、Redmineデータを自動的に読み込む
                 if (ViewModel.SelectedProject != null && ViewModel.IsRedmineConnected)
                 {
                     ViewModel.LoadRedmineData();
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
                     GenerateDateColumns();
                 }
             }
        }

        /// <summary>
        /// 展開/折りたたみテキストのマウスクリックイベントハンドラー（軽量化版）
        /// </summary>
        private void ExpansionText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is WbsItem item)
            {
                // 展開状態を切り替え
                item.IsExpanded = !item.IsExpanded;
                
                // ViewModelの更新処理を実行
                ViewModel.ToggleExpansion(item);
                
                // UIを即座に更新
                WbsDataGrid.Items.Refresh();
            }
        }

        /// <summary>
        /// DataGridの日付テキストボックスでフォーカスが失われた時の処理
        /// </summary>
        private void DateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // DatePickerを使用するため、このメソッドは不要
            // 日付の検証はDatePickerで自動的に行われる
        }

        /// <summary>
        /// 日付形式が有効かどうかをチェックする
        /// </summary>
        /// <param name="dateText">チェックする日付文字列</param>
        /// <returns>有効な場合はtrue</returns>
        private bool IsValidDateFormat(string dateText)
        {
            try
            {
                // DatePickerを使用するため、このメソッドは不要
                // 日付の検証はDatePickerで自動的に行われる
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// タスクスケジュール変更イベントを発行する
        /// </summary>
        private void OnTaskScheduleChanged(WbsItem task, DateTime oldStartDate, DateTime newStartDate, DateTime newEndDate)
        {
            try
            {
                // DatePickerを使用するため、このメソッドは不要
                // 日付の変更はDatePickerで自動的に処理される
                System.Diagnostics.Debug.WriteLine($"タスク '{task.Title}' のスケジュールが変更されました: {oldStartDate:yyyy/MM/dd} → {newStartDate:yyyy/MM/dd} - {newEndDate:yyyy/MM/dd}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"スケジュール変更イベント処理エラー: {ex.Message}");
            }
        }
    }
}
