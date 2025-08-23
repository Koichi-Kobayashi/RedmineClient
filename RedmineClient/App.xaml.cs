using System.IO;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RedmineClient.DependencyModel;
using RedmineClient.Models;
using RedmineClient.Services;
using RedmineClient.ViewModels.Pages;
using RedmineClient.ViewModels.Windows;
using RedmineClient.Views.Pages;
using RedmineClient.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using Wpf.Ui.Appearance;

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
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
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
                services.AddTransient<DashboardPage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<DashboardViewModel>();

                services.AddTransient<IWindowFactory, WindowFactory>();

                // All other pages and view models
                services.AddTransientFromNamespace("RedmineClient.Views", RedmineClientAssembly.Asssembly);
                services.AddTransientFromNamespace("RedmineClient.ViewModels", RedmineClientAssembly.Asssembly);

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
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // アプリケーション起動時に設定を読み込み
            AppConfig.Load();
            
            // テーマ設定を適用
            AppConfig.ApplyTheme();
            
            _host.Start();
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
