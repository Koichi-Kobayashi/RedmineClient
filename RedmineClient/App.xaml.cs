using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RedmineClient.Models;
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
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly?.Location != null)
                {
                    c.SetBasePath(Path.GetDirectoryName(entryAssembly.Location));
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
        /// Occurs when the application is loading.
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
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
                
                System.Diagnostics.Debug.WriteLine("SSL証明書検証を無効化しました（開発環境用）");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SSL証明書検証の無効化に失敗: {ex.Message}");
            }

            try
            {
                AppConfig.Load();
                AppConfig.ApplyTheme();
            }
            catch
            {
                // 設定の読み込みに失敗してもアプリケーションは起動する
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
                                    System.Diagnostics.Debug.WriteLine("起動時: Redmine接続成功");
                                    
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
                                                System.Diagnostics.Debug.WriteLine($"起動時: デフォルトトラッカーIDを更新: {newDefaultTrackerId}");
                                            }
                                            
                                            // トラッカー一覧をAppConfigに保存（SettingsViewModelで使用）
                                            try
                                            {
                                                // TrackerをTrackerItemに変換
                                                var trackerItems = trackers.Select(t => new TrackerItem(t)).ToList();
                                                AppConfig.SaveTrackers(trackerItems);
                                                System.Diagnostics.Debug.WriteLine($"起動時: トラッカー一覧を保存: {trackerItems.Count}件");
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"起動時: トラッカー一覧の保存でエラー: {ex.Message}");
                                            }

                                            // ステータス一覧を取得してAppConfigに保存
                                            try
                                            {
                                                var statuses = await redmineService.GetStatusesAsync();
                                                if (statuses != null && statuses.Count > 0)
                                                {
                                                    // IssueStatusをStatusItemに変換
                                                    var statusItems = statuses.Select(s => new StatusItem(s)).ToList();
                                                    AppConfig.SaveStatuses(statusItems);
                                                    System.Diagnostics.Debug.WriteLine($"起動時: ステータス一覧を保存: {statusItems.Count}件");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"起動時: ステータス一覧の保存でエラー: {ex.Message}");
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"起動時: トラッカー・ステータス取得でエラー: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("起動時: Redmine接続失敗");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // 自動Redmine接続処理でエラー
                            System.Diagnostics.Debug.WriteLine($"起動時: Redmine接続処理でエラー: {ex.Message}");
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

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
