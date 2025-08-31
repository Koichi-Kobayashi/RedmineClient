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

        private WbsItem? _selectedItem;

        public WbsItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
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
            }
        }

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

        /// <summary>
        /// 依存関係矢印の表示/非表示
        /// true: 依存関係矢印を表示
        /// false: 依存関係矢印を非表示
        /// </summary>
        [ObservableProperty]
        private bool _showDependencyArrows = true;

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
        private bool _showTodayLine = true;

        public bool ShowTodayLine
        {
            get => _showTodayLine;
            set
            {
                if (SetProperty(ref _showTodayLine, value))
                {
                    // 設定を保存
                    AppConfig.ShowTodayLine = value;
                    AppConfig.Save();
                }
            }
        }

        /// <summary>
        /// Redmineサービス
        /// </summary>
        private readonly RedmineService? _redmineService;

        /// <summary>
        /// ガントチャート表示するかどうか
        /// </summary>
        [ObservableProperty]
        private bool _isGanttChartVisible = false;

        /// <summary>
        /// 日付変更の監視を有効にするかどうか
        /// </summary>
        [ObservableProperty]
        private bool _isDateChangeWatchingEnabled = false;

        /// <summary>
        /// 日付変更の監視を開始する
        /// </summary>
        public void StartDateChangeWatching()
        {
            IsDateChangeWatchingEnabled = true;
        }

        /// <summary>
        /// 日付変更の監視を停止する
        /// </summary>
        public void StopDateChangeWatching()
        {
            IsDateChangeWatchingEnabled = false;
        }

        /// <summary>
        /// 日付変更時の更新処理を実行する
        /// </summary>
        /// <param name="task">変更されたタスク</param>
        /// <param name="oldStartDate">変更前の開始日</param>
        /// <param name="oldEndDate">変更前の終了日</param>
        public async Task UpdateTaskScheduleAsync(WbsItem task, DateTime oldStartDate, DateTime oldEndDate)
        {
            try
            {
                if (!IsDateChangeWatchingEnabled)
                {
                    return;
                }

                if (task == null)
                {
                    return;
                }

                // 新規登録時は更新処理を実行しない
                if (int.TryParse(task.Id, out int taskId) && taskId <= 0)
                {
                    return;
                }

                // Redmineに更新を送信
                if (IsRedmineConnected && SelectedProject != null)
                {
                    try
                    {
                        await UpdateRedmineIssueAsync(task, oldStartDate, oldEndDate);
                        // 更新が成功したら未保存フラグをクリア
                        task.HasUnsavedChanges = false;
                    }
                    catch (RedmineClient.Services.RedmineApiException redmineEx)
                    {
                        // Redmine API固有のエラーの場合はログ出力
                        System.Diagnostics.Debug.WriteLine($"Redmine更新エラー (タスク: {task.Title}): {redmineEx.Message}");
                        // 未保存フラグを設定
                        task.HasUnsavedChanges = true;
                        // 必要に応じてユーザーに通知
                        ShowErrorMessage($"タスク '{task.Title}' のRedmine更新に失敗しました: {redmineEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        // その他の予期しないエラーの場合はログ出力
                        System.Diagnostics.Debug.WriteLine($"タスク更新で予期しないエラー (タスク: {task.Title}): {ex.Message}");
                        // 未保存フラグを設定
                        task.HasUnsavedChanges = true;
                        // 必要に応じてユーザーに通知
                        ShowErrorMessage($"タスク '{task.Title}' の更新で予期しないエラーが発生しました: {ex.Message}");
                    }
                }
                else
                {
                    // Redmineに接続されていない場合は未保存フラグを設定
                    task.HasUnsavedChanges = true;
                }

                // スケジュール表を再生成
                try
                {
                    await RefreshScheduleAsync();
                }
                catch (Exception)
                {
                    // スケジュール表再生成でエラーが発生した場合は無視
                }
            }
            catch (Exception)
            {
                // UpdateTaskScheduleAsyncで予期しないエラーが発生した場合は無視
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
        private string _scheduleStartYearMonth = DateTime.Now.ToString("yyyy/MM");

        public string ScheduleStartYearMonth
        {
            get => _scheduleStartYearMonth;
            set
            {
                if (SetProperty(ref _scheduleStartYearMonth, value))
                {
                    // 設定を保存
                    AppConfig.ScheduleStartYearMonth = value;
                    AppConfig.Save();
                }
            }
        }

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

        private Project? _selectedProject;

        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    if (value != null)
                    {
                        // 選択されたプロジェクトIDを保存
                        AppConfig.SelectedProjectId = value.Id;
                        AppConfig.Save();

                        // プロジェクトが変更された場合、Redmineデータを自動的に読み込む
                        if (IsRedmineConnected)
                        {
                            // 重複実行を防ぐためのロック
                            lock (_dataLoadingLock)
                            {
                                if (_isLoadingRedmineData)
                                {
                                    System.Diagnostics.Debug.WriteLine($"プロジェクト {value.Id} のデータ読み込みは既に実行中です。重複実行をスキップします。");
                                    return;
                                }
                                _isLoadingRedmineData = true;
                            }

                            System.Diagnostics.Debug.WriteLine($"プロジェクト {value.Id} のデータ読み込みを開始します。");

                            // 非同期処理を開始（プロパティのsetter内ではawaitできないため）
                            _ = LoadProjectDataAsync(value);
                        }
                    }
                    else
                    {
                        // 接続されていない場合は何もしない（接続テストは削除）
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
        }

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
        /// データ読み込みの重複実行を防ぐためのロックオブジェクト
        /// </summary>
        private readonly object _dataLoadingLock = new object();

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
        public ICommand DeleteSelectedItemCommand { get; }
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
        public ICommand RemoveDependencyCommand { get; }
        public ICommand RemovePredecessorCommand { get; }
        public ICommand RemoveSuccessorCommand { get; }

        public WbsViewModel()
        {
            // 初期状態でプログレスバーを表示
            IsWbsLoading = true;
            WbsProgress = 0;
            WbsProgressMessage = "WBSページを初期化中...";

            // 設定からスケジュール開始年月を読み込み（getアクセサーを呼び出さない）
            _scheduleStartYearMonth = AppConfig.GetScheduleStartYearMonthForInitialization();

            // Redmineサービスを初期化
            try
            {
                if (!string.IsNullOrEmpty(AppConfig.RedmineHost) && !string.IsNullOrEmpty(AppConfig.ApiKey))
                {
                    _redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                }
            }
            catch
            {
                // Redmineサービスの初期化に失敗した場合は無視
            }

            AddRootItemCommand = new RelayCommand(AddRootItem);
            AddChildItemCommand = new RelayCommand<WbsItem>(AddChildItem);
            DeleteItemCommand = new RelayCommand<WbsItem>(DeleteItem);
            DeleteSelectedItemCommand = new RelayCommand(DeleteSelectedItem);
            EditItemCommand = new RelayCommand<WbsItem>(EditItem);
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            RefreshCommand = new RelayCommand(() => Refresh());
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
            // TestConnectionCommand = new AsyncRelayCommand(TestRedmineConnection); // テスト接続は削除
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
            RemoveDependencyCommand = new RelayCommand<WbsItem?>(RemoveDependency);
            RemovePredecessorCommand = new RelayCommand<WbsItem?>(RemovePredecessor);
            RemoveSuccessorCommand = new RelayCommand<WbsItem?>(RemoveSuccessor);
        }

        public virtual async Task OnNavigatedToAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedTo, cts.Token);
            }
        }

        public virtual async void OnNavigatedTo()
        {
            await InitializeViewModel();
        }

        public async Task InitializeViewModel()
        {
            // 即座にプログレスバーを表示（初期化開始）
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsWbsLoading = true;
                WbsProgress = 0;
                WbsProgressMessage = "WBSページを初期化中...";
            });

            try
            {
                // Redmine接続状態の確認
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 10;
                    WbsProgressMessage = "Redmine接続状態を確認中...";
                    
                    // Redmine接続状態を確認
                    if (!string.IsNullOrEmpty(AppConfig.RedmineHost) && !string.IsNullOrEmpty(AppConfig.ApiKey))
                    {
                        IsRedmineConnected = true;
                        ConnectionStatus = "接続済み";
                        System.Diagnostics.Debug.WriteLine($"Redmine接続確認: Host={AppConfig.RedmineHost}, 接続状態={IsRedmineConnected}");
                    }
                    else
                    {
                        IsRedmineConnected = false;
                        ConnectionStatus = "未接続";
                        System.Diagnostics.Debug.WriteLine("Redmine接続情報が不足しています");
                    }
                });

                // プロジェクト選択の初期化
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 20;
                    WbsProgressMessage = "プロジェクト情報を初期化中...";
                });
                if (AvailableProjects.Count == 0)
                {
                    AvailableProjects = new List<Project>();
                }

                // Redmineデータの読み込みはDataGridのロードイベントで行うため、ここでは削除
                if (!IsRedmineConnected)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WbsProgress = 30;
                        WbsProgressMessage = "接続状態を確認中...";
                    });
                    // Redmineに接続されていない場合のメッセージ
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ErrorMessage = "Redmineに接続されていません。設定画面で接続情報を設定してください。";
                        WbsItems.Clear();
                        FlattenedWbsItems.Clear();
                        
                        // FlattenedWbsItemsを初期化
                        FlattenedWbsItems.Clear();
                    });
                }
                else
                {
                    // Redmineに接続されている場合はプロジェクト一覧を取得
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        WbsProgress = 30;
                        WbsProgressMessage = "プロジェクト一覧を取得中...";
                    });
                    
                    try
                    {
                        using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                        {
                            var projects = await redmineService.GetProjectsAsync();
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                AvailableProjects = projects;
                                System.Diagnostics.Debug.WriteLine($"プロジェクト一覧を取得しました: {projects.Count}件");
                                
                                // 保存されたプロジェクトIDがある場合はそのプロジェクトを選択
                                if (AppConfig.SelectedProjectId.HasValue)
                                {
                                    var savedProject = projects.FirstOrDefault(p => p.Id == AppConfig.SelectedProjectId.Value);
                                    if (savedProject != null)
                                    {
                                        SelectedProject = savedProject;
                                        System.Diagnostics.Debug.WriteLine($"保存されたプロジェクトを選択しました: {savedProject.Name} (ID: {savedProject.Id})");
                                    }
                                }
                                
                                // FlattenedWbsItemsを初期化（プロジェクト一覧取得完了後）
                                if (FlattenedWbsItems == null)
                                {
                                    FlattenedWbsItems = new ObservableCollection<WbsItem>();
                                }
                                FlattenedWbsItems.Clear();
                                
                                // FlattenedWbsItemsを初期化完了
                                System.Diagnostics.Debug.WriteLine("プロジェクト一覧取得後: FlattenedWbsItemsを初期化しました");
                                
                                System.Diagnostics.Debug.WriteLine("プロジェクト一覧取得後: FlattenedWbsItemsを初期化しました");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ErrorMessage = $"プロジェクト一覧の取得に失敗しました: {ex.Message}";
                            System.Diagnostics.Debug.WriteLine($"プロジェクト一覧取得エラー: {ex.Message}");
                        });
                    }
                }

                // 平坦化リストを初期化（UIスレッドで実行）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 50;
                    WbsProgressMessage = "WBSリストを初期化中...";
                });
                await UpdateFlattenedList();

                // スケジュール表を初期化
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 70;
                    WbsProgressMessage = "スケジュール表を初期化中...";
                });
                await InitializeScheduleItems();

                // 完了
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 100;
                    WbsProgressMessage = "初期化完了！";
                    
                    // 初期化完了後のデバッグ出力
                    System.Diagnostics.Debug.WriteLine($"ViewModel初期化完了: FlattenedWbsItems.Count = {FlattenedWbsItems?.Count ?? 0}");
                    System.Diagnostics.Debug.WriteLine($"ViewModel初期化完了: WbsItems.Count = {WbsItems?.Count ?? 0}");
                });

                // 完了メッセージを少し表示してから非表示
                await Task.Delay(2000);

                // 日付変更の監視を開始
                StartDateChangeWatching();
            }
            finally
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = false;
                    WbsProgress = 0;
                    WbsProgressMessage = string.Empty;
                });
            }
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedFrom, cts.Token);
            }
        }

        public void OnNavigatedFrom()
        { }

        // TestRedmineConnectionメソッドは削除（LoadRedmineDataAsyncとの混乱を避けるため）

        // GetDetailedErrorMessageメソッドは削除（TestRedmineConnectionでのみ使用されていたため）

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

        /// <summary>
        /// 選択されたアイテムを削除する
        /// </summary>
        private void DeleteSelectedItem()
        {
            if (SelectedItem == null)
            {
                System.Windows.MessageBox.Show("削除するアイテムが選択されていません。", "削除",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            DeleteItem(SelectedItem);
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
        /// プロジェクトデータを読み込む（非同期）
        /// </summary>
        private async Task LoadProjectDataAsync(Project project)
        {
            try
            {
                // プログレスバーを表示
                IsWbsLoading = true;
                WbsProgress = 0;
                WbsProgressMessage = $"プロジェクト {project.Name} のデータ読み込みを開始します...";

                // 既存のアイテムの親子関係をクリア
                foreach (var existingItem in WbsItems)
                {
                    existingItem.Children.Clear();
                }

                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    // データ取得開始
                    WbsProgress = 20;
                    WbsProgressMessage = "Redmineからデータを取得中...";

                    var issues = await redmineService.GetIssuesWithHierarchyAsync(project.Id).ConfigureAwait(false);

                    // チケットが0個の場合でも適切に処理
                    if (issues == null || issues.Count == 0)
                    {
                        // UIスレッドで空の状態を設定
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
                        return;
                    }

                    // チケットデータ変換開始
                    WbsProgress = 40;
                    WbsProgressMessage = "チケットデータを変換中...";

                    // バックグラウンドでWBSアイテムの変換を実行
                    var wbsItems = await Task.Run(() =>
                    {
                        // すべてのルートチケットを一度に処理して依存関係を正しく設定
                        return ConvertMultipleRedmineIssuesToWbsItems(issues);
                    }).ConfigureAwait(false);

                    // UIコレクション更新開始
                    WbsProgress = 60;
                    WbsProgressMessage = "UIコレクションを更新中...";

                    try
                    {
                        // WBSアイテムを完全にクリア（親子関係も含めて）
                        WbsItems.Clear();
                        foreach (var wbsItem in wbsItems)
                        {
                            WbsItems.Add(wbsItem);
                        }

                        // 平坦化リスト更新開始
                        WbsProgress = 80;
                        WbsProgressMessage = "平坦化リストを更新中...";

                        // 平坦化リストを更新（チケットがある場合のみ）
                        if (wbsItems.Count > 0)
                        {
                            // プログレスバーの状態を変更せずに平坦化リストを更新
                            await UpdateFlattenedListWithoutProgressBar();
                        }
                        else
                        {
                            FlattenedWbsItems.Clear();
                            // チケットがない場合は進捗を100%に設定
                            WbsProgress = 100;
                            WbsProgressMessage = "データ読み込み完了";
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
                }
            }
            catch (Exception ex)
            {
                // エラーもUIスレッドで表示
                ErrorMessage = $"チケットの読み込みに失敗しました: {ex.Message}";
            }
            finally
            {
                // プログレスバーを完了
                WbsProgress = 100;
                WbsProgressMessage = "データ読み込み完了";

                // 完了メッセージを少し表示してから非表示
                await Task.Delay(1000);

                IsWbsLoading = false;
                WbsProgress = 0;

                lock (_dataLoadingLock)
                {
                    _isLoadingRedmineData = false;
                }
                System.Diagnostics.Debug.WriteLine($"プロジェクト {project.Id} のデータ読み込みが完了しました。");
            }
        }

        /// <summary>
        /// プログレスバーなしで平坦化リストを更新する
        /// </summary>
        private async Task UpdateFlattenedListWithoutProgressBar()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FlattenedWbsItems.Clear();

                    // WbsItemsが空の場合はダミーアイテムを追加して初期化完了とみなす
                    if (WbsItems.Count == 0)
                    {
                        var dummyItem = new WbsItem { Title = "データなし" };
                        FlattenedWbsItems.Add(dummyItem);
                        System.Diagnostics.Debug.WriteLine("WbsItemsが空のため、ダミーアイテムを追加しました");
                        return;
                    }

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
                            var progress = 80 + (double)processedItems / totalItems * 15; // 80%から95%まで
                            WbsProgress = Math.Min(progress, 95); // 95%を超えないように制限
                            WbsProgressMessage = $"WBSリストを更新中... {processedItems}/{totalItems}";
                        }
                    }
                });

                // スケジュール表も更新（非同期版を使用）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 95;
                    WbsProgressMessage = "スケジュール表を更新中...";
                });

                await UpdateScheduleItemsAsync();

                // 最終的な進捗を設定（メイン処理で制御される）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WbsProgress = 100;
                    WbsProgressMessage = "データ読み込み完了";
                });
            }
            catch (Exception ex)
            {
                // エラーが発生してもプログレスバーの状態は変更しない
                System.Diagnostics.Debug.WriteLine($"平坦化リスト更新中にエラーが発生しました: {ex.Message}");
            }
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

                    // WbsItemsが空の場合は何も表示しない
                    if (WbsItems.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("WbsItemsが空のため、何も表示しません");
                        return;
                    }

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
                            WbsProgress = Math.Min(progress, 50); // 50%を超えないように制限
                            WbsProgressMessage = $"WBSリストを更新中... {WbsItems.Count} アイテムを処理中...";
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
        /// タスクの順番を変更する（ドラッグ&ドロップ用）
        /// </summary>
        /// <param name="sourceItem">移動元のタスク</param>
        /// <param name="targetItem">移動先のタスク</param>
        /// <remarks>
        /// 同じ階層レベルで同じ親を持つタスク間でのみ順番変更が可能です。
        /// 異なる階層や親が異なるタスクは移動できません。
        /// </remarks>
        public void ReorderTask(WbsItem sourceItem, WbsItem targetItem)
        {
            if (sourceItem == null || targetItem == null || sourceItem == targetItem) return;

            try
            {
                // 平坦化リストでのインデックスを取得
                var sourceIndex = FlattenedWbsItems.IndexOf(sourceItem);
                var targetIndex = FlattenedWbsItems.IndexOf(targetItem);

                if (sourceIndex == -1 || targetIndex == -1) return;

                // 階層レベルが同じ場合のみ順番変更を許可
                if (sourceItem.Level == targetItem.Level)
                {
                    // 同じ親を持つタスク間での順番変更
                    if (sourceItem.Parent == targetItem.Parent)
                    {
                        // 親の子コレクションで順番を変更
                        if (sourceItem.Parent != null)
                        {
                            var parentChildren = sourceItem.Parent.Children;
                            var sourceChildIndex = parentChildren.IndexOf(sourceItem);
                            var targetChildIndex = parentChildren.IndexOf(targetItem);

                            if (sourceChildIndex != -1 && targetChildIndex != -1)
                            {
                                parentChildren.Move(sourceChildIndex, targetChildIndex);
                            }
                        }
                        else
                        {
                            // ルートレベルのタスクの場合
                            var sourceRootIndex = WbsItems.IndexOf(sourceItem);
                            var targetRootIndex = WbsItems.IndexOf(targetItem);

                            if (sourceRootIndex != -1 && targetRootIndex != -1)
                            {
                                WbsItems.Move(sourceRootIndex, targetRootIndex);
                            }
                        }

                        // 平坦化リストを更新
                        _ = Task.Run(async () => await UpdateFlattenedList());

                        // 成功メッセージをデバッグ出力
                    }
                }
            }
            catch (Exception)
            {
                // タスク順番変更でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 先行・後続の関係性を設定する（ドラッグ&ドロップ用）
        /// </summary>
        /// <param name="sourceItem">ドラッグ元のタスク</param>
        /// <param name="targetItem">ドロップ先のタスク</param>
        /// <param name="isPredecessor">先行関係として設定する場合はtrue、双方向関係の場合はfalse</param>
        /// <remarks>
        /// ドラッグ&ドロップで先行・後続の関係性を設定します。
        /// 循環参照が発生する場合は設定を拒否します。
        /// タスクAをタスクBにドロップしたとき、タスクBがタスクAの先行タスクになります。
        /// </remarks>
        public async Task SetDependencyAsync(WbsItem sourceItem, WbsItem targetItem, bool isPredecessor)
        {
            if (sourceItem == null || targetItem == null || sourceItem == targetItem) return;

            try
            {
                if (isPredecessor)
                {
                    // 先行関係を設定
                    // タスクAをタスクBにドロップしたとき、タスクBがタスクAの先行タスクになる
                    sourceItem.AddPredecessor(targetItem);
                }
                else
                {
                    // 双方向の関係を設定
                    // sourceItemの先行がtargetItemになる
                    sourceItem.AddPredecessor(targetItem);
                    // targetItemの後続がsourceItemになる
                    targetItem.AddSuccessor(sourceItem);
                }

                // UIを更新
                OnPropertyChanged(nameof(FlattenedWbsItems));

                // 個々のアイテムのプロパティ変更通知を強制的に発火
                sourceItem.NotifyDependencyChanged();
                targetItem.NotifyDependencyChanged();

                // Redmineに依存関係を登録
                if (IsRedmineConnected && SelectedProject != null && _redmineService != null)
                {
                    try
                    {
                        if (int.TryParse(sourceItem.Id, out int sourceId) && int.TryParse(targetItem.Id, out int targetId))
                        {
                            // 先行関係の場合：sourceItemがtargetItemの先行タスク
                            // 双方向関係の場合：targetItemがsourceItemの先行タスク
                            var predecessorId = isPredecessor ? sourceId : targetId;
                            var successorId = isPredecessor ? targetId : sourceId;

                            // Redmineに依存関係を登録（先行タスクが完了するまで後続タスクは開始できない）
                            await _redmineService.CreateIssueRelationAsync(predecessorId, successorId, IssueRelationType.Precedes, CancellationToken.None);
                        }
                    }
                    catch (Exception)
                    {
                        // Redmineへの登録に失敗した場合は未保存フラグを設定
                        sourceItem.HasUnsavedChanges = true;
                        targetItem.HasUnsavedChanges = true;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // 循環参照などのエラーが発生した場合は無視
            }
            catch (Exception)
            {
                // その他のエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 先行・後続の関係性を設定する（ドラッグ&ドロップ用、非同期版のラッパー）
        /// </summary>
        /// <param name="sourceItem">ドラッグ元のタスク</param>
        /// <param name="targetItem">ドロップ先のタスク</param>
        /// <param name="isPredecessor">先行関係として設定する場合はtrue、双方向関係の場合はfalse</param>
        public void SetDependency(WbsItem sourceItem, WbsItem targetItem, bool isPredecessor)
        {
            // 非同期処理を開始（結果は待たない）
            _ = SetDependencyAsync(sourceItem, targetItem, isPredecessor);
        }

        /// <summary>
        /// 先行・後続の関係性を削除する
        /// </summary>
        /// <param name="sourceItem">関係性を削除するタスク</param>
        /// <param name="targetItem">関係性を削除するタスク</param>
        public void RemoveDependency(WbsItem sourceItem, WbsItem targetItem)
        {
            if (sourceItem == null || targetItem == null) return;

            try
            {
                // 両方向の関係性を削除
                if (sourceItem.Predecessors.Contains(targetItem))
                {
                    sourceItem.RemovePredecessor(targetItem);
                }
                else if (sourceItem.Successors.Contains(targetItem))
                {
                    sourceItem.RemoveSuccessor(targetItem);
                }

                // UIを更新
                OnPropertyChanged(nameof(FlattenedWbsItems));
            }
            catch (Exception)
            {
                // 依存関係削除でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 選択されたアイテムの依存関係を削除する（コマンド用）
        /// </summary>
        /// <param name="selectedItem">依存関係を削除するタスク</param>
        public void RemoveDependency(WbsItem? selectedItem)
        {
            if (selectedItem == null) return;

            try
            {
                // 先行タスクをすべて削除
                var predecessorsToRemove = selectedItem.Predecessors.ToList();
                foreach (var predecessor in predecessorsToRemove)
                {
                    selectedItem.RemovePredecessor(predecessor);
                }

                // 後続タスクをすべて削除
                var successorsToRemove = selectedItem.Successors.ToList();
                foreach (var successor in successorsToRemove)
                {
                    selectedItem.RemoveSuccessor(successor);
                }

                // UIを更新
                OnPropertyChanged(nameof(FlattenedWbsItems));
            }
            catch (Exception)
            {
                // 依存関係削除でエラーが発生した場合は無視
            }
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
                            Application.Current.Dispatcher.Invoke(() =>
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

                Application.Current.Dispatcher.Invoke(() =>
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
        /// Redmineデータを読み込む（非同期版）
        /// </summary>
        public async Task LoadRedmineDataAsync()
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
                // プログレスバーを表示
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "Redmineデータを読み込み中...";
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
                    IsWbsLoading = false;
                    WbsProgress = 100;
                    WbsProgressMessage = "Redmineデータの読み込みが完了しました";
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
            // 重複実行を防ぐためのロック
            lock (_dataLoadingLock)
            {
                if (_isLoadingRedmineData)
                {
                    System.Diagnostics.Debug.WriteLine($"プロジェクト {projectId} のデータ読み込みは既に実行中です。重複実行をスキップします。");
                    return;
                }
                _isLoadingRedmineData = true;
            }

            try
            {
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    // 非同期版を使用して同期的に実行
                    var issues = redmineService.GetIssuesWithHierarchyAsync(projectId).GetAwaiter().GetResult();

                    // WBSアイテムを完全にクリア（親子関係も含めて）
                    WbsItems.Clear();

                    // すべてのルートチケットを一度に処理して依存関係を正しく設定
                    var wbsItems = ConvertMultipleRedmineIssuesToWbsItems(issues);
                    foreach (var wbsItem in wbsItems)
                    {
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
            finally
            {
                lock (_dataLoadingLock)
                {
                    _isLoadingRedmineData = false;
                }
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
            // 重複実行を防ぐためのロック
            lock (_dataLoadingLock)
            {
                if (_isLoadingRedmineData)
                {
                    System.Diagnostics.Debug.WriteLine($"プロジェクト {projectId} のデータ読み込みは既に実行中です。重複実行をスキップします。");
                    return;
                }
                _isLoadingRedmineData = true;
            }

            try
            {
                // プログレスバーを開始
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsWbsLoading = true;
                    WbsProgress = 0;
                    WbsProgressMessage = "Redmineからデータを取得中...";
                });

                // プログレスバーの表示を確実にするために少し待機
                await Task.Delay(50);

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
                        // すべてのルートチケットを一度に処理して依存関係を正しく設定
                        return ConvertMultipleRedmineIssuesToWbsItems(issues);
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
                    await Task.Delay(500);

                    IsWbsLoading = false;
                    WbsProgress = 0;
                });

                // 重複実行防止フラグをリセット
                lock (_dataLoadingLock)
                {
                    _isLoadingRedmineData = false;
                }
            }
        }

        /// <summary>
        /// 複数のRedmineチケットをWBSアイテムに変換（依存関係を正しく設定）
        /// </summary>
        private List<WbsItem> ConvertMultipleRedmineIssuesToWbsItems(List<HierarchicalIssue> issues)
        {
            var convertedItems = new Dictionary<int, WbsItem>();
            var rootWbsItems = new List<WbsItem>();

            // データの整合性を事前チェック
            foreach (var issue in issues)
            {
                ValidateIssueHierarchy(issue);
            }

            // まず、すべてのWBSアイテムを変換
            var rootIssues = issues.Where(issue => issue.Parent == null).ToList();

            foreach (var rootIssue in rootIssues)
            {
                var wbsItem = ConvertRedmineIssueToWbsItemInternal(rootIssue, convertedItems);
                if (wbsItem != null)
                {
                    rootWbsItems.Add(wbsItem);
                }
            }

            // すべての変換が完了した後、依存関係を設定
            foreach (var kvp in convertedItems)
            {
                var wbsItem = kvp.Value;
                var originalIssue = FindOriginalIssueInList(issues, kvp.Key);
                if (originalIssue != null)
                {
                    SetDependencies(wbsItem, originalIssue, convertedItems);
                }
            }

            return rootWbsItems;
        }

        /// <summary>
        /// RedmineチケットをWBSアイテムに変換（単一チケット用）
        /// </summary>
        private WbsItem ConvertRedmineIssueToWbsItem(HierarchicalIssue issue)
        {
            var convertedItems = new Dictionary<int, WbsItem>();

            // データの整合性を事前チェック
            ValidateIssueHierarchy(issue);

            // まず、すべてのWBSアイテムを変換
            var result = ConvertRedmineIssueToWbsItemInternal(issue, convertedItems);

            // すべての変換が完了した後、依存関係を設定
            foreach (var kvp in convertedItems)
            {
                var wbsItem = kvp.Value;
                var originalIssue = FindOriginalIssue(issue, kvp.Key);
                if (originalIssue != null)
                {
                    SetDependencies(wbsItem, originalIssue, convertedItems);
                }
            }

            return result;
        }

        /// <summary>
        /// 指定されたIDのHierarchicalIssueを見つける（単一ルート用）
        /// </summary>
        private HierarchicalIssue? FindOriginalIssue(HierarchicalIssue rootIssue, int targetId)
        {
            if (rootIssue.Id == targetId)
                return rootIssue;

            if (rootIssue.Children != null)
            {
                foreach (var child in rootIssue.Children)
                {
                    var found = FindOriginalIssue(child, targetId);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        /// <summary>
        /// 指定されたIDのHierarchicalIssueを見つける（複数ルート用）
        /// </summary>
        private HierarchicalIssue? FindOriginalIssueInList(List<HierarchicalIssue> issues, int targetId)
        {
            foreach (var issue in issues)
            {
                var found = FindOriginalIssue(issue, targetId);
                if (found != null)
                    return found;
            }
            return null;
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

                // 依存関係は後で一括設定するため、ここでは設定しない

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

            // 依存関係は後で一括設定するため、ここでは設定しない

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

                    // WbsItemsが空の場合は何も表示しない
                    if (WbsItems.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("WbsItemsが空のため、何も表示しません");
                        return;
                    }

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
                            WbsProgress = Math.Min(progress, 50); // 50%を超えないように制限
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

            // 重複実行を防ぐためのロック
            lock (_dataLoadingLock)
            {
                if (_isLoadingRedmineData)
                {
                    System.Diagnostics.Debug.WriteLine($"プロジェクト {SelectedProject.Id} のデータ読み込みは既に実行中です。重複実行をスキップします。");
                    return;
                }
                _isLoadingRedmineData = true;
            }

            try
            {
                var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);
                var issues = redmineService.GetIssuesWithHierarchyAsync(SelectedProject.Id).GetAwaiter().GetResult();

                // 既存のアイテムを完全にクリア
                WbsItems.Clear();

                // すべてのルートチケットを一度に処理して依存関係を正しく設定
                var wbsItems = ConvertMultipleRedmineIssuesToWbsItems(issues);
                foreach (var wbsItem in wbsItems)
                {
                    WbsItems.Add(wbsItem);
                }

                // 平坦化リストを更新
                _ = Task.Run(async () => await UpdateFlattenedList());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"チケットの読み込み中にエラーが発生しました: {ex.Message}";
            }
            finally
            {
                lock (_dataLoadingLock)
                {
                    _isLoadingRedmineData = false;
                }
            }
        }

        /// <summary>
        /// Redmineのチケットを更新する
        /// </summary>
        /// <param name="task">更新するタスク</param>
        /// <param name="oldStartDate">変更前の開始日</param>
        /// <param name="oldEndDate">変更前の終了日</param>
        private async Task UpdateRedmineIssueAsync(WbsItem task, DateTime oldStartDate, DateTime oldEndDate)
        {
            try
            {
                if (task.RedmineIssueId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"Redmine更新開始: タスク '{task.Title}' (ID: {task.RedmineIssueId.Value})");

                    RedmineService? redmineService = null;
                    try
                    {
                        redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey);

                        var issue = await redmineService.GetIssueAsync(task.RedmineIssueId.Value);
                        if (issue != null)
                        {
                            // 変更された日付のみを更新
                            bool hasChanges = false;

                            if (task.StartDate != oldStartDate)
                            {
                                System.Diagnostics.Debug.WriteLine($"開始日変更: {oldStartDate:yyyy/MM/dd} -> {task.StartDate:yyyy/MM/dd}");
                                issue.StartDate = task.StartDate;
                                hasChanges = true;
                            }

                            if (task.EndDate != oldEndDate)
                            {
                                System.Diagnostics.Debug.WriteLine($"終了日変更: {oldEndDate:yyyy/MM/dd} -> {task.EndDate:yyyy/MM/dd}");
                                issue.DueDate = task.EndDate;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                System.Diagnostics.Debug.WriteLine($"Redmineに更新を送信: タスク '{task.Title}'");
                                await redmineService.UpdateIssueAsync(issue);
                                System.Diagnostics.Debug.WriteLine($"Redmine更新完了: タスク '{task.Title}'");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"変更なし: タスク '{task.Title}'");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"チケットが見つかりません: ID {task.RedmineIssueId.Value}");
                        }
                    }
                    finally
                    {
                        // RedmineServiceを確実に破棄
                        redmineService?.Dispose();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"RedmineIssueIdが設定されていません: タスク '{task.Title}'");
                }
            }
            catch (RedmineClient.Services.RedmineApiException redmineEx)
            {
                System.Diagnostics.Debug.WriteLine($"Redmine更新でRedmineApiException: タスク '{task.Title}': {redmineEx.Message}");
                throw; // 上位に伝播させる
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Redmine更新で予期しないエラー: タスク '{task.Title}': {ex.Message}");
                throw; // 上位に伝播させる
            }
        }

        /// <summary>
        /// スケジュール表を再生成する
        /// </summary>
        private async Task RefreshScheduleAsync()
        {
            try
            {
                await InitializeScheduleItems();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"スケジュール表再生成エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// エラーメッセージをユーザーに表示する
        /// </summary>
        /// <param name="message">表示するエラーメッセージ</param>
        private void ShowErrorMessage(string message)
        {
            try
            {
                // UIスレッドでエラーメッセージを表示
                if (Application.Current?.MainWindow?.Dispatcher != null)
                {
                    Application.Current.MainWindow.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            // シンプルなメッセージボックスで表示
                            System.Windows.MessageBox.Show(
                                message,
                                "エラー",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        }
                        catch (Exception ex)
                        {
                            // メッセージボックス表示でエラーが発生した場合はデバッグ出力のみ
                            System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でエラー: {ex.Message}");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
                else
                {
                    // ディスパッチャーが取得できない場合はデバッグ出力のみ
                    System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でディスパッチャーが取得できません: {message}");
                }
            }
            catch (Exception ex)
            {
                // エラーメッセージ表示でエラーが発生した場合はデバッグ出力のみ
                System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// アプリケーション終了時の保存処理
        /// </summary>
        public async Task SavePendingChangesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("アプリケーション終了時の保存処理を開始...");

                // 日付変更の監視を停止
                StopDateChangeWatching();

                // 未保存の変更がある場合は保存処理を実行
                if (IsRedmineConnected && SelectedProject != null)
                {
                    var tasksWithChanges = FlattenedWbsItems
                        .Where(item => item.HasUnsavedChanges)
                        .ToList();

                    if (tasksWithChanges.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"{tasksWithChanges.Count}件の未保存変更を保存中...");

                        foreach (var task in tasksWithChanges)
                        {
                            try
                            {
                                // 現在の日付を使用して更新（変更前の日付は不明なため）
                                await UpdateRedmineIssueAsync(task, task.StartDate, task.EndDate);
                                task.HasUnsavedChanges = false;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"タスク '{task.Title}' の保存に失敗: {ex.Message}");
                            }
                        }

                        System.Diagnostics.Debug.WriteLine("未保存変更の保存が完了しました");
                    }
                }

                System.Diagnostics.Debug.WriteLine("アプリケーション終了時の保存処理が完了しました");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"アプリケーション終了時の保存処理でエラーが発生: {ex.Message}");
            }
        }

        /// <summary>
        /// 選択されたアイテムの先行タスクを削除する
        /// </summary>
        /// <param name="selectedItem">先行タスクを削除するタスク</param>
        public void RemovePredecessor(WbsItem? selectedItem)
        {
            if (selectedItem == null) return;

            try
            {
                // 先行タスクをすべて削除
                var predecessorsToRemove = selectedItem.Predecessors.ToList();
                foreach (var predecessor in predecessorsToRemove)
                {
                    selectedItem.RemovePredecessor(predecessor);
                }

                // UIを更新
                OnPropertyChanged(nameof(FlattenedWbsItems));
            }
            catch
            {
                // 先行タスク削除でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 選択されたアイテムの後続タスクを削除する
        /// </summary>
        /// <param name="selectedItem">後続タスクを削除するタスク</param>
        public void RemoveSuccessor(WbsItem? selectedItem)
        {
            if (selectedItem == null) return;

            try
            {
                // 後続タスクをすべて削除
                var successorsToRemove = selectedItem.Successors.ToList();
                foreach (var successor in successorsToRemove)
                {
                    selectedItem.RemoveSuccessor(successor);
                }

                // UIを更新
                OnPropertyChanged(nameof(FlattenedWbsItems));
            }
            catch
            {
                // 後続タスク削除でエラーが発生した場合は無視
            }
        }

        /// <summary>
        /// 依存関係を設定
        /// </summary>
        /// <param name="wbsItem">設定対象のWbsItem</param>
        /// <param name="issue">参照元のHierarchicalIssue</param>
        /// <param name="convertedItems">変換済みアイテムの辞書</param>
        private void SetDependencies(WbsItem wbsItem, HierarchicalIssue issue, Dictionary<int, WbsItem> convertedItems)
        {
            try
            {
                // 先行タスクを設定
                if (issue.Predecessors != null)
                {
                    foreach (var predecessor in issue.Predecessors)
                    {
                        if (convertedItems.ContainsKey(predecessor.Id))
                        {
                            var predecessorWbsItem = convertedItems[predecessor.Id];
                            wbsItem.AddPredecessor(predecessorWbsItem);
                        }
                    }
                }

                // 後続タスクを設定
                if (issue.Successors != null)
                {
                    foreach (var successor in issue.Successors)
                    {
                        if (convertedItems.ContainsKey(successor.Id))
                        {
                            var successorWbsItem = convertedItems[successor.Id];
                            wbsItem.AddSuccessor(successorWbsItem);
                        }
                    }
                }
            }
            catch
            {
                // 依存関係の設定でエラーが発生した場合は無視して続行
            }
        }
    }
}