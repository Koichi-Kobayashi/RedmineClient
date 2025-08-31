using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RedmineClient.Helpers;
using RedmineClient.ViewModels.Pages;
using RedmineClient.Views.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.Views.Pages
{
    /// <summary>
    /// WbsPage.xaml の相互作用ロジック
    /// </summary>
    public partial class WbsPage : INavigableView<WbsViewModel>, INavigationAware
    {
        public WbsViewModel ViewModel { get; }

        static WbsPage()
        {
            // 静的コンストラクタでDatePickerのプリロードを実行
            Task.Run(StaticPreloadDatePickersAsync);
        }

        public WbsPage(WbsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            // 初期化完了後にタスク詳細の幅を設定
            this.Loaded += WbsPage_InitialLoaded;

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

            // プロジェクト選択変更時のイベントを登録
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // 日付変更の監視を有効化
            ViewModel.StartDateChangeWatching();

            // キーボードショートカットを設定
            this.KeyDown += WbsPage_KeyDown;

            // DataGridにフォーカスを設定してキーボードイベントを受け取れるようにする
            this.Loaded += (s, e) =>
            {
                WbsDataGrid.Focus();
                // DataGridのキーボードイベントを確実に設定
                WbsDataGrid.KeyDown += WbsDataGrid_KeyDown;
                WbsDataGrid.PreviewKeyDown += WbsDataGrid_PreviewKeyDown;

                // DataGridのサイズ変更時に矢印を再描画
                WbsDataGrid.SizeChanged += WbsDataGrid_SizeChanged;
            };

            // アプリケーション終了時の処理を追加
            Application.Current.MainWindow.Closing += MainWindow_Closing;

            // DatePickerのプリロードを開始
            _ = Task.Run(async () => await PreloadDatePickersAsync());
        }

        /// <summary>
        /// 祝日データを初期化する
        /// </summary>
        private async Task InitializeHolidayDataAsync()
        {
            try
            {
                await RedmineClient.Services.HolidayService.ForceUpdateAsync();
            }
            catch (Exception)
            {
                // 祝日データの初期化に失敗しても処理を続行
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
            }
        }

        /// <summary>
        /// DataGridのサイズ変更時に矢印を再描画する
        /// </summary>
        private void WbsDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // サイズ変更後に矢印を再描画
            WbsDataGrid?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                DrawDependencyArrowsLightweight();
            }));
        }

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

            // このイベントは一度だけ実行
            this.Loaded -= WbsPage_InitialLoaded;
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
            catch (Exception)
            {
                // 祝日データの再初期化に失敗しても処理を続行
            }
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

        private async void GenerateDateColumns()
        {
            // 無限ループを防ぐためのフラグ
            if (_isGeneratingColumns)
            {
                return;
            }

            if (WbsDataGrid != null && WbsDataGrid.IsLoaded && WbsDataGrid.IsInitialized)
            {
                _isGeneratingColumns = true;

                try
                {
                                            // プログレスバーを表示
                        ViewModel.IsWbsLoading = true;
                        ViewModel.WbsProgressMessage = "日付列を生成中...";
                        ViewModel.WbsProgress = 0;
                        
                        // UIの更新を確実にするために少し待機
                        await Task.Delay(100);

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

                        // 固定列の数を取得（ID、タスク名、説明、開始日、終了日、進捗、ステータス、優先度、担当者、先行・後続）
                        var fixedColumnCount = 10;

                        // 日付列の総数を計算
                        var totalColumns = (int)((endDate - startDate).TotalDays) + 1;

                        for (int columnCount = 0; columnCount < totalColumns; columnCount++)
                        {
                            // プログレスメッセージを更新（10列ごと）
                            if (columnCount % 10 == 0)
                            {
                                var progressPercent = (int)((double)columnCount / totalColumns * 100);
                                ViewModel.WbsProgress = progressPercent;
                                ViewModel.WbsProgressMessage = $"日付列を生成中... ({progressPercent}%)";
                                
                                // UIの更新を確実にするために少し待機
                                await Task.Delay(10);
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

                            // 進捗に応じた背景色の設定（WbsItemから直接取得）
                            // ただし、土日祝の場合は日付の背景色を優先するため、透明度を下げる
                            var progressBinding = new System.Windows.Data.Binding
                            {
                                Path = new System.Windows.PropertyPath("Progress"),
                                Converter = new TaskProgressToColorConverter()
                            };
                            taskPeriodFactory.SetValue(DraggableTaskBorder.BackgroundProperty, progressBinding);

                            // 土日祝の場合は背景色の透明度を下げて、日付の背景色が見えるようにする
                            var opacityBinding = new System.Windows.Data.Binding
                            {
                                Source = loopDate,
                                Converter = new RedmineClient.Helpers.DateToOpacityConverter()
                            };
                            taskPeriodFactory.SetValue(DraggableTaskBorder.OpacityProperty, opacityBinding);

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
                                        var actualColumnIndex = 9 + columnCount;

                                        // 列0の日付（表示開始日）を基準として渡す
                                        // これにより、各列での日付計算が正しく行われる
                                        // totalColumnsには固定列9個 + 日付列の総数を渡す必要がある
                                        var actualTotalColumns = 9 + totalColumns;

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
                    catch (Exception)
                    {
                        // エラーが発生した場合でもItemsSourceを復元
                        WbsDataGrid.ItemsSource = currentItemsSource;
                        throw;
                    }

                    // 完了メッセージを表示
                    ViewModel.WbsProgress = 100;
                    ViewModel.WbsProgressMessage = "日付列の生成が完了しました";
                }
                finally
                {
                    _isGeneratingColumns = false;
                }
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void WbsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // プロジェクト選択の初期化
            if (ViewModel?.AvailableProjects != null && ViewModel.AvailableProjects.Any())
            {
                ProjectComboBox.SelectedIndex = 0;
            }

            // スケジュール開始年月の初期化
            InitializeScheduleStartYearMonth();

            // 依存関係矢印の初期化
            InitializeDependencyArrows();
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

        private void InitializeDependencyArrows()
        {
            // 依存関係矢印の表示/非表示を制御
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ViewModel.ShowDependencyArrows))
                    {
                        UpdateDependencyArrowsAsync();
                    }
                };
            }

            // スクロール中の描画処理を最適化
            if (WbsDataGrid != null)
            {
                var scrollViewer = GetScrollViewer(WbsDataGrid);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollChanged += (s, e) =>
                    {
                        // スクロール中は矢印描画を一時停止
                        if (_isDrawingArrows)
                        {
                            _arrowDrawingCancellation?.Cancel();
                        }

                        // スクロール停止後、少し遅延して矢印を再描画
                        Task.Delay(300).ContinueWith(_ =>
                        {
                            if (ViewModel?.ShowDependencyArrows == true)
                            {
                                Dispatcher.BeginInvoke(() => UpdateDependencyArrowsAsync());
                            }
                        });
                    };
                }
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private bool _isDrawingArrows = false;
        private CancellationTokenSource _arrowDrawingCancellation;

        private async void UpdateDependencyArrowsAsync()
        {
            if (_isDrawingArrows)
            {
                _arrowDrawingCancellation?.Cancel();
            }

            if (!ViewModel?.ShowDependencyArrows == true || DependencyArrowCanvas == null)
            {
                ClearDependencyArrows();
                return;
            }

            _isDrawingArrows = true;
            _arrowDrawingCancellation = new CancellationTokenSource();

            try
            {
                await Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (_arrowDrawingCancellation.Token.IsCancellationRequested) return;
                        DrawDependencyArrowsLightweight();
                    }, DispatcherPriority.Background);
                }, _arrowDrawingCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // 描画がキャンセルされた場合は何もしない
            }
            finally
            {
                _isDrawingArrows = false;
            }
        }

        private void DrawDependencyArrowsLightweight()
        {
            if (DependencyArrowCanvas == null || ViewModel?.FlattenedWbsItems == null) return;

            // 既存の矢印をクリア
            DependencyArrowCanvas.Children.Clear();

            // DataGridのレイアウトが完了するまで待機
            WbsDataGrid?.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                try
                {
                    // 表示されている範囲のみを描画（パフォーマンス向上）
                    var visibleItems = ViewModel.FlattenedWbsItems.Take(50).ToList(); // 最初の50件のみ描画

                    foreach (var item in visibleItems)
                    {
                        if (item.Predecessors?.Any() == true)
                        {
                            foreach (var predecessor in item.Predecessors)
                            {
                                DrawSimpleArrow(predecessor, item);
                            }
                        }
                    }
                }
                catch
                {
                    // 矢印描画エラーは無視
                }
            }));
        }

        private void DrawSimpleArrow(WbsItem from, WbsItem to)
        {
            if (DependencyArrowCanvas == null || WbsDataGrid == null) return;

            try
            {
                // DataGridの行の位置を取得
                var fromRow = GetRowIndex(from);
                var toRow = GetRowIndex(to);

                if (fromRow == -1 || toRow == -1)
                {
                    return;
                }

                // DataGridの実際の位置とサイズを取得
                var dataGridBounds = WbsDataGrid.TransformToVisual(DependencyArrowCanvas).TransformBounds(
                    new Rect(0, 0, WbsDataGrid.ActualWidth, WbsDataGrid.ActualHeight));

                // 行の高さを取得（DataGridの実際の行高さを使用）
                var rowHeight = WbsDataGrid.RowHeight > 0 ? WbsDataGrid.RowHeight : 30.0;

                // 行の位置を計算（DataGridの実際の位置を考慮）
                var fromY = dataGridBounds.Top + fromRow * rowHeight + rowHeight / 2;
                var toY = dataGridBounds.Top + toRow * rowHeight + rowHeight / 2;

                // スケジュール表の開始位置を計算（固定列の後ろから）
                var scheduleStartX = CalculateScheduleStartX();

                // 先行タスクの終了日位置を計算
                var fromEndDateX = CalculateDateColumnX(from.EndDate, scheduleStartX);

                // 後続タスクの開始日位置を計算
                var toStartDateX = CalculateDateColumnX(to.StartDate, scheduleStartX);

                // L字型の矢印を描画
                // 1. 先行タスクの終了日から水平に右へ
                var horizontalStartX = fromEndDateX;
                var horizontalEndX = toStartDateX;

                // 2. 垂直線のX座標（先行タスクと後続タスクの中間）
                var verticalX = (horizontalStartX + horizontalEndX) / 2;

                // 水平線1（先行タスクの終了日から垂直線まで）
                var horizontalLine1 = new Line
                {
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 2.0,
                    X1 = horizontalStartX,
                    Y1 = fromY,
                    X2 = verticalX,
                    Y2 = fromY
                };

                // 垂直線
                var verticalLine = new Line
                {
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 2.0,
                    X1 = verticalX,
                    Y1 = fromY,
                    X2 = verticalX,
                    Y2 = toY
                };

                // 水平線2（垂直線から後続タスクの開始日まで）
                var horizontalLine2 = new Line
                {
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 2.0,
                    X1 = verticalX,
                    Y1 = toY,
                    X2 = horizontalEndX,
                    Y2 = toY
                };

                // 矢印ヘッド
                var arrowHead = new Polygon
                {
                    Fill = Brushes.DarkBlue,
                    Points = new PointCollection
                    {
                        new Point(horizontalEndX - 6, toY - 6),
                        new Point(horizontalEndX + 6, toY),
                        new Point(horizontalEndX - 6, toY + 6)
                    }
                };

                // Canvasに追加
                DependencyArrowCanvas.Children.Add(horizontalLine1);
                DependencyArrowCanvas.Children.Add(verticalLine);
                DependencyArrowCanvas.Children.Add(horizontalLine2);
                DependencyArrowCanvas.Children.Add(arrowHead);
            }
            catch
            {
                // 描画エラーは無視
            }
        }

        /// <summary>
        /// スケジュール表の開始X座標を計算する
        /// </summary>
        private double CalculateScheduleStartX()
        {
            if (WbsDataGrid == null) return 0.0;

            try
            {
                // 固定列の幅を累積して計算
                double x = 0;
                for (int i = 0; i < WbsDataGrid.Columns.Count; i++)
                {
                    var column = WbsDataGrid.Columns[i];
                    // 日付列（StackPanelヘッダーを持つ列）の前まで
                    // 固定列: ID, タスク名, 説明, 開始日, 終了日, 進捗, ステータス, 優先度, 担当者, 先行・後続
                    if (i >= 10) // 固定列は10個
                    {
                        break;
                    }
                    x += column.Width.Value;
                }
                return x;
            }
            catch
            {
                return 0.0;
            }
        }

        /// <summary>
        /// 指定された日付の列のX座標を計算する
        /// </summary>
        private double CalculateDateColumnX(DateTime date, double scheduleStartX)
        {
            if (WbsDataGrid == null || ViewModel?.ScheduleStartYearMonth == null) return scheduleStartX;

            try
            {
                // 設定された年月の1日から開始
                DateTime startDate;
                if (DateTime.TryParseExact(ViewModel.ScheduleStartYearMonth, "yyyy/MM", null, System.Globalization.DateTimeStyles.None, out startDate))
                {
                    startDate = startDate.AddDays(-startDate.Day + 1); // 月の1日に設定
                }
                else
                {
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
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

                // 日付列の幅（40px）
                var dateColumnWidth = 40.0;

                // 指定された日付までの日数を計算
                var daysDiff = (int)((date - startDate).TotalDays);

                // 日付列の位置を計算
                var dateColumnIndex = Math.Max(0, daysDiff);

                return scheduleStartX + (dateColumnIndex * dateColumnWidth) + (dateColumnWidth / 2);
            }
            catch
            {
                return scheduleStartX;
            }
        }

        /// <summary>
        /// WbsItemの行インデックスを取得する
        /// </summary>
        private int GetRowIndex(WbsItem item)
        {
            if (ViewModel?.FlattenedWbsItems == null) return -1;

            var flattenedItems = ViewModel.FlattenedWbsItems;
            for (int i = 0; i < flattenedItems.Count; i++)
            {
                if (flattenedItems[i] == item)
                {
                    return i;
                }
            }
            return -1;
        }

        private void ClearDependencyArrows()
        {
            if (DependencyArrowCanvas != null)
            {
                DependencyArrowCanvas.Children.Clear();
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
            if (sender is System.Windows.Controls.TextBlock textBlock && textBlock.DataContext is WbsItem item)
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
        /// DataGridのLoadedイベントハンドラー（イベント用のラッパー）
        /// </summary>
        private async void WbsDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                // 描画完了を待ってからプログレスバーとメッセージを非表示にする
                await WaitForRenderComplete(dataGrid);

                // 描画完了後にプログレスバーとメッセージを非表示にする
                await Dispatcher.InvokeAsync(() =>
                {
                    if (ViewModel.IsWbsLoading)
                    {
                        ViewModel.IsWbsLoading = false;
                        ViewModel.WbsProgressMessage = ""; // プログレスメッセージも消す
                    }
                }, DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// 「UIキューが描画まで進み、アイドルになる」まで待つユーティリティ
        /// </summary>
        public static async Task WaitForRenderComplete(FrameworkElement element)
        {
            // レイアウト計算を一度進める
            await Dispatcher.Yield(DispatcherPriority.Background);
            element.UpdateLayout();

            // Render 優先度の処理が流れ切るのを待つ
            await element.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

            // さらに ApplicationIdle になるまで待つ（微妙な残り処理対策）
            await element.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
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
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (var datePicker in datePickers)
                                    {
                                        datePicker.IsDropDownOpen = false;
                                    }
                                });
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
                // 未保存の変更がある場合は保存処理を実行
                if (ViewModel != null)
                {
                    // 未保存の変更を保存
                    await ViewModel.SavePendingChangesAsync();
                }
            }
            catch (Exception)
            {
                // アプリケーション終了時の保存処理でエラーが発生した場合は無視
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
    }
}