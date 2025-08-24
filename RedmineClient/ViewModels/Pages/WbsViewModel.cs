using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Collections;

namespace RedmineClient.ViewModels.Pages
{
    public partial class WbsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private ObservableCollection<WbsItem> _wbsItems = new();

        [ObservableProperty]
        private WbsItem? _selectedItem;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _showCompleted = true;

        [ObservableProperty]
        private bool _showInProgress = true;

        [ObservableProperty]
        private bool _showNotStarted = true;

        [ObservableProperty]
        private bool _isRedmineConnected = false;

        [ObservableProperty]
        private string _connectionStatus = "未接続";

        [ObservableProperty]
        private string _errorMessage = string.Empty;

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

        public WbsViewModel()
        {
            AddRootItemCommand = new RelayCommand(AddRootItem);
            AddChildItemCommand = new RelayCommand<WbsItem>(AddChildItem);
            DeleteItemCommand = new RelayCommand<WbsItem>(DeleteItem);
            EditItemCommand = new RelayCommand<WbsItem>(EditItem);
            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
            TestConnectionCommand = new RelayCommand(async () => await TestRedmineConnectionAsync());
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
            await InitializeViewModelAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            // 初回のみサンプルデータを読み込み
            if (WbsItems.Count == 0)
            {
                LoadSampleData();
                
                // 最初のアイテムを選択
                if (WbsItems.Count > 0)
                {
                    SelectedItem = WbsItems[0];
                }
            }
            
            // Redmine接続状態を確認
            await TestRedmineConnectionAsync();
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                await DispatchAsync(OnNavigatedFrom, cts.Token);
            }
        }

        public void OnNavigatedFrom() { }

        private async Task TestRedmineConnectionAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                ConnectionStatus = "接続確認中...";

                // 設定からRedmine接続情報を取得（テーマ設定は初期化しない）
                if (string.IsNullOrEmpty(AppConfig.RedmineHost))
                {
                    IsRedmineConnected = false;
                    ConnectionStatus = "設定されていません";
                    ErrorMessage = "Redmineのホストが設定されていません。設定画面でRedmine接続情報を設定してください。";
                    return;
                }

                // 簡単な接続テスト（HTTP GETリクエスト）
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(10);
                    
                    var response = await httpClient.GetAsync($"{AppConfig.RedmineHost}/");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        IsRedmineConnected = true;
                        ConnectionStatus = "接続済み";
                        ErrorMessage = string.Empty;
                    }
                    else
                    {
                        IsRedmineConnected = false;
                        ConnectionStatus = "接続エラー";
                        ErrorMessage = $"Redmineサーバーに接続できません。HTTPステータス: {response.StatusCode}";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                ErrorMessage = $"Redmineサーバーに接続できません: {ex.Message}";
            }
            catch (TaskCanceledException)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続タイムアウト";
                ErrorMessage = "Redmineサーバーへの接続がタイムアウトしました。サーバーが起動しているか確認してください。";
            }
            catch (Exception ex)
            {
                IsRedmineConnected = false;
                ConnectionStatus = "接続エラー";
                ErrorMessage = $"予期しないエラーが発生しました: {ex.Message}";
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
            
            // 新しいタスクを選択状態にして表示を更新
            SelectedItem = newItem;
            
            // 親タスクを展開状態にする
            parent.IsExpanded = true;
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

        private async Task RefreshAsync()
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
                // 実際の実装ではRedmine APIを使用
                await Task.Delay(1000); // 仮の処理時間

                // データの更新処理（サンプルデータの場合は再読み込みしない）
                // 実際のRedmine API実装時は、ここでデータを更新
                // WbsItems.Clear();
                // LoadSampleData();
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
            
            item.IsExpanded = !item.IsExpanded;
            
            // 子アイテムの表示/非表示を制御
            if (item.IsExpanded)
            {
                // 子アイテムを表示
                foreach (var child in item.Children)
                {
                    if (!WbsItems.Contains(child))
                    {
                        // 親アイテムの直後に挿入
                        var parentIndex = WbsItems.IndexOf(item);
                        WbsItems.Insert(parentIndex + 1, child);
                    }
                }
            }
            else
            {
                // 子アイテムを非表示（再帰的に）
                HideChildrenRecursively(item);
            }
        }

        private void HideChildrenRecursively(WbsItem parent)
        {
            foreach (var child in parent.Children)
            {
                WbsItems.Remove(child);
                HideChildrenRecursively(child);
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
    }
}
