using RedmineClient.ViewModels.Pages;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic; // Added for List

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
            
            // 年月の選択肢を初期化
            InitializeYearMonthOptions();
        }

        private void WbsPage_InitialLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("WbsPage_InitialLoaded: 開始");
            
            // 保存されたウィンドウサイズを復元
            RestoreWindowSize();
            
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
                GenerateDateColumns();
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
            return Task.CompletedTask;
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
                var yearMonthOptions = new List<string>();
                var currentDate = DateTime.Now.AddYears(-2); // 2年前から
                var endDate = DateTime.Now.AddYears(3); // 3年後まで
                
                while (currentDate <= endDate)
                {
                    yearMonthOptions.Add(currentDate.ToString("yyyy/MM"));
                    currentDate = currentDate.AddMonths(1);
                }
                
                ScheduleStartYearMonthComboBox.ItemsSource = yearMonthOptions;
                
                // 現在の年月が選択されていることを確認
                if (ViewModel.ScheduleStartYearMonth != null && yearMonthOptions.Contains(ViewModel.ScheduleStartYearMonth))
                {
                    ScheduleStartYearMonthComboBox.SelectedItem = ViewModel.ScheduleStartYearMonth;
                }
                else
                {
                    ScheduleStartYearMonthComboBox.SelectedItem = DateTime.Now.ToString("yyyy/MM");
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
        /// 月ヘッダーの内容を作成する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>月ヘッダーの内容</returns>
        private object CreateMonthHeader(DateTime date)
        {
            var monthText = new TextBlock
            {
                Text = $"{date:MM}月",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                VerticalAlignment = VerticalAlignment.Center
            };

            return monthText;
        }

        /// <summary>
        /// 月ヘッダーと日付ヘッダーを縦に結合したヘッダーを作成する
        /// </summary>
        /// <param name="date">日付</param>
        /// <param name="monthDate">月の開始日（nullの場合は月ヘッダーを表示しない）</param>
        /// <returns>結合されたヘッダーの内容</returns>
        private object CreateCombinedHeader(DateTime date, DateTime? monthDate)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 月ヘッダーがある場合（月の開始位置）
            if (monthDate.HasValue)
            {
                var monthText = new TextBlock
                {
                    Text = $"{monthDate.Value:yyyy年MM月}",
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Height = 20,
                    Foreground = System.Windows.Media.Brushes.DarkBlue
                };
                stackPanel.Children.Add(monthText);
            }
            else
            {
                // 月ヘッダーがない場合は空のスペース
                var emptySpace = new TextBlock
                {
                    Height = 20
                };
                stackPanel.Children.Add(emptySpace);
            }

            // 日付ヘッダー
            var dayText = new TextBlock
            {
                Text = $"{date:dd}",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
                stackPanel.Children.Add(dayText);

            // 曜日
            var dayOfWeekText = new TextBlock
            {
                Text = GetDayOfWeek(date),
                FontSize = 8,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
            stackPanel.Children.Add(dayOfWeekText);

            return stackPanel;
        }

        /// <summary>
        /// 3行ヘッダーを作成する（1行目：月ヘッダー、2行目：日付、3行目：曜日）
        /// </summary>
        /// <param name="monthDate">月の開始日</param>
        /// <param name="spanDays">横に結合する日数</param>
        /// <returns>3行ヘッダー</returns>
        private object CreateMonthHeaderWithSpan(DateTime monthDate, int spanDays)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1行目：月ヘッダー（横結合用）- 中央寄せで表示
            var monthText = new TextBlock
            {
                Text = $"{monthDate:yyyy年MM月}",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                TextAlignment = TextAlignment.Center
            };
            stackPanel.Children.Add(monthText);

            // 2行目：空のスペース（日付は各列に表示）
            var emptyDaySpace = new TextBlock
            {
                Height = 20,
                Text = ""
            };
            stackPanel.Children.Add(emptyDaySpace);

            // 3行目：空のスペース（曜日は各列に表示）
            var emptyWeekSpace = new TextBlock
            {
                Height = 20,
                Text = ""
            };
            stackPanel.Children.Add(emptyWeekSpace);

            return stackPanel;
        }

        /// <summary>
        /// 3行ヘッダーを作成する（1行目：空のスペース、2行目：日付、3行目：曜日）
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>3行ヘッダー</returns>
        private object CreateDateOnlyHeader(DateTime date)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1行目：空のスペース（月ヘッダーなし）
            var emptyMonthSpace = new TextBlock
            {
                Height = 20,
                Text = ""
            };
            stackPanel.Children.Add(emptyMonthSpace);

            // 2行目：日付
            var dayText = new TextBlock
            {
                Text = $"{date:dd}",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
            stackPanel.Children.Add(dayText);

            // 3行目：曜日
            var dayOfWeekText = new TextBlock
            {
                Text = GetDayOfWeek(date),
                FontSize = 8,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
            stackPanel.Children.Add(dayOfWeekText);

            return stackPanel;
        }

        /// <summary>
        /// 日付ヘッダーの内容を作成する（従来の方法）
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>日付ヘッダーの内容</returns>
        private object CreateDateHeader(DateTime date)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 1行目：日
            var dayText = new TextBlock
            {
                Text = $"{date:dd}",
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
            stackPanel.Children.Add(dayText);

            // 2行目：曜日
            var dayOfWeekText = new TextBlock
            {
                Text = GetDayOfWeek(date),
                FontSize = 8,
                FontWeight = FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Height = 20
            };
            stackPanel.Children.Add(dayOfWeekText);

            return stackPanel;
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
            
            // 高さ（月ヘッダーと日付・曜日表示のため調整）
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HeightProperty, 
                60.0));
            
            return style;
        }

        /// <summary>
        /// 月ヘッダー用のスタイルを作成する（横結合用）
        /// </summary>
        /// <returns>月ヘッダー用のスタイル</returns>
        private Style CreateMonthHeaderStyle()
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
            
            // 高さ（月ヘッダーと日付・曜日表示のため調整）
            style.Setters.Add(new Setter(System.Windows.Controls.Primitives.DataGridColumnHeader.HeightProperty, 
                60.0));
            
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
                
                // 既存のスケジュール列を削除（月ヘッダーと日付列）
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
                var monthStartIndex = 0;
                var monthInfo = new List<(int startIndex, int endIndex, DateTime monthDate)>();
                
                // 固定列の数を取得（タスク名、ID、説明、開始日、終了日、進捗、ステータス、優先度、担当者）
                var fixedColumnCount = 9;
                
                while (currentDate <= endDate)
                {
                    // 月が変わった場合、前の月の情報を記録
                    if (currentDate.Month != lastMonth && lastMonth != -1)
                    {
                        monthInfo.Add((monthStartIndex, columnCount - 1, currentDate.AddDays(-1)));
                        System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 月情報を記録: {monthStartIndex} から {columnCount - 1}");
                    }
                    
                    // 月が変わった場合、新しい月の開始位置を記録
                    if (currentDate.Month != lastMonth)
                    {
                        lastMonth = currentDate.Month;
                        monthStartIndex = columnCount;
                        System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 新しい月開始: {currentDate:yyyy年MM月} (列位置: {columnCount})");
                    }
                    
                    var dateColumn = new DataGridTemplateColumn
                    {
                        Width = 25,
                        Header = CreateDateHeader(currentDate), // 初期ヘッダー（後で更新される）
                        IsReadOnly = true,
                        HeaderStyle = CreateDateHeaderStyle()
                    };
                    
                    // 日付列用のセルテンプレート
                    var cellTemplate = new DataTemplate();
                    var factory = new FrameworkElementFactory(typeof(Border));
                    
                    factory.SetValue(Border.WidthProperty, 25.0);
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
                
                // 最後の月の情報を記録
                if (lastMonth != -1)
                {
                    monthInfo.Add((monthStartIndex, columnCount - 1, endDate));
                    System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 最後の月情報を記録: {monthStartIndex} から {columnCount - 1}");
                }
                
                                 // 月ヘッダーを横結合するためのカスタムヘッダーテンプレートを作成
                 // 各月の開始列に月ヘッダーを設定し、その月の他の列は空のヘッダーにする
                 for (int i = 0; i < columnCount; i++)
                 {
                     var dateColumn = WbsDataGrid.Columns[fixedColumnCount + i] as DataGridTemplateColumn;
                     if (dateColumn != null)
                     {
                         var columnDate = startDate.AddDays(i);
                         
                         // 月の開始位置かどうかをチェック
                         var monthData = monthInfo.FirstOrDefault(m => m.startIndex == i);
                         if (monthData != default)
                         {
                             // 月の開始位置：月ヘッダー付きのヘッダー（横結合用）
                             var monthDays = monthData.endIndex - monthData.startIndex + 1;
                             
                             // 月の日数を計算（より正確な方法）
                             var monthStart = monthData.Item3;
                             var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                             var actualMonthDays = (monthEnd - monthStart).Days + 1;
                             
                             dateColumn.Header = CreateMonthHeaderWithSpan(monthData.Item3, actualMonthDays);
                             
                             // 月ヘッダー列の幅を月の日数分に設定（横結合用）
                             dateColumn.Width = monthDays * 25.0;
                             
                             // 月ヘッダー用のスタイルを適用
                             dateColumn.HeaderStyle = CreateMonthHeaderStyle();
                         }
                         else
                         {
                             // 月の開始位置以外：空のヘッダー（月ヘッダーは表示しない）
                             dateColumn.Header = CreateDateOnlyHeader(columnDate);
                             
                             // 月の開始列以外の列の幅を0にして、月ヘッダーが横結合されて見えるようにする
                             dateColumn.Width = 0;
                         }
                     }
                 }
                
                System.Diagnostics.Debug.WriteLine($"GenerateDateColumns: 完了。{columnCount}個の日付列と{monthInfo.Count}個の月ヘッダー列を追加しました。現在の総列数: {WbsDataGrid.Columns.Count}");
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
