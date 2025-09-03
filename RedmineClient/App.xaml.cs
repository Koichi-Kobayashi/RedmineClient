using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RedmineClient.Services;
using RedmineClient.ViewModels.Pages;
using RedmineClient.ViewModels.Windows;
using RedmineClient.Views.Pages;
using RedmineClient.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.DependencyInjection;

namespace RedmineClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                try
                {
                    var entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly?.Location != null)
                    {
                        var basePath = Path.GetDirectoryName(entryAssembly.Location);
                        if (!string.IsNullOrEmpty(basePath))
                        {
                            c.SetBasePath(basePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // アセンブリの場所取得でエラーが発生した場合は、デフォルトの設定を使用
                    System.Diagnostics.Debug.WriteLine($"アプリケーション設定のベースパス設定でエラー: {ex.Message}");
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<WindowsProviderService>();

                // Main window with navigation
                services.AddSingleton<MainWindow>();
                services.AddSingleton<INavigationWindow>(provider => provider.GetRequiredService<MainWindow>());
                services.AddSingleton<MainWindowViewModel>();

                // ページとViewModelをTransientに変更（メモリ効率向上）
                services.AddTransient<SettingsPage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<DashboardPage>();
                services.AddTransient<DashboardViewModel>();

                services.AddTransient<IWindowFactory, WindowFactory>();

                // All other pages and view models
                // AddTransientFromNamespaceは匿名型や内部クラスを誤って登録する可能性があるため、
                // 明示的に必要なクラスのみを登録
                services.AddTransient<WbsPage>();
                services.AddTransient<WbsViewModel>();

                // WBS V2 ページ（ガントチャート）
                services.AddTransient<WbsPageV2>();
                services.AddTransient<WbsV2ViewModel>();

                services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
            }).Build();

        public App() : base()
        {
        }

        /// <summary>
        /// Gets registered service.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// グローバル例外ハンドラーを設定
        /// </summary>
        private void SetupGlobalExceptionHandling()
        {
            // UIスレッドでの未処理例外をキャッチ
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            
            // 非UIスレッドでの未処理例外をキャッチ
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            
            // タスクでの未処理例外をキャッチ
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// UIスレッドでの未処理例外を処理
        /// </summary>
        private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"UIスレッド未処理例外: {e.Exception.Message}");
                
                // Redmine API関連の例外の場合は詳細ログを出力
                if (e.Exception is RedmineClient.Services.RedmineApiException redmineEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Redmine API 例外: {redmineEx.Message}");
                    if (redmineEx.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"内部例外: {redmineEx.InnerException.Message}");
                    }
                }
                
                // 例外を処理済みとしてマーク（アプリケーションクラッシュを防ぐ）
                e.Handled = true;
                
                // ユーザーにエラーメッセージを表示
                ShowErrorMessage($"予期しないエラーが発生しました: {e.Exception.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"例外ハンドラーでエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 非UIスレッドでの未処理例外を処理
        /// </summary>
        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"非UIスレッド未処理例外: {ex.Message}");
                    
                    // Redmine API関連の例外の場合は詳細ログを出力
                    if (ex is RedmineClient.Services.RedmineApiException redmineEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Redmine API 例外: {redmineEx.Message}");
                        if (redmineEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"内部例外: {redmineEx.InnerException.Message}");
                        }
                    }
                }
            }
            catch (Exception handlerEx)
            {
                System.Diagnostics.Debug.WriteLine($"例外ハンドラーでエラー: {handlerEx.Message}");
            }
        }

        /// <summary>
        /// タスクでの未処理例外を処理
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"タスク未処理例外: {e.Exception.Message}");
                
                // Redmine API関連の例外の場合は詳細ログを出力
                foreach (var innerEx in e.Exception.InnerExceptions)
                {
                    if (innerEx is RedmineClient.Services.RedmineApiException redmineEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Redmine API 例外: {redmineEx.Message}");
                        if (redmineEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"内部例外: {redmineEx.InnerException.Message}");
                        }
                    }
                }
                
                // 例外を処理済みとしてマーク
                e.SetObserved();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"例外ハンドラーでエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// エラーメッセージをユーザーに表示
        /// </summary>
        /// <param name="message">表示するエラーメッセージ</param>
        private void ShowErrorMessage(string message)
        {
            try
            {
                // UIスレッドでエラーメッセージを表示
                if (MainWindow?.Dispatcher != null)
                {
                    MainWindow.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            System.Windows.MessageBox.Show(
                                message,
                                "エラー",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でエラー: {ex.Message}");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Normal);
                }
                else
                {
                    // メインウィンドウが取得できない場合はデバッグ出力のみ
                    System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でメインウィンドウが取得できません: {message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"エラーメッセージ表示でエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            // グローバル例外ハンドラーを設定
            SetupGlobalExceptionHandling();
            
            // SSL証明書の検証を無効化（開発環境や自己署名証明書を使用している場合）
            // 注意: 本番環境では適切な証明書を使用してください
            try
            {
                // 現代的なHttpClientHandlerを使用してSSL証明書検証を無効化
                // ただし、Redmine.Net.Apiライブラリが内部的にWebRequestを使用するため、
                // 完全な解決にはライブラリの更新が必要
                
                // グローバルなSSL証明書検証の無効化（Redmine.Net.Apiライブラリにも適用）
                // 警告は出るが、Redmine.Net.Apiライブラリの動作に必要
                #pragma warning disable SYSLIB0014
                System.Net.ServicePointManager.ServerCertificateValidationCallback += 
                    (sender, cert, chain, sslPolicyErrors) => true;
                
                // 追加のSSL設定
                System.Net.ServicePointManager.SecurityProtocol = 
                    System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;
                #pragma warning restore SYSLIB0014
                

            }
            catch (Exception)
            {
                // SSL証明書検証の無効化に失敗
            }

            try
            {
                AppConfig.Load();
                AppConfig.ApplyTheme();
            }
            catch (Exception ex)
            {
                // 設定の読み込みに失敗してもアプリケーションは起動する
                System.Diagnostics.Debug.WriteLine($"設定読み込みエラー: {ex.Message}");
                if (ex is FileNotFoundException fileEx)
                {
                    System.Diagnostics.Debug.WriteLine($"ファイルが見つかりません: {fileEx.FileName}");
                }
            }

            try
            {
                // アプリケーション起動時の自動Redmine接続処理（メインウィンドウ表示前に実行）
                try
                {
                    // 設定が完了している場合のみ自動接続を実行
                    if (!string.IsNullOrEmpty(AppConfig.RedmineHost) && !string.IsNullOrEmpty(AppConfig.ApiKey))
                    {
                        // 同期的に自動接続を実行（メインウィンドウ表示前に完了）
                        try
                        {
                            using (var redmineService = new RedmineService(AppConfig.RedmineHost, AppConfig.ApiKey))
                            {
                                // 接続テスト（タイムアウトを短く設定）
                                var isConnected = await redmineService.TestConnectionAsync();
                                if (isConnected)
                                {
                                    // トラッカー一覧を取得してデフォルト値を設定
                                    try
                                    {
                                        var trackers = await redmineService.GetTrackersAsync();
                                        if (trackers != null && trackers.Count > 0)
                                        {
                                            // 設定されたデフォルトトラッカーIDが有効かチェック
                                            var defaultTracker = trackers.FirstOrDefault(t => t.Id == AppConfig.DefaultTrackerId);
                                            if (defaultTracker == null)
                                            {
                                                // デフォルトトラッカーIDが無効な場合は、最初のトラッカーを設定
                                                var newDefaultTrackerId = trackers[0].Id;
                                                AppConfig.DefaultTrackerId = newDefaultTrackerId;
                                            }
                                            
                                            // トラッカー一覧をAppConfigに保存（SettingsViewModelで使用）
                                            try
                                            {
                                                // TrackerをTrackerItemに変換
                                                var trackerItems = trackers.Select(t => new TrackerItem(t)).ToList();
                                                AppConfig.SaveTrackers(trackerItems);
                                            }
                                            catch (Exception)
                                            {
                                                // トラッカー一覧の保存でエラー
                                            }

                                            // ステータス一覧を取得してAppConfigに保存
                                            try
                                            {
                                                // 修正内容: RedmineServiceにGetStatusesAsyncが存在しないため、GetIssueStatusesAsyncに置き換え
                                                // 変更前: var statuses = await redmineService.GetStatusesAsync();
                                                // 変更後: var statuses = await redmineService.GetIssueStatusesAsync();
                                                var statuses = await redmineService.GetIssueStatusesAsync();
                                                if (statuses != null && statuses.Count > 0)
                                                {
                                                    // IssueStatusをStatusItemに変換
                                                    var statusItems = statuses.Select(s => new StatusItem(s)).ToList();
                                                    AppConfig.SaveStatuses(statusItems);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                // ステータス一覧の保存でエラー
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // トラッカー・ステータス取得でエラー
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // 自動Redmine接続処理でエラー
                        }
                    }
                }
                catch
                {
                    // 自動Redmine接続処理の初期化でエラー
                }
                
                // ホストを開始
                _host.Start();
                // メインウィンドウを表示
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.ShowWindow();
            }
            catch
            {
                // ホストの開始に失敗した場合は、設定のみで起動を試行
            }

            base.OnStartup(e);
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                ApplicationThemeManager.Apply(AppConfig.ApplicationTheme);
            }
            catch
            {
                // デフォルトはライトテーマ
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
            }
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }


    }
}
