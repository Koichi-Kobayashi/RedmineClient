using System.Collections.ObjectModel;
using System.Windows.Input;
using Redmine.Net.Api.Types;
using RedmineClient.Services;
using RedmineClient.ViewModels.Windows;
using RedmineClient.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.ViewModels.Pages
{
    public partial class WbsViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// WBSアイテムのリスト
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<WbsItem> _wbsItems = new();
    
        /// <summary>
        /// 階層構造を平坦化したアイテムリスト（DataGrid表示用）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<WbsItem> _flattenedWbsItems = new();

        /// <summary>
        /// スケジュール表のデータ
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<ScheduleItem> _scheduleItems = new();

        /// <summary>
        /// スケジュール表の描画中かどうか
        /// </summary>
        [ObservableProperty]
        private bool _isScheduleLoading = false;

        /// <summary>
        /// スケジュール表の描画進捗（0-100）
        /// </summary>
        [ObservableProperty]
        private double _scheduleProgress = 0;

        /// <summary>
        /// スケジュール表の描画進捗メッセージ
        /// </summary>
        [ObservableProperty]
        private string _scheduleProgressMessage = "スケジュール表を描画中...";

        /// <summary>
        /// WBSデータの読み込み中かどうか
        /// </summary>
        [ObservableProperty]
        private bool _isWbsLoading = false;

        /// <summary>
        /// WBSデータの読み込み進捗（0-100）
        /// </summary>
        [ObservableProperty]
        private double _wbsProgress = 0;

        /// <summary>
        /// WBSデータの読み込み進捗メッセージ
        /// </summary>
        [ObservableProperty]
        private string _wbsProgressMessage = "WBSデータを読み込み中...";

        [ObservableProperty]
        private WbsItem? _selectedItem;

        /// <summary>
        /// 選択されたアイテムに子タスクを追加可能かどうか
        /// </summary>
        [ObservableProperty]
        private bool _canAddChild = false;



        /// <summary>
        /// タスク詳細の表示/非表示
        /// true: タスク詳細を表示
        /// false: タスク詳細を非表示
        /// </summary>
        [ObservableProperty]
        private bool _isTaskDetailVisible = false;

        /// <summary>
        /// プロジェクトが1つの場合、コンボボックスを読み取り専用にする
        /// </summary>
        [ObservableProperty]
        private bool _isProjectSelectionReadOnly = false;

        /// <summary>
        /// 今日の日付ラインを表示するかどうか
        /// </summary>
        [ObservableProperty]
        private bool _showTodayLine = true;



        partial void OnSelectedItemChanged(WbsItem? value)
        {
            // 選択されたアイテムが変更されたときに、子タスク追加可能かどうかを更新
            // 親タスク（子タスクを持てるタスク）が選択されている場合のみ子タスク追加可能
            if (value != null)
            {
                // 選択されたアイテムが親タスクかどうかを判定
                CanAddChild = value.IsParentTask;
            }
            else
            {
                CanAddChild = false;
            }
        }



        partial void OnScheduleStartYearMonthChanged(string value)
        {
            // 設定を保存
            AppConfig.ScheduleStartYearMonth = value;
            AppConfig.Save();
        }

        partial void OnShowTodayLineChanged(bool value)
        {
            // 設定を保存
            AppConfig.ShowTodayLine = value;
            AppConfig.Save();
        }

        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value != null)
            {
                // 選択されたプロジェクトIDを保存
                AppConfig.SelectedProjectId = value.Id;
                AppConfig.Save();
                
                // プロジェクトが変更された場合、Redmineデータを自動的に読み込む
                if (IsRedmineConnected)
                {
                    // 重複実行を防ぐためのフラグ
                    if (_isLoadingRedmineData)
                    {
                        return;
                    }
                    
                    // より安全な非同期実行（UIスレッドをブロックしない）
                    
                    // 完全にバックグラウンドで実行（UIスレッドとの競合を避ける）
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _isLoadingRedmineData = true;
                            
                            // UIスレッドでの処理を避けるため、直接RedmineServiceを使用
                            await Task.Delay(100); // 短い遅延
                            
                            // 既存のアイテムの親子関係をクリア
                            foreach (var existingItem in WbsItems)
                            {
                                existingItem.Children.Clear();
                            }
                            
                            using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                            {
                                var issues = await redmineService.GetIssuesWithHierarchyAsync(value.Id).ConfigureAwait(false);
                                
                                // チケットが0個の場合でも適切に処理
                                if (issues == null || issues.Count == 0)
                                {
                                    // UIスレッドで空の状態を設定
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        try
                                        {
                                            WbsItems.Clear();
                                            FlattenedWbsItems.Clear();
                                            IsRedmineDataLoaded = true;
                                            ErrorMessage = string.Empty;
                                        }
                                        catch (Exception ex)
                                        {
                                            ErrorMessage = $"状態設定中にエラーが発生しました: {ex.Message}";
                                        }
                                    });
                                    return;
                                }
                                
                                // バックグラウンドでWBSアイテムの変換を実行
                                var wbsItems = await Task.Run(() =>
                                {
                                    var tempItems = new List<WbsItem>();
                                    // ルートレベルのチケットのみを処理
                                    var rootIssues = issues.Where(issue => issue.Parent == null).ToList();
                                    
                                    foreach (var rootIssue in rootIssues)
                                    {
                                        var wbsItem = ConvertRedmineIssueToWbsItem(rootIssue);
                                        tempItems.Add(wbsItem);
                                    }
                                    return tempItems;
                                }).ConfigureAwait(false);
                                
                                // UIスレッドでの更新は最後に一度だけ実行
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        // WBSアイテムを完全にクリア（親子関係も含めて）
                                        WbsItems.Clear();
                                        foreach (var wbsItem in wbsItems)
                                        {
                                            WbsItems.Add(wbsItem);
                                        }
                                        
                                        // 平坦化リストを更新（チケットがある場合のみ）
                                        if (wbsItems.Count > 0)
                                        {
                                            _ = Task.Run(async () => await UpdateFlattenedList());
                                        }
                                        else
                                        {
                                            FlattenedWbsItems.Clear();
                                        }
                                        
                                        // Redmineデータ読み込み後、選択状態を復元
                                        if (SelectedItem != null)
                                        {
                                            // 選択されたアイテムがまだ存在するかチェック
                                            var currentSelectedItem = SelectedItem;
                                            var foundItem = WbsItems.FirstOrDefault(item => item.Id == currentSelectedItem.Id);
                                            if (foundItem != null)
                                            {
                                                SelectedItem = foundItem;
                                                CanAddChild = foundItem.IsParentTask;
                                            }
                                        }
                                        
                                        // 状態を更新
                                        IsRedmineDataLoaded = true;
                                        ErrorMessage = string.Empty;
                                        
                                    }
                                    catch (Exception ex)
                                    {
                                        ErrorMessage = $"UI更新中にエラーが発生しました: {ex.Message}";
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // エラーもUIスレッドで表示
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ErrorMessage = $"チケットの読み込みに失敗しました: {ex.Message}";
                            });
                        }
                        finally
                        {
                            _isLoadingRedmineData = false;
                        }
                    });
                }
                else
                {
                    // 接続されていない場合は接続テストを実行
                    _ = TestRedmineConnection();
                }
            }
            else
            {
                // プロジェクト選択がクリアされた場合
                AppConfig.SelectedProjectId = null;
                AppConfig.Save();
                WbsItems.Clear();
                                         _ = Task.Run(async () => await UpdateFlattenedList());
                IsRedmineDataLoaded = false;
                ErrorMessage = string.Empty;
            }
        }

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showCompleted = true;

        [ObservableProperty]
        private bool _showInProgress = true;

        /// <summary>
        /// スケジュール開始年月
        /// </summary>
        [ObservableProperty]
        private string _scheduleStartYearMonth = DateTime.Now.ToString("yyyy/MM");

        [ObservableProperty]
        private bool _showNotStarted = true;

        /// <summary>
        /// 追加後編集モードかどうか
        /// true: 追加後編集、false: 連続追加
        /// </summary>
        [ObservableProperty]
        private bool _isEditModeAfterAdd = false;

        [ObservableProperty]
        private bool _isRedmineConnected = false;

        [ObservableProperty]
        private string _connectionStatus = "未接続";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private List<Project> _availableProjects = new();

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private bool _isRedmineDataLoaded = false;

        /// <summary>
        /// 新しく追加されたWBSアイテムのリスト（Redmineに未登録）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<WbsItem> _newWbsItems = new();

        /// <summary>
        /// 登録可能なアイテムがあるかどうか
        /// </summary>
        [ObservableProperty]
        private bool _canRegisterItems = false;

        /// <summary>
        /// 登録処理中かどうか
        /// </summary>
        [ObservableProperty]
        private bool _isRegistering = false;

        /// <summary>
        /// Redmineデータ読み込み中かどうか（重複実行防止用）
        /// </summary>
        private bool _isLoadingRedmineData = false;

        /// <summary>
        /// 登録ボタンのコマンド
        /// </summary>
        public ICommand RegisterItemsCommand { get; }

        /// <summary>
        /// 登録可能なアイテムの数を取得
        /// </summary>
        public int NewItemsCount => NewWbsItems.Count;

        public ICommand AddRootItemCommand { get; }
        public ICommand AddChildItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand TestConnectionCommand { get; }
        public ICommand ToggleExpansionCommand { get; }
        public ICommand MoveItemCommand { get; }
        public ICommand AddMultipleChildrenCommand { get; }
        public ICommand AddBatchChildrenCommand { get; }
        public ICommand UpdateProgressCommand { get; }
        public ICommand LimitHierarchyCommand { get; }
        public ICommand SelectItemCommand { get; }
        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand MoveLeftCommand { get; }
        public ICommand MoveRightCommand { get; }
        public ICommand LoadRedmineDataCommand { get; }
        public ICommand RefreshRedmineDataCommand { get; }
        public ICommand SettingsCommand { get; }
        public ICommand CreateNewIssueCommand { get; }

        public WbsViewModel()
        {
            // 設定からスケジュール開始年月を読み込み（getアクセサーを呼び出さない）
            _scheduleStartYearMonth = AppConfig.GetScheduleStartYearMonthForInitialization();
            
            AddRootItemCommand = new RelayCommand(AddRootItem);
            AddChildItemCommand = new RelayCommand<WbsItem>(AddChildItem);
            DeleteItemCommand = new RelayCommand<WbsItem>(DeleteItem);
            EditItemCommand = new RelayCommand<WbsItem>(EditItem);
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            RefreshCommand = new RelayCommand(() => Refresh());
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
            TestConnectionCommand = new AsyncRelayCommand(TestRedmineConnection);
            ToggleExpansionCommand = new RelayCommand<WbsItem>(ToggleExpansion);
            MoveItemCommand = new RelayCommand<WbsItem>(item => 
            {
                if (item?.Title != null)
                {
                    // TODO: ドラッグ&ドロップの実装
                    System.Windows.MessageBox.Show($"タスク '{item.Title}' の移動機能は今後実装予定です。", "移動", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            });
            AddMultipleChildrenCommand = new RelayCommand<WbsItem>(AddMultipleChildren);
            AddBatchChildrenCommand = new RelayCommand<WbsItem>(parent => AddBatchChildren(parent, 20)); // 20個のサブタスクを一括追加
            UpdateProgressCommand = new RelayCommand(UpdateParentProgress);
            LimitHierarchyCommand = new RelayCommand(() => LimitHierarchyDepth(5));
            SelectItemCommand = new RelayCommand<WbsItem>(SelectItem);
            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);
            MoveLeftCommand = new RelayCommand(MoveLeft);
            MoveRightCommand = new RelayCommand(MoveRight);
            LoadRedmineDataCommand = new AsyncRelayCommand(LoadRedmineDataAsync);
            RefreshRedmineDataCommand = new AsyncRelayCommand(RefreshRedmineDataAsync);
            SettingsCommand = new RelayCommand(OpenSettings);
            CreateNewIssueCommand = new RelayCommand(CreateNewIssue);
            RegisterItemsCommand = new RelayCommand(RegisterItems);
        }

        public virtual async Task OnNavigatedToAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedTo, cts.Token);
            }
        }

        public virtual void OnNavigatedTo()
        {
            InitializeViewModel();
        }

        private async void InitializeViewModel()
        {
            // Redmine接続状態を確認してプロジェクトを取得
            await TestRedmineConnection();
            
            // プロジェクト選択の初期化
            if (AvailableProjects.Count == 0)
            {
                AvailableProjects = new List<Project>();
            }
            
            // Redmineに接続されている場合は実際のデータを読み込み、接続されていない場合はサンプルデータを読み込み
            if (IsRedmineConnected && SelectedProject != null)
            {
                // 実際のRedmineデータを読み込み
                await LoadRedmineDataAsync();
            }
            else
            {
                // サンプルデータを読み込み（開発・テスト用）
                await LoadSampleDataAsync();
            }
            
            // 平坦化リストを初期化（UIスレッドで実行）
            await UpdateFlattenedList();
            
            // スケジュール表を初期化
            await InitializeScheduleItems();
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedFrom, cts.Token);
            }
        }

        public void OnNavigatedFrom() { }

        /// <summary>
        /// Redmine接続テストを実行（非同期版）
        /// </summary>
        public async Task TestRedmineConnection()
        {
            if (string.IsNullOrEmpty(AppConfig.RedmineHost) || string.IsNullOrEmpty(AppConfig.ApiKey))
            {
                IsRedmineConnected = false;
                ConnectionStatus = "設定未完了";
                ErrorMessage = "RedmineホストまたはAPIキーが設定されていません";
                return;
            }



            try
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続中...";
                ErrorMessage = string.Empty;

                // RedmineServiceを使用した接続テスト（非同期版）
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    var isConnected = await redmineService.TestConnectionAsync();
                    
                    if (isConnected)
                    {
                        IsRedmineConnected = true;
                        ConnectionStatus = "接続済み";
                        ErrorMessage = string.Empty;
                        
                        // プロジェクト一覧を取得
                        await LoadProjectsAsync(redmineService);
                        
                        // プロジェクトが選択されている場合は、自動的にRedmineデータを読み込む
                        if (SelectedProject != null)
                        {
                            // 少し遅延を入れてから読み込みを実行（UIの更新を待つ）
                            await Task.Delay(200);
                            await LoadRedmineDataAsync();
                        }
                    }
                    else
                    {
                        IsRedmineConnected = false;
                        ConnectionStatus = "接続失敗";
                        ErrorMessage = "接続に失敗しました";
                    }
                }
            }
            catch (Exception ex)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                
                // より詳細なエラー情報を表示
                var errorDetails = GetDetailedErrorMessage(ex);
                ErrorMessage = $"接続エラー: {errorDetails}";
                

            }
        }

        /// <summary>
        /// 例外の詳細なエラーメッセージを取得
        /// </summary>
        /// <param name="ex">例外</param>
        /// <returns>詳細なエラーメッセージ</returns>
        private string GetDetailedErrorMessage(Exception ex)
        {
            if (ex == null) return "不明なエラー";

            var errorMessage = ex.Message;

            // 内部例外がある場合は追加情報を表示
            if (ex.InnerException != null)
            {
                errorMessage += $" (内部エラー: {ex.InnerException.Message})";
            }

            // 特定の例外タイプに応じて詳細情報を追加
            switch (ex)
            {
                case System.Security.Authentication.AuthenticationException:
                    errorMessage += " - SSL/TLS認証エラー。証明書の問題やHTTP/HTTPSの設定を確認してください。";
                    break;
                case System.Net.Http.HttpRequestException:
                    errorMessage += " - HTTPリクエストエラー。ネットワーク接続やURLの設定を確認してください。";
                    break;
                case System.Net.WebException:
                    errorMessage += " - Web接続エラー。ネットワーク設定やファイアウォールを確認してください。";
                    break;
                case Redmine.Net.Api.Exceptions.RedmineException:
                    errorMessage += " - Redmine APIエラー。APIキーや権限を確認してください。";
                    break;
            }

            return errorMessage;
        }

        private async Task LoadSampleDataAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "サンプルデータを読み込み中...";
                });

                // UI更新を一括で行うため、一時的にコレクションを作成
                var tempItems = new List<WbsItem>();
                
                var project = new WbsItem
                {
                    Id = "PROJ-001",
                    Title = "Redmineクライアントアプリ開発",
                    Description = "WPFを使用したRedmineクライアントアプリケーションの開発",
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(3),
                    Status = "進行中",
                    Priority = "高",
                    Assignee = "開発チーム"
                };

            var planning = new WbsItem
            {
                Id = "TASK-001",
                Title = "要件定義・設計",
                Description = "アプリケーションの要件定義と基本設計",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(14),
                Status = "完了",
                Progress = 100,
                Priority = "高",
                Assignee = "プロジェクトマネージャー"
            };

            var development = new WbsItem
            {
                Id = "TASK-002",
                Title = "開発・実装",
                Description = "WPFアプリケーションの開発と実装",
                StartDate = DateTime.Today.AddDays(15),
                EndDate = DateTime.Today.AddDays(60),
                Status = "進行中",
                Progress = 45,
                Priority = "高",
                Assignee = "開発者"
            };

            var testing = new WbsItem
            {
                Id = "TASK-003",
                Title = "テスト・検証",
                Description = "アプリケーションのテストと品質検証",
                StartDate = DateTime.Today.AddDays(61),
                EndDate = DateTime.Today.AddDays(75),
                Status = "未着手",
                Progress = 0,
                Priority = "中",
                Assignee = "テスター"
            };

            var deployment = new WbsItem
            {
                Id = "TASK-004",
                Title = "リリース・展開",
                Description = "アプリケーションのリリースと展開",
                StartDate = DateTime.Today.AddDays(76),
                EndDate = DateTime.Today.AddDays(90),
                Status = "未着手",
                Progress = 0,
                Priority = "中",
                Assignee = "運用チーム"
            };

            // サブタスクの追加
            var uiDesign = new WbsItem
            {
                Id = "TASK-001-1",
                Title = "UI/UX設計",
                Description = "ユーザーインターフェースとユーザーエクスペリエンスの設計",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                Status = "完了",
                Progress = 100,
                Priority = "高",
                Assignee = "UIデザイナー"
            };

            var architecture = new WbsItem
            {
                Id = "TASK-001-2",
                Title = "アーキテクチャ設計",
                Description = "システムアーキテクチャとデータベース設計",
                StartDate = DateTime.Today.AddDays(8),
                EndDate = DateTime.Today.AddDays(14),
                Status = "完了",
                Progress = 100,
                Priority = "高",
                Assignee = "アーキテクト"
            };

            var coreDev = new WbsItem
            {
                Id = "TASK-002-1",
                Title = "コア機能開発",
                Description = "Redmine API連携と基本機能の実装",
                StartDate = DateTime.Today.AddDays(15),
                EndDate = DateTime.Today.AddDays(40),
                Status = "進行中",
                Progress = 60,
                Priority = "高",
                Assignee = "開発者A"
            };

            var wbsDev = new WbsItem
            {
                Id = "TASK-002-2",
                Title = "WBS機能開発",
                Description = "Work Breakdown Structure機能の実装",
                StartDate = DateTime.Today.AddDays(25),
                EndDate = DateTime.Today.AddDays(60),
                Status = "進行中",
                Progress = 30,
                Priority = "高",
                Assignee = "開発者B"
            };

            // 階層構造の構築
            planning.AddChild(uiDesign);
            planning.AddChild(architecture);
            development.AddChild(coreDev);
            development.AddChild(wbsDev);

            project.AddChild(planning);
            project.AddChild(development);
            project.AddChild(testing);
            project.AddChild(deployment);

            // 進捗を更新
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                WbsProgress = 50;
                WbsProgressMessage = "サンプルデータの階層構造を構築中...";
            });

            // UIの応答性を保つために少し待機
            await Task.Delay(10);

            // サンプルデータを追加
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                WbsItems.Add(project);
                WbsProgress = 100;
                WbsProgressMessage = "サンプルデータの読み込みが完了しました";
            });

            // 完了メッセージを少し表示してからクリア
            await Task.Delay(1000);
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsWbsLoading = false;
                WbsProgress = 0;
                WbsProgressMessage = string.Empty;
            });
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsWbsLoading = false;
                WbsProgress = 0;
                WbsProgressMessage = $"エラーが発生しました: {ex.Message}";
            });
        }
        }



        private void AddRootItem()
        {

            
            // パフォーマンス向上：事前にアイテムを作成
            var newItem = CreateWbsItemTemplate(null, "新しいタスク", "タスクの説明を入力してください");
            newItem.StartDate = DateTime.Today;
            newItem.EndDate = DateTime.Today.AddDays(7);

            // パフォーマンス向上のため、一括更新
            WbsItems.Add(newItem);
            
            // 新しく追加されたアイテムをNewWbsItemsリストに追加
            NewWbsItems.Add(newItem);
            CanRegisterItems = true;
            OnPropertyChanged(nameof(NewItemsCount));
            
            // 新しいタスクを選択状態にする
            SelectedItem = newItem;
            
            // 手動でCanAddChildを更新（OnSelectedItemChangedが呼び出されない場合の対策）
            CanAddChild = newItem.IsParentTask;
            
            // MS Projectレベルの高速化：即座にUI更新
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateFlattenedListManually();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void AddChildItem(WbsItem? parent)
        {
            if (parent == null) return;



            // パフォーマンス向上：事前にアイテムを作成
            var newItem = CreateWbsItemTemplate(parent, "新しいサブタスク", "サブタスクの説明を入力してください");
            
            // 親タスクに追加
            parent.AddChild(newItem);
            
            // 新しく追加されたアイテムをNewWbsItemsリストに追加
            NewWbsItems.Add(newItem);
            CanRegisterItems = true;
            OnPropertyChanged(nameof(NewItemsCount));
            
            // 親タスクを選択状態に保つ（連続追加のため）
            SelectedItem = parent;
            
            // 手動でCanAddChildを更新（OnSelectedItemChangedが呼び出されない場合の対策）
            CanAddChild = parent.IsParentTask;
            
            // MS Projectレベルの高速化：即座にUI更新
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateFlattenedListManually();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// WBSアイテムのテンプレートを作成（パフォーマンス向上のため）
        /// </summary>
        private WbsItem CreateWbsItemTemplate(WbsItem? parent, string title, string description)
        {
            var newItem = new WbsItem
            {
                Id = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                Title = title,
                Description = description,
                StartDate = parent?.StartDate ?? DateTime.Today,
                EndDate = parent?.EndDate ?? DateTime.Today.AddDays(7),
                Status = "未着手",
                Priority = "中",
                Assignee = parent?.Assignee ?? "未割り当て",
                Parent = parent,
                IsNew = true // 新しく作成されたアイテムであることを示す
            };

            // デフォルト設定を適用
            ApplyDefaultSettings(newItem);



            return newItem;
        }

        /// <summary>
        /// 新しく作成されたWBSアイテムにデフォルト設定を適用
        /// </summary>
        private void ApplyDefaultSettings(WbsItem item)
        {
            if (SelectedProject != null)
            {
                // プロジェクト情報を設定
                item.RedmineProjectId = SelectedProject.Id;
                item.RedmineProjectName = SelectedProject.Name;


            }

            // デフォルトトラッカーとステータスの情報を設定（表示用）
            if (AppConfig.DefaultTrackerId > 0)
            {
                var defaultTracker = AppConfig.AvailableTrackers.FirstOrDefault(t => t.Id == AppConfig.DefaultTrackerId);
                if (defaultTracker != null)
                {
                    item.RedmineTracker = defaultTracker.Name;
                }
            }

            if (AppConfig.DefaultStatusId > 0)
            {
                var defaultStatus = AppConfig.AvailableStatuses.FirstOrDefault(s => s.Id == AppConfig.DefaultStatusId);
                if (defaultStatus != null)
                {
                    item.Status = defaultStatus.Name;
                }
            }
        }

        /// <summary>
        /// 大量のサブタスクをバッチ処理で追加（パフォーマンス最適化版）
        /// </summary>
        public void AddBatchChildren(WbsItem? parent, int count = 10)
        {
            if (parent == null) return;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // フェーズ1: 事前作成（メモリ上で全アイテムを作成）
            var newItems = new List<WbsItem>(count); // 容量を事前に確保
            for (int i = 0; i < count; i++)
            {
                var newItem = CreateWbsItemTemplate(parent, $"バッチサブタスク {i + 1}", $"バッチサブタスク {i + 1} の説明");
                // CreateWbsItemTemplate内でApplyDefaultSettingsが呼ばれるため、追加の設定は不要
                newItems.Add(newItem);
            }
            
            // フェーズ2: 一括挿入（全アイテムを親に追加）
            foreach (var item in newItems)
            {
                parent.AddChild(item);
            }

            // 新しく追加されたアイテムをNewWbsItemsリストに追加
            foreach (var item in newItems)
            {
                NewWbsItems.Add(item);
            }
            CanRegisterItems = true;
            OnPropertyChanged(nameof(NewItemsCount));
            
            // フェーズ3: UI更新
            SelectedItem = parent;
            CanAddChild = parent.IsParentTask;
            
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateFlattenedListManually();
            }), System.Windows.Threading.DispatcherPriority.Background);
            
            stopwatch.Stop();
        }

        private void DeleteItem(WbsItem? item)
        {
            if (item == null) return;

            // 確認ダイアログを表示
            var message = item.HasChildren 
                ? $"タスク '{item.Title}' とその子タスク {item.Children.Count} 個を削除しますか？" 
                : $"タスク '{item.Title}' を削除しますか？";
            
            var result = System.Windows.MessageBox.Show(message, "削除確認", 
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                if (item.Parent != null)
                {
                    item.Parent.RemoveChild(item);
                }
                else
                {
                    WbsItems.Remove(item);
                }
                
                // 選択されたアイテムが削除された場合、選択をクリア
                if (SelectedItem == item)
                {
                    SelectedItem = null;
                }
            }
        }

        private void EditItem(WbsItem? item)
        {
            if (item == null) return;

            // 簡易的な編集機能（実際の実装ではダイアログを表示）
            // 現在は選択されたアイテムを編集可能にするだけ
            // TODO: 編集ダイアログの実装
            System.Windows.MessageBox.Show($"タスク '{item.Title}' の編集機能は今後実装予定です。", "編集", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// サブタスクを一括で追加
        /// </summary>
        /// <param name="parent">親タスク</param>
        public void AddMultipleChildren(WbsItem? parent)
        {
            if (parent == null) return;
            


            // パフォーマンス向上：事前に全アイテムを作成
            var newItems = new List<WbsItem>();
            int count = 3;
            
            // 事前作成フェーズ：全アイテムをメモリ上で作成
            for (int i = 0; i < count; i++)
            {
                var newItem = CreateWbsItemTemplate(parent, $"サブタスク {i + 1}", $"サブタスク {i + 1} の説明");
                // CreateWbsItemTemplate内でApplyDefaultSettingsが呼ばれるため、追加の設定は不要
                newItems.Add(newItem);
            }
            
            // 一括挿入フェーズ：全アイテムを親に追加
            foreach (var item in newItems)
            {
                parent.AddChild(item);
            }
            
            // 新しく追加されたアイテムをNewWbsItemsリストに追加
            foreach (var item in newItems)
            {
                NewWbsItems.Add(item);
            }
            CanRegisterItems = true;
            OnPropertyChanged(nameof(NewItemsCount));
            
            // 一括追加の場合は常に親タスクを選択（連続追加のため）
            SelectedItem = parent;
            
            // 手動でCanAddChildを更新（OnSelectedItemChangedが呼び出されない場合の対策）
            CanAddChild = parent.IsParentTask;
            
            // MS Projectレベルの高速化：即座にUI更新
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateFlattenedListManually();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// 階層の深さを制限する（デフォルトで5階層まで）
        /// </summary>
        /// <param name="maxDepth">最大階層数</param>
        public void LimitHierarchyDepth(int maxDepth = 5)
        {
            foreach (var item in WbsItems)
            {
                LimitItemDepth(item, 0, maxDepth);
            }
        }

        private void LimitItemDepth(WbsItem item, int currentDepth, int maxDepth)
        {
            if (currentDepth >= maxDepth)
            {
                // 最大階層に達した場合、子アイテムを削除
                item.Children.Clear();
                return;
            }

            foreach (var child in item.Children.ToList())
            {
                LimitItemDepth(child, currentDepth + 1, maxDepth);
            }
        }

        /// <summary>
        /// サブタスクの進捗を親タスクに反映
        /// </summary>
        public void UpdateParentProgress()
        {
            foreach (var item in WbsItems)
            {
                UpdateItemProgress(item);
            }
        }

        private void UpdateItemProgress(WbsItem item)
        {
            if (item.HasChildren)
            {
                // 子アイテムの進捗を再帰的に更新
                foreach (var child in item.Children)
                {
                    UpdateItemProgress(child);
                }

                // 親タスクの進捗を子タスクの平均で更新
                if (item.Children.Count > 0)
                {
                    double totalProgress = item.Children.Sum(child => child.Progress);
                    item.Progress = totalProgress / item.Children.Count;
                }
            }
        }

        private void ExpandAll()
        {
            ExpandCollapseAll(true);
        }

        private void CollapseAll()
        {
            ExpandCollapseAll(false);
        }

        private void ExpandCollapseAll(bool expand)
        {
            void ProcessItem(WbsItem item)
            {
                item.IsExpanded = expand;
                foreach (var child in item.Children)
                {
                    ProcessItem(child);
                }
            }

            foreach (var item in WbsItems)
            {
                ProcessItem(item);
            }
        }

        private void Refresh()
        {
            if (!IsRedmineConnected)
            {
                ErrorMessage = "Redmineに接続されていません。接続を確認してください。";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                // Redmineからのデータ更新処理
                if (SelectedProject != null)
                {
                    LoadRedmineData();
                }
                else
                {
                    ErrorMessage = "プロジェクトが選択されていません。";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"データの更新中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Export()
        {
            // WBSデータのエクスポート処理
            // CSV、Excel、PDF等の形式で出力
        }

        private void Import()
        {
            // WBSデータのインポート処理
            // ファイル選択ダイアログを表示
        }

        public void ToggleExpansion(WbsItem? item)
        {
            if (item == null) return;
            
            // 展開状態を切り替え
            item.IsExpanded = !item.IsExpanded;
            
            // MS Projectレベルの高速化：即座にUI更新
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateFlattenedListManually();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        /// <summary>
        /// 階層構造を平坦化したリストを更新
        /// </summary>
        private async Task UpdateFlattenedList()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "WBSリストを更新中...";
                });

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FlattenedWbsItems.Clear();
                    
                    // 重複チェック用のHashSet
                    var addedItems = new HashSet<WbsItem>();
                    
                    var totalItems = WbsItems.Count;
                    var processedItems = 0;
                    
                    foreach (var rootItem in WbsItems)
                    {
                        AddItemToFlattened(rootItem, addedItems);
                        processedItems++;
                        
                        // 進捗を更新（10アイテムごと）
                        if (processedItems % 10 == 0 || processedItems == totalItems)
                        {
                            var progress = (double)processedItems / totalItems * 50; // 50%まで
                            WbsProgress = progress;
                            WbsProgressMessage = $"WBSリストを更新中... {processedItems}/{totalItems}";
                        }
                    }
                });
                
                // スケジュール表も更新（非同期版を使用）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 100;
                    WbsProgressMessage = "WBSリストの更新が完了しました";
                });
                
                await UpdateScheduleItemsAsync();
                
                // 完了メッセージを少し表示してからクリア
                await Task.Delay(500);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = false;
                    WbsProgress = 0;
                    WbsProgressMessage = string.Empty;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = false;
                    WbsProgress = 0;
                    WbsProgressMessage = $"エラーが発生しました: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// アイテムと（展開されている場合）その子アイテムを平坦化リストに追加（重複チェック付き）
        /// </summary>
        private void AddItemToFlattened(WbsItem item, HashSet<WbsItem> addedItems)
        {
            // 重複チェック
            if (addedItems.Contains(item))
            {
                return;
            }
            
            // UIスレッドで実行されていることを確認
            if (Application.Current.Dispatcher.CheckAccess())
            {
                FlattenedWbsItems.Add(item);
                addedItems.Add(item);
                
                if (item.IsExpanded && item.HasChildren)
                {
                    foreach (var child in item.Children)
                    {
                        AddItemToFlattened(child, addedItems);
                    }
                }
            }
            else
            {
                // UIスレッドでない場合は、UIスレッドで実行
                Application.Current.Dispatcher.Invoke(() => AddItemToFlattened(item, addedItems));
            }
        }



        /// <summary>
        /// タスクを選択する
        /// </summary>
        /// <param name="item">選択するタスク</param>
        public void SelectItem(WbsItem? item)
        {
            // 全てのアイテムの選択状態をクリア
            ClearAllSelections(WbsItems);
            
            // 新しいアイテムを選択
            if (item != null)
            {
                item.IsSelected = true;
                SelectedItem = item;
            }
            else
            {
                SelectedItem = null;
            }
        }

        /// <summary>
        /// 全てのアイテムの選択状態をクリアする
        /// </summary>
        /// <param name="items">アイテムコレクション</param>
        private void ClearAllSelections(ObservableCollection<WbsItem> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = false;
                if (item.Children.Count > 0)
                {
                    ClearAllSelections(item.Children);
                }
            }
        }

        /// <summary>
        /// 上に移動
        /// </summary>
        private void MoveUp()
        {
            if (SelectedItem == null || FlattenedWbsItems.Count == 0) return;

            var currentIndex = FlattenedWbsItems.IndexOf(SelectedItem);
            if (currentIndex > 0)
            {
                SelectedItem = FlattenedWbsItems[currentIndex - 1];
            }
        }

        /// <summary>
        /// 下に移動
        /// </summary>
        private void MoveDown()
        {
            if (SelectedItem == null || FlattenedWbsItems.Count == 0) return;

            var currentIndex = FlattenedWbsItems.IndexOf(SelectedItem);
            if (currentIndex < FlattenedWbsItems.Count - 1)
            {
                SelectedItem = FlattenedWbsItems[currentIndex + 1];
            }
        }

        /// <summary>
        /// 左に移動（前の列）
        /// </summary>
        private void MoveLeft()
        {
            if (SelectedItem == null || FlattenedWbsItems.Count == 0) return;

            var currentIndex = FlattenedWbsItems.IndexOf(SelectedItem);
            var targetIndex = Math.Max(0, currentIndex - 1);
            SelectedItem = FlattenedWbsItems[targetIndex];
        }

        /// <summary>
        /// 右に移動（次の列）
        /// </summary>
        private void MoveRight()
        {
            if (SelectedItem == null || FlattenedWbsItems.Count == 0) return;

            var currentIndex = FlattenedWbsItems.IndexOf(SelectedItem);
            var targetIndex = Math.Min(FlattenedWbsItems.Count - 1, currentIndex + 1);
            SelectedItem = FlattenedWbsItems[targetIndex];
        }

        /// <summary>
        /// スケジュール表を初期化する
        /// </summary>
        private async Task InitializeScheduleItems()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsScheduleLoading = true;
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = "スケジュール表を初期化中...";
                });

                ScheduleItems.Clear();
                
                var startDate = DateTime.Today;
                var endDate = DateTime.Today.AddMonths(2); // 2か月先まで
                
                // 週単位でグループ化して表示
                var currentDate = startDate;
                var totalDays = 0;
                var processedDays = 0;
                
                // まず総日数を計算
                var tempDate = startDate;
                while (tempDate <= endDate)
                {
                    var weekStart = tempDate;
                    while (weekStart.DayOfWeek != System.DayOfWeek.Monday)
                    {
                        weekStart = weekStart.AddDays(-1);
                    }
                    
                    var weekEnd = weekStart.AddDays(6);
                    
                    for (var date = weekStart; date <= weekEnd && date <= endDate; date = date.AddDays(1))
                    {
                        totalDays++;
                    }
                    
                    tempDate = weekEnd.AddDays(1);
                }
                
                // 実際の初期化処理
                currentDate = startDate;
                while (currentDate <= endDate)
                {
                    // 週の開始日（月曜日）を取得
                    var weekStart = currentDate;
                    while (weekStart.DayOfWeek != System.DayOfWeek.Monday)
                    {
                        weekStart = weekStart.AddDays(-1);
                    }
                    
                    // 週の終了日（日曜日）を取得
                    var weekEnd = weekStart.AddDays(6);
                    
                    // 週の各日を追加
                    for (var date = weekStart; date <= weekEnd && date <= endDate; date = date.AddDays(1))
                    {
                        var scheduleItem = new ScheduleItem
                        {
                            Date = date,
                            TaskTitle = GetTaskTitleForDate(date)
                        };
                        
                        ScheduleItems.Add(scheduleItem);
                        processedDays++;
                        
                        // 進捗を更新（10日ごと）
                        if (processedDays % 10 == 0 || processedDays == totalDays)
                        {
                            var progress = (double)processedDays / totalDays * 100;
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ScheduleProgress = progress;
                                ScheduleProgressMessage = $"スケジュール表を初期化中... {processedDays}/{totalDays}";
                            });
                            
                            // UIの応答性を保つために少し待機
                            await Task.Delay(1);
                        }
                    }
                    
                    // 次の週に移動
                    currentDate = weekEnd.AddDays(1);
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScheduleProgress = 100;
                    ScheduleProgressMessage = "スケジュール表の初期化が完了しました";
                    IsScheduleLoading = false;
                });

                // 完了メッセージを少し表示してからクリア
                await Task.Delay(1000);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = string.Empty;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsScheduleLoading = false;
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = $"エラーが発生しました: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// 指定された日付に対応するタスクタイトルを取得する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>タスクタイトル</returns>
        private string GetTaskTitleForDate(DateTime date)
        {
            // WBSアイテムから該当する日付のタスクを検索
            foreach (var item in FlattenedWbsItems)
            {
                if (item.StartDate <= date && date <= item.EndDate)
                {
                    return item.Title;
                }
            }
            
            return string.Empty;
        }

        /// <summary>
        /// 指定された日付が非稼働日かどうかを判定する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>非稼働日の場合はtrue</returns>
        public bool IsNonWorkingDay(DateTime date)
        {
            return date.DayOfWeek == System.DayOfWeek.Saturday || date.DayOfWeek == System.DayOfWeek.Sunday;
        }

        /// <summary>
        /// 指定された日付の背景色を取得する
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>背景色</returns>
        public System.Windows.Media.Brush GetBackgroundColorForDate(DateTime date)
        {
            if (IsNonWorkingDay(date))
            {
                return date.DayOfWeek == System.DayOfWeek.Saturday 
                    ? System.Windows.Media.Brushes.LightBlue 
                    : System.Windows.Media.Brushes.LightPink;
            }
            return System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// スケジュール表のタスク情報を更新する
        /// </summary>
        private async Task UpdateScheduleItemsAsync()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsScheduleLoading = true;
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = "スケジュール表を描画中...";
                });

                var totalItems = ScheduleItems.Count;
                var processedItems = 0;

                // バッチ処理で進捗を更新
                var batchSize = Math.Max(1, totalItems / 20); // 20回に分けて進捗を更新

                for (int i = 0; i < totalItems; i++)
                {
                    var scheduleItem = ScheduleItems[i];
                    scheduleItem.TaskTitle = GetTaskTitleForDate(scheduleItem.Date);

                    processedItems++;
                    
                    // バッチサイズごとに進捗を更新
                    if (processedItems % batchSize == 0 || processedItems == totalItems)
                    {
                        var progress = (double)processedItems / totalItems * 100;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ScheduleProgress = progress;
                            ScheduleProgressMessage = $"スケジュール表を描画中... {processedItems}/{totalItems}";
                        });

                        // UIの応答性を保つために少し待機
                        await Task.Delay(1);
                    }
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScheduleProgress = 100;
                    ScheduleProgressMessage = "スケジュール表の描画が完了しました";
                    IsScheduleLoading = false;
                });

                // 完了メッセージを少し表示してからクリア
                await Task.Delay(1000);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = string.Empty;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsScheduleLoading = false;
                    ScheduleProgress = 0;
                    ScheduleProgressMessage = $"エラーが発生しました: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// スケジュール表のタスク情報を更新する（同期版）
        /// </summary>
        private void UpdateScheduleItems()
        {
            foreach (var scheduleItem in ScheduleItems)
            {
                scheduleItem.TaskTitle = GetTaskTitleForDate(scheduleItem.Date);
            }
        }

        /// <summary>
        /// プロジェクト一覧を読み込む（非同期版）
        /// </summary>
        private async Task LoadProjectsAsync(RedmineService redmineService)
        {
            try
            {
                var projects = await redmineService.GetProjectsAsync();
                
                if (projects != null && projects.Count > 0)
                {
                    AvailableProjects.Clear();
                    foreach (var project in projects)
                    {
                        AvailableProjects.Add(project);
                    }
                    
                    // 設定から選択されたプロジェクトIDを復元
                    if (AppConfig.SelectedProjectId.HasValue && AvailableProjects.Count > 0)
                    {
                        var restoredProject = AvailableProjects.FirstOrDefault(p => p.Id == AppConfig.SelectedProjectId.Value);
                        if (restoredProject != null)
                        {
                            SelectedProject = restoredProject;
                            
                            // プロジェクトが復元された場合、自動的にRedmineデータを読み込む
                            if (IsRedmineConnected)
                            {
                                await Task.Delay(300);
                                await LoadRedmineDataAsync();
                            }
                            return; // 復元成功
                        }
                    }
                    
                    // プロジェクトが1つの場合は自動選択
                    if (AvailableProjects.Count == 1)
                    {
                        var singleProject = AvailableProjects[0];
                        SelectedProject = singleProject;
                        IsProjectSelectionReadOnly = true;
                        
                        // 自動選択されたプロジェクトのデータを読み込む
                        if (IsRedmineConnected)
                        {
                            await Task.Delay(300);
                            await LoadRedmineDataAsync();
                        }
                    }
                    // プロジェクトが複数ある場合は最初のプロジェクトを選択（従来の動作）
                    else if (AvailableProjects.Count > 1)
                    {
                        SelectedProject = AvailableProjects[0];
                        IsProjectSelectionReadOnly = false;
                    }
                    else
                    {
                        IsProjectSelectionReadOnly = false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"プロジェクト一覧の読み込みに失敗しました: {ex.Message}";
            }
        }

        /// <summary>
        /// Redmineデータを読み込む（非同期版）
        /// </summary>
        private async Task LoadRedmineDataAsync()
        {
            if (SelectedProject == null)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = "プロジェクトが選択されていません";
                });
                return;
            }

            try
            {
                // UIスレッドでの状態更新を最小限に
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                });

                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    await LoadRedmineIssuesAsync(SelectedProject.Id).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Redmineデータの読み込みに失敗しました: {ex.Message}";
                });
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        /// <summary>
        /// Redmineデータを読み込む（同期的なラッパー）
        /// </summary>
        public void LoadRedmineData()
        {
            if (!IsRedmineConnected)
            {
                ErrorMessage = "Redmineに接続されていません。接続を確認してください。";
                return;
            }

            if (SelectedProject == null)
            {
                ErrorMessage = "プロジェクトが選択されていません。上記のプロジェクト選択プルダウンからプロジェクトを選択してください。";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    LoadRedmineIssues(SelectedProject.Id);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Redmineデータの読み込みに失敗しました: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 指定されたプロジェクトのチケットを読み込む（同期的なラッパー）
        /// </summary>
        private void LoadRedmineIssues(int projectId)
        {
            try
            {
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    // 非同期版を使用して同期的に実行
                    var issues = redmineService.GetIssuesWithHierarchyAsync(projectId).GetAwaiter().GetResult();
                    
                    // WBSアイテムを完全にクリア（親子関係も含めて）
                    WbsItems.Clear();
                    
                    // ルートレベルのチケットのみを処理
                    var rootIssues = issues.Where(issue => issue.Parent == null).ToList();
                    
                    foreach (var rootIssue in rootIssues)
                    {
                        var wbsItem = ConvertRedmineIssueToWbsItem(rootIssue);
                        WbsItems.Add(wbsItem);
                    }
                    
                    // 平坦化リストを更新
                    _ = Task.Run(async () => await UpdateFlattenedList());
                    
                    // Redmineデータ読み込み後、選択状態を復元
                    if (SelectedItem != null)
                    {
                        // 選択されたアイテムがまだ存在するかチェック
                        var currentSelectedItem = SelectedItem;
                        var foundItem = WbsItems.FirstOrDefault(item => item.Id == currentSelectedItem.Id);
                        if (foundItem != null)
                        {
                            SelectedItem = foundItem;
                            CanAddChild = foundItem.IsParentTask;
                        }
                    }
                    
                    IsRedmineDataLoaded = true;
                    ErrorMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // より詳細なエラー情報を提供
                var errorMessage = $"プロジェクトID {projectId} のチケットの読み込みに失敗しました。";
                if (ex is RedmineApiException redmineEx)
                {
                    errorMessage += $" Redmine API エラー: {redmineEx.Message}";
                }
                else
                {
                    errorMessage += $" エラー: {ex.Message}";
                }
                
                ErrorMessage = errorMessage;
                IsRedmineDataLoaded = false;
            }
        }

        /// <summary>
        /// Redmineデータを更新する（非同期版）
        /// </summary>
        private async Task RefreshRedmineDataAsync()
        {
            await LoadRedmineDataAsync();
        }

        /// <summary>
        /// 指定されたプロジェクトのチケットを読み込む（非同期版）
        /// </summary>
        private async Task LoadRedmineIssuesAsync(int projectId)
        {
            try
            {
                // プログレスバーを開始
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "Redmineからデータを取得中...";
                });

                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    // データ取得（20%）
                    var issues = await redmineService.GetIssuesWithHierarchyAsync(projectId).ConfigureAwait(false);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WbsProgress = 20;
                        WbsProgressMessage = "チケットデータを変換中...";
                    });
                    
                    // バックグラウンドスレッドでWBSアイテムの変換を実行（40%）
                    var wbsItems = await Task.Run(() =>
                    {
                        var tempItems = new List<WbsItem>();
                        // ルートレベルのチケットのみを処理
                        var rootIssues = issues.Where(issue => issue.Parent == null).ToList();
                        
                        foreach (var rootIssue in rootIssues)
                        {
                            var wbsItem = ConvertRedmineIssueToWbsItem(rootIssue);
                            tempItems.Add(wbsItem);
                        }
                        return tempItems;
                    }).ConfigureAwait(false);
                    
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WbsProgress = 60;
                        WbsProgressMessage = "UIコレクションを更新中...";
                    });
                    
                    // UIスレッドでコレクションを更新（80%）
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // WBSアイテムを完全にクリア（親子関係も含めて）
                            WbsItems.Clear();
                            foreach (var wbsItem in wbsItems)
                            {
                                WbsItems.Add(wbsItem);
                            }
                            
                            WbsProgress = 80;
                            WbsProgressMessage = "平坦化リストを更新中...";
                            
                            // 平坦化リストを更新
                            _ = Task.Run(async () => await UpdateFlattenedList());
                            
                            // Redmineデータ読み込み後、選択状態を復元
                            if (SelectedItem != null)
                            {
                                // 選択されたアイテムがまだ存在するかチェック
                                var currentSelectedItem = SelectedItem;
                                var foundItem = WbsItems.FirstOrDefault(item => item.Id == currentSelectedItem.Id);
                                if (foundItem != null)
                                {
                                    SelectedItem = foundItem;
                                    CanAddChild = foundItem.IsParentTask;
                                }
                            }
                            
                            IsRedmineDataLoaded = true;
                            ErrorMessage = string.Empty;
                            
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = $"UI更新中にエラーが発生しました: {ex.Message}";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"チケットの読み込みに失敗しました: {ex.Message}";
                });
            }
            finally
            {
                // プログレスバーを完了
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    WbsProgress = 100;
                    WbsProgressMessage = "データ読み込み完了";
                    
                    // 完了メッセージを少し表示してから非表示
                    await Task.Delay(1000);
                    
                    IsWbsLoading = false;
                    WbsProgress = 0;
                });
            }
        }

                /// <summary>
        /// RedmineチケットをWBSアイテムに変換
        /// </summary>
        private WbsItem ConvertRedmineIssueToWbsItem(HierarchicalIssue issue)
        {
            var convertedItems = new Dictionary<int, WbsItem>();
            
            // データの整合性を事前チェック
            ValidateIssueHierarchy(issue);
            
            return ConvertRedmineIssueToWbsItemInternal(issue, convertedItems);
        }
        
        /// <summary>
        /// チケット階層の整合性をチェック
        /// </summary>
        private void ValidateIssueHierarchy(HierarchicalIssue issue, HashSet<int>? visitedIds = null)
        {
            if (visitedIds == null)
                visitedIds = new HashSet<int>();
            
            if (visitedIds.Contains(issue.Id))
            {
                return;
            }
            
            visitedIds.Add(issue.Id);
            
            // 子チケットの整合性チェック
            if (issue.Children != null)
            {
                foreach (var child in issue.Children)
                {
                    // 子チケットの親が正しく設定されているかチェック
                    if (child.Parent == null || child.Parent.Id != issue.Id)
                    {
                        // 親子関係の不整合を検出
                    }
                    
                    ValidateIssueHierarchy(child, visitedIds);
                }
            }
            
            visitedIds.Remove(issue.Id);
        }
        
        /// <summary>
        /// RedmineチケットをWBSアイテムに変換（内部実装、重複チェック付き）
        /// </summary>
        private WbsItem? ConvertRedmineIssueToWbsItemInternal(HierarchicalIssue issue, Dictionary<int, WbsItem> convertedItems, HashSet<int>? parentChain = null)
        {
            // 循環参照を防ぐための親チェーン
            if (parentChain == null)
                parentChain = new HashSet<int>();
            
            // 循環参照チェック
            if (parentChain.Contains(issue.Id))
            {
                return null; // 循環参照の場合はnullを返す
            }
            
            // 既に変換済みのアイテムがある場合は再利用
            if (convertedItems.ContainsKey(issue.Id))
            {
                var existingItem = convertedItems[issue.Id];
                
                // 既存アイテムの子アイテムをクリアしてから再設定
                existingItem.Children.Clear();
                
                // 親チェーンに追加
                parentChain.Add(issue.Id);
                
                // 子チケットを再帰的に変換して設定
                foreach (var childIssue in issue.Children)
                {
                    var childWbsItem = ConvertRedmineIssueToWbsItemInternal(childIssue, convertedItems, parentChain);
                    if (childWbsItem != null) // nullチェック
                    {
                        existingItem.AddChild(childWbsItem);
                    }
                }
                
                // 親チェーンから削除
                parentChain.Remove(issue.Id);
                
                return existingItem;
            }

            var wbsItem = new WbsItem
            {
                Id = issue.Id.ToString(),
                Title = issue.Subject ?? string.Empty,
                Description = issue.Description ?? string.Empty,
                StartDate = issue.StartDate ?? DateTime.Today,
                EndDate = issue.DueDate ?? (issue.StartDate?.AddDays(1) ?? DateTime.Today.AddDays(1)),
                Progress = issue.DoneRatio ?? 0,
                Status = issue.Status?.Name ?? "未着手",
                Priority = issue.Priority?.Name ?? "中",
                Assignee = issue.AssignedTo?.Name ?? "未割り当て",
                RedmineIssueId = issue.Id,
                RedmineProjectId = issue.Project?.Id ?? 0,
                RedmineProjectName = issue.Project?.Name ?? string.Empty,
                RedmineTracker = issue.Tracker?.Name ?? string.Empty,
                RedmineAuthor = issue.Author?.Name ?? string.Empty,
                RedmineCreatedOn = issue.CreatedOn ?? DateTime.Today,
                RedmineUpdatedOn = issue.UpdatedOn ?? DateTime.Today,
                RedmineUrl = $"{AppConfig.RedmineHost}/issues/{issue.Id}"
            };

            // 変換済みアイテムとして記録
            convertedItems[issue.Id] = wbsItem;

            // 親チェーンに追加
            parentChain.Add(issue.Id);
            
            // 子チケットがある場合のみ親子関係を設定
            if (issue.Children != null && issue.Children.Count > 0)
            {
                foreach (var childIssue in issue.Children)
                {
                    var childWbsItem = ConvertRedmineIssueToWbsItemInternal(childIssue, convertedItems, parentChain);
                    if (childWbsItem != null) // nullチェック
                    {
                        wbsItem.AddChild(childWbsItem);
                    }
                }
            }
            
            // 親チェーンから削除
            parentChain.Remove(issue.Id);

            return wbsItem;
        }

        /// <summary>
        /// Dispatches the specified action on the UI thread.
        /// </summary>
        /// <param name="action">The action to be dispatched.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected static async Task DispatchAsync(Action action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(action);
        }

        /// <summary>
        /// 新しいチケットを作成
        /// </summary>
        private void CreateNewIssue()
        {
            if (SelectedProject == null)
            {
                ErrorMessage = "プロジェクトが選択されていません。プロジェクトを選択してからチケットを作成してください。";
                return;
            }

            if (!IsRedmineConnected)
            {
                ErrorMessage = "Redmineに接続されていません。接続を確認してからチケットを作成してください。";
                return;
            }

            try
            {
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    var viewModel = new CreateIssueViewModel(redmineService, SelectedProject);
                    var window = new CreateIssueWindow(viewModel);
                    
                    // ダイアログを表示
                    var result = window.ShowDialog();
                    
                    // チケットが作成された場合は、プロジェクトのデータを再読み込み
                    if (result == true)
                    {
                        LoadRedmineData();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"チケット作成ダイアログの表示に失敗しました: {ex.Message}";
            }
        }

        /// <summary>
        /// 設定画面を開く
        /// </summary>
        private void OpenSettings()
        {
            // TODO: 設定画面の実装
            System.Windows.MessageBox.Show("設定画面は今後実装予定です。", "設定", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// 階層構造を平坦化したリストを更新（無限ループを避けるため）
        /// </summary>
        private async Task UpdateFlattenedListSafely()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "WBSリストを安全に更新中...";
                });

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FlattenedWbsItems.Clear();
                    
                    // 重複チェック用のHashSet
                    var addedItems = new HashSet<WbsItem>();
                    
                    var totalItems = WbsItems.Count;
                    var processedItems = 0;
                    
                    foreach (var rootItem in WbsItems)
                    {
                        AddItemToFlattened(rootItem, addedItems);
                        processedItems++;
                        
                        // 進捗を更新（10アイテムごと）
                        if (processedItems % 10 == 0 || processedItems == totalItems)
                        {
                            var progress = (double)processedItems / totalItems * 50; // 50%まで
                            WbsProgress = progress;
                            WbsProgressMessage = $"WBSリストを安全に更新中... {processedItems}/{totalItems}";
                        }
                    }
                });
                
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 100;
                    WbsProgressMessage = "WBSリストの安全な更新が完了しました";
                });
                
                await UpdateScheduleItemsAsync();
                
                // 完了メッセージを少し表示してからクリア
                await Task.Delay(1000);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = false;
                    WbsProgress = 0;
                    WbsProgressMessage = string.Empty;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = false;
                    WbsProgress = 0;
                    WbsProgressMessage = $"エラーが発生しました: {ex.Message}";
                });
            }
        }

        /// <summary>
        /// 階層構造を平坦化したリストを手動で更新（MS Projectレベルの高速化）
        /// </summary>
        private void UpdateFlattenedListManually()
        {
            // 現在の選択状態を保存
            var currentSelectedItem = SelectedItem;
            
            // パフォーマンス向上のため、バッチ処理で更新
            var tempList = new List<WbsItem>(WbsItems.Count * 2); // 事前に容量を確保
            
            // 並列処理で高速化（大量データの場合）
            if (WbsItems.Count > 100)
            {
                var tasks = WbsItems.Select(rootItem => Task.Run(() => 
                {
                    var localList = new List<WbsItem>();
                    AddItemToFlattenedOptimized(rootItem, localList);
                    return localList;
                })).ToArray();
                
                Task.WaitAll(tasks);
                
                foreach (var task in tasks)
                {
                    tempList.AddRange(task.Result);
                }
            }
            else
            {
                // 小規模データの場合は通常処理
                foreach (var rootItem in WbsItems)
                {
                    AddItemToFlattenedOptimized(rootItem, tempList);
                }
            }
            
            // UIスレッドで一括更新（パフォーマンス向上）
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                FlattenedWbsItems.Clear();
                foreach (var item in tempList)
                {
                    FlattenedWbsItems.Add(item);
                }
                
                // 選択状態を復元
                if (currentSelectedItem != null)
                {
                    SelectedItem = currentSelectedItem;
                }
                
                // スケジュール表も更新（非同期版を使用）
                _ = Task.Run(async () => await UpdateScheduleItemsAsync());
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        
        /// <summary>
        /// 最適化された平坦化処理（メモリ効率向上、重複チェック付き）
        /// </summary>
        private void AddItemToFlattenedOptimized(WbsItem item, List<WbsItem> targetList)
        {
            // 重複チェック
            if (targetList.Contains(item))
            {
                return;
            }
            
            targetList.Add(item);
            
            if (item.IsExpanded && item.HasChildren)
            {
                foreach (var child in item.Children)
                {
                    AddItemToFlattenedOptimized(child, targetList);
                }
            }
        }

                 /// <summary>
         /// 新しく追加されたWBSアイテムをRedmineに登録する
         /// </summary>
         private async void RegisterItems()
         {
             if (!IsRedmineConnected)
             {
                 ErrorMessage = "Redmineに接続されていません。接続を確認してください。";
                 return;
             }

             if (SelectedProject == null)
             {
                 ErrorMessage = "プロジェクトが選択されていません。プロジェクトを選択してから登録を実行してください。";
                 return;
             }

             if (NewWbsItems.Count == 0)
             {
                 ErrorMessage = "登録するアイテムがありません。";
                 return;
             }

             try
             {
                 IsRegistering = true;
                 ErrorMessage = string.Empty;

                 using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                 {
                     int successCount = 0;
                     var itemsToRegister = NewWbsItems.ToList();



                     // 階層順にソート（親タスクを先に登録）
                     var sortedItems = itemsToRegister.OrderBy(item => item.Level).ToList();
                     
                     // 親タスクを先に登録
                     foreach (var newItem in sortedItems.Where(item => item.Parent == null))
                     {
                         try
                         {
                             var newIssue = new Issue
                             {
                                 Subject = newItem.Title,
                                 Description = newItem.Description,
                                 StartDate = newItem.StartDate,
                                 DueDate = newItem.EndDate
                             };
                             
                             // プロジェクトを設定（Projectオブジェクトを使用）
                             var project = new Project();
                             // Idプロパティをリフレクションで設定
                             var projectIdProperty = typeof(Project).GetProperty("Id");
                             if (projectIdProperty?.CanWrite == true)
                             {
                                 projectIdProperty.SetValue(project, SelectedProject.Id);
                             }
                             newIssue.Project = project;

                             // トラッカーを設定（Trackerオブジェクトを使用）
                             var tracker = new Tracker();
                             // Idプロパティをリフレクションで設定
                             var trackerIdProperty = typeof(Tracker).GetProperty("Id");
                             if (trackerIdProperty?.CanWrite == true)
                             {
                                 trackerIdProperty.SetValue(tracker, AppConfig.DefaultTrackerId);
                             }
                             newIssue.Tracker = tracker;

                             // ステータスを設定（IssueStatusオブジェクトを使用）
                             var status = new IssueStatus();
                             // Idプロパティをリフレクションで設定
                             var statusIdProperty = typeof(IssueStatus).GetProperty("Id");
                             if (statusIdProperty?.CanWrite == true)
                             {
                                 statusIdProperty.SetValue(status, AppConfig.DefaultStatusId);
                             }
                             newIssue.Status = status;

                             var createdIssueId = await redmineService.CreateIssueAsync(newIssue);
                             
                             // 登録成功したアイテムに RedmineIssueId を設定
                             newItem.RedmineIssueId = createdIssueId;
                             newItem.IsNew = false; // 新規フラグを無効化
                             
                             successCount++;
                         }
                         catch (Exception ex)
                         {
                             // 親タスクの登録に失敗
                             ErrorMessage = $"親タスク '{newItem.Title}' の登録に失敗しました: {ex.Message}";
                         }
                     }

                     // 子タスクを登録（親タスクのIDを設定）
                     foreach (var newItem in sortedItems.Where(item => item.Parent != null))
                     {
                         try
                         {
                             var newIssue = new Issue
                             {
                                 Subject = newItem.Title,
                                 Description = newItem.Description,
                                 StartDate = newItem.StartDate,
                                 DueDate = newItem.EndDate
                             };
                             
                             // プロジェクトを設定（Projectオブジェクトを使用）
                             var project2 = new Project();
                             // Idプロパティをリフレクションで設定
                             var projectIdProperty = typeof(Project).GetProperty("Id");
                             if (projectIdProperty?.CanWrite == true)
                             {
                                 projectIdProperty.SetValue(project2, SelectedProject.Id);
                             }
                             newIssue.Project = project2;

                             // トラッカーを設定（Trackerオブジェクトを使用）
                             var tracker = new Tracker();
                             // Idプロパティをリフレクションで設定
                             var trackerIdProperty = typeof(Tracker).GetProperty("Id");
                             if (trackerIdProperty?.CanWrite == true)
                             {
                                 trackerIdProperty.SetValue(tracker, AppConfig.DefaultTrackerId);
                             }
                             newIssue.Tracker = tracker;

                             // ステータスを設定（IssueStatusオブジェクトを使用）
                             var status = new IssueStatus();
                             // Idプロパティをリフレクションで設定
                             var statusIdProperty = typeof(IssueStatus).GetProperty("Id");
                             if (statusIdProperty?.CanWrite == true)
                             {
                                 statusIdProperty.SetValue(status, AppConfig.DefaultStatusId);
                             }
                             newIssue.Status = status;

                             // 親タスクのIDを設定
                             if (newItem.Parent?.RedmineIssueId.HasValue == true)
                             {
                                 var parentIdProperty = typeof(Issue).GetProperty("ParentId");
                                 if (parentIdProperty?.CanWrite == true)
                                 {
                                     parentIdProperty.SetValue(newIssue, newItem.Parent.RedmineIssueId.Value);
                                 }
                             }

                             var createdIssueId = await redmineService.CreateIssueAsync(newIssue);
                             
                             // 登録成功したアイテムに RedmineIssueId を設定
                             newItem.RedmineIssueId = createdIssueId;
                             newItem.IsNew = false; // 新規フラグを無効化
                             
                             successCount++;
                         }
                         catch (Exception ex)
                         {
                             // 子タスクの登録に失敗
                             ErrorMessage = $"子タスク '{newItem.Title}' の登録に失敗しました: {ex.Message}";
                         }
                     }

                     if (successCount > 0)
                     {
                         // 登録成功したアイテムを NewWbsItems から削除
                         foreach (var item in itemsToRegister.Where(i => i.RedmineIssueId.HasValue))
                         {
                             NewWbsItems.Remove(item);
                         }
                         
                         CanRegisterItems = NewWbsItems.Count > 0;
                         OnPropertyChanged(nameof(NewItemsCount));
                         _ = Task.Run(async () => await UpdateFlattenedList());
                         
                         ErrorMessage = $"{successCount} 件のチケットを登録しました。";
                     }
                     else
                     {
                         ErrorMessage = "チケットの登録に失敗しました。";
                     }
                 }
             }
             catch (Exception ex)
             {
                 ErrorMessage = $"チケットの登録中にエラーが発生しました: {ex.Message}";
             }
             finally
             {
                 IsRegistering = false;
             }
         }

        /// <summary>
        /// Redmineからチケットを読み込む（同期版）
        /// </summary>
        private void LoadRedmineIssues()
        {
            if (!IsRedmineConnected || SelectedProject == null)
                return;

            try
            {
                var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                var issues = redmineService.GetIssuesWithHierarchyAsync(SelectedProject.Id).GetAwaiter().GetResult();
                
                // 既存のアイテムを完全にクリア
                WbsItems.Clear();
                
                // ルートレベルのチケットのみを処理
                var rootIssues = issues.Where(issue => issue.Parent == null).ToList();
                
                foreach (var rootIssue in rootIssues)
                {
                    var wbsItem = ConvertRedmineIssueToWbsItem(rootIssue);
                    WbsItems.Add(wbsItem);
                }
                
                                    // 平坦化リストを更新
                    _ = Task.Run(async () => await UpdateFlattenedList());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"チケットの読み込み中にエラーが発生しました: {ex.Message}";
            }
        }
    }
}


