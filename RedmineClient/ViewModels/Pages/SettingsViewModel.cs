using CommunityToolkit.Mvvm.Messaging;
using RedmineClient.Models;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using System.Collections.ObjectModel;
using Redmine.Net.Api.Types;

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
        private ObservableCollection<Tracker> _availableTrackers = new();

        [ObservableProperty]
        private Tracker? _selectedTracker;

        partial void OnSelectedTrackerChanged(Tracker? value)
        {
            System.Diagnostics.Debug.WriteLine($"OnSelectedTrackerChanged: 値が変更されました - {value?.Name ?? "null"} (ID: {value?.Id ?? 0})");
            
            if (value != null)
            {
                try
                {
                    AppConfig.DefaultTrackerId = value.Id;
                    System.Diagnostics.Debug.WriteLine($"OnSelectedTrackerChanged: 設定保存開始 - トラッカー: {value.Name} (ID: {value.Id})");
                    AppConfig.Save();
                    System.Diagnostics.Debug.WriteLine($"トラッカー設定を保存しました: {value.Name} (ID: {value.Id})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OnSelectedTrackerChanged: 設定保存でエラー - {ex.GetType().Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"OnSelectedTrackerChanged: スタックトレース: {ex.StackTrace}");
                }
            }
        }

        public Task OnNavigatedToAsync()
        {
            using CancellationTokenSource cts = new();

            return DispatchAsync(OnNavigatedFrom, cts.Token);
        }

        public virtual async Task OnNavigatedTo()
        {
            await InitializeViewModel();
        }

        private async Task InitializeViewModel()
        {
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: InitializeViewModel開始");
            
            try
            {
                // 保存された設定を読み込み
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: AppConfig.Load()呼び出し前");
                AppConfig.Load();
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: AppConfig.Load()呼び出し完了");
                
                RedmineHost = AppConfig.RedmineHost;
                ApiKey = AppConfig.ApiKey;
            
            // 保存されたテーマ設定を読み込む
            CurrentTheme = AppConfig.ApplicationTheme;
            
            // 保存されたトラッカーIDがあれば、デフォルトトラッカーを設定
            if (AppConfig.DefaultTrackerId > 0)
            {
                // UIスレッドで実行
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // デフォルトトラッカーを一時的に設定
                    var defaultTracker = new Redmine.Net.Api.Types.Tracker();
                    var idProperty = typeof(Redmine.Net.Api.Types.Tracker).GetProperty("Id");
                    var nameProperty = typeof(Redmine.Net.Api.Types.Tracker).GetProperty("Name");
                    
                    if (idProperty?.CanWrite == true)
                    {
                        idProperty.SetValue(defaultTracker, AppConfig.DefaultTrackerId);
                    }
                    if (nameProperty?.CanWrite == true)
                    {
                        nameProperty.SetValue(defaultTracker, $"トラッカー {AppConfig.DefaultTrackerId}");
                    }
                    
                    AvailableTrackers.Add(defaultTracker);
                    SelectedTracker = defaultTracker;
                    
                    System.Diagnostics.Debug.WriteLine($"初期化時にデフォルトトラッカーを設定: {defaultTracker.Name} (ID: {AppConfig.DefaultTrackerId})");
                });
            }
            
                AppVersion = $"RedmineClient - {GetAssemblyVersion()}";
                
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: InitializeViewModel完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: InitializeViewModelでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: スタックトレース: {ex.StackTrace}");
            }
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
                            AvailableTrackers.Add(tracker);
                        }
                        
                        // 保存されたトラッカーIDを選択
                        var savedTrackerId = AppConfig.DefaultTrackerId;
                        var defaultTracker = AvailableTrackers.FirstOrDefault(t => t.Id == savedTrackerId);
                        
                        // 選択状態を設定
                        if (defaultTracker != null)
                        {
                            SelectedTracker = defaultTracker;
                            System.Diagnostics.Debug.WriteLine($"トラッカー一覧取得成功: {trackers.Count}件, 選択されたトラッカー: {defaultTracker.Name} (ID: {savedTrackerId})");
                        }
                        else
                        {
                            // 保存されたIDが見つからない場合は最初のトラッカーを選択
                            if (AvailableTrackers.Count > 0)
                            {
                                SelectedTracker = AvailableTrackers[0];
                                System.Diagnostics.Debug.WriteLine($"トラッカー一覧取得成功: {trackers.Count}件, 保存されたID {savedTrackerId}が見つからないため、最初のトラッカーを選択: {AvailableTrackers[0].Name}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"トラッカー一覧取得成功: {trackers.Count}件, 選択可能なトラッカーなし");
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"トラッカー読み込みエラー: {ex.Message}");
                // エラーが発生した場合はデフォルト値を設定
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AvailableTrackers.Clear();
                    
                    var bugTracker = new Redmine.Net.Api.Types.Tracker();
                    var bugIdProperty = typeof(Redmine.Net.Api.Types.Tracker).GetProperty("Id");
                    if (bugIdProperty?.CanWrite == true)
                    {
                        bugIdProperty.SetValue(bugTracker, 1);
                    }
                    var bugNameProperty = typeof(Redmine.Net.Api.Types.Tracker).GetProperty("Name");
                    if (bugNameProperty?.CanWrite == true)
                    {
                        bugNameProperty.SetValue(bugTracker, "バグ");
                    }
                    AvailableTrackers.Add(bugTracker);
                    
                    var featureTracker = new Redmine.Net.Api.Types.Tracker();
                    if (bugIdProperty?.CanWrite == true)
                    {
                        bugIdProperty.SetValue(featureTracker, 2);
                    }
                    if (bugNameProperty?.CanWrite == true)
                    {
                        bugNameProperty.SetValue(featureTracker, "機能");
                    }
                    AvailableTrackers.Add(featureTracker);
                    
                    var supportTracker = new Redmine.Net.Api.Types.Tracker();
                    if (bugIdProperty?.CanWrite == true)
                    {
                        bugIdProperty.SetValue(supportTracker, 3);
                    }
                    if (bugNameProperty?.CanWrite == true)
                    {
                        bugNameProperty.SetValue(supportTracker, "サポート");
                    }
                    AvailableTrackers.Add(supportTracker);
                    
                    SelectedTracker = AvailableTrackers.FirstOrDefault(t => t.Id == AppConfig.DefaultTrackerId);
                    
                    System.Diagnostics.Debug.WriteLine($"フォールバックトラッカーを設定: {AvailableTrackers.Count}件");
                });
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
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnChangeTheme開始 - パラメータ: {parameter}");
                
                switch (parameter)
                {
                    case "theme_light":
                        if (CurrentTheme == ApplicationTheme.Light)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Light);
                        CurrentTheme = ApplicationTheme.Light;
                        AppConfig.ApplicationTheme = ApplicationTheme.Light;
                        System.Diagnostics.Debug.WriteLine("SettingsViewModel: ライトテーマを保存中...");
                        AppConfig.Save();
                        System.Diagnostics.Debug.WriteLine("SettingsViewModel: ライトテーマ保存完了");

                        break;

                    default:
                        if (CurrentTheme == ApplicationTheme.Dark)
                            break;

                        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                        CurrentTheme = ApplicationTheme.Dark;
                        AppConfig.ApplicationTheme = ApplicationTheme.Dark;
                        System.Diagnostics.Debug.WriteLine("SettingsViewModel: ダークテーマを保存中...");
                        AppConfig.Save();
                        System.Diagnostics.Debug.WriteLine("SettingsViewModel: ダークテーマ保存完了");

                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnChangeThemeでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: スタックトレース: {ex.StackTrace}");
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

                // 接続テストとトラッカー一覧取得
                await LoadTrackersAsync();
                
                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "接続成功！トラッカー一覧を取得しました。" });
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = $"接続失敗: {ex.Message}" });
                System.Diagnostics.Debug.WriteLine($"接続エラー: {ex.Message}");
            }
        }

        [RelayCommand]
        private void OnSave()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnSave開始");
                
                AppConfig.RedmineHost = RedmineHost;
                AppConfig.ApiKey = ApiKey;
                
                // トラッカー設定も保存
                if (SelectedTracker != null)
                {
                    AppConfig.DefaultTrackerId = SelectedTracker.Id;
                    System.Diagnostics.Debug.WriteLine($"SettingsViewModel: トラッカー設定を設定: {SelectedTracker.Name} (ID: {SelectedTracker.Id})");
                }
                
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: 設定を保存中...");
                AppConfig.Save();
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: 設定保存完了");

                WeakReferenceMessenger.Default.Send(new SnackbarMessage { Message = "設定を保存しました。" });
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnSave完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnSaveでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: スタックトレース: {ex.StackTrace}");
            }
        }
    }
}
