using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Redmine.Net.Api.Types;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace RedmineClient.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty]
        private string _redmineHost = String.Empty;

        [ObservableProperty]
        private string _apiKey = String.Empty;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        [ObservableProperty]
        private ObservableCollection<TrackerItem> _availableTrackers = new();

        [ObservableProperty]
        private ObservableCollection<StatusItem> _availableStatuses = new();

        private TrackerItem? _selectedTracker;
        public TrackerItem? SelectedTracker
        {
            get => _selectedTracker;
            set
            {
                // 参照比較ではなく、IDベースの比較を使用
                var currentId = _selectedTracker?.Id ?? 0;
                var newId = value?.Id ?? 0;
                var isDifferent = currentId != newId;

                if (isDifferent)
                {
                    _selectedTracker = value;
                    OnPropertyChanged(nameof(SelectedTracker));
                }
            }
        }

        private StatusItem? _selectedStatus;
        public StatusItem? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                // 参照比較ではなく、IDベースの比較を使用
                var currentId = _selectedStatus?.Id ?? 0;
                var newId = value?.Id ?? 0;
                var isDifferent = currentId != newId;

                if (isDifferent)
                {
                    _selectedStatus = value;
                    OnPropertyChanged(nameof(SelectedStatus));
                }
            }
        }

        private bool _isUpdatingTracker = false;
        private bool _isUpdatingStatus = false;

        public async Task OnNavigatedToAsync()
        {
            try
            {
                await InitializeViewModel();
            }
            catch
            {
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
            }
        }

        public virtual async Task OnNavigatedTo()
        {
            try
            {
                // OnNavigatedToAsyncで既に初期化が完了している可能性があるため、重複実行を避ける
                if (AvailableTrackers.Count == 0)
                {
                    await InitializeViewModel();
                }
            }
            catch
            {
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
            }
        }

        private async Task InitializeViewModel()
        {
            try
            {
                // 保存された設定を読み込み
                AppConfig.Load();
                
                RedmineHost = AppConfig.RedmineHost;
                ApiKey = AppConfig.ApiKey;
            
                // 保存されたテーマ設定を読み込む
                CurrentTheme = AppConfig.ApplicationTheme;
                
                // アプリケーション起動時の自動トラッカー一覧取得
                if (!string.IsNullOrEmpty(RedmineHost) && !string.IsNullOrEmpty(ApiKey))
                {
                    // まず、AppConfigから保存されたトラッカー一覧を確認
                    if (AppConfig.AvailableTrackers.Count > 0)
                    {
                        await LoadTrackersFromAppConfig();
                    }
                    else
                    {
                        try
                        {
                            await LoadTrackersAsync();
                        }
                        catch
                        {
                            // エラーが発生した場合はフォールバックトラッカーを設定
                            await SetFallbackTrackers();
                        }
                    }

                    // ステータス一覧も読み込み
                    if (AppConfig.AvailableStatuses.Count > 0)
                    {
                        await LoadStatusesFromAppConfig();
                    }
                    else
                    {
                        try
                        {
                            await LoadStatusesAsync();
                        }
                        catch
                        {
                            // エラーが発生した場合は何もしない
                        }
                    }
                }
                else
                {
                    // フォールバックトラッカーを設定
                    await SetFallbackTrackers();
                }
                
                AppVersion = $"RedmineClient - {GetAssemblyVersion()}";
            }
            catch
            {
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
            }
        }

        /// <summary>
        /// AppConfigから保存されたトラッカー一覧を読み込み
        /// </summary>
        private async Task LoadTrackersFromAppConfig()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableTrackers.Clear();
                foreach (var tracker in AppConfig.AvailableTrackers)
                {
                    AvailableTrackers.Add(tracker);
                }
                
                // 保存されたトラッカーIDを選択
                var savedTrackerId = AppConfig.DefaultTrackerId;
                
                var defaultTracker = AvailableTrackers.FirstOrDefault(t => t.Id == savedTrackerId);
                
                if (defaultTracker != null)
                {
                    // 現在選択されているトラッカーと同じIDの場合は、変更を避ける
                    if (SelectedTracker?.Id != defaultTracker.Id)
                    {
                        SelectedTracker = defaultTracker;
                    }
                }
                else
                {
                    // 保存されたIDが見つからない場合は最初のトラッカーを選択
                    if (AvailableTrackers.Count > 0)
                    {
                        SelectedTracker = AvailableTrackers[0];
                    }
                }
            });
        }

        /// <summary>
        /// AppConfigから保存されたステータス一覧を読み込み
        /// </summary>
        private async Task LoadStatusesFromAppConfig()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableStatuses.Clear();
                foreach (var status in AppConfig.AvailableStatuses)
                {
                    AvailableStatuses.Add(status);
                }
                
                // デフォルトステータス（新規）を選択
                var defaultStatus = AvailableStatuses.FirstOrDefault(s => s.Id == 1);
                if (defaultStatus != null)
                {
                    SelectedStatus = defaultStatus;
                }
                else if (AvailableStatuses.Count > 0)
                {
                    SelectedStatus = AvailableStatuses[0];
                }
            });
        }

        /// <summary>
        /// フォールバックトラッカーを設定（Redmine接続ができない場合）
        /// </summary>
        private async Task SetFallbackTrackers()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableTrackers.Clear();
                
                var bugTracker = new TrackerItem { Id = 1, Name = "バグ" };
                AvailableTrackers.Add(bugTracker);
                
                var featureTracker = new TrackerItem { Id = 2, Name = "機能" };
                AvailableTrackers.Add(featureTracker);
                
                var supportTracker = new TrackerItem { Id = 3, Name = "サポート" };
                AvailableTrackers.Add(supportTracker);
                
                // 保存されたトラッカーIDを選択
                var savedTrackerId = AppConfig.DefaultTrackerId;
                SelectedTracker = AvailableTrackers.FirstOrDefault(t => t.Id == savedTrackerId);
                
                if (SelectedTracker == null && AvailableTrackers.Count > 0)
                {
                    SelectedTracker = AvailableTrackers[0];
                }
            });
        }

        private async Task LoadTrackersAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(RedmineHost) && !string.IsNullOrEmpty(ApiKey))
                {
                    using var redmineService = new RedmineClient.Services.RedmineService(RedmineHost, ApiKey);
                    var trackers = await redmineService.GetTrackersAsync();
                    
                    // UIスレッドで実行
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AvailableTrackers.Clear();
                        foreach (var tracker in trackers)
                        {
                            // TrackerをTrackerItemに変換
                            var trackerItem = new TrackerItem(tracker);
                            AvailableTrackers.Add(trackerItem);
                        }
                        
                        // 保存されたトラッカーIDを選択
                        var savedTrackerId = AppConfig.DefaultTrackerId;
                        var defaultTracker = AvailableTrackers.FirstOrDefault(t => t.Id == savedTrackerId);
                        
                        // 選択状態を設定
                        if (defaultTracker != null)
                        {
                            // 現在選択されているトラッカーと同じIDの場合は、変更を避ける
                            if (SelectedTracker?.Id != defaultTracker.Id)
                            {
                                SelectedTracker = defaultTracker;
                            }
                        }
                        else
                        {
                            // 保存されたIDが見つからない場合は最初のトラッカーを選択
                            if (AvailableTrackers.Count > 0)
                            {
                                SelectedTracker = AvailableTrackers[0];
                            }
                        }
                    });
                }
            }
            catch
            {
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
            }
        }

        private async Task LoadStatusesAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(RedmineHost) && !string.IsNullOrEmpty(ApiKey))
                {
                    using var redmineService = new RedmineClient.Services.RedmineService(RedmineHost, ApiKey);
                    var statuses = await redmineService.GetStatusesAsync();
                    
                    // UIスレッドで実行
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AvailableStatuses.Clear();
                        foreach (var status in statuses)
                        {
                            // IssueStatusをStatusItemに変換
                            var statusItem = new StatusItem(status);
                            AvailableStatuses.Add(statusItem);
                        }
                    });
                }
            }
            catch
            {
                // エラーが発生した場合は何もしない
            }
        }

        public virtual async Task OnNavigatedFromAsync()
        {
            using CancellationTokenSource cts = new();

            await DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        public virtual async Task OnNavigatedFrom()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Dispatches the specified Func on the UI thread.
        /// </summary>
        /// <param name="callback">The Func to be dispatched.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected static async Task DispatchAsync<TResult>(Func<TResult> callback, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(callback);
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            try
            {
                switch (parameter)
                {
                    case "theme_light":
                        if (CurrentTheme == ApplicationTheme.Light)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Light);
                        CurrentTheme = ApplicationTheme.Light;
                        AppConfig.ApplicationTheme = ApplicationTheme.Light;
                        AppConfig.Save();

                        break;

                    default:
                        if (CurrentTheme == ApplicationTheme.Dark)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                        CurrentTheme = ApplicationTheme.Dark;
                        AppConfig.ApplicationTheme = ApplicationTheme.Dark;
                        AppConfig.Save();

                        break;
                }
            }
            catch
            {
                // エラーが発生した場合は何もしない
            }
        }

        [RelayCommand]
        private async Task Connect()
        {
            try
            {
                if (string.IsNullOrEmpty(RedmineHost) || string.IsNullOrEmpty(ApiKey))
                {
                    WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "ホストURLとAPIキーを入力してください。" });
                    return;
                }

                // 接続テストとトラッカー一覧、ステータス一覧取得
                await LoadTrackersAsync();
                await LoadStatusesAsync();
                
                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "接続成功！トラッカー一覧とステータス一覧を取得しました。" });
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = $"接続失敗: {ex.Message}" });
            }
        }

        [RelayCommand]
        private void OnSave()
        {
            try
            {
                AppConfig.RedmineHost = RedmineHost;
                AppConfig.ApiKey = ApiKey;
                
                // トラッカー設定も保存
                if (SelectedTracker != null)
                {
                    AppConfig.DefaultTrackerId = SelectedTracker.Id;
                }

                // ステータス設定も保存
                if (SelectedStatus != null)
                {
                    AppConfig.DefaultStatusId = SelectedStatus.Id;
                }
                
                AppConfig.Save();

                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "設定を保存しました。" });
            }
            catch (Exception ex)
            {
                // エラーメッセージをユーザーに表示
                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = $"設定の保存中にエラーが発生しました: {ex.Message}" });
            }
        }

        private void OnTrackerSelected(TrackerItem? selectedTracker)
        {
            // 無限ループを防ぐ
            if (_isUpdatingTracker)
            {
                return;
            }

            if (selectedTracker != null)
            {
                try
                {
                    _isUpdatingTracker = true;
                    
                    // 同じトラッカーが選択されている場合は何もしない
                    if (SelectedTracker?.Id == selectedTracker.Id)
                    {
                        return;
                    }
                    
                    // AvailableTrackersから同じIDのTrackerオブジェクトを取得（参照問題を回避）
                    var existingTracker = AvailableTrackers.FirstOrDefault(t => t.Id == selectedTracker.Id);
                    if (existingTracker != null)
                    {
                        // AvailableTrackersから同じIDのTrackerオブジェクトを取得（参照問題を回避）
                        var trackerFromCollection = AvailableTrackers.FirstOrDefault(t => t.Id == selectedTracker.Id);
                        if (trackerFromCollection != null)
                        {
                            SelectedTracker = trackerFromCollection;
                        }
                        else
                        {
                            SelectedTracker = existingTracker;
                        }
                    }
                    else
                    {
                        // 見つからない場合は元のオブジェクトを使用
                        SelectedTracker = selectedTracker;
                    }
                    
                    // 設定を保存
                    AppConfig.DefaultTrackerId = selectedTracker.Id;
                    AppConfig.Save();
                    
                    // 強制的にUIを更新（AvailableTrackersコレクションをリフレッシュ）
                    var currentTrackers = AvailableTrackers.ToList();
                    AvailableTrackers.Clear();
                    foreach (var tracker in currentTrackers)
                    {
                        AvailableTrackers.Add(tracker);
                    }
                }
                finally
                {
                    _isUpdatingTracker = false;
                }
            }
        }

        public ICommand OnTrackerSelectedCommand => new RelayCommand<TrackerItem?>(OnTrackerSelected);

        private void OnStatusSelected(StatusItem? selectedStatus)
        {
            // 無限ループを防ぐ
            if (_isUpdatingStatus)
            {
                return;
            }

            if (selectedStatus != null)
            {
                try
                {
                    _isUpdatingStatus = true;
                    
                    // 同じステータスが選択されている場合は何もしない
                    if (SelectedStatus?.Id == selectedStatus.Id)
                    {
                        return;
                    }
                    
                    // AvailableStatusesから同じIDのStatusオブジェクトを取得（参照問題を回避）
                    var existingStatus = AvailableStatuses.FirstOrDefault(s => s.Id == selectedStatus.Id);
                    if (existingStatus != null)
                    {
                        var statusFromCollection = AvailableStatuses.FirstOrDefault(s => s.Id == selectedStatus.Id);
                        if (statusFromCollection != null)
                        {
                            SelectedStatus = statusFromCollection;
                        }
                        else
                        {
                            SelectedStatus = existingStatus;
                        }
                    }
                    else
                    {
                        // 見つからない場合は元のオブジェクトを使用
                        SelectedStatus = selectedStatus;
                    }
                    
                    // 強制的にUIを更新（AvailableStatusesコレクションをリフレッシュ）
                    var currentStatuses = AvailableStatuses.ToList();
                    AvailableStatuses.Clear();
                    foreach (var status in currentStatuses)
                    {
                        AvailableStatuses.Add(status);
                    }
                }
                finally
                {
                    _isUpdatingStatus = false;
                }
            }
        }

        public ICommand OnStatusSelectedCommand => new RelayCommand<StatusItem?>(OnStatusSelected);
    }
}
