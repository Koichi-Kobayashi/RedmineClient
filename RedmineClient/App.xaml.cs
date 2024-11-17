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

namespace RedmineClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private Dictionary<Type, Type> ViewModels { get; set; }

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

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddTransient<IWindowFactory, WindowFactory>();
                services.AddTransient<DashboardViewModel>();

                // All other pages and view models
                services.AddTransientFromNamespace("RedmineClient.Views", RedmineClientAssembly.Asssembly);
                services.AddTransientFromNamespace("RedmineClient.ViewModels", RedmineClientAssembly.Asssembly);

                services.Configure<AppConfig>(context.Configuration.GetSection(nameof(AppConfig)));
            }).Build();

        public App() : base()
        {
            // ViewModel と View の組み合わせを設定する
            ViewModels = new Dictionary<Type, Type>();
            ViewModels.Add(typeof(IssueWindowViewModel), typeof(IssueWindow));
        }

        /// <summary>
        /// ViewModelからViewを生成する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public Window CreateView<T>(T viewModel)
        {
            // ViewModel に対応する Viewが存在する？
            if (ViewModels.ContainsKey(viewModel.GetType()))
            {
                // View を生成し、DataContext に ViewModel を設定する
                Type viewType = ViewModels[viewModel.GetType()];
                Window wnd = Activator.CreateInstance(viewType) as Window;
                if (wnd != null)
                    wnd.DataContext = viewModel;
                return wnd;
            }
            else
                return null;
        }

        /// <summary>
        /// ViewModelからモーダルでViewを表示する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public bool ShowModalView<T>(T viewModel)
        {
            Window view = CreateView(viewModel);
            if (view != null)
                return (view.ShowDialog() == true);
            else
                return false;
        }

        // ViewModeからモードレスでViewを表示する
        public void ShowView<T>(T viewModel)
        {
            Window view = CreateView(viewModel);
            if (view != null)
                view.Show();
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
            _host.Start();
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
