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

        private TrackerItem? _selectedTracker;
        public TrackerItem? SelectedTracker
        {
            get => _selectedTracker;
            set
            {
                System.Diagnostics.Debug.WriteLine($"SelectedTrackerプロパティset呼び出し: {_selectedTracker?.Name ?? "null"} -> {value?.Name ?? "null"}");
                
                // 参照比較ではなく、IDベースの比較を使用
                var currentId = _selectedTracker?.Id ?? 0;
                var newId = value?.Id ?? 0;
                var isDifferent = currentId != newId;
                
                System.Diagnostics.Debug.WriteLine($"ID比較: {currentId} != {newId} = {isDifferent}");
                
                if (isDifferent)
                {
                    System.Diagnostics.Debug.WriteLine($"IDが異なるため更新を実行");
                    _selectedTracker = value;
                    System.Diagnostics.Debug.WriteLine($"_selectedTrackerフィールド更新完了: {_selectedTracker?.Name ?? "null"}");
                    
                    OnPropertyChanged(nameof(SelectedTracker));
                    System.Diagnostics.Debug.WriteLine($"OnPropertyChanged呼び出し完了");
                    
                    // UI更新のデバッグ情報
                    System.Diagnostics.Debug.WriteLine($"UI更新完了 - SelectedTracker: {_selectedTracker?.Name ?? "null"} (ID: {_selectedTracker?.Id ?? 0})");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"IDが同じため更新をスキップ");
                }
                
                System.Diagnostics.Debug.WriteLine($"SelectedTrackerプロパティset完了: {_selectedTracker?.Name ?? "null"}");
            }
        }

        private bool _isUpdatingTracker = false;

        public async Task OnNavigatedToAsync()
        {
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnNavigatedToAsync開始");
            try
            {
                await InitializeViewModel();
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnNavigatedToAsync完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnNavigatedToAsyncでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnNavigatedToAsyncスタックトレース: {ex.StackTrace}");
            }
        }

        public virtual async Task OnNavigatedTo()
        {
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnNavigatedTo開始");
            try
            {
                // OnNavigatedToAsyncで既に初期化が完了している可能性があるため、重複実行を避ける
                if (AvailableTrackers.Count == 0)
                {
                    await InitializeViewModel();
                }
                System.Diagnostics.Debug.WriteLine("SettingsViewModel: OnNavigatedTo完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnNavigatedToでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnNavigatedToスタックトレース: {ex.StackTrace}");
            }
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
                
                // AppConfigの状態を確認
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: AppConfig状態確認 - RedmineHost: '{RedmineHost}', ApiKey長: {ApiKey.Length}, AvailableTrackers数: {AppConfig.AvailableTrackers.Count}");
                
                // アプリケーション起動時の自動トラッカー一覧取得
                if (!string.IsNullOrEmpty(RedmineHost) && !string.IsNullOrEmpty(ApiKey))
                {
                    System.Diagnostics.Debug.WriteLine("SettingsViewModel: 自動トラッカー一覧取得を開始");
                    
                    // まず、AppConfigから保存されたトラッカー一覧を確認
                    if (AppConfig.AvailableTrackers.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"SettingsViewModel: AppConfigから保存されたトラッカー一覧を読み込み - {AppConfig.AvailableTrackers.Count}件");
                        await LoadTrackersFromAppConfig();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SettingsViewModel: AppConfigに保存されたトラッカー一覧がないため、Redmineから取得");
                        try
                        {
                            await LoadTrackersAsync();
                            System.Diagnostics.Debug.WriteLine("SettingsViewModel: 自動トラッカー一覧取得完了");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"SettingsViewModel: 自動トラッカー一覧取得でエラー: {ex.Message}");
                            // エラーが発生した場合はフォールバックトラッカーを設定
                            await SetFallbackTrackers();
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SettingsViewModel: Redmine設定が不完全なため、自動トラッカー一覧取得をスキップ");
                    // フォールバックトラッカーを設定
                    await SetFallbackTrackers();
                }
                
                AppVersion = $"RedmineClient - {GetAssemblyVersion()}";
                
                // 最終的な選択状態を確認
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: InitializeViewModel完了 - 最終選択状態: {SelectedTracker?.Name ?? "null"} (ID: {SelectedTracker?.Id ?? 0})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: InitializeViewModelでエラー - {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: スタックトレース: {ex.StackTrace}");
                
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
            }
        }

        /// <summary>
        /// AppConfigから保存されたトラッカー一覧を読み込み
        /// </summary>
        private async Task LoadTrackersFromAppConfig()
        {
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: LoadTrackersFromAppConfig開始");
            
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableTrackers.Clear();
                foreach (var tracker in AppConfig.AvailableTrackers)
                {
                    AvailableTrackers.Add(tracker);
                }
                
                // 保存されたトラッカーIDを選択
                var savedTrackerId = AppConfig.DefaultTrackerId;
                System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: 保存されたトラッカーID: {savedTrackerId}");
                
                var defaultTracker = AvailableTrackers.FirstOrDefault(t => t.Id == savedTrackerId);
                
                if (defaultTracker != null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: 保存されたID {savedTrackerId}に対応するトラッカーを発見: {defaultTracker.Name}");
                    
                    // 現在選択されているトラッカーと同じIDの場合は、変更を避ける
                    if (SelectedTracker?.Id != defaultTracker.Id)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: トラッカーを選択状態に設定: {defaultTracker.Name} (ID: {defaultTracker.Id})");
                        SelectedTracker = defaultTracker;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: 既に同じトラッカーが選択されています: {defaultTracker.Name} (ID: {defaultTracker.Id})");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"AppConfigからトラッカー一覧を読み込み: {AvailableTrackers.Count}件, 選択されたトラッカー: {defaultTracker.Name} (ID: {savedTrackerId})");
                }
                else
                {
                    // 保存されたIDが見つからない場合は最初のトラッカーを選択
                    if (AvailableTrackers.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: 保存されたID {savedTrackerId}が見つからないため、最初のトラッカーを選択: {AvailableTrackers[0].Name} (ID: {AvailableTrackers[0].Id})");
                        SelectedTracker = AvailableTrackers[0];
                        System.Diagnostics.Debug.WriteLine($"AppConfigからトラッカー一覧を読み込み: {AvailableTrackers.Count}件, 保存されたID {savedTrackerId}が見つからないため、最初のトラッカーを選択: {AvailableTrackers[0].Name}");
                    }
                }
                
                // 選択状態を確認
                System.Diagnostics.Debug.WriteLine($"LoadTrackersFromAppConfig: 最終的な選択状態 - SelectedTracker: {SelectedTracker?.Name ?? "null"} (ID: {SelectedTracker?.Id ?? 0})");
            });
            
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: LoadTrackersFromAppConfig完了");
        }

        /// <summary>
        /// フォールバックトラッカーを設定（Redmine接続ができない場合）
        /// </summary>
        private async Task SetFallbackTrackers()
        {
            System.Diagnostics.Debug.WriteLine("SettingsViewModel: フォールバックトラッカーを設定開始");
            
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
                
                System.Diagnostics.Debug.WriteLine($"フォールバックトラッカーを設定: {AvailableTrackers.Count}件, 選択されたトラッカー: {SelectedTracker?.Name ?? "なし"}");
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
                                System.Diagnostics.Debug.WriteLine($"LoadTrackersAsync: トラッカーを選択状態に設定: {defaultTracker.Name} (ID: {defaultTracker.Id})");
                                SelectedTracker = defaultTracker;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"LoadTrackersAsync: 既に同じトラッカーが選択されています: {defaultTracker.Name} (ID: {defaultTracker.Id})");
                            }
                            
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
                // エラーが発生した場合はフォールバックトラッカーを設定
                await SetFallbackTrackers();
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
                System.Diagnostics.Debug.WriteLine($"SettingsViewModel: OnSave呼び出し元スタックトレース: {Environment.StackTrace}");
                
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

        private void OnTrackerSelected(TrackerItem? selectedTracker)
        {
            // 無限ループを防ぐ
            if (_isUpdatingTracker)
            {
                System.Diagnostics.Debug.WriteLine($"OnTrackerSelected: 更新中のためスキップ - {selectedTracker?.Name ?? "null"}");
                return;
            }

            if (selectedTracker != null)
            {
                try
                {
                    _isUpdatingTracker = true;
                    
                    System.Diagnostics.Debug.WriteLine($"OnTrackerSelected開始: {selectedTracker.Name} (ID: {selectedTracker.Id})");
                    System.Diagnostics.Debug.WriteLine($"更新前のSelectedTracker: {SelectedTracker?.Name ?? "null"} (ID: {SelectedTracker?.Id ?? 0})");
                    
                    // 同じトラッカーが選択されている場合は何もしない
                    if (SelectedTracker?.Id == selectedTracker.Id)
                    {
                        System.Diagnostics.Debug.WriteLine($"同じトラッカーが選択されているためスキップ: {selectedTracker.Name}");
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
                        System.Diagnostics.Debug.WriteLine($"コレクションからTrackerオブジェクトを取得: {trackerFromCollection.Name} (ID: {trackerFromCollection.Id})");
                    }
                    else
                    {
                        SelectedTracker = existingTracker;
                        System.Diagnostics.Debug.WriteLine($"既存のTrackerオブジェクトを使用: {existingTracker.Name} (ID: {existingTracker.Id})");
                    }
                }
                else
                {
                    // 見つからない場合は元のオブジェクトを使用
                    SelectedTracker = selectedTracker;
                    System.Diagnostics.Debug.WriteLine($"元のTrackerオブジェクトを使用: {selectedTracker.Name} (ID: {selectedTracker.Id})");
                }
                    
                    System.Diagnostics.Debug.WriteLine($"更新後のSelectedTracker: {SelectedTracker?.Name ?? "null"} (ID: {SelectedTracker?.Id ?? 0})");
                    
                    // 設定を保存
                    AppConfig.DefaultTrackerId = selectedTracker.Id;
                    AppConfig.Save();
                    
                    System.Diagnostics.Debug.WriteLine($"トラッカーを選択しました: {selectedTracker.Name} (ID: {selectedTracker.Id})");
                    
                                    // 強制的にUIを更新（AvailableTrackersコレクションをリフレッシュ）
                var currentTrackers = AvailableTrackers.ToList();
                AvailableTrackers.Clear();
                foreach (var tracker in currentTrackers)
                {
                    AvailableTrackers.Add(tracker);
                }
                
                System.Diagnostics.Debug.WriteLine($"強制UI更新完了 - AvailableTrackers数: {AvailableTrackers.Count}, SelectedTracker: {SelectedTracker?.Name ?? "null"}");
                }
                finally
                {
                    _isUpdatingTracker = false;
                }
            }
        }

        public ICommand OnTrackerSelectedCommand => new RelayCommand<TrackerItem?>(OnTrackerSelected);
    }
}
