using System.Collections.ObjectModel;
using System.Windows.Input;
using Redmine.Net.Api.Types;
using RedmineClient.Models;
using RedmineClient.Services;
using RedmineClient.ViewModels.Windows;
using RedmineClient.Views.Windows;
using Wpf.Ui.Abstractions.Controls;

namespace RedmineClient.ViewModels.Pages
{
    public partial class WbsViewModel : ObservableObject, INavigationAware
    {
        /// <summary>
        /// フォーカス設定要求イベント（編集モードかどうかを渡す）
        /// </summary>
        public event Action<bool>? RequestFocus;

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

        [ObservableProperty]
        private WbsItem? _selectedItem;

        /// <summary>
        /// 選択されたアイテムに子タスクを追加可能かどうか
        /// </summary>
        [ObservableProperty]
        private bool _canAddChild = false;

        /// <summary>
        /// タスク追加後の動作モード
        /// true: 編集モード（新しく追加されたタスクを選択）
        /// false: 連続追加モード（親タスクを選択したまま）
        /// </summary>
        [ObservableProperty]
        private bool _isEditModeAfterAdd = true;

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
        /// プロジェクト選択の説明テキスト
        /// </summary>
        [ObservableProperty]
        private string _projectSelectionDescription = string.Empty;

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

        partial void OnIsEditModeAfterAddChanged(bool value)
        {
            // 編集モード変更時の処理
        }

        partial void OnScheduleStartYearMonthChanged(string value)
        {
            // 設定を保存
            AppConfig.ScheduleStartYearMonth = value;
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
                    // 少し遅延を入れてから読み込みを実行（UIの更新を待つ）
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LoadRedmineData();
                        });
                    });
                }
                else
                {
                    // 接続されていない場合は接続テストを実行
                    TestRedmineConnection();
                }
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
            TestConnectionCommand = new RelayCommand(() => TestRedmineConnection());
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
            UpdateProgressCommand = new RelayCommand(UpdateParentProgress);
            LimitHierarchyCommand = new RelayCommand(() => LimitHierarchyDepth(5));
            SelectItemCommand = new RelayCommand<WbsItem>(SelectItem);
            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);
            MoveLeftCommand = new RelayCommand(MoveLeft);
            MoveRightCommand = new RelayCommand(MoveRight);
            LoadRedmineDataCommand = new RelayCommand(() => LoadRedmineData());
            RefreshRedmineDataCommand = new RelayCommand(() => RefreshRedmineData());
            SettingsCommand = new RelayCommand(OpenSettings);
            CreateNewIssueCommand = new RelayCommand(CreateNewIssue);
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

        private void InitializeViewModel()
        {
            // 平坦化リストを初期化
            UpdateFlattenedList();
            
            // スケジュール表を初期化
            InitializeScheduleItems();
            
            // Redmine接続状態を確認してプロジェクトを取得
            TestRedmineConnection();
            
            // プロジェクト選択の初期化
            if (AvailableProjects.Count == 0)
            {
                AvailableProjects = new List<Project>();
            }
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedFrom, cts.Token);
            }
        }

        public void OnNavigatedFrom() { }

        public void TestRedmineConnection()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                ConnectionStatus = "接続確認中...";
                System.Diagnostics.Debug.WriteLine("TestRedmineConnection: 接続テストを開始");

                // 設定からRedmine接続情報を取得
                if (string.IsNullOrEmpty(AppConfig.RedmineHost))
                {
                    IsRedmineConnected = false;
                    ConnectionStatus = "設定されていません";
                    ErrorMessage = "Redmineのホストが設定されていません。設定画面でRedmine接続情報を設定してください。";
                    System.Diagnostics.Debug.WriteLine("TestRedmineConnection: ホストが設定されていません");
                    
                    // 接続情報が設定されていない場合は空のリストを設定
                    AvailableProjects = new List<Project>();
                    return;
                }

                if (string.IsNullOrEmpty(AppConfig.ApiKey))
                {
                    IsRedmineConnected = false;
                    ConnectionStatus = "設定されていません";
                    ErrorMessage = "RedmineのAPIキーが設定されていません。設定画面でAPIキーを設定してください。";
                    System.Diagnostics.Debug.WriteLine("TestRedmineConnection: APIキーが設定されていません");
                    
                    // APIキーが設定されていない場合は空のリストを設定
                    AvailableProjects = new List<Project>();
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"TestRedmineConnection: ホスト={AppConfig.RedmineHost}, APIキー設定済み");

                // RedmineServiceを使用した接続テスト
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    var isConnected = redmineService.TestConnection();
                    
                    if (isConnected)
                    {
                        IsRedmineConnected = true;
                        ConnectionStatus = "接続済み";
                        ErrorMessage = string.Empty;
                        System.Diagnostics.Debug.WriteLine("TestRedmineConnection: 接続成功");
                        
                        // プロジェクト一覧を取得
                        LoadProjects(redmineService);
                        
                        // プロジェクトが選択されている場合は、自動的にRedmineデータを読み込む
                        if (SelectedProject != null)
                        {
                            // 少し遅延を入れてから読み込みを実行（UIの更新を待つ）
                            Task.Delay(200).ContinueWith(_ =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    LoadRedmineData();
                                });
                            });
                        }
                    }
                    else
                    {
                        IsRedmineConnected = false;
                        ConnectionStatus = "認証エラー";
                        ErrorMessage = "APIキーが無効です。正しいAPIキーを設定してください。";
                        System.Diagnostics.Debug.WriteLine("TestRedmineConnection: 認証エラー");
                        
                        // 認証エラーの場合は空のリストを設定
                        AvailableProjects = new List<Project>();
                    }
                }
            }
            catch (System.Net.Sockets.SocketException socketEx)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                
                var errorMessage = "ネットワーク接続エラーが発生しました。";
                if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                {
                    errorMessage += " Redmineサーバーに接続できません。サーバーが起動しているか、ホスト名とポート番号を確認してください。";
                }
                else if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                {
                    errorMessage += " 接続がタイムアウトしました。ネットワークの状態を確認してください。";
                }
                else
                {
                    errorMessage += $" ソケットエラー: {socketEx.SocketErrorCode} - {socketEx.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"TestRedmineConnection: SocketException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (System.Net.Http.HttpRequestException httpEx)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                
                var errorMessage = "HTTPリクエストエラーが発生しました。";
                if (httpEx.InnerException is System.Net.Sockets.SocketException innerSocketEx)
                {
                    if (innerSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                    {
                        errorMessage += " Redmineサーバーに接続できません。サーバーが起動しているか、ホスト名とポート番号を確認してください。";
                    }
                    else if (innerSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    {
                        errorMessage += " 接続がタイムアウトしました。ネットワークの状態を確認してください。";
                    }
                }
                else
                {
                    errorMessage += $" {httpEx.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"TestRedmineConnection: HttpRequestException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (Redmine.Net.Api.Exceptions.RedmineException redmineEx)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                
                var errorMessage = $"Redmine API エラー: {redmineEx.Message}";
                if (redmineEx.InnerException != null)
                {
                    errorMessage += $" 詳細: {redmineEx.InnerException.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"TestRedmineConnection: RedmineException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (Exception ex)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                var errorMessage = $"接続テストでエラーが発生しました: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" 詳細: {ex.InnerException.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"TestRedmineConnection: Exception - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadSampleData()
        {
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

            // サンプルデータを追加
            WbsItems.Add(project);
        }



        private void AddRootItem()
        {
            var newItem = new WbsItem
            {
                Id = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}",
                Title = "新しいタスク",
                Description = "タスクの説明を入力してください",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                Status = "未着手",
                Priority = "中",
                Assignee = "未割り当て"
            };

            WbsItems.Add(newItem);
            
            // 新しいタスクを選択状態にする
            SelectedItem = newItem;
            
            // UIの更新を強制する（新規タスクの表示更新のため）
            OnPropertyChanged(nameof(WbsItems));
            
            // 平坦化リストを更新
            UpdateFlattenedList();
        }

        private void AddChildItem(WbsItem? parent)
        {
            if (parent == null) return;

            var newItem = new WbsItem
            {
                Id = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}",
                Title = "新しいサブタスク",
                Description = "サブタスクの説明を入力してください",
                StartDate = parent.StartDate,
                EndDate = parent.EndDate,
                Status = "未着手",
                Priority = "中",
                Assignee = "未割り当て"
            };

            parent.AddChild(newItem);
            
            // モードに応じて選択するアイテムを決定
            if (IsEditModeAfterAdd)
            {
                // 編集モード：新しく追加されたサブタスクを選択
                SelectedItem = newItem;
            }
            else
            {
                // 連続追加モード：親タスクを選択したまま
                SelectedItem = parent;
                
                            // 連続追加モードでは、親タスクが確実に選択されていることを確認
            }
        
        // UIの更新を強制する（展開状態とサブタスクの表示更新のため）
        OnPropertyChanged(nameof(WbsItems));
        
        // 平坦化リストを更新
        UpdateFlattenedList();
        
        // 平坦化リスト更新後に選択状態を再確認・復元（遅延実行で確実に復元）
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!IsEditModeAfterAdd && SelectedItem != parent)
            {
                SelectedItem = parent;
            }
            else if (IsEditModeAfterAdd && SelectedItem != newItem)
            {
                SelectedItem = newItem;
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
        
        // フォーカスを設定（編集モードかどうかを渡す）
        RequestFocus?.Invoke(IsEditModeAfterAdd);
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



            // デフォルトで3つのサブタスクを追加
            int count = 3;
            for (int i = 0; i < count; i++)
            {
                var newItem = new WbsItem
                {
                    Id = $"TASK-{DateTime.Now:yyyyMMdd-HHmmss}-{i + 1}",
                    Title = $"サブタスク {i + 1}",
                    Description = $"サブタスク {i + 1} の説明",
                    StartDate = parent.StartDate,
                    EndDate = parent.EndDate,
                    Status = "未着手",
                    Priority = "中",
                    Assignee = "未割り当て"
                };

                parent.AddChild(newItem);
            }
            
            // 一括追加の場合は常に親タスクを選択（連続追加のため）
            SelectedItem = parent;
            
            // UIの更新を強制する（展開状態とサブタスクの表示更新のため）
            OnPropertyChanged(nameof(WbsItems));
            
            // 平坦化リストを更新
            UpdateFlattenedList();
            
                    // 平坦化リスト更新後に選択状態を再確認・復元（遅延実行で確実に復元）
        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (SelectedItem != parent)
            {
                SelectedItem = parent;
            }
        }), System.Windows.Threading.DispatcherPriority.Loaded);
            
            // フォーカスを設定（編集モードかどうかを渡す）
            RequestFocus?.Invoke(IsEditModeAfterAdd);
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
                    LoadRedmineIssues(SelectedProject.Id);
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

        private void ToggleExpansion(WbsItem? item)
        {
            if (item == null) return;
            
            // 展開状態を切り替え
            item.IsExpanded = !item.IsExpanded;
            
            // 平坦化リストを更新
            UpdateFlattenedList();
        }

        /// <summary>
        /// 階層構造を平坦化したリストを更新
        /// </summary>
        private void UpdateFlattenedList()
        {
            FlattenedWbsItems.Clear();
            
            foreach (var rootItem in WbsItems)
            {
                AddItemToFlattened(rootItem);
            }
            
            // スケジュール表も更新
            UpdateScheduleItems();
        }

        /// <summary>
        /// アイテムと（展開されている場合）その子アイテムを平坦化リストに追加
        /// </summary>
        private void AddItemToFlattened(WbsItem item)
        {
            FlattenedWbsItems.Add(item);
            
            if (item.IsExpanded && item.HasChildren)
            {
                foreach (var child in item.Children)
                {
                    AddItemToFlattened(child);
                }
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
        private void InitializeScheduleItems()
        {
            ScheduleItems.Clear();
            
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddMonths(2); // 2か月先まで
            
            // 週単位でグループ化して表示
            var currentDate = startDate;
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
                }
                
                // 次の週に移動
                currentDate = weekEnd.AddDays(1);
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
        private void UpdateScheduleItems()
        {
            foreach (var scheduleItem in ScheduleItems)
            {
                scheduleItem.TaskTitle = GetTaskTitleForDate(scheduleItem.Date);
            }
        }

        /// <summary>
        /// プロジェクト一覧を読み込む
        /// </summary>
        private void LoadProjects(RedmineService redmineService)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LoadProjects: プロジェクト一覧の取得を開始");
                var projects = redmineService.GetProjects();
                System.Diagnostics.Debug.WriteLine($"LoadProjects: {projects.Count}件のプロジェクトを取得しました");
                
                AvailableProjects = projects;
                
                // 設定から選択されたプロジェクトIDを復元
                if (AppConfig.SelectedProjectId.HasValue && projects.Count > 0)
                {
                    var restoredProject = projects.FirstOrDefault(p => p.Id == AppConfig.SelectedProjectId.Value);
                    if (restoredProject != null)
                    {
                        SelectedProject = restoredProject;
                        System.Diagnostics.Debug.WriteLine($"LoadProjects: 設定からプロジェクトを復元: {restoredProject.Name}");
                        
                        // プロジェクトが復元された場合、自動的にRedmineデータを読み込む
                        if (IsRedmineConnected)
                        {
                            // 少し遅延を入れてから読み込みを実行（UIの更新を待つ）
                            Task.Delay(300).ContinueWith(_ =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    LoadRedmineData();
                                });
                            });
                        }
                        return; // 復元成功
                    }
                }
                
                // プロジェクトが1つの場合は自動選択
                if (projects.Count == 1)
                {
                    var singleProject = projects[0];
                    SelectedProject = singleProject;
                    IsProjectSelectionReadOnly = true;
                    ProjectSelectionDescription = $"プロジェクトが1つのため自動選択: {singleProject.Name}";
                    System.Diagnostics.Debug.WriteLine($"LoadProjects: 単一プロジェクトを自動選択: {singleProject.Name}");
                    
                    // 自動選択されたプロジェクトのデータを読み込む
                    if (IsRedmineConnected)
                    {
                        Task.Delay(300).ContinueWith(_ =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LoadRedmineData();
                            });
                        });
                    }
                }
                // プロジェクトが複数ある場合は最初のプロジェクトを選択（従来の動作）
                else if (projects.Count > 1)
                {
                    SelectedProject = projects[0];
                    IsProjectSelectionReadOnly = false;
                    ProjectSelectionDescription = $"複数プロジェクトから選択: {projects[0].Name}";
                    System.Diagnostics.Debug.WriteLine($"LoadProjects: 複数プロジェクトから最初のプロジェクトを選択: {projects[0].Name}");
                }
                else
                {
                    IsProjectSelectionReadOnly = false;
                    ProjectSelectionDescription = "プロジェクトが選択されていません";
                    System.Diagnostics.Debug.WriteLine("LoadProjects: プロジェクトが見つかりませんでした");
                }
            }
            catch (System.Net.Sockets.SocketException socketEx)
            {
                var errorMessage = "ネットワーク接続エラーが発生しました。";
                if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                {
                    errorMessage += " Redmineサーバーに接続できません。サーバーが起動しているか、ホスト名とポート番号を確認してください。";
                }
                else if (socketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                {
                    errorMessage += " 接続がタイムアウトしました。ネットワークの状態を確認してください。";
                }
                else
                {
                    errorMessage += $" ソケットエラー: {socketEx.SocketErrorCode} - {socketEx.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"LoadProjects: SocketException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (System.Net.Http.HttpRequestException httpEx)
            {
                var errorMessage = "HTTPリクエストエラーが発生しました。";
                if (httpEx.InnerException is System.Net.Sockets.SocketException innerSocketEx)
                {
                    if (innerSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
                    {
                        errorMessage += " Redmineサーバーに接続できません。サーバーが起動しているか、ホスト名とポート番号を確認してください。";
                    }
                    else if (innerSocketEx.SocketErrorCode == System.Net.Sockets.SocketError.TimedOut)
                    {
                        errorMessage += " 接続がタイムアウトしました。ネットワークの状態を確認してください。";
                    }
                }
                else
                {
                    errorMessage += $" {httpEx.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"LoadProjects: HttpRequestException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (Redmine.Net.Api.Exceptions.RedmineException redmineEx)
            {
                var errorMessage = $"Redmine API エラー: {redmineEx.Message}";
                if (redmineEx.InnerException != null)
                {
                    errorMessage += $" 詳細: {redmineEx.InnerException.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"LoadProjects: RedmineException - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
            catch (Exception ex)
            {
                var errorMessage = $"プロジェクト一覧の取得に失敗しました: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" 詳細: {ex.InnerException.Message}";
                }
                
                ErrorMessage = errorMessage;
                System.Diagnostics.Debug.WriteLine($"LoadProjects: Exception - {errorMessage}");
                
                // エラーが発生した場合は空のリストを設定
                AvailableProjects = new List<Project>();
            }
        }

        /// <summary>
        /// Redmineデータを読み込む
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
        /// Redmineデータを更新する
        /// </summary>
        private void RefreshRedmineData()
        {
            LoadRedmineData();
        }

        /// <summary>
        /// 指定されたプロジェクトのチケットを読み込む
        /// </summary>
        private void LoadRedmineIssues(int projectId)
        {
            try
            {
                using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                {
                    var issues = redmineService.GetIssuesWithHierarchy(projectId);
                    
                    // デバッグ用ログ
                    System.Diagnostics.Debug.WriteLine($"LoadRedmineIssues: プロジェクトID {projectId} から {issues.Count} 件のチケットを取得しました");
                    
                    // WBSアイテムに変換
                    WbsItems.Clear();
                    foreach (var issue in issues)
                    {
                        var wbsItem = ConvertRedmineIssueToWbsItem(issue);
                        WbsItems.Add(wbsItem);
                        System.Diagnostics.Debug.WriteLine($"LoadRedmineIssues: チケット '{issue.Subject}' (ID: {issue.Id}) をWBSアイテムに変換しました");
                    }
                    
                    // 平坦化リストを更新
                    UpdateFlattenedList();
                    
                    IsRedmineDataLoaded = true;
                    ErrorMessage = string.Empty;
                    
                    System.Diagnostics.Debug.WriteLine($"LoadRedmineIssues: 完了。WBSアイテム数: {WbsItems.Count}");
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
        /// RedmineチケットをWBSアイテムに変換
        /// </summary>
        private WbsItem ConvertRedmineIssueToWbsItem(RedmineIssue issue)
        {
            var wbsItem = new WbsItem
            {
                Id = issue.Id.ToString(),
                Title = issue.Subject,
                Description = issue.Description,
                StartDate = issue.StartDate ?? DateTime.Today,
                EndDate = issue.DueDate ?? (issue.StartDate?.AddDays(1) ?? DateTime.Today.AddDays(1)),
                Progress = issue.DoneRatio,
                Status = issue.Status ?? "未着手",
                Priority = issue.Priority ?? "中",
                Assignee = issue.AssignedTo ?? "未割り当て",
                RedmineIssueId = issue.Id,
                RedmineProjectId = issue.ProjectId,
                RedmineProjectName = issue.ProjectName,
                RedmineTracker = issue.Tracker ?? string.Empty,
                RedmineAuthor = issue.Author ?? string.Empty,
                RedmineCreatedOn = issue.CreatedOn ?? DateTime.Today,
                RedmineUpdatedOn = issue.UpdatedOn ?? DateTime.Today,
                RedmineUrl = $"{AppConfig.RedmineHost}/issues/{issue.Id}"
            };

            // 子チケットを再帰的に変換
            foreach (var childIssue in issue.Children)
            {
                var childWbsItem = ConvertRedmineIssueToWbsItem(childIssue);
                wbsItem.AddChild(childWbsItem);
            }

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
    }
}
