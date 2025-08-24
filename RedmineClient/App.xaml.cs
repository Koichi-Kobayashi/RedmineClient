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
                services.AddSingleton<INavigationWindow, MainWindow>();
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
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                AppConfig.Load();
                AppConfig.ApplyTheme();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定の読み込み中にエラーが発生しました: {ex.Message}");
                // 設定の読み込みに失敗してもアプリケーションは起動する
            }

            try
            {
                // ホストを開始
                _host.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ホストの開始中にエラーが発生しました: {ex.Message}");
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
