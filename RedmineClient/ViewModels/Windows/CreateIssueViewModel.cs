using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Redmine.Net.Api.Types;
using RedmineClient.Services;
using RedmineClient.Models;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;

namespace RedmineClient.ViewModels.Windows
{
    public partial class CreateIssueViewModel : ObservableObject
    {
        private readonly RedmineService _redmineService;
        private readonly Project _selectedProject;

        public ICommand CreateIssueCommand { get; }
        public ICommand CancelCommand { get; }

        [ObservableProperty]
        private string _subject = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private DateTime? _startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime? _dueDate;

        [ObservableProperty]
        private float? _estimatedHours;

        [ObservableProperty]
        private Tracker? _selectedTracker;

        [ObservableProperty]
        private IssueStatus? _selectedStatus;

        [ObservableProperty]
        private IssuePriority? _selectedPriority;

        [ObservableProperty]
        private bool _addAsWatcher = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public ObservableCollection<Tracker> AvailableTrackers { get; } = new();
        public ObservableCollection<IssueStatus> AvailableStatuses { get; } = new();
        public ObservableCollection<IssuePriority> AvailablePriorities { get; } = new();

        public CreateIssueViewModel(RedmineService redmineService, Project selectedProject)
        {
            _redmineService = redmineService;
            _selectedProject = selectedProject;
            
            CreateIssueCommand = new AsyncRelayCommand(CreateIssueAsync);
            CancelCommand = new RelayCommand(Cancel);
            
            _ = LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                // UIスレッドでローディング状態を設定
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                });

                // トラッカー、ステータス、優先度を読み込み
                var trackers = await _redmineService.GetTrackersAsync();
                var statuses = await _redmineService.GetIssueStatusesAsync();
                var priorities = await _redmineService.GetIssuePrioritiesAsync();

                // UIスレッドでコレクションを更新
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableTrackers.Clear();
                    foreach (var tracker in trackers)
                    {
                        AvailableTrackers.Add(tracker);
                    }

                    AvailableStatuses.Clear();
                    foreach (var status in statuses)
                    {
                        AvailableStatuses.Add(status);
                    }

                    AvailablePriorities.Clear();
                    foreach (var priority in priorities)
                    {
                        AvailablePriorities.Add(priority);
                    }
                });

                // デフォルト値を設定（UIスレッドで処理）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SelectedTracker = AvailableTrackers.FirstOrDefault();
                    SelectedStatus = AvailableStatuses.FirstOrDefault();
                    SelectedPriority = AvailablePriorities.FirstOrDefault();
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"初期データの読み込みに失敗しました: {ex.Message}";
            }
            finally
            {
                // UIスレッドでローディング状態をリセット
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private async Task CreateIssueAsync()
        {
            try
            {
                // バリデーション
                if (string.IsNullOrWhiteSpace(Subject))
                {
                    ErrorMessage = "件名を入力してください。";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Description))
                {
                    ErrorMessage = "説明を入力してください。";
                    return;
                }

                // UIスレッドでローディング状態を設定
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    ErrorMessage = string.Empty;
                });

                // 新しいチケットを作成
                var newIssue = new Issue
                {
                    Subject = Subject,
                    Description = Description,
                    StartDate = StartDate,
                    DueDate = DueDate,
                    EstimatedHours = EstimatedHours,
                    Project = _selectedProject,
                    Tracker = SelectedTracker,
                    Status = SelectedStatus,
                    Priority = SelectedPriority
                };

                // チケットを作成
                var createdIssueId = await _redmineService.CreateIssueAsync(newIssue);

                // ウォッチャーとして登録するオプションが選択されている場合
                if (AddAsWatcher)
                {
                    try
                    {
                        await _redmineService.AddWatcherAsync(createdIssueId, AppConfig.CurrentUserId);
                    }
                    catch (Exception ex)
                    {
                        // ウォッチャー登録に失敗してもチケット作成は成功とする
                        System.Diagnostics.Debug.WriteLine($"ウォッチャー登録に失敗: {ex.Message}");
                    }
                }

                // 成功メッセージを表示（非同期で処理）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"チケット #{createdIssueId} を作成しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                // ウィンドウを閉じる（UIスレッドで処理）
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    if (window != null)
                    {
                        window.DialogResult = true;
                        window.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"チケットの作成に失敗しました: {ex.Message}";
            }
            finally
            {
                // UIスレッドでローディング状態をリセット
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }

        private void Cancel()
        {
            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = false;
                window.Close();
            }
        }
    }
}
